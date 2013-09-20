using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Ria;
using System.Linq;

namespace System.Windows.Ria.Data
{
    /// <summary>
    /// Event arguments for a completed submit operation
    /// </summary>
    public class SubmittedChangesEventArgs : AsyncCompletedEventArgs
    {
        private EntityChangeSet _changeSet;
        private IEnumerable<EntityOperation> _operationResults;

        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="changeSet">The changeset that was submitted.</param>
        /// <param name="operationResults">The operation results returned from the domain service. Will be null
        /// if the submit failed client side validation.</param>
        /// <param name="error">Exception for the submit operation if it failed.</param>
        /// <param name="canceled">True if the submit operation was canceled, false otherwise.</param>
        /// <param name="userState">Optional user state.</param>
        public SubmittedChangesEventArgs(EntityChangeSet changeSet, IEnumerable<EntityOperation> operationResults, Exception error, bool canceled, object userState)
            : base(error, canceled, userState)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException("changeSet");
            }
            this._changeSet = changeSet;
            this._operationResults = operationResults;
        }

        /// <summary>
        /// Gets the <see cref="EntityChangeSet"/> that was submitted
        /// </summary>
        public EntityChangeSet ChangeSet
        {
            get
            {
                return this._changeSet;
            }
        }

        /// <summary>
        /// Gets the entities that caused the submit operation to fail.
        /// </summary>
        public IEnumerable<Entity> EntitiesInError
        {
            get
            {
                IEnumerable<Entity> entities;
                if (this._operationResults != null)
                {
                    entities = this._operationResults.Select(p => p.ClientEntity);
                }
                else if (this._changeSet != null)
                {
                    entities = this._changeSet;
                }
                else
                {
                    entities = new Entity[0];
                }

                return entities.Where(p => p.Conflict != null || p.HasErrors || p.HasValidationErrors).Distinct();
            }
        }
    }
}
