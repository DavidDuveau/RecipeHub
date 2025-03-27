using System;
using System.Threading.Tasks;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de cache.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Récupère un élément du cache.
        /// </summary>
        /// <typeparam name="T">Type de l'élément à récupérer</typeparam>
        /// <param name="key">Clé unique de l'élément</param>
        /// <returns>L'élément en cache ou la valeur par défaut si non trouvé</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Ajoute ou met à jour un élément dans le cache.
        /// </summary>
        /// <typeparam name="T">Type de l'élément à stocker</typeparam>
        /// <param name="key">Clé unique pour identifier l'élément</param>
        /// <param name="value">Valeur à stocker</param>
        /// <param name="expirationTime">Durée de validité du cache (null pour ne pas expirer)</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null) where T : class;

        /// <summary>
        /// Supprime un élément du cache.
        /// </summary>
        /// <param name="key">Clé de l'élément à supprimer</param>
        /// <returns>True si l'élément a été supprimé avec succès, False sinon</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// Vérifie si un élément existe dans le cache.
        /// </summary>
        /// <param name="key">Clé à vérifier</param>
        /// <returns>True si l'élément existe et n'est pas expiré, False sinon</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Vide le cache.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        Task ClearAsync();
    }
}
