using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une chaîne en Visibility. Retourne Visible si la chaîne n'est pas vide, Collapsed sinon.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
