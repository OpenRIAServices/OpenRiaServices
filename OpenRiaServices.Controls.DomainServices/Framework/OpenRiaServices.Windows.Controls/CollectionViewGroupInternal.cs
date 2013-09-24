namespace OpenRiaServices.Controls
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// A CollectionViewGroupInternal, as created by a PagedCollectionView 
    /// according to a GroupDescription.
    /// </summary>
    internal class CollectionViewGroupInternal : CollectionViewGroup
    {
        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        /// <summary>
        /// GroupDescription used to define how to group the items
        /// </summary>
        private GroupDescription _groupBy;

        /// <summary>
        /// Parent group of this CollectionViewGroupInternal
        /// </summary>
        private CollectionViewGroupInternal _parentGroup;

        /// <summary>
        /// Used for detecting stale enumerators
        /// </summary>
        private int _version;

        #endregion Private Fields

        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionViewGroupInternal"/> class.
        /// </summary>
        /// <param name="name">Name of the CollectionViewGroupInternal</param>
        /// <param name="parent">Parent node of the CollectionViewGroup</param>
        internal CollectionViewGroupInternal(object name, CollectionViewGroupInternal parent)
            : base(name)
        {
            this._parentGroup = parent;
        }

        #endregion Constructors

        #region Public Properties

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether this group 
        /// is at the bottom level (not further sub-grouped).
        /// </summary>
        public override bool IsBottomLevel
        {
            get { return this._groupBy == null; }
        }

        #endregion  Public Properties

        #region Internal Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Gets or sets the number of items and groups in the subtree under this group
        /// </summary>
        [DefaultValue(1)]
        internal int FullCount { get; set; }

        /// <summary>
        /// Gets or sets how this group divides into subgroups
        /// </summary>
        internal GroupDescription GroupBy
        {
            get
            {
                return this._groupBy;
            }

            set
            {
                bool oldIsBottomLevel = this.IsBottomLevel;

                if (this._groupBy != null)
                {
                    ((INotifyPropertyChanged)this._groupBy).PropertyChanged -= new PropertyChangedEventHandler(this.OnGroupByChanged);
                }

                this._groupBy = value;

                if (this._groupBy != null)
                {
                    ((INotifyPropertyChanged)this._groupBy).PropertyChanged += new PropertyChangedEventHandler(this.OnGroupByChanged);
                }

                if (oldIsBottomLevel != this.IsBottomLevel)
                {
                    this.OnPropertyChanged(new PropertyChangedEventArgs("IsBottomLevel"));
                }
            }
        }

        /// <summary>
        /// Gets or sets the most recent index where activity took place
        /// </summary>
        internal int LastIndex { get; set; }

        /// <summary>
        /// Gets the first item (leaf) added to this group.  If this can't be determined,
        /// DependencyProperty.UnsetValue.
        /// </summary>
        internal object SeedItem
        {
            get
            {
                if (this.ItemCount > 0 && (this.GroupBy == null || this.GroupBy.GroupNames.Count == 0))
                {
                    // look for first item, child by child
                    for (int k = 0, n = Items.Count; k < n; ++k)
                    {
                        CollectionViewGroupInternal subgroup = this.Items[k] as CollectionViewGroupInternal;
                        if (subgroup == null)
                        {
                            // child is an item - return it
                            return this.Items[k];
                        }
                        else if (subgroup.ItemCount > 0)
                        {
                            // child is a nonempty subgroup - ask it
                            return subgroup.SeedItem;
                        }
                        //// otherwise child is an empty subgroup - go to next child
                    }

                    // we shouldn't get here, but just in case...
                    return DependencyProperty.UnsetValue;
                }
                else
                {
                    // the group is empty, or it has explicit subgroups.
                    // In either case, we cannot determine the first item -
                    // it could have gone into any of the subgroups.
                    return DependencyProperty.UnsetValue;
                }
            }
        }

        #endregion Internal Properties

        #region Private Properties

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Gets the parent node for this CollectionViewGroupInternal
        /// </summary>
        private CollectionViewGroupInternal Parent
        {
            get { return this._parentGroup; }
        }

        #endregion Private Properties

        #region Internal Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Adds the specified item to the collection
        /// </summary>
        /// <param name="item">Item to add</param>
        internal void Add(object item)
        {
            this.ChangeCounts(item, +1);
            this.ProtectedItems.Add(item);
        }

        /// <summary>
        /// Clears the collection of items
        /// </summary>
        internal void Clear()
        {
            this.ProtectedItems.Clear();
            this.FullCount = 1;
            this.ProtectedItemCount = 0;
        }

        /// <summary>
        /// Insert a new item or subgroup.
        /// </summary>
        /// <param name="item">The item being inserted.</param>
        /// <param name="index">The index at which to insert the item.</param>
        internal void Insert(object item, int index)
        {
            this.ChangeCounts(item, +1);
            ProtectedItems.Insert(index, item);
        }

        /// <summary>
        /// Return the item at the given index within the list of leaves governed
        /// by this group
        /// </summary>
        /// <param name="index">Index of the leaf</param>
        /// <returns>Item at given index</returns>
        internal object LeafAt(int index)
        {
            for (int k = 0, n = this.Items.Count; k < n; ++k)
            {
                CollectionViewGroupInternal subgroup = this.Items[k] as CollectionViewGroupInternal;
                if (subgroup != null)
                {
                    // current item is a group - either drill in, or skip over
                    if (index < subgroup.ItemCount)
                    {
                        return subgroup.LeafAt(index);
                    }
                    else
                    {
                        index -= subgroup.ItemCount;
                    }
                }
                else
                {
                    // current item is a leaf - see if we're done
                    if (index == 0)
                    {
                        return this.Items[k];
                    }
                    else
                    {
                        index -= 1;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the index of the given item within the list of leaves governed
        /// by the full group structure.  The item must be a (direct) child of this
        /// group.  The caller provides the index of the item within this group,
        /// if known, or -1 if not.
        /// </summary>
        /// <param name="item">Item we are looking for</param>
        /// <param name="index">Index of the leaf</param>
        /// <returns>Number of items under that leaf</returns>
        internal int LeafIndexFromItem(object item, int index)
        {
            int result = 0;

            // accumulate the number of predecessors at each level
            for (CollectionViewGroupInternal group = this;
                    group != null;
                    item = group, group = group.Parent, index = -1)
            {
                // accumulate the number of predecessors at the level of item
                for (int k = 0, n = group.Items.Count; k < n; ++k)
                {
                    // if we've reached the item, move up to the next level
                    if ((index < 0 && Object.Equals(item, group.Items[k])) ||
                        index == k)
                    {
                        break;
                    }

                    // accumulate leaf count
                    CollectionViewGroupInternal subgroup = group.Items[k] as CollectionViewGroupInternal;
                    result += (subgroup == null) ? 1 : subgroup.ItemCount;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the index of the given item within the list of leaves governed
        /// by this group
        /// </summary>
        /// <param name="item">Item we are looking for</param>
        /// <returns>Number of items under that leaf</returns>
        internal int LeafIndexOf(object item)
        {
            int leaves = 0;         // number of leaves we've passed over so far
            for (int k = 0, n = Items.Count; k < n; ++k)
            {
                CollectionViewGroupInternal subgroup = Items[k] as CollectionViewGroupInternal;
                if (subgroup != null)
                {
                    int subgroupIndex = subgroup.LeafIndexOf(item);
                    if (subgroupIndex < 0)
                    {
                        leaves += subgroup.ItemCount;       // item not in this subgroup
                    }
                    else
                    {
                        return leaves + subgroupIndex;    // item is in this subgroup
                    }
                }
                else
                {
                    // current item is a leaf - compare it directly
                    if (Object.Equals(item, Items[k]))
                    {
                        return leaves;
                    }
                    else
                    {
                        leaves += 1;
                    }
                }
            }

            // item not found
            return -1;
        }

        /// <summary>
        /// The group's description has changed - notify parent 
        /// </summary>
        protected virtual void OnGroupByChanged()
        {
            if (this.Parent != null)
            {
                this.Parent.OnGroupByChanged();
            }
        }

        /// <summary>
        /// Removes the specified item from the collection
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="returnLeafIndex">Whether we want to return the leaf index</param>
        /// <returns>Leaf index where item was removed, if value was specified. Otherwise '-1'</returns>
        internal int Remove(object item, bool returnLeafIndex)
        {
            int index = -1;
            int localIndex = this.ProtectedItems.IndexOf(item);

            if (localIndex >= 0)
            {
                if (returnLeafIndex)
                {
                    index = this.LeafIndexFromItem(null, localIndex);
                }

                this.ChangeCounts(item, -1);
                this.ProtectedItems.RemoveAt(localIndex);
            }

            return index;
        }

        #endregion Internal Methods

        #region Private Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Removes an empty group from the PagedCollectionView grouping
        /// </summary>
        /// <param name="group">Empty subgroup to remove</param>
        private static void RemoveEmptyGroup(CollectionViewGroupInternal group)
        {
            CollectionViewGroupInternal parent = group.Parent;

            if (parent != null)
            {
                GroupDescription groupBy = parent.GroupBy;
                int index = parent.ProtectedItems.IndexOf(group);

                // remove the subgroup unless it is one of the explicit groups
                if (index >= groupBy.GroupNames.Count)
                {
                    parent.Remove(group, false);
                }
            }
        }

        /// <summary>
        /// Update the item count of the CollectionViewGroup
        /// </summary>
        /// <param name="item">CollectionViewGroup to update</param>
        /// <param name="delta">Delta to change count by</param>
        protected void ChangeCounts(object item, int delta)
        {
            bool changeLeafCount = !(item is CollectionViewGroup);

            for (CollectionViewGroupInternal group = this;
                    group != null;
                    group = group._parentGroup)
            {
                group.FullCount += delta;
                if (changeLeafCount)
                {
                    group.ProtectedItemCount += delta;

                    if (group.ProtectedItemCount == 0)
                    {
                        RemoveEmptyGroup(group);
                    }
                }
            }

            unchecked
            {
                // this invalidates enumerators
                ++this._version;
            }
        }

        /// <summary>
        /// Handler for the GroupBy PropertyChanged event
        /// </summary>
        /// <param name="sender">CollectionViewGroupInternal whose GroupBy property changed</param>
        /// <param name="e">The args for the PropertyChanged event</param>
        private void OnGroupByChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.OnGroupByChanged();
        }

        #endregion Private Methods
    }
}
