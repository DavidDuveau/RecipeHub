using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Services.API
{
    /// <summary>
    /// Implémentation du service pour accéder à l'API TheMealDB.
    /// </summary>
    public class MealDbService : IMealDbService
    {
        private readonly RestClient _client;
        private const string API_KEY = "1"; // Clé API gratuite (à remplacer par une vraie clé si nécessaire)
        private const string BASE_URL = "https://www.themealdb.com/api/json/v1/";

        /// <summary>
        /// Constructeur du service MealDB.
        /// </summary>
        public MealDbService()
        {
            _client = new RestClient($"{BASE_URL}{API_KEY}/");
        }

        /// <summary>
        /// Récupère une recette par son identifiant.
        /// </summary>
        /// <param name="id">Identifiant de la recette</param>
        /// <returns>La recette correspondante ou null si non trouvée</returns>
        public async Task<Recipe?> GetRecipeByIdAsync(int id)
        {
            var request = new RestRequest($"lookup.php?i={id}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return null;

            // Traitement de la réponse JSON
            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            return result?.Meals?.FirstOrDefault() != null 
                ? ConvertToRecipe(result.Meals.First()) 
                : null;
        }

        /// <summary>
        /// Recherche des recettes par leur nom.
        /// </summary>
        /// <param name="name">Nom ou partie du nom à rechercher</param>
        /// <returns>Liste des recettes correspondantes</returns>
        public async Task<List<Recipe>> SearchRecipesByNameAsync(string name)
        {
            var request = new RestRequest($"search.php?s={name}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            return result?.Meals != null 
                ? result.Meals.Select(ConvertToRecipe).ToList() 
                : new List<Recipe>();
        }

        /// <summary>
        /// Récupère une liste de recettes aléatoires.
        /// </summary>
        /// <param name="count">Nombre de recettes à récupérer</param>
        /// <returns>Liste des recettes aléatoires</returns>
        public async Task<List<Recipe>> GetRandomRecipesAsync(int count)
        {
            var recipes = new List<Recipe>();

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
                    recipes.Add(ConvertToRecipe(result.Meals.First()));
                }
            }

            return recipes;
        }

        /// <summary>
        /// Récupère toutes les catégories disponibles.
        /// </summary>
        /// <returns>Liste des catégories</returns>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var request = new RestRequest("categories.php");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Category>();

            var result = JsonConvert.DeserializeObject<MealDbCategoryResponse>(response.Content);
            
            return result?.Categories != null 
                ? result.Categories.Select(c => new Category
                {
                    Id = int.Parse(c.IdCategory),
                    Name = c.StrCategory,
                    Description = c.StrCategoryDescription,
                    Thumbnail = c.StrCategoryThumb
                }).ToList() 
                : new List<Category>();
        }

        /// <summary>
        /// Récupère les recettes d'une catégorie spécifique.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        /// <returns>Liste des recettes de la catégorie</returns>
        public async Task<List<Recipe>> GetRecipesByCategoryAsync(string category)
        {
            var request = new RestRequest($"filter.php?c={category}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            // L'API renvoie des recettes partielles avec cette requête, 
            // il nous faut donc récupérer les détails de chaque recette ensuite
            if (result?.Meals == null)
                return new List<Recipe>();

            var recipes = new List<Recipe>();
            foreach (var mealSummary in result.Meals)
            {
                if (int.TryParse(mealSummary.IdMeal, out int id))
                {
                    var recipeDetails = await GetRecipeByIdAsync(id);
                    if (recipeDetails != null)
                        recipes.Add(recipeDetails);
                }
            }

            return recipes;
        }

        /// <summary>
        /// Récupère les régions (aires) culinaires disponibles.
        /// </summary>
        /// <returns>Liste des régions</returns>
        public async Task<List<string>> GetAreasAsync()
        {
            var request = new RestRequest("list.php?a=list");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<string>();

            var result = JsonConvert.DeserializeObject<MealDbAreaResponse>(response.Content);
            
            return result?.Meals != null 
                ? result.Meals.Select(a => a.StrArea).ToList() 
                : new List<string>();
        }

        /// <summary>
        /// Récupère les recettes d'une région spécifique.
        /// </summary>
        /// <param name="area">Nom de la région</param>
        /// <returns>Liste des recettes de la région</returns>
        public async Task<List<Recipe>> GetRecipesByAreaAsync(string area)
        {
            var request = new RestRequest($"filter.php?a={area}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            // Même logique que pour les catégories
            if (result?.Meals == null)
                return new List<Recipe>();

            var recipes = new List<Recipe>();
            foreach (var mealSummary in result.Meals)
            {
                if (int.TryParse(mealSummary.IdMeal, out int id))
                {
                    var recipeDetails = await GetRecipeByIdAsync(id);
                    if (recipeDetails != null)
                        recipes.Add(recipeDetails);
                }
            }

            return recipes;
        }

        /// <summary>
        /// Récupère la liste des ingrédients disponibles.
        /// </summary>
        /// <returns>Liste des ingrédients</returns>
        public async Task<List<string>> GetIngredientsAsync()
        {
            var request = new RestRequest("list.php?i=list");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<string>();

            var result = JsonConvert.DeserializeObject<MealDbIngredientResponse>(response.Content);
            
            return result?.Meals != null 
                ? result.Meals.Select(i => i.StrIngredient).ToList() 
                : new List<string>();
        }

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <returns>Liste des recettes contenant l'ingrédient</returns>
        public async Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient)
        {
            var request = new RestRequest($"filter.php?i={ingredient}");
            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return new List<Recipe>();

            var result = JsonConvert.DeserializeObject<MealDbRecipeResponse>(response.Content);
            
            // Même logique que pour les catégories et régions
            if (result?.Meals == null)
                return new List<Recipe>();

            var recipes = new List<Recipe>();
            foreach (var mealSummary in result.Meals)
            {
                if (int.TryParse(mealSummary.IdMeal, out int id))
                {
                    var recipeDetails = await GetRecipeByIdAsync(id);
                    if (recipeDetails != null)
                        recipes.Add(recipeDetails);
                }
            }

            return recipes;
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
