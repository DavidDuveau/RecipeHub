using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RecipeHub.UI.ViewModels
{
    /// <summary>
    /// ViewModel pour la gestion des recettes favorites.
    /// </summary>
    public class FavoritesViewModel : BindableBase, INavigationAware
    {
        private readonly IFavoritesService _favoritesService;
        private readonly IRegionManager _regionManager;
        private ObservableCollection<Recipe> _favoriteRecipes = new ObservableCollection<Recipe>();
        private ObservableCollection<string> _collections = new ObservableCollection<string>();
        private bool _isLoading;
        private string _errorMessage;
        private string _searchText;
        private string _selectedCollection;
        private bool _hasNoFavorites;

        /// <summary>
        /// Collection des recettes favorites.
        /// </summary>
        public ObservableCollection<Recipe> FavoriteRecipes
        {
            get => _favoriteRecipes;
            set => SetProperty(ref _favoriteRecipes, value);
        }

        /// <summary>
        /// Collections disponibles pour l'utilisateur.
        /// </summary>
        public ObservableCollection<string> Collections
        {
            get => _collections;
            set => SetProperty(ref _collections, value);
        }

        /// <summary>
        /// Indique si les données sont en cours de chargement.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Message d'erreur à afficher en cas de problème.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Texte de recherche pour filtrer les recettes favorites.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterRecipes();
                }
            }
        }

        /// <summary>
        /// Collection actuellement sélectionnée pour le filtrage.
        /// </summary>
        public string SelectedCollection
        {
            get => _selectedCollection;
            set
            {
                if (SetProperty(ref _selectedCollection, value))
                {
                    LoadRecipesByCollection();
                }
            }
        }

        /// <summary>
        /// Indique si l'utilisateur n'a aucune recette favorite.
        /// </summary>
        public bool HasNoFavorites
        {
            get => _hasNoFavorites;
            set => SetProperty(ref _hasNoFavorites, value);
        }

        /// <summary>
        /// Commande pour supprimer une recette des favoris.
        /// </summary>
        public ICommand RemoveFavoriteCommand { get; }

        /// <summary>
        /// Commande pour voir les détails d'une recette.
        /// </summary>
        public ICommand ViewRecipeDetailsCommand { get; }

        /// <summary>
        /// Commande pour naviguer vers l'explorateur de recettes.
        /// </summary>
        public ICommand NavigateToExploreCommand { get; }

        /// <summary>
        /// Commande pour rafraîchir la liste des favoris.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Constructeur du ViewModel pour la gestion des favoris.
        /// </summary>
        /// <param name="favoritesService">Service de gestion des favoris</param>
        /// <param name="regionManager">Gestionnaire de régions pour la navigation</param>
        public FavoritesViewModel(IFavoritesService favoritesService, IRegionManager regionManager)
        {
            _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // Initialisation des commandes
            RemoveFavoriteCommand = new DelegateCommand<Recipe>(RemoveFavorite);
            ViewRecipeDetailsCommand = new DelegateCommand<Recipe>(ViewRecipeDetails);
            NavigateToExploreCommand = new DelegateCommand(NavigateToExplore);
            RefreshCommand = new DelegateCommand(async () => await LoadFavoritesAsync());

            // Ajouter l'option "Toutes les recettes" aux collections
            Collections.Add("Toutes les recettes");
            SelectedCollection = "Toutes les recettes";
        }

        /// <summary>
        /// Méthode appelée lors de la navigation vers cette vue.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadFavoritesAsync().ConfigureAwait(false);
            LoadCollectionsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Détermine si cette vue peut être la cible d'une navigation.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        /// <returns>True si la navigation est autorisée, False sinon</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <summary>
        /// Méthode appelée lorsque la navigation quitte cette vue.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // Rien à faire lors de la navigation depuis cette vue
        }

        /// <summary>
        /// Charge les recettes favorites de l'utilisateur.
        /// </summary>
        private async Task LoadFavoritesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var favorites = await _favoritesService.GetAllFavoritesAsync();
                
                FavoriteRecipes.Clear();
                foreach (var recipe in favorites)
                {
                    FavoriteRecipes.Add(recipe);
                }

                HasNoFavorites = FavoriteRecipes.Count == 0;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des favoris : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les collections de l'utilisateur.
        /// </summary>
        private async Task LoadCollectionsAsync()
        {
            try
            {
                var collections = await _favoritesService.GetCollectionsAsync();
                
                Collections.Clear();
                Collections.Add("Toutes les recettes");
                
                foreach (var collection in collections)
                {
                    Collections.Add(collection);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des collections : {ex.Message}";
            }
        }

        /// <summary>
        /// Charge les recettes d'une collection spécifique.
        /// </summary>
        private async void LoadRecipesByCollection()
        {
            if (string.IsNullOrEmpty(SelectedCollection) || SelectedCollection == "Toutes les recettes")
            {
                await LoadFavoritesAsync();
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var recipes = await _favoritesService.GetRecipesByCollectionAsync(SelectedCollection);
                
                FavoriteRecipes.Clear();
                foreach (var recipe in recipes)
                {
                    FavoriteRecipes.Add(recipe);
                }

                HasNoFavorites = FavoriteRecipes.Count == 0;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des recettes de la collection : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Filtre les recettes en fonction du texte de recherche.
        /// </summary>
        private void FilterRecipes()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Si la recherche est vide, recharger les recettes selon la collection sélectionnée
                LoadRecipesByCollection();
                return;
            }

            // Filtrer les recettes existantes selon le texte de recherche
            var filtered = FavoriteRecipes.Where(r => 
                r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Area.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Tags.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            FavoriteRecipes.Clear();
            foreach (var recipe in filtered)
            {
                FavoriteRecipes.Add(recipe);
            }

            HasNoFavorites = FavoriteRecipes.Count == 0;
        }

        /// <summary>
        /// Supprime une recette des favoris.
        /// </summary>
        /// <param name="recipe">Recette à supprimer des favoris</param>
        private async void RemoveFavorite(Recipe recipe)
        {
            if (recipe == null)
                return;

            try
            {
                bool success = await _favoritesService.RemoveFavoriteAsync(recipe.Id);
                if (success)
                {
                    FavoriteRecipes.Remove(recipe);
                    HasNoFavorites = FavoriteRecipes.Count == 0;
                }
                else
                {
                    ErrorMessage = $"Impossible de supprimer la recette des favoris.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la suppression du favori : {ex.Message}";
            }
        }

        /// <summary>
        /// Navigue vers les détails d'une recette.
        /// </summary>
        /// <param name="recipe">Recette à afficher</param>
        private void ViewRecipeDetails(Recipe recipe)
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
        /// Navigue vers l'explorateur de recettes.
        /// </summary>
        private void NavigateToExplore()
        {
            _regionManager.RequestNavigate("ContentRegion", "ExploreView");
        }
    }
}