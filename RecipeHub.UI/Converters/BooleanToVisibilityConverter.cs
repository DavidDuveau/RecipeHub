using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une valeur booléenne en Visibility. Retourne Visible si true, Collapsed si false.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Si le paramètre est "Inverse", on inverse la logique
                if (parameter is string paramString && paramString.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Si le paramètre est "Inverse", on inverse la logique
                if (parameter is string paramString && paramString.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return visibility != Visibility.Visible;
                }
                
                return visibility == Visibility.Visible;
            }
            
            return false;
        }
    }
}