using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service d'accès à l'API TheMealDB.
    /// </summary>
    public interface IMealDbService
    {
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
        /// <returns>Liste des recettes correspondantes</returns>
        Task<List<Recipe>> SearchRecipesByNameAsync(string name);

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
        /// <returns>Liste des recettes de la catégorie</returns>
        Task<List<Recipe>> GetRecipesByCategoryAsync(string category);

        /// <summary>
        /// Récupère les régions (aires) culinaires disponibles.
        /// </summary>
        /// <returns>Liste des régions</returns>
        Task<List<string>> GetAreasAsync();

        /// <summary>
        /// Récupère les recettes d'une région spécifique.
        /// </summary>
        /// <param name="area">Nom de la région</param>
        /// <returns>Liste des recettes de la région</returns>
        Task<List<Recipe>> GetRecipesByAreaAsync(string area);

        /// <summary>
        /// Récupère la liste des ingrédients disponibles.
        /// </summary>
        /// <returns>Liste des ingrédients</returns>
        Task<List<string>> GetIngredientsAsync();

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <returns>Liste des recettes contenant l'ingrédient</returns>
        Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient);
    }
}
