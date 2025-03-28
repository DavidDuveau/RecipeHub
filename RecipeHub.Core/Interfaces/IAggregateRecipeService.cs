using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service d'agrégation de recettes qui centralise l'accès aux différentes sources.
    /// </summary>
    public interface IAggregateRecipeService
    {
        /// <summary>
        /// Liste des fournisseurs de recettes disponibles.
        /// </summary>
        IEnumerable<IRecipeProvider> Providers { get; }

        /// <summary>
        /// Récupère une recette par son identifiant.
        /// </summary>
        /// <param name="id">Identifiant de la recette</param>
        /// <param name="preferredProvider">Nom du fournisseur préféré (optionnel)</param>
        /// <returns>La recette correspondante ou null si non trouvée</returns>
        Task<Recipe?> GetRecipeByIdAsync(int id, string? preferredProvider = null);

        /// <summary>
        /// Recherche des recettes par leur nom à travers toutes les sources disponibles.
        /// </summary>
        /// <param name="name">Nom ou partie du nom à rechercher</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes correspondantes</returns>
        Task<List<Recipe>> SearchRecipesByNameAsync(string name, int limit = 10);

        /// <summary>
        /// Récupère une liste de recettes aléatoires.
        /// </summary>
        /// <param name="count">Nombre de recettes à récupérer</param>
        /// <returns>Liste des recettes aléatoires</returns>
        Task<List<Recipe>> GetRandomRecipesAsync(int count);

        /// <summary>
        /// Récupère toutes les catégories disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des catégories</returns>
        Task<List<Category>> GetCategoriesAsync();

        /// <summary>
        /// Récupère les recettes d'une catégorie spécifique.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes de la catégorie</returns>
        Task<List<Recipe>> GetRecipesByCategoryAsync(string category, int limit = 20);

        /// <summary>
        /// Récupère les régions (cuisines) disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des régions</returns>
        Task<List<string>> GetCuisinesAsync();

        /// <summary>
        /// Récupère les recettes d'une région spécifique.
        /// </summary>
        /// <param name="cuisine">Nom de la région/cuisine</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes de la région</returns>
        Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisine, int limit = 20);

        /// <summary>
        /// Récupère la liste des ingrédients disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des ingrédients</returns>
        Task<List<string>> GetIngredientsAsync();

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes contenant l'ingrédient</returns>
        Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient, int limit = 20);

        /// <summary>
        /// Obtient les statistiques d'utilisation de tous les fournisseurs.
        /// </summary>
        /// <returns>Dictionnaire associant le nom du fournisseur à son utilisation (utilisé/total)</returns>
        Task<Dictionary<string, (int Used, int Total)>> GetApiUsageStatisticsAsync();

        /// <summary>
        /// Définit l'ordre de priorité des fournisseurs.
        /// </summary>
        /// <param name="providerOrder">Liste ordonnée des noms de fournisseurs</param>
        void SetProviderPriority(List<string> providerOrder);

        /// <summary>
        /// Obtient l'ordre de priorité actuel des fournisseurs.
        /// </summary>
        /// <returns>Liste ordonnée des noms de fournisseurs</returns>
        List<string> GetProviderPriority();
    }
}
