using System;
using System.Globalization;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une valeur booléenne en icône de favoris (coeur plein ou vide).
    /// </summary>
    public class FavoriteIconConverter : IValueConverter
    {
        /// <summary>
        /// Chemin d'accès à l'icône pour l'état favori (true).
        /// </summary>
        public string FavoriteIcon { get; set; } = "/RecipeHub.UI;component/Assets/favorite_filled.png";
        
        /// <summary>
        /// Chemin d'accès à l'icône pour l'état non favori (false).
        /// </summary>
        public string NotFavoriteIcon { get; set; } = "/RecipeHub.UI;component/Assets/favorite_outline.png";
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite)
            {
                return isFavorite ? FavoriteIcon : NotFavoriteIcon;
            }
            
            return NotFavoriteIcon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as string == FavoriteIcon;
        }
    }
}