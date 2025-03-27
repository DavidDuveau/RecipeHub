using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace RecipeHub.UI.Converters
{
    /// <summary>
    /// Convertit une chaîne de caractères en index d'onglet et vice-versa.
    /// </summary>
    public class StringToTabIndexConverter : IValueConverter
    {
        /// <summary>
        /// Convertit une chaîne de caractères en index d'onglet.
        /// </summary>
        /// <param name="value">Chaîne de caractères à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Paramètre de conversion au format "string1:index1;string2:index2;..."</param>
        /// <param name="culture">Culture à utiliser pour la conversion</param>
        /// <returns>Index de l'onglet correspondant à la chaîne ou 0 si non trouvé</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0;

            string stringValue = value.ToString();
            string paramString = parameter.ToString();

            // Extraire les mappings depuis le paramètre
            var mappings = ParseMappings(paramString);

            // Rechercher l'index correspondant à la chaîne
            if (mappings.TryGetValue(stringValue.ToLowerInvariant(), out int index))
                return index;

            return 0;
        }

        /// <summary>
        /// Convertit un index d'onglet en chaîne de caractères.
        /// </summary>
        /// <param name="value">Index de l'onglet à convertir</param>
        /// <param name="targetType">Type cible de la conversion</param>
        /// <param name="parameter">Paramètre de conversion au format "string1:index1;string2:index2;..."</param>
        /// <param name="culture">Culture à utiliser pour la conversion</param>
        /// <returns>Chaîne de caractères correspondant à l'index ou une chaîne vide si non trouvé</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return string.Empty;

            if (!int.TryParse(value.ToString(), out int intValue))
                return string.Empty;

            string paramString = parameter.ToString();

            // Extraire les mappings depuis le paramètre
            var mappings = ParseMappings(paramString);

            // Rechercher la chaîne correspondant à l'index
            foreach (var mapping in mappings)
            {
                if (mapping.Value == intValue)
                    return mapping.Key;
            }

            return string.Empty;
        }

        /// <summary>
        /// Analyse la chaîne de paramètres pour extraire les mappings entre chaînes et index.
        /// </summary>
        /// <param name="paramString">Chaîne de paramètres au format "string1:index1;string2:index2;..."</param>
        /// <returns>Dictionnaire des mappings entre chaînes et index</returns>
        private Dictionary<string, int> ParseMappings(string paramString)
        {
            var mappings = new Dictionary<string, int>();

            string[] pairs = paramString.Split(';');
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim().ToLowerInvariant();
                    if (int.TryParse(parts[1].Trim(), out int value))
                    {
                        mappings[key] = value;
                    }
                }
            }

            return mappings;
        }
    }
}
