# RecipeHub

Application de bureau destinée aux amateurs de cuisine pour découvrir, organiser et sauvegarder des recettes du monde entier.

## Description

RecipeHub est une application WPF qui permet aux utilisateurs d'explorer une vaste collection de recettes provenant des APIs TheMealDB et Spoonacular. 
L'application offre des fonctionnalités avancées de recherche et de filtrage, ainsi que la possibilité de sauvegarder des recettes favorites 
pour un accès hors ligne. Le système de basculement entre les sources permet d'optimiser l'utilisation des quotas d'API.

## Fonctionnalités

- **Exploration des recettes** : Parcourez des recettes par catégories, régions ou ingrédients
- **Recherche avancée** : Trouvez des recettes selon vos critères de recherche
- **Sources multiples** : Accédez aux recettes de TheMealDB et Spoonacular avec basculement automatique entre les sources
- **Favoris** : Sauvegardez vos recettes préférées pour un accès rapide
- **Mode hors connexion** : Accédez à vos recettes favorites même sans connexion internet
- **Gestion des collections** : Organisez vos recettes dans des collections personnalisées
- **Personnalisation** : Adaptez l'interface à vos préférences avec les thèmes clair/sombre
- **Gestion des APIs** : Visualisez et gérez l'utilisation de vos quotas d'API

## Structure du projet

Le projet est structuré suivant le pattern MVVM (Model-View-ViewModel) :

- **RecipeHub.Core** : Contient les modèles et interfaces de base
- **RecipeHub.Services** : Implémente les services d'accès aux données (API, stockage local, cache, métriques)
  - **API** : Services d'accès aux APIs de recettes (TheMealDB, Spoonacular)
  - **Cache** : Services de mise en cache des données
  - **Metrics** : Gestion des métriques d'utilisation des APIs
  - **Data** : Services de persistance locale des données
- **RecipeHub.UI** : Application WPF principale avec les vues et ViewModels
- **RecipeHub.Tests** : Tests unitaires et d'intégration

## Technologies utilisées

- **.NET 7** : Framework de développement
- **WPF** : Windows Presentation Foundation pour l'interface utilisateur
- **Prism** : Framework MVVM
- **RestSharp** : Client HTTP pour la communication avec l'API
- **Newtonsoft.Json** : Sérialisation/désérialisation JSON
- **SQLite** : Stockage local des données
- **MaterialDesignInXAML** : Composants UI modernes

## Prérequis

- Windows 10 ou supérieur
- .NET 7 SDK
- Visual Studio 2022 (recommandé)

## Installation

1. Clonez le repository
2. Ouvrez la solution RecipeHub.sln dans Visual Studio
3. Restaurez les packages NuGet
4. Compilez et exécutez l'application

## État d'avancement

- [x] Création et structuration du repository
- [x] Mise en place de l'architecture MVVM
- [x] Définition des interfaces principales
- [x] Création des modèles de données de base
- [x] Implémentation des services d'accès à l'API
  - [x] Service TheMealDB
  - [x] Service Spoonacular
  - [x] Service d'agrégation des sources
- [x] Mise en place du système de cache
- [x] Développement des méthodes principales d'accès aux données
- [x] Gestion des métriques d'utilisation des APIs
- [x] Mise en place du stockage local (favoris)
- [ ] Développement de l'interface utilisateur
  - [x] Interface principale (ShellView)
  - [x] Page d'accueil (HomeView)
  - [x] Page d'exploration des recettes (ExploreView)
    - [x] Correction des problèmes de convertisseurs
    - [x] Adaptation de l'interface de recherche d'ingrédients
  - [x] Page de détails des recettes (RecipeDetailsView)
    - [x] Affichage des informations détaillées de la recette
    - [x] Gestion des ingrédients et instructions
    - [x] Intégration des recettes similaires
  - [ ] Page des favoris (FavoritesView)
  - [ ] Page de recherche (SearchView)
  - [ ] Interface de gestion des APIs (ApiManagementView)
- [x] Correction des problèmes de compilation
  - [x] Résolution des erreurs de références aux convertisseurs
  - [x] Adaptation des composants UI pour compatibilité MaterialDesign

## Dernières modifications

- Implémentation du système multi-source avec TheMealDB et Spoonacular
- Mise en place du service d'agrégation de recettes pour gérer les différentes sources
- Implémentation du service de métriques d'utilisation des APIs
- Création du système de persistance des métriques avec SQLite
- Gestion intelligente du basculement entre les sources en fonction des quotas disponibles
- Organisation modulaire du fournisseur Spoonacular pour une meilleure maintenabilité

## Roadmap

Consultez le fichier [roadmap.md](roadmap.md) pour suivre l'avancement prévu du projet.

## Licence

Ce projet est sous licence MIT.
