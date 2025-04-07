using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une valeur null en Visibility.
    /// Null => Collapsed, Non null => Visible
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convertit une valeur null en Visibility.
        /// </summary>
        /// <param name="value">Valeur à tester</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Si 'inverse' est spécifié, la logique est inversée</param>
        /// <param name="culture">Culture à utiliser pour la conversion</param>
        /// <returns>Visibility basée sur le fait que la valeur est null ou non</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            
            // Si le paramètre "inverse" est spécifié, inverser la logique
            bool inverse = parameter is string paramStr && paramStr.ToLower() == "inverse";
            
            if (inverse)
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return isNull ? Visibility.Collapsed : Visibility.Visible;
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