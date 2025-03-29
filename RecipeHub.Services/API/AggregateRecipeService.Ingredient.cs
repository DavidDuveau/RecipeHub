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
        /// Récupère la liste des ingrédients disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des ingrédients</returns>
        public async Task<List<string>> GetIngredientsAsync()
        {
            // Clé de cache pour les ingrédients consolidés
            string cacheKey = "consolidated_ingredients";
            
            // Vérifier si les ingrédients sont dans le cache
            var cachedIngredients = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cachedIngredients != null)
                return cachedIngredients;
                
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tasks = new List<Task<List<string>>>();
            
            // Collecter les ingrédients de tous les fournisseurs en parallèle
            foreach (var provider in _providers)
            {
                var task = _requestOptimizer.OptimizeCollectionRequestAsync(
                    provider.ProviderName,
                    () => provider.GetIngredientsAsync(),
                    $"provider_{provider.ProviderName}_ingredients",
                    TimeSpan.FromDays(30));
                    
                tasks.Add(task);
            }
            
            // Attendre que toutes les requêtes soient terminées
            await Task.WhenAll(tasks);
            
            // Consolider les résultats
            foreach (var task in tasks)
            {
                var providerIngredients = await task;
                if (providerIngredients != null)
                {
                    foreach (var ingredient in providerIngredients)
                    {
                        results.Add(ingredient);
                    }
                }
            }
            
            var consolidatedIngredients = results.OrderBy(i => i).ToList();
            
            // Mettre en cache les ingrédients consolidés
            await _cacheService.SetAsync(cacheKey, consolidatedIngredients, TimeSpan.FromDays(30));

            return consolidatedIngredients;
        }

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes contenant l'ingrédient</returns>
        public async Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(ingredient))
                return new List<Recipe>();
                
            // Clé de cache pour cette recherche par ingrédient
            string normalizedIngredient = ingredient.Trim().ToLowerInvariant();
            string cacheKey = $"ingredient_{normalizedIngredient}_limit_{limit}";
            
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
                            var ingredientResults = await _requestOptimizer.OptimizeCollectionRequestAsync(
                                currentProvider.ProviderName,
                                () => currentProvider.GetRecipesByIngredientAsync(ingredient, limit),
                                $"provider_{currentProvider.ProviderName}_ingredient_{normalizedIngredient}_limit_{limit}",
                                TimeSpan.FromDays(1));
                                
                            if (ingredientResults != null && ingredientResults.Any())
                            {
                                providerResults[currentProvider.ProviderName] = ingredientResults;
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
