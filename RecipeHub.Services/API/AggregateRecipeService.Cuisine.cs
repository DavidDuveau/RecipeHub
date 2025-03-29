using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace RecipeHub.Services.API
{
    public partial class AggregateRecipeService
    {
        /// <summary>
        /// Récupère les régions (cuisines) disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des régions</returns>
        public async Task<List<string>> GetCuisinesAsync()
        {
            // Clé de cache pour les cuisines consolidées
            string cacheKey = "consolidated_cuisines";
            
            // Vérifier si les cuisines sont dans le cache
            var cachedCuisines = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cachedCuisines != null)
                return cachedCuisines;
                
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tasks = new List<Task<List<string>>>();
            
            // Collecter les cuisines de tous les fournisseurs en parallèle
            foreach (var provider in _providers)
            {
                var task = _requestOptimizer.OptimizeCollectionRequestAsync(
                    provider.ProviderName,
                    () => provider.GetCuisinesAsync(),
                    $"provider_{provider.ProviderName}_cuisines",
                    TimeSpan.FromDays(30));
                    
                tasks.Add(task);
            }
            
            // Attendre que toutes les requêtes soient terminées
            await Task.WhenAll(tasks);
            
            // Consolider les résultats
            foreach (var task in tasks)
            {
                var providerCuisines = await task;
                if (providerCuisines != null)
                {
                    foreach (var cuisine in providerCuisines)
                    {
                        results.Add(cuisine);
                    }
                }
            }
            
            var consolidatedCuisines = results.OrderBy(c => c).ToList();
            
            // Mettre en cache les cuisines consolidées
            await _cacheService.SetAsync(cacheKey, consolidatedCuisines, TimeSpan.FromDays(30));

            return consolidatedCuisines;
        }

        /// <summary>
        /// Récupère les recettes d'une région spécifique.
        /// </summary>
        /// <param name="cuisine">Nom de la région/cuisine</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes de la région</returns>
        public async Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisine, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(cuisine))
                return new List<Recipe>();
                
            // Clé de cache pour cette recherche par cuisine
            string normalizedCuisine = cuisine.Trim().ToLowerInvariant();
            string cacheKey = $"cuisine_{normalizedCuisine}_limit_{limit}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<Recipe>>(cacheKey);
            if (cachedResults != null)
                return cachedResults;

            var results = new List<Recipe>();
            var seenIds = new HashSet<int>(); // Pour éviter les doublons
            var tasks = new List<Task>();
            var providerResults = new ConcurrentDictionary<string, List<Recipe>>();
            
            // Préparer toutes les recherches en parallèle
            foreach (var providerName in _providerPriority)
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    var currentProvider = provider; // Capture pour le lambda
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            // Utiliser l'optimiseur de requêtes
                            var cuisineResults = await _requestOptimizer.OptimizeCollectionRequestAsync(
                                currentProvider.ProviderName,
                                () => currentProvider.GetRecipesByCuisineAsync(cuisine, limit),
                                $"provider_{currentProvider.ProviderName}_cuisine_{normalizedCuisine}_limit_{limit}",
                                TimeSpan.FromDays(1));
                                
                            if (cuisineResults != null && cuisineResults.Any())
                            {
                                providerResults[currentProvider.ProviderName] = cuisineResults;
                            }
                        }
                        catch (Exception)
                        {
                            // Ignorer les erreurs pour un fournisseur spécifique
                        }
                    });
                    
                    tasks.Add(task);
                }
            }
            
            // Attendre que toutes les recherches soient terminées
            await Task.WhenAll(tasks);
            
            // Consolider les résultats dans l'ordre de priorité
            foreach (var providerName in _providerPriority)
            {
                if (providerResults.TryGetValue(providerName, out var providerList))
                {
                    // Ajouter les résultats qui n'ont pas déjà été trouvés
                    foreach (var recipe in providerList)
                    {
                        if (!seenIds.Contains(recipe.Id))
                        {
                            results.Add(recipe);
                            seenIds.Add(recipe.Id);
                        }
                    }
                }
            }
            
            // Mettre en cache les résultats consolidés
            await _cacheService.SetAsync(cacheKey, results, TimeSpan.FromHours(24));

            return results;
        }
    }
}
