using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le repository de données utilisateur.
    /// </summary>
    public interface IUserDataRepository
    {
        #region Favoris et Collections

        /// <summary>
        /// Récupère toutes les recettes favorites de l'utilisateur.
        /// </summary>
        Task<List<Recipe>> GetAllFavoritesAsync();

        /// <summary>
        /// Vérifie si une recette est dans les favoris.
        /// </summary>
        Task<bool> IsFavoriteAsync(int recipeId);

        /// <summary>
        /// Récupère toutes les collections de l'utilisateur.
        /// </summary>
        Task<List<string>> GetAllCollectionsAsync();

        /// <summary>
        /// Récupère les recettes d'une collection spécifique.
        /// </summary>
        Task<List<Recipe>> GetRecipesByCollectionAsync(string collectionName);

        #endregion

        #region Plans de Repas

        /// <summary>
        /// Récupère tous les plans de repas.
        /// </summary>
        Task<List<MealPlan>> GetAllMealPlansAsync();

        /// <summary>
        /// Récupère le plan de repas actif.
        /// </summary>
        Task<MealPlan?> GetActiveMealPlanAsync();

        /// <summary>
        /// Récupère un plan de repas par son identifiant.
        /// </summary>
        Task<MealPlan?> GetMealPlanByIdAsync(int planId);

        #endregion

        #region Listes de Courses

        /// <summary>
        /// Récupère toutes les listes de courses.
        /// </summary>
        Task<List<ShoppingList>> GetAllShoppingListsAsync();

        /// <summary>
        /// Récupère une liste de courses par son identifiant.
        /// </summary>
        Task<ShoppingList?> GetShoppingListByIdAsync(int listId);

        /// <summary>
        /// Récupère la liste de courses associée à un plan de repas.
        /// </summary>
        Task<ShoppingList?> GetShoppingListByMealPlanIdAsync(int mealPlanId);

        #endregion

        #region Sauvegarde et Restauration

        /// <summary>
        /// Exporte toutes les données utilisateur dans un fichier JSON.
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'export</param>
        Task<bool> ExportUserDataAsync(string filePath);

        /// <summary>
        /// Importe les données utilisateur depuis un fichier JSON.
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'import</param>
        Task<bool> ImportUserDataAsync(string filePath);

        /// <summary>
        /// Crée une sauvegarde de la base de données.
        /// </summary>
        /// <param name="backupPath">Chemin du fichier de sauvegarde</param>
        bool BackupDatabase(string backupPath);

        /// <summary>
        /// Restaure la base de données à partir d'une sauvegarde.
        /// </summary>
        /// <param name="backupPath">Chemin du fichier de sauvegarde</param>
        bool RestoreDatabase(string backupPath);

        #endregion

        #region Statistiques

        /// <summary>
        /// Obtient des statistiques sur les données utilisateur.
        /// </summary>
        Task<Dictionary<string, int>> GetUserDataStatisticsAsync();

        #endregion
    }
}
