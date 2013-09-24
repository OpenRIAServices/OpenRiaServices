using System;
using System.ComponentModel;
using OpenRiaServices.DomainServices.Client;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Event arguments for an in progress submit operation
    /// </summary>
    public sealed class SubmittingChangesEventArgs : CancelEventArgs
    {
        private EntityChangeSet _changeSet;

        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="changeSet">The changeset being submitted</param>
        internal SubmittingChangesEventArgs(EntityChangeSet changeSet)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException("changeSet");
            }

            this._changeSet = changeSet;
        }

        /// <summary>
        /// Gets the <see cref="EntityChangeSet"/> being submitted
        /// </summary>
        public EntityChangeSet ChangeSet
        {
            get
            {
                return this._changeSet;
            }
        }
    }
}
