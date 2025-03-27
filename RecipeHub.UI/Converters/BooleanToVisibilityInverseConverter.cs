using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une valeur booléenne en Visibility, avec la logique inversée par rapport au convertisseur standard.
    /// True => Collapsed, False => Visible
    /// </summary>
    public class BooleanToVisibilityInverseConverter : IValueConverter
    {
        /// <summary>
        /// Convertit une valeur booléenne en Visibility.
        /// </summary>
        /// <param name="value">Valeur booléenne à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Paramètre de conversion (non utilisé)</param>
        /// <param name="culture">Culture à utiliser pour la conversion</param>
        /// <returns>Collapsed si True, Visible si False</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        /// <summary>
        /// Convertit une Visibility en valeur booléenne.
        /// </summary>
        /// <param name="value">Visibility à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Paramètre de conversion (non utilisé)</param>
        /// <param name="culture">Culture à utiliser pour la conversion</param>
        /// <returns>True si Collapsed, False si Visible</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }

            return false;
        }
    }
}