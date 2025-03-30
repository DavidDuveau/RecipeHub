using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RecipeHub.Services.Data
{
    /// <summary>
    /// Service pour la synchronisation et l'export/import des données utilisateur.
    /// </summary>
    public class DataSyncService : IDisposable
    {
        private readonly IUserDataRepository _userDataRepository;
        private readonly DatabaseService _databaseService;
        private bool _disposed = false;

        /// <summary>
        /// Constructeur du service de synchronisation.
        /// </summary>
        public DataSyncService(
            IUserDataRepository userDataRepository,
            DatabaseService databaseService)
        {
            _userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Exporte toutes les données utilisateur dans un fichier JSON.
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'export</param>
        public async Task<bool> ExportUserDataToJsonAsync(string filePath)
        {
            try
            {
                var userData = new
                {
                    Favorites = await _userDataRepository.GetAllFavoritesAsync(),
                    Collections = await _userDataRepository.GetAllCollectionsAsync(),
                    MealPlans = await _userDataRepository.GetAllMealPlansAsync(),
                    ShoppingLists = await _userDataRepository.GetAllShoppingListsAsync(),
                    ExportDate = DateTime.Now,
                    Version = "1.0"
                };

                var json = JsonConvert.SerializeObject(userData, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Importe les données utilisateur depuis un fichier JSON.
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'import</param>
        public async Task<(bool Success, string Message)> ImportUserDataFromJsonAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                
                // Vérifier que le JSON est valide
                try
                {
                    JsonConvert.DeserializeObject(json);
                }
                catch (JsonException)
                {
                    return (false, "Le fichier n'est pas un JSON valide.");
                }
                
                // Vérifier que le JSON contient les données attendues
                if (!json.Contains("Favorites") || !json.Contains("Collections") || 
                    !json.Contains("MealPlans") || !json.Contains("ShoppingLists"))
                {
                    return (false, "Le fichier JSON ne contient pas les données attendues.");
                }
                
                // Effectuer l'import
                var success = await _userDataRepository.ImportUserDataAsync(filePath);
                return success 
                    ? (true, "Importation réussie.")
                    : (false, "Erreur lors de l'importation des données.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de l'importation: {ex.Message}");
            }
        }

        /// <summary>
        /// Crée une sauvegarde de la base de données.
        /// </summary>
        /// <param name="backupPath">Chemin du fichier de sauvegarde</param>
        public bool BackupDatabase(string backupPath)
        {
            return _databaseService.BackupDatabase(backupPath);
        }

        /// <summary>
        /// Restaure une base de données à partir d'une sauvegarde.
        /// </summary>
        /// <param name="backupPath">Chemin du fichier de sauvegarde</param>
        public bool RestoreDatabase(string backupPath)
        {
            return _databaseService.RestoreDatabase(backupPath);
        }

        /// <summary>
        /// Obtient des statistiques sur les données utilisateur.
        /// </summary>
        public async Task<Dictionary<string, int>> GetUserDataStatisticsAsync()
        {
            return await _userDataRepository.GetUserDataStatisticsAsync();
        }

        /// <summary>
        /// Dispose des ressources utilisées par le service.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose des ressources utilisées par le service.
        /// </summary>
        /// <param name="disposing">Indique si les ressources managées doivent être libérées</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Pas de ressources à libérer
                }

                _disposed = true;
            }
        }
    }
}
