namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Type de repas (petit-déjeuner, déjeuner, dîner, etc.).
    /// </summary>
    public enum MealType
    {
        Breakfast,
        Lunch,
        Dinner,
        Snack,
        Other
    }

    /// <summary>
    /// Représente un repas planifié pour un jour spécifique.
    /// </summary>
    public class PlannedMeal
    {
        /// <summary>
        /// Identifiant unique du repas planifié.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Recette associée à ce repas planifié.
        /// </summary>
        public Recipe Recipe { get; set; } = null!;

        /// <summary>
        /// ID de la recette associée.
        /// </summary>
        public int RecipeId { get; set; }

        /// <summary>
        /// Date du repas.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Type de repas (petit-déjeuner, déjeuner, dîner, etc.).
        /// </summary>
        public MealType Type { get; set; } = MealType.Dinner;

        /// <summary>
        /// Nombre de portions.
        /// </summary>
        public int Servings { get; set; } = 1;

        /// <summary>
        /// Notes personnelles pour ce repas.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Indique si les ingrédients de ce repas sont inclus dans la liste de courses.
        /// </summary>
        public bool IncludeInShoppingList { get; set; } = true;
    }
}
