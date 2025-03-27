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
        /// Quantité nécessaire de l'ingrédient.
        /// </summary>
        public string Measure { get; set; } = string.Empty;

        /// <summary>
        /// Constructeur par défaut.
        /// </summary>
        public Ingredient()
        {
        }

        /// <summary>
        /// Constructeur avec initialisation des propriétés.
        /// </summary>
        /// <param name="name">Nom de l'ingrédient</param>
        /// <param name="measure">Quantité de l'ingrédient</param>
        public Ingredient(string name, string measure)
        {
            Name = name;
            Measure = measure;
        }
    }
}
