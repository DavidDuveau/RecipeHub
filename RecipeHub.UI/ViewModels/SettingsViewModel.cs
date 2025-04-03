using RecipeHub.Services.Data;
using RecipeHub.UI.Commands;
using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace RecipeHub.UI.ViewModels
{
    /// <summary>
    /// ViewModel pour la vue des paramètres.
    /// </summary>
    public class SettingsViewModel : BindableBase
    {
        private readonly DataSyncService _dataSyncService;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private bool _isStatusSuccess;
        private ObservableCollection<KeyValuePair<string, int>> _statistics = new();
        private string _selectedImportStrategy = "merge";
        private bool _includeLocalData = true;
        private bool _encryptBackup = false;
        private string _backupPassword = string.Empty;
        private bool _syncEnabled = false;
        private DateTime? _lastSyncDate = null;

        /// <summary>
        /// Constructeur du ViewModel des paramètres.
        /// </summary>
        public SettingsViewModel(DataSyncService dataSyncService)
        {
            _dataSyncService = dataSyncService ?? throw new ArgumentNullException(nameof(dataSyncService));
            
            ExportDataCommand = new DelegateCommand(ExportData);
            ImportDataCommand = new DelegateCommand(ImportData);
            BackupDatabaseCommand = new DelegateCommand(BackupDatabase);
            RestoreDatabaseCommand = new DelegateCommand(RestoreDatabase);
            RefreshStatisticsCommand = new DelegateCommand(RefreshStatistics);
            SynchronizeDataCommand = new DelegateCommand(SynchronizeData);
            
            // Initialiser les stratégies d'importation disponibles
            ImportStrategies = new ObservableCollection<string> { "merge", "replace", "keepExisting" };
            ImportStrategyDescriptions = new Dictionary<string, string>
            {
                { "merge", "Fusion : Importer les nouvelles données et mettre à jour les existantes" },
                { "replace", "Remplacer : Effacer les données existantes et les remplacer" },
                { "keepExisting", "Conserver l'existant : N'importer que les nouvelles données" }
            };
            
            // Charger les statistiques au démarrage
            RefreshStatistics();
        }

        /// <summary>
        /// Commande pour exporter les données utilisateur.
        /// </summary>
        public DelegateCommand ExportDataCommand { get; }

        /// <summary>
        /// Commande pour importer les données utilisateur.
        /// </summary>
        public DelegateCommand ImportDataCommand { get; }

        /// <summary>
        /// Commande pour créer une sauvegarde de la base de données.
        /// </summary>
        public DelegateCommand BackupDatabaseCommand { get; }

        /// <summary>
        /// Commande pour restaurer une sauvegarde de la base de données.
        /// </summary>
        public DelegateCommand RestoreDatabaseCommand { get; }

        /// <summary>
        /// Commande pour rafraîchir les statistiques.
        /// </summary>
        public DelegateCommand RefreshStatisticsCommand { get; }
        
        /// <summary>
        /// Commande pour synchroniser les données avec le serveur.
        /// </summary>
        public DelegateCommand SynchronizeDataCommand { get; }

        /// <summary>
        /// Liste des stratégies d'importation disponibles.
        /// </summary>
        public ObservableCollection<string> ImportStrategies { get; }
        
        /// <summary>
        /// Descriptions des stratégies d'importation.
        /// </summary>
        public Dictionary<string, string> ImportStrategyDescriptions { get; }
        
        /// <summary>
        /// Stratégie d'importation sélectionnée.
        /// </summary>
        public string SelectedImportStrategy
        {
            get => _selectedImportStrategy;
            set => SetProperty(ref _selectedImportStrategy, value);
        }
        
        /// <summary>
        /// Indique si les données locales doivent être incluses dans l'exportation.
        /// </summary>
        public bool IncludeLocalData
        {
            get => _includeLocalData;
            set => SetProperty(ref _includeLocalData, value);
        }
        
        /// <summary>
        /// Indique si la sauvegarde doit être cryptée.
        /// </summary>
        public bool EncryptBackup
        {
            get => _encryptBackup;
            set => SetProperty(ref _encryptBackup, value);
        }
        
        /// <summary>
        /// Mot de passe pour le cryptage de la sauvegarde.
        /// </summary>
        public string BackupPassword
        {
            get => _backupPassword;
            set => SetProperty(ref _backupPassword, value);
        }
        
        /// <summary>
        /// Indique si la synchronisation est activée.
        /// </summary>
        public bool SyncEnabled
        {
            get => _syncEnabled;
            set => SetProperty(ref _syncEnabled, value);
        }
        
        /// <summary>
        /// Date de la dernière synchronisation.
        /// </summary>
        public DateTime? LastSyncDate
        {
            get => _lastSyncDate;
            set => SetProperty(ref _lastSyncDate, value);
        }

        /// <summary>
        /// Indique si une opération est en cours.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Message de statut de la dernière opération.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Indique si le statut est un succès.
        /// </summary>
        public bool IsStatusSuccess
        {
            get => _isStatusSuccess;
            set => SetProperty(ref _isStatusSuccess, value);
        }

        /// <summary>
        /// Statistiques sur les données utilisateur.
        /// </summary>
        public ObservableCollection<KeyValuePair<string, int>> Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        /// <summary>
        /// Exporte les données utilisateur dans un fichier JSON.
        /// </summary>
        private async void ExportData()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Fichier JSON (*.json)|*.json",
                Title = "Exporter les données",
                FileName = $"RecipeHub_Export_{DateTime.Now:yyyyMMdd}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Exportation en cours...";
                IsStatusSuccess = false;

                try
                {
                    bool success;
                    
                    if (EncryptBackup && !string.IsNullOrEmpty(BackupPassword))
                    {
                        // Exportation cryptée
                        success = await _dataSyncService.ExportEncryptedUserDataAsync(dialog.FileName, BackupPassword);
                        StatusMessage = success
                            ? "Exportation cryptée réussie!"
                            : "Erreur lors de l'exportation cryptée des données.";
                    }
                    else
                    {
                        // Exportation standard
                        success = await _dataSyncService.ExportUserDataToJsonAsync(dialog.FileName, IncludeLocalData);
                        StatusMessage = success
                            ? "Exportation réussie!"
                            : "Erreur lors de l'exportation des données.";
                    }
                    
                    IsStatusSuccess = success;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erreur: {ex.Message}";
                    IsStatusSuccess = false;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Importe les données utilisateur depuis un fichier JSON.
        /// </summary>
        private async void ImportData()
        {
            var result = MessageBox.Show(
                "L'importation de données peut remplacer certaines de vos données existantes. Voulez-vous continuer?",
                "Confirmation d'importation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result != MessageBoxResult.Yes)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Fichier JSON (*.json)|*.json",
                Title = "Importer des données"
            };

            if (dialog.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Importation en cours...";
                IsStatusSuccess = false;

                try
                {
                    var importResult = await _dataSyncService.ImportUserDataFromJsonAsync(dialog.FileName, SelectedImportStrategy);
                    
                    StatusMessage = importResult.Message;
                    IsStatusSuccess = importResult.Success;
                    
                    if (importResult.Success)
                    {
                        // Rafraîchir les statistiques après une importation réussie
                        RefreshStatistics();
                        
                        MessageBox.Show(
                            $"Importation réussie avec la stratégie '{SelectedImportStrategy}'.",
                            "Importation terminée",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erreur: {ex.Message}";
                    IsStatusSuccess = false;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Crée une sauvegarde de la base de données.
        /// </summary>
        private void BackupDatabase()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Fichier de base de données SQLite (*.db)|*.db",
                Title = "Sauvegarder la base de données",
                FileName = $"RecipeHub_Backup_{DateTime.Now:yyyyMMdd}.db"
            };

            if (dialog.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Sauvegarde en cours...";
                IsStatusSuccess = false;

                try
                {
                    bool success = _dataSyncService.BackupDatabase(dialog.FileName);
                    
                    StatusMessage = success 
                        ? "Sauvegarde réussie!" 
                        : "Erreur lors de la sauvegarde de la base de données.";
                    IsStatusSuccess = success;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erreur: {ex.Message}";
                    IsStatusSuccess = false;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Restaure une sauvegarde de la base de données.
        /// </summary>
        private void RestoreDatabase()
        {
            var result = MessageBox.Show(
                "La restauration d'une sauvegarde remplacera TOUTES vos données actuelles. Cette opération ne peut pas être annulée. Voulez-vous continuer?",
                "Confirmation de restauration",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result != MessageBoxResult.Yes)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Fichier de base de données SQLite (*.db)|*.db",
                Title = "Restaurer une sauvegarde"
            };

            if (dialog.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Restauration en cours...";
                IsStatusSuccess = false;

                try
                {
                    bool success = _dataSyncService.RestoreDatabase(dialog.FileName);
                    
                    StatusMessage = success 
                        ? "Restauration réussie! Veuillez redémarrer l'application." 
                        : "Erreur lors de la restauration de la base de données.";
                    IsStatusSuccess = success;
                    
                    if (success)
                    {
                        // Rafraîchir les statistiques après une restauration réussie
                        RefreshStatistics();
                        
                        // Informer l'utilisateur qu'il doit redémarrer l'application
                        MessageBox.Show(
                            "La base de données a été restaurée avec succès. Veuillez redémarrer l'application pour appliquer les changements.",
                            "Restauration réussie",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erreur: {ex.Message}";
                    IsStatusSuccess = false;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Synchronise les données avec le serveur distant.
        /// </summary>
        private async void SynchronizeData()
        {
            IsBusy = true;
            StatusMessage = "Synchronisation en cours...";
            IsStatusSuccess = false;

            try
            {
                var syncResult = await _dataSyncService.SynchronizeDataAsync();
                
                StatusMessage = syncResult.Message;
                IsStatusSuccess = syncResult.Success;
                
                if (syncResult.Success)
                {
                    // Mettre à jour la date de dernière synchronisation
                    LastSyncDate = DateTime.Now;
                    
                    // Rafraîchir les statistiques après une synchronisation réussie
                    RefreshStatistics();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors de la synchronisation: {ex.Message}";
                IsStatusSuccess = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Rafraîchit les statistiques sur les données utilisateur.
        /// </summary>
        private async void RefreshStatistics()
        {
            IsBusy = true;
            
            try
            {
                var stats = await _dataSyncService.GetUserDataStatisticsAsync();
                
                var formattedStats = new ObservableCollection<KeyValuePair<string, int>>();
                
                // Convertir les clés techniques en noms lisibles
                foreach (var stat in stats)
                {
                    string displayName = stat.Key switch
                    {
                        "FavoritesCount" => "Recettes favorites",
                        "CollectionsCount" => "Collections",
                        "MealPlansCount" => "Plans de repas",
                        "PlannedMealsCount" => "Repas planifiés",
                        "ShoppingListsCount" => "Listes de courses",
                        "ShoppingItemsCount" => "Articles de courses",
                        "DatabaseSizeKB" => "Taille de la base de données (KB)",
                        "LastSyncDate" => "Dernière synchronisation",
                        _ => stat.Key
                    };
                    
                    formattedStats.Add(new KeyValuePair<string, int>(displayName, stat.Value));
                }
                
                Statistics = formattedStats;
                
                // Si on a une timestamp de dernière synchronisation, la convertir en DateTime
                if (stats.TryGetValue("LastSyncDate", out int lastSyncTimestamp) && lastSyncTimestamp > 0)
                {
                    LastSyncDate = DateTimeOffset.FromUnixTimeSeconds(lastSyncTimestamp).DateTime;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors du chargement des statistiques: {ex.Message}";
                IsStatusSuccess = false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
