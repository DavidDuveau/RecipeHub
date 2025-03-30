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
        /// Recherche des recettes par leur nom à travers toutes les sources disponibles.
        /// </summary>
        /// <param name="name">Nom ou partie du nom à rechercher</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes correspondantes</returns>
        public async Task<List<Recipe>> SearchRecipesByNameAsync(string name, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Recipe>();
                
            // Clé de cache pour cette recherche
            string normalizedName = name.Trim().ToLowerInvariant();
            string cacheKey = $"search_{normalizedName}_limit_{limit}";
            
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
                            var searchResults = await _requestOptimizer.OptimizeCollectionRequestAsync(
                                currentProvider.ProviderName,
                                () => currentProvider.SearchRecipesByNameAsync(name, limit),
                                $"provider_{currentProvider.ProviderName}_search_{normalizedName}_limit_{limit}",
                                TimeSpan.FromDays(1));
                                
                            if (searchResults != null && searchResults.Any())
                            {
                                providerResults[currentProvider.ProviderName] = searchResults;
                            }
                        }
                        catch (Exception)
                        {
                            // Ignorer les erreurs pour un fournisseur spécifique
                            // Les autres fournisseurs peuvent encore fournir des résultats
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
