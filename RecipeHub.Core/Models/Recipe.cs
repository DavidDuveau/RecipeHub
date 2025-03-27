namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Représente une recette de cuisine avec toutes ses informations associées.
    /// </summary>
    public class Recipe
    {
        /// <summary>
        /// Identifiant unique de la recette.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom de la recette.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Catégorie à laquelle appartient la recette (dessert, plat principal, etc.).
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Origine géographique de la recette.
        /// </summary>
        public string Area { get; set; } = string.Empty;

        /// <summary>
        /// Instructions de préparation de la recette.
        /// </summary>
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// URL de l'image de la recette.
        /// </summary>
        public string Thumbnail { get; set; } = string.Empty;

        /// <summary>
        /// Liste des ingrédients nécessaires pour la recette.
        /// </summary>
        public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

        /// <summary>
        /// URL de la vidéo de préparation, si disponible.
        /// </summary>
        public string? VideoUrl { get; set; }

        /// <summary>
        /// Tags associés à la recette pour faciliter la recherche.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Indique si la recette est marquée comme favorite par l'utilisateur.
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// Collections personnalisées auxquelles appartient cette recette.
        /// </summary>
        public List<string> Collections { get; set; } = new List<string>();
    }
}
