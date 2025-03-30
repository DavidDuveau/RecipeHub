using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Partie du service MealPlanningDatabaseService dédiée à la gestion des plans de repas.
    /// </summary>
    public partial class MealPlanningDatabaseService
    {
        /// <inheritdoc />
        public async Task<List<MealPlan>> GetAllMealPlansAsync()
        {
            var mealPlans = new List<MealPlan>();
            
            using var command = _databaseService.CreateCommand();
            command.CommandText = "SELECT Id, Name, StartDate, EndDate, IsActive, CreatedAt, UpdatedAt FROM MealPlans ORDER BY StartDate DESC";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var mealPlan = new MealPlan
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    StartDate = DateTime.Parse(reader.GetString(2)),
                    EndDate = DateTime.Parse(reader.GetString(3)),
                    IsActive = reader.GetBoolean(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    UpdatedAt = DateTime.Parse(reader.GetString(6))
                };
                
                // Charger les repas planifiés pour ce plan
                mealPlan.Meals = await GetPlannedMealsByPlanIdAsync(mealPlan.Id);
                
                mealPlans.Add(mealPlan);
            }
            
            return mealPlans;
        }

        /// <inheritdoc />
        public async Task<MealPlan?> GetActiveMealPlanAsync()
        {
            using var command = _databaseService.CreateCommand();
            command.CommandText = "SELECT Id, Name, StartDate, EndDate, IsActive, CreatedAt, UpdatedAt FROM MealPlans WHERE IsActive = 1 LIMIT 1";
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var mealPlan = new MealPlan
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    StartDate = DateTime.Parse(reader.GetString(2)),
                    EndDate = DateTime.Parse(reader.GetString(3)),
                    IsActive = reader.GetBoolean(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    UpdatedAt = DateTime.Parse(reader.GetString(6))
                };
                
                // Charger les repas planifiés pour ce plan
                mealPlan.Meals = await GetPlannedMealsByPlanIdAsync(mealPlan.Id);
                
                return mealPlan;
            }
            
            return null;
        }

        /// <inheritdoc />
        public async Task<MealPlan?> GetMealPlanByIdAsync(int planId)
        {
            using var command = _databaseService.CreateCommand();
            command.CommandText = "SELECT Id, Name, StartDate, EndDate, IsActive, CreatedAt, UpdatedAt FROM MealPlans WHERE Id = @PlanId";
            command.Parameters.AddWithValue("@PlanId", planId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var mealPlan = new MealPlan
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    StartDate = DateTime.Parse(reader.GetString(2)),
                    EndDate = DateTime.Parse(reader.GetString(3)),
                    IsActive = reader.GetBoolean(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    UpdatedAt = DateTime.Parse(reader.GetString(6))
                };
                
                // Charger les repas planifiés pour ce plan
                mealPlan.Meals = await GetPlannedMealsByPlanIdAsync(mealPlan.Id);
                
                return mealPlan;
            }
            
            return null;
        }

        /// <inheritdoc />
        public async Task<List<MealPlan>> GetMealPlansByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var mealPlans = new List<MealPlan>();
            
            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, StartDate, EndDate, IsActive, CreatedAt, UpdatedAt 
                FROM MealPlans 
                WHERE (StartDate BETWEEN @StartDate AND @EndDate) 
                   OR (EndDate BETWEEN @StartDate AND @EndDate)
                   OR (StartDate <= @StartDate AND EndDate >= @EndDate)
                ORDER BY StartDate";
                
            command.Parameters.AddWithValue("@StartDate", startDate.ToString("o"));
            command.Parameters.AddWithValue("@EndDate", endDate.ToString("o"));
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var mealPlan = new MealPlan
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    StartDate = DateTime.Parse(reader.GetString(2)),
                    EndDate = DateTime.Parse(reader.GetString(3)),
                    IsActive = reader.GetBoolean(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    UpdatedAt = DateTime.Parse(reader.GetString(6))
                };
                
                // Charger les repas planifiés pour ce plan
                mealPlan.Meals = await GetPlannedMealsByPlanIdAsync(mealPlan.Id);
                
                mealPlans.Add(mealPlan);
            }
            
            return mealPlans;
        }

        /// <inheritdoc />
        public async Task<int> CreateMealPlanAsync(MealPlan mealPlan)
        {
            if (mealPlan == null)
                throw new ArgumentNullException(nameof(mealPlan));

            // Mise à jour des dates de création/modification
            var now = DateTime.Now;
            mealPlan.CreatedAt = now;
            mealPlan.UpdatedAt = now;

            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Si ce plan est actif, désactiver tous les autres plans
                if (mealPlan.IsActive)
                {
                    var deactivateCommand = _databaseService.CreateCommand();
                    deactivateCommand.Transaction = transaction;
                    deactivateCommand.CommandText = "UPDATE MealPlans SET IsActive = 0";
                    await deactivateCommand.ExecuteNonQueryAsync();
                }
                
                // Insérer le plan de repas
                var insertCommand = _databaseService.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
                    INSERT INTO MealPlans (Name, StartDate, EndDate, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@Name, @StartDate, @EndDate, @IsActive, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();";
                    
                insertCommand.Parameters.AddWithValue("@Name", mealPlan.Name);
                insertCommand.Parameters.AddWithValue("@StartDate", mealPlan.StartDate.ToString("o"));
                insertCommand.Parameters.AddWithValue("@EndDate", mealPlan.EndDate.ToString("o"));
                insertCommand.Parameters.AddWithValue("@IsActive", mealPlan.IsActive ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@CreatedAt", mealPlan.CreatedAt.ToString("o"));
                insertCommand.Parameters.AddWithValue("@UpdatedAt", mealPlan.UpdatedAt.ToString("o"));
                
                var planId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());
                mealPlan.Id = planId;
                
                // Insérer les repas planifiés
                if (mealPlan.Meals != null && mealPlan.Meals.Any())
                {
                    foreach (var meal in mealPlan.Meals)
                    {
                        meal.RecipeId = meal.Recipe?.Id ?? 0;
                        await AddPlannedMealInternalAsync(transaction, planId, meal);
                    }
                }
                
                transaction.Commit();
                return planId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateMealPlanAsync(MealPlan mealPlan)
        {
            if (mealPlan == null)
                throw new ArgumentNullException(nameof(mealPlan));
                
            if (mealPlan.Id <= 0)
                throw new ArgumentException("Le plan de repas doit avoir un ID valide.", nameof(mealPlan));

            // Mise à jour de la date de modification
            mealPlan.UpdatedAt = DateTime.Now;

            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Si ce plan est actif, désactiver tous les autres plans
                if (mealPlan.IsActive)
                {
                    var deactivateCommand = _databaseService.CreateCommand();
                    deactivateCommand.Transaction = transaction;
                    deactivateCommand.CommandText = "UPDATE MealPlans SET IsActive = 0 WHERE Id != @PlanId";
                    deactivateCommand.Parameters.AddWithValue("@PlanId", mealPlan.Id);
                    await deactivateCommand.ExecuteNonQueryAsync();
                }
                
                // Mettre à jour le plan de repas
                var updateCommand = _databaseService.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
                    UPDATE MealPlans 
                    SET Name = @Name, 
                        StartDate = @StartDate, 
                        EndDate = @EndDate, 
                        IsActive = @IsActive, 
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @PlanId";
                    
                updateCommand.Parameters.AddWithValue("@Name", mealPlan.Name);
                updateCommand.Parameters.AddWithValue("@StartDate", mealPlan.StartDate.ToString("o"));
                updateCommand.Parameters.AddWithValue("@EndDate", mealPlan.EndDate.ToString("o"));
                updateCommand.Parameters.AddWithValue("@IsActive", mealPlan.IsActive ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@UpdatedAt", mealPlan.UpdatedAt.ToString("o"));
                updateCommand.Parameters.AddWithValue("@PlanId", mealPlan.Id);
                
                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                
                if (rowsAffected <= 0)
                {
                    transaction.Rollback();
                    return false;
                }
                
                // Supprimer tous les repas planifiés existants
                var deleteCommand = _databaseService.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM PlannedMeals WHERE MealPlanId = @PlanId";
                deleteCommand.Parameters.AddWithValue("@PlanId", mealPlan.Id);
                await deleteCommand.ExecuteNonQueryAsync();
                
                // Insérer les repas planifiés mis à jour
                if (mealPlan.Meals != null && mealPlan.Meals.Any())
                {
                    foreach (var meal in mealPlan.Meals)
                    {
                        meal.RecipeId = meal.Recipe?.Id ?? 0;
                        await AddPlannedMealInternalAsync(transaction, mealPlan.Id, meal);
                    }
                }
                
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteMealPlanAsync(int planId)
        {
            using var command = _databaseService.CreateCommand();
            command.CommandText = "DELETE FROM MealPlans WHERE Id = @PlanId";
            command.Parameters.AddWithValue("@PlanId", planId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <inheritdoc />
        public async Task<bool> SetMealPlanActiveAsync(int planId)
        {
            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Désactiver tous les plans
                var deactivateCommand = _databaseService.CreateCommand();
                deactivateCommand.Transaction = transaction;
                deactivateCommand.CommandText = "UPDATE MealPlans SET IsActive = 0";
                await deactivateCommand.ExecuteNonQueryAsync();
                
                // Activer le plan spécifié
                var activateCommand = _databaseService.CreateCommand();
                activateCommand.Transaction = transaction;
                activateCommand.CommandText = "UPDATE MealPlans SET IsActive = 1 WHERE Id = @PlanId";
                activateCommand.Parameters.AddWithValue("@PlanId", planId);
                
                var rowsAffected = await activateCommand.ExecuteNonQueryAsync();
                
                transaction.Commit();
                return rowsAffected > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
