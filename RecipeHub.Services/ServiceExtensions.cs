using Microsoft.Extensions.DependencyInjection;
using RecipeHub.Core.Interfaces;
using RecipeHub.Services.Data;
using RecipeHub.Services.Cache;

namespace RecipeHub.Services
{
    /// <summary>
    /// Extensions pour l'inscription des services dans le conteneur de dépendances.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Enregistre les services de RecipeHub dans le conteneur de dépendances.
        /// </summary>
        /// <param name="services">Collection de services</param>
        /// <returns>Collection de services mise à jour</returns>
        public static IServiceCollection AddRecipeHubServices(this IServiceCollection services)
        {
            // Services de cache
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            services.AddSingleton<SqliteCacheService>();

            // Service de base de données
            services.AddSingleton<DatabaseService>();

            // Service de favoris
            services.AddSingleton<IFavoritesService, FavoritesService>();

            // Services de planification et de listes de courses
            services.AddSingleton<IMealPlanningService, MealPlanningDatabaseService>();
            services.AddSingleton<IShoppingListService, ShoppingListDatabaseService>();

            // Repository de données utilisateur
            services.AddSingleton<IUserDataRepository, UserDataRepository>();

            // Service de synchronisation
            services.AddSingleton<DataSyncService>();

            return services;
        }
    }
}
