# Instructions de compilation de RecipeHub

Suite aux corrections effectuées pour résoudre les erreurs de compilation, suivez ces étapes pour reconstruire complètement la solution :

## 1. Nettoyage complet de la solution

Dans Visual Studio 2022 :
1. Ouvrez la solution RecipeHub.sln
2. Sélectionnez `Générer > Nettoyer la solution` dans le menu principal
3. Après le nettoyage, sélectionnez `Générer > Régénérer la solution` 

Alternativement, vous pouvez utiliser la ligne de commande :
```
cd C:\Users\david\source\repos\RecipeHub
dotnet clean
dotnet build
```

## 2. Résolution des erreurs liées aux DLL manquantes

Les erreurs comme `Fichier de métadonnées 'C:\Users\david\source\repos\RecipeHub\RecipeHub.Services\bin\Debug\net7.0-windows\RecipeHub.Services.dll' introuvable` sont généralement résolues après une régénération complète de la solution.

Si ces erreurs persistent après la régénération :

1. Fermez Visual Studio
2. Supprimez manuellement les dossiers bin et obj de chaque projet :
   ```
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.Core\bin"
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.Core\obj"
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.Services\bin"
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.Services\obj"
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.UI\bin"
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.UI\obj"
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.Tests\bin"
   rmdir /s /q "C:\Users\david\source\repos\RecipeHub\RecipeHub.Tests\obj"
   ```
3. Rouvrez Visual Studio et régénérez la solution

## 3. Vérification des références de projet

Si les erreurs persistent encore, vérifiez que les références de projet sont correctes :

1. Dans l'Explorateur de solutions, développez chaque projet
2. Faites un clic droit sur le dossier "Dépendances" ou "References" puis sélectionnez "Ajouter une référence..."
3. Dans l'onglet "Projets", assurez-vous que les références sont correctes :
   - RecipeHub.UI doit référencer RecipeHub.Core et RecipeHub.Services
   - RecipeHub.Services doit référencer RecipeHub.Core
   - RecipeHub.Tests doit référencer RecipeHub.Core, RecipeHub.Services et RecipeHub.UI

## 4. Restauration des packages NuGet

Il est également recommandé de restaurer manuellement les packages NuGet :

1. Clic droit sur la solution dans l'Explorateur de solutions
2. Sélectionnez "Restaurer les packages NuGet"

## 5. Vérification du Target Framework

Assurez-vous que tous les projets ciblent le même framework :

1. Clic droit sur chaque projet et sélectionnez "Propriétés"
2. Dans l'onglet "Application", vérifiez que le "Framework cible" est défini sur "net7.0-windows" pour tous les projets

## Lancement de l'application

Une fois la solution correctement compilée :

1. Assurez-vous que RecipeHub.UI est défini comme projet de démarrage (clic droit sur RecipeHub.UI > Définir comme projet de démarrage)
2. Appuyez sur F5 ou cliquez sur le bouton "Démarrer" pour lancer l'application

Si vous rencontrez encore des problèmes, n'hésitez pas à consulter la documentation .NET ou à demander de l'aide supplémentaire.
