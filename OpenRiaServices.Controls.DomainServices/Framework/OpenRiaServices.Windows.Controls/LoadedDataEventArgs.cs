using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using OpenRiaServices.DomainServices.Client;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Event arguments for the completion of a load operation
    /// </summary>
    public sealed class LoadedDataEventArgs : AsyncCompletedEventArgs
    {
        #region Member fields

        private bool _isErrorHandled;

        private readonly ReadOnlyCollection<Entity> _entities;
        private readonly ReadOnlyCollection<Entity> _allEntities;
        private readonly int _totalEntityCount;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadedDataEventArgs"/> class.
        /// </summary>
        /// <param name="entities">The top-level entities loaded with this operation.</param>
        /// <param name="allEntities">All entities loaded with this operation, including those loaded as associations.</param>
        /// <param name="totalEntityCount">The total number of records for the original query without any paging applied to it.</param>
        /// <param name="cancelled"><c>true</c> if the load operation was cancelled, <c>false</c> otherwise.</param>
        /// <param name="error"><see cref="Exception"/> for the load operation if it failed.</param>
        internal LoadedDataEventArgs(IEnumerable<Entity> entities, IEnumerable<Entity> allEntities, int totalEntityCount, bool cancelled, Exception error)
            : base(error, cancelled, null)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entities");
            }

            if (allEntities == null)
            {
                throw new ArgumentNullException("allEntities");
            }

            this._entities = entities.ToList().AsReadOnly();
            this._allEntities = allEntities.ToList().AsReadOnly();
            this._totalEntityCount = totalEntityCount;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets all the entities loaded, including any
        /// entities referenced by the top level entities.
        /// </summary>
        public IEnumerable<Entity> AllEntities
        {
            get
            {
                RaiseExceptionIfNecessary();
                return this._allEntities;
            }
        }

        /// <summary>
        /// Gets all the top level entities loaded.
        /// </summary>
        public IEnumerable<Entity> Entities
        {
            get
            {
                RaiseExceptionIfNecessary();
                return this._entities;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operation has failed. If
        /// true, inspect the Error property for details.
        /// </summary>
        public bool HasError
        {
            get { return this.Error != null; }
        }

        /// <summary>
        /// Gets a value indicating whether the operation error has been marked as
        /// handled by calling <see cref="MarkErrorAsHandled"/>.
        /// </summary>
        public bool IsErrorHandled
        {
            get { return this._isErrorHandled; }
        }

        /// <summary>
        /// Gets the total server entity count for the query.
        /// </summary>
        public int TotalEntityCount
        {
            get
            {
                RaiseExceptionIfNecessary();
                return this._totalEntityCount;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// For an operation where <see cref="HasError"/> is <c>true</c>, this method marks the error as handled.
        /// If this method is not called for a failed operation, an exception will be thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="HasError"/> is <c>false</c>.</exception>
        public void MarkErrorAsHandled()
        {
            if (!this.HasError)
            {
                throw new InvalidOperationException(DomainDataSourceResources.HasErrorMustBeTrue);
            }

            if (!this._isErrorHandled)
            {
                this._isErrorHandled = true;
            }
        }

        #endregion
    }
}
