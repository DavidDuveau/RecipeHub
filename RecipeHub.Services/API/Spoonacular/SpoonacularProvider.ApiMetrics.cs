using System.Threading.Tasks;

namespace RecipeHub.Services.API.Spoonacular
{
    /// <summary>
    /// Partie du fournisseur Spoonacular gérant les méthodes liées aux métriques d'API.
    /// </summary>
    public partial class SpoonacularProvider
    {
        /// <summary>
        /// Incrémente le compteur d'utilisation de l'API.
        /// </summary>
        /// <param name="count">Nombre d'appels à comptabiliser (1 par défaut)</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task IncrementApiUsageAsync(int count = 1)
        {
            await _metricsService.IncrementUsageAsync(PROVIDER_NAME, count);
        }

        /// <summary>
        /// Réinitialise le compteur d'utilisation quotidienne.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task ResetDailyCounterAsync()
        {
            await _metricsService.ResetCounterAsync(PROVIDER_NAME);
        }
    }
}
