namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Représente un ingrédient utilisé dans une recette, avec sa quantité.
    /// </summary>
    public class Ingredient
    {
        /// <summary>
        /// Nom de l'ingrédient.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Format textuel de la quantité (hérité de l'API MealDB).
        /// </summary>
        public string Measure { get; set; } = string.Empty;

        /// <summary>
        /// Quantité numérique de l'ingrédient (pour les calculs).
        /// </summary>
        public double Quantity { get; set; } = 1.0;

        /// <summary>
        /// Unité de mesure (grammes, ml, cuillère à soupe, etc.).
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Constructeur par défaut.
        /// </summary>
        public Ingredient()
        {
        }

        /// <summary>
        /// Constructeur avec initialisation des propriétés de base.
        /// </summary>
        /// <param name="name">Nom de l'ingrédient</param>
        /// <param name="measure">Format textuel de la quantité</param>
        public Ingredient(string name, string measure)
        {
            Name = name;
            Measure = measure;
            ParseMeasureToQuantityAndUnit(measure);
        }

        /// <summary>
        /// Constructeur complet.
        /// </summary>
        /// <param name="name">Nom de l'ingrédient</param>
        /// <param name="measure">Format textuel de la quantité</param>
        /// <param name="quantity">Quantité numérique</param>
        /// <param name="unit">Unité de mesure</param>
        public Ingredient(string name, string measure, double quantity, string unit)
        {
            Name = name;
            Measure = measure;
            Quantity = quantity;
            Unit = unit;
        }

        /// <summary>
        /// Tente de parser la mesure textuelle en quantité numérique et unité.
        /// </summary>
        private void ParseMeasureToQuantityAndUnit(string measure)
        {
            if (string.IsNullOrWhiteSpace(measure))
                return;

            // Exemples de formats possibles : "200g", "1 cup", "2 1/2 tablespoons", etc.
            // Pattern de base : un nombre (potentiellement avec fraction) suivi d'une unité
            
            // Extraction numérique basique
            var parts = measure.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length > 0)
            {   
                // Tentative d'extraction du nombre 
                if (double.TryParse(parts[0], out double quantity))
                {
                    Quantity = quantity;
                    
                    // Si on a une unité
                    if (parts.Length > 1)
                    {
                        Unit = parts[1];
                    }
                    else
                    {
                        // Tenter d'extraire l'unité du premier segment (ex: "200g")
                        var numericPart = ExtractNumericPart(parts[0]);
                        if (numericPart.Length < parts[0].Length)
                        {
                            Unit = parts[0].Substring(numericPart.Length).Trim();
                        }
                    }
                }
                else
                {
                    // Si pas de nombre explicite, c'est peut-être juste une unité ou une description
                    Unit = measure;
                    Quantity = 1; // Par défaut
                }
            }
        }

        /// <summary>
        /// Extrait la partie numérique d'une chaîne.
        /// </summary>
        private string ExtractNumericPart(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            int i = 0;
            while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.' || input[i] == '/' || input[i] == ','))
            {
                i++;
            }

            return input.Substring(0, i);
        }
    }
}
