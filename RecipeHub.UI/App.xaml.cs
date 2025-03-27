using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using RecipeHub.Core.Interfaces;
using RecipeHub.Services.API;
using RecipeHub.Services.Cache;
using RecipeHub.Services.Data;
using RecipeHub.UI.Views;

namespace RecipeHub.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<ShellView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Enregistrement des services
            containerRegistry.RegisterSingleton<ICacheService, InMemoryCacheService>();
            containerRegistry.RegisterSingleton<IFavoritesService, FavoritesService>();
            containerRegistry.RegisterSingleton<IMealDbService, MealDbService>();
            
            // Enregistrement des vues pour la navigation
            // À activer progressivement lors du développement
            containerRegistry.RegisterForNavigation<HomeView>();
            // containerRegistry.RegisterForNavigation<ExploreView>();
            // containerRegistry.RegisterForNavigation<RecipeDetailsView>();
            // containerRegistry.RegisterForNavigation<FavoritesView>();
            // containerRegistry.RegisterForNavigation<SearchView>();
        }
    }
}
