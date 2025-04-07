using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de gestion des recettes favorites.
    /// </summary>
    public interface IFavoritesService
    {
        /// <summary>
        /// Récupère toutes les recettes favorites de l'utilisateur.
        /// </summary>
        /// <returns>Liste des recettes favorites</returns>
        Task<List<Recipe>> GetAllFavoritesAsync();

        /// <summary>
        /// Ajoute une recette aux favoris.
        /// </summary>
        /// <param name="recipe">Recette à ajouter aux favoris</param>
        /// <returns>True si l'ajout est réussi, False sinon</returns>
        Task<bool> AddFavoriteAsync(Recipe recipe);

        /// <summary>
        /// Supprime une recette des favoris.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette à supprimer</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        Task<bool> RemoveFavoriteAsync(int recipeId);

        /// <summary>
        /// Vérifie si une recette est dans les favoris.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette à vérifier</param>
        /// <returns>True si la recette est dans les favoris, False sinon</returns>
        Task<bool> IsFavoriteAsync(int recipeId);

        /// <summary>
        /// Récupère toutes les collections personnalisées de l'utilisateur.
        /// </summary>
        /// <returns>Liste des noms de collections</returns>
        Task<List<string>> GetCollectionsAsync();

        /// <summary>
        /// Ajoute une recette à une collection spécifique.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette</param>
        /// <param name="collectionName">Nom de la collection</param>
        /// <returns>True si l'ajout est réussi, False sinon</returns>
        Task<bool> AddToCollectionAsync(int recipeId, string collectionName);

        /// <summary>
        /// Supprime une recette d'une collection spécifique.
        /// </summary>
        /// <param name="recipeId">Identifiant de la recette</param>
        /// <param name="collectionName">Nom de la collection</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        Task<bool> RemoveFromCollectionAsync(int recipeId, string collectionName);

        /// <summary>
        /// Récupère les recettes d'une collection spécifique.
        /// </summary>
        /// <param name="collectionName">Nom de la collection</param>
        /// <returns>Liste des recettes de la collection</returns>
        Task<List<Recipe>> GetRecipesByCollectionAsync(string collectionName);

        /// <summary>
        /// Crée une nouvelle collection.
        /// </summary>
        /// <param name="collectionName">Nom de la collection à créer</param>
        /// <returns>True si la création est réussie, False sinon</returns>
        Task<bool> CreateCollectionAsync(string collectionName);

        /// <summary>
        /// Renomme une collection existante.
        /// </summary>
        /// <param name="oldName">Nom actuel de la collection</param>
        /// <param name="newName">Nouveau nom de la collection</param>
        /// <returns>True si le renommage est réussi, False sinon</returns>
        Task<bool> RenameCollectionAsync(string oldName, string newName);

        /// <summary>
        /// Supprime une collection et ses associations avec les recettes.
        /// </summary>
        /// <param name="collectionName">Nom de la collection à supprimer</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        Task<bool> DeleteCollectionAsync(string collectionName);
    }
}
