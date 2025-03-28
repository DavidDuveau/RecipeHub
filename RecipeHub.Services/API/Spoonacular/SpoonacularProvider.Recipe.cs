using Newtonsoft.Json;
using RecipeHub.Core.Models;
using RecipeHub.Services.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Services.API.Spoonacular
{
    /// <summary>
    /// Partie du fournisseur Spoonacular gérant les méthodes liées aux recettes.
    /// </summary>
    public partial class SpoonacularProvider
    {
        /// <summary>
        /// Récupère une recette par son identifiant.
        /// </summary>
        /// <param name="id">Identifiant de la recette</param>
        /// <returns>La recette correspondante ou null si non trouvée</returns>
        public async Task<Recipe?> GetRecipeByIdAsync(int id)
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return null;
                
            // Clé de cache pour cette recette
            string cacheKey = $"spoonacular_recipe_{id}";
            
            // Vérifier si la recette est dans le cache
            var cachedRecipe = await _cacheService.GetAsync<Recipe>(cacheKey);
            if (cachedRecipe != null)
            {
                // Vérifier si la recette est dans les favoris
                if (_favoritesService != null)
                {
                    cachedRecipe.IsFavorite = await _favoritesService.IsFavoriteAsync(id);
                }
                return cachedRecipe;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest($"recipes/{id}/information");
            request.AddQueryParameter("apiKey", _apiKey);
            request.AddQueryParameter("includeNutrition", "false");
            
            try
            {
                var response = await _client.ExecuteAsync(request);
                await IncrementApiUsageAsync(); // Comptabiliser l'appel API
                
                if (!response.IsSuccessful)
                    return null;

                var spoonacularRecipe = JsonConvert.DeserializeObject<SpoonacularRecipe>(response.Content);
                if (spoonacularRecipe == null)
                    return null;
                    
                var recipe = ConvertToRecipe(spoonacularRecipe);
                
                // Vérifier si la recette est dans les favoris
                if (_favoritesService != null)
                {
                    recipe.IsFavorite = await _favoritesService.IsFavoriteAsync(id);
                }
                
                // Sauvegarder la recette dans le cache
                await _cacheService.SetAsync(cacheKey, recipe, RecipeCacheDuration);
                
                return recipe;
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner null
                return null;
            }
        }
        
        /// <summary>
        /// Recherche des recettes par leur nom.
        /// </summary>
        /// <param name="name">Nom ou partie du nom à rechercher</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes correspondantes</returns>
        public async Task<List<Recipe>> SearchRecipesByNameAsync(string name, int limit = 10)
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<Recipe>();
                
            if (string.IsNullOrWhiteSpace(name))
                return new List<Recipe>();
                
            // Clé de cache normalisée
            string normalizedName = name.Trim().ToLowerInvariant();
            string cacheKey = $"spoonacular_search_{normalizedName}_{limit}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<int>>(cacheKey);
            if (cachedResults != null)
            {
                // Récupérer les recettes à partir de leurs identifiants
                var searchRecipes = new List<Recipe>();
                foreach (var id in cachedResults)
                {
                    var recipe = await GetRecipeByIdAsync(id);
                    if (recipe != null)
                        searchRecipes.Add(recipe);
                }
                return searchRecipes;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest("recipes/complexSearch");
            request.AddQueryParameter("apiKey", _apiKey);
            request.AddQueryParameter("query", name);
            request.AddQueryParameter("number", limit.ToString());
            request.AddQueryParameter("addRecipeInformation", "true");
            
            try
            {
                var response = await _client.ExecuteAsync(request);
                await IncrementApiUsageAsync(); // Comptabiliser l'appel API
                
                if (!response.IsSuccessful)
                    return new List<Recipe>();

                var searchResult = JsonConvert.DeserializeObject<SpoonacularSearchResult>(response.Content);
                if (searchResult?.Results == null || !searchResult.Results.Any())
                    return new List<Recipe>();
                    
                // Convertir les résultats en recettes
                var recipes = searchResult.Results.Select(ConvertToRecipe).ToList();
                
                // Mettre à jour le statut de favori pour chaque recette
                if (_favoritesService != null)
                {
                    foreach (var recipe in recipes)
                    {
                        recipe.IsFavorite = await _favoritesService.IsFavoriteAsync(recipe.Id);
                    }
                }
                
                // Sauvegarder les identifiants des recettes dans le cache
                var recipeIds = recipes.Select(r => r.Id).ToList();
                await _cacheService.SetAsync(cacheKey, recipeIds, SearchCacheDuration);
                
                // Mettre en cache chaque recette individuelle
                foreach (var recipe in recipes)
                {
                    await _cacheService.SetAsync($"spoonacular_recipe_{recipe.Id}", recipe, RecipeCacheDuration);
                }
                
                return recipes;
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner une liste vide
                return new List<Recipe>();
            }
        }

        /// <summary>
        /// Récupère une liste de recettes aléatoires.
        /// </summary>
        /// <param name="count">Nombre de recettes à récupérer</param>
        /// <returns>Liste des recettes aléatoires</returns>
        public async Task<List<Recipe>> GetRandomRecipesAsync(int count)
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<Recipe>();
                
            // Les recettes aléatoires ne sont pas mises en cache car elles doivent
            // changer à chaque appel
            var request = new RestRequest("recipes/random");
            request.AddQueryParameter("apiKey", _apiKey);
            request.AddQueryParameter("number", count.ToString());
            
            try
            {
                var response = await _client.ExecuteAsync(request);
                await IncrementApiUsageAsync(); // Comptabiliser l'appel API
                
                if (!response.IsSuccessful)
                    return new List<Recipe>();

                var randomResult = JsonConvert.DeserializeObject<SpoonacularRandomResult>(response.Content);
                if (randomResult?.Recipes == null || !randomResult.Recipes.Any())
                    return new List<Recipe>();
                    
                // Convertir les résultats en recettes
                var recipes = randomResult.Recipes.Select(ConvertToRecipe).ToList();
                
                // Mettre à jour le statut de favori pour chaque recette
                if (_favoritesService != null)
                {
                    foreach (var recipe in recipes)
                    {
                        recipe.IsFavorite = await _favoritesService.IsFavoriteAsync(recipe.Id);
                    }
                }
                
                // Mettre en cache chaque recette individuelle
                foreach (var recipe in recipes)
                {
                    await _cacheService.SetAsync($"spoonacular_recipe_{recipe.Id}", recipe, RecipeCacheDuration);
                }
                
                return recipes;
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner une liste vide
                return new List<Recipe>();
            }
        }
    }
}
