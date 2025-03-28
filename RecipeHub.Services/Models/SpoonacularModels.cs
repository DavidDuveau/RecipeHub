using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RecipeHub.Services.Models
{
    /// <summary>
    /// Classe représentant une recette dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularRecipe
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;

        [JsonProperty("imageType")]
        public string ImageType { get; set; } = string.Empty;

        [JsonProperty("servings")]
        public int Servings { get; set; }

        [JsonProperty("readyInMinutes")]
        public int ReadyInMinutes { get; set; }

        [JsonProperty("license")]
        public string License { get; set; } = string.Empty;

        [JsonProperty("sourceName")]
        public string SourceName { get; set; } = string.Empty;

        [JsonProperty("sourceUrl")]
        public string SourceUrl { get; set; } = string.Empty;

        [JsonProperty("spoonacularSourceUrl")]
        public string SpoonacularSourceUrl { get; set; } = string.Empty;

        [JsonProperty("aggregateLikes")]
        public int AggregateLikes { get; set; }

        [JsonProperty("healthScore")]
        public double HealthScore { get; set; }

        [JsonProperty("spoonacularScore")]
        public double SpoonacularScore { get; set; }

        [JsonProperty("pricePerServing")]
        public double PricePerServing { get; set; }

        [JsonProperty("analyzedInstructions")]
        public List<SpoonacularInstruction> AnalyzedInstructions { get; set; } = new List<SpoonacularInstruction>();

        [JsonProperty("cheap")]
        public bool Cheap { get; set; }

        [JsonProperty("creditsText")]
        public string CreditsText { get; set; } = string.Empty;

        [JsonProperty("cuisines")]
        public List<string> Cuisines { get; set; } = new List<string>();

        [JsonProperty("dairyFree")]
        public bool DairyFree { get; set; }

        [JsonProperty("diets")]
        public List<string> Diets { get; set; } = new List<string>();

        [JsonProperty("gaps")]
        public string Gaps { get; set; } = string.Empty;

        [JsonProperty("glutenFree")]
        public bool GlutenFree { get; set; }

        [JsonProperty("instructions")]
        public string Instructions { get; set; } = string.Empty;

        [JsonProperty("ketogenic")]
        public bool Ketogenic { get; set; }

        [JsonProperty("lowFodmap")]
        public bool LowFodmap { get; set; }

        [JsonProperty("occasions")]
        public List<string> Occasions { get; set; } = new List<string>();

        [JsonProperty("sustainable")]
        public bool Sustainable { get; set; }

        [JsonProperty("vegan")]
        public bool Vegan { get; set; }

        [JsonProperty("vegetarian")]
        public bool Vegetarian { get; set; }

        [JsonProperty("veryHealthy")]
        public bool VeryHealthy { get; set; }

        [JsonProperty("veryPopular")]
        public bool VeryPopular { get; set; }

        [JsonProperty("whole30")]
        public bool Whole30 { get; set; }

        [JsonProperty("weightWatcherSmartPoints")]
        public int WeightWatcherSmartPoints { get; set; }

        [JsonProperty("dishTypes")]
        public List<string> DishTypes { get; set; } = new List<string>();

        [JsonProperty("extendedIngredients")]
        public List<SpoonacularIngredient> ExtendedIngredients { get; set; } = new List<SpoonacularIngredient>();

        [JsonProperty("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonProperty("winePairing")]
        public SpoonacularWinePairing WinePairing { get; set; } = new SpoonacularWinePairing();
    }

    /// <summary>
    /// Classe représentant une instruction de recette dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularInstruction
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("steps")]
        public List<SpoonacularStep> Steps { get; set; } = new List<SpoonacularStep>();
    }

    /// <summary>
    /// Classe représentant une étape d'instruction dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularStep
    {
        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("step")]
        public string Step { get; set; } = string.Empty;

        [JsonProperty("ingredients")]
        public List<SpoonacularStepItem> Ingredients { get; set; } = new List<SpoonacularStepItem>();

        [JsonProperty("equipment")]
        public List<SpoonacularStepItem> Equipment { get; set; } = new List<SpoonacularStepItem>();
    }

    /// <summary>
    /// Classe représentant un élément d'étape (ingrédient ou équipement) dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularStepItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("localizedName")]
        public string LocalizedName { get; set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;
    }

    /// <summary>
    /// Classe représentant un ingrédient dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularIngredient
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("aisle")]
        public string Aisle { get; set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;

        [JsonProperty("consistency")]
        public string Consistency { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("original")]
        public string Original { get; set; } = string.Empty;

        [JsonProperty("originalString")]
        public string OriginalString { get; set; } = string.Empty;

        [JsonProperty("originalName")]
        public string OriginalName { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonProperty("meta")]
        public List<string> Meta { get; set; } = new List<string>();

        [JsonProperty("metaInformation")]
        public List<string> MetaInformation { get; set; } = new List<string>();

        [JsonProperty("measures")]
        public SpoonacularMeasures Measures { get; set; } = new SpoonacularMeasures();
    }

    /// <summary>
    /// Classe représentant les mesures d'un ingrédient dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularMeasures
    {
        [JsonProperty("us")]
        public SpoonacularMeasure Us { get; set; } = new SpoonacularMeasure();

        [JsonProperty("metric")]
        public SpoonacularMeasure Metric { get; set; } = new SpoonacularMeasure();
    }

    /// <summary>
    /// Classe représentant une mesure dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularMeasure
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("unitShort")]
        public string UnitShort { get; set; } = string.Empty;

        [JsonProperty("unitLong")]
        public string UnitLong { get; set; } = string.Empty;
    }

    /// <summary>
    /// Classe représentant un accord de vin dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularWinePairing
    {
        [JsonProperty("pairedWines")]
        public List<string> PairedWines { get; set; } = new List<string>();

        [JsonProperty("pairingText")]
        public string PairingText { get; set; } = string.Empty;

        [JsonProperty("productMatches")]
        public List<SpoonacularProductMatch> ProductMatches { get; set; } = new List<SpoonacularProductMatch>();
    }

    /// <summary>
    /// Classe représentant un produit dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularProductMatch
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("price")]
        public string Price { get; set; } = string.Empty;

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonProperty("averageRating")]
        public double AverageRating { get; set; }

        [JsonProperty("ratingCount")]
        public int RatingCount { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;
    }

    /// <summary>
    /// Classe représentant le résultat d'une recherche dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularSearchResult
    {
        [JsonProperty("results")]
        public List<SpoonacularRecipe> Results { get; set; } = new List<SpoonacularRecipe>();

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("totalResults")]
        public int TotalResults { get; set; }
    }

    /// <summary>
    /// Classe représentant le résultat d'une recherche aléatoire dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularRandomResult
    {
        [JsonProperty("recipes")]
        public List<SpoonacularRecipe> Recipes { get; set; } = new List<SpoonacularRecipe>();
    }

    /// <summary>
    /// Classe représentant le résultat d'une recherche par ingrédient dans l'API Spoonacular.
    /// </summary>
    public class SpoonacularIngredientResult
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("image")]
        public string Image { get; set; } = string.Empty;

        [JsonProperty("usedIngredientCount")]
        public int UsedIngredientCount { get; set; }

        [JsonProperty("missedIngredientCount")]
        public int MissedIngredientCount { get; set; }

        [JsonProperty("missedIngredients")]
        public List<SpoonacularIngredient> MissedIngredients { get; set; } = new List<SpoonacularIngredient>();

        [JsonProperty("usedIngredients")]
        public List<SpoonacularIngredient> UsedIngredients { get; set; } = new List<SpoonacularIngredient>();

        [JsonProperty("unusedIngredients")]
        public List<SpoonacularIngredient> UnusedIngredients { get; set; } = new List<SpoonacularIngredient>();

        [JsonProperty("likes")]
        public int Likes { get; set; }
    }
}
