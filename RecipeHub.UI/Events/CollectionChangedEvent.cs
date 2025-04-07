using Prism.Events;
using System;

namespace RecipeHub.UI.Events
{
    /// <summary>
    /// Type d'action effectuée sur une collection.
    /// </summary>
    public enum CollectionAction
    {
        Created,
        Renamed,
        Deleted,
        RecipeAdded,
        RecipeRemoved
    }

    /// <summary>
    /// Arguments pour un événement de changement de collection.
    /// </summary>
    public class CollectionChangedEventArgs
    {
        /// <summary>
        /// Nom de la collection concernée.
        /// </summary>
        public string CollectionName { get; set; }
        
        /// <summary>
        /// Ancien nom de la collection (dans le cas d'un renommage).
        /// </summary>
        public string OldCollectionName { get; set; }
        
        /// <summary>
        /// Identifiant de la recette (si applicable).
        /// </summary>
        public int RecipeId { get; set; }
        
        /// <summary>
        /// Type d'action effectuée sur la collection.
        /// </summary>
        public CollectionAction Action { get; set; }
    }

    /// <summary>
    /// Événement déclenché lors d'un changement dans les collections.
    /// </summary>
    public class CollectionChangedEvent : PubSubEvent<CollectionChangedEventArgs>
    {
    }

    /// <summary>
    /// Événement déclenché lorsqu'une recette est ajoutée/supprimée des favoris.
    /// </summary>
    public class FavoriteChangedEvent : PubSubEvent<(int RecipeId, bool IsFavorite)>
    {
    }
}
