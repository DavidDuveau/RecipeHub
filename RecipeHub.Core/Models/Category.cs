namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Représente une catégorie de recettes (ex: Dessert, Plat principal, etc.)
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Identifiant unique de la catégorie.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom de la catégorie.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description de la catégorie.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// URL de l'image représentant la catégorie.
        /// </summary>
        public string Thumbnail { get; set; } = string.Empty;
    }
}
