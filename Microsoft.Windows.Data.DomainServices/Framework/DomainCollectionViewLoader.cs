using System;
using System.ComponentModel;
using OpenRiaServices.DomainServices.Client;

namespace Microsoft.Windows.Data.DomainServices
{
    /// <summary>
    /// Concrete implementation of the <see cref="CollectionViewLoader"/> that uses callbacks to
    /// load data for the source collection of the collection view.
    /// </summary>
    public class DomainCollectionViewLoader : CollectionViewLoader
    {
        #region Member fields

        private readonly Func<LoadOperation> _load;
        private readonly Action<LoadOperation> _onLoadCompleted;

        private LoadOperation _currentOperation;
        private object _currentUserState;

        private bool _isBusy;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainCollectionView"/> with a
        /// load callback
        /// </summary>
        /// <param name="load">The load callback. This function will be called every time
        /// <see cref="Load"/> is invoked. It should return the <see cref="LoadOperation"/>
        /// responsible for loading the data for the source collection.
        /// </param>
        public DomainCollectionViewLoader(Func<LoadOperation> load)
            : this(load, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainCollectionView"/> with load
        /// and completion callbacks
        /// </summary>
        /// <param name="load">The load callback. This function will be called every time
        /// <see cref="Load"/> is invoked. It should return the <see cref="LoadOperation"/>
        /// responsible for loading the data for the source collection.
        /// </param>
        /// <param name="onLoadCompleted">The completion callback. This action will be called
        /// upon completion of each <see cref="Load"/> operation and should handle success,
        /// cancellation, and exceptions.
        /// </param>
        public DomainCollectionViewLoader(Func<LoadOperation> load, Action<LoadOperation> onLoadCompleted)
        {
            if (load == null)
            {
                throw new ArgumentNullException("load");
            }

            this._load = load;
            this._onLoadCompleted = onLoadCompleted;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value that indicates whether the loader is busy
        /// </summary>
        /// <remarks>
        /// Setting this value will also update <see cref="CanLoad"/>.
        /// </remarks>
        public bool IsBusy
        {
            get
            {
                return this._isBusy;
            }

            set
            {
                if (this._isBusy != value)
                {
                    this._isBusy = value;
                    this.OnCanLoadChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a <see cref="Load"/> can be successfully invoked
        /// </summary>
        public override bool CanLoad
        {
            get { return !this.IsBusy; }
        }

        /// <summary>
        /// Gets or sets the current operation
        /// </summary>
        /// <remarks>
        /// Setting the current operation will cancel all pending operations and subscribe to the
        /// completion of the new operation.
        /// </remarks>
        private LoadOperation CurrentOperation
        {
            get
            {
                return this._currentOperation;
            }

            set
            {
                if (this._currentOperation != value)
                {
                    if (this._currentOperation != null)
                    {
                        if (this._currentOperation.CanCancel)
                        {
                            this._currentOperation.Cancel();
                        }
                    }

                    this._currentOperation = value;

                    if (this._currentOperation != null)
                    {
                        this._currentOperation.Completed += this.OnLoadCompleted;
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously loads data into the source collection of the collection view by invoking
        /// the load callback passed into the constructor
        /// </summary>
        /// <param name="userState">The user state will be returned in the
        /// <see cref="CollectionViewLoader.LoadCompleted"/> event args. This parameter is optional.
        /// </param>
        /// <exception cref="InvalidOperationException"> is thrown when <see cref="CanLoad"/> is false</exception>
        public override void Load(object userState)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(Resources.CannotLoad);
            }
            this._currentUserState = userState;
            this.CurrentOperation = this._load();
        }

        /// <summary>
        /// Handles load completion by invoking the completion callback passed into the constructor and
        /// raising the <see cref="CollectionViewLoader.LoadCompleted"/> event
        /// </summary>
        /// <param name="sender">The completed operation</param>
        /// <param name="e">Empty event args</param>
        private void OnLoadCompleted(object sender, EventArgs e)
        {
            LoadOperation op = (LoadOperation)sender;

            if (this._onLoadCompleted != null)
            {
                this._onLoadCompleted(op);
            }

            if (op == this.CurrentOperation)
            {
                this.OnLoadCompleted(new AsyncCompletedEventArgs(op.Error, op.IsCanceled, this._currentUserState));
                this._currentUserState = null;
                this.CurrentOperation = null;
            }
            else
            {
                this.OnLoadCompleted(new AsyncCompletedEventArgs(op.Error, op.IsCanceled, null));
            }
        }

        #endregion
    }

    /// <summary>
    /// Generic concrete implementation of the <see cref="CollectionViewLoader"/> that uses callbacks to
    /// load data for the source collection of the collection view.
    /// </summary>
    /// <typeparam name="TEntity">The entity type for this loader</typeparam>
    public class DomainCollectionViewLoader<TEntity> : DomainCollectionViewLoader where TEntity : Entity
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainCollectionView"/> with a
        /// load callback
        /// </summary>
        /// <param name="load">The load callback. This function will be called every time
        /// <see cref="DomainCollectionViewLoader.Load"/> is invoked. It should return the <see cref="LoadOperation"/>
        /// responsible for loading the data for the source collection.
        /// </param>
        public DomainCollectionViewLoader(Func<LoadOperation<TEntity>> load)
            : this(load, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainCollectionView"/> with load
        /// and completion callbacks
        /// </summary>
        /// <param name="load">The load callback. This function will be called every time
        /// <see cref="DomainCollectionViewLoader.Load"/> is invoked. It should return the <see cref="LoadOperation"/>
        /// responsible for loading the data for the source collection.
        /// </param>
        /// <param name="onLoadCompleted">The completion callback. This action will be called
        /// upon completion of each <see cref="DomainCollectionViewLoader.Load"/> operation and should handle success,
        /// cancellation, and exceptions.
        /// </param>
        public DomainCollectionViewLoader(Func<LoadOperation<TEntity>> load, Action<LoadOperation<TEntity>> onLoadCompleted)
            : base(() => (LoadOperation<TEntity>)load(), lo => onLoadCompleted((LoadOperation<TEntity>)lo))
        {
        }

        #endregion
    }
}
