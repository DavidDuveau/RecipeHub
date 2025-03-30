using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Service pour la gestion des listes de courses avec persistance dans SQLite.
    /// </summary>
    public partial class ShoppingListDatabaseService : IShoppingListService, IDisposable
    {
        private readonly DatabaseService _databaseService;
        private readonly IMealPlanningService _mealPlanningService;
        private readonly IMealDbService _mealDbService;
        private bool _disposed = false;

        /// <summary>
        /// Constructeur du service de listes de courses.
        /// </summary>
        public ShoppingListDatabaseService(
            DatabaseService databaseService,
            IMealPlanningService mealPlanningService,
            IMealDbService mealDbService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mealPlanningService = mealPlanningService ?? throw new ArgumentNullException(nameof(mealPlanningService));
            _mealDbService = mealDbService ?? throw new ArgumentNullException(nameof(mealDbService));
            
            // Initialisation de la base de données
            InitializeDatabaseAsync().Wait();
        }

        /// <summary>
        /// Initialise les tables nécessaires pour les listes de courses.
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            // Création de la table des listes de courses
            await _databaseService.ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS ShoppingLists (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    MealPlanId INTEGER,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                )");

            // Création de la table des articles de la liste de courses
            await _databaseService.ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS ShoppingItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ShoppingListId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Quantity REAL NOT NULL,
                    Unit TEXT NOT NULL,
                    Category INTEGER NOT NULL,
                    IsPurchased INTEGER NOT NULL DEFAULT 0,
                    Notes TEXT,
                    IsManuallyAdded INTEGER NOT NULL DEFAULT 0,
                    AssociatedRecipesData TEXT,
                    FOREIGN KEY (ShoppingListId) REFERENCES ShoppingLists (Id) ON DELETE CASCADE
                )");
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
                    // Pas de ressources à libérer
                }

                _disposed = true;
            }
        }
    }
}
