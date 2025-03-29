using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecipeHub.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service d'optimisation des requêtes API.
    /// </summary>
    public interface IApiRequestOptimizer
    {
        /// <summary>
        /// Configure la stratégie d'optimisation pour un fournisseur spécifique.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="strategy">Stratégie d'optimisation</param>
        void SetProviderStrategy(string providerName, OptimizationStrategy strategy);

        /// <summary>
        /// Récupère la stratégie d'optimisation actuelle pour un fournisseur.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Stratégie d'optimisation</returns>
        OptimizationStrategy GetProviderStrategy(string providerName);

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
        Task<T> OptimizeRequestAsync<T>(
            string providerName,
            Func<Task<T>> action,
            string? cacheKey = null,
            TimeSpan? cacheDuration = null,
            int apiCost = 1) where T : class;

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
        Task<List<T>> OptimizeCollectionRequestAsync<T>(
            string providerName,
            Func<Task<List<T>>> action,
            string? cacheKey = null,
            TimeSpan? cacheDuration = null,
            int apiCost = 1) where T : class;

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
        Task<Dictionary<TInput, TOutput>> BatchProcessAsync<TInput, TOutput>(
            string providerName,
            IEnumerable<TInput> items,
            Func<IEnumerable<TInput>, Task<Dictionary<TInput, TOutput>>> batchAction,
            int batchSize = 10,
            Func<TInput, string>? getCacheKey = null,
            TimeSpan? cacheDuration = null,
            int apiCostPerBatch = 1) where TOutput : class;

        /// <summary>
        /// Exécute la même requête sur plusieurs fournisseurs et agrège les résultats.
        /// </summary>
        /// <typeparam name="T">Type de retour des actions</typeparam>
        /// <param name="actions">Dictionnaire associant le nom du fournisseur à l'action à exécuter</param>
        /// <param name="aggregator">Fonction pour agréger les résultats</param>
        /// <param name="cachePrefixKey">Préfixe de la clé de cache (optionnel)</param>
        /// <param name="cacheDuration">Durée de mise en cache (optionnelle)</param>
        /// <returns>Résultat agrégé</returns>
        Task<T> ExecuteMultiProviderAsync<T>(
            Dictionary<string, Func<Task<T>>> actions,
            Func<Dictionary<string, T>, T> aggregator,
            string? cachePrefixKey = null,
            TimeSpan? cacheDuration = null) where T : class;
    }

    /// <summary>
    /// Stratégies d'optimisation des requêtes API.
    /// </summary>
    public enum OptimizationStrategy
    {
        /// <summary>
        /// Stratégie équilibrée : utilisation normale des quotas disponibles.
        /// </summary>
        Balanced,

        /// <summary>
        /// Stratégie conservatrice : préserve les quotas pour les requêtes importantes.
        /// </summary>
        ConservativeQuota,

        /// <summary>
        /// Stratégie de protection stricte : refuse les appels lorsque le quota est dépassé.
        /// </summary>
        QuotaProtection,

        /// <summary>
        /// Stratégie de repli : bascule vers un autre fournisseur lorsque le quota est dépassé.
        /// </summary>
        Fallback
    }
}
