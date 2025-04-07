using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une chaîne en Visibility avec une logique inversée.
    /// Chaîne vide ou null => Visible, chaîne non vide => Collapsed
    /// </summary>
    public class StringToInverseVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convertit une chaîne en Visibility avec une logique inversée.
        /// </summary>
        /// <param name="value">Chaîne à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Paramètre de conversion (non utilisé)</param>
        /// <param name="culture">Culture à utiliser pour la conversion</param>
        /// <returns>Visible si la chaîne est vide/null, Collapsed sinon</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrEmpty(stringValue) ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Visible;
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