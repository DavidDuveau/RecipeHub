# RecipeHub

Application de bureau destinée aux amateurs de cuisine pour découvrir, organiser et sauvegarder des recettes du monde entier.

## Description

RecipeHub est une application WPF qui permet aux utilisateurs d'explorer une vaste collection de recettes provenant de l'API TheMealDB. 
L'application offre des fonctionnalités avancées de recherche et de filtrage, ainsi que la possibilité de sauvegarder des recettes favorites 
pour un accès hors ligne.

## Fonctionnalités

- **Exploration des recettes** : Parcourez des recettes par catégories, régions ou ingrédients
- **Recherche avancée** : Trouvez des recettes selon vos critères de recherche
- **Favoris** : Sauvegardez vos recettes préférées pour un accès rapide
- **Mode hors connexion** : Accédez à vos recettes favorites même sans connexion internet
- **Gestion des collections** : Organisez vos recettes dans des collections personnalisées
- **Personnalisation** : Adaptez l'interface à vos préférences avec les thèmes clair/sombre

## Structure du projet

Le projet est structuré suivant le pattern MVVM (Model-View-ViewModel) :

- **RecipeHub.Core** : Contient les modèles et interfaces de base
- **RecipeHub.Services** : Implémente les services d'accès aux données (API, stockage local)
- **RecipeHub.UI** : Application WPF principale avec les vues et ViewModels
- **RecipeHub.Tests** : Tests unitaires et d'intégration

## Technologies utilisées

- **.NET 7** : Framework de développement
- **WPF** : Windows Presentation Foundation pour l'interface utilisateur
- **Prism** : Framework MVVM
- **RestSharp** : Client HTTP pour la communication avec l'API
- **Newtonsoft.Json** : Sérialisation/désérialisation JSON
- **MaterialDesignInXAML** : Composants UI modernes
- **SQLite** : Stockage local des données

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
- [ ] Développement de l'interface utilisateur
- [ ] Mise en place du stockage local (favoris)

## Roadmap

Consultez le fichier [roadmap.md](roadmap.md) pour suivre l'avancement prévu du projet.

## Licence

Ce projet est sous licence MIT.
