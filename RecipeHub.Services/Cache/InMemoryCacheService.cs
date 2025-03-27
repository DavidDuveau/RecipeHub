using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RecipeHub.Services.Cache
{
    /// <summary>
    /// Implémentation en mémoire du service de cache.
    /// </summary>
    public class InMemoryCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

        /// <summary>
        /// Récupère un élément du cache.
        /// </summary>
        /// <typeparam name="T">Type de l'élément à récupérer</typeparam>
        /// <param name="key">Clé unique de l'élément</param>
        /// <returns>L'élément en cache ou la valeur par défaut si non trouvé ou expiré</returns>
        public Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var cacheItem))
            {
                // Vérification de l'expiration
                if (cacheItem.ExpirationTime.HasValue && DateTime.UtcNow > cacheItem.ExpirationTime.Value)
                {
                    // L'élément est expiré, on le supprime
                    _cache.TryRemove(key, out _);
                    return Task.FromResult<T?>(null);
                }

                // Désérialisation de l'objet stocké
                return Task.FromResult(JsonConvert.DeserializeObject<T>(cacheItem.SerializedValue));
            }

            return Task.FromResult<T?>(null);
        }

        /// <summary>
        /// Ajoute ou met à jour un élément dans le cache.
        /// </summary>
        /// <typeparam name="T">Type de l'élément à stocker</typeparam>
        /// <param name="key">Clé unique pour identifier l'élément</param>
        /// <param name="value">Valeur à stocker</param>
        /// <param name="expirationTime">Durée de validité du cache (null pour ne pas expirer)</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Sérialisation de l'objet pour le stockage
            var serializedValue = JsonConvert.SerializeObject(value);
            
            // Calcul de la date d'expiration si nécessaire
            DateTime? expiration = expirationTime.HasValue 
                ? DateTime.UtcNow.Add(expirationTime.Value) 
                : null;

            // Création ou mise à jour de l'élément dans le cache
            var cacheItem = new CacheItem
            {
                SerializedValue = serializedValue,
                ExpirationTime = expiration
            };

            _cache[key] = cacheItem;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Supprime un élément du cache.
        /// </summary>
        /// <param name="key">Clé de l'élément à supprimer</param>
        /// <returns>True si l'élément a été supprimé avec succès, False sinon</returns>
        public Task<bool> RemoveAsync(string key)
        {
            return Task.FromResult(_cache.TryRemove(key, out _));
        }

        /// <summary>
        /// Vérifie si un élément existe dans le cache et n'est pas expiré.
        /// </summary>
        /// <param name="key">Clé à vérifier</param>
        /// <returns>True si l'élément existe et n'est pas expiré, False sinon</returns>
        public Task<bool> ExistsAsync(string key)
        {
            if (_cache.TryGetValue(key, out var cacheItem))
            {
                // Vérification de l'expiration
                if (cacheItem.ExpirationTime.HasValue && DateTime.UtcNow > cacheItem.ExpirationTime.Value)
                {
                    // L'élément est expiré, on le supprime
                    _cache.TryRemove(key, out _);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Vide le cache.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public Task ClearAsync()
        {
            _cache.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Classe interne représentant un élément du cache.
        /// </summary>
        private class CacheItem
        {
            /// <summary>
            /// La valeur sérialisée de l'objet stocké.
            /// </summary>
            public string SerializedValue { get; set; } = string.Empty;

            /// <summary>
            /// Date d'expiration de l'élément (UTC).
            /// </summary>
            public DateTime? ExpirationTime { get; set; }
        }
    }
}
