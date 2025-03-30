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
                _requestOptimizer.SetProviderStrategy(provider.ProviderName, Core.Interfaces.OptimizationStrategy.Balanced);
            }
        }

        /// <summary>
        /// Définit l'ordre de priorité des fournisseurs.
        /// </summary>
        /// <param name="providerOrder">Liste ordonnée des noms de fournisseurs</param>
        public void SetProviderPriority(List<string> providerOrder)
        {
            if (providerOrder == null)
                throw new ArgumentNullException(nameof(providerOrder));

            // Vérifier que tous les noms sont valides
            foreach (var providerName in providerOrder)
            {
                if (!_providers.Any(p => p.ProviderName == providerName))
                    throw new ArgumentException($"Fournisseur inconnu : {providerName}", nameof(providerOrder));
            }

            // Vérifier que tous les fournisseurs sont inclus
            var providersNotIncluded = _providers.Select(p => p.ProviderName)
                .Except(providerOrder)
                .ToList();

            if (providersNotIncluded.Any())
            {
                // Ajouter les fournisseurs manquants à la fin de la liste
                providerOrder.AddRange(providersNotIncluded);
            }

            _providerPriority = providerOrder;
        }

        /// <summary>
        /// Obtient l'ordre de priorité actuel des fournisseurs.
        /// </summary>
        /// <returns>Liste ordonnée des noms de fournisseurs</returns>
        public List<string> GetProviderPriority()
        {
            return new List<string>(_providerPriority);
        }

        /// <summary>
        /// Obtient les stratégies d'optimisation pour tous les fournisseurs.
        /// </summary>
        /// <returns>Dictionnaire associant le nom du fournisseur à sa stratégie d'optimisation</returns>
        public Dictionary<string, Core.Interfaces.OptimizationStrategy> GetProviderOptimizationStrategies()
        {
            var strategies = new Dictionary<string, Core.Interfaces.OptimizationStrategy>();
            
            foreach (var provider in _providers)
            {
                strategies[provider.ProviderName] = _requestOptimizer.GetProviderStrategy(provider.ProviderName);
            }
            
            return strategies;
        }

        /// <summary>
        /// Définit la stratégie d'optimisation pour un fournisseur spécifique.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="strategy">Stratégie d'optimisation</param>
        public void SetProviderOptimizationStrategy(string providerName, Core.Interfaces.OptimizationStrategy strategy)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Le nom du fournisseur ne peut pas être vide.", nameof(providerName));
                
            if (!_providers.Any(p => p.ProviderName == providerName))
                throw new ArgumentException($"Fournisseur inconnu : {providerName}", nameof(providerName));
                
            _requestOptimizer.SetProviderStrategy(providerName, strategy);
        }
    }
}
