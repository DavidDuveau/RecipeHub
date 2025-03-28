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
    /// ViewModel pour la vue détaillée d'une recette.
    /// </summary>
    public class RecipeDetailsViewModel : BindableBase, INavigationAware
    {
        private readonly IMealDbService _mealDbService;
        private readonly IFavoritesService _favoritesService;
        private readonly IRegionManager _regionManager;
        private Recipe _recipe;
        private bool _isLoading;
        private string _errorMessage;
        private int _selectedTabIndex;
        private ObservableCollection<Recipe> _similarRecipes = new ObservableCollection<Recipe>();
        
        /// <summary>
        /// Recette actuellement affichée.
        /// </summary>
        public Recipe Recipe
        {
            get => _recipe;
            set => SetProperty(ref _recipe, value);
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
        /// Index de l'onglet actuellement sélectionné.
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        /// <summary>
        /// Collection des recettes similaires.
        /// </summary>
        public ObservableCollection<Recipe> SimilarRecipes
        {
            get => _similarRecipes;
            set => SetProperty(ref _similarRecipes, value);
        }

        /// <summary>
        /// Commande pour ajouter ou supprimer la recette des favoris.
        /// </summary>
        public ICommand ToggleFavoriteCommand { get; }

        /// <summary>
        /// Commande pour retourner à la vue précédente.
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// Commande pour voir les détails d'une recette similaire.
        /// </summary>
        public ICommand ViewSimilarRecipeCommand { get; }

        /// <summary>
        /// Commande pour ajouter la recette à une collection.
        /// </summary>
        public ICommand AddToCollectionCommand { get; }

        /// <summary>
        /// Constructeur du ViewModel pour la vue détaillée d'une recette.
        /// </summary>
        /// <param name="mealDbService">Service d'accès à l'API TheMealDB</param>
        /// <param name="favoritesService">Service de gestion des favoris</param>
        /// <param name="regionManager">Gestionnaire de régions pour la navigation</param>
        public RecipeDetailsViewModel(IMealDbService mealDbService, IFavoritesService favoritesService, IRegionManager regionManager)
        {
            _mealDbService = mealDbService ?? throw new ArgumentNullException(nameof(mealDbService));
            _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // Initialisation des commandes
            ToggleFavoriteCommand = new DelegateCommand(ToggleFavorite);
            GoBackCommand = new DelegateCommand(GoBack);
            ViewSimilarRecipeCommand = new DelegateCommand<Recipe>(ViewSimilarRecipe);
            AddToCollectionCommand = new DelegateCommand<string>(AddToCollection);
        }

        /// <summary>
        /// Méthode appelée lors de la navigation vers cette vue.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Récupérer l'ID de la recette depuis les paramètres de navigation
            if (navigationContext.Parameters.ContainsKey("recipeId"))
            {
                int recipeId = navigationContext.Parameters.GetValue<int>("recipeId");
                await LoadRecipeAsync(recipeId);
            }
        }

        /// <summary>
        /// Détermine si cette vue peut être la cible d'une navigation.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        /// <returns>True si la navigation est autorisée, False sinon</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (Recipe == null || !navigationContext.Parameters.ContainsKey("recipeId"))
                return false;

            int recipeId = navigationContext.Parameters.GetValue<int>("recipeId");
            return Recipe.Id == recipeId;
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
        /// Charge les détails d'une recette et des recettes similaires.
        /// </summary>
        /// <param name="recipeId">ID de la recette à charger</param>
        private async Task LoadRecipeAsync(int recipeId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Charger les détails de la recette
                var recipe = await _mealDbService.GetRecipeByIdAsync(recipeId);
                if (recipe == null)
                {
                    ErrorMessage = "Impossible de charger la recette demandée.";
                    return;
                }

                Recipe = recipe;

                // Charger les recettes similaires (de la même catégorie)
                await LoadSimilarRecipesAsync(Recipe.Category);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement de la recette : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les recettes similaires (même catégorie que la recette actuelle).
        /// </summary>
        /// <param name="category">Catégorie de la recette actuelle</param>
        private async Task LoadSimilarRecipesAsync(string category)
        {
            if (string.IsNullOrEmpty(category))
                return;

            try
            {
                var similarRecipes = await _mealDbService.GetRecipesByCategoryAsync(category);
                
                // Filtrer pour exclure la recette actuelle et limiter le nombre
                var filteredRecipes = similarRecipes
                    .Where(r => r.Id != Recipe.Id)
                    .Take(6)
                    .ToList();

                SimilarRecipes.Clear();
                foreach (var recipe in filteredRecipes)
                {
                    SimilarRecipes.Add(recipe);
                }
            }
            catch (Exception ex)
            {
                // Nous ne définissons pas ErrorMessage ici pour ne pas remplacer les erreurs plus importantes
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des recettes similaires : {ex.Message}");
            }
        }

        /// <summary>
        /// Ajoute ou supprime la recette actuelle des favoris.
        /// </summary>
        private async void ToggleFavorite()
        {
            if (Recipe == null)
                return;

            try
            {
                if (Recipe.IsFavorite)
                {
                    // Supprimer des favoris
                    await _favoritesService.RemoveFavoriteAsync(Recipe.Id);
                    Recipe.IsFavorite = false;
                }
                else
                {
                    // Ajouter aux favoris
                    await _favoritesService.AddFavoriteAsync(Recipe);
                    Recipe.IsFavorite = true;
                }

                // Notifier que la propriété IsFavorite a changé
                RaisePropertyChanged(nameof(Recipe));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la mise à jour des favoris : {ex.Message}";
            }
        }

        /// <summary>
        /// Retourne à la vue précédente.
        /// </summary>
        private void GoBack()
        {
            _regionManager.RequestNavigate("ContentRegion", "ExploreView");
        }

        /// <summary>
        /// Navigue vers les détails d'une recette similaire.
        /// </summary>
        /// <param name="recipe">Recette similaire à afficher</param>
        private void ViewSimilarRecipe(Recipe recipe)
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
        /// Ajoute la recette actuelle à une collection.
        /// </summary>
        /// <param name="collectionName">Nom de la collection</param>
        private async void AddToCollection(string collectionName)
        {
            if (Recipe == null || string.IsNullOrEmpty(collectionName))
                return;

            try
            {
                bool success = await _favoritesService.AddToCollectionAsync(Recipe.Id, collectionName);
                if (success)
                {
                    // Mettre à jour les collections de la recette
                    if (!Recipe.Collections.Contains(collectionName))
                    {
                        Recipe.Collections.Add(collectionName);
                        RaisePropertyChanged(nameof(Recipe));
                    }
                }
                else
                {
                    ErrorMessage = $"Impossible d'ajouter la recette à la collection {collectionName}.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de l'ajout à la collection : {ex.Message}";
            }
        }
    }
}
