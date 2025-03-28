using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit un entier (généralement un compte d'éléments) en visibilité.
    /// Retourne Visible si la valeur est supérieure à 0, Collapsed sinon.
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convertit un entier en Visibility.
        /// </summary>
        /// <param name="value">Entier à convertir</param>
        /// <param name="targetType">Type cible (ignoré)</param>
        /// <param name="parameter">Paramètre (ignoré)</param>
        /// <param name="culture">Culture (ignorée)</param>
        /// <returns>Visible si > 0, Collapsed sinon</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Convertit une visibilité en entier (non implémenté).
        /// </summary>
        /// <param name="value">Valeur à convertir</param>
        /// <param name="targetType">Type cible</param>
        /// <param name="parameter">Paramètre</param>
        /// <param name="culture">Culture</param>
        /// <returns>Toujours 0</returns>
        /// <exception cref="NotImplementedException">Cette méthode n'est pas implémentée</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
