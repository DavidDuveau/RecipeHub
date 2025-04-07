using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using RecipeHub.UI.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RecipeHub.UI.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion des collections de recettes.
    /// </summary>
    public class CollectionsViewModel : BindableBase, INavigationAware
    {
        private readonly IFavoritesService _favoritesService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private string _selectedCollection;
        private Recipe _selectedRecipe;
        private string _searchText;
        private string _errorMessage;
        private bool _isLoading;
        private bool _isCreatingCollection;
        private string _newCollectionName;
        private string _editingCollectionName;
        private string _originalCollectionName;

        /// <summary>
        /// Constructeur du ViewModel.
        /// </summary>
        /// <param name="favoritesService">Service de gestion des favoris</param>
        /// <param name="regionManager">Gestionnaire de régions pour la navigation</param>
        /// <param name="eventAggregator">Agrégateur d'événements</param>
        public CollectionsViewModel(
            IFavoritesService favoritesService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator)
        {
            _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            Collections = new ObservableCollection<string>();
            CollectionRecipes = new ObservableCollection<Recipe>();

            // Commandes
            CreateCollectionCommand = new DelegateCommand(ExecuteCreateCollection);
            CancelCreateCollectionCommand = new DelegateCommand(ExecuteCancelCreateCollection);
            SaveNewCollectionCommand = new DelegateCommand(ExecuteSaveNewCollection, CanExecuteSaveNewCollection)
                .ObservesProperty(() => NewCollectionName);
            DeleteCollectionCommand = new DelegateCommand<string>(ExecuteDeleteCollection);
            EditCollectionCommand = new DelegateCommand<string>(ExecuteEditCollection);
            SaveEditCollectionCommand = new DelegateCommand(ExecuteSaveEditCollection, CanExecuteSaveEditCollection)
                .ObservesProperty(() => EditingCollectionName);
            CancelEditCollectionCommand = new DelegateCommand(ExecuteCancelEditCollection);
            ViewRecipeDetailsCommand = new DelegateCommand<Recipe>(ExecuteViewRecipeDetails);
            RemoveFromCollectionCommand = new DelegateCommand<Recipe>(ExecuteRemoveFromCollection);
            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
            NavigateToFavoritesCommand = new DelegateCommand(() =>
            {
                _regionManager.RequestNavigate("ContentRegion", "FavoritesView");
            });

            // S'abonner aux événements de changement des collections
            _eventAggregator.GetEvent<CollectionChangedEvent>().Subscribe(async (args) =>
            {
                // Rafraîchir la liste des collections
                await LoadCollectionsAsync();
                
                // Rafraîchir la collection actuelle si affichée
                if (!string.IsNullOrWhiteSpace(SelectedCollection))
                {
                    await LoadCollectionRecipesAsync(SelectedCollection);
                }
            });
        }

        #region Propriétés

        /// <summary>
        /// Liste des collections disponibles.
        /// </summary>
        public ObservableCollection<string> Collections { get; private set; }

        /// <summary>
        /// Liste des recettes dans la collection sélectionnée.
        /// </summary>
        public ObservableCollection<Recipe> CollectionRecipes { get; private set; }

        /// <summary>
        /// Collection actuellement sélectionnée.
        /// </summary>
        public string SelectedCollection
        {
            get => _selectedCollection;
            set
            {
                if (SetProperty(ref _selectedCollection, value))
                {
                    // Charger les recettes de la collection sélectionnée
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _ = LoadCollectionRecipesAsync(value);
                    }
                    else
                    {
                        CollectionRecipes.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Recette actuellement sélectionnée.
        /// </summary>
        public Recipe SelectedRecipe
        {
            get => _selectedRecipe;
            set => SetProperty(ref _selectedRecipe, value);
        }

        /// <summary>
        /// Texte de recherche pour filtrer les recettes.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Appliquer le filtre de recherche
                    ApplySearchFilter();
                }
            }
        }

        /// <summary>
        /// Message d'erreur à afficher.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Indique si une opération de chargement est en cours.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Indique si la création d'une nouvelle collection est en cours.
        /// </summary>
        public bool IsCreatingCollection
        {
            get => _isCreatingCollection;
            set => SetProperty(ref _isCreatingCollection, value);
        }

        /// <summary>
        /// Nom de la nouvelle collection en cours de création.
        /// </summary>
        public string NewCollectionName
        {
            get => _newCollectionName;
            set => SetProperty(ref _newCollectionName, value);
        }

        /// <summary>
        /// Nom de la collection en cours d'édition.
        /// </summary>
        public string EditingCollectionName
        {
            get => _editingCollectionName;
            set => SetProperty(ref _editingCollectionName, value);
        }

        /// <summary>
        /// Nom original de la collection en cours d'édition.
        /// </summary>
        public string OriginalCollectionName
        {
            get => _originalCollectionName;
            set => SetProperty(ref _originalCollectionName, value);
        }

        /// <summary>
        /// Indique si l'interface d'édition de collection est ouverte.
        /// </summary>
        public bool IsEditingCollection => !string.IsNullOrEmpty(OriginalCollectionName);

        /// <summary>
        /// Indique si la liste des collections est vide.
        /// </summary>
        public bool HasNoCollections => Collections.Count == 0;

        /// <summary>
        /// Indique si la collection sélectionnée est vide.
        /// </summary>
        public bool HasNoRecipesInCollection => !string.IsNullOrEmpty(SelectedCollection) && CollectionRecipes.Count == 0;

        #endregion

        #region Commandes

        /// <summary>
        /// Commande pour créer une nouvelle collection.
        /// </summary>
        public ICommand CreateCollectionCommand { get; }

        /// <summary>
        /// Commande pour annuler la création d'une collection.
        /// </summary>
        public ICommand CancelCreateCollectionCommand { get; }

        /// <summary>
        /// Commande pour sauvegarder une nouvelle collection.
        /// </summary>
        public ICommand SaveNewCollectionCommand { get; }

        /// <summary>
        /// Commande pour supprimer une collection.
        /// </summary>
        public ICommand DeleteCollectionCommand { get; }

        /// <summary>
        /// Commande pour modifier le nom d'une collection.
        /// </summary>
        public ICommand EditCollectionCommand { get; }

        /// <summary>
        /// Commande pour sauvegarder le nouveau nom d'une collection.
        /// </summary>
        public ICommand SaveEditCollectionCommand { get; }

        /// <summary>
        /// Commande pour annuler l'édition d'une collection.
        /// </summary>
        public ICommand CancelEditCollectionCommand { get; }

        /// <summary>
        /// Commande pour voir les détails d'une recette.
        /// </summary>
        public ICommand ViewRecipeDetailsCommand { get; }

        /// <summary>
        /// Commande pour retirer une recette d'une collection.
        /// </summary>
        public ICommand RemoveFromCollectionCommand { get; }

        /// <summary>
        /// Commande pour rafraîchir les données.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Commande pour naviguer vers la vue des favoris.
        /// </summary>
        public ICommand NavigateToFavoritesCommand { get; }

        #endregion

        #region Méthodes privées pour les commandes

        /// <summary>
        /// Ouvre l'interface de création de collection.
        /// </summary>
        private void ExecuteCreateCollection()
        {
            NewCollectionName = string.Empty;
            IsCreatingCollection = true;
        }

        /// <summary>
        /// Annule la création d'une collection.
        /// </summary>
        private void ExecuteCancelCreateCollection()
        {
            IsCreatingCollection = false;
            NewCollectionName = string.Empty;
        }

        /// <summary>
        /// Sauvegarde une nouvelle collection.
        /// </summary>
        private async void ExecuteSaveNewCollection()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _favoritesService.CreateCollectionAsync(NewCollectionName.Trim());
                if (result)
                {
                    // Rafraîchir la liste des collections
                    await LoadCollectionsAsync();
                    SelectedCollection = NewCollectionName.Trim();
                    
                    // Informer les autres vues du changement
                    _eventAggregator.GetEvent<CollectionChangedEvent>().Publish(new CollectionChangedEventArgs
                    {
                        CollectionName = NewCollectionName.Trim(),
                        Action = CollectionAction.Created
                    });
                    
                    IsCreatingCollection = false;
                    NewCollectionName = string.Empty;
                }
                else
                {
                    ErrorMessage = "Une collection avec ce nom existe déjà.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la création de la collection : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Vérifie si une nouvelle collection peut être créée.
        /// </summary>
        private bool CanExecuteSaveNewCollection()
        {
            return !string.IsNullOrWhiteSpace(NewCollectionName) && 
                   NewCollectionName.Trim().Length >= 3;
        }

        /// <summary>
        /// Supprime une collection.
        /// </summary>
        private async void ExecuteDeleteCollection(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _favoritesService.DeleteCollectionAsync(collectionName);
                if (result)
                {
                    // Si la collection supprimée était la collection sélectionnée
                    if (collectionName == SelectedCollection)
                    {
                        SelectedCollection = null;
                        CollectionRecipes.Clear();
                    }

                    // Rafraîchir la liste des collections
                    await LoadCollectionsAsync();
                    
                    // Informer les autres vues du changement
                    _eventAggregator.GetEvent<CollectionChangedEvent>().Publish(new CollectionChangedEventArgs
                    {
                        CollectionName = collectionName,
                        Action = CollectionAction.Deleted
                    });
                }
                else
                {
                    ErrorMessage = "Impossible de supprimer la collection.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la suppression de la collection : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Ouvre l'interface d'édition de collection.
        /// </summary>
        private void ExecuteEditCollection(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                return;

            OriginalCollectionName = collectionName;
            EditingCollectionName = collectionName;
            RaisePropertyChanged(nameof(IsEditingCollection));
        }

        /// <summary>
        /// Sauvegarde le nouveau nom d'une collection.
        /// </summary>
        private async void ExecuteSaveEditCollection()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _favoritesService.RenameCollectionAsync(
                    OriginalCollectionName,
                    EditingCollectionName.Trim());

                if (result)
                {
                    // Si la collection renommée était la collection sélectionnée
                    if (OriginalCollectionName == SelectedCollection)
                    {
                        SelectedCollection = EditingCollectionName.Trim();
                    }

                    // Rafraîchir la liste des collections
                    await LoadCollectionsAsync();
                    
                    // Informer les autres vues du changement
                    _eventAggregator.GetEvent<CollectionChangedEvent>().Publish(new CollectionChangedEventArgs
                    {
                        CollectionName = EditingCollectionName.Trim(),
                        OldCollectionName = OriginalCollectionName,
                        Action = CollectionAction.Renamed
                    });
                    
                    OriginalCollectionName = null;
                    EditingCollectionName = null;
                    RaisePropertyChanged(nameof(IsEditingCollection));
                }
                else
                {
                    ErrorMessage = "Une collection avec ce nom existe déjà ou la collection d'origine n'existe pas.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du renommage de la collection : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Vérifie si une collection peut être renommée.
        /// </summary>
        private bool CanExecuteSaveEditCollection()
        {
            return !string.IsNullOrWhiteSpace(EditingCollectionName) && 
                   EditingCollectionName.Trim().Length >= 3 &&
                   !string.Equals(OriginalCollectionName, EditingCollectionName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Annule l'édition d'une collection.
        /// </summary>
        private void ExecuteCancelEditCollection()
        {
            OriginalCollectionName = null;
            EditingCollectionName = null;
            RaisePropertyChanged(nameof(IsEditingCollection));
        }

        /// <summary>
        /// Affiche les détails d'une recette.
        /// </summary>
        private void ExecuteViewRecipeDetails(Recipe recipe)
        {
            if (recipe == null)
                return;

            var parameters = new NavigationParameters
            {
                { "recipeId", recipe.Id }
            };

            _regionManager.RequestNavigate("ContentRegion", "RecipeDetailsView", parameters);
        }

        /// <summary>
        /// Retire une recette d'une collection.
        /// </summary>
        private async void ExecuteRemoveFromCollection(Recipe recipe)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(SelectedCollection))
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _favoritesService.RemoveFromCollectionAsync(recipe.Id, SelectedCollection);
                if (result)
                {
                    // Rafraîchir la liste des recettes de la collection
                    await LoadCollectionRecipesAsync(SelectedCollection);
                    
                    // Informer les autres vues du changement
                    _eventAggregator.GetEvent<CollectionChangedEvent>().Publish(new CollectionChangedEventArgs
                    {
                        RecipeId = recipe.Id,
                        CollectionName = SelectedCollection,
                        Action = CollectionAction.RecipeRemoved
                    });
                }
                else
                {
                    ErrorMessage = "Impossible de retirer la recette de la collection.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du retrait de la recette : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Applique le filtre de recherche aux recettes de la collection.
        /// </summary>
        private async void ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(SelectedCollection))
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Récupérer les recettes complètes
                var allRecipes = await _favoritesService.GetRecipesByCollectionAsync(SelectedCollection);

                // Appliquer le filtre si nécessaire
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerms = SearchText.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    allRecipes = allRecipes.Where(r =>
                        searchTerms.All(term =>
                            r.Name.ToLowerInvariant().Contains(term) ||
                            r.Category.ToLowerInvariant().Contains(term) ||
                            r.Area.ToLowerInvariant().Contains(term) ||
                            r.Tags.Any(tag => tag.ToLowerInvariant().Contains(term)) ||
                            r.Ingredients.Any(i => i.Name.ToLowerInvariant().Contains(term))
                        )
                    ).ToList();
                }

                // Mettre à jour la liste
                CollectionRecipes.Clear();
                foreach (var recipe in allRecipes)
                {
                    CollectionRecipes.Add(recipe);
                }
                
                RaisePropertyChanged(nameof(HasNoRecipesInCollection));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de l'application du filtre : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Chargement des données

        /// <summary>
        /// Charge toutes les données (collections).
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await LoadCollectionsAsync();

                // Charger les recettes de la collection sélectionnée si elle existe
                if (!string.IsNullOrWhiteSpace(SelectedCollection))
                {
                    await LoadCollectionRecipesAsync(SelectedCollection);
                }

                RaisePropertyChanged(nameof(HasNoCollections));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des données : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge la liste des collections.
        /// </summary>
        private async Task LoadCollectionsAsync()
        {
            try
            {
                // Récupérer les collections
                var collections = await _favoritesService.GetCollectionsAsync();

                // Mettre à jour la liste observable
                Collections.Clear();
                foreach (var collection in collections.OrderBy(c => c))
                {
                    Collections.Add(collection);
                }

                RaisePropertyChanged(nameof(HasNoCollections));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des collections : {ex.Message}";
            }
        }

        /// <summary>
        /// Charge les recettes d'une collection spécifique.
        /// </summary>
        private async Task LoadCollectionRecipesAsync(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                return;

            try
            {
                IsLoading = true;

                // Récupérer les recettes de la collection
                var recipes = await _favoritesService.GetRecipesByCollectionAsync(collectionName);

                // Appliquer le filtre si nécessaire
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerms = SearchText.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    recipes = recipes.Where(r =>
                        searchTerms.All(term =>
                            r.Name.ToLowerInvariant().Contains(term) ||
                            r.Category.ToLowerInvariant().Contains(term) ||
                            r.Area.ToLowerInvariant().Contains(term) ||
                            r.Tags.Any(tag => tag.ToLowerInvariant().Contains(term)) ||
                            r.Ingredients.Any(i => i.Name.ToLowerInvariant().Contains(term))
                        )
                    ).ToList();
                }

                // Mettre à jour la liste observable
                CollectionRecipes.Clear();
                foreach (var recipe in recipes)
                {
                    CollectionRecipes.Add(recipe);
                }

                RaisePropertyChanged(nameof(HasNoRecipesInCollection));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des recettes : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Appelé lorsque la navigation vers cette vue est demandée.
        /// </summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Charger les données au démarrage
            _ = LoadDataAsync();

            // Si un paramètre de collection est fourni, sélectionner cette collection
            if (navigationContext.Parameters.ContainsKey("collectionName"))
            {
                string collectionName = navigationContext.Parameters.GetValue<string>("collectionName");
                if (!string.IsNullOrWhiteSpace(collectionName))
                {
                    SelectedCollection = collectionName;
                }
            }
        }

        /// <summary>
        /// Détermine si cette vue peut être la cible d'une navigation.
        /// </summary>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <summary>
        /// Appelé lorsque la navigation quitte cette vue.
        /// </summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // Rien à faire lors de la navigation vers une autre vue
        }

        #endregion
    }
}
