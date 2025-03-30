using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Partie du service ShoppingListDatabaseService dédiée à la gestion des listes de courses.
    /// </summary>
    public partial class ShoppingListDatabaseService
    {
        /// <inheritdoc />
        public async Task<List<ShoppingList>> GetAllShoppingListsAsync()
        {
            var shoppingLists = new List<ShoppingList>();
            
            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, MealPlanId, IsActive, CreatedAt, UpdatedAt 
                FROM ShoppingLists 
                ORDER BY CreatedAt DESC";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var shoppingList = new ShoppingList
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    MealPlanId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    IsActive = reader.GetBoolean(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5))
                };
                
                // Charger les articles de la liste
                shoppingList.Items = await GetShoppingItemsByListIdAsync(shoppingList.Id);
                
                shoppingLists.Add(shoppingList);
            }
            
            return shoppingLists;
        }

        /// <inheritdoc />
        public async Task<ShoppingList?> GetShoppingListByIdAsync(int listId)
        {
            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, MealPlanId, IsActive, CreatedAt, UpdatedAt 
                FROM ShoppingLists 
                WHERE Id = @ListId";
                
            command.Parameters.AddWithValue("@ListId", listId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var shoppingList = new ShoppingList
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    MealPlanId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    IsActive = reader.GetBoolean(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5))
                };
                
                // Charger les articles de la liste
                shoppingList.Items = await GetShoppingItemsByListIdAsync(shoppingList.Id);
                
                return shoppingList;
            }
            
            return null;
        }

        /// <inheritdoc />
        public async Task<ShoppingList?> GetShoppingListByMealPlanIdAsync(int mealPlanId)
        {
            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, MealPlanId, IsActive, CreatedAt, UpdatedAt 
                FROM ShoppingLists 
                WHERE MealPlanId = @MealPlanId";
                
            command.Parameters.AddWithValue("@MealPlanId", mealPlanId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var shoppingList = new ShoppingList
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    MealPlanId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                    IsActive = reader.GetBoolean(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5))
                };
                
                // Charger les articles de la liste
                shoppingList.Items = await GetShoppingItemsByListIdAsync(shoppingList.Id);
                
                return shoppingList;
            }
            
            return null;
        }

        /// <inheritdoc />
        public async Task<int> CreateShoppingListAsync(ShoppingList shoppingList)
        {
            if (shoppingList == null)
                throw new ArgumentNullException(nameof(shoppingList));
                
            // Mise à jour des dates de création/modification
            var now = DateTime.Now;
            shoppingList.CreatedAt = now;
            shoppingList.UpdatedAt = now;

            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Insérer la liste de courses
                var insertCommand = _databaseService.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
                    INSERT INTO ShoppingLists (Name, MealPlanId, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@Name, @MealPlanId, @IsActive, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();";
                    
                insertCommand.Parameters.AddWithValue("@Name", shoppingList.Name);
                insertCommand.Parameters.AddWithValue("@MealPlanId", shoppingList.MealPlanId as object ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@IsActive", shoppingList.IsActive ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@CreatedAt", shoppingList.CreatedAt.ToString("o"));
                insertCommand.Parameters.AddWithValue("@UpdatedAt", shoppingList.UpdatedAt.ToString("o"));
                
                var listId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());
                shoppingList.Id = listId;
                
                // Insérer les articles de la liste
                if (shoppingList.Items != null && shoppingList.Items.Any())
                {
                    foreach (var item in shoppingList.Items)
                    {
                        await AddShoppingItemInternalAsync(transaction, listId, item);
                    }
                }
                
                transaction.Commit();
                return listId;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateShoppingListAsync(ShoppingList shoppingList)
        {
            if (shoppingList == null)
                throw new ArgumentNullException(nameof(shoppingList));
                
            if (shoppingList.Id <= 0)
                throw new ArgumentException("La liste de courses doit avoir un ID valide.", nameof(shoppingList));
                
            // Mise à jour de la date de modification
            shoppingList.UpdatedAt = DateTime.Now;

            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Mettre à jour la liste de courses
                var updateCommand = _databaseService.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
                    UPDATE ShoppingLists 
                    SET Name = @Name, 
                        MealPlanId = @MealPlanId, 
                        IsActive = @IsActive, 
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @ListId";
                    
                updateCommand.Parameters.AddWithValue("@Name", shoppingList.Name);
                updateCommand.Parameters.AddWithValue("@MealPlanId", shoppingList.MealPlanId as object ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@IsActive", shoppingList.IsActive ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@UpdatedAt", shoppingList.UpdatedAt.ToString("o"));
                updateCommand.Parameters.AddWithValue("@ListId", shoppingList.Id);
                
                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                
                if (rowsAffected <= 0)
                {
                    transaction.Rollback();
                    return false;
                }
                
                // Supprimer tous les articles existants
                var deleteCommand = _databaseService.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM ShoppingItems WHERE ShoppingListId = @ListId";
                deleteCommand.Parameters.AddWithValue("@ListId", shoppingList.Id);
                await deleteCommand.ExecuteNonQueryAsync();
                
                // Insérer les articles mis à jour
                if (shoppingList.Items != null && shoppingList.Items.Any())
                {
                    foreach (var item in shoppingList.Items)
                    {
                        await AddShoppingItemInternalAsync(transaction, shoppingList.Id, item);
                    }
                }
                
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteShoppingListAsync(int listId)
        {
            using var command = _databaseService.CreateCommand();
            command.CommandText = "DELETE FROM ShoppingLists WHERE Id = @ListId";
            command.Parameters.AddWithValue("@ListId", listId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Récupère les articles d'une liste de courses.
        /// </summary>
        /// <param name="listId">ID de la liste de courses</param>
        /// <returns>Liste des articles</returns>
        private async Task<List<ShoppingItem>> GetShoppingItemsByListIdAsync(int listId)
        {
            var items = new List<ShoppingItem>();
            
            using var command = _databaseService.CreateCommand();
            command.CommandText = @"
                SELECT Id, ShoppingListId, Name, Quantity, Unit, Category, 
                       IsPurchased, Notes, IsManuallyAdded, AssociatedRecipesData
                FROM ShoppingItems
                WHERE ShoppingListId = @ListId
                ORDER BY Category, Name";
                
            command.Parameters.AddWithValue("@ListId", listId);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var recipesData = reader.GetString(9);
                List<Recipe> associatedRecipes;
                
                try
                {
                    associatedRecipes = JsonConvert.DeserializeObject<List<Recipe>>(recipesData) ?? new List<Recipe>();
                }
                catch
                {
                    associatedRecipes = new List<Recipe>();
                }
                
                var item = new ShoppingItem
                {
                    Id = reader.GetInt32(0),
                    ShoppingListId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    Quantity = reader.GetDouble(3),
                    Unit = reader.GetString(4),
                    Category = (ShoppingCategory)reader.GetInt32(5),
                    IsPurchased = reader.GetBoolean(6),
                    Notes = reader.GetString(7),
                    IsManuallyAdded = reader.GetBoolean(8),
                    AssociatedRecipes = associatedRecipes
                };
                
                items.Add(item);
            }
            
            return items;
        }
    }
}
