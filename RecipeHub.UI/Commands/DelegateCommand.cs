using System;
using System.Windows.Input;

namespace RecipeHub.UI.Commands
{
    /// <summary>
    /// Implémentation simple du pattern Command pouvant être utilisée 
    /// pour créer des commandes à partir de méthodes de callback.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Crée une nouvelle commande qui peut toujours être exécutée.
        /// </summary>
        /// <param name="execute">Délégué à exécuter</param>
        public DelegateCommand(Action execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Crée une nouvelle commande.
        /// </summary>
        /// <param name="execute">Délégué à exécuter</param>
        /// <param name="canExecute">Délégué pour vérifier si la commande peut être exécutée</param>
        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Détermine si cette commande peut être exécutée.
        /// </summary>
        /// <param name="parameter">Données utilisées par la commande (non utilisé)</param>
        /// <returns>Vrai si la commande peut être exécutée, faux sinon</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        /// <summary>
        /// Déclenché quand les changements détectés indiquent que la capacité 
        /// d'exécution de la commande a changé.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Signale aux gestionnaires que l'état de CanExecute a changé.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Définit l'action à exécuter quand cette commande est invoquée.
        /// </summary>
        /// <param name="parameter">Données utilisées par la commande (non utilisé)</param>
        public void Execute(object parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// Implémentation générique du pattern Command pouvant être utilisée 
    /// pour créer des commandes à partir de méthodes de callback.
    /// </summary>
    /// <typeparam name="T">Type du paramètre de la commande</typeparam>
    public class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// Crée une nouvelle commande qui peut toujours être exécutée.
        /// </summary>
        /// <param name="execute">Délégué à exécuter</param>
        public DelegateCommand(Action<T> execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Crée une nouvelle commande.
        /// </summary>
        /// <param name="execute">Délégué à exécuter</param>
        /// <param name="canExecute">Délégué pour vérifier si la commande peut être exécutée</param>
        public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Détermine si cette commande peut être exécutée.
        /// </summary>
        /// <param name="parameter">Données utilisées par la commande</param>
        /// <returns>Vrai si la commande peut être exécutée, faux sinon</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(ConvertParameter(parameter));
        }

        /// <summary>
        /// Déclenché quand les changements détectés indiquent que la capacité 
        /// d'exécution de la commande a changé.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Signale aux gestionnaires que l'état de CanExecute a changé.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Définit l'action à exécuter quand cette commande est invoquée.
        /// </summary>
        /// <param name="parameter">Données utilisées par la commande</param>
        public void Execute(object parameter)
        {
            _execute(ConvertParameter(parameter));
        }

        /// <summary>
        /// Convertit le paramètre d'objet en type T.
        /// </summary>
        /// <param name="parameter">Paramètre à convertir</param>
        /// <returns>Paramètre converti</returns>
        private static T ConvertParameter(object parameter)
        {
            if (parameter == null && typeof(T).IsValueType)
            {
                return default;
            }
            
            return (T)parameter;
        }
    }
}
