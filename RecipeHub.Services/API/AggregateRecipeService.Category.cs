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
        /// Récupère toutes les catégories disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des catégories</returns>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            // Clé de cache pour les catégories consolidées
            string cacheKey = "consolidated_categories";
            
            // Vérifier si les catégories sont dans le cache
            var cachedCategories = await _cacheService.GetAsync<List<Category>>(cacheKey);
            if (cachedCategories != null)
                return cachedCategories;
                
            var results = new Dictionary<string, Category>();
            var tasks = new List<Task<List<Category>>>();
            
            // Collecter les catégories de tous les fournisseurs en parallèle
            foreach (var provider in _providers)
            {
                var task = _requestOptimizer.OptimizeCollectionRequestAsync(
                    provider.ProviderName,
                    () => provider.GetCategoriesAsync(),
                    $"provider_{provider.ProviderName}_categories",
                    TimeSpan.FromDays(30));
                    
                tasks.Add(task);
            }
            
            // Attendre que toutes les requêtes soient terminées
            await Task.WhenAll(tasks);
            
            // Consolider les résultats
            foreach (var task in tasks)
            {
                var providerCategories = await task;
                if (providerCategories != null)
                {
                    foreach (var category in providerCategories)
                    {
                        // Utiliser le nom comme clé pour éviter les doublons
                        if (!results.ContainsKey(category.Name))
                        {
                            results[category.Name] = category;
                        }
                    }
                }
            }
            
            var consolidatedCategories = results.Values.OrderBy(c => c.Name).ToList();
            
            // Mettre en cache les catégories consolidées
            await _cacheService.SetAsync(cacheKey, consolidatedCategories, TimeSpan.FromDays(30));

            return consolidatedCategories;
        }

        /// <summary>
        /// Récupère les recettes d'une catégorie spécifique.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes de la catégorie</returns>
        public async Task<List<Recipe>> GetRecipesByCategoryAsync(string category, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<Recipe>();
                
            // Clé de cache pour cette recherche par catégorie
            string normalizedCategory = category.Trim().ToLowerInvariant();
            string cacheKey = $"category_{normalizedCategory}_limit_{limit}";
            
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
                            var categoryResults = await _requestOptimizer.OptimizeCollectionRequestAsync(
                                currentProvider.ProviderName,
                                () => currentProvider.GetRecipesByCategoryAsync(category, limit),
                                $"provider_{currentProvider.ProviderName}_category_{normalizedCategory}_limit_{limit}",
                                TimeSpan.FromDays(1));
                                
                            if (categoryResults != null && categoryResults.Any())
                            {
                                providerResults[currentProvider.ProviderName] = categoryResults;
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
