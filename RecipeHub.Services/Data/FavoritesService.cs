using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Service de gestion des recettes favorites utilisant SQLite pour le stockage.
    /// </summary>
    public class FavoritesService : IFavoritesService, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private bool _disposed = false;

        /// <summary>
        /// Constructeur du service de favoris.
        /// </summary>
        /// <param name="databaseName">Nom du fichier de base de données (sans extension)</param>
        public FavoritesService(string databaseName = "recipehub_favorites")
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
        /// Initialise la base de données et crée les tables nécessaires si elles n'existent pas.
        /// </summary>
        private async Task InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Création de la table des recettes favorites
            var createRecipesTableCommand = connection.CreateCommand();
            createRecipesTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Recipes (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    Area TEXT NOT NULL,
                    Instructions TEXT NOT NULL,
                    Thumbnail TEXT NOT NULL,
                    VideoUrl TEXT,
                    RecipeData TEXT NOT NULL,
                    DateAdded TEXT NOT NULL
                )";

            await createRecipesTableCommand.ExecuteNonQueryAsync();

            // Création de la table des collections
            var createCollectionsTableCommand = connection.CreateCommand();
            createCollectionsTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Collections (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT UNIQUE NOT NULL
                )";

            await createCollectionsTableCommand.ExecuteNonQueryAsync();

            // Création de la table de relation entre recettes et collections
            var createRecipeCollectionsTableCommand = connection.CreateCommand();
            createRecipeCollectionsTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS RecipeCollections (
                    RecipeId INTEGER NOT NULL,
                    CollectionId INTEGER NOT NULL,
                    PRIMARY KEY (RecipeId, CollectionId),
                    FOREIGN KEY (RecipeId) REFERENCES Recipes (Id) ON DELETE CASCADE,
                    FOREIGN KEY (CollectionId) REFERENCES Collections (Id) ON DELETE CASCADE
                )";

            await createRecipeCollectionsTableCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Récupère toutes les recettes favorites de l'utilisateur.
        /// </summary>
        /// <returns>Liste des recettes favorites</returns>
        public async Task<List<Recipe>> GetAllFavoritesAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT RecipeData FROM Recipes ORDER BY DateAdded DESC";

            var favorites = new List<Recipe>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var recipeJson = reader.GetString(0);
                var recipe = JsonConvert.DeserializeObject<Recipe>(recipeJson);
                
                if (recipe != null)
                {
                    recipe.IsFavorite = true;
                    favorites.Add(recipe);
                }
            }

            // Chargement des collections pour chaque recette
            foreach (var recipe in favorites)
            {
                recipe.Collections = await GetRecipeCollectionsAsync(recipe.Id);
            }

            return favorites;
        }

        /// <summary>
        /// Ajoute une recette aux favoris.
        /// </summary>
        /// <param name="recipe">Recette à ajouter aux favoris</param>
        /// <returns>True si l'ajout est réussi, False sinon</returns>
        public async Task<bool> AddFavoriteAsync(Recipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Marquer comme favori
                recipe.IsFavorite = true;

                // Préparation de la commande d'insertion
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT OR REPLACE INTO Recipes (
                        Id, Name, Category, Area, Instructions, Thumbnail, VideoUrl, RecipeData, DateAdded
                    )
                    VALUES (
                        @Id, @Name, @Category, @Area, @Instructions, @Thumbnail, @VideoUrl, @RecipeData, @DateAdded
                    )";

                command.Parameters.AddWithValue("@Id", recipe.Id);
                command.Parameters.AddWithValue("@Name", recipe.Name);
                command.Parameters.AddWithValue("@Category", recipe.Category);
                command.Parameters.AddWithValue("@Area", recipe.Area);
                command.Parameters.AddWithValue("@Instructions", recipe.Instructions);
                command.Parameters.AddWithValue("@Thumbnail", recipe.Thumbnail);
                command.Parameters.AddWithValue("@VideoUrl", recipe.VideoUrl as object ?? DBNull.Value);
                command.Parameters.AddWithValue("@RecipeData", JsonConvert.SerializeObject(recipe));
                command.Parameters.AddWithValue("@DateAdded", DateTime.UtcNow.ToString("o"));

                await command.ExecuteNonQueryAsync();

                // Sauvegarde des collections
                if (recipe.Collections != null && recipe.Collections.Any())
                {
                    foreach (var collectionName in recipe.Collections)
                    {
                        await AddToCollectionInternalAsync(connection, transaction, recipe.Id, collectionName);
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Supprime une recette des favoris.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette à supprimer</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        public async Task<bool> RemoveFavoriteAsync(int recipeId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Recipes WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", recipeId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Vérifie si une recette est dans les favoris.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette à vérifier</param>
        /// <returns>True si la recette est dans les favoris, False sinon</returns>
        public async Task<bool> IsFavoriteAsync(int recipeId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Recipes WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", recipeId);

            var count = (long)await command.ExecuteScalarAsync();
            return count > 0;
        }

        /// <summary>
        /// Récupère toutes les collections personnalisées de l'utilisateur.
        /// </summary>
        /// <returns>Liste des noms de collections</returns>
        public async Task<List<string>> GetCollectionsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Name FROM Collections ORDER BY Name";

            var collections = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                collections.Add(reader.GetString(0));
            }

            return collections;
        }

        /// <summary>
        /// Ajoute une recette à une collection spécifique.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette</param>
        /// <param name="collectionName">Nom de la collection</param>
        /// <returns>True si l'ajout est réussi, False sinon</returns>
        public async Task<bool> AddToCollectionAsync(int recipeId, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Le nom de la collection ne peut pas être vide.", nameof(collectionName));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                await AddToCollectionInternalAsync(connection, transaction, recipeId, collectionName);
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Méthode interne pour ajouter une recette à une collection.
        /// </summary>
        private async Task AddToCollectionInternalAsync(
            SqliteConnection connection, 
            SqliteTransaction transaction, 
            int recipeId, 
            string collectionName)
        {
            // Vérifier si la collection existe
            var collectionId = await GetOrCreateCollectionIdAsync(connection, transaction, collectionName);

            // Ajouter la relation
            var addToCollectionCommand = connection.CreateCommand();
            addToCollectionCommand.Transaction = transaction;
            addToCollectionCommand.CommandText = @"
                INSERT OR IGNORE INTO RecipeCollections (RecipeId, CollectionId)
                VALUES (@RecipeId, @CollectionId)";
                
            addToCollectionCommand.Parameters.AddWithValue("@RecipeId", recipeId);
            addToCollectionCommand.Parameters.AddWithValue("@CollectionId", collectionId);

            await addToCollectionCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Récupère l'ID d'une collection existante ou en crée une nouvelle.
        /// </summary>
        private async Task<long> GetOrCreateCollectionIdAsync(
            SqliteConnection connection, 
            SqliteTransaction transaction, 
            string collectionName)
        {
            // Vérifier si la collection existe
            var checkCollectionCommand = connection.CreateCommand();
            checkCollectionCommand.Transaction = transaction;
            checkCollectionCommand.CommandText = "SELECT Id FROM Collections WHERE Name = @Name";
            checkCollectionCommand.Parameters.AddWithValue("@Name", collectionName);

            var collectionId = await checkCollectionCommand.ExecuteScalarAsync();

            if (collectionId != null && collectionId != DBNull.Value)
            {
                return Convert.ToInt64(collectionId);
            }

            // Créer la collection si elle n'existe pas
            var createCollectionCommand = connection.CreateCommand();
            createCollectionCommand.Transaction = transaction;
            createCollectionCommand.CommandText = "INSERT INTO Collections (Name) VALUES (@Name); SELECT last_insert_rowid();";
            createCollectionCommand.Parameters.AddWithValue("@Name", collectionName);

            return (long)await createCollectionCommand.ExecuteScalarAsync();
        }

        /// <summary>
        /// Supprime une recette d'une collection spécifique.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette</param>
        /// <param name="collectionName">Nom de la collection</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        public async Task<bool> RemoveFromCollectionAsync(int recipeId, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Le nom de la collection ne peut pas être vide.", nameof(collectionName));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Récupérer l'ID de la collection
            var getCollectionIdCommand = connection.CreateCommand();
            getCollectionIdCommand.CommandText = "SELECT Id FROM Collections WHERE Name = @Name";
            getCollectionIdCommand.Parameters.AddWithValue("@Name", collectionName);

            var collectionId = await getCollectionIdCommand.ExecuteScalarAsync();

            if (collectionId == null || collectionId == DBNull.Value)
                return false;

            // Supprimer la relation
            var removeCommand = connection.CreateCommand();
            removeCommand.CommandText = @"
                DELETE FROM RecipeCollections 
                WHERE RecipeId = @RecipeId AND CollectionId = @CollectionId";
                
            removeCommand.Parameters.AddWithValue("@RecipeId", recipeId);
            removeCommand.Parameters.AddWithValue("@CollectionId", Convert.ToInt64(collectionId));

            var rowsAffected = await removeCommand.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Récupère les recettes d'une collection spécifique.
        /// </summary>
        /// <param name="collectionName">Nom de la collection</param>
        /// <returns>Liste des recettes de la collection</returns>
        public async Task<List<Recipe>> GetRecipesByCollectionAsync(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Le nom de la collection ne peut pas être vide.", nameof(collectionName));

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Récupérer l'ID de la collection
            var getCollectionIdCommand = connection.CreateCommand();
            getCollectionIdCommand.CommandText = "SELECT Id FROM Collections WHERE Name = @Name";
            getCollectionIdCommand.Parameters.AddWithValue("@Name", collectionName);

            var collectionId = await getCollectionIdCommand.ExecuteScalarAsync();

            if (collectionId == null || collectionId == DBNull.Value)
                return new List<Recipe>();

            // Récupérer les recettes de la collection
            var getRecipesCommand = connection.CreateCommand();
            getRecipesCommand.CommandText = @"
                SELECT r.RecipeData 
                FROM Recipes r
                JOIN RecipeCollections rc ON r.Id = rc.RecipeId
                WHERE rc.CollectionId = @CollectionId
                ORDER BY r.Name";
                
            getRecipesCommand.Parameters.AddWithValue("@CollectionId", Convert.ToInt64(collectionId));

            var recipes = new List<Recipe>();
            using var reader = await getRecipesCommand.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var recipeJson = reader.GetString(0);
                var recipe = JsonConvert.DeserializeObject<Recipe>(recipeJson);
                
                if (recipe != null)
                {
                    recipe.IsFavorite = true;
                    recipe.Collections = await GetRecipeCollectionsAsync(recipe.Id);
                    recipes.Add(recipe);
                }
            }

            return recipes;
        }

        /// <summary>
        /// Récupère les collections d'une recette.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette</param>
        /// <returns>Liste des noms de collections auxquelles appartient la recette</returns>
        private async Task<List<string>> GetRecipeCollectionsAsync(int recipeId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT c.Name 
                FROM Collections c
                JOIN RecipeCollections rc ON c.Id = rc.CollectionId
                WHERE rc.RecipeId = @RecipeId
                ORDER BY c.Name";
                
            command.Parameters.AddWithValue("@RecipeId", recipeId);

            var collections = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                collections.Add(reader.GetString(0));
            }

            return collections;
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
