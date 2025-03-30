using Newtonsoft.Json;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Partie du service MealPlanningDatabaseService dédiée à la génération de suggestions.
    /// </summary>
    public partial class MealPlanningDatabaseService
    {
        /// <inheritdoc />
        public async Task<MealPlan> GenerateSuggestedMealPlanAsync(DateTime startDate, DateTime endDate, Dictionary<string, object>? preferences = null)
        {
            // Créer un nouveau plan
            var mealPlan = new MealPlan
            {
                Name = $"Plan suggéré: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
                StartDate = startDate,
                EndDate = endDate,
                IsActive = false,
                Meals = new List<PlannedMeal>()
            };
            
            // Nombre de jours dans le plan
            int days = (int)(endDate - startDate).TotalDays + 1;
            
            try
            {
                // Pour chaque jour, générer des repas (petit-déjeuner, déjeuner, dîner)
                for (int i = 0; i < days; i++)
                {
                    var currentDate = startDate.AddDays(i);
                    
                    // Récupérer des recettes aléatoires pour chaque type de repas
                    // Petit déjeuner
                    var breakfastRecipe = await GetRandomRecipeForMealTypeAsync("Breakfast", preferences);
                    if (breakfastRecipe != null)
                    {
                        mealPlan.Meals.Add(new PlannedMeal
                        {
                            Recipe = breakfastRecipe,
                            RecipeId = breakfastRecipe.Id,
                            Date = currentDate,
                            Type = MealType.Breakfast,
                            Servings = 2,
                            IncludeInShoppingList = true
                        });
                    }
                    
                    // Déjeuner
                    var lunchRecipe = await GetRandomRecipeForMealTypeAsync("Lunch", preferences);
                    if (lunchRecipe != null)
                    {
                        mealPlan.Meals.Add(new PlannedMeal
                        {
                            Recipe = lunchRecipe,
                            RecipeId = lunchRecipe.Id,
                            Date = currentDate,
                            Type = MealType.Lunch,
                            Servings = 2,
                            IncludeInShoppingList = true
                        });
                    }
                    
                    // Dîner
                    var dinnerRecipe = await GetRandomRecipeForMealTypeAsync("Dinner", preferences);
                    if (dinnerRecipe != null)
                    {
                        mealPlan.Meals.Add(new PlannedMeal
                        {
                            Recipe = dinnerRecipe,
                            RecipeId = dinnerRecipe.Id,
                            Date = currentDate,
                            Type = MealType.Dinner,
                            Servings = 2,
                            IncludeInShoppingList = true
                        });
                    }
                }
                
                return mealPlan;
            }
            catch (Exception)
            {
                // En cas d'erreur, retourner un plan vide mais valide
                return mealPlan;
            }
        }
        
        /// <summary>
        /// Récupère une recette aléatoire pour un type de repas spécifique.
        /// </summary>
        private async Task<Recipe?> GetRandomRecipeForMealTypeAsync(string mealType, Dictionary<string, object>? preferences = null)
        {
            try
            {
                // Essayer d'abord de récupérer des favoris adaptés
                var favorites = await _favoritesService.GetAllFavoritesAsync();
                var suitableFavorites = favorites.Where(r => 
                    IsSuitableForMealType(r, mealType) && 
                    MatchesPreferences(r, preferences)).ToList();
                
                if (suitableFavorites.Any())
                {
                    // Choisir un favori aléatoire
                    var random = new Random();
                    return suitableFavorites[random.Next(suitableFavorites.Count)];
                }
                
                // Si pas de favoris adaptés, rechercher de nouvelles recettes
                var searchTerm = GetSearchTermForMealType(mealType);
                var recipes = await _mealDbService.SearchRecipesByNameAsync(searchTerm);
                
                var suitableRecipes = recipes.Where(r => MatchesPreferences(r, preferences)).ToList();
                
                if (suitableRecipes.Any())
                {
                    var random = new Random();
                    return suitableRecipes[random.Next(suitableRecipes.Count)];
                }
                
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        /// <summary>
        /// Détermine si une recette est adaptée à un type de repas spécifique.
        /// </summary>
        private bool IsSuitableForMealType(Recipe recipe, string mealType)
        {
            if (string.IsNullOrEmpty(recipe.Category))
                return false;
                
            return mealType.ToLower() switch
            {
                "breakfast" => recipe.Category.ToLower().Contains("breakfast") || 
                               recipe.Tags.Any(t => t.ToLower().Contains("breakfast")),
                               
                "lunch" => !recipe.Category.ToLower().Contains("breakfast") && 
                           !recipe.Category.ToLower().Contains("dessert"),
                           
                "dinner" => !recipe.Category.ToLower().Contains("breakfast") && 
                            !recipe.Category.ToLower().Contains("dessert"),
                            
                _ => true
            };
        }
        
        /// <summary>
        /// Retourne un terme de recherche adapté au type de repas.
        /// </summary>
        private string GetSearchTermForMealType(string mealType)
        {
            return mealType.ToLower() switch
            {
                "breakfast" => "breakfast",
                "lunch" => "lunch",
                "dinner" => "dinner",
                "snack" => "snack",
                _ => string.Empty
            };
        }
        
        /// <summary>
        /// Vérifie si une recette correspond aux préférences spécifiées.
        /// </summary>
        private bool MatchesPreferences(Recipe recipe, Dictionary<string, object>? preferences)
        {
            if (preferences == null || !preferences.Any())
                return true;
                
            // Exemple de vérification des préférences
            if (preferences.TryGetValue("vegetarian", out var vegetarianObj) && 
                vegetarianObj is bool vegetarian && vegetarian)
            {
                // Vérifier si la recette est végétarienne
                var nonVegetarianIngredients = new[] { "meat", "chicken", "beef", "pork", "fish", "seafood" };
                if (recipe.Ingredients.Any(i => nonVegetarianIngredients.Any(nv => 
                    i.Name.ToLower().Contains(nv.ToLower()))))
                {
                    return false;
                }
            }
            
            if (preferences.TryGetValue("category", out var categoryObj) && 
                categoryObj is string category && !string.IsNullOrEmpty(category))
            {
                if (!recipe.Category.ToLower().Contains(category.ToLower()))
                {
                    return false;
                }
            }
            
            if (preferences.TryGetValue("area", out var areaObj) && 
                areaObj is string area && !string.IsNullOrEmpty(area))
            {
                if (!recipe.Area.ToLower().Contains(area.ToLower()))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
