using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace RecipeHub.UI.ViewModels
{
    /// <summary>
    /// ViewModel pour la vue principale de l'application.
    /// </summary>
    public class ShellViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        
        /// <summary>
        /// Titre de l'application affiché dans la barre de titre.
        /// </summary>
        private string _title = "RecipeHub - Découvrez des recettes du monde entier";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        
        /// <summary>
        /// Commande pour naviguer vers une vue spécifique.
        /// </summary>
        public DelegateCommand<string> NavigateCommand { get; private set; }
        
        /// <summary>
        /// Constructeur du ViewModel Shell.
        /// </summary>
        /// <param name="regionManager">Le gestionnaire de régions de Prism</param>
        public ShellViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string>(Navigate);
            
            // Naviguer vers la page d'accueil au démarrage
            _regionManager.RequestNavigate("ContentRegion", "HomeView");
        }
        
        /// <summary>
        /// Méthode de navigation vers les différentes vues.
        /// </summary>
        /// <param name="viewName">Nom de la vue vers laquelle naviguer</param>
        private void Navigate(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                return;
                
            _regionManager.RequestNavigate("ContentRegion", viewName);
        }
    }
}
