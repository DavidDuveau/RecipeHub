using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Service pour la gestion des plans de repas avec persistance dans SQLite.
    /// </summary>
    public partial class MealPlanningDatabaseService : IMealPlanningService, IDisposable
    {
        private readonly DatabaseService _databaseService;
        private readonly IFavoritesService _favoritesService;
        private readonly IMealDbService _mealDbService;
        private bool _disposed = false;

        /// <summary>
        /// Constructeur du service de planification des repas.
        /// </summary>
        public MealPlanningDatabaseService(
            DatabaseService databaseService,
            IFavoritesService favoritesService,
            IMealDbService mealDbService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
            _mealDbService = mealDbService ?? throw new ArgumentNullException(nameof(mealDbService));
            
            // Initialisation de la base de données
            InitializeDatabaseAsync().Wait();
        }

        /// <summary>
        /// Initialise les tables nécessaires pour les plans de repas.
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            // Création de la table des plans de repas
            await _databaseService.ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS MealPlans (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                )");

            // Création de la table des repas planifiés
            await _databaseService.ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS PlannedMeals (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MealPlanId INTEGER NOT NULL,
                    RecipeId INTEGER NOT NULL,
                    RecipeData TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Type INTEGER NOT NULL,
                    Servings INTEGER NOT NULL DEFAULT 1,
                    Notes TEXT,
                    IncludeInShoppingList INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY (MealPlanId) REFERENCES MealPlans (Id) ON DELETE CASCADE
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
