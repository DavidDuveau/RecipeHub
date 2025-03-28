namespace RecipeHub.Core.Models
{
    /// <summary>
    /// Modèle pour stocker les métriques d'utilisation d'une API.
    /// </summary>
    public class ApiUsageMetrics
    {
        /// <summary>
        /// Nom du fournisseur d'API.
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Quota quotidien maximal d'appels API.
        /// </summary>
        public int DailyQuota { get; set; }

        /// <summary>
        /// Nombre d'appels utilisés aujourd'hui.
        /// </summary>
        public int UsedToday { get; set; }

        /// <summary>
        /// Date de la dernière réinitialisation du compteur quotidien.
        /// </summary>
        public DateTime LastResetDate { get; set; }

        /// <summary>
        /// Constructeur par défaut pour sérialisation.
        /// </summary>
        public ApiUsageMetrics()
        {
            ProviderName = string.Empty;
            DailyQuota = 0;
            UsedToday = 0;
            LastResetDate = DateTime.Now.Date;
        }

        /// <summary>
        /// Constructeur avec valeurs initiales.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="dailyQuota">Quota quotidien</param>
        public ApiUsageMetrics(string providerName, int dailyQuota)
        {
            ProviderName = providerName;
            DailyQuota = dailyQuota;
            UsedToday = 0;
            LastResetDate = DateTime.Now.Date;
        }

        /// <summary>
        /// Vérifie s'il est nécessaire de réinitialiser le compteur quotidien.
        /// </summary>
        /// <returns>Vrai si le compteur doit être réinitialisé, faux sinon.</returns>
        public bool ShouldResetCounter()
        {
            return DateTime.Now.Date > LastResetDate;
        }

        /// <summary>
        /// Réinitialise le compteur quotidien.
        /// </summary>
        public void ResetCounter()
        {
            UsedToday = 0;
            LastResetDate = DateTime.Now.Date;
        }

        /// <summary>
        /// Vérifie si le quota quotidien est dépassé.
        /// </summary>
        /// <returns>Vrai si le quota est dépassé, faux sinon.</returns>
        public bool IsQuotaExceeded()
        {
            if (ShouldResetCounter())
            {
                ResetCounter();
                return false;
            }

            return UsedToday >= DailyQuota;
        }

        /// <summary>
        /// Incrémente le compteur d'utilisation.
        /// </summary>
        /// <param name="count">Nombre d'appels à comptabiliser</param>
        public void IncrementUsage(int count = 1)
        {
            if (ShouldResetCounter())
            {
                ResetCounter();
            }

            UsedToday += count;
        }

        /// <summary>
        /// Obtient le nombre d'appels restants pour aujourd'hui.
        /// </summary>
        /// <returns>Nombre d'appels restants</returns>
        public int GetRemainingCalls()
        {
            if (ShouldResetCounter())
            {
                ResetCounter();
                return DailyQuota;
            }

            return Math.Max(0, DailyQuota - UsedToday);
        }

        /// <summary>
        /// Obtient le pourcentage d'utilisation du quota quotidien.
        /// </summary>
        /// <returns>Pourcentage d'utilisation (0-100)</returns>
        public double GetUsagePercentage()
        {
            if (DailyQuota <= 0)
                return 100.0;

            if (ShouldResetCounter())
            {
                ResetCounter();
                return 0.0;
            }

            return (UsedToday * 100.0) / DailyQuota;
        }
    }
}
