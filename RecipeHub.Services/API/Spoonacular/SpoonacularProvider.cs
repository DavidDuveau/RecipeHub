using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using RecipeHub.Services.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Services.API.Spoonacular
{
    /// <summary>
    /// Implémentation du fournisseur de recettes pour l'API Spoonacular.
    /// </summary>
    public partial class SpoonacularProvider : IRecipeProvider
    {
        private readonly RestClient _client;
        private readonly ICacheService _cacheService;
        private readonly IApiMetricsService _metricsService;
        private readonly IFavoritesService? _favoritesService;
        
        private const string PROVIDER_NAME = "Spoonacular";
        private const int DAILY_QUOTA = 150; // Quota quotidien pour l'API gratuite
        private const string BASE_URL = "https://api.spoonacular.com/";
        private readonly string _apiKey;
        
        // Durées de cache par type de données
        private static readonly TimeSpan RecipeCacheDuration = TimeSpan.FromDays(7);
        private static readonly TimeSpan CategoryCacheDuration = TimeSpan.FromDays(30);
        private static readonly TimeSpan CuisineCacheDuration = TimeSpan.FromDays(30);
        private static readonly TimeSpan IngredientCacheDuration = TimeSpan.FromDays(30);
        private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromDays(1);
        private static readonly TimeSpan FilterCacheDuration = TimeSpan.FromDays(1);

        /// <summary>
        /// Nom du fournisseur de recettes.
        /// </summary>
        public string ProviderName => PROVIDER_NAME;

        /// <summary>
        /// Quota quotidien maximum d'appels API.
        /// </summary>
        public int DailyQuota => DAILY_QUOTA;

        /// <summary>
        /// Constructeur du fournisseur Spoonacular.
        /// </summary>
        /// <param name="apiKey">Clé API pour Spoonacular</param>
        /// <param name="cacheService">Service de cache</param>
        /// <param name="metricsService">Service de métriques d'API</param>
        /// <param name="favoritesService">Service de gestion des favoris (optionnel)</param>
        public SpoonacularProvider(
            string apiKey,
            ICacheService cacheService,
            IApiMetricsService metricsService,
            IFavoritesService? favoritesService = null)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _client = new RestClient(BASE_URL);
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _favoritesService = favoritesService;
            
            // Enregistrer ce fournisseur auprès du service de métriques
            _metricsService.RegisterProviderAsync(PROVIDER_NAME, DAILY_QUOTA).Wait();
        }

        /// <summary>
        /// Obtient le nombre d'appels API restants pour aujourd'hui.
        /// </summary>
        /// <returns>Le nombre d'appels restants</returns>
        public async Task<int> GetRemainingCallsAsync()
        {
            return await _metricsService.GetRemainingCallsAsync(PROVIDER_NAME);
        }
        
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
