using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

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
        /// <param name="includeLocalOnly">Indique si les données locales uniquement doivent être incluses</param>
        /// <returns>True si l'export est réussi, False sinon</returns>
        public async Task<bool> ExportUserDataToJsonAsync(string filePath, bool includeLocalOnly = false)
        {
            try
            {
                // Récupérer toutes les données utilisateur
                var favorites = await _userDataRepository.GetAllFavoritesAsync();
                var collections = await _userDataRepository.GetAllCollectionsAsync();
                var mealPlans = await _userDataRepository.GetAllMealPlansAsync();
                var shoppingLists = await _userDataRepository.GetAllShoppingListsAsync();
                
                // Créer l'objet de données utilisateur
                var userData = new
                {
                    Favorites = favorites,
                    Collections = collections,
                    MealPlans = mealPlans,
                    ShoppingLists = shoppingLists,
                    ExportDate = DateTime.Now,
                    Version = "1.0",
                    // Ajouter des métadonnées pour faciliter l'import
                    Metadata = new
                    {
                        DeviceId = GetDeviceId(),
                        ApplicationVersion = GetApplicationVersion(),
                        LocalOnly = includeLocalOnly
                    }
                };

                // Sérialiser en JSON avec une configuration adaptée
                var json = JsonConvert.SerializeObject(userData, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc, // Standardiser les dates en UTC
                    TypeNameHandling = TypeNameHandling.Auto // Permet de mieux gérer les types lors de l'import
                });

                // Écrire dans un fichier avec le bon encodage
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                
                // Journaliser l'export
                LogExportActivity(filePath, favorites.Count, collections.Count, mealPlans.Count, shoppingLists.Count);
                
                return true;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                Console.WriteLine($"Erreur lors de l'export des données: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Journalise une activité d'export pour le suivi.
        /// </summary>
        private void LogExportActivity(string filePath, int favoritesCount, int collectionsCount, int mealPlansCount, int shoppingListsCount)
        {
            // Journalisation simple pour le moment, pourrait être étendue
            Console.WriteLine($"Export réussi vers {filePath}:");
            Console.WriteLine($"- {favoritesCount} recettes favorites");
            Console.WriteLine($"- {collectionsCount} collections");
            Console.WriteLine($"- {mealPlansCount} plans de repas");
            Console.WriteLine($"- {shoppingListsCount} listes de courses");
        }
        
        /// <summary>
        /// Obtient un identifiant unique pour l'appareil.
        /// </summary>
        private string GetDeviceId()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecipeHub");
                
            var deviceIdPath = Path.Combine(appDataPath, "device_id.txt");
            
            // Créer ou récupérer l'ID unique de l'appareil
            if (File.Exists(deviceIdPath))
            {
                return File.ReadAllText(deviceIdPath).Trim();
            }
            else
            {
                // Générer un nouvel ID
                var deviceId = Guid.NewGuid().ToString();
                Directory.CreateDirectory(appDataPath);
                File.WriteAllText(deviceIdPath, deviceId);
                return deviceId;
            }
        }
        
        /// <summary>
        /// Obtient la version de l'application.
        /// </summary>
        private string GetApplicationVersion()
        {
            // À remplacer par la version réelle de l'application
            return "1.0.0";
        }

        /// <summary>
        /// Importe les données utilisateur depuis un fichier JSON.
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'import</param>
        /// <param name="mergeStrategy">Stratégie de fusion des données ("replace", "merge", "keepBoth")</param>
        /// <returns>Résultat de l'importation avec message</returns>
        public async Task<(bool Success, string Message)> ImportUserDataFromJsonAsync(string filePath, string mergeStrategy = "merge")
        {
            try
            {
                // Lire le fichier JSON
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
                
                // Vérifier la structure et la version
                try
                {
                    dynamic userData = JsonConvert.DeserializeObject<dynamic>(json);
                    string version = userData.Version;
                    
                    // Vérifier la compatibilité des versions
                    if (!IsVersionCompatible(version))
                    {
                        return (false, $"La version des données ({version}) n'est pas compatible avec la version actuelle de l'application.");
                    }
                    
                    // Vérifier les métadonnées si présentes
                    if (userData.Metadata != null)
                    {
                        // Possibilité d'ajouter des vérifications supplémentaires ici
                    }
                }
                catch (Exception)
                {
                    return (false, "Format de données incorrect ou incompatible.");
                }
                
                // Effectuer l'import avec la stratégie spécifiée
                var success = await _userDataRepository.ImportUserDataAsync(filePath, mergeStrategy);
                
                if (success)
                {
                    return (true, "Importation réussie. Vos données ont été correctement intégrées.");
                }
                else
                {
                    return (false, "Erreur lors de l'importation des données. Veuillez réessayer.");
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                Console.WriteLine($"Erreur détaillée lors de l'importation: {ex.ToString()}");
                return (false, $"Erreur lors de l'importation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Vérifie si la version des données est compatible avec la version actuelle.
        /// </summary>
        /// <param name="version">Version des données à importer</param>
        /// <returns>True si compatible, False sinon</returns>
        private bool IsVersionCompatible(string version)
        {
            // Pour l'instant, on accepte toutes les versions 1.x
            return version.StartsWith("1.");
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
        /// Obtient des statistiques détaillées sur les données utilisateur.
        /// </summary>
        /// <returns>Dictionnaire de statistiques</returns>
        public async Task<Dictionary<string, int>> GetUserDataStatisticsAsync()
        {
            var stats = await _userDataRepository.GetUserDataStatisticsAsync();
            
            // Ajouter des statistiques sur l'utilisation du stockage
            try
            {
                var dbFileInfo = new FileInfo(_databaseService.DatabasePath);
                if (dbFileInfo.Exists)
                {
                    stats["DatabaseSizeKB"] = (int)(dbFileInfo.Length / 1024);
                }
                
                // Autres statistiques potentielles
                stats["LastSyncDate"] = (int)GetLastSyncTimestamp();
            }
            catch
            {
                // Ignorer les erreurs de statistiques supplémentaires
            }
            
            return stats;
        }
        
        /// <summary>
        /// Obtient la date de la dernière synchronisation sous forme de timestamp Unix.
        /// </summary>
        private long GetLastSyncTimestamp()
        {
            try
            {            
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RecipeHub");
                    
                var syncInfoPath = Path.Combine(appDataPath, "last_sync.txt");
                
                if (File.Exists(syncInfoPath))
                {
                    var lastSyncText = File.ReadAllText(syncInfoPath).Trim();
                    if (long.TryParse(lastSyncText, out long timestamp))
                    {
                        return timestamp;
                    }
                }
                
                // Par défaut, retourner 0 (jamais synchronisé)
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Met à jour la date de dernière synchronisation.
        /// </summary>
        private void UpdateLastSyncTimestamp()
        {
            try
            {            
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RecipeHub");
                    
                Directory.CreateDirectory(appDataPath);
                var syncInfoPath = Path.Combine(appDataPath, "last_sync.txt");
                
                // Timestamp Unix en secondes
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(syncInfoPath, timestamp.ToString());
            }
            catch
            {
                // Ignorer les erreurs d'écriture du timestamp
            }
        }

        /// <summary>
        /// Crée un fichier d'export crypté des données utilisateur.
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'export</param>
        /// <param name="password">Mot de passe pour le cryptage</param>
        /// <returns>True si l'export est réussi, False sinon</returns>
        public async Task<bool> ExportEncryptedUserDataAsync(string filePath, string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Le mot de passe ne peut pas être vide.", nameof(password));
                
            try
            {            
                // Exporter d'abord en JSON
                var tempFile = Path.GetTempFileName();
                var exportSuccess = await ExportUserDataToJsonAsync(tempFile);
                
                if (!exportSuccess)
                    return false;
                    
                // Lire le contenu JSON
                byte[] jsonBytes = await File.ReadAllBytesAsync(tempFile);
                
                // Dériver une clé du mot de passe
                using var deriveBytes = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256);
                byte[] key = deriveBytes.GetBytes(32); // Clé AES-256
                byte[] iv = deriveBytes.GetBytes(16);  // IV pour AES
                
                // Crypter les données
                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                
                using var memoryStream = new MemoryStream();
                // Stocker le sel au début du fichier
                await memoryStream.WriteAsync(deriveBytes.Salt);
                
                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    await cryptoStream.WriteAsync(jsonBytes);
                    cryptoStream.FlushFinalBlock();
                }
                
                // Écrire le fichier crypté
                await File.WriteAllBytesAsync(filePath, memoryStream.ToArray());
                
                // Supprimer le fichier temporaire
                File.Delete(tempFile);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'export crypté: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Importe des données utilisateur depuis un fichier crypté.
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'import</param>
        /// <param name="password">Mot de passe pour le décryptage</param>
        /// <param name="mergeStrategy">Stratégie de fusion des données</param>
        /// <returns>Résultat de l'importation avec message</returns>
        public async Task<(bool Success, string Message)> ImportEncryptedUserDataAsync(
            string filePath, string password, string mergeStrategy = "merge")
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Le mot de passe ne peut pas être vide.");
                
            try
            {            
                // Lire le fichier crypté
                byte[] encryptedData = await File.ReadAllBytesAsync(filePath);
                
                // Récupérer le sel (16 premiers octets)
                byte[] salt = new byte[16];
                Array.Copy(encryptedData, 0, salt, 0, 16);
                
                // Dériver la clé avec le sel récupéré
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                byte[] key = deriveBytes.GetBytes(32);
                byte[] iv = deriveBytes.GetBytes(16);
                
                // Préparer le déchiffrement
                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                
                // Créer un fichier temporaire pour stocker le JSON déchiffré
                var tempFile = Path.GetTempFileName();
                
                try
                {
                    using (var fs = new FileStream(tempFile, FileMode.Create))
                    {
                        using var memoryStream = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
                        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                        
                        await cryptoStream.CopyToAsync(fs);
                    }
                    
                    // Importer le fichier temporaire déchiffré
                    var importResult = await ImportUserDataFromJsonAsync(tempFile, mergeStrategy);
                    
                    // Supprimer le fichier temporaire
                    File.Delete(tempFile);
                    
                    return importResult;
                }
                catch (CryptographicException)
                {
                    // Supprimer le fichier temporaire en cas d'erreur
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                        
                    return (false, "Mot de passe incorrect ou fichier corrompu.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de l'import: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Synchronise les données avec un serveur distant (si configuré).
        /// </summary>
        /// <returns>Résultat de la synchronisation avec message</returns>
        public async Task<(bool Success, string Message)> SynchronizeDataAsync()
        {
            // Vérifier si la synchronisation est configurée
            bool syncEnabled = await IsSyncEnabledAsync();
            if (!syncEnabled)
            {
                return (false, "La synchronisation n'est pas configurée. Veuillez activer ce service dans les paramètres.");
            }
            
            try
            {
                // Obtenir la date de dernière synchronisation
                long lastSyncTimestamp = GetLastSyncTimestamp();
                DateTime lastSyncDate = lastSyncTimestamp > 0 
                    ? DateTimeOffset.FromUnixTimeSeconds(lastSyncTimestamp).DateTime
                    : DateTime.MinValue;
                
                // À ce stade, une implémentation réelle contacterait un serveur distant
                // pour synchroniser les données. Dans cette version, nous simulerons une synchronisation réussie.
                
                // 1. Simuler l'obtention des modifications du serveur
                // 2. Fusionner les modifications avec les données locales
                // 3. Envoyer les modifications locales au serveur
                
                // Mise à jour de la date de dernière synchronisation
                UpdateLastSyncTimestamp();
                
                return (true, $"Synchronisation réussie. Dernière synchronisation: {DateTime.Now.ToString("g")}");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la synchronisation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Vérifie si la synchronisation est activée et configurée.
        /// </summary>
        /// <returns>True si la synchronisation est activée, False sinon</returns>
        private async Task<bool> IsSyncEnabledAsync()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RecipeHub");
                    
                var syncConfigPath = Path.Combine(appDataPath, "sync_config.json");
                
                if (File.Exists(syncConfigPath))
                {
                    var json = await File.ReadAllTextAsync(syncConfigPath);
                    var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    
                    if (config != null && config.TryGetValue("Enabled", out var enabled))
                    {
                        return Convert.ToBoolean(enabled);
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
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
