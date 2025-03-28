namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Catégorie d'un article de courses.
    /// </summary>
    public enum ShoppingCategory
    {
        Produce,        // Fruits et légumes
        Dairy,          // Produits laitiers
        Meat,           // Viandes
        Seafood,        // Poissons et fruits de mer
        Bakery,         // Boulangerie
        Pantry,         // Garde-manger (épices, conserves, etc.)
        Frozen,         // Surgelés
        Beverages,      // Boissons
        Snacks,         // Snacks et confiseries
        Cleaning,       // Produits d'entretien
        Health,         // Santé et beauté
        Other           // Divers
    }

    /// <summary>
    /// Représente un article dans une liste de courses.
    /// </summary>
    public class ShoppingItem
    {
        /// <summary>
        /// Identifiant unique de l'article.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom de l'article.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Quantité nécessaire.
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        /// Unité de mesure (g, kg, ml, l, etc.).
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Catégorie de l'article pour faciliter l'organisation des courses.
        /// </summary>
        public ShoppingCategory Category { get; set; } = ShoppingCategory.Other;

        /// <summary>
        /// Indique si l'article a été acheté.
        /// </summary>
        public bool IsPurchased { get; set; }

        /// <summary>
        /// Notes supplémentaires sur l'article.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// ID de la liste de courses à laquelle appartient cet article.
        /// </summary>
        public int ShoppingListId { get; set; }

        /// <summary>
        /// Liste des recettes qui nécessitent cet ingrédient.
        /// </summary>
        public List<Recipe> AssociatedRecipes { get; set; } = new List<Recipe>();

        /// <summary>
        /// Indique si l'article a été ajouté manuellement (non associé à une recette).
        /// </summary>
        public bool IsManuallyAdded { get; set; }

        /// <summary>
        /// Formatte la quantité et l'unité en texte lisible.
        /// </summary>
        /// <returns>Texte formatté représentant la quantité</returns>
        public string FormattedQuantity()
        {
            if (string.IsNullOrEmpty(Unit))
                return Quantity.ToString();

            return $"{Quantity} {Unit}";
        }
    }
}
