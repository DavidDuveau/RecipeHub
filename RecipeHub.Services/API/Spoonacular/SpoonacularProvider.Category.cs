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
    /// Partie du fournisseur Spoonacular gérant les méthodes liées aux catégories.
    /// </summary>
    public partial class SpoonacularProvider
    {
        /// <summary>
        /// Récupère toutes les catégories disponibles.
        /// </summary>
        /// <returns>Liste des catégories</returns>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<Category>();
                
            // Clé de cache pour les catégories
            string cacheKey = "spoonacular_categories";
            
            // Vérifier si les catégories sont dans le cache
            var cachedCategories = await _cacheService.GetAsync<List<Category>>(cacheKey);
            if (cachedCategories != null)
                return cachedCategories;

            // Si pas dans le cache, faire la requête à l'API
            // Spoonacular n'a pas d'endpoint spécifique pour les catégories,
            // nous allons utiliser les types de plats (meal types)
            var request = new RestRequest("recipes/complexSearch");
            request.AddQueryParameter("apiKey", _apiKey);
            request.AddQueryParameter("type", "main course,side dish,dessert,appetizer,salad,bread,breakfast,soup,beverage,sauce,drink");
            request.AddQueryParameter("number", "1"); // Juste besoin des types, pas des recettes
            
            try
            {
                var response = await _client.ExecuteAsync(request);
                await IncrementApiUsageAsync(); // Comptabiliser l'appel API
                
                if (!response.IsSuccessful)
                    return new List<Category>();

                // Créer les catégories à partir d'une liste prédéfinie
                // car Spoonacular ne fournit pas directement cette information
                var categories = new List<Category>
                {
                    new Category { Id = 1, Name = "Main Course", Description = "Main dishes", Thumbnail = "https://spoonacular.com/recipeImages/main-course.jpg" },
                    new Category { Id = 2, Name = "Side Dish", Description = "Side dishes", Thumbnail = "https://spoonacular.com/recipeImages/side-dish.jpg" },
                    new Category { Id = 3, Name = "Dessert", Description = "Sweet dishes served after the main course", Thumbnail = "https://spoonacular.com/recipeImages/dessert.jpg" },
                    new Category { Id = 4, Name = "Appetizer", Description = "Small dishes served before the main course", Thumbnail = "https://spoonacular.com/recipeImages/appetizer.jpg" },
                    new Category { Id = 5, Name = "Salad", Description = "Dishes with mixed vegetables, often served cold", Thumbnail = "https://spoonacular.com/recipeImages/salad.jpg" },
                    new Category { Id = 6, Name = "Bread", Description = "Bread and bread-based dishes", Thumbnail = "https://spoonacular.com/recipeImages/bread.jpg" },
                    new Category { Id = 7, Name = "Breakfast", Description = "Morning meals", Thumbnail = "https://spoonacular.com/recipeImages/breakfast.jpg" },
                    new Category { Id = 8, Name = "Soup", Description = "Liquid food typically made by boiling ingredients", Thumbnail = "https://spoonacular.com/recipeImages/soup.jpg" },
                    new Category { Id = 9, Name = "Beverage", Description = "Drinks of various types", Thumbnail = "https://spoonacular.com/recipeImages/beverage.jpg" },
                    new Category { Id = 10, Name = "Sauce", Description = "Condiments to accompany other dishes", Thumbnail = "https://spoonacular.com/recipeImages/sauce.jpg" },
                    new Category { Id = 11, Name = "Drink", Description = "Alcoholic and non-alcoholic drinks", Thumbnail = "https://spoonacular.com/recipeImages/drink.jpg" }
                };
                
                // Sauvegarder les catégories dans le cache
                await _cacheService.SetAsync(cacheKey, categories, CategoryCacheDuration);
                
                return categories;
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner une liste vide
                return new List<Category>();
            }
        }

        /// <summary>
        /// Récupère les recettes d'une catégorie spécifique.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        /// <param name="limit">Nombre maximum de résultats à retourner</param>
        /// <returns>Liste des recettes de la catégorie</returns>
        public async Task<List<Recipe>> GetRecipesByCategoryAsync(string category, int limit = 20)
        {
            // Vérifier si le quota est dépassé
            if (await _metricsService.IsQuotaExceededAsync(PROVIDER_NAME))
                return new List<Recipe>();
                
            if (string.IsNullOrWhiteSpace(category))
                return new List<Recipe>();
                
            // Normaliser le nom de catégorie pour Spoonacular
            string normalizedCategory = NormalizeCategoryName(category);
            
            // Clé de cache pour cette catégorie
            string cacheKey = $"spoonacular_category_{normalizedCategory}_{limit}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<int>>(cacheKey);
            if (cachedResults != null)
            {
                // Récupérer les recettes à partir de leurs identifiants
                var categoryRecipes = new List<Recipe>();
                foreach (var id in cachedResults)
                {
                    var recipe = await GetRecipeByIdAsync(id);
                    if (recipe != null)
                        categoryRecipes.Add(recipe);
                }
                return categoryRecipes;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest("recipes/complexSearch");
            request.AddQueryParameter("apiKey", _apiKey);
            request.AddQueryParameter("type", normalizedCategory);
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
        
        /// <summary>
        /// Convertit un nom de catégorie en format utilisé par Spoonacular.
        /// </summary>
        /// <param name="category">Nom de catégorie à normaliser</param>
        /// <returns>Nom normalisé pour l'API Spoonacular</returns>
        private string NormalizeCategoryName(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return string.Empty;
                
            // Correspondance entre les noms de catégories courants et les types de plats de Spoonacular
            var categoryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Main Course", "main course" },
                { "Main Dish", "main course" },
                { "Side Dish", "side dish" },
                { "Side", "side dish" },
                { "Dessert", "dessert" },
                { "Sweets", "dessert" },
                { "Appetizer", "appetizer" },
                { "Starter", "appetizer" },
                { "Salad", "salad" },
                { "Bread", "bread" },
                { "Breakfast", "breakfast" },
                { "Morning Meal", "breakfast" },
                { "Brunch", "breakfast" },
                { "Soup", "soup" },
                { "Beverage", "beverage" },
                { "Drink", "drink" },
                { "Sauce", "sauce" },
                { "Condiment", "sauce" }
            };
            
            // Vérifier si la catégorie est dans notre mapping
            if (categoryMapping.TryGetValue(category, out string? mappedCategory))
                return mappedCategory;
            
            // Si pas dans le mapping, utiliser le nom original en minuscules sans espaces excessifs
            return category.Trim().ToLowerInvariant();
        }
    }
}
