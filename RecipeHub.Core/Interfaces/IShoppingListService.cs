using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de gestion des listes de courses.
    /// </summary>
    public interface IShoppingListService
    {
        /// <summary>
        /// Récupère toutes les listes de courses.
        /// </summary>
        /// <returns>Liste des listes de courses</returns>
        Task<List<ShoppingList>> GetAllShoppingListsAsync();

        /// <summary>
        /// Récupère une liste de courses par son identifiant.
        /// </summary>
        /// <param name="listId">Identifiant de la liste</param>
        /// <returns>La liste de courses ou null si non trouvée</returns>
        Task<ShoppingList?> GetShoppingListByIdAsync(int listId);

        /// <summary>
        /// Récupère la liste de courses associée à un plan de repas.
        /// </summary>
        /// <param name="mealPlanId">Identifiant du plan de repas</param>
        /// <returns>La liste de courses ou null si non trouvée</returns>
        Task<ShoppingList?> GetShoppingListByMealPlanIdAsync(int mealPlanId);

        /// <summary>
        /// Crée une nouvelle liste de courses.
        /// </summary>
        /// <param name="shoppingList">Liste de courses à créer</param>
        /// <returns>L'identifiant de la nouvelle liste</returns>
        Task<int> CreateShoppingListAsync(ShoppingList shoppingList);

        /// <summary>
        /// Met à jour une liste de courses existante.
        /// </summary>
        /// <param name="shoppingList">Liste de courses à mettre à jour</param>
        /// <returns>True si la mise à jour est réussie, False sinon</returns>
        Task<bool> UpdateShoppingListAsync(ShoppingList shoppingList);

        /// <summary>
        /// Supprime une liste de courses.
        /// </summary>
        /// <param name="listId">Identifiant de la liste à supprimer</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        Task<bool> DeleteShoppingListAsync(int listId);

        /// <summary>
        /// Ajoute un article à une liste de courses existante.
        /// </summary>
        /// <param name="listId">Identifiant de la liste</param>
        /// <param name="item">Article à ajouter</param>
        /// <returns>True si l'ajout est réussi, False sinon</returns>
        Task<bool> AddShoppingItemAsync(int listId, ShoppingItem item);

        /// <summary>
        /// Met à jour un article dans une liste de courses.
        /// </summary>
        /// <param name="item">Article à mettre à jour</param>
        /// <returns>True si la mise à jour est réussie, False sinon</returns>
        Task<bool> UpdateShoppingItemAsync(ShoppingItem item);

        /// <summary>
        /// Supprime un article d'une liste de courses.
        /// </summary>
        /// <param name="itemId">Identifiant de l'article à supprimer</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        Task<bool> DeleteShoppingItemAsync(int itemId);

        /// <summary>
        /// Marque un article comme acheté ou non acheté.
        /// </summary>
        /// <param name="itemId">Identifiant de l'article</param>
        /// <param name="isPurchased">État d'achat de l'article</param>
        /// <returns>True si la mise à jour est réussie, False sinon</returns>
        Task<bool> SetItemPurchasedStatusAsync(int itemId, bool isPurchased);

        /// <summary>
        /// Génère une liste de courses à partir d'un plan de repas.
        /// </summary>
        /// <param name="mealPlanId">Identifiant du plan de repas</param>
        /// <returns>La liste de courses générée</returns>
        Task<ShoppingList> GenerateShoppingListFromMealPlanAsync(int mealPlanId);

        /// <summary>
        /// Génère une liste de courses à partir d'une sélection de recettes.
        /// </summary>
        /// <param name="recipeIds">Liste des identifiants de recettes</param>
        /// <param name="listName">Nom de la liste à créer (optionnel)</param>
        /// <returns>La liste de courses générée</returns>
        Task<ShoppingList> GenerateShoppingListFromRecipesAsync(List<int> recipeIds, string listName = "Ma liste de courses");

        /// <summary>
        /// Optimise une liste de courses en regroupant les ingrédients similaires.
        /// </summary>
        /// <param name="listId">Identifiant de la liste à optimiser</param>
        /// <returns>True si l'optimisation est réussie, False sinon</returns>
        Task<bool> OptimizeShoppingListAsync(int listId);
    }
}
