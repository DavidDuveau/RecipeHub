using RecipeHub.Core.Models;
using RecipeHub.Services.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RecipeHub.Services.API.Spoonacular
{
    /// <summary>
    /// Partie du fournisseur Spoonacular contenant les méthodes de conversion entre les modèles Spoonacular et les modèles de l'application.
    /// </summary>
    public partial class SpoonacularProvider
    {
        /// <summary>
        /// Convertit un objet SpoonacularRecipe en objet Recipe (modèle de l'application).
        /// </summary>
        /// <param name="spoonacularRecipe">L'objet SpoonacularRecipe à convertir</param>
        /// <returns>L'objet Recipe correspondant</returns>
        private Recipe ConvertToRecipe(SpoonacularRecipe spoonacularRecipe)
        {
            // Créer l'objet Recipe avec les informations de base
            var recipe = new Recipe
            {
                Id = spoonacularRecipe.Id,
                Name = spoonacularRecipe.Title,
                Instructions = spoonacularRecipe.Instructions ?? string.Empty,
                Thumbnail = spoonacularRecipe.Image ?? string.Empty,
                
                // Pour la catégorie, on utilise le premier type de plat si disponible
                Category = spoonacularRecipe.DishTypes?.FirstOrDefault() ?? string.Empty,
                
                // Pour la région, on utilise la première cuisine si disponible
                Area = spoonacularRecipe.Cuisines?.FirstOrDefault() ?? string.Empty,
                
                // Pour les tags, on combine divers attributs disponibles
                Tags = new List<string>()
            };
            
            // Ajouter les cuisines comme tags
            if (spoonacularRecipe.Cuisines != null && spoonacularRecipe.Cuisines.Any())
            {
                recipe.Tags.AddRange(spoonacularRecipe.Cuisines);
            }
            
            // Ajouter les types de plats comme tags
            if (spoonacularRecipe.DishTypes != null && spoonacularRecipe.DishTypes.Any())
            {
                recipe.Tags.AddRange(spoonacularRecipe.DishTypes);
            }
            
            // Ajouter les régimes comme tags
            if (spoonacularRecipe.Diets != null && spoonacularRecipe.Diets.Any())
            {
                recipe.Tags.AddRange(spoonacularRecipe.Diets);
            }
            
            // Ajouter les occasions comme tags
            if (spoonacularRecipe.Occasions != null && spoonacularRecipe.Occasions.Any())
            {
                recipe.Tags.AddRange(spoonacularRecipe.Occasions);
            }
            
            // Ajouter d'autres attributs spécifiques comme tags
            if (spoonacularRecipe.Vegetarian)
                recipe.Tags.Add("Vegetarian");
                
            if (spoonacularRecipe.Vegan)
                recipe.Tags.Add("Vegan");
                
            if (spoonacularRecipe.GlutenFree)
                recipe.Tags.Add("Gluten-Free");
                
            if (spoonacularRecipe.DairyFree)
                recipe.Tags.Add("Dairy-Free");
                
            if (spoonacularRecipe.VeryHealthy)
                recipe.Tags.Add("Healthy");
            
            // Supprimer les doublons et trier les tags
            recipe.Tags = recipe.Tags.Distinct().OrderBy(t => t).ToList();
            
            // URL de la vidéo
            if (spoonacularRecipe.SpoonacularSourceUrl != null)
            {
                recipe.VideoUrl = spoonacularRecipe.SpoonacularSourceUrl;
            }
            
            // Convertir les ingrédients
            recipe.Ingredients = new List<Ingredient>();
            
            if (spoonacularRecipe.ExtendedIngredients != null)
            {
                foreach (var extendedIngredient in spoonacularRecipe.ExtendedIngredients)
                {
                    // Créer une mesure formatée pour l'ingrédient
                    string measure = string.Empty;
                    if (extendedIngredient.Amount > 0)
                    {
                        // Formatter le nombre (éviter les décimales inutiles)
                        string formattedAmount = extendedIngredient.Amount % 1 == 0
                            ? extendedIngredient.Amount.ToString("0")
                            : extendedIngredient.Amount.ToString("0.##");
                            
                        // Ajouter l'unité si disponible
                        measure = !string.IsNullOrEmpty(extendedIngredient.Unit)
                            ? $"{formattedAmount} {extendedIngredient.Unit}"
                            : formattedAmount;
                    }
                    
                    recipe.Ingredients.Add(new Ingredient
                    {
                        Name = extendedIngredient.Name ?? extendedIngredient.OriginalName ?? string.Empty,
                        Quantity = measure
                    });
                }
            }
            
            // Nettoyer les instructions : supprimer les balises HTML si présentes
            if (!string.IsNullOrEmpty(recipe.Instructions))
            {
                recipe.Instructions = Regex.Replace(recipe.Instructions, "<.*?>", string.Empty);
            }
            
            return recipe;
        }
    }
}
