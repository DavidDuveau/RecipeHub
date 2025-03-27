using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
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
            // containerRegistry.Register<IMealDbService, MealDbService>();
            // containerRegistry.Register<IFavoritesService, FavoritesService>();
            
            // Enregistrement des vues pour la navigation
            // containerRegistry.RegisterForNavigation<HomeView>();
            // containerRegistry.RegisterForNavigation<ExploreView>();
            // containerRegistry.RegisterForNavigation<RecipeDetailsView>();
            // containerRegistry.RegisterForNavigation<FavoritesView>();
            // containerRegistry.RegisterForNavigation<SearchView>();
        }
    }
}
