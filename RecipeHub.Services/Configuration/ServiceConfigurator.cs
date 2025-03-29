using Microsoft.Extensions.DependencyInjection;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using RecipeHub.Services.API;
using RecipeHub.Services.API.Spoonacular;
using RecipeHub.Services.Cache;
using RecipeHub.Services.Data;
using RecipeHub.Services.Metrics;
using System;
using System.IO;

namespace RecipeHub.Services.Configuration
{
    /// <summary>
    /// Classe utilitaire pour configurer les services de l'application.
    /// </summary>
    public static class ServiceConfigurator
    {
        /// <summary>
        /// Enregistre tous les services nécessaires dans le conteneur d'injection de dépendances.
        /// </summary>
        /// <param name="services">Collection de services à configurer</param>
        /// <param name="dataDirectoryPath">Chemin vers le répertoire de données de l'application</param>
        /// <param name="spoonacularApiKey">Clé API pour Spoonacular (optionnelle)</param>
        /// <returns>La collection de services mise à jour</returns>
        public static IServiceCollection ConfigureServices(
            this IServiceCollection services,
            string dataDirectoryPath,
            string? spoonacularApiKey = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
                
            if (string.IsNullOrWhiteSpace(dataDirectoryPath))
                throw new ArgumentException("Le chemin du répertoire de données ne peut pas être vide.", nameof(dataDirectoryPath));
                
            // S'assurer que le répertoire de données existe
            Directory.CreateDirectory(dataDirectoryPath);
            
            // Chemins des fichiers de données
            var cacheDbPath = Path.Combine(dataDirectoryPath, "cache.db");
            var favoritesDbPath = Path.Combine(dataDirectoryPath, "favorites.db");
            var metricsDbPath = Path.Combine(dataDirectoryPath, "metrics.db");
            
            // Enregistrer les services de base
            services.AddSingleton<ICacheService>(provider => new SqliteCacheService(cacheDbPath));
            services.AddSingleton<IFavoritesService>(provider => new FavoritesService(favoritesDbPath));
            services.AddSingleton<IApiMetricsService>(provider => new ApiMetricsService(metricsDbPath));
            
            // Enregistrer les fournisseurs de recettes
            services.AddSingleton<IMealDbService, MealDbService>(provider => 
                new MealDbService(
                    provider.GetRequiredService<ICacheService>(),
                    provider.GetRequiredService<IFavoritesService>()));
                    
            // Enregistrer le fournisseur Spoonacular si une clé API est fournie
            if (!string.IsNullOrWhiteSpace(spoonacularApiKey))
            {
                services.AddSingleton<IRecipeProvider, SpoonacularProvider>(provider => 
                    new SpoonacularProvider(
                        spoonacularApiKey,
                        provider.GetRequiredService<ICacheService>(),
                        provider.GetRequiredService<IApiMetricsService>(),
                        provider.GetRequiredService<IFavoritesService>()));
            }
            
            // Adapter MealDbService pour implémenter IRecipeProvider
            services.AddSingleton<IRecipeProvider, MealDbAdapter>(provider => 
                new MealDbAdapter(provider.GetRequiredService<IMealDbService>()));
                
            // Enregistrer le service d'agrégation
            services.AddSingleton<IAggregateRecipeService, AggregateRecipeService>();
            
            return services;
        }
    }
    
    /// <summary>
    /// Adaptateur pour convertir un IMealDbService en IRecipeProvider.
    /// </summary>
    internal class MealDbAdapter : IRecipeProvider
    {
        private readonly IMealDbService _mealDbService;
        
        public string ProviderName => "TheMealDB";
        
        public int DailyQuota => 1000; // TheMealDB a un quota quotidien de 1000 appels pour l'API gratuite
        
        public MealDbAdapter(IMealDbService mealDbService)
        {
            _mealDbService = mealDbService ?? throw new ArgumentNullException(nameof(mealDbService));
        }
        
        public Task<int> GetRemainingCallsAsync()
        {
            // TheMealDB n'a pas de compteur d'utilisation, on retourne toujours le quota complet
            return Task.FromResult(DailyQuota);
        }
        
        public Task<Recipe?> GetRecipeByIdAsync(int id)
        {
            return _mealDbService.GetRecipeByIdAsync(id);
        }
        
        public Task<List<Recipe>> SearchRecipesByNameAsync(string name, int limit = 10)
        {
            return _mealDbService.SearchRecipesByNameAsync(name);
        }
        
        public Task<List<Recipe>> GetRandomRecipesAsync(int count)
        {
            return _mealDbService.GetRandomRecipesAsync(count);
        }
        
        public Task<List<Category>> GetCategoriesAsync()
        {
            return _mealDbService.GetCategoriesAsync();
        }
        
        public Task<List<Recipe>> GetRecipesByCategoryAsync(string category, int limit = 20)
        {
            return _mealDbService.GetRecipesByCategoryAsync(category);
        }
        
        public Task<List<string>> GetCuisinesAsync()
        {
            // Adapter la méthode GetAreasAsync à GetCuisinesAsync
            return _mealDbService.GetAreasAsync();
        }
        
        public Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisine, int limit = 20)
        {
            // Adapter la méthode GetRecipesByAreaAsync à GetRecipesByCuisineAsync
            return _mealDbService.GetRecipesByAreaAsync(cuisine);
        }
        
        public Task<List<string>> GetIngredientsAsync()
        {
            return _mealDbService.GetIngredientsAsync();
        }
        
        public Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient, int limit = 20)
        {
            return _mealDbService.GetRecipesByIngredientAsync(ingredient);
        }
        
        public Task IncrementApiUsageAsync(int count = 1)
        {
            // TheMealDB n'a pas de compteur d'utilisation, cette méthode ne fait rien
            return Task.CompletedTask;
        }
        
        public Task ResetDailyCounterAsync()
        {
            // TheMealDB n'a pas de compteur d'utilisation, cette méthode ne fait rien
            return Task.CompletedTask;
        }
    }
}
