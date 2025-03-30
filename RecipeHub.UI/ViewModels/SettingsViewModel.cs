using RecipeHub.Services.Data;
using RecipeHub.UI.Commands;
using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Mvvm;
using System.Collections.ObjectModel;

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
                    bool success = await _dataSyncService.ExportUserDataToJsonAsync(dialog.FileName);
                    
                    StatusMessage = success 
                        ? "Exportation réussie!" 
                        : "Erreur lors de l'exportation des données.";
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
                    var importResult = await _dataSyncService.ImportUserDataFromJsonAsync(dialog.FileName);
                    
                    StatusMessage = importResult.Message;
                    IsStatusSuccess = importResult.Success;
                    
                    if (importResult.Success)
                    {
                        // Rafraîchir les statistiques après une importation réussie
                        RefreshStatistics();
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
                        _ => stat.Key
                    };
                    
                    formattedStats.Add(new KeyValuePair<string, int>(displayName, stat.Value));
                }
                
                Statistics = formattedStats;
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
