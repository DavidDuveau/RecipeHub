using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows.Input;

namespace RecipeHub.UI.ViewModels
{
    /// <summary>
    /// ViewModel pour le dialogue de création de collection.
    /// </summary>
    public class CreateCollectionDialogViewModel : BindableBase
    {
        private string _collectionName;
        private string _errorMessage;
        private readonly Action<string> _createCallback;
        private readonly Action _cancelCallback;

        /// <summary>
        /// Constructeur du ViewModel.
        /// </summary>
        /// <param name="createCallback">Callback à appeler lors de la création</param>
        /// <param name="cancelCallback">Callback à appeler lors de l'annulation</param>
        public CreateCollectionDialogViewModel(Action<string> createCallback, Action cancelCallback)
        {
            _createCallback = createCallback ?? throw new ArgumentNullException(nameof(createCallback));
            _cancelCallback = cancelCallback ?? throw new ArgumentNullException(nameof(cancelCallback));

            CreateCommand = new DelegateCommand(ExecuteCreate, CanExecuteCreate)
                .ObservesProperty(() => CollectionName);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        /// <summary>
        /// Nom de la collection à créer.
        /// </summary>
        public string CollectionName
        {
            get => _collectionName;
            set => SetProperty(ref _collectionName, value);
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
        /// Commande pour créer la collection.
        /// </summary>
        public ICommand CreateCommand { get; }

        /// <summary>
        /// Commande pour annuler la création.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Vérifie si la commande de création peut être exécutée.
        /// </summary>
        private bool CanExecuteCreate()
        {
            return !string.IsNullOrWhiteSpace(CollectionName) && CollectionName.Trim().Length >= 3;
        }

        /// <summary>
        /// Exécute la commande de création.
        /// </summary>
        private void ExecuteCreate()
        {
            if (CanExecuteCreate())
            {
                _createCallback(CollectionName.Trim());
            }
            else
            {
                ErrorMessage = "Le nom de la collection doit contenir au moins 3 caractères.";
            }
        }

        /// <summary>
        /// Exécute la commande d'annulation.
        /// </summary>
        private void ExecuteCancel()
        {
            _cancelCallback();
        }
    }
}
