using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace System.Windows.Controls
{
    /// <summary>
    /// Manager that observes and collates the events raised by a <see cref="GroupDescriptorCollection"/>
    /// and all the <see cref="GroupDescriptor"/>s it contains.
    /// </summary>
    /// <remarks>
    /// The manager also keeps the collection synchronized with the specified collection of
    /// <see cref="GroupDescription"/>s. The main challenge in synchronization is
    /// <see cref="GroupDescriptor"/>s can exist in an intermediate state while <see cref="GroupDescription"/>s
    /// cannot. This may occasionally lead to a state where the <see cref="GroupDescriptorCollection"/> has
    /// more items than the collection of <see cref="GroupDescription"/>s. This state will resolve once all of
    /// the <see cref="GroupDescriptor"/>s are fully defined.
    /// </remarks>
    internal class GroupCollectionManager : CollectionManager
    {
        #region Member fields

        private readonly GroupDescriptorCollection _sourceCollection;
        private readonly ObservableCollection<GroupDescription> _descriptionCollection;
        private readonly Action<GroupDescriptor> _groupValidationAction;

        // used during synchronization
        private bool _ignoreChanges;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupCollectionManager"/> class.
        /// </summary>
        /// <param name="sourceCollection">The collection of <see cref="GroupDescriptor"/>s to manage</param>
        /// <param name="descriptionCollection">The collection of <see cref="GroupDescription"/>s to synchronize with the <paramref name="sourceCollection"/></param>
        /// <param name="expressionCache">The cache with entries for the <see cref="GroupDescriptor"/>s</param>
        /// <param name="validationAction">The callback for validating items that are added or changed</param>
        public GroupCollectionManager(GroupDescriptorCollection sourceCollection, ObservableCollection<GroupDescription> descriptionCollection, ExpressionCache expressionCache, Action<GroupDescriptor> validationAction)
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
            this._groupValidationAction = validationAction;

            this.ValidationAction = this.Validate;
            this.AsINotifyPropertyChangedFunc = this.AsINotifyPropertyChanged;

            this.AddCollection(sourceCollection);
            this.AddCollection(descriptionCollection);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Action that validates <see cref="GroupDescriptor"/>s when they are added or changed.
        /// </summary>
        /// <param name="item">The item to validate</param>
        private void Validate(object item)
        {
            GroupDescriptor gd = item as GroupDescriptor;
            if (gd != null)
            {
                this._groupValidationAction(gd);
            }
        }

        /// <summary>
        /// Function that returns the <see cref="INotifyPropertyChanged"/> implementation for the
        /// specified <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The item to get the <see cref="INotifyPropertyChanged"/> implementation for</param>
        /// <returns>The <see cref="INotifyPropertyChanged"/> implementation for the specified <paramref name="item"/>
        /// </returns>
        private INotifyPropertyChanged AsINotifyPropertyChanged(object item)
        {
            if (item is GroupDescription)
            {
                return (GroupDescription)item;
            }
            return ((GroupDescriptor)item).Notifier;
        }

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

            if (sender == this._descriptionCollection)
            {
                this.HandleGroupDescriptionCollectionChanged(e);
            }
            else
            {
                this.HandleGroupDescriptorCollectionChanged(e);
            }

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

            if (sender is GroupDescription)
            {
                this.HandleGroupDescriptionChanged((GroupDescription)sender, e);
            }
            else
            {
                this.HandleGroupDescriptorChanged((GroupDescriptor)sender, e);
            }

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
        /// Resets the <paramref name="groupDescriptors"/> collection to match the <paramref name="groupDescriptions"/> collection.
        /// </summary>
        /// <param name="groupDescriptions">The collection to match</param>
        /// <param name="groupDescriptors">The collection to reset</param>
        private static void ResetToGroupDescriptions(ObservableCollection<GroupDescription> groupDescriptions, GroupDescriptorCollection groupDescriptors)
        {
            groupDescriptors.Clear();
            foreach (GroupDescription description in groupDescriptions)
            {
                groupDescriptors.Add(GroupCollectionManager.GetDescriptorFromDescription(description));
            }
        }

        /// <summary>
        /// Resets the <paramref name="groupDescriptions"/> collection to match the <paramref name="groupDescriptors"/> collection.
        /// </summary>
        /// <param name="groupDescriptions">The collection to reset</param>
        /// <param name="groupDescriptors">The collection to match</param>
        private static void ResetToGroupDescriptors(ObservableCollection<GroupDescription> groupDescriptions, GroupDescriptorCollection groupDescriptors)
        {
            groupDescriptions.Clear();
            foreach (GroupDescriptor descriptor in groupDescriptors)
            {
                GroupDescription description = GroupCollectionManager.GetDescriptionFromDescriptor(descriptor);
                if (description != null)
                {
                    groupDescriptions.Add(description);
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="GroupDescription"/> equivalent to the specified descriptor.
        /// </summary>
        /// <param name="groupDescriptor">The descriptor to get a description from</param>
        /// <returns>A <see cref="GroupDescription"/> equivalent to the specified descriptor</returns>
        private static GroupDescription GetDescriptionFromDescriptor(GroupDescriptor groupDescriptor)
        {
            if (string.IsNullOrEmpty(groupDescriptor.PropertyPath))
            {
                return null;
            }
            return new PropertyGroupDescription(groupDescriptor.PropertyPath);
        }

        /// <summary>
        /// Returns a <see cref="GroupDescriptor"/> equivalent to the specified description
        /// </summary>
        /// <param name="groupDescription">The description to get a descriptor from</param>
        /// <returns>A <see cref="GroupDescriptor"/> equivalent to the specified description</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the description is not a
        /// <see cref="PropertyGroupDescription"/>.
        /// </exception>
        private static GroupDescriptor GetDescriptorFromDescription(GroupDescription groupDescription)
        {
            PropertyGroupDescription propertyGroupDescription = groupDescription as PropertyGroupDescription;
            if (propertyGroupDescription == null)
            {
                throw new InvalidOperationException(DomainDataSourceResources.RequiresPropertyGroupDescription);
            }
            return new GroupDescriptor(propertyGroupDescription.PropertyName);
        }

        /// <summary>
        /// Synchronizes the group descriptors collection to the group descriptions collection.
        /// </summary>
        /// <param name="e">The collection change event</param>
        private void HandleGroupDescriptionCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this._ignoreChanges = true;
            try
            {
                // We have to reset in a number of situations
                // 1) Resetting the GroupDescriptions
                // 2) Collections were not equal before replacing GroupDescriptions
                // 3) Collections were not equal before removing GroupDescriptions
                // 4) Collections were not equal before adding GroupDescriptions
                if ((e.Action == NotifyCollectionChangedAction.Reset) ||
                    ((e.Action == NotifyCollectionChangedAction.Replace) && ((this._sourceCollection.Count + e.NewItems.Count) != (this._descriptionCollection.Count + e.OldItems.Count))) ||
                    ((e.Action == NotifyCollectionChangedAction.Remove) && (this._sourceCollection.Count != (this._descriptionCollection.Count + e.OldItems.Count))) ||
                    ((e.Action == NotifyCollectionChangedAction.Add) && ((this._sourceCollection.Count + e.NewItems.Count) != this._descriptionCollection.Count)))
                {
                    GroupCollectionManager.ResetToGroupDescriptions(this._descriptionCollection, this._sourceCollection);
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
                            this._sourceCollection.Insert(index++, GroupCollectionManager.GetDescriptorFromDescription((GroupDescription)item));
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
        /// Synchronizes the group descriptions collection to the group descriptors collection.
        /// </summary>
        /// <param name="e">The collection change event</param>
        private void HandleGroupDescriptorCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this._ignoreChanges = true;
            try
            {
                // We have to reset in a number of situations
                // 1) Resetting the GroupDescriptors
                // 2) Collections were not equal before replacing GroupDescriptors
                // 3) Collections were not equal before removing GroupDescriptors
                // 4) Collections were not equal before adding GroupDescriptors
                if ((e.Action == NotifyCollectionChangedAction.Reset) ||
                    ((e.Action == NotifyCollectionChangedAction.Replace) && ((this._sourceCollection.Count + e.OldItems.Count) != (this._descriptionCollection.Count + e.NewItems.Count))) ||
                    ((e.Action == NotifyCollectionChangedAction.Remove) && ((this._sourceCollection.Count + e.OldItems.Count) != this._descriptionCollection.Count)) ||
                    ((e.Action == NotifyCollectionChangedAction.Add) && (this._sourceCollection.Count != (this._descriptionCollection.Count + e.NewItems.Count))))
                {
                    GroupCollectionManager.ResetToGroupDescriptors(this._descriptionCollection, this._sourceCollection);
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
                            GroupDescription description = GroupCollectionManager.GetDescriptionFromDescriptor((GroupDescriptor)item);
                            if (description != null)
                            {
                                this._descriptionCollection.Insert(index++, description);
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
        /// Synchronizes the group descriptors collection to the group descriptions collection.
        /// </summary>
        /// <param name="description">The description that changed</param>
        /// <param name="e">The property change event</param>
        private void HandleGroupDescriptionChanged(GroupDescription description, PropertyChangedEventArgs e)
        {
            this._ignoreChanges = true;
            try
            {
                // We have to reset when the collections were not equal before the change
                if (this._sourceCollection.Count != this._descriptionCollection.Count)
                {
                    GroupCollectionManager.ResetToGroupDescriptions(this._descriptionCollection, this._sourceCollection);
                }
                else
                {
                    int index = this._descriptionCollection.IndexOf(description);
                    this._sourceCollection[index] = GroupCollectionManager.GetDescriptorFromDescription(description);
                }
            }
            finally
            {
                this._ignoreChanges = false;
            }
        }

        /// <summary>
        /// Synchronizes the group descriptions collection to the group descriptors collection.
        /// </summary>
        /// <param name="descriptor">The descriptor that changed</param>
        /// <param name="e">The property change event</param>
        private void HandleGroupDescriptorChanged(GroupDescriptor descriptor, PropertyChangedEventArgs e)
        {
            this._ignoreChanges = true;
            try
            {
                // We have to reset when the collections were not equal before the change
                if (this._sourceCollection.Count != this._descriptionCollection.Count)
                {
                    GroupCollectionManager.ResetToGroupDescriptors(this._descriptionCollection, this._sourceCollection);
                }
                else
                {
                    int index = this._sourceCollection.IndexOf(descriptor);
                    GroupDescription description = GroupCollectionManager.GetDescriptionFromDescriptor(descriptor);
                    if (description == null)
                    {
                        this._descriptionCollection.RemoveAt(index);
                    }
                    else
                    {
                        this._descriptionCollection[index] = description;
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
            Debug.Assert(GroupCollectionManager.AreEquivalent(this._descriptionCollection, this._sourceCollection), "Collections should be equal.");
        }

        /// <summary>
        /// Determines whether the <paramref name="groupDescriptions"/> are equivalent to the <paramref name="groupDescriptors"/>.
        /// </summary>
        /// <param name="groupDescriptions">The descriptions to compare</param>
        /// <param name="groupDescriptors">The descriptors to compare</param>
        /// <returns><c>true</c> if the collections are equivalent</returns>
        private static bool AreEquivalent(ObservableCollection<GroupDescription> groupDescriptions, GroupDescriptorCollection groupDescriptors)
        {
            Debug.Assert((groupDescriptions != null) && (groupDescriptors != null), "Both should be non-null.");

            if (groupDescriptions.Count != groupDescriptors.Count)
            {
                return false;
            }

            for (int i = 0, count = groupDescriptions.Count; i < count; i++)
            {
                if (!GroupCollectionManager.AreEquivalent(groupDescriptions[i], groupDescriptors[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the <paramref name="groupDescription"/> and <paramref name="groupDescriptor"/> are equivalent.
        /// </summary>
        /// <param name="groupDescription">The description to compare</param>
        /// <param name="groupDescriptor">The descriptor to compare</param>
        /// <returns><c>true</c> if the two are equivalent</returns>
        private static bool AreEquivalent(GroupDescription groupDescription, GroupDescriptor groupDescriptor)
        {
            Debug.Assert((groupDescription != null) && (groupDescriptor != null), "Both should be non-null.");

            PropertyGroupDescription propertyGroupDescription = groupDescription as PropertyGroupDescription;
            if (propertyGroupDescription == null)
            {
                return false;
            }

            return (propertyGroupDescription.PropertyName == groupDescriptor.PropertyPath);
        }

        #endregion
    }
}
