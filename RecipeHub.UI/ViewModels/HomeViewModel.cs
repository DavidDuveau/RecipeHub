using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RecipeHub.UI.ViewModels
{
    /// <summary>
    /// ViewModel pour la page d'accueil de l'application.
    /// </summary>
    public class HomeViewModel : BindableBase, INavigationAware
    {
        private readonly IMealDbService _mealDbService;
        private readonly IRegionManager _regionManager;
        private bool _isLoading;
        private string _errorMessage;

        /// <summary>
        /// Collection de recettes populaires à afficher.
        /// </summary>
        public ObservableCollection<Recipe> PopularRecipes { get; } = new ObservableCollection<Recipe>();

        /// <summary>
        /// Collection de recettes récentes à afficher.
        /// </summary>
        public ObservableCollection<Recipe> RecentRecipes { get; } = new ObservableCollection<Recipe>();

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
        /// Commande pour naviguer vers les détails d'une recette.
        /// </summary>
        public ICommand ViewRecipeDetailsCommand { get; }

        /// <summary>
        /// Commande pour actualiser les données.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Commande pour naviguer vers la page d'exploration.
        /// </summary>
        public ICommand ExploreCommand { get; }

        /// <summary>
        /// Constructeur du ViewModel de la page d'accueil.
        /// </summary>
        /// <param name="mealDbService">Service d'accès à l'API TheMealDB</param>
        /// <param name="regionManager">Gestionnaire de régions pour la navigation</param>
        public HomeViewModel(IMealDbService mealDbService, IRegionManager regionManager)
        {
            _mealDbService = mealDbService ?? throw new ArgumentNullException(nameof(mealDbService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            ViewRecipeDetailsCommand = new DelegateCommand<Recipe>(ViewRecipeDetails);
            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
            ExploreCommand = new DelegateCommand(() => _regionManager.RequestNavigate("ContentRegion", "ExploreView"));
        }

        /// <summary>
        /// Navigue vers la vue détaillée d'une recette.
        /// </summary>
        /// <param name="recipe">Recette à afficher</param>
        private void ViewRecipeDetails(Recipe recipe)
        {
            if (recipe == null) return;

            var parameters = new NavigationParameters
            {
                { "recipeId", recipe.Id }
            };

            _regionManager.RequestNavigate("ContentRegion", "RecipeDetailsView", parameters);
        }

        /// <summary>
        /// Charge les données à afficher sur la page d'accueil.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                PopularRecipes.Clear();
                RecentRecipes.Clear();

                // Pour l'exemple, nous utilisons des recettes aléatoires
                // Dans une application réelle, on pourrait avoir une API pour obtenir
                // les recettes populaires ou récentes
                var popularRecipes = await _mealDbService.GetRandomRecipesAsync(8);
                foreach (var recipe in popularRecipes)
                {
                    PopularRecipes.Add(recipe);
                }

                var recentRecipes = await _mealDbService.GetRandomRecipesAsync(4);
                foreach (var recipe in recentRecipes)
                {
                    RecentRecipes.Add(recipe);
                }
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
        /// Appelé lorsque la navigation vers cette vue est terminée.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// Détermine si cette vue peut être la cible d'une navigation.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        /// <returns>True si la navigation est autorisée, False sinon</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // La page d'accueil est toujours une cible de navigation valide
            return true;
        }

        /// <summary>
        /// Appelé lorsque la navigation quitte cette vue.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // Rien à faire lorsqu'on quitte la page d'accueil
        }
    }
}
