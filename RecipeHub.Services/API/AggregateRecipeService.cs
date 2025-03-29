using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace RecipeHub.Services.API
{
    /// <summary>
    /// Service d'agrégation centralisant l'accès aux différentes sources de recettes.
    /// </summary>
    public partial class AggregateRecipeService : IAggregateRecipeService
    {
        private readonly List<IRecipeProvider> _providers;
        private readonly ICacheService _cacheService;
        private readonly IApiRequestOptimizer _requestOptimizer;
        private List<string> _providerPriority;

        /// <summary>
        /// Liste des fournisseurs de recettes disponibles.
        /// </summary>
        public IEnumerable<IRecipeProvider> Providers => _providers.AsReadOnly();

        /// <summary>
        /// Constructeur du service d'agrégation.
        /// </summary>
        /// <param name="providers">Liste des fournisseurs de recettes</param>
        /// <param name="cacheService">Service de cache</param>
        /// <param name="requestOptimizer">Service d'optimisation des requêtes API</param>
        public AggregateRecipeService(
            IEnumerable<IRecipeProvider> providers, 
            ICacheService cacheService,
            IApiRequestOptimizer requestOptimizer)
        {
            _providers = providers.ToList() ?? throw new ArgumentNullException(nameof(providers));
            if (!_providers.Any())
                throw new ArgumentException("La liste des fournisseurs ne peut pas être vide.", nameof(providers));
                
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _requestOptimizer = requestOptimizer ?? throw new ArgumentNullException(nameof(requestOptimizer));
            
            // Par défaut, l'ordre de priorité est l'ordre de la liste des fournisseurs
            _providerPriority = _providers.Select(p => p.ProviderName).ToList();
            
            // Configuration des stratégies d'optimisation par défaut
            foreach (var provider in _providers)
            {
                _requestOptimizer.SetProviderStrategy(provider.ProviderName, OptimizationStrategy.Balanced);
            }
        }
    }
}
