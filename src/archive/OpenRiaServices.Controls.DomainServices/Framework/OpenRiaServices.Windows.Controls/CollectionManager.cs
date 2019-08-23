using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Abstract base class that observes and collates events from
    /// <see cref="INotifyCollectionChanged"/> collections of <see cref="INotifyPropertyChanged"/>
    /// items.
    /// </summary>
    internal abstract class CollectionManager
    {
        #region Member Fields

        private readonly IDictionary<IEnumerable, IList> _localCollections = new Dictionary<IEnumerable, IList>();
        private Func<object, INotifyPropertyChanged> _asINotifyPropertyChangedFunc;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionManager"/> class.
        /// </summary>
        protected CollectionManager()
        {
            this._asINotifyPropertyChangedFunc = item => (INotifyPropertyChanged)item;
        }

        #endregion

        #region Events

        /// <summary>
        /// Raises a generic event any time a managed collection changes.
        /// </summary>
        public event EventHandler CollectionChanged;

        /// <summary>
        /// Raises a generic event any time an item in a managed collection changes.
        /// </summary>
        public event EventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a cache of expressions. The entry for an item will be cleared if
        /// that item is changed or removed.
        /// </summary>
        protected ExpressionCache ExpressionCache { get; set; }

        /// <summary>
        /// Gets or sets the action that is invoked to validate a new or changed item.
        /// </summary>
        /// <remarks>
        /// This can be used to raise programming exceptions. The manager will not attempt
        /// to restore the previous state.
        /// </remarks>
        protected Action<object> ValidationAction { get; set; }

        /// <summary>
        /// Gets or sets the function that an <see cref="INotifyPropertyChanged"/> implementation
        /// that corresponds to the specified item.
        /// </summary>
        /// <remarks>
        /// In most cases, this is used because the items do not directly implement <see cref="INotifyPropertyChanged"/>.
        /// </remarks>
        protected Func<object, INotifyPropertyChanged> AsINotifyPropertyChangedFunc
        {
            get
            {
                return this._asINotifyPropertyChangedFunc;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this._asINotifyPropertyChangedFunc = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a collection to this manager.
        /// </summary>
        /// <typeparam name="TCollection">The type of the collection</typeparam>
        /// <param name="collection">The collection to manage</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="collection"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException"> is thrown if the collection has already been added to this manager.
        /// </exception>
        protected void AddCollection<TCollection>(TCollection collection)
            where TCollection : IEnumerable, INotifyCollectionChanged
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (this._localCollections.ContainsKey(collection))
            {
                throw new ArgumentException("This collection is already being managed.", "collection");
            }

            this._localCollections[collection] = new List<object>();
            collection.CollectionChanged += this.HandleCollectionChanged;

            this.AddAll(collection);
        }

        /// <summary>
        /// Removes a collection from this manager.
        /// </summary>
        /// <typeparam name="TCollection">The type of the collection</typeparam>
        /// <param name="collection">The collection to remove</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="collection"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException"> is thrown if the collection has not been added to this manager.
        /// </exception>
        protected void RemoveCollection<TCollection>(TCollection collection)
            where TCollection : IEnumerable, INotifyCollectionChanged
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (!this._localCollections.ContainsKey(collection))
            {
                throw new ArgumentException("This collection is not being managed.", "collection");
            }

            this.RemoveAll(collection);

            collection.CollectionChanged -= this.HandleCollectionChanged;
            this._localCollections.Remove(collection);
        }

        /// <summary>
        /// Adds each item in the enumeration to this manager.
        /// </summary>
        /// <param name="collection">The collection of items to add</param>
        private void AddAll(IEnumerable collection)
        {
            IList localCollection = this._localCollections[collection];
            foreach (object item in collection)
            {
                this.AddItem(localCollection, item);
            }
        }

        /// <summary>
        /// Removes each item in the enumeration from this manager.
        /// </summary>
        /// <param name="collection">The collection of items to remove</param>
        private void RemoveAll(IEnumerable collection)
        {
            IList localCollection = this._localCollections[collection];
            foreach (object item in localCollection.Cast<object>().ToArray())
            {
                this.RemoveItem(localCollection, item);
            }
        }

        /// <summary>
        /// Adds an item to this manager.
        /// </summary>
        /// <param name="localCollection">The local collection instance for tracking the item</param>
        /// <param name="item">The item to add</param>
        private void AddItem(IList localCollection, object item)
        {
            localCollection.Add(item);
            this.AsINotifyPropertyChangedFunc(item).PropertyChanged += this.HandlePropertyChanged;

            if (this.ValidationAction != null)
            {
                this.ValidationAction(item);
            }
        }

        /// <summary>
        /// Removes an item from this manager.
        /// </summary>
        /// <param name="localCollection">The local collection instance for tracking the item</param>
        /// <param name="item">The item to remove</param>
        private void RemoveItem(IList localCollection, object item)
        {
            this.AsINotifyPropertyChangedFunc(item).PropertyChanged -= this.HandlePropertyChanged;
            localCollection.Remove(item);

            if (this.ExpressionCache != null)
            {
                this.ExpressionCache.Remove(item);
            }
        }

        /// <summary>
        /// Handles collection changes by adding or removing the corresponding items and raising a <see cref="CollectionChanged"/> event.
        /// </summary>
        /// <remarks>
        /// Derived classes should call the base implementation when overriding this method.
        /// </remarks>
        /// <param name="sender">The collection that changed</param>
        /// <param name="e">The collection changed event</param>
        protected virtual void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IEnumerable collection = (IEnumerable)sender;
            IList localCollection = this._localCollections[collection];

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.RemoveAll(collection);
                this.AddAll(collection);
            }
            if ((e.Action == NotifyCollectionChangedAction.Remove) ||
                (e.Action == NotifyCollectionChangedAction.Replace))
            {
                foreach (object item in e.OldItems)
                {
                    this.RemoveItem(localCollection, item);
                }
            }
            if ((e.Action == NotifyCollectionChangedAction.Add) ||
                (e.Action == NotifyCollectionChangedAction.Replace))
            {
                foreach (object item in e.NewItems)
                {
                    this.AddItem(localCollection, item);
                }
            }

            this.OnCollectionChanged();
        }

        /// <summary>
        /// Handles item changes and raises a <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <remarks>
        /// Derived classes should call the base implementation when overriding this method.
        /// </remarks>
        /// <param name="sender">The item that changed</param>
        /// <param name="e">The property changed event</param>
        protected virtual void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.ExpressionCache != null)
            {
                this.ExpressionCache.Remove(sender);
            }

            if (this.ValidationAction != null)
            {
                this.ValidationAction(sender);
            }

            this.OnPropertyChanged();
        }

        /// <summary>
        /// Raises a <see cref="CollectionChanged"/> event.
        /// </summary>
        protected virtual void OnCollectionChanged()
        {
            EventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        /// <summary>
        /// Raises a <see cref="PropertyChanged"/> event.
        /// </summary>
        protected virtual void OnPropertyChanged()
        {
            EventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        /// <summary>
        /// Stores the original values for all <see cref="IRestorable"/> items.
        /// </summary>
        public void StoreOriginalValues()
        {
            foreach (IList localCollection in this._localCollections.Values)
            {
                foreach (IRestorable item in localCollection.OfType<IRestorable>())
                {
                    item.StoreOriginalValue();
                }
            }
        }

        /// <summary>
        /// Restores the original values for all <see cref="IRestorable"/> items.
        /// </summary>
        public void RestoreOriginalValues()
        {
            foreach (IList localCollection in this._localCollections.Values)
            {
                foreach (IRestorable item in localCollection.OfType<IRestorable>())
                {
                    item.RestoreOriginalValue();
                }
            }
        }

        #endregion
    }
}
