using System;
using System.Globalization;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une chaîne en valeur booléenne.
    /// Chaîne non-vide => True, chaîne vide ou null => False
    /// </summary>
    public class StringToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Convertit une chaîne en valeur booléenne.
        /// </summary>
        /// <param name="value">Chaîne à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Si 'inverse' est spécifié, la logique est inversée</param>
        /// <param name="culture">Culture à utiliser pour la conversion</param>
        /// <returns>True si la chaîne n'est pas vide/null, False sinon</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = string.IsNullOrEmpty(value as string);
            
            // Si le paramètre "inverse" est spécifié, inverser la logique
            bool inverse = parameter is string paramStr && paramStr.ToLower() == "inverse";
            
            if (inverse)
            {
                return isEmpty;
            }
            
            return !isEmpty;
        }

        /// <summary>
        /// Conversion inverse (non implémentée).
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Cette conversion n'est pas bidirectionnelle
            throw new NotImplementedException();
        }
    }
}