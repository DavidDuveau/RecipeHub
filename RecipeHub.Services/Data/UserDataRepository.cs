using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Implémentation du repository pour les données utilisateur.
    /// </summary>
    public class UserDataRepository : IUserDataRepository, IDisposable
    {
        private readonly DatabaseService _databaseService;
        private readonly IFavoritesService _favoritesService;
        private readonly IMealPlanningService _mealPlanningService;
        private readonly IShoppingListService _shoppingListService;
        private bool _disposed = false;

        /// <summary>
        /// Constructeur du repository de données utilisateur.
        /// </summary>
        public UserDataRepository(
            DatabaseService databaseService,
            IFavoritesService favoritesService,
            IMealPlanningService mealPlanningService,
            IShoppingListService shoppingListService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
            _mealPlanningService = mealPlanningService ?? throw new ArgumentNullException(nameof(mealPlanningService));
            _shoppingListService = shoppingListService ?? throw new ArgumentNullException(nameof(shoppingListService));
        }

        #region Favoris et Collections

        /// <inheritdoc />
        public async Task<List<Recipe>> GetAllFavoritesAsync()
        {
            return await _favoritesService.GetAllFavoritesAsync();
        }

        /// <inheritdoc />
        public async Task<bool> IsFavoriteAsync(int recipeId)
        {
            return await _favoritesService.IsFavoriteAsync(recipeId);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetAllCollectionsAsync()
        {
            return await _favoritesService.GetCollectionsAsync();
        }

        /// <inheritdoc />
        public async Task<List<Recipe>> GetRecipesByCollectionAsync(string collectionName)
        {
            return await _favoritesService.GetRecipesByCollectionAsync(collectionName);
        }

        #endregion

        #region Plans de Repas

        /// <inheritdoc />
        public async Task<List<MealPlan>> GetAllMealPlansAsync()
        {
            return await _mealPlanningService.GetAllMealPlansAsync();
        }

        /// <inheritdoc />
        public async Task<MealPlan?> GetActiveMealPlanAsync()
        {
            return await _mealPlanningService.GetActiveMealPlanAsync();
        }

        /// <inheritdoc />
        public async Task<MealPlan?> GetMealPlanByIdAsync(int planId)
        {
            return await _mealPlanningService.GetMealPlanByIdAsync(planId);
        }

        #endregion

        #region Listes de Courses

        /// <inheritdoc />
        public async Task<List<ShoppingList>> GetAllShoppingListsAsync()
        {
            return await _shoppingListService.GetAllShoppingListsAsync();
        }

        /// <inheritdoc />
        public async Task<ShoppingList?> GetShoppingListByIdAsync(int listId)
        {
            return await _shoppingListService.GetShoppingListByIdAsync(listId);
        }

        /// <inheritdoc />
        public async Task<ShoppingList?> GetShoppingListByMealPlanIdAsync(int mealPlanId)
        {
            return await _shoppingListService.GetShoppingListByMealPlanIdAsync(mealPlanId);
        }

        #endregion

        #region Sauvegarde et Restauration

        /// <inheritdoc />
        public async Task<bool> ExportUserDataAsync(string filePath)
        {
            try
            {
                // Créer un objet contenant toutes les données utilisateur
                var userData = new
                {
                    Favorites = await GetAllFavoritesAsync(),
                    Collections = await GetAllCollectionsAsync(),
                    MealPlans = await GetAllMealPlansAsync(),
                    ShoppingLists = await GetAllShoppingListsAsync(),
                    ExportDate = DateTime.Now
                };

                // Sérialiser l'objet en JSON
                var json = JsonConvert.SerializeObject(userData, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                // Écrire le JSON dans le fichier
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ImportUserDataAsync(string filePath)
        {
            try
            {
                // Lire le fichier JSON
                var json = await File.ReadAllTextAsync(filePath);

                // Désérialiser le JSON
                var userData = JsonConvert.DeserializeObject<dynamic>(json);

                if (userData == null)
                    return false;

                using var connection = _databaseService.GetConnection();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Importer les favoris
                    if (userData.Favorites != null)
                    {
                        foreach (var favorite in userData.Favorites)
                        {
                            var recipe = favorite.ToObject<Recipe>();
                            if (recipe != null && !await _favoritesService.IsFavoriteAsync(recipe.Id))
                            {
                                await _favoritesService.AddFavoriteAsync(recipe);
                            }
                        }
                    }

                    // Note: L'importation des plans de repas et des listes de courses nécessiterait
                    // une logique plus complexe pour éviter les doublons et maintenir l'intégrité des données

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool BackupDatabase(string backupPath)
        {
            return _databaseService.BackupDatabase(backupPath);
        }

        /// <inheritdoc />
        public bool RestoreDatabase(string backupPath)
        {
            return _databaseService.RestoreDatabase(backupPath);
        }

        #endregion

        #region Statistiques

        /// <inheritdoc />
        public async Task<Dictionary<string, int>> GetUserDataStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();

            try
            {
                // Récupérer les statistiques de favoris
                var favorites = await GetAllFavoritesAsync();
                stats["FavoritesCount"] = favorites.Count;

                // Récupérer les collections
                var collections = await GetAllCollectionsAsync();
                stats["CollectionsCount"] = collections.Count;

                // Récupérer les plans de repas
                var mealPlans = await GetAllMealPlansAsync();
                stats["MealPlansCount"] = mealPlans.Count;

                int plannedMealsCount = 0;
                foreach (var plan in mealPlans)
                {
                    plannedMealsCount += plan.Meals.Count;
                }
                stats["PlannedMealsCount"] = plannedMealsCount;

                // Récupérer les listes de courses
                var shoppingLists = await GetAllShoppingListsAsync();
                stats["ShoppingListsCount"] = shoppingLists.Count;

                int shoppingItemsCount = 0;
                foreach (var list in shoppingLists)
                {
                    shoppingItemsCount += list.Items.Count;
                }
                stats["ShoppingItemsCount"] = shoppingItemsCount;

                return stats;
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner des statistiques vides
                return new Dictionary<string, int>();
            }
        }

        #endregion

        /// <summary>
        /// Dispose des ressources utilisées par le repository.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose des ressources utilisées par le repository.
        /// </summary>
        /// <param name="disposing">Indique si les ressources managées doivent être libérées</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Disposer des services qui implémentent IDisposable
                }

                _disposed = true;
            }
        }
    }
}
