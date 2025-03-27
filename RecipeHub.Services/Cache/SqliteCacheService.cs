using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RecipeHub.Services.Cache
{
    /// <summary>
    /// Implémentation du service de cache utilisant SQLite pour la persistance.
    /// </summary>
    public class SqliteCacheService : ICacheService, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private bool _disposed = false;

        /// <summary>
        /// Constructeur du service de cache SQLite.
        /// </summary>
        /// <param name="databaseName">Nom du fichier de base de données (sans extension)</param>
        public SqliteCacheService(string databaseName = "recipehub_cache")
        {
            // Définir le chemin de la base de données dans le dossier AppData de l'utilisateur
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecipeHub");

            // Créer le répertoire s'il n'existe pas
            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            _databasePath = Path.Combine(appDataPath, $"{databaseName}.db");
            _connectionString = $"Data Source={_databasePath}";

            // Initialisation de la base de données
            InitializeDatabase().Wait();
        }

        /// <summary>
        /// Initialise la base de données et crée la table du cache si nécessaire.
        /// </summary>
        private async Task InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS CacheItems (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    ExpirationTime TEXT NULL
                )";

            await createTableCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Récupère un élément du cache.
        /// </summary>
        /// <typeparam name="T">Type de l'élément à récupérer</typeparam>
        /// <param name="key">Clé unique de l'élément</param>
        /// <returns>L'élément en cache ou la valeur par défaut si non trouvé ou expiré</returns>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value, ExpirationTime FROM CacheItems WHERE Key = @Key";
            command.Parameters.AddWithValue("@Key", key);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                // Vérification de l'expiration
                if (!reader.IsDBNull(1))
                {
                    var expirationTime = DateTime.Parse(reader.GetString(1));
                    if (DateTime.UtcNow > expirationTime)
                    {
                        // L'élément est expiré, on le supprime
                        await RemoveAsync(key);
                        return null;
                    }
                }

                // Désérialisation de la valeur
                var serializedValue = reader.GetString(0);
                return JsonConvert.DeserializeObject<T>(serializedValue);
            }

            return null;
        }

        /// <summary>
        /// Ajoute ou met à jour un élément dans le cache.
        /// </summary>
        /// <typeparam name="T">Type de l'élément à stocker</typeparam>
        /// <param name="key">Clé unique pour identifier l'élément</param>
        /// <param name="value">Valeur à stocker</param>
        /// <param name="expirationTime">Durée de validité du cache (null pour ne pas expirer)</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Sérialisation de l'objet
            var serializedValue = JsonConvert.SerializeObject(value);
            
            // Calcul de la date d'expiration si nécessaire
            DateTime? expiration = expirationTime.HasValue 
                ? DateTime.UtcNow.Add(expirationTime.Value) 
                : null;

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO CacheItems (Key, Value, ExpirationTime)
                VALUES (@Key, @Value, @ExpirationTime)";
            
            command.Parameters.AddWithValue("@Key", key);
            command.Parameters.AddWithValue("@Value", serializedValue);
            command.Parameters.AddWithValue("@ExpirationTime", expiration?.ToString("o") as object ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Supprime un élément du cache.
        /// </summary>
        /// <param name="key">Clé de l'élément à supprimer</param>
        /// <returns>True si l'élément a été supprimé avec succès, False sinon</returns>
        public async Task<bool> RemoveAsync(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CacheItems WHERE Key = @Key";
            command.Parameters.AddWithValue("@Key", key);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Vérifie si un élément existe dans le cache et n'est pas expiré.
        /// </summary>
        /// <param name="key">Clé à vérifier</param>
        /// <returns>True si l'élément existe et n'est pas expiré, False sinon</returns>
        public async Task<bool> ExistsAsync(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT ExpirationTime FROM CacheItems WHERE Key = @Key";
            command.Parameters.AddWithValue("@Key", key);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                // Vérification de l'expiration
                if (!reader.IsDBNull(0))
                {
                    var expirationTime = DateTime.Parse(reader.GetString(0));
                    if (DateTime.UtcNow > expirationTime)
                    {
                        // L'élément est expiré, on le supprime
                        await RemoveAsync(key);
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Vide le cache.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task ClearAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CacheItems";

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Nettoyage des éléments expirés du cache.
        /// </summary>
        /// <returns>Nombre d'éléments supprimés</returns>
        public async Task<int> CleanExpiredItemsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM CacheItems WHERE ExpirationTime IS NOT NULL AND ExpirationTime < @CurrentTime";
            command.Parameters.AddWithValue("@CurrentTime", DateTime.UtcNow.ToString("o"));

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Dispose des ressources utilisées par le service.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose des ressources utilisées par le service.
        /// </summary>
        /// <param name="disposing">Indique si les ressources managées doivent être libérées</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Pas de connexion persistante à fermer car nous utilisons using pour chaque opération
                }

                _disposed = true;
            }
        }
    }
}
