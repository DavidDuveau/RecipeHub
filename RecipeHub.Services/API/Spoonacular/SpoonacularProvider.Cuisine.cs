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
    /// Partie du fournisseur Spoonacular gérant les méthodes liées aux cuisines.
    /// </summary>
    public partial class SpoonacularProvider
    {
        /// <summary>
        /// Récupère les régions (cuisines) disponibles.
        /// </summary>
        /// <returns>Liste des régions</returns>
        public async Task<List<string>> GetCuisinesAsync()
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<string>();
                
            // Clé de cache pour les cuisines
            string cacheKey = "spoonacular_cuisines";
            
            // Vérifier si les cuisines sont dans le cache
            var cachedCuisines = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cachedCuisines != null)
                return cachedCuisines;

            // Liste prédéfinie des cuisines disponibles dans Spoonacular
            var cuisines = new List<string>
            {
                "African", "American", "British", "Cajun", "Caribbean", "Chinese", "Eastern European", 
                "European", "French", "German", "Greek", "Indian", "Irish", "Italian", "Japanese", 
                "Jewish", "Korean", "Latin American", "Mediterranean", "Mexican", "Middle Eastern", 
                "Nordic", "Southern", "Spanish", "Thai", "Vietnamese"
            };
            
            // Sauvegarder les cuisines dans le cache
            await _cacheService.SetAsync(cacheKey, cuisines, CuisineCacheDuration);
            
            return cuisines;
        }

        /// <summary>
        /// Récupère les recettes d'une région spécifique.
        /// </summary>
        /// <param name="cuisine">Nom de la région/cuisine</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes de la région</returns>
        public async Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisine, int limit = 20)
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<Recipe>();
                
            if (string.IsNullOrWhiteSpace(cuisine))
                return new List<Recipe>();
                
            // Clé de cache pour cette cuisine
            string normalizedCuisine = cuisine.Trim().ToLowerInvariant();
            string cacheKey = $"spoonacular_cuisine_{normalizedCuisine}_{limit}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<int>>(cacheKey);
            if (cachedResults != null)
            {
                // Récupérer les recettes à partir de leurs identifiants
                var cuisineRecipes = new List<Recipe>();
                foreach (var id in cachedResults)
                {
                    var recipe = await GetRecipeByIdAsync(id);
                    if (recipe != null)
                        cuisineRecipes.Add(recipe);
                }
                return cuisineRecipes;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest("recipes/complexSearch");
            request.AddQueryParameter("apiKey", _apiKey);
            request.AddQueryParameter("cuisine", cuisine);
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
                await _cacheService.SetAsync(cacheKey, recipeIds, FilterCacheDuration);
                
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
