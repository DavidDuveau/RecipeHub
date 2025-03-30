using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit un booléen en couleur de succès ou d'erreur.
    /// </summary>
    public class BoolToSuccessColorConverter : IValueConverter
    {
        /// <summary>
        /// Convertit un booléen en couleur.
        /// </summary>
        /// <param name="value">Le booléen à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Non utilisé</param>
        /// <param name="culture">Culture à utiliser dans la conversion</param>
        /// <returns>Une couleur verte pour true, rouge pour false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)) :  // Vert (succès)
                    new SolidColorBrush(Color.FromRgb(244, 67, 54));   // Rouge (erreur)
            }
            
            return new SolidColorBrush(Colors.Gray);
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
