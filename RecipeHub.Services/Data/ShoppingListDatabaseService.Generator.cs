using Newtonsoft.Json;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Partie du service ShoppingListDatabaseService dédiée à la génération de listes de courses.
    /// </summary>
    public partial class ShoppingListDatabaseService
    {
        /// <inheritdoc />
        public async Task<ShoppingList> GenerateShoppingListFromMealPlanAsync(int mealPlanId)
        {
            // Récupérer le plan de repas
            var mealPlan = await _mealPlanningService.GetMealPlanByIdAsync(mealPlanId);
            
            if (mealPlan == null)
                throw new ArgumentException($"Plan de repas avec l'ID {mealPlanId} non trouvé.", nameof(mealPlanId));
                
            // Vérifier si une liste existe déjà pour ce plan
            var existingList = await GetShoppingListByMealPlanIdAsync(mealPlanId);
            if (existingList != null)
                return existingList;
                
            // Créer une nouvelle liste de courses
            var shoppingList = new ShoppingList
            {
                Name = $"Liste pour: {mealPlan.Name}",
                MealPlanId = mealPlanId,
                IsActive = true,
                Items = new List<ShoppingItem>()
            };
            
            var ingredientGroups = new Dictionary<string, (double Quantity, string Unit, HashSet<Recipe> Recipes)>();
            
            // Regrouper les ingrédients par nom
            foreach (var meal in mealPlan.Meals.Where(m => m.IncludeInShoppingList))
            {
                if (meal.Recipe == null || !meal.Recipe.Ingredients.Any())
                    continue;
                
                foreach (var ingredient in meal.Recipe.Ingredients)
                {
                    var adjustedQuantity = ingredient.Quantity * meal.Servings;
                    
                    var key = ingredient.Name.ToLower().Trim();
                    
                    if (ingredientGroups.TryGetValue(key, out var existing))
                    {
                        // Si même unité, additionner les quantités
                        if (string.Equals(existing.Unit, ingredient.Unit, StringComparison.OrdinalIgnoreCase))
                        {
                            ingredientGroups[key] = (existing.Quantity + adjustedQuantity, existing.Unit, existing.Recipes);
                        }
                        else
                        {
                            // Si unités différentes, garder séparément (cela pourrait être amélioré pour convertir les unités)
                            key = $"{key} ({ingredient.Unit})";
                            if (ingredientGroups.TryGetValue(key, out var existingWithUnit))
                            {
                                ingredientGroups[key] = (existingWithUnit.Quantity + adjustedQuantity, ingredient.Unit, existingWithUnit.Recipes);
                            }
                            else
                            {
                                var recipes = new HashSet<Recipe> { meal.Recipe };
                                ingredientGroups[key] = (adjustedQuantity, ingredient.Unit, recipes);
                            }
                        }
                        
                        // Ajouter la recette associée
                        existing.Recipes.Add(meal.Recipe);
                    }
                    else
                    {
                        var recipes = new HashSet<Recipe> { meal.Recipe };
                        ingredientGroups[key] = (adjustedQuantity, ingredient.Unit, recipes);
                    }
                }
            }
            
            // Créer les articles de la liste de courses
            foreach (var group in ingredientGroups)
            {
                var (name, data) = group;
                var (quantity, unit, recipes) = data;
                
                var item = new ShoppingItem
                {
                    Name = CapitalizeFirstLetter(name),
                    Quantity = quantity,
                    Unit = unit,
                    Category = DetermineCategory(name),
                    IsPurchased = false,
                    AssociatedRecipes = recipes.ToList(),
                    IsManuallyAdded = false
                };
                
                shoppingList.Items.Add(item);
            }
            
            // Trier les articles par catégorie
            shoppingList.Items = shoppingList.Items.OrderBy(i => i.Category).ToList();
            
            // Enregistrer la liste de courses
            await CreateShoppingListAsync(shoppingList);
            
            return shoppingList;
        }

        /// <inheritdoc />
        public async Task<ShoppingList> GenerateShoppingListFromRecipesAsync(List<int> recipeIds, string listName = "Ma liste de courses")
        {
            if (recipeIds == null || !recipeIds.Any())
                throw new ArgumentException("La liste d'identifiants de recettes ne peut pas être vide.", nameof(recipeIds));
                
            // Créer une nouvelle liste de courses
            var shoppingList = new ShoppingList
            {
                Name = listName,
                MealPlanId = null,
                IsActive = true,
                Items = new List<ShoppingItem>()
            };
            
            var ingredientGroups = new Dictionary<string, (double Quantity, string Unit, HashSet<Recipe> Recipes)>();
            
            // Récupérer les recettes et regrouper les ingrédients
            foreach (var recipeId in recipeIds)
            {
                var recipe = await _mealDbService.GetRecipeByIdAsync(recipeId);
                if (recipe == null || !recipe.Ingredients.Any())
                    continue;
                
                foreach (var ingredient in recipe.Ingredients)
                {
                    var key = ingredient.Name.ToLower().Trim();
                    
                    if (ingredientGroups.TryGetValue(key, out var existing))
                    {
                        // Si même unité, additionner les quantités
                        if (string.Equals(existing.Unit, ingredient.Unit, StringComparison.OrdinalIgnoreCase))
                        {
                            ingredientGroups[key] = (existing.Quantity + ingredient.Quantity, existing.Unit, existing.Recipes);
                        }
                        else
                        {
                            // Si unités différentes, garder séparément
                            key = $"{key} ({ingredient.Unit})";
                            if (ingredientGroups.TryGetValue(key, out var existingWithUnit))
                            {
                                ingredientGroups[key] = (existingWithUnit.Quantity + ingredient.Quantity, ingredient.Unit, existingWithUnit.Recipes);
                            }
                            else
                            {
                                var recipes = new HashSet<Recipe> { recipe };
                                ingredientGroups[key] = (ingredient.Quantity, ingredient.Unit, recipes);
                            }
                        }
                        
                        // Ajouter la recette associée
                        existing.Recipes.Add(recipe);
                    }
                    else
                    {
                        var recipes = new HashSet<Recipe> { recipe };
                        ingredientGroups[key] = (ingredient.Quantity, ingredient.Unit, recipes);
                    }
                }
            }
            
            // Créer les articles de la liste de courses
            foreach (var group in ingredientGroups)
            {
                var (name, data) = group;
                var (quantity, unit, recipes) = data;
                
                var item = new ShoppingItem
                {
                    Name = CapitalizeFirstLetter(name),
                    Quantity = quantity,
                    Unit = unit,
                    Category = DetermineCategory(name),
                    IsPurchased = false,
                    AssociatedRecipes = recipes.ToList(),
                    IsManuallyAdded = false
                };
                
                shoppingList.Items.Add(item);
            }
            
            // Trier les articles par catégorie
            shoppingList.Items = shoppingList.Items.OrderBy(i => i.Category).ToList();
            
            // Enregistrer la liste de courses
            await CreateShoppingListAsync(shoppingList);
            
            return shoppingList;
        }

        /// <inheritdoc />
        public async Task<bool> OptimizeShoppingListAsync(int listId)
        {
            var shoppingList = await GetShoppingListByIdAsync(listId);
            if (shoppingList == null)
                return false;
                
            var originalItemCount = shoppingList.Items.Count;
            var ingredientGroups = new Dictionary<string, (double Quantity, string Unit, List<Recipe> Recipes, bool IsPurchased, string Notes)>();
            
            // Regrouper les ingrédients similaires
            foreach (var item in shoppingList.Items)
            {
                // Ne pas fusionner les articles déjà achetés
                if (item.IsPurchased)
                    continue;
                    
                var key = item.Name.ToLower().Trim();
                
                if (ingredientGroups.TryGetValue(key, out var existing))
                {
                    // Si même unité, additionner les quantités
                    if (string.Equals(existing.Unit, item.Unit, StringComparison.OrdinalIgnoreCase))
                    {
                        // Fusionner les recettes associées
                        var combinedRecipes = new List<Recipe>(existing.Recipes);
                        combinedRecipes.AddRange(item.AssociatedRecipes.Where(r => !existing.Recipes.Any(er => er.Id == r.Id)));
                        
                        // Combiner les notes
                        var combinedNotes = string.IsNullOrEmpty(existing.Notes) 
                            ? item.Notes 
                            : string.IsNullOrEmpty(item.Notes) 
                                ? existing.Notes 
                                : $"{existing.Notes}; {item.Notes}";
                                
                        ingredientGroups[key] = (existing.Quantity + item.Quantity, existing.Unit, combinedRecipes, 
                                               existing.IsPurchased || item.IsPurchased, combinedNotes);
                    }
                }
                else
                {
                    ingredientGroups[key] = (item.Quantity, item.Unit, new List<Recipe>(item.AssociatedRecipes), 
                                           item.IsPurchased, item.Notes);
                }
            }
            
            // Si aucune optimisation n'est possible
            if (ingredientGroups.Count >= originalItemCount)
                return false;
                
            // Créer une liste optimisée
            shoppingList.Items.Clear();
            
            foreach (var group in ingredientGroups)
            {
                var (name, data) = group;
                var (quantity, unit, recipes, isPurchased, notes) = data;
                
                var item = new ShoppingItem
                {
                    Name = CapitalizeFirstLetter(name),
                    Quantity = quantity,
                    Unit = unit,
                    Category = DetermineCategory(name),
                    IsPurchased = isPurchased,
                    AssociatedRecipes = recipes,
                    Notes = notes,
                    IsManuallyAdded = false,
                    ShoppingListId = listId
                };
                
                shoppingList.Items.Add(item);
            }
            
            // Conserver les articles achetés tels quels
            shoppingList.Items = shoppingList.Items.OrderBy(i => i.Category).ToList();
            
            // Mettre à jour la liste de courses
            return await UpdateShoppingListAsync(shoppingList);
        }

        /// <summary>
        /// Détermine la catégorie d'un ingrédient en fonction de son nom.
        /// </summary>
        private ShoppingCategory DetermineCategory(string ingredientName)
        {
            var name = ingredientName.ToLower();

            // Fruits et légumes
            if (name.Contains("tomate") || name.Contains("carotte") || name.Contains("pomme") || 
                name.Contains("banane") || name.Contains("orange") || name.Contains("salade") || 
                name.Contains("légume") || name.Contains("fruit") || name.Contains("oignon") || 
                name.Contains("pomme de terre") || name.Contains("patate") || name.Contains("ail"))
            {
                return ShoppingCategory.Produce;
            }

            // Produits laitiers
            if (name.Contains("lait") || name.Contains("fromage") || name.Contains("beurre") || 
                name.Contains("crème") || name.Contains("yaourt") || name.Contains("yogourt") || 
                name.Contains("margarine"))
            {
                return ShoppingCategory.Dairy;
            }

            // Viandes
            if (name.Contains("boeuf") || name.Contains("poulet") || name.Contains("porc") || 
                name.Contains("steak") || name.Contains("côtelette") || name.Contains("viande"))
            {
                return ShoppingCategory.Meat;
            }

            // Poissons et fruits de mer
            if (name.Contains("poisson") || name.Contains("saumon") || name.Contains("thon") || 
                name.Contains("crevette") || name.Contains("moule") || name.Contains("fruits de mer"))
            {
                return ShoppingCategory.Seafood;
            }

            // Boulangerie
            if (name.Contains("pain") || name.Contains("baguette") || name.Contains("brioche") || 
                name.Contains("croissant"))
            {
                return ShoppingCategory.Bakery;
            }

            // Garde-manger
            if (name.Contains("farine") || name.Contains("riz") || name.Contains("pâte") || 
                name.Contains("huile") || name.Contains("sucre") || name.Contains("sel") || 
                name.Contains("épice") || name.Contains("conserve") || name.Contains("sauce"))
            {
                return ShoppingCategory.Pantry;
            }

            // Surgelés
            if (name.Contains("surgelé") || name.Contains("congelé") || name.Contains("glacé"))
            {
                return ShoppingCategory.Frozen;
            }

            // Boissons
            if (name.Contains("eau") || name.Contains("jus") || name.Contains("soda") || 
                name.Contains("vin") || name.Contains("bière") || name.Contains("café") || 
                name.Contains("thé"))
            {
                return ShoppingCategory.Beverages;
            }

            // Par défaut: autre
            return ShoppingCategory.Other;
        }

        /// <summary>
        /// Met la première lettre d'une chaîne en majuscule.
        /// </summary>
        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}
