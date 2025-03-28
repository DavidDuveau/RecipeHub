using RecipeHub.Core.Models;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de gestion des métriques d'utilisation des APIs.
    /// </summary>
    public interface IApiMetricsService
    {
        /// <summary>
        /// Enregistre un nouveau fournisseur d'API.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="dailyQuota">Quota quotidien</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task RegisterProviderAsync(string providerName, int dailyQuota);

        /// <summary>
        /// Obtient les métriques d'utilisation d'un fournisseur spécifique.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Métriques d'utilisation ou null si le fournisseur n'est pas enregistré</returns>
        Task<ApiUsageMetrics?> GetProviderMetricsAsync(string providerName);

        /// <summary>
        /// Obtient les métriques d'utilisation de tous les fournisseurs enregistrés.
        /// </summary>
        /// <returns>Dictionnaire associant le nom du fournisseur à ses métriques</returns>
        Task<Dictionary<string, ApiUsageMetrics>> GetAllProviderMetricsAsync();

        /// <summary>
        /// Incrémente le compteur d'utilisation d'un fournisseur.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="count">Nombre d'appels à comptabiliser (1 par défaut)</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task IncrementUsageAsync(string providerName, int count = 1);

        /// <summary>
        /// Réinitialise le compteur quotidien d'un fournisseur.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task ResetCounterAsync(string providerName);

        /// <summary>
        /// Vérifie si le quota d'un fournisseur est dépassé.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Vrai si le quota est dépassé, faux sinon</returns>
        Task<bool> IsQuotaExceededAsync(string providerName);

        /// <summary>
        /// Obtient le nombre d'appels restants pour un fournisseur aujourd'hui.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Nombre d'appels restants</returns>
        Task<int> GetRemainingCallsAsync(string providerName);

        /// <summary>
        /// Obtient le fournisseur ayant le plus d'appels restants.
        /// </summary>
        /// <param name="preferredOrder">Ordre de préférence des fournisseurs (optionnel)</param>
        /// <returns>Nom du fournisseur recommandé ou null si aucun n'est disponible</returns>
        Task<string?> GetRecommendedProviderAsync(List<string>? preferredOrder = null);

        /// <summary>
        /// Sauvegarde les métriques d'utilisation.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task SaveMetricsAsync();

        /// <summary>
        /// Charge les métriques d'utilisation précédemment sauvegardées.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task LoadMetricsAsync();
    }
}
