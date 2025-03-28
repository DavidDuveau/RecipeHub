using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RecipeHub.Core.Models;

namespace RecipeHub.Services.API.Spoonacular
{
    /// <summary>
    /// Partie du fournisseur Spoonacular gérant les méthodes supplémentaires liées aux ingrédients.
    /// </summary>
    public partial class SpoonacularProvider
    {
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
            var request = new RestSharp.RestRequest("recipes/complexSearch");
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

                var searchResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.SpoonacularSearchResult>(response.Content);
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

        /// <summary>
        /// Récupère la liste des ingrédients disponibles.
        /// Réimplémentation de SpoonacularProvider.Ingredient.cs
        /// </summary>
        /// <returns>Liste des ingrédients</returns>
        public async Task<List<string>> GetIngredientsAsync()
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<string>();
                
            // Clé de cache pour les ingrédients
            string cacheKey = "spoonacular_ingredients";
            
            // Vérifier si les ingrédients sont dans le cache
            var cachedIngredients = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cachedIngredients != null)
                return cachedIngredients;

            // Pour économiser des appels API, nous allons utiliser une liste prédéfinie
            // des ingrédients courants plutôt que de faire un appel API
            // Dans une application réelle, on pourrait faire un appel à l'API Spoonacular
            // qui fournit une liste complète d'ingrédients
            var ingredients = new List<string>
            {
                "chicken", "beef", "pork", "fish", "shrimp", "tofu", "eggs", "milk", "cheese",
                "butter", "flour", "sugar", "salt", "pepper", "olive oil", "garlic", "onion",
                "tomato", "potato", "carrot", "broccoli", "spinach", "rice", "pasta", "bread",
                "apple", "banana", "orange", "lemon", "strawberry", "blueberry", "chocolate",
                "vanilla", "cinnamon", "mint", "basil", "oregano", "thyme", "rosemary"
            };
            
            // Sauvegarder les ingrédients dans le cache
            await _cacheService.SetAsync(cacheKey, ingredients, IngredientCacheDuration);
            
            return ingredients;
        }

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes contenant l'ingrédient</returns>
        public async Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient, int limit = 20)
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<Recipe>();
                
            if (string.IsNullOrWhiteSpace(ingredient))
                return new List<Recipe>();
                
            // Clé de cache pour cet ingrédient
            string normalizedIngredient = ingredient.Trim().ToLowerInvariant();
            string cacheKey = $"spoonacular_ingredient_{normalizedIngredient}_{limit}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<int>>(cacheKey);
            if (cachedResults != null)
            {
                // Récupérer les recettes à partir de leurs identifiants
                var ingredientRecipes = new List<Recipe>();
                foreach (var id in cachedResults)
                {
                    var recipe = await GetRecipeByIdAsync(id);
                    if (recipe != null)
                        ingredientRecipes.Add(recipe);
                }
                return ingredientRecipes;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestSharp.RestRequest("recipes/findByIngredients");
            request.AddQueryParameter("apiKey", _apiKey);
            request.AddQueryParameter("ingredients", ingredient);
            request.AddQueryParameter("number", limit.ToString());
            
            try
            {
                var response = await _client.ExecuteAsync(request);
                await IncrementApiUsageAsync(); // Comptabiliser l'appel API
                
                if (!response.IsSuccessful)
                    return new List<Recipe>();

                var ingredientResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Models.SpoonacularIngredientResult>>(response.Content);
                if (ingredientResults == null || !ingredientResults.Any())
                    return new List<Recipe>();
                    
                // Pour chaque résultat, récupérer les détails complets de la recette
                var recipes = new List<Recipe>();
                var recipeIds = new List<int>();
                
                foreach (var result in ingredientResults)
                {
                    recipeIds.Add(result.Id);
                    var recipeDetails = await GetRecipeByIdAsync(result.Id);
                    if (recipeDetails != null)
                        recipes.Add(recipeDetails);
                }
                
                // Sauvegarder les identifiants des recettes dans le cache
                await _cacheService.SetAsync(cacheKey, recipeIds, FilterCacheDuration);
                
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
