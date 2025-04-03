using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.IO;
using System.Linq;
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
                    ExportDate = DateTime.Now,
                    Version = "1.0"
                };

                // Sérialiser l'objet en JSON
                var json = JsonConvert.SerializeObject(userData, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                });

                // Écrire le JSON dans le fichier
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'export des données: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ImportUserDataAsync(string filePath, string mergeStrategy = "merge")
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
                    // Importer les favoris selon la stratégie choisie
                    if (userData.Favorites != null)
                    {
                        if (mergeStrategy == "replace")
                        {
                            // Supprimer d'abord tous les favoris existants
                            var existingFavorites = await _favoritesService.GetAllFavoritesAsync();
                            foreach (var recipe in existingFavorites)
                            {
                                await _favoritesService.RemoveFavoriteAsync(recipe.Id);
                            }
                        }

                        foreach (var favorite in userData.Favorites)
                        {
                            var recipe = favorite.ToObject<Recipe>();
                            if (recipe != null)
                            {
                                bool exists = await _favoritesService.IsFavoriteAsync(recipe.Id);
                                
                                if (!exists || mergeStrategy != "keepExisting")
                                {
                                    await _favoritesService.AddFavoriteAsync(recipe);
                                }
                            }
                        }
                    }

                    // Importer les collections
                    if (userData.Collections != null)
                    {
                        // Les collections sont gérées automatiquement lors de l'ajout des recettes
                    }

                    // Importer les plans de repas
                    if (userData.MealPlans != null)
                    {
                        // Ne pas supprimer les plans existants si on utilise la stratégie de fusion
                        if (mergeStrategy == "replace")
                        {
                            var existingPlans = await _mealPlanningService.GetAllMealPlansAsync();
                            foreach (var plan in existingPlans)
                            {
                                await _mealPlanningService.DeleteMealPlanAsync(plan.Id);
                            }
                        }

                        foreach (var mealPlan in userData.MealPlans)
                        {
                            var plan = mealPlan.ToObject<MealPlan>();
                            if (plan != null)
                            {
                                // Vérifier si un plan avec le même nom existe déjà
                                var existingPlans = await _mealPlanningService.GetAllMealPlansAsync();
                                var existingPlan = existingPlans.FirstOrDefault(p => p.Name == plan.Name);
                                
                                if (existingPlan != null)
                                {
                                    if (mergeStrategy == "merge" || mergeStrategy == "replace")
                                    {
                                        // Mettre à jour le plan existant
                                        plan.Id = existingPlan.Id; // Conserver l'ID existant
                                        await _mealPlanningService.UpdateMealPlanAsync(plan);
                                    }
                                    // Pour "keepExisting", ne rien faire
                                }
                                else
                                {
                                    // Créer un nouveau plan
                                    await _mealPlanningService.CreateMealPlanAsync(plan);
                                }
                            }
                        }
                    }

                    // Importer les listes de courses
                    if (userData.ShoppingLists != null)
                    {
                        // Ne pas supprimer les listes existantes si on utilise la stratégie de fusion
                        if (mergeStrategy == "replace")
                        {
                            var existingLists = await _shoppingListService.GetAllShoppingListsAsync();
                            foreach (var list in existingLists)
                            {
                                await _shoppingListService.DeleteShoppingListAsync(list.Id);
                            }
                        }

                        foreach (var shoppingList in userData.ShoppingLists)
                        {
                            var list = shoppingList.ToObject<ShoppingList>();
                            if (list != null)
                            {
                                // Vérifier si une liste avec le même nom existe déjà
                                var existingLists = await _shoppingListService.GetAllShoppingListsAsync();
                                var existingList = existingLists.FirstOrDefault(l => l.Name == list.Name);
                                
                                if (existingList != null)
                                {
                                    if (mergeStrategy == "merge" || mergeStrategy == "replace")
                                    {
                                        // Mettre à jour la liste existante
                                        list.Id = existingList.Id; // Conserver l'ID existant
                                        await _shoppingListService.UpdateShoppingListAsync(list);
                                    }
                                    // Pour "keepExisting", ne rien faire
                                }
                                else
                                {
                                    // Créer une nouvelle liste
                                    await _shoppingListService.CreateShoppingListAsync(list);
                                }
                            }
                        }
                    }

                    transaction.Commit();
                    
                    // Journaliser l'importation
                    Console.WriteLine($"Importation réussie: {filePath} (stratégie: {mergeStrategy})");
                    
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Erreur lors de l'importation: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur critique lors de l'importation: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool BackupDatabase(string backupPath)
        {
            try
            {
                var result = _databaseService.BackupDatabase(backupPath);
                
                if (result)
                {
                    Console.WriteLine($"Sauvegarde réussie: {backupPath}");
                }
                else
                {
                    Console.WriteLine($"Échec de la sauvegarde: {backupPath}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool RestoreDatabase(string backupPath)
        {
            try
            {
                var result = _databaseService.RestoreDatabase(backupPath);
                
                if (result)
                {
                    Console.WriteLine($"Restauration réussie: {backupPath}");
                }
                else
                {
                    Console.WriteLine($"Échec de la restauration: {backupPath}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la restauration: {ex.Message}");
                return false;
            }
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

                // Ajouter la taille de la base de données
                try
                {
                    var dbFileInfo = new FileInfo(_databaseService.DatabasePath);
                    if (dbFileInfo.Exists)
                    {
                        stats["DatabaseSizeKB"] = (int)(dbFileInfo.Length / 1024);
                    }
                }
                catch 
                {
                    // Ignorer les erreurs de statistiques supplémentaires
                }

                return stats;
            }
            catch (Exception ex)
            {
                // En cas d'erreur, journaliser et retourner des statistiques partielles
                Console.WriteLine($"Erreur lors de la récupération des statistiques: {ex.Message}");
                return stats;
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
