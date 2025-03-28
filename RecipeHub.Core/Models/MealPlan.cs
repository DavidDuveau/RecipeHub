namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Représente un plan de repas pour une période définie.
    /// </summary>
    public class MealPlan
    {
        /// <summary>
        /// Identifiant unique du plan de repas.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nom du plan de repas (ex: "Semaine du 1er avril").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Date de début du plan de repas.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Date de fin du plan de repas.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Collection des repas planifiés.
        /// </summary>
        public List<PlannedMeal> Meals { get; set; } = new List<PlannedMeal>();

        /// <summary>
        /// Liste de courses associée à ce plan de repas.
        /// </summary>
        public ShoppingList? ShoppingList { get; set; }

        /// <summary>
        /// Indique si le plan est actif.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Date de création du plan.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification du plan.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
