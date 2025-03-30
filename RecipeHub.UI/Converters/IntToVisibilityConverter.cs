using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit un entier en visibilité en fonction d'une valeur de comparaison.
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convertit un entier en visibilité.
        /// </summary>
        /// <param name="value">L'entier à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">
        /// Valeur de comparaison. Si value == parameter, retourne Collapsed, sinon Visible.
        /// Si omis, retourne Visible si value > 0, sinon Collapsed.
        /// </param>
        /// <param name="culture">Culture à utiliser dans la conversion</param>
        /// <returns>Visibility.Visible ou Visibility.Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                if (parameter != null)
                {
                    int compareValue;
                    
                    if (parameter is int paramInt)
                    {
                        compareValue = paramInt;
                    }
                    else if (int.TryParse(parameter.ToString(), out int parsedValue))
                    {
                        compareValue = parsedValue;
                    }
                    else
                    {
                        return Visibility.Visible;
                    }
                    
                    return intValue == compareValue ? Visibility.Collapsed : Visibility.Visible;
                }
                
                return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Méthode non implémentée.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
