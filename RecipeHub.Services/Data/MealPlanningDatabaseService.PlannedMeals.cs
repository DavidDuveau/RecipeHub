using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Partie du service MealPlanningDatabaseService dédiée à la gestion des repas planifiés.
    /// </summary>
    public partial class MealPlanningDatabaseService
    {
        /// <inheritdoc />
        public async Task<bool> AddPlannedMealAsync(int planId, PlannedMeal plannedMeal)
        {
            if (plannedMeal == null)
                throw new ArgumentNullException(nameof(plannedMeal));
                
            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                await AddPlannedMealInternalAsync(transaction, planId, plannedMeal);
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Méthode interne pour ajouter un repas planifié.
        /// </summary>
        private async Task AddPlannedMealInternalAsync(SqliteTransaction transaction, int planId, PlannedMeal plannedMeal)
        {
            var insertCommand = _databaseService.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = @"
                INSERT INTO PlannedMeals (
                    MealPlanId, RecipeId, RecipeData, Date, Type, 
                    Servings, Notes, IncludeInShoppingList
                )
                VALUES (
                    @MealPlanId, @RecipeId, @RecipeData, @Date, @Type, 
                    @Servings, @Notes, @IncludeInShoppingList
                );
                SELECT last_insert_rowid();";
                
            insertCommand.Parameters.AddWithValue("@MealPlanId", planId);
            insertCommand.Parameters.AddWithValue("@RecipeId", plannedMeal.RecipeId);
            insertCommand.Parameters.AddWithValue("@RecipeData", JsonConvert.SerializeObject(plannedMeal.Recipe));
            insertCommand.Parameters.AddWithValue("@Date", plannedMeal.Date.ToString("o"));
            insertCommand.Parameters.AddWithValue("@Type", (int)plannedMeal.Type);
            insertCommand.Parameters.AddWithValue("@Servings", plannedMeal.Servings);
            insertCommand.Parameters.AddWithValue("@Notes", plannedMeal.Notes ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@IncludeInShoppingList", plannedMeal.IncludeInShoppingList ? 1 : 0);
            
            var mealId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());
            plannedMeal.Id = mealId;
        }

        /// <inheritdoc />
        public async Task<bool> UpdatePlannedMealAsync(PlannedMeal plannedMeal)
        {
            if (plannedMeal == null)
                throw new ArgumentNullException(nameof(plannedMeal));
                
            if (plannedMeal.Id <= 0)
                throw new ArgumentException("Le repas planifié doit avoir un ID valide.", nameof(plannedMeal));

            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                UPDATE PlannedMeals
                SET RecipeId = @RecipeId,
                    RecipeData = @RecipeData,
                    Date = @Date,
                    Type = @Type,
                    Servings = @Servings,
                    Notes = @Notes,
                    IncludeInShoppingList = @IncludeInShoppingList
                WHERE Id = @MealId";
                
            command.Parameters.AddWithValue("@RecipeId", plannedMeal.RecipeId);
            command.Parameters.AddWithValue("@RecipeData", JsonConvert.SerializeObject(plannedMeal.Recipe));
            command.Parameters.AddWithValue("@Date", plannedMeal.Date.ToString("o"));
            command.Parameters.AddWithValue("@Type", (int)plannedMeal.Type);
            command.Parameters.AddWithValue("@Servings", plannedMeal.Servings);
            command.Parameters.AddWithValue("@Notes", plannedMeal.Notes ?? string.Empty);
            command.Parameters.AddWithValue("@IncludeInShoppingList", plannedMeal.IncludeInShoppingList ? 1 : 0);
            command.Parameters.AddWithValue("@MealId", plannedMeal.Id);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <inheritdoc />
        public async Task<bool> DeletePlannedMealAsync(int mealId)
        {
            using var command = _databaseService.CreateCommand();
            command.CommandText = "DELETE FROM PlannedMeals WHERE Id = @MealId";
            command.Parameters.AddWithValue("@MealId", mealId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <inheritdoc />
        public async Task<List<PlannedMeal>> GetPlannedMealsByDateAsync(DateTime date)
        {
            var dateString = date.Date.ToString("o").Split('T')[0]; // Seulement la partie date
            var meals = new List<PlannedMeal>();
            
            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                SELECT p.Id, p.MealPlanId, p.RecipeId, p.RecipeData, p.Date, 
                       p.Type, p.Servings, p.Notes, p.IncludeInShoppingList
                FROM PlannedMeals p
                JOIN MealPlans mp ON p.MealPlanId = mp.Id
                WHERE p.Date LIKE @DatePrefix
                ORDER BY p.Type";
                
            command.Parameters.AddWithValue("@DatePrefix", $"{dateString}%");
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var recipeData = reader.GetString(3);
                var recipe = JsonConvert.DeserializeObject<Recipe>(recipeData);
                
                var meal = new PlannedMeal
                {
                    Id = reader.GetInt32(0),
                    RecipeId = reader.GetInt32(2),
                    Recipe = recipe,
                    Date = DateTime.Parse(reader.GetString(4)),
                    Type = (MealType)reader.GetInt32(5),
                    Servings = reader.GetInt32(6),
                    Notes = reader.GetString(7),
                    IncludeInShoppingList = reader.GetBoolean(8)
                };
                
                meals.Add(meal);
            }
            
            return meals;
        }

        /// <summary>
        /// Récupère les repas planifiés pour un plan spécifique.
        /// </summary>
        /// <param name="planId">ID du plan de repas</param>
        private async Task<List<PlannedMeal>> GetPlannedMealsByPlanIdAsync(int planId)
        {
            var meals = new List<PlannedMeal>();
            
            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                SELECT Id, MealPlanId, RecipeId, RecipeData, Date, Type, Servings, Notes, IncludeInShoppingList
                FROM PlannedMeals
                WHERE MealPlanId = @PlanId
                ORDER BY Date, Type";
                
            command.Parameters.AddWithValue("@PlanId", planId);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var recipeData = reader.GetString(3);
                var recipe = JsonConvert.DeserializeObject<Recipe>(recipeData);
                
                var meal = new PlannedMeal
                {
                    Id = reader.GetInt32(0),
                    RecipeId = reader.GetInt32(2),
                    Recipe = recipe,
                    Date = DateTime.Parse(reader.GetString(4)),
                    Type = (MealType)reader.GetInt32(5),
                    Servings = reader.GetInt32(6),
                    Notes = reader.GetString(7),
                    IncludeInShoppingList = reader.GetBoolean(8)
                };
                
                meals.Add(meal);
            }
            
            return meals;
        }
    }
}
