namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Représente une liste de courses.
    /// </summary>
    public class ShoppingList
    {
        /// <summary>
        /// Identifiant unique de la liste de courses.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom de la liste de courses.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Date de création de la liste.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification de la liste.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Articles de la liste de courses.
        /// </summary>
        public List<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();

        /// <summary>
        /// ID du plan de repas associé à cette liste, si applicable.
        /// </summary>
        public int? MealPlanId { get; set; }

        /// <summary>
        /// Indique si la liste est active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Calcule le pourcentage d'articles achetés.
        /// </summary>
        /// <returns>Pourcentage d'achèvement (0-100)</returns>
        public int GetCompletionPercentage()
        {
            if (Items.Count == 0)
                return 0;

            int purchasedCount = Items.Count(i => i.IsPurchased);
            return (int)((double)purchasedCount / Items.Count * 100);
        }
    }
}
