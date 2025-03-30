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
        /// Récupère une recette par son identifiant.
        /// </summary>
        /// <param name="id">Identifiant de la recette</param>
        /// <param name="preferredProvider">Nom du fournisseur préféré (optionnel)</param>
        /// <returns>La recette correspondante ou null si non trouvée</returns>
        public async Task<Recipe?> GetRecipeByIdAsync(int id, string? preferredProvider = null)
        {
            // Clé de cache globale pour cette recette (indépendamment du fournisseur)
            string cacheKey = $"aggregate_recipe_{id}";
            
            // Vérifier si la recette est dans le cache global
            var cachedRecipe = await _cacheService.GetAsync<Recipe>(cacheKey);
            if (cachedRecipe != null)
                return cachedRecipe;
                
            try
            {
                // Vérifier si une source préférée est spécifiée
                if (!string.IsNullOrWhiteSpace(preferredProvider))
                {
                    var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase));
                    if (provider != null)
                    {
                        // Utiliser l'optimiseur de requêtes
                        var recipe = await _requestOptimizer.OptimizeRequestAsync(
                            provider.ProviderName,
                            () => provider.GetRecipeByIdAsync(id),
                            $"provider_{provider.ProviderName}_recipe_{id}",
                            TimeSpan.FromDays(7));
                            
                        if (recipe != null)
                        {
                            // Mettre en cache global
                            await _cacheService.SetAsync(cacheKey, recipe, TimeSpan.FromDays(7));
                            return recipe;
                        }
                    }
                }

                // Sinon, essayer les fournisseurs dans l'ordre de priorité
                foreach (var providerName in _providerPriority)
                {
                    var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                    if (provider != null)
                    {
                        try
                        {
                            // Utiliser l'optimiseur de requêtes
                            var recipe = await _requestOptimizer.OptimizeRequestAsync(
                                provider.ProviderName,
                                () => provider.GetRecipeByIdAsync(id),
                                $"provider_{provider.ProviderName}_recipe_{id}",
                                TimeSpan.FromDays(7));
                                
                            if (recipe != null)
                            {
                                // Mettre en cache global
                                await _cacheService.SetAsync(cacheKey, recipe, TimeSpan.FromDays(7));
                                return recipe;
                            }
                        }
                        catch (ProviderFallbackException)
                        {
                            // Continuer avec le prochain fournisseur
                            continue;
                        }
                        catch (QuotaExceededException)
                        {
                            // Continuer avec le prochain fournisseur
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log de l'erreur (à implémenter avec un vrai logger)
                Console.WriteLine($"Erreur lors de la récupération de la recette {id}: {ex.Message}");
            }

            // Si aucun fournisseur n'a trouvé la recette
            return null;
        }

        /// <summary>
        /// Récupère une liste de recettes aléatoires.
        /// </summary>
        /// <param name="count">Nombre de recettes à récupérer</param>
        /// <returns>Liste des recettes aléatoires</returns>
        public async Task<List<Recipe>> GetRandomRecipesAsync(int count)
        {
            // Les recettes aléatoires ne sont pas mises en cache entre les appels
            // mais les appels individuels aux APIs sont optimisés
            
            // Répartir le nombre de recettes entre les fournisseurs disponibles
            var results = new List<Recipe>();
            var providersCount = _providers.Count;
            
            if (providersCount == 0)
                return results;
                
            // Calculer combien de recettes demander à chaque fournisseur
            var recipesPerProvider = count / providersCount;
            var remainder = count % providersCount;
            
            var tasks = new List<Task<List<Recipe>>>();
            
            for (int i = 0; i < providersCount; i++)
            {
                var provider = _providers[i];
                var providerCount = recipesPerProvider + (i < remainder ? 1 : 0);
                
                if (providerCount > 0)
                {
                    var task = _requestOptimizer.OptimizeCollectionRequestAsync(
                        provider.ProviderName,
                        () => provider.GetRandomRecipesAsync(providerCount));
                        
                    tasks.Add(task);
                }
            }
            
            // Attendre que toutes les requêtes soient terminées
            await Task.WhenAll(tasks);
            
            // Consolider les résultats
            foreach (var task in tasks)
            {
                var providerResults = await task;
                if (providerResults != null && providerResults.Any())
                {
                    results.AddRange(providerResults);
                }
            }

            // S'assurer qu'on n'a pas plus de recettes que demandé
            return results.Take(count).ToList();
        }
    }
}
