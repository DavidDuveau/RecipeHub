using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Services.API
{
    /// <summary>
    /// Service d'agrégation centralisant l'accès aux différentes sources de recettes.
    /// </summary>
    public class AggregateRecipeService : IAggregateRecipeService
    {
        private readonly List<IRecipeProvider> _providers;
        private readonly ICacheService _cacheService;
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
        public AggregateRecipeService(IEnumerable<IRecipeProvider> providers, ICacheService cacheService)
        {
            _providers = providers.ToList() ?? throw new ArgumentNullException(nameof(providers));
            if (!_providers.Any())
                throw new ArgumentException("La liste des fournisseurs ne peut pas être vide.", nameof(providers));
                
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            
            // Par défaut, l'ordre de priorité est l'ordre de la liste des fournisseurs
            _providerPriority = _providers.Select(p => p.ProviderName).ToList();
        }

        /// <summary>
        /// Récupère une recette par son identifiant.
        /// </summary>
        /// <param name="id">Identifiant de la recette</param>
        /// <param name="preferredProvider">Nom du fournisseur préféré (optionnel)</param>
        /// <returns>La recette correspondante ou null si non trouvée</returns>
        public async Task<Recipe?> GetRecipeByIdAsync(int id, string? preferredProvider = null)
        {
            // Vérifier si une source préférée est spécifiée
            if (!string.IsNullOrWhiteSpace(preferredProvider))
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    var recipe = await provider.GetRecipeByIdAsync(id);
                    if (recipe != null)
                        return recipe;
                }
            }

            // Sinon, essayer les fournisseurs dans l'ordre de priorité
            foreach (var providerName in _providerPriority)
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    var recipe = await provider.GetRecipeByIdAsync(id);
                    if (recipe != null)
                        return recipe;
                }
            }

            // Si aucun fournisseur n'a trouvé la recette
            return null;
        }

        /// <summary>
        /// Recherche des recettes par leur nom à travers toutes les sources disponibles.
        /// </summary>
        /// <param name="name">Nom ou partie du nom à rechercher</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes correspondantes</returns>
        public async Task<List<Recipe>> SearchRecipesByNameAsync(string name, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Recipe>();

            var results = new List<Recipe>();
            var seenIds = new HashSet<int>(); // Pour éviter les doublons
            
            // Effectuer la recherche avec chaque fournisseur dans l'ordre de priorité
            foreach (var providerName in _providerPriority)
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    var providerResults = await provider.SearchRecipesByNameAsync(name, limit);
                    
                    // Ajouter les résultats qui n'ont pas déjà été trouvés
                    foreach (var recipe in providerResults)
                    {
                        if (!seenIds.Contains(recipe.Id))
                        {
                            results.Add(recipe);
                            seenIds.Add(recipe.Id);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Récupère une liste de recettes aléatoires.
        /// </summary>
        /// <param name="count">Nombre de recettes à récupérer</param>
        /// <returns>Liste des recettes aléatoires</returns>
        public async Task<List<Recipe>> GetRandomRecipesAsync(int count)
        {
            // Répartir le nombre de recettes entre les fournisseurs disponibles
            var results = new List<Recipe>();
            var providersCount = _providers.Count;
            
            if (providersCount == 0)
                return results;
                
            // Calculer combien de recettes demander à chaque fournisseur
            var recipesPerProvider = count / providersCount;
            var remainder = count % providersCount;
            
            for (int i = 0; i < providersCount; i++)
            {
                var provider = _providers[i];
                var providerCount = recipesPerProvider + (i < remainder ? 1 : 0);
                
                if (providerCount > 0)
                {
                    var providerResults = await provider.GetRandomRecipesAsync(providerCount);
                    results.AddRange(providerResults);
                }
            }

            // S'assurer qu'on n'a pas plus de recettes que demandé
            return results.Take(count).ToList();
        }

        /// <summary>
        /// Récupère toutes les catégories disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des catégories</returns>
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var results = new Dictionary<string, Category>();
            
            // Collecter les catégories de tous les fournisseurs
            foreach (var provider in _providers)
            {
                var providerCategories = await provider.GetCategoriesAsync();
                foreach (var category in providerCategories)
                {
                    // Utiliser le nom comme clé pour éviter les doublons
                    if (!results.ContainsKey(category.Name))
                    {
                        results[category.Name] = category;
                    }
                }
            }

            return results.Values.OrderBy(c => c.Name).ToList();
        }

        /// <summary>
        /// Récupère les recettes d'une catégorie spécifique.
        /// </summary>
        /// <param name="category">Nom de la catégorie</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes de la catégorie</returns>
        public async Task<List<Recipe>> GetRecipesByCategoryAsync(string category, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<Recipe>();

            var results = new List<Recipe>();
            var seenIds = new HashSet<int>(); // Pour éviter les doublons
            
            // Effectuer la recherche avec chaque fournisseur dans l'ordre de priorité
            foreach (var providerName in _providerPriority)
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    var providerResults = await provider.GetRecipesByCategoryAsync(category, limit);
                    
                    // Ajouter les résultats qui n'ont pas déjà été trouvés
                    foreach (var recipe in providerResults)
                    {
                        if (!seenIds.Contains(recipe.Id))
                        {
                            results.Add(recipe);
                            seenIds.Add(recipe.Id);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Récupère les régions (cuisines) disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des régions</returns>
        public async Task<List<string>> GetCuisinesAsync()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Collecter les cuisines de tous les fournisseurs
            foreach (var provider in _providers)
            {
                var providerCuisines = await provider.GetCuisinesAsync();
                foreach (var cuisine in providerCuisines)
                {
                    results.Add(cuisine);
                }
            }

            return results.OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Récupère les recettes d'une région spécifique.
        /// </summary>
        /// <param name="cuisine">Nom de la région/cuisine</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes de la région</returns>
        public async Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisine, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(cuisine))
                return new List<Recipe>();

            var results = new List<Recipe>();
            var seenIds = new HashSet<int>(); // Pour éviter les doublons
            
            // Effectuer la recherche avec chaque fournisseur dans l'ordre de priorité
            foreach (var providerName in _providerPriority)
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    var providerResults = await provider.GetRecipesByCuisineAsync(cuisine, limit);
                    
                    // Ajouter les résultats qui n'ont pas déjà été trouvés
                    foreach (var recipe in providerResults)
                    {
                        if (!seenIds.Contains(recipe.Id))
                        {
                            results.Add(recipe);
                            seenIds.Add(recipe.Id);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Récupère la liste des ingrédients disponibles à travers toutes les sources.
        /// </summary>
        /// <returns>Liste consolidée des ingrédients</returns>
        public async Task<List<string>> GetIngredientsAsync()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Collecter les ingrédients de tous les fournisseurs
            foreach (var provider in _providers)
            {
                var providerIngredients = await provider.GetIngredientsAsync();
                foreach (var ingredient in providerIngredients)
                {
                    results.Add(ingredient);
                }
            }

            return results.OrderBy(i => i).ToList();
        }

        /// <summary>
        /// Récupère les recettes contenant un ingrédient spécifique.
        /// </summary>
        /// <param name="ingredient">Nom de l'ingrédient</param>
        /// <param name="limit">Nombre maximum de résultats par source</param>
        /// <returns>Liste consolidée des recettes contenant l'ingrédient</returns>
        public async Task<List<Recipe>> GetRecipesByIngredientAsync(string ingredient, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(ingredient))
                return new List<Recipe>();

            var results = new List<Recipe>();
            var seenIds = new HashSet<int>(); // Pour éviter les doublons
            
            // Effectuer la recherche avec chaque fournisseur dans l'ordre de priorité
            foreach (var providerName in _providerPriority)
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (provider != null)
                {
                    var providerResults = await provider.GetRecipesByIngredientAsync(ingredient, limit);
                    
                    // Ajouter les résultats qui n'ont pas déjà été trouvés
                    foreach (var recipe in providerResults)
                    {
                        if (!seenIds.Contains(recipe.Id))
                        {
                            results.Add(recipe);
                            seenIds.Add(recipe.Id);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Obtient les statistiques d'utilisation de tous les fournisseurs.
        /// </summary>
        /// <returns>Dictionnaire associant le nom du fournisseur à son utilisation (utilisé/total)</returns>
        public async Task<Dictionary<string, (int Used, int Total)>> GetApiUsageStatisticsAsync()
        {
            var statistics = new Dictionary<string, (int Used, int Total)>();
            
            foreach (var provider in _providers)
            {
                var remaining = await provider.GetRemainingCallsAsync();
                var total = provider.DailyQuota;
                var used = total - remaining;
                
                statistics[provider.ProviderName] = (used, total);
            }
            
            return statistics;
        }

        /// <summary>
        /// Définit l'ordre de priorité des fournisseurs.
        /// </summary>
        /// <param name="providerOrder">Liste ordonnée des noms de fournisseurs</param>
        public void SetProviderPriority(List<string> providerOrder)
        {
            if (providerOrder == null || !providerOrder.Any())
                throw new ArgumentException("L'ordre des fournisseurs ne peut pas être vide.", nameof(providerOrder));
                
            // Vérifier que tous les noms correspondent à des fournisseurs existants
            var validProviderNames = _providers.Select(p => p.ProviderName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var providerName in providerOrder)
            {
                if (!validProviderNames.Contains(providerName))
                    throw new ArgumentException($"Le fournisseur '{providerName}' n'existe pas.", nameof(providerOrder));
            }
            
            // S'assurer que tous les fournisseurs sont inclus
            var missingProviders = _providers
                .Where(p => !providerOrder.Contains(p.ProviderName, StringComparer.OrdinalIgnoreCase))
                .Select(p => p.ProviderName)
                .ToList();
                
            // Ajouter les fournisseurs manquants à la fin de la liste
            var newOrder = new List<string>(providerOrder);
            newOrder.AddRange(missingProviders);
            
            _providerPriority = newOrder;
        }

        /// <summary>
        /// Obtient l'ordre de priorité actuel des fournisseurs.
        /// </summary>
        /// <returns>Liste ordonnée des noms de fournisseurs</returns>
        public List<string> GetProviderPriority()
        {
            return new List<string>(_providerPriority);
        }
    }
}
