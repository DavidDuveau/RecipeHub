using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Service central pour gérer la base de données SQLite de l'application.
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private readonly SqliteConnection _connection;
        private bool _disposed = false;

        /// <summary>
        /// Constructeur du service de base de données.
        /// </summary>
        /// <param name="databaseName">Nom du fichier de base de données (sans extension)</param>
        public DatabaseService(string databaseName = "recipehub_database")
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
            
            // Créer et ouvrir une connexion persistante
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
            
            // Activer les contraintes de clés étrangères
            using var pragmaCommand = _connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
            pragmaCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Obtient la chaîne de connexion à la base de données.
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// Obtient le chemin du fichier de base de données.
        /// </summary>
        public string DatabasePath => _databasePath;
        
        /// <summary>
        /// Obtient une connexion à la base de données.
        /// </summary>
        /// <returns>Connexion SQLite</returns>
        public SqliteConnection GetConnection() 
        {
            return _connection;
        }
        
        /// <summary>
        /// Crée une commande SQLite avec la connexion courante.
        /// </summary>
        /// <returns>Une commande SQLite</returns>
        public SqliteCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }
        
        /// <summary>
        /// Commence une transaction SQLite.
        /// </summary>
        /// <returns>Transaction SQLite</returns>
        public SqliteTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }
        
        /// <summary>
        /// Exécute une commande SQL qui ne retourne pas de résultats.
        /// </summary>
        /// <param name="commandText">Texte de la commande SQL</param>
        /// <param name="parameters">Paramètres de la commande (optionnel)</param>
        /// <returns>Nombre de lignes affectées</returns>
        public async Task<int> ExecuteNonQueryAsync(string commandText, params (string name, object value)[] parameters)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = commandText;
            
            // Ajouter les paramètres
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }
            
            return await command.ExecuteNonQueryAsync();
        }
        
        /// <summary>
        /// Exécute une commande SQL qui retourne une valeur scalaire.
        /// </summary>
        /// <typeparam name="T">Type de la valeur de retour</typeparam>
        /// <param name="commandText">Texte de la commande SQL</param>
        /// <param name="parameters">Paramètres de la commande (optionnel)</param>
        /// <returns>Valeur scalaire retournée par la commande</returns>
        public async Task<T> ExecuteScalarAsync<T>(string commandText, params (string name, object value)[] parameters)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = commandText;
            
            // Ajouter les paramètres
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }
            
            var result = await command.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                return default!;
                
            return (T)Convert.ChangeType(result, typeof(T));
        }
        
        /// <summary>
        /// Vérifie si une table existe dans la base de données.
        /// </summary>
        /// <param name="tableName">Nom de la table à vérifier</param>
        /// <returns>True si la table existe, False sinon</returns>
        public async Task<bool> TableExistsAsync(string tableName)
        {
            var count = await ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name;",
                ("@name", tableName));
                
            return count > 0;
        }
        
        /// <summary>
        /// Obtient la liste des tables dans la base de données.
        /// </summary>
        /// <returns>Tableau des noms de tables</returns>
        public async Task<string[]> GetTablesAsync()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
            
            var tables = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            
            return tables.ToArray();
        }
        
        /// <summary>
        /// Obtient les informations sur les colonnes d'une table.
        /// </summary>
        /// <param name="tableName">Nom de la table</param>
        /// <returns>Liste des informations de colonnes</returns>
        public async Task<List<(string Name, string Type)>> GetTableColumnsAsync(string tableName)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";
            
            var columns = new List<(string Name, string Type)>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(1);
                var type = reader.GetString(2);
                columns.Add((name, type));
            }
            
            return columns;
        }
        
        /// <summary>
        /// Exécute une sauvegarde de la base de données vers un fichier.
        /// </summary>
        /// <param name="backupPath">Chemin où sauvegarder la base de données</param>
        /// <returns>True si la sauvegarde a réussi, False sinon</returns>
        public bool BackupDatabase(string backupPath)
        {
            try
            {
                File.Copy(_databasePath, backupPath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Restaure une base de données à partir d'un fichier de sauvegarde.
        /// </summary>
        /// <param name="backupPath">Chemin du fichier de sauvegarde</param>
        /// <returns>True si la restauration a réussi, False sinon</returns>
        public bool RestoreDatabase(string backupPath)
        {
            try
            {
                // Fermer la connexion actuelle
                _connection.Close();
                
                // Copier le fichier de sauvegarde
                File.Copy(backupPath, _databasePath, true);
                
                // Rouvrir la connexion
                _connection.Open();
                
                // Réactiver les contraintes de clés étrangères
                using var pragmaCommand = _connection.CreateCommand();
                pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCommand.ExecuteNonQuery();
                
                return true;
            }
            catch
            {
                // Essayer de rouvrir la connexion en cas d'erreur
                try
                {
                    _connection.Open();
                }
                catch
                {
                    // Ignorer les erreurs ici
                }
                
                return false;
            }
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
                    _connection?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
