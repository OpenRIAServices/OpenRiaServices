using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Manager that observes and collates the events raised by a <see cref="SortDescriptorCollection"/>
    /// and all the <see cref="SortDescriptor"/>s it contains.
    /// </summary>
    /// <remarks>
    /// The manager also keeps the collection synchronized with the specified
    /// <see cref="SortDescriptionCollection"/>. The main challenge in synchronization is
    /// <see cref="SortDescriptor"/>s can exist in an intermediate state while <see cref="SortDescription"/>s
    /// cannot. This may occasionally lead to a state where the <see cref="SortDescriptorCollection"/> has
    /// more items than the <see cref="SortDescriptionCollection"/>. This state will resolve once all of
    /// the <see cref="SortDescriptor"/>s are fully defined.
    /// </remarks>
    internal class SortCollectionManager : CollectionManager
    {
        #region Member fields

        private readonly SortDescriptorCollection _sourceCollection;
        private readonly SortDescriptionCollection _descriptionCollection;

        // used during synchronization
        private bool _ignoreChanges;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortCollectionManager"/> class.
        /// </summary>
        /// <param name="sourceCollection">The collection of <see cref="SortDescriptor"/>s to manage</param>
        /// <param name="descriptionCollection">The collection of <see cref="SortDescription"/>s to synchronize with the <paramref name="sourceCollection"/></param>
        /// <param name="expressionCache">The cache with entries for the <see cref="SortDescriptor"/>s</param>
        /// <param name="validationAction">The callback for validating items that are added or changed</param>
        public SortCollectionManager(SortDescriptorCollection sourceCollection, SortDescriptionCollection descriptionCollection, ExpressionCache expressionCache, Action<SortDescriptor> validationAction)
        {
            if (sourceCollection == null)
            {
                throw new ArgumentNullException("sourceCollection");
            }
            if (descriptionCollection == null)
            {
                throw new ArgumentNullException("descriptionCollection");
            }
            if (expressionCache == null)
            {
                throw new ArgumentNullException("expressionCache");
            }
            if (validationAction == null)
            {
                throw new ArgumentNullException("validationAction");
            }

            this._sourceCollection = sourceCollection;
            this._descriptionCollection = descriptionCollection;
            this.ExpressionCache = expressionCache;
            this.ValidationAction = (item) => validationAction((SortDescriptor)item);
            this.AsINotifyPropertyChangedFunc = (item) => ((SortDescriptor)item).Notifier;

            ((INotifyCollectionChanged)descriptionCollection).CollectionChanged += this.HandleDescriptionCollectionChanged;

            this.AddCollection(sourceCollection);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Overridden to synchronize collections.
        /// </summary>
        /// <param name="sender">The collection that changed</param>
        /// <param name="e">The collection change event</param>
        protected override void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.HandleCollectionChanged(sender, e);

            if (this._ignoreChanges)
            {
                return;
            }

            this.HandleSortDescriptorCollectionChanged(e);

            this.AssertCollectionsAreEquivalent();
        }

        /// <summary>
        /// Synchronizes collections in response to a change in the _descriptionCollection.
        /// </summary>
        /// <param name="sender">The collection that changed</param>
        /// <param name="e">The collection change event</param>
        private void HandleDescriptionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnCollectionChanged();

            if (this._ignoreChanges)
            {
                return;
            }

            this.HandleSortDescriptionCollectionChanged(e);

            this.AssertCollectionsAreEquivalent();
        }

        /// <summary>
        /// Overridden to synchronize collections.
        /// </summary>
        /// <param name="sender">The item that changed</param>
        /// <param name="e">The property change event</param>
        protected override void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.HandlePropertyChanged(sender, e);

            if (this._ignoreChanges)
            {
                return;
            }

            this.HandleSortDescriptorChanged((SortDescriptor)sender, e);

            this.AssertCollectionsAreEquivalent();
        }

        /// <summary>
        /// Overridden to suppress events when synchronizing.
        /// </summary>
        protected override void OnCollectionChanged()
        {
            if (this._ignoreChanges)
            {
                return;
            }

            base.OnCollectionChanged();
        }

        /// <summary>
        /// Overridden to suppress events when synchronizing.
        /// </summary>
        protected override void OnPropertyChanged()
        {
            if (this._ignoreChanges)
            {
                return;
            }

            base.OnPropertyChanged();
        }

        /// <summary>
        /// Resets the <paramref name="sortDescriptors"/> collection to match the <paramref name="sortDescriptions"/> collection.
        /// </summary>
        /// <param name="sortDescriptions">The collection to match</param>
        /// <param name="sortDescriptors">The collection to reset</param>
        private static void ResetToSortDescriptions(SortDescriptionCollection sortDescriptions, SortDescriptorCollection sortDescriptors)
        {
            sortDescriptors.Clear();
            foreach (SortDescription description in sortDescriptions)
            {
                sortDescriptors.Add(SortCollectionManager.GetDescriptorFromDescription(description));
            }
        }

        /// <summary>
        /// Resets the <paramref name="sortDescriptions"/> collection to match the <paramref name="sortDescriptors"/> collection.
        /// </summary>
        /// <param name="sortDescriptions">The collection to reset</param>
        /// <param name="sortDescriptors">The collection to match</param>
        private static void ResetToSortDescriptors(SortDescriptionCollection sortDescriptions, SortDescriptorCollection sortDescriptors)
        {
            sortDescriptions.Clear();
            foreach (SortDescriptor descriptor in sortDescriptors)
            {
                SortDescription? description = SortCollectionManager.GetDescriptionFromDescriptor(descriptor);
                if (description.HasValue)
                {
                    sortDescriptions.Add(description.Value);
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="SortDescription"/> equivalent to the specified descriptor.
        /// </summary>
        /// <param name="sortDescriptor">The descriptor to get a description from</param>
        /// <returns>A <see cref="SortDescription"/> equivalent to the specified descriptor</returns>
        private static SortDescription? GetDescriptionFromDescriptor(SortDescriptor sortDescriptor)
        {
            if (string.IsNullOrEmpty(sortDescriptor.PropertyPath))
            {
                return null;
            }
            return new SortDescription(sortDescriptor.PropertyPath, sortDescriptor.Direction);
        }

        /// <summary>
        /// Returns a <see cref="SortDescriptor"/> equivalent to the specified description
        /// </summary>
        /// <param name="sortDescription">The description to get a descriptor from</param>
        /// <returns>A <see cref="SortDescriptor"/> equivalent to the specified description</returns>
        private static SortDescriptor GetDescriptorFromDescription(SortDescription sortDescription)
        {
            return new SortDescriptor(sortDescription.PropertyName, sortDescription.Direction);
        }

        /// <summary>
        /// Synchronizes the sort descriptors collection to the sort descriptions collection.
        /// </summary>
        /// <param name="e">The collection change event</param>
        private void HandleSortDescriptionCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this._ignoreChanges = true;
            try
            {
                // We have to reset in a number of situations
                // 1) Resetting the SortDescriptions
                // 2) Collections were not equal before replacing SortDescriptions
                // 3) Collections were not equal before removing SortDescriptions
                // 4) Collections were not equal before adding SortDescriptions
                if ((e.Action == NotifyCollectionChangedAction.Reset) ||
                    ((e.Action == NotifyCollectionChangedAction.Replace) && ((this._sourceCollection.Count + e.NewItems.Count) != (this._descriptionCollection.Count + e.OldItems.Count))) ||
                    ((e.Action == NotifyCollectionChangedAction.Remove) && (this._sourceCollection.Count != (this._descriptionCollection.Count + e.OldItems.Count))) ||
                    ((e.Action == NotifyCollectionChangedAction.Add) && ((this._sourceCollection.Count + e.NewItems.Count) != this._descriptionCollection.Count)))
                {
                    SortCollectionManager.ResetToSortDescriptions(this._descriptionCollection, this._sourceCollection);
                }
                else
                {
                    if ((e.Action == NotifyCollectionChangedAction.Remove) ||
                        (e.Action == NotifyCollectionChangedAction.Replace))
                    {
                        int index = e.OldStartingIndex;
                        if (e.Action == NotifyCollectionChangedAction.Replace) // TODO: This is a ObservableCollection bug!
                        {
                            index = e.NewStartingIndex;
                        }
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            this._sourceCollection.RemoveAt(index);
                        }
                    }
                    if ((e.Action == NotifyCollectionChangedAction.Add) ||
                        (e.Action == NotifyCollectionChangedAction.Replace))
                    {
                        int index = e.NewStartingIndex;
                        foreach (object item in e.NewItems)
                        {
                            this._sourceCollection.Insert(index++, SortCollectionManager.GetDescriptorFromDescription((SortDescription)item));
                        }
                    }
                }
            }
            finally
            {
                this._ignoreChanges = false;
            }
        }

        /// <summary>
        /// Synchronizes the sort descriptions collection to the sort descriptors collection.
        /// </summary>
        /// <param name="e">The collection change event</param>
        private void HandleSortDescriptorCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this._ignoreChanges = true;
            try
            {
                // We have to reset in a number of situations
                // 1) Resetting the SortDescriptors
                // 2) Collections were not equal before replacing SortDescriptors
                // 3) Collections were not equal before removing SortDescriptors
                // 4) Collections were not equal before adding SortDescriptors
                if ((e.Action == NotifyCollectionChangedAction.Reset) ||
                    ((e.Action == NotifyCollectionChangedAction.Replace) && ((this._sourceCollection.Count + e.OldItems.Count) != (this._descriptionCollection.Count + e.NewItems.Count))) ||
                    ((e.Action == NotifyCollectionChangedAction.Remove) && ((this._sourceCollection.Count + e.OldItems.Count) != this._descriptionCollection.Count)) ||
                    ((e.Action == NotifyCollectionChangedAction.Add) && (this._sourceCollection.Count != (this._descriptionCollection.Count + e.NewItems.Count))))
                {
                    SortCollectionManager.ResetToSortDescriptors(this._descriptionCollection, this._sourceCollection);
                }
                else
                {
                    if ((e.Action == NotifyCollectionChangedAction.Remove) ||
                        (e.Action == NotifyCollectionChangedAction.Replace))
                    {
                        int index = e.OldStartingIndex;
                        if (e.Action == NotifyCollectionChangedAction.Replace) // TODO: This is a DependencyObjectCollection bug!
                        {
                            index = e.NewStartingIndex;
                        }
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            this._descriptionCollection.RemoveAt(index);
                        }
                    }
                    if ((e.Action == NotifyCollectionChangedAction.Add) ||
                        (e.Action == NotifyCollectionChangedAction.Replace))
                    {
                        int index = e.NewStartingIndex;
                        foreach (object item in e.NewItems)
                        {
                            SortDescription? description = SortCollectionManager.GetDescriptionFromDescriptor((SortDescriptor)item);
                            if (description.HasValue)
                            {
                                this._descriptionCollection.Insert(index++, description.Value);
                            }
                        }
                    }
                }
            }
            finally
            {
                this._ignoreChanges = false;
            }
        }

        /// <summary>
        /// Synchronizes the sort descriptions collection to the sort descriptors collection.
        /// </summary>
        /// <param name="descriptor">The descriptor that changed</param>
        /// <param name="e">The property change event</param>
        private void HandleSortDescriptorChanged(SortDescriptor descriptor, PropertyChangedEventArgs e)
        {
            this._ignoreChanges = true;
            try
            {
                // We have to reset when the collections were not equal before the change
                if (this._sourceCollection.Count != this._descriptionCollection.Count)
                {
                    SortCollectionManager.ResetToSortDescriptors(this._descriptionCollection, this._sourceCollection);
                }
                else
                {
                    int index = this._sourceCollection.IndexOf(descriptor);
                    SortDescription? description = SortCollectionManager.GetDescriptionFromDescriptor(descriptor);
                    if (!description.HasValue)
                    {
                        this._descriptionCollection.RemoveAt(index);
                    }
                    else
                    {
                        this._descriptionCollection[index] = description.Value;
                    }
                }
            }
            finally
            {
                this._ignoreChanges = false;
            }
        }

        /// <summary>
        /// Asserts that the collections are equivalent.
        /// </summary>
        [Conditional("DEBUG")]
        private void AssertCollectionsAreEquivalent()
        {
            if (this._sourceCollection.Count != this._descriptionCollection.Count)
            {
                // Collections may not be equivalent if a descriptor isn't fully defined
                return;
            }
            Debug.Assert(SortCollectionManager.AreEquivalent(this._descriptionCollection, this._sourceCollection), "Collections should be equal.");
        }

        /// <summary>
        /// Determines whether the <paramref name="sortDescriptions"/> are equivalent to the <paramref name="sortDescriptors"/>.
        /// </summary>
        /// <param name="sortDescriptions">The descriptions to compare</param>
        /// <param name="sortDescriptors">The descriptors to compare</param>
        /// <returns><c>true</c> if the collections are equivalent</returns>
        public static bool AreEquivalent(SortDescriptionCollection sortDescriptions, SortDescriptorCollection sortDescriptors)
        {
            Debug.Assert((sortDescriptions != null) && (sortDescriptors != null), "Both should be non-null.");

            if (sortDescriptions.Count != sortDescriptors.Count)
            {
                return false;
            }

            for (int i = 0, count = sortDescriptions.Count; i < count; i++)
            {
                if (!SortCollectionManager.AreEquivalent(sortDescriptions[i], sortDescriptors[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the <paramref name="sortDescription"/> and <paramref name="sortDescriptor"/> are equivalent.
        /// </summary>
        /// <param name="sortDescription">The description to compare</param>
        /// <param name="sortDescriptor">The descriptor to compare</param>
        /// <returns><c>true</c> if the two are equivalent</returns>
        private static bool AreEquivalent(SortDescription sortDescription, SortDescriptor sortDescriptor)
        {
            Debug.Assert((sortDescription != null) && (sortDescriptor != null), "Both should be non-null.");

            return (sortDescription.Direction == sortDescriptor.Direction) &&
                (sortDescription.PropertyName == sortDescriptor.PropertyPath);
        }

        #endregion
    }
}
