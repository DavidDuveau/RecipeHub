using Microsoft.Extensions.DependencyInjection;
using RecipeHub.Core.Interfaces;
using RecipeHub.Services.API;
using RecipeHub.Services.Cache;
using RecipeHub.Services.Data;
using System;

namespace RecipeHub.Services.Configuration
{
    /// <summary>
    /// Configuration des services de l'application pour l'injection de dépendances.
    /// </summary>
    public static class ServicesConfiguration
    {
        /// <summary>
        /// Enregistre les services principaux de l'application.
        /// </summary>
        /// <param name="services">Collection de services</param>
        /// <param name="usePersistentCache">Indique si le cache persistant doit être utilisé</param>
        /// <returns>Collection de services mise à jour</returns>
        public static IServiceCollection RegisterServices(this IServiceCollection services, bool usePersistentCache = true)
        {
            // Enregistrement du service de cache
            if (usePersistentCache)
            {
                services.AddSingleton<ICacheService, SqliteCacheService>();
            }
            else
            {
                services.AddSingleton<ICacheService, InMemoryCacheService>();
            }

            // Enregistrement du service de favoris
            services.AddSingleton<IFavoritesService, FavoritesService>();

            // Enregistrement du service d'API TheMealDB
            services.AddSingleton<IMealDbService, MealDbService>(provider => 
                new MealDbService(
                    provider.GetRequiredService<ICacheService>(),
                    provider.GetRequiredService<IFavoritesService>()
                )
            );

            return services;
        }
    }
}
