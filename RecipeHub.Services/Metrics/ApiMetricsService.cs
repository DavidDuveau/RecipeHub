using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using RecipeHub.Core.Interfaces;
using RecipeHub.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RecipeHub.Services.Metrics
{
    /// <summary>
    /// Service de gestion des métriques d'utilisation des APIs.
    /// </summary>
    public class ApiMetricsService : IApiMetricsService
    {
        private readonly string _dbPath;
        private readonly Dictionary<string, ApiUsageMetrics> _metricsCache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _initialized = false;

        /// <summary>
        /// Constructeur du service de métriques d'API.
        /// </summary>
        /// <param name="dbPath">Chemin vers le fichier de base de données SQLite</param>
        public ApiMetricsService(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            _metricsCache = new Dictionary<string, ApiUsageMetrics>(StringComparer.OrdinalIgnoreCase);
            
            // S'assurer que le répertoire existe
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Initialiser la base de données et charger les métriques
            InitializeAsync().Wait();
        }

        /// <summary>
        /// Initialise la base de données et charge les métriques.
        /// </summary>
        private async Task InitializeAsync()
        {
            if (_initialized)
                return;
                
            await _semaphore.WaitAsync();
            try
            {
                if (_initialized)
                    return;
                    
                // Créer la base de données si elle n'existe pas
                if (!File.Exists(_dbPath))
                {
                    await CreateDatabaseAsync();
                }
                
                // Charger les métriques depuis la base de données
                await LoadMetricsAsync();
                
                _initialized = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Crée la base de données SQLite.
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                await connection.OpenAsync();
                
                // Créer la table pour stocker les métriques d'API
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ApiMetrics (
                            ProviderName TEXT PRIMARY KEY,
                            Metrics TEXT NOT NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Enregistre un nouveau fournisseur d'API.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="dailyQuota">Quota quotidien</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task RegisterProviderAsync(string providerName, int dailyQuota)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Le nom du fournisseur ne peut pas être vide.", nameof(providerName));
            
            if (dailyQuota <= 0)
                throw new ArgumentException("Le quota quotidien doit être supérieur à zéro.", nameof(dailyQuota));
            
            await _semaphore.WaitAsync();
            try
            {
                // Vérifier si le fournisseur existe déjà
                if (_metricsCache.TryGetValue(providerName, out var existingMetrics))
                {
                    // Mettre à jour le quota si nécessaire
                    if (existingMetrics.DailyQuota != dailyQuota)
                    {
                        existingMetrics.DailyQuota = dailyQuota;
                        await SaveMetricsAsync();
                    }
                    return;
                }
                
                // Créer de nouvelles métriques pour ce fournisseur
                var metrics = new ApiUsageMetrics(providerName, dailyQuota);
                _metricsCache[providerName] = metrics;
                
                // Enregistrer dans la base de données
                await SaveProviderMetricsAsync(metrics);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Obtient les métriques d'utilisation d'un fournisseur spécifique.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Métriques d'utilisation ou null si le fournisseur n'est pas enregistré</returns>
        public async Task<ApiUsageMetrics?> GetProviderMetricsAsync(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                return null;
            
            await _semaphore.WaitAsync();
            try
            {
                // Vérifier si le fournisseur est dans le cache
                if (_metricsCache.TryGetValue(providerName, out var metrics))
                {
                    // Vérifier si le compteur doit être réinitialisé
                    if (metrics.ShouldResetCounter())
                    {
                        metrics.ResetCounter();
                        await SaveProviderMetricsAsync(metrics);
                    }
                    return metrics;
                }
                
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Obtient les métriques d'utilisation de tous les fournisseurs enregistrés.
        /// </summary>
        /// <returns>Dictionnaire associant le nom du fournisseur à ses métriques</returns>
        public async Task<Dictionary<string, ApiUsageMetrics>> GetAllProviderMetricsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Créer une copie du dictionnaire pour éviter les problèmes de concurrence
                var result = new Dictionary<string, ApiUsageMetrics>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var kvp in _metricsCache)
                {
                    // Vérifier si le compteur doit être réinitialisé
                    if (kvp.Value.ShouldResetCounter())
                    {
                        kvp.Value.ResetCounter();
                        await SaveProviderMetricsAsync(kvp.Value);
                    }
                    
                    // Ajouter au résultat
                    result[kvp.Key] = kvp.Value;
                }
                
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Incrémente le compteur d'utilisation d'un fournisseur.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <param name="count">Nombre d'appels à comptabiliser (1 par défaut)</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task IncrementUsageAsync(string providerName, int count = 1)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                return;
                
            if (count <= 0)
                return;
            
            await _semaphore.WaitAsync();
            try
            {
                // Vérifier si le fournisseur est dans le cache
                if (_metricsCache.TryGetValue(providerName, out var metrics))
                {
                    // Incrémenter le compteur
                    metrics.IncrementUsage(count);
                    
                    // Enregistrer les modifications
                    await SaveProviderMetricsAsync(metrics);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Réinitialise le compteur quotidien d'un fournisseur.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task ResetCounterAsync(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                return;
            
            await _semaphore.WaitAsync();
            try
            {
                // Vérifier si le fournisseur est dans le cache
                if (_metricsCache.TryGetValue(providerName, out var metrics))
                {
                    // Réinitialiser le compteur
                    metrics.ResetCounter();
                    
                    // Enregistrer les modifications
                    await SaveProviderMetricsAsync(metrics);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Vérifie si le quota d'un fournisseur est dépassé.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Vrai si le quota est dépassé, faux sinon</returns>
        public async Task<bool> IsQuotaExceededAsync(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                return true; // Par sécurité, considérer que le quota est dépassé si le nom est invalide
            
            var metrics = await GetProviderMetricsAsync(providerName);
            return metrics?.IsQuotaExceeded() ?? true;
        }

        /// <summary>
        /// Obtient le nombre d'appels restants pour un fournisseur aujourd'hui.
        /// </summary>
        /// <param name="providerName">Nom du fournisseur</param>
        /// <returns>Nombre d'appels restants</returns>
        public async Task<int> GetRemainingCallsAsync(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                return 0;
            
            var metrics = await GetProviderMetricsAsync(providerName);
            return metrics?.GetRemainingCalls() ?? 0;
        }

        /// <summary>
        /// Obtient le fournisseur ayant le plus d'appels restants.
        /// </summary>
        /// <param name="preferredOrder">Ordre de préférence des fournisseurs (optionnel)</param>
        /// <returns>Nom du fournisseur recommandé ou null si aucun n'est disponible</returns>
        public async Task<string?> GetRecommendedProviderAsync(List<string>? preferredOrder = null)
        {
            var allMetrics = await GetAllProviderMetricsAsync();
            
            if (allMetrics.Count == 0)
                return null;
            
            // Si un ordre de préférence est spécifié, l'utiliser
            if (preferredOrder != null && preferredOrder.Count > 0)
            {
                foreach (var providerName in preferredOrder)
                {
                    if (allMetrics.TryGetValue(providerName, out var metrics) && !metrics.IsQuotaExceeded())
                    {
                        return providerName;
                    }
                }
            }
            
            // Sinon, trouver le fournisseur avec le plus d'appels restants
            string? bestProvider = null;
            int maxRemaining = -1;
            
            foreach (var kvp in allMetrics)
            {
                var remaining = kvp.Value.GetRemainingCalls();
                if (remaining > maxRemaining)
                {
                    maxRemaining = remaining;
                    bestProvider = kvp.Key;
                }
            }
            
            // Si tous les quotas sont dépassés, retourner null
            return maxRemaining > 0 ? bestProvider : null;
        }

        /// <summary>
        /// Sauvegarde les métriques d'utilisation.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task SaveMetricsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Sauvegarder chaque fournisseur
                foreach (var metrics in _metricsCache.Values)
                {
                    await SaveProviderMetricsAsync(metrics);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Sauvegarde les métriques d'un fournisseur spécifique.
        /// </summary>
        /// <param name="metrics">Métriques à sauvegarder</param>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        private async Task SaveProviderMetricsAsync(ApiUsageMetrics metrics)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                await connection.OpenAsync();
                
                // Sérialiser les métriques en JSON
                var json = JsonConvert.SerializeObject(metrics);
                
                // Enregistrer dans la base de données
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT OR REPLACE INTO ApiMetrics (ProviderName, Metrics)
                        VALUES (@ProviderName, @Metrics);";
                    
                    command.Parameters.AddWithValue("@ProviderName", metrics.ProviderName);
                    command.Parameters.AddWithValue("@Metrics", json);
                    
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Charge les métriques d'utilisation précédemment sauvegardées.
        /// </summary>
        /// <returns>Tâche représentant l'opération asynchrone</returns>
        public async Task LoadMetricsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Vider le cache actuel
                _metricsCache.Clear();
                
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    await connection.OpenAsync();
                    
                    // Charger toutes les métriques
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT ProviderName, Metrics FROM ApiMetrics;";
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var providerName = reader.GetString(0);
                                var json = reader.GetString(1);
                                
                                try
                                {
                                    var metrics = JsonConvert.DeserializeObject<ApiUsageMetrics>(json);
                                    if (metrics != null)
                                    {
                                        // Vérifier si le compteur doit être réinitialisé
                                        if (metrics.ShouldResetCounter())
                                        {
                                            metrics.ResetCounter();
                                            // On ne sauvegarde pas ici pour éviter des opérations en cascade
                                        }
                                        
                                        _metricsCache[providerName] = metrics;
                                    }
                                }
                                catch
                                {
                                    // Ignorer les erreurs de désérialisation
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
