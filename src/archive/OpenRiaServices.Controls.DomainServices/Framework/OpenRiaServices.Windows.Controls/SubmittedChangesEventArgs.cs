using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenRiaServices.Client;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Event arguments for a completed submit operation
    /// </summary>
    public sealed class SubmittedChangesEventArgs : AsyncCompletedEventArgs
    {
        private EntityChangeSet _changeSet;
        private IEnumerable<Entity> _entitiesInError;
        private bool _isErrorHandled;

        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="changeSet">The changeset that was submitted.</param>
        /// <param name="entitiesInError">The list of entities that were in error.</param>
        /// <param name="error"><see cref="Exception"/> for the submit operation if it failed.</param>
        /// <param name="canceled"><c>true</c> if the submit operation was canceled, <c>false</c> otherwise.</param>
        internal SubmittedChangesEventArgs(EntityChangeSet changeSet, IEnumerable<Entity> entitiesInError, Exception error, bool canceled)
            : base(error, canceled, null)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException("changeSet");
            }
            this.ChangeSet = changeSet;
            this.EntitiesInError = entitiesInError;
        }

        /// <summary>
        /// Gets the <see cref="EntityChangeSet"/> that was submitted
        /// </summary>
        public EntityChangeSet ChangeSet
        {
            get { return this._changeSet; }
            private set { this._changeSet = value; }
        }

        /// <summary>
        /// Gets the entities that caused the submit operation to fail.
        /// </summary>
        public IEnumerable<Entity> EntitiesInError
        {
            get { return this._entitiesInError; }
            private set { this._entitiesInError = value; }
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
        /// Gets or sets a value indicating whether the operation error has been marked as
        /// handled by calling <see cref="MarkErrorAsHandled"/>.
        /// </summary>
        public bool IsErrorHandled
        {
            get { return this._isErrorHandled; }
        }

        /// <summary>
        /// For an operation in error, this method marks the error as handled. If this method is
        /// not called for a failed operation, an exception will be thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="HasError"/> <c>false</c>.</exception>
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
    }
}
