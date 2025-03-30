using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Models;
using System;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Partie du service ShoppingListDatabaseService dédiée à la gestion des articles de courses.
    /// </summary>
    public partial class ShoppingListDatabaseService
    {
        /// <inheritdoc />
        public async Task<bool> AddShoppingItemAsync(int listId, ShoppingItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Mettre à jour la date de modification de la liste
                var updateListCommand = _databaseService.CreateCommand();
                updateListCommand.Transaction = transaction;
                updateListCommand.CommandText = "UPDATE ShoppingLists SET UpdatedAt = @UpdatedAt WHERE Id = @ListId";
                updateListCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("o"));
                updateListCommand.Parameters.AddWithValue("@ListId", listId);
                await updateListCommand.ExecuteNonQueryAsync();
                
                // Ajouter l'article
                await AddShoppingItemInternalAsync(transaction, listId, item);
                
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Méthode interne pour ajouter un article à une liste de courses.
        /// </summary>
        private async Task AddShoppingItemInternalAsync(SqliteTransaction transaction, int listId, ShoppingItem item)
        {
            var insertCommand = _databaseService.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = @"
                INSERT INTO ShoppingItems (
                    ShoppingListId, Name, Quantity, Unit, Category, 
                    IsPurchased, Notes, IsManuallyAdded, AssociatedRecipesData
                )
                VALUES (
                    @ShoppingListId, @Name, @Quantity, @Unit, @Category, 
                    @IsPurchased, @Notes, @IsManuallyAdded, @AssociatedRecipesData
                );
                SELECT last_insert_rowid();";
                
            insertCommand.Parameters.AddWithValue("@ShoppingListId", listId);
            insertCommand.Parameters.AddWithValue("@Name", item.Name);
            insertCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
            insertCommand.Parameters.AddWithValue("@Unit", item.Unit);
            insertCommand.Parameters.AddWithValue("@Category", (int)item.Category);
            insertCommand.Parameters.AddWithValue("@IsPurchased", item.IsPurchased ? 1 : 0);
            insertCommand.Parameters.AddWithValue("@Notes", item.Notes ?? string.Empty);
            insertCommand.Parameters.AddWithValue("@IsManuallyAdded", item.IsManuallyAdded ? 1 : 0);
            insertCommand.Parameters.AddWithValue("@AssociatedRecipesData", JsonConvert.SerializeObject(item.AssociatedRecipes));
            
            var itemId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());
            item.Id = itemId;
            item.ShoppingListId = listId;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateShoppingItemAsync(ShoppingItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            if (item.Id <= 0)
                throw new ArgumentException("L'article doit avoir un ID valide.", nameof(item));

            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Mettre à jour la date de modification de la liste
                var updateListCommand = _databaseService.CreateCommand();
                updateListCommand.Transaction = transaction;
                updateListCommand.CommandText = "UPDATE ShoppingLists SET UpdatedAt = @UpdatedAt WHERE Id = @ListId";
                updateListCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("o"));
                updateListCommand.Parameters.AddWithValue("@ListId", item.ShoppingListId);
                await updateListCommand.ExecuteNonQueryAsync();
                
                // Mettre à jour l'article
                var updateCommand = _databaseService.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
                    UPDATE ShoppingItems
                    SET Name = @Name,
                        Quantity = @Quantity,
                        Unit = @Unit,
                        Category = @Category,
                        IsPurchased = @IsPurchased,
                        Notes = @Notes,
                        IsManuallyAdded = @IsManuallyAdded,
                        AssociatedRecipesData = @AssociatedRecipesData
                    WHERE Id = @ItemId";
                    
                updateCommand.Parameters.AddWithValue("@Name", item.Name);
                updateCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                updateCommand.Parameters.AddWithValue("@Unit", item.Unit);
                updateCommand.Parameters.AddWithValue("@Category", (int)item.Category);
                updateCommand.Parameters.AddWithValue("@IsPurchased", item.IsPurchased ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@Notes", item.Notes ?? string.Empty);
                updateCommand.Parameters.AddWithValue("@IsManuallyAdded", item.IsManuallyAdded ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@AssociatedRecipesData", JsonConvert.SerializeObject(item.AssociatedRecipes));
                updateCommand.Parameters.AddWithValue("@ItemId", item.Id);
                
                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                
                transaction.Commit();
                return rowsAffected > 0;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteShoppingItemAsync(int itemId)
        {
            // Récupérer d'abord l'ID de la liste pour mettre à jour sa date de modification
            int listId;
            using (var getListIdCommand = _databaseService.CreateCommand())
            {
                getListIdCommand.CommandText = "SELECT ShoppingListId FROM ShoppingItems WHERE Id = @ItemId";
                getListIdCommand.Parameters.AddWithValue("@ItemId", itemId);
                
                var result = await getListIdCommand.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    return false;
                    
                listId = Convert.ToInt32(result);
            }

            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Mettre à jour la date de modification de la liste
                var updateListCommand = _databaseService.CreateCommand();
                updateListCommand.Transaction = transaction;
                updateListCommand.CommandText = "UPDATE ShoppingLists SET UpdatedAt = @UpdatedAt WHERE Id = @ListId";
                updateListCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("o"));
                updateListCommand.Parameters.AddWithValue("@ListId", listId);
                await updateListCommand.ExecuteNonQueryAsync();
                
                // Supprimer l'article
                var deleteCommand = _databaseService.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM ShoppingItems WHERE Id = @ItemId";
                deleteCommand.Parameters.AddWithValue("@ItemId", itemId);
                
                var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                
                transaction.Commit();
                return rowsAffected > 0;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetItemPurchasedStatusAsync(int itemId, bool isPurchased)
        {
            // Récupérer d'abord l'ID de la liste pour mettre à jour sa date de modification
            int listId;
            using (var getListIdCommand = _databaseService.CreateCommand())
            {
                getListIdCommand.CommandText = "SELECT ShoppingListId FROM ShoppingItems WHERE Id = @ItemId";
                getListIdCommand.Parameters.AddWithValue("@ItemId", itemId);
                
                var result = await getListIdCommand.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    return false;
                    
                listId = Convert.ToInt32(result);
            }

            using var transaction = _databaseService.BeginTransaction();
            
            try
            {
                // Mettre à jour la date de modification de la liste
                var updateListCommand = _databaseService.CreateCommand();
                updateListCommand.Transaction = transaction;
                updateListCommand.CommandText = "UPDATE ShoppingLists SET UpdatedAt = @UpdatedAt WHERE Id = @ListId";
                updateListCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("o"));
                updateListCommand.Parameters.AddWithValue("@ListId", listId);
                await updateListCommand.ExecuteNonQueryAsync();
                
                // Mettre à jour le statut d'achat
                var updateCommand = _databaseService.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = "UPDATE ShoppingItems SET IsPurchased = @IsPurchased WHERE Id = @ItemId";
                updateCommand.Parameters.AddWithValue("@IsPurchased", isPurchased ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@ItemId", itemId);
                
                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                
                transaction.Commit();
                return rowsAffected > 0;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
