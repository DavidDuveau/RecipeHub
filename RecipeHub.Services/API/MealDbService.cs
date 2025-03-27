using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Services.API
{
    /// <summary>
    /// Implémentation du service pour accéder à l'API TheMealDB avec support de cache.
    /// </summary>
    public class MealDbService : IMealDbService
    {
        private readonly RestClient _client;
        private readonly ICacheService _cacheService;
        private readonly IFavoritesService _favoritesService;
        
        private const string API_KEY = "1"; // Clé API gratuite (à remplacer par une vraie clé si nécessaire)
        private const string BASE_URL = "https://www.themealdb.com/api/json/v1/";
        
        // Durées de cache par type de données
        private static readonly TimeSpan RecipeCacheDuration = TimeSpan.FromDays(7);
        private static readonly TimeSpan CategoryCacheDuration = TimeSpan.FromDays(30);
        private static readonly TimeSpan AreaCacheDuration = TimeSpan.FromDays(30);
        private static readonly TimeSpan IngredientCacheDuration = TimeSpan.FromDays(30);
        private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromHours(24);
        private static readonly TimeSpan FilterCacheDuration = TimeSpan.FromDays(1);

        /// <summary>
        /// Constructeur du service MealDB.
        /// </summary>
        /// <param name="cacheService">Service de cache à utiliser</param>
        /// <param name="favoritesService">Service de gestion des favoris (optionnel)</param>
        public MealDbService(ICacheService cacheService, IFavoritesService favoritesService = null)
        {
            _client = new RestClient($"{BASE_URL}{API_KEY}/");
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _favoritesService = favoritesService;
        }

        /// <summary>
        /// Récupère une recette par son identifiant, en utilisant le cache si disponible.
        /// </summary>
        /// <param name="id">Identifiant de la recette</param>
        /// <returns>La recette correspondante ou null si non trouvée</returns>
        public async Task<Recipe?> GetRecipeByIdAsync(int id)
        {
            // Clé de cache pour cette recette
            string cacheKey = $"recipe_{id}";
            
            // Vérifier si la recette est dans le cache
            var cachedRecipe = await _cacheService.GetAsync<Recipe>(cacheKey);
            if (cachedRecipe != null)
            {
                // Vérifier si la recette est dans les favoris (si le service de favoris est disponible)
                if (_favoritesService != null)
                {
                    cachedRecipe.IsFavorite = await _favoritesService.IsFavoriteAsync(id);
                }
                return cachedRecipe;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest($"lookup.php?i={id}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return null;

            // Traitement de la réponse JSON
            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            if (result?.Meals?.FirstOrDefault() == null)
                return null;
                
            var recipe = ConvertToRecipe(result.Meals.First());
            
            // Vérifier si la recette est dans les favoris (si le service de favoris est disponible)
            if (_favoritesService != null)
            {
                recipe.IsFavorite = await _favoritesService.IsFavoriteAsync(id);
            }
            
            // Sauvegarder la recette dans le cache
            await _cacheService.SetAsync(cacheKey, recipe, RecipeCacheDuration);
            
            return recipe;
        }

        /// <summary>
        /// Recherche des recettes par leur nom, en utilisant le cache si disponible.
        /// </summary>
        /// <param name="name">Nom ou partie du nom à rechercher</param>
        /// <returns>Liste des recettes correspondantes</returns>
        public async Task<List<Recipe>> SearchRecipesByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Recipe>();
                
            // Clé de cache normalisée (en minuscules sans espaces excessifs)
            string normalizedName = name.Trim().ToLowerInvariant();
            string cacheKey = $"search_{normalizedName}";
            
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
            var request = new RestRequest($"search.php?s={name}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            if (result?.Meals == null)
                return new List<Recipe>();
                
            // Convertir les résultats en recettes
            var apiRecipes = result.Meals.Select(ConvertToRecipe).ToList();
            
            // Mettre à jour le statut de favori pour chaque recette
            if (_favoritesService != null)
            {
                foreach (var recipe in apiRecipes)
                {
                    recipe.IsFavorite = await _favoritesService.IsFavoriteAsync(recipe.Id);
                }
            }
            
            // Sauvegarder les identifiants des recettes dans le cache
            var recipeIds = apiRecipes.Select(r => r.Id).ToList();
            await _cacheService.SetAsync(cacheKey, recipeIds, SearchCacheDuration);
            
            // Mettre en cache chaque recette individuelle
            foreach (var recipe in apiRecipes)
            {
                await _cacheService.SetAsync($"recipe_{recipe.Id}", recipe, RecipeCacheDuration);
            }
            
            return apiRecipes;
        }

        /// <summary>
        /// Récupère une liste de recettes aléatoires.
        /// </summary>
        /// <param name="count">Nombre de recettes à récupérer</param>
        /// <returns>Liste des recettes aléatoires</returns>
        public async Task<List<Recipe>> GetRandomRecipesAsync(int count)
        {
            // Les recettes aléatoires ne sont pas mises en cache car elles doivent 
            // changer à chaque appel
            var randomRecipes = new List<Recipe>();

            // L'API gratuite ne permet de récupérer qu'une recette aléatoire à la fois
            // Nous devons donc faire plusieurs appels pour en avoir plusieurs
            for (int i = 0; i < count; i++)
            {
                var request = new RestRequest("random.php");
                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    continue;

                var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
                
                if (result?.Meals?.FirstOrDefault() != null)
                {
                    var recipe = ConvertToRecipe(result.Meals.First());
                    
                    // Vérifier si la recette est dans les favoris
                    if (_favoritesService != null)
                    {
                        recipe.IsFavorite = await _favoritesService.IsFavoriteAsync(recipe.Id);
                    }
                    
                    randomRecipes.Add(recipe);
                    
                    // On profite de l'occasion pour mettre cette recette en cache
                    await _cacheService.SetAsync($"recipe_{recipe.Id}", recipe, RecipeCacheDuration);
                }
            }

            return randomRecipes;
        }

        /// <summary>
        /// Récupère toutes les catégories disponibles, en utilisant le cache si disponible.
        /// </summary>
        /// <returns>Liste des catégories</returns>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            // Clé de cache pour les catégories
            string cacheKey = "categories";
            
            // Vérifier si les catégories sont dans le cache
            var cachedCategories = await _cacheService.GetAsync<List<Category>>(cacheKey);
            if (cachedCategories != null)
                return cachedCategories;

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest("categories.php");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Category>();

            var result = JsonConvert.DeserializeObject<MealDbCategoryResponse>(response.Content);
            if (result?.Categories == null)
                return new List<Category>();
                
            // Convertir les résultats en catégories
            var categories = result.Categories.Select(c => new Category
            {
                Id = int.Parse(c.IdCategory),
                Name = c.StrCategory,
                Description = c.StrCategoryDescription,
                Thumbnail = c.StrCategoryThumb
            }).ToList();
            
            // Sauvegarder les catégories dans le cache
            await _cacheService.SetAsync(cacheKey, categories, CategoryCacheDuration);
            
            return categories;
        }

        /// <summary>
        /// Récupère les recettes d'une catégorie spécifique, en utilisant le cache si disponible.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        /// <returns>Liste des recettes de la catégorie</returns>
        public async Task<List<Recipe>> GetRecipesByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<Recipe>();
                
            // Clé de cache pour cette catégorie
            string normalizedCategory = category.Trim().ToLowerInvariant();
            string cacheKey = $"category_{normalizedCategory}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<int>>(cacheKey);
            if (cachedResults != null)
            {
                // Récupérer les recettes à partir de leurs identifiants
                var catRecipes = new List<Recipe>();
                foreach (var id in cachedResults)
                {
                    var recipe = await GetRecipeByIdAsync(id);
                    if (recipe != null)
                        catRecipes.Add(recipe);
                }
                return catRecipes;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest($"filter.php?c={category}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            // L'API renvoie des recettes partielles avec cette requête, 
            // il nous faut donc récupérer les détails de chaque recette ensuite
            if (result?.Meals == null)
                return new List<Recipe>();

            // Extraire les identifiants et les mettre en cache
            var recipeIds = new List<int>();
            var categoryResults = new List<Recipe>();
            
            foreach (var mealSummary in result.Meals)
            {
                if (int.TryParse(mealSummary.IdMeal, out int id))
                {
                    recipeIds.Add(id);
                    var recipeDetails = await GetRecipeByIdAsync(id);
                    if (recipeDetails != null)
                        categoryResults.Add(recipeDetails);
                }
            }
            
            // Sauvegarder les identifiants des recettes dans le cache
            await _cacheService.SetAsync(cacheKey, recipeIds, FilterCacheDuration);
            
            return categoryResults;
        }

        /// <summary>
        /// Récupère les régions (aires) culinaires disponibles, en utilisant le cache si disponible.
        /// </summary>
        /// <returns>Liste des régions</returns>
        public async Task<List<string>> GetAreasAsync()
        {
            // Clé de cache pour les régions
            string cacheKey = "areas";
            
            // Vérifier si les régions sont dans le cache
            var cachedAreas = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cachedAreas != null)
                return cachedAreas;

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest("list.php?a=list");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<string>();

            var result = JsonConvert.DeserializeObject<MealDbAreaResponse>(response.Content);
            if (result?.Meals == null)
                return new List<string>();
                
            // Extraire les noms des régions
            var areas = result.Meals.Select(a => a.StrArea).ToList();
            
            // Sauvegarder les régions dans le cache
            await _cacheService.SetAsync(cacheKey, areas, AreaCacheDuration);
            
            return areas;
        }

        /// <summary>
        /// Récupère les recettes d'une région spécifique, en utilisant le cache si disponible.
        /// </summary>
        /// <param name="area">Nom de la région</param>
        /// <returns>Liste des recettes de la région</returns>
        public async Task<List<Recipe>> GetRecipesByAreaAsync(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
                return new List<Recipe>();
                
            // Clé de cache pour cette région
            string normalizedArea = area.Trim().ToLowerInvariant();
            string cacheKey = $"area_{normalizedArea}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<int>>(cacheKey);
            if (cachedResults != null)
            {
                // Récupérer les recettes à partir de leurs identifiants
                var regionRecipes = new List<Recipe>();
                foreach (var id in cachedResults)
                {
                    var recipe = await GetRecipeByIdAsync(id);
                    if (recipe != null)
                        regionRecipes.Add(recipe);
                }
                return regionRecipes;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest($"filter.php?a={area}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            // Même logique que pour les catégories
            if (result?.Meals == null)
                return new List<Recipe>();

            // Extraire les identifiants et les mettre en cache
            var recipeIds = new List<int>();
            var areaResults = new List<Recipe>();
            
            foreach (var mealSummary in result.Meals)
            {
                if (int.TryParse(mealSummary.IdMeal, out int id))
                {
                    recipeIds.Add(id);
                    var recipeDetails = await GetRecipeByIdAsync(id);
                    if (recipeDetails != null)
                        areaResults.Add(recipeDetails);
                }
            }
            
            // Sauvegarder les identifiants des recettes dans le cache
            await _cacheService.SetAsync(cacheKey, recipeIds, FilterCacheDuration);
            
            return areaResults;
        }

        /// <summary>
        /// Récupère la liste des ingrédients disponibles, en utilisant le cache si disponible.
        /// </summary>
        /// <returns>Liste des ingrédients</returns>
        public async Task<List<string>> GetIngredientsAsync()
        {
            // Clé de cache pour les ingrédients
            string cacheKey = "ingredients";
            
            // Vérifier si les ingrédients sont dans le cache
            var cachedIngredients = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (cachedIngredients != null)
                return cachedIngredients;

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest("list.php?i=list");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<string>();

            var result = JsonConvert.DeserializeObject<MealDbIngredientResponse>(response.Content);
            if (result?.Meals == null)
                return new List<string>();
                
            // Extraire les noms des ingrédients
            var ingredients = result.Meals.Select(i => i.StrIngredient).ToList();
            
            // Sauvegarder les ingrédients dans le cache
            await _cacheService.SetAsync(cacheKey, ingredients, IngredientCacheDuration);
            
            return ingredients;
        }

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique, en utilisant le cache si disponible.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <returns>Liste des recettes contenant l'ingrédient</returns>
        public async Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient)
        {
            if (string.IsNullOrWhiteSpace(ingredient))
                return new List<Recipe>();
                
            // Clé de cache pour cet ingrédient
            string normalizedIngredient = ingredient.Trim().ToLowerInvariant();
            string cacheKey = $"ingredient_{normalizedIngredient}";
            
            // Vérifier si les résultats sont dans le cache
            var cachedResults = await _cacheService.GetAsync<List<int>>(cacheKey);
            if (cachedResults != null)
            {
                // Récupérer les recettes à partir de leurs identifiants
                var ingRecipes = new List<Recipe>();
                foreach (var id in cachedResults)
                {
                    var recipe = await GetRecipeByIdAsync(id);
                    if (recipe != null)
                        ingRecipes.Add(recipe);
                }
                return ingRecipes;
            }

            // Si pas dans le cache, faire la requête à l'API
            var request = new RestRequest($"filter.php?i={ingredient}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            // Même logique que pour les catégories et régions
            if (result?.Meals == null)
                return new List<Recipe>();

            // Extraire les identifiants et les mettre en cache
            var recipeIds = new List<int>();
            var ingredientResults = new List<Recipe>();
            
            foreach (var mealSummary in result.Meals)
            {
                if (int.TryParse(mealSummary.IdMeal, out int id))
                {
                    recipeIds.Add(id);
                    var recipeDetails = await GetRecipeByIdAsync(id);
                    if (recipeDetails != null)
                        ingredientResults.Add(recipeDetails);
                }
            }
            
            // Sauvegarder les identifiants des recettes dans le cache
            await _cacheService.SetAsync(cacheKey, recipeIds, FilterCacheDuration);
            
            return ingredientResults;
        }

        /// <summary>
        /// Convertit un objet MealDbMeal en objet Recipe.
        /// </summary>
        /// <param name="meal">L'objet MealDbMeal à convertir</param>
        /// <returns>L'objet Recipe correspondant</returns>
        private Recipe ConvertToRecipe(MealDbMeal meal)
        {
            var recipe = new Recipe
            {
                Id = int.Parse(meal.IdMeal),
                Name = meal.StrMeal,
                Category = meal.StrCategory ?? string.Empty,
                Area = meal.StrArea ?? string.Empty,
                Instructions = meal.StrInstructions ?? string.Empty,
                Thumbnail = meal.StrMealThumb ?? string.Empty,
                VideoUrl = meal.StrYoutube,
                Tags = !string.IsNullOrEmpty(meal.StrTags) 
                    ? meal.StrTags.Split(',').ToList() 
                    : new List<string>()
            };

            // Extraction des ingrédients et mesures
            var ingredients = new List<Ingredient>();
            
            // TheMealDB stocke les ingrédients et mesures dans des propriétés numérotées
            for (int i = 1; i <= 20; i++)
            {
                var ingredientProperty = typeof(MealDbMeal).GetProperty($"StrIngredient{i}");
                var measureProperty = typeof(MealDbMeal).GetProperty($"StrMeasure{i}");
                
                if (ingredientProperty != null && measureProperty != null)
                {
                    var ingredientValue = ingredientProperty.GetValue(meal) as string;
                    var measureValue = measureProperty.GetValue(meal) as string;
                    
                    if (!string.IsNullOrWhiteSpace(ingredientValue))
                    {
                        ingredients.Add(new Ingredient(
                            ingredientValue,
                            !string.IsNullOrWhiteSpace(measureValue) ? measureValue : string.Empty
                        ));
                    }
                }
            }
            
            recipe.Ingredients = ingredients;
            
            return recipe;
        }

        #region Classes pour la désérialisation JSON

        /// <summary>
        /// Classe pour la désérialisation des réponses de l'API contenant des recettes.
        /// </summary>
        private class MealDbRecipeResponse
        {
            public List<MealDbMeal> Meals { get; set; }
        }

        /// <summary>
        /// Classe pour la désérialisation des réponses de l'API contenant des catégories.
        /// </summary>
        private class MealDbCategoryResponse
        {
            public List<MealDbCategory> Categories { get; set; }
        }

        /// <summary>
        /// Classe pour la désérialisation des réponses de l'API contenant des régions.
        /// </summary>
        private class MealDbAreaResponse
        {
            public List<MealDbArea> Meals { get; set; }
        }

        /// <summary>
        /// Classe pour la désérialisation des réponses de l'API contenant des ingrédients.
        /// </summary>
        private class MealDbIngredientResponse
        {
            public List<MealDbIngredientList> Meals { get; set; }
        }

        /// <summary>
        /// Classe représentant une recette dans l'API TheMealDB.
        /// </summary>
        private class MealDbMeal
        {
            public string IdMeal { get; set; }
            public string StrMeal { get; set; }
            public string StrCategory { get; set; }
            public string StrArea { get; set; }
            public string StrInstructions { get; set; }
            public string StrMealThumb { get; set; }
            public string StrTags { get; set; }
            public string StrYoutube { get; set; }
            
            // Ingrédients et mesures (propriétés numérotées de 1 à 20)
            public string StrIngredient1 { get; set; }
            public string StrIngredient2 { get; set; }
            public string StrIngredient3 { get; set; }
            public string StrIngredient4 { get; set; }
            public string StrIngredient5 { get; set; }
            public string StrIngredient6 { get; set; }
            public string StrIngredient7 { get; set; }
            public string StrIngredient8 { get; set; }
            public string StrIngredient9 { get; set; }
            public string StrIngredient10 { get; set; }
            public string StrIngredient11 { get; set; }
            public string StrIngredient12 { get; set; }
            public string StrIngredient13 { get; set; }
            public string StrIngredient14 { get; set; }
            public string StrIngredient15 { get; set; }
            public string StrIngredient16 { get; set; }
            public string StrIngredient17 { get; set; }
            public string StrIngredient18 { get; set; }
            public string StrIngredient19 { get; set; }
            public string StrIngredient20 { get; set; }
            
            public string StrMeasure1 { get; set; }
            public string StrMeasure2 { get; set; }
            public string StrMeasure3 { get; set; }
            public string StrMeasure4 { get; set; }
            public string StrMeasure5 { get; set; }
            public string StrMeasure6 { get; set; }
            public string StrMeasure7 { get; set; }
            public string StrMeasure8 { get; set; }
            public string StrMeasure9 { get; set; }
            public string StrMeasure10 { get; set; }
            public string StrMeasure11 { get; set; }
            public string StrMeasure12 { get; set; }
            public string StrMeasure13 { get; set; }
            public string StrMeasure14 { get; set; }
            public string StrMeasure15 { get; set; }
            public string StrMeasure16 { get; set; }
            public string StrMeasure17 { get; set; }
            public string StrMeasure18 { get; set; }
            public string StrMeasure19 { get; set; }
            public string StrMeasure20 { get; set; }
        }

        /// <summary>
        /// Classe représentant une catégorie dans l'API TheMealDB.
        /// </summary>
        private class MealDbCategory
        {
            public string IdCategory { get; set; }
            public string StrCategory { get; set; }
            public string StrCategoryThumb { get; set; }
            public string StrCategoryDescription { get; set; }
        }

        /// <summary>
        /// Classe représentant une région culinaire dans l'API TheMealDB.
        /// </summary>
        private class MealDbArea
        {
            public string StrArea { get; set; }
        }

        /// <summary>
        /// Classe représentant un ingrédient dans l'API TheMealDB.
        /// </summary>
        private class MealDbIngredientList
        {
            public string IdIngredient { get; set; }
            public string StrIngredient { get; set; }
            public string StrDescription { get; set; }
            public string StrType { get; set; }
        }

        #endregion
    }
}
