using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une chaîne en Visibility. Retourne Visible si la chaîne n'est pas vide, Collapsed sinon.
    /// Peut être inversé avec le paramètre "inverse".
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue));
            bool inverse = parameter is string paramStr && paramStr.ToLower() == "inverse";
            
            if (inverse)
            {
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}