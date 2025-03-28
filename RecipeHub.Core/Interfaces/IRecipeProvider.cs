using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface commune pour tous les fournisseurs de recettes.
    /// </summary>
    public interface IRecipeProvider
    {
        /// <summary>
        /// Nom du fournisseur de recettes.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Obtient le quota quotidien maximum d'appels API.
        /// </summary>
        int DailyQuota { get; }

        /// <summary>
        /// Obtient le nombre d'appels API restants pour aujourd'hui.
        /// </summary>
        /// <returns>Le nombre d'appels restants</returns>
        Task<int> GetRemainingCallsAsync();

        /// <summary>
        /// Récupère une recette par son identifiant.
        /// </summary>
        /// <param name="id">Identifiant de la recette</param>
        /// <returns>La recette correspondante ou null si non trouvée</returns>
        Task<Recipe?> GetRecipeByIdAsync(int id);

        /// <summary>
        /// Recherche des recettes par leur nom.
        /// </summary>
        /// <param name="name">Nom ou partie du nom à rechercher</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes correspondantes</returns>
        Task<List<Recipe>> SearchRecipesByNameAsync(string name, int limit = 10);

        /// <summary>
        /// Récupère une liste de recettes aléatoires.
        /// </summary>
        /// <param name="count">Nombre de recettes à récupérer</param>
        /// <returns>Liste des recettes aléatoires</returns>
        Task<List<Recipe>> GetRandomRecipesAsync(int count);

        /// <summary>
        /// Récupère toutes les catégories disponibles.
        /// </summary>
        /// <returns>Liste des catégories</returns>
        Task<List<Category>> GetCategoriesAsync();

        /// <summary>
        /// Récupère les recettes d'une catégorie spécifique.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes de la catégorie</returns>
        Task<List<Recipe>> GetRecipesByCategoryAsync(string category, int limit = 20);

        /// <summary>
        /// Récupère les régions (cuisines) disponibles.
        /// </summary>
        /// <returns>Liste des régions</returns>
        Task<List<string>> GetCuisinesAsync();

        /// <summary>
        /// Récupère les recettes d'une région spécifique.
        /// </summary>
        /// <param name="cuisine">Nom de la région/cuisine</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes de la région</returns>
        Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisine, int limit = 20);

        /// <summary>
        /// Récupère la liste des ingrédients disponibles.
        /// </summary>
        /// <returns>Liste des ingrédients</returns>
        Task<List<string>> GetIngredientsAsync();

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes contenant l'ingrédient</returns>
        Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient, int limit = 20);

        /// <summary>
        /// Incrémente le compteur d'utilisation de l'API.
        /// </summary>
        /// <param name="count">Nombre d'appels à comptabiliser (1 par défaut)</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task IncrementApiUsageAsync(int count = 1);

        /// <summary>
        /// Réinitialise le compteur d'utilisation quotidienne.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task ResetDailyCounterAsync();
    }
}
