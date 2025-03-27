using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RecipeHub.UI.ViewModels
{
    /// <summary>
    /// ViewModel pour la vue d'exploration des recettes par catégories, régions et ingrédients.
    /// </summary>
    public class ExploreViewModel : BindableBase, INavigationAware
    {
        private readonly IMealDbService _mealDbService;
        private readonly IFavoritesService _favoritesService;
        private readonly IRegionManager _regionManager;
        private bool _isLoading;
        private string _errorMessage;
        private int _selectedTabIndex;
        private string _selectedTab = "categories";
        private Category _selectedCategory;
        private string _selectedArea;
        private string _selectedIngredient;
        private string _ingredientFilter = string.Empty;
        private string _recipesTitle = "Recettes";
        private bool _isNoRecipesSelected = true;

        /// <summary>
        /// Collection des catégories disponibles.
        /// </summary>
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

        /// <summary>
        /// Collection des régions (aires) culinaires disponibles.
        /// </summary>
        public ObservableCollection<string> Areas { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Collection des ingrédients disponibles.
        /// </summary>
        public ObservableCollection<string> Ingredients { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Collection des ingrédients filtrés (selon la recherche).
        /// </summary>
        public ObservableCollection<string> FilteredIngredients { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Collection des recettes à afficher selon le filtre actif.
        /// </summary>
        public ObservableCollection<Recipe> FilteredRecipes { get; } = new ObservableCollection<Recipe>();

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
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    // Mettre à jour le nom de l'onglet en fonction de l'index
                    switch (value)
                    {
                        case 0:
                            SelectedTab = "categories";
                            break;
                        case 1:
                            SelectedTab = "areas";
                            break;
                        case 2:
                            SelectedTab = "ingredients";
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Onglet actuellement sélectionné (categories, areas, ingredients).
        /// </summary>
        public string SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    // Réinitialiser les sélections précédentes
                    SelectedCategory = null;
                    SelectedArea = null;
                    SelectedIngredient = null;
                    FilteredRecipes.Clear();
                    IsNoRecipesSelected = true;

                    // Charger les données de l'onglet sélectionné s'il n'y en a pas
                    _ = LoadTabDataAsync(value);
                }
            }
        }

        /// <summary>
        /// Catégorie actuellement sélectionnée.
        /// </summary>
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        /// <summary>
        /// Région (aire) culinaire actuellement sélectionnée.
        /// </summary>
        public string SelectedArea
        {
            get => _selectedArea;
            set => SetProperty(ref _selectedArea, value);
        }

        /// <summary>
        /// Ingrédient actuellement sélectionné.
        /// </summary>
        public string SelectedIngredient
        {
            get => _selectedIngredient;
            set => SetProperty(ref _selectedIngredient, value);
        }

        /// <summary>
        /// Filtre pour rechercher parmi les ingrédients.
        /// </summary>
        public string IngredientFilter
        {
            get => _ingredientFilter;
            set
            {
                if (SetProperty(ref _ingredientFilter, value))
                {
                    FilterIngredients();
                }
            }
        }

        /// <summary>
        /// Titre de la section des recettes.
        /// </summary>
        public string RecipesTitle
        {
            get => _recipesTitle;
            set => SetProperty(ref _recipesTitle, value);
        }

        /// <summary>
        /// Indique si aucune recette n'est sélectionnée.
        /// </summary>
        public bool IsNoRecipesSelected
        {
            get => _isNoRecipesSelected;
            set => SetProperty(ref _isNoRecipesSelected, value);
        }

        /// <summary>
        /// Commande pour sélectionner une catégorie.
        /// </summary>
        public ICommand CategorySelectedCommand { get; }

        /// <summary>
        /// Commande pour sélectionner une région.
        /// </summary>
        public ICommand AreaSelectedCommand { get; }

        /// <summary>
        /// Commande pour sélectionner un ingrédient.
        /// </summary>
        public ICommand IngredientSelectedCommand { get; }

        /// <summary>
        /// Commande pour filtrer les ingrédients.
        /// </summary>
        public ICommand FilterIngredientsCommand { get; }

        /// <summary>
        /// Commande pour naviguer vers les détails d'une recette.
        /// </summary>
        public ICommand ViewRecipeDetailsCommand { get; }

        /// <summary>
        /// Commande pour marquer/démarquer une recette comme favorite.
        /// </summary>
        public ICommand ToggleFavoriteCommand { get; }

        /// <summary>
        /// Commande pour actualiser les données.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Constructeur du ViewModel de la vue d'exploration.
        /// </summary>
        /// <param name="mealDbService">Service d'accès à l'API TheMealDB</param>
        /// <param name="favoritesService">Service de gestion des favoris</param>
        /// <param name="regionManager">Gestionnaire de régions pour la navigation</param>
        public ExploreViewModel(IMealDbService mealDbService, IFavoritesService favoritesService, IRegionManager regionManager)
        {
            _mealDbService = mealDbService ?? throw new ArgumentNullException(nameof(mealDbService));
            _favoritesService = favoritesService ?? throw new ArgumentNullException(nameof(favoritesService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // Initialisation des commandes
            CategorySelectedCommand = new DelegateCommand<Category>(CategorySelected);
            AreaSelectedCommand = new DelegateCommand<string>(AreaSelected);
            IngredientSelectedCommand = new DelegateCommand<string>(IngredientSelected);
            FilterIngredientsCommand = new DelegateCommand(FilterIngredients);
            ViewRecipeDetailsCommand = new DelegateCommand<Recipe>(ViewRecipeDetails);
            ToggleFavoriteCommand = new DelegateCommand<Recipe>(ToggleFavorite);
            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
        }

        /// <summary>
        /// Gère la sélection d'une catégorie.
        /// </summary>
        /// <param name="category">Catégorie sélectionnée</param>
        private async void CategorySelected(Category category)
        {
            if (category == null) return;

            SelectedCategory = category;
            RecipesTitle = $"Recettes de {category.Name}";
            await LoadRecipesByCategoryAsync(category.Name);
        }

        /// <summary>
        /// Gère la sélection d'une région.
        /// </summary>
        /// <param name="area">Région sélectionnée</param>
        private async void AreaSelected(string area)
        {
            if (string.IsNullOrEmpty(area)) return;

            SelectedArea = area;
            RecipesTitle = $"Cuisine {area}";
            await LoadRecipesByAreaAsync(area);
        }

        /// <summary>
        /// Gère la sélection d'un ingrédient.
        /// </summary>
        /// <param name="ingredient">Ingrédient sélectionné</param>
        private async void IngredientSelected(string ingredient)
        {
            if (string.IsNullOrEmpty(ingredient)) return;

            SelectedIngredient = ingredient;
            RecipesTitle = $"Recettes avec {ingredient}";
            await LoadRecipesByIngredientAsync(ingredient);
        }

        /// <summary>
        /// Filtre les ingrédients selon le texte saisi.
        /// </summary>
        private void FilterIngredients()
        {
            FilteredIngredients.Clear();

            if (string.IsNullOrWhiteSpace(IngredientFilter))
            {
                // Si le filtre est vide, montrer tous les ingrédients
                foreach (var ingredient in Ingredients.Take(50)) // Limite pour éviter de surcharger l'UI
                {
                    FilteredIngredients.Add(ingredient);
                }
            }
            else
            {
                // Filtrer par nom
                var filter = IngredientFilter.Trim().ToLowerInvariant();
                foreach (var ingredient in Ingredients.Where(i => i.ToLowerInvariant().Contains(filter)).Take(50))
                {
                    FilteredIngredients.Add(ingredient);
                }
            }
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
        /// Marque ou démarque une recette comme favorite.
        /// </summary>
        /// <param name="recipe">Recette à marquer/démarquer</param>
        private async void ToggleFavorite(Recipe recipe)
        {
            if (recipe == null) return;

            try
            {
                if (recipe.IsFavorite)
                {
                    // Supprimer des favoris
                    await _favoritesService.RemoveFavoriteAsync(recipe.Id);
                    recipe.IsFavorite = false;
                }
                else
                {
                    // Ajouter aux favoris
                    await _favoritesService.AddFavoriteAsync(recipe);
                    recipe.IsFavorite = true;
                }

                // Notifier que la propriété IsFavorite a changé
                // Pour mettre à jour l'icône dans la vue
                RaisePropertyChanged(nameof(FilteredRecipes));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors de la mise à jour des favoris : {ex.Message}";
            }
        }

        /// <summary>
        /// Charge les données initiales de la vue.
        /// </summary>
        private async Task LoadDataAsync()
        {
            await LoadTabDataAsync(SelectedTab);
        }

        /// <summary>
        /// Charge les données pour l'onglet spécifié.
        /// </summary>
        /// <param name="tabName">Nom de l'onglet à charger</param>
        private async Task LoadTabDataAsync(string tabName)
        {
            switch (tabName.ToLowerInvariant())
            {
                case "categories":
                    await LoadCategoriesAsync();
                    break;
                case "areas":
                    await LoadAreasAsync();
                    break;
                case "ingredients":
                    await LoadIngredientsAsync();
                    break;
            }
        }

        /// <summary>
        /// Charge la liste des catégories.
        /// </summary>
        private async Task LoadCategoriesAsync()
        {
            if (Categories.Any())
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var categories = await _mealDbService.GetCategoriesAsync();
                Categories.Clear();
                foreach (var category in categories.OrderBy(c => c.Name))
                {
                    Categories.Add(category);
                }

                // Initialiser les ingrédients filtrés si c'est la première fois
                if (!FilteredIngredients.Any() && Ingredients.Any())
                {
                    FilterIngredients();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des catégories : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge la liste des régions (aires) culinaires.
        /// </summary>
        private async Task LoadAreasAsync()
        {
            if (Areas.Any())
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var areas = await _mealDbService.GetAreasAsync();
                Areas.Clear();
                foreach (var area in areas.OrderBy(a => a))
                {
                    Areas.Add(area);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des régions : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge la liste des ingrédients.
        /// </summary>
        private async Task LoadIngredientsAsync()
        {
            if (Ingredients.Any())
            {
                // Si les ingrédients sont déjà chargés mais pas les ingrédients filtrés
                if (!FilteredIngredients.Any())
                {
                    FilterIngredients();
                }
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var ingredients = await _mealDbService.GetIngredientsAsync();
                Ingredients.Clear();
                foreach (var ingredient in ingredients.OrderBy(i => i))
                {
                    Ingredients.Add(ingredient);
                }

                // Initialiser les ingrédients filtrés
                FilterIngredients();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des ingrédients : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les recettes d'une catégorie spécifique.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        private async Task LoadRecipesByCategoryAsync(string category)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                FilteredRecipes.Clear();

                var recipes = await _mealDbService.GetRecipesByCategoryAsync(category);
                foreach (var recipe in recipes)
                {
                    FilteredRecipes.Add(recipe);
                }

                IsNoRecipesSelected = !FilteredRecipes.Any();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des recettes : {ex.Message}";
                IsNoRecipesSelected = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les recettes d'une région spécifique.
        /// </summary>
        /// <param name="area">Nom de la région</param>
        private async Task LoadRecipesByAreaAsync(string area)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                FilteredRecipes.Clear();

                var recipes = await _mealDbService.GetRecipesByAreaAsync(area);
                foreach (var recipe in recipes)
                {
                    FilteredRecipes.Add(recipe);
                }

                IsNoRecipesSelected = !FilteredRecipes.Any();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des recettes : {ex.Message}";
                IsNoRecipesSelected = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Charge les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        private async Task LoadRecipesByIngredientAsync(string ingredient)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                FilteredRecipes.Clear();

                var recipes = await _mealDbService.GetRecipesByIngredientAsync(ingredient);
                foreach (var recipe in recipes)
                {
                    FilteredRecipes.Add(recipe);
                }

                IsNoRecipesSelected = !FilteredRecipes.Any();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur lors du chargement des recettes : {ex.Message}";
                IsNoRecipesSelected = true;
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
            return true;
        }

        /// <summary>
        /// Appelé lorsque la navigation quitte cette vue.
        /// </summary>
        /// <param name="navigationContext">Contexte de navigation</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // Rien à faire lorsqu'on quitte la vue d'exploration
        }
    }
}
