using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace RecipeHub.Services.API
{
    public partial class AggregateRecipeService
    {
        /// <summary>
        /// Obtient les statistiques d'utilisation de tous les fournisseurs.
        /// </summary>
        /// <returns>Dictionnaire associant le nom du fournisseur à son utilisation (utilisé/total)</returns>
        public async Task<Dictionary<string, (int Used, int Total)>> GetApiUsageStatisticsAsync()
        {
            // Clé de cache pour les statistiques d'utilisation
            string cacheKey = "api_usage_statistics";
            
            // Vérifier si les statistiques sont dans le cache (avec une durée de validité courte)
            var cachedStatistics = await _cacheService.GetAsync<Dictionary<string, (int Used, int Total)>>(cacheKey);
            if (cachedStatistics != null)
                return cachedStatistics;
                
            var statistics = new Dictionary<string, (int Used, int Total)>();
            var tasks = new Dictionary<string, Task<int>>();
            
            // Récupérer les statistiques de tous les fournisseurs en parallèle
            foreach (var provider in _providers)
            {
                tasks[provider.ProviderName] = provider.GetRemainingCallsAsync();
            }
            
            // Attendre que toutes les requêtes soient terminées
            await Task.WhenAll(tasks.Values);
            
            // Consolider les résultats
            foreach (var provider in _providers)
            {
                var remaining = await tasks[provider.ProviderName];
                var total = provider.DailyQuota;
                var used = total - remaining;
                
                statistics[provider.ProviderName] = (used, total);
            }
            
            // Mettre en cache les statistiques (pour une courte durée)
            await _cacheService.SetAsync(cacheKey, statistics, TimeSpan.FromMinutes(2));
            
            return statistics;
        }

        /// <summary>
        /// Définit l'ordre de priorité des fournisseurs.
        /// </summary>
        /// <param name="providerOrder">Liste ordonnée des noms de fournisseurs</param>
        /// <param name="optimizationStrategies">Dictionnaire associant des stratégies d'optimisation aux fournisseurs (optionnel)</param>
        public void SetProviderPriority(List<string> providerOrder, Dictionary<string, OptimizationStrategy>? optimizationStrategies = null)
        {
            if (providerOrder == null || !providerOrder.Any())
                throw new ArgumentException("L'ordre des fournisseurs ne peut pas être vide.", nameof(providerOrder));
                
            // Vérifier que tous les noms correspondent à des fournisseurs existants
            var validProviderNames = _providers.Select(p => p.ProviderName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var providerName in providerOrder)
            {
                if (!validProviderNames.Contains(providerName))
                    throw new ArgumentException($"Le fournisseur '{providerName}' n'existe pas.", nameof(providerOrder));
            }
            
            // S'assurer que tous les fournisseurs sont inclus
            var missingProviders = _providers
                .Where(p => !providerOrder.Contains(p.ProviderName, StringComparer.OrdinalIgnoreCase))
                .Select(p => p.ProviderName)
                .ToList();
                
            // Ajouter les fournisseurs manquants à la fin de la liste
            var newOrder = new List<string>(providerOrder);
            newOrder.AddRange(missingProviders);
            
            _providerPriority = newOrder;
            
            // Mettre à jour les stratégies d'optimisation si spécifiées
            if (optimizationStrategies != null)
            {
                foreach (var kvp in optimizationStrategies)
                {
                    if (validProviderNames.Contains(kvp.Key))
                    {
                        _requestOptimizer.SetProviderStrategy(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Obtient l'ordre de priorité actuel des fournisseurs.
        /// </summary>
        /// <returns>Liste ordonnée des noms de fournisseurs</returns>
        public List<string> GetProviderPriority()
        {
            return new List<string>(_providerPriority);
        }
        
        /// <summary>
        /// Obtient les stratégies d'optimisation pour tous les fournisseurs.
        /// </summary>
        /// <returns>Dictionnaire associant le nom du fournisseur à sa stratégie d'optimisation</returns>
        public Dictionary<string, OptimizationStrategy> GetProviderOptimizationStrategies()
        {
            var result = new Dictionary<string, OptimizationStrategy>();
            
            foreach (var provider in _providers)
            {
                var strategy = _requestOptimizer.GetProviderStrategy(provider.ProviderName);
                result[provider.ProviderName] = strategy;
            }
            
            return result;
        }
        
        /// <summary>
        /// Définit la stratégie d'optimisation pour un fournisseur spécifique.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="strategy">Stratégie d'optimisation</param>
        public void SetProviderOptimizationStrategy(string providerName, OptimizationStrategy strategy)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Le nom du fournisseur ne peut pas être vide.", nameof(providerName));
                
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
                throw new ArgumentException($"Le fournisseur '{providerName}' n'existe pas.", nameof(providerName));
                
            _requestOptimizer.SetProviderStrategy(providerName, strategy);
        }
    }
}
