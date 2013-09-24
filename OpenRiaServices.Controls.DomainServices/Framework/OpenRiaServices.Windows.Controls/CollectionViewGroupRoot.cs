namespace OpenRiaServices.Controls
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// PagedCollectionView classes use this class as the manager 
    /// of all Grouping functionality
    /// </summary>
    internal class CollectionViewGroupRoot : CollectionViewGroupInternal, INotifyCollectionChanged
    {
        #region Static Fields

        //------------------------------------------------------
        //
        //  Static/Constant Fields
        //
        //------------------------------------------------------

        /// <summary>
        /// String constant used for the Root Name
        /// </summary>
        private const string RootName = "Root";

        /// <summary>
        /// Private accessor for empty object instance
        /// </summary>
        private static readonly object UseAsItemDirectly = new object();

        /// <summary>
        /// Private accessor for the top level GroupDescription
        /// </summary>
        private static GroupDescription topLevelGroupDescription;

        #endregion Static Fields

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        /// <summary>
        /// Private accessor for an ObservableCollection containing group descriptions
        /// </summary>
        private ObservableCollection<GroupDescription> _groupBy = new ObservableCollection<GroupDescription>();

        /// <summary>
        /// Indicates whether the list of items (after applying the sort and filters, if any) 
        /// is already in the correct order for grouping.
        /// </summary>
        private bool _isDataInGroupOrder;

        /// <summary>
        /// Private accessor for the owning ICollectionView
        /// </summary>
        private ICollectionView _view;

        #endregion Private Fields

        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionViewGroupRoot"/> class.
        /// </summary>
        /// <param name="view">PagedCollectionView that contains this grouping</param>
        /// <param name="isDataInGroupOrder">True if items are already in correct order for grouping</param>
        internal CollectionViewGroupRoot(ICollectionView view, bool isDataInGroupOrder)
            : base(RootName, null)
        {
            this._view = view;
            this._isDataInGroupOrder = isDataInGroupOrder;
        }

        #endregion Constructors

        #region Events

        //------------------------------------------------------
        //
        //  Events
        //
        //------------------------------------------------------

        /// <summary>
        /// Raise this event when the (grouped) view changes
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raise this event when the GroupDescriptions change
        /// </summary>
        internal event EventHandler GroupDescriptionChanged;

        #endregion Events

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Gets or sets a delegate to select the group description as a 
        /// function of the parent group and its level.
        /// </summary>
        public virtual GroupDescriptionSelectorCallback GroupBySelector { get; set; }

        /// <summary>
        /// Gets the description of grouping, indexed by level.
        /// </summary>
        public virtual ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return this._groupBy; }
        }

        #endregion Public Properties

        #region Internal Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Gets the culture to use during sorting.
        /// </summary>
        internal CultureInfo Culture
        {
            get 
            {
                Debug.Assert(this._view != null, "this._view should have been set from the constructor");
                return this._view.Culture; 
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the data is in group order
        /// </summary>
        internal bool IsDataInGroupOrder
        {
            get { return this._isDataInGroupOrder; }
            set { this._isDataInGroupOrder = value; }
        }

        #endregion Internal Properties

        #region Methods

        //------------------------------------------------------
        //
        //  Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Adds specified item to subgroups.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <param name="loading">Whether we are currently loading.</param>
        /// <param name="insertedBefore">The item to insert this item before,
        /// or <c>null</c> if the item should be added at the end.</param>
        internal void AddToSubgroups(object item, bool loading, object insertedBefore)
        {
            this.AddToSubgroups(item, this, 0, loading, insertedBefore);
        }

        /// <summary>
        /// Initializes the group descriptions
        /// </summary>
        internal void Initialize()
        {
            if (topLevelGroupDescription == null)
            {
                topLevelGroupDescription = new TopLevelGroupDescription();
            }

            this.InitializeGroup(this, 0, null);
        }

        /// <summary>
        /// Notify listeners that this View has changed
        /// </summary>
        /// <remarks>
        /// CollectionViews (and sub-classes) should take their filter/sort/grouping
        /// into account before calling this method to forward CollectionChanged events.
        /// </remarks>
        /// <param name="args">The NotifyCollectionChangedEventArgs to be passed to the EventHandler</param>
        public void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            Debug.Assert(args != null, "Arguments passed in should not be null");

            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, args);
            }
        }

        /// <summary>
        /// Notify host that a group description has changed somewhere in the tree
        /// </summary>
        protected override void OnGroupByChanged()
        {
            if (this.GroupDescriptionChanged != null)
            {
                this.GroupDescriptionChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Remove specified item from subgroups
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>Whether the operation was successful</returns>
        internal bool RemoveFromSubgroups(object item)
        {
            return this.RemoveFromSubgroups(item, this, 0);
        }

        /// <summary>
        /// Remove specified item from subgroups using an exhaustive search
        /// </summary>
        /// <param name="item">Item to remove</param>
        internal void RemoveItemFromSubgroupsByExhaustiveSearch(object item)
        {
            this.RemoveItemFromSubgroupsByExhaustiveSearch(this, item);
        }

        #endregion Methods

        #region Private Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        
        /// <summary>
        /// Add an item to the subgroup with the given name
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="group">Group to add item to</param>
        /// <param name="level">The level of grouping.</param>
        /// <param name="name">Name of subgroup to add to</param>
        /// <param name="loading">Whether we are currently loading</param>
        /// <param name="insertedBefore">The item to insert this item before,
        /// or <c>null</c> if the item should be added at the end.</param>
        private void AddToSubgroup(object item, CollectionViewGroupInternal group, int level, object name, bool loading, object insertedBefore)
        {
            CollectionViewGroupInternal subgroup;
            int index = (this._isDataInGroupOrder) ? group.LastIndex : 0;

            // find the desired subgroup
            for (int n = group.Items.Count; index < n; ++index)
            {
                subgroup = group.Items[index] as CollectionViewGroupInternal;
                if (subgroup == null)
                {
                    continue;           // skip children that are not groups
                }

                if (group.GroupBy.NamesMatch(subgroup.Name, name))
                {
                    group.LastIndex = index;
                    this.AddToSubgroups(item, subgroup, level + 1, loading, insertedBefore);
                    return;
                }
            }

            // the item didn't match any subgroups.  Create a new subgroup and add the item.
            subgroup = new CollectionViewGroupInternal(name, group);
            this.InitializeGroup(subgroup, level + 1, item);

            if (loading)
            {
                group.Add(subgroup);
                group.LastIndex = index;
            }
            else
            {
                if (insertedBefore != null && group.ItemCount > 0)
                {
                    index = 0;

                    // If we were given an item to insert before, find its index
                    // within the target group.  If it's not in the group, we'll
                    // get -1 back, which we'll translate into putting the item
                    // at the end of the list.
                    int itemIndex = group.LeafIndexOf(insertedBefore);

                    if (itemIndex == -1)
                    {
                        index = group.ItemCount;
                    }
                    else
                    {
                        // But if the item to be inserted before is within this
                        // group, then we need to find the correct group position
                        // for the insert.  Iterate through the groups, finding
                        // the correct insert position.
                        CollectionViewGroupInternal nextGroup = ((CollectionViewGroupInternal)group.Items[index]);

                        while (itemIndex >= nextGroup.ItemCount)
                        {
                            itemIndex -= nextGroup.ItemCount;

                            // Increment the index and set the next group
                            nextGroup = ((CollectionViewGroupInternal)group.Items[++index]);
                        }
                    }
                }

                group.Insert(subgroup, index);
            }

            this.AddToSubgroups(item, subgroup, level + 1, loading, insertedBefore);
        }

        /// <summary>
        /// Add an item to the desired subgroup(s) of the given group
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="group">Group to add item to</param>
        /// <param name="level">The level of grouping</param>
        /// <param name="loading">Whether we are currently loading</param>
        /// <param name="insertedBefore">The item to insert this item before,
        /// or <c>null</c> if the item should be added at the end.</param>
        private void AddToSubgroups(object item, CollectionViewGroupInternal group, int level, bool loading, object insertedBefore)
        {
            object name = this.GetGroupName(item, group.GroupBy, level);
            ICollection nameList;

            if (name == UseAsItemDirectly)
            {
                // the item belongs to the group itself (not to any subgroups)
                if (loading)
                {
                    group.Add(item);
                }
                else
                {
                    int localIndex = -1;

                    // If we were given an item to insert before, find its index
                    // within the target group.  If it's not in the group, we'll
                    // get -1 back, which we'll translate into putting the item
                    // at the end of the list.
                    if (insertedBefore != null)
                    {
                        localIndex = group.LeafIndexOf(insertedBefore);
                    }

                    if (localIndex == -1)
                    {
                        localIndex = group.ItemCount;
                    }

                    group.Insert(item, localIndex);
                    int index = group.LeafIndexFromItem(item, localIndex);
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                }
            }
            else if ((nameList = name as ICollection) == null)
            {
                // the item belongs to one subgroup
                this.AddToSubgroup(item, group, level, name, loading, insertedBefore);
            }
            else
            {
                // the item belongs to multiple subgroups
                foreach (object o in nameList)
                {
                    this.AddToSubgroup(item, group, level, o, loading, insertedBefore);
                }
            }
        }

        /// <summary>
        /// Returns the description of how to divide the given group into subgroups
        /// </summary>
        /// <param name="group">CollectionViewGroup to get group description from</param>
        /// <param name="level">The level of grouping</param>
        /// <returns>GroupDescription of how to divide the given group</returns>
        private GroupDescription GetGroupDescription(CollectionViewGroup group, int level)
        {
            GroupDescription result = null;
            if (group == this)
            {
                group = null;
            }

            if (result == null && this.GroupBySelector != null)
            {
                result = this.GroupBySelector(group, level);
            }

            if (result == null && level < this.GroupDescriptions.Count)
            {
                result = this.GroupDescriptions[level];
            }

            return result;
        }

        /// <summary>
        /// Get the group name(s) for the given item
        /// </summary>
        /// <param name="item">Item to get group name for</param>
        /// <param name="groupDescription">GroupDescription for the group</param>
        /// <param name="level">The level of grouping</param>
        /// <returns>Group names for the specified item</returns>
        private object GetGroupName(object item, GroupDescription groupDescription, int level)
        {
            if (groupDescription != null)
            {
                return groupDescription.GroupNameFromItem(item, level, this.Culture);
            }
            else
            {
                return UseAsItemDirectly;
            }
        }

        /// <summary>
        /// Initialize the given group
        /// </summary>
        /// <param name="group">Group to initialize</param>
        /// <param name="level">The level of grouping</param>
        /// <param name="seedItem">The seed item to compare with to see where to insert</param>
        private void InitializeGroup(CollectionViewGroupInternal group, int level, object seedItem)
        {
            // set the group description for dividing the group into subgroups
            GroupDescription groupDescription = this.GetGroupDescription(group, level);
            group.GroupBy = groupDescription;

            // create subgroups for each of the explicit names
            ObservableCollection<object> explicitNames =
                (groupDescription != null) ? groupDescription.GroupNames : null;
            if (explicitNames != null)
            {
                for (int k = 0, n = explicitNames.Count; k < n; ++k)
                {
                    CollectionViewGroupInternal subgroup = new CollectionViewGroupInternal(explicitNames[k], group);
                    this.InitializeGroup(subgroup, level + 1, seedItem);
                    group.Add(subgroup);
                }
            }

            group.LastIndex = 0;
        }

        /// <summary>
        /// Remove an item from the direct children of a group.
        /// </summary>
        /// <param name="group">Group to remove item from</param>
        /// <param name="item">Item to remove</param>
        /// <returns>True if item could not be removed</returns>
        private bool RemoveFromGroupDirectly(CollectionViewGroupInternal group, object item)
        {
            int leafIndex = group.Remove(item, true);
            if (leafIndex >= 0)
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, leafIndex));
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Remove an item from the subgroup with the given name.
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="group">Group to remove item from</param>
        /// <param name="level">The level of grouping</param>
        /// <param name="name">Name of item to remove</param>
        /// <returns>Return true if the item was not in one of the subgroups it was supposed to be.</returns>
        private bool RemoveFromSubgroup(object item, CollectionViewGroupInternal group, int level, object name)
        {
            bool itemIsMissing = false;
            CollectionViewGroupInternal subgroup;

            // find the desired subgroup
            for (int index = 0, n = group.Items.Count; index < n; ++index)
            {
                subgroup = group.Items[index] as CollectionViewGroupInternal;
                if (subgroup == null)
                {
                    continue;           // skip children that are not groups
                }

                if (group.GroupBy.NamesMatch(subgroup.Name, name))
                {
                    if (this.RemoveFromSubgroups(item, subgroup, level + 1))
                    {
                        itemIsMissing = true;
                    }

                    return itemIsMissing;
                }
            }

            // the item didn't match any subgroups.  It should have.
            return true;
        }

        /// <summary>
        /// Remove an item from the desired subgroup(s) of the given group.
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="group">Group to remove item from</param>
        /// <param name="level">The level of grouping</param>
        /// <returns>Return true if the item was not in one of the subgroups it was supposed to be.</returns>
        private bool RemoveFromSubgroups(object item, CollectionViewGroupInternal group, int level)
        {
            bool itemIsMissing = false;
            object name = this.GetGroupName(item, group.GroupBy, level);
            ICollection nameList;

            if (name == UseAsItemDirectly)
            {
                // the item belongs to the group itself (not to any subgroups)
                itemIsMissing = this.RemoveFromGroupDirectly(group, item);
            }
            else if ((nameList = name as ICollection) == null)
            {
                // the item belongs to one subgroup
                if (this.RemoveFromSubgroup(item, group, level, name))
                {
                    itemIsMissing = true;
                }
            }
            else
            {
                // the item belongs to multiple subgroups
                foreach (object o in nameList)
                {
                    if (this.RemoveFromSubgroup(item, group, level, o))
                    {
                        itemIsMissing = true;
                    }
                }
            }

            return itemIsMissing;
        }

        /// <summary>
        /// The item did not appear in one or more of the subgroups it
        /// was supposed to.  This can happen if the item's properties
        /// change so that the group names we used to insert it are
        /// different from the names used to remove it. If this happens,
        /// remove the item the hard way.
        /// </summary>
        /// <param name="group">Group to remove item from</param>
        /// <param name="item">Item to remove</param>
        private void RemoveItemFromSubgroupsByExhaustiveSearch(CollectionViewGroupInternal group, object item)
        {
            // try to remove the item from the direct children 
            // this function only returns true if it failed to remove from group directly
            // in which case we will step through and search exhaustively
            if (this.RemoveFromGroupDirectly(group, item))
            {
                // if that didn't work, recurse into each subgroup
                // (loop runs backwards in case an entire group is deleted)
                for (int k = group.Items.Count - 1; k >= 0; --k)
                {
                    CollectionViewGroupInternal subgroup = group.Items[k] as CollectionViewGroupInternal;
                    if (subgroup != null)
                    {
                        this.RemoveItemFromSubgroupsByExhaustiveSearch(subgroup, item);
                    }
                }
            }
        }

        #endregion Private Methods

        #region Private Classes

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        /// <summary>
        /// TopLevelGroupDescription class
        /// </summary>
        private class TopLevelGroupDescription : GroupDescription
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TopLevelGroupDescription"/> class.
            /// </summary>
            public TopLevelGroupDescription()
            {
            }

            /// <summary>
            /// We have to implement this abstract method, but it should never be called
            /// </summary>
            /// <param name="item">Item to get group name from</param>
            /// <param name="level">The level of grouping</param>
            /// <param name="culture">Culture used for sorting</param>
            /// <returns>We do not return a value here</returns>
            public override object GroupNameFromItem(object item, int level, System.Globalization.CultureInfo culture)
            {
                Debug.Assert(true, "We have to implement this abstract method, but it should never be called");
                return null;
            }
        }

        #endregion Private Classes
    }
}
