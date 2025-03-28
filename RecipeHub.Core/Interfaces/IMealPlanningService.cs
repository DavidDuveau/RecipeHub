using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de planification des repas.
    /// </summary>
    public interface IMealPlanningService
    {
        /// <summary>
        /// Récupère tous les plans de repas.
        /// </summary>
        /// <returns>Liste des plans de repas</returns>
        Task<List<MealPlan>> GetAllMealPlansAsync();

        /// <summary>
        /// Récupère le plan de repas actif.
        /// </summary>
        /// <returns>Le plan de repas actif ou null si aucun n'est actif</returns>
        Task<MealPlan?> GetActiveMealPlanAsync();

        /// <summary>
        /// Récupère un plan de repas par son identifiant.
        /// </summary>
        /// <param name="planId">Identifiant du plan</param>
        /// <returns>Le plan de repas ou null si non trouvé</returns>
        Task<MealPlan?> GetMealPlanByIdAsync(int planId);

        /// <summary>
        /// Récupère les plans de repas pour une période donnée.
        /// </summary>
        /// <param name="startDate">Date de début</param>
        /// <param name="endDate">Date de fin</param>
        /// <returns>Liste des plans de repas dans la période</returns>
        Task<List<MealPlan>> GetMealPlansByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Crée un nouveau plan de repas.
        /// </summary>
        /// <param name="mealPlan">Plan de repas à créer</param>
        /// <returns>L'identifiant du nouveau plan</returns>
        Task<int> CreateMealPlanAsync(MealPlan mealPlan);

        /// <summary>
        /// Met à jour un plan de repas existant.
        /// </summary>
        /// <param name="mealPlan">Plan de repas à mettre à jour</param>
        /// <returns>True si la mise à jour est réussie, False sinon</returns>
        Task<bool> UpdateMealPlanAsync(MealPlan mealPlan);

        /// <summary>
        /// Supprime un plan de repas.
        /// </summary>
        /// <param name="planId">Identifiant du plan à supprimer</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        Task<bool> DeleteMealPlanAsync(int planId);

        /// <summary>
        /// Définit un plan de repas comme actif et désactive les autres.
        /// </summary>
        /// <param name="planId">Identifiant du plan à activer</param>
        /// <returns>True si l'activation est réussie, False sinon</returns>
        Task<bool> SetMealPlanActiveAsync(int planId);

        /// <summary>
        /// Ajoute un repas planifié à un plan existant.
        /// </summary>
        /// <param name="planId">Identifiant du plan</param>
        /// <param name="plannedMeal">Repas planifié à ajouter</param>
        /// <returns>True si l'ajout est réussi, False sinon</returns>
        Task<bool> AddPlannedMealAsync(int planId, PlannedMeal plannedMeal);

        /// <summary>
        /// Met à jour un repas planifié.
        /// </summary>
        /// <param name="plannedMeal">Repas planifié à mettre à jour</param>
        /// <returns>True si la mise à jour est réussie, False sinon</returns>
        Task<bool> UpdatePlannedMealAsync(PlannedMeal plannedMeal);

        /// <summary>
        /// Supprime un repas planifié.
        /// </summary>
        /// <param name="mealId">Identifiant du repas à supprimer</param>
        /// <returns>True si la suppression est réussie, False sinon</returns>
        Task<bool> DeletePlannedMealAsync(int mealId);

        /// <summary>
        /// Récupère tous les repas planifiés pour une date spécifique.
        /// </summary>
        /// <param name="date">Date des repas</param>
        /// <returns>Liste des repas planifiés pour la date</returns>
        Task<List<PlannedMeal>> GetPlannedMealsByDateAsync(DateTime date);

        /// <summary>
        /// Génère un plan de repas suggéré pour une période donnée.
        /// </summary>
        /// <param name="startDate">Date de début</param>
        /// <param name="endDate">Date de fin</param>
        /// <param name="preferences">Préférences alimentaires (facultatif)</param>
        /// <returns>Un plan de repas suggéré</returns>
        Task<MealPlan> GenerateSuggestedMealPlanAsync(DateTime startDate, DateTime endDate, Dictionary<string, object>? preferences = null);
    }
}
