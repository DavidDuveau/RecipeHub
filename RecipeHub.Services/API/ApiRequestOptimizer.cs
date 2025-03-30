using RecipeHub.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RecipeHub.Services.API
{
    /// <summary>
    /// Service d'optimisation des requêtes API permettant de maximiser l'utilisation des quotas disponibles.
    /// </summary>
    public class ApiRequestOptimizer : IApiRequestOptimizer
    {
        private readonly IApiMetricsService _metricsService;
        private readonly ICacheService _cacheService;
        private readonly SemaphoreSlim _requestSemaphore = new SemaphoreSlim(1, 1);
        
        // Fenêtre de temps minimum entre les appels à la même API (pour éviter les surcharges)
        private readonly Dictionary<string, DateTime> _lastRequestTimes = new Dictionary<string, DateTime>();
        private readonly TimeSpan _minimumRequestInterval = TimeSpan.FromMilliseconds(100);
        
        // Configuration des stratégies par fournisseur
        private readonly Dictionary<string, Core.Interfaces.OptimizationStrategy> _providerStrategies = new Dictionary<string, Core.Interfaces.OptimizationStrategy>();
        
        /// <summary>
        /// Constructeur du service d'optimisation des requêtes.
        /// </summary>
        /// <param name="metricsService">Service de métriques d'API</param>
        /// <param name="cacheService">Service de cache</param>
        public ApiRequestOptimizer(IApiMetricsService metricsService, ICacheService cacheService)
        {
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        /// <summary>
        /// Configure la stratégie d'optimisation pour un fournisseur spécifique.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="strategy">Stratégie d'optimisation</param>
        public void SetProviderStrategy(string providerName, Core.Interfaces.OptimizationStrategy strategy)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Le nom du fournisseur ne peut pas être vide.", nameof(providerName));
                
            _providerStrategies[providerName] = strategy;
        }

        /// <summary>
        /// Récupère la stratégie d'optimisation actuelle pour un fournisseur.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Stratégie d'optimisation</returns>
        public Core.Interfaces.OptimizationStrategy GetProviderStrategy(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Le nom du fournisseur ne peut pas être vide.", nameof(providerName));
                
            if (_providerStrategies.TryGetValue(providerName, out var strategy))
                return strategy;
                
            // Stratégie par défaut
            return Core.Interfaces.OptimizationStrategy.Balanced;
        }

        /// <summary>
        /// Exécute une action en appliquant les stratégies d'optimisation des requêtes API.
        /// </summary>
        /// <typeparam name="T">Type de retour de l'action</typeparam>
        /// <param name="providerName">Nom du fournisseur d'API</param>
        /// <param name="action">Action à exécuter</param>
        /// <param name="cacheKey">Clé de cache (optionnelle)</param>
        /// <param name="cacheDuration">Durée de mise en cache (optionnelle)</param>
        /// <param name="apiCost">Coût de l'appel API en nombre d'unités (1 par défaut)</param>
        /// <returns>Résultat de l'action</returns>
        public async Task<T> OptimizeRequestAsync<T>(
            string providerName,
            Func<Task<T>> action,
            string? cacheKey = null,
            TimeSpan? cacheDuration = null,
            int apiCost = 1) where T : class
        {
            // Vérifier si la réponse est dans le cache (si une clé de cache est fournie)
            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                var cachedResult = await _cacheService.GetAsync<T>(cacheKey);
                if (cachedResult != null)
                    return cachedResult;
            }

            // Vérifier les quotas et appliquer la stratégie d'optimisation
            await _requestSemaphore.WaitAsync();
            try
            {
                // Vérifier si le quota est dépassé
                bool isQuotaExceeded = await _metricsService.IsQuotaExceededAsync(providerName);
                if (isQuotaExceeded)
                {
                    // Si le quota est dépassé, choisir un autre fournisseur ou retourner un résultat par défaut
                    var strategy = GetProviderStrategy(providerName);
                    
                    switch (strategy)
                    {
                        case Core.Interfaces.OptimizationStrategy.ConservativeQuota:
                            // En mode conservateur, on ne fait pas l'appel si on est à plus de 90% du quota
                            var metrics = await _metricsService.GetProviderMetricsAsync(providerName);
                            if (metrics != null && metrics.GetUsagePercentage() > 90)
                            {
                                throw new QuotaExceededException($"Le quota pour {providerName} est presque épuisé (stratégie conservative).");
                            }
                            break;
                            
                        case Core.Interfaces.OptimizationStrategy.QuotaProtection:
                            // En mode protection, on refuse l'appel si le quota est dépassé
                            throw new QuotaExceededException($"Le quota pour {providerName} est épuisé.");
                            
                        case Core.Interfaces.OptimizationStrategy.Fallback:
                            // En mode fallback, on cherche un autre fournisseur avec des quotas disponibles
                            var alternateProvider = await _metricsService.GetRecommendedProviderAsync();
                            if (alternateProvider != null && alternateProvider != providerName)
                            {
                                // Notifier qu'on a basculé vers un autre fournisseur (via log ou événement)
                                // Cette partie pourrait être améliorée avec un pattern d'observateur
                                throw new ProviderFallbackException(providerName, alternateProvider);
                            }
                            break;
                            
                        default:
                            // En mode équilibré (par défaut), on continue malgré tout
                            break;
                    }
                }

                // Respecter l'intervalle minimum entre les requêtes
                if (_lastRequestTimes.TryGetValue(providerName, out var lastRequestTime))
                {
                    var elapsed = DateTime.UtcNow - lastRequestTime;
                    if (elapsed < _minimumRequestInterval)
                    {
                        // Attendre avant de faire la requête
                        await Task.Delay(_minimumRequestInterval - elapsed);
                    }
                }

                // Exécuter l'action (l'appel API)
                var result = await action();

                // Mettre à jour le temps de la dernière requête
                _lastRequestTimes[providerName] = DateTime.UtcNow;

                // Incrémenter le compteur d'utilisation de l'API
                await _metricsService.IncrementUsageAsync(providerName, apiCost);

                // Mettre en cache le résultat si nécessaire
                if (!string.IsNullOrWhiteSpace(cacheKey) && result != null && cacheDuration.HasValue)
                {
                    await _cacheService.SetAsync(cacheKey, result, cacheDuration);
                }

                return result;
            }
            finally
            {
                _requestSemaphore.Release();
            }
        }

        /// <summary>
        /// Exécute une action qui renvoie une liste en appliquant les stratégies d'optimisation des requêtes API.
        /// Inclut des optimisations spécifiques pour les collections.
        /// </summary>
        /// <typeparam name="T">Type des éléments de la liste</typeparam>
        /// <param name="providerName">Nom du fournisseur d'API</param>
        /// <param name="action">Action à exécuter</param>
        /// <param name="cacheKey">Clé de cache (optionnelle)</param>
        /// <param name="cacheDuration">Durée de mise en cache (optionnelle)</param>
        /// <param name="apiCost">Coût de l'appel API en nombre d'unités (1 par défaut)</param>
        /// <returns>Liste des résultats</returns>
        public async Task<List<T>> OptimizeCollectionRequestAsync<T>(
            string providerName,
            Func<Task<List<T>>> action,
            string? cacheKey = null,
            TimeSpan? cacheDuration = null,
            int apiCost = 1) where T : class
        {
            // Réutiliser la méthode générique pour traiter le cas des listes
            return await OptimizeRequestAsync(
                providerName,
                action,
                cacheKey,
                cacheDuration,
                apiCost);
        }

        /// <summary>
        /// Exécute une action de manière groupée pour optimiser les appels API multiples.
        /// </summary>
        /// <typeparam name="TInput">Type des données d'entrée</typeparam>
        /// <typeparam name="TOutput">Type des données de sortie</typeparam>
        /// <param name="providerName">Nom du fournisseur d'API</param>
        /// <param name="items">Liste des éléments à traiter</param>
        /// <param name="batchAction">Action à exécuter sur un lot d'éléments</param>
        /// <param name="batchSize">Taille du lot (nombre d'éléments à traiter en une seule requête)</param>
        /// <param name="getCacheKey">Fonction pour générer une clé de cache pour un élément</param>
        /// <param name="cacheDuration">Durée de mise en cache (optionnelle)</param>
        /// <param name="apiCostPerBatch">Coût API pour chaque lot traité (1 par défaut)</param>
        /// <returns>Dictionnaire associant chaque élément d'entrée à son résultat</returns>
        public async Task<Dictionary<TInput, TOutput>> BatchProcessAsync<TInput, TOutput>(
            string providerName,
            IEnumerable<TInput> items,
            Func<IEnumerable<TInput>, Task<Dictionary<TInput, TOutput>>> batchAction,
            int batchSize = 10,
            Func<TInput, string>? getCacheKey = null,
            TimeSpan? cacheDuration = null,
            int apiCostPerBatch = 1) where TOutput : class
        {
            var itemsList = items.ToList();
            var results = new Dictionary<TInput, TOutput>();
            var uncachedItems = new List<TInput>();

            // Vérifier d'abord le cache pour chaque élément (si la fonction de clé de cache est fournie)
            if (getCacheKey != null)
            {
                foreach (var item in itemsList)
                {
                    var cacheKey = getCacheKey(item);
                    if (!string.IsNullOrWhiteSpace(cacheKey))
                    {
                        var cachedResult = await _cacheService.GetAsync<TOutput>(cacheKey);
                        if (cachedResult != null)
                        {
                            results[item] = cachedResult;
                        }
                        else
                        {
                            uncachedItems.Add(item);
                        }
                    }
                    else
                    {
                        uncachedItems.Add(item);
                    }
                }
            }
            else
            {
                uncachedItems = itemsList;
            }

            // Traiter les éléments non mis en cache par lots
            for (int i = 0; i < uncachedItems.Count; i += batchSize)
            {
                var batch = uncachedItems.Skip(i).Take(batchSize).ToList();
                if (batch.Count > 0)
                {
                    // Optimiser la requête pour ce lot
                    var batchResults = await OptimizeRequestAsync(
                        providerName,
                        async () => await batchAction(batch),
                        null, // Pas de mise en cache globale du lot
                        null,
                        apiCostPerBatch);

                    // Ajouter les résultats du lot
                    foreach (var kvp in batchResults)
                    {
                        results[kvp.Key] = kvp.Value;

                        // Mettre en cache individuellement chaque résultat si nécessaire
                        if (getCacheKey != null && cacheDuration.HasValue)
                        {
                            var cacheKey = getCacheKey(kvp.Key);
                            if (!string.IsNullOrWhiteSpace(cacheKey))
                            {
                                await _cacheService.SetAsync(cacheKey, kvp.Value, cacheDuration);
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Exécute la même requête sur plusieurs fournisseurs et agrège les résultats.
        /// </summary>
        /// <typeparam name="T">Type de retour des actions</typeparam>
        /// <param name="actions">Dictionnaire associant le nom du fournisseur à l'action à exécuter</param>
        /// <param name="aggregator">Fonction pour agréger les résultats</param>
        /// <param name="cachePrefixKey">Préfixe de la clé de cache (optionnel)</param>
        /// <param name="cacheDuration">Durée de mise en cache (optionnelle)</param>
        /// <returns>Résultat agrégé</returns>
        public async Task<T> ExecuteMultiProviderAsync<T>(
            Dictionary<string, Func<Task<T>>> actions,
            Func<Dictionary<string, T>, T> aggregator,
            string? cachePrefixKey = null,
            TimeSpan? cacheDuration = null) where T : class
        {
            // Vérifier le cache global si une clé est fournie
            if (!string.IsNullOrWhiteSpace(cachePrefixKey))
            {
                var cacheKey = $"{cachePrefixKey}_aggregated";
                var cachedResult = await _cacheService.GetAsync<T>(cacheKey);
                if (cachedResult != null)
                    return cachedResult;
            }

            // Exécuter toutes les actions en parallèle
            var results = new Dictionary<string, T>();
            var tasks = new Dictionary<string, Task<T?>>();

            foreach (var kvp in actions)
            {
                var providerName = kvp.Key;
                var action = kvp.Value;
                
                // Générer une clé de cache spécifique au fournisseur si nécessaire
                string? cacheKey = !string.IsNullOrWhiteSpace(cachePrefixKey) 
                    ? $"{cachePrefixKey}_{providerName}"
                    : null;
                
                // Utiliser OptimizeRequestAsync pour chaque fournisseur
                tasks[providerName] = OptimizeRequestAsync(providerName, action, cacheKey, cacheDuration);
            }

            // Attendre que toutes les tâches soient terminées
            await Task.WhenAll(tasks.Values);

            // Récupérer les résultats (ignorer les null)
            foreach (var kvp in tasks)
            {
                var result = await kvp.Value;
                if (result != null)
                {
                    results[kvp.Key] = result;
                }
            }

            // Agréger les résultats
            var aggregatedResult = aggregator(results);

            // Mettre en cache le résultat agrégé si nécessaire
            if (!string.IsNullOrWhiteSpace(cachePrefixKey) && aggregatedResult != null && cacheDuration.HasValue)
            {
                var cacheKey = $"{cachePrefixKey}_aggregated";
                await _cacheService.SetAsync(cacheKey, aggregatedResult, cacheDuration);
            }

            return aggregatedResult;
        }
    }

    /// <summary>
    /// Exception levée lorsqu'un quota d'API est dépassé.
    /// </summary>
    public class QuotaExceededException : Exception
    {
        /// <summary>
        /// Constructeur de l'exception de quota dépassé.
        /// </summary>
        /// <param name="message">Message d'erreur</param>
        public QuotaExceededException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception levée lorsqu'un basculement vers un autre fournisseur est nécessaire.
    /// </summary>
    public class ProviderFallbackException : Exception
    {
        /// <summary>
        /// Nom du fournisseur d'origine.
        /// </summary>
        public string OriginalProvider { get; }

        /// <summary>
        /// Nom du fournisseur de repli.
        /// </summary>
        public string FallbackProvider { get; }

        /// <summary>
        /// Constructeur de l'exception de basculement de fournisseur.
        /// </summary>
        /// <param name="originalProvider">Nom du fournisseur d'origine</param>
        /// <param name="fallbackProvider">Nom du fournisseur de repli</param>
        public ProviderFallbackException(string originalProvider, string fallbackProvider)
            : base($"Basculement du fournisseur {originalProvider} vers {fallbackProvider} en raison d'un quota épuisé.")
        {
            OriginalProvider = originalProvider;
            FallbackProvider = fallbackProvider;
        }
    }
}
