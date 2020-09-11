using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// Represents the result of a submit operation.
    /// </summary>
    public class SubmitCompletedResult
    {
        private readonly EntityChangeSet _changeSet;
        private readonly ReadOnlyCollection<ChangeSetEntry> _operationResults;

        /// <summary>
        /// Initializes a new instance of the SubmitCompletedResult class
        /// </summary>
        /// <param name="changeSet">The changeset that was submitted.</param>
        /// <param name="operationResults">The <see cref="ChangeSetEntry"/> results sent back from the 
        /// DomainService for the submit operation.</param>
        public SubmitCompletedResult(EntityChangeSet changeSet, IEnumerable<ChangeSetEntry> operationResults)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException(nameof(changeSet));
            }
            if (operationResults == null)
            {
                throw new ArgumentNullException(nameof(operationResults));
            }

            this._changeSet = changeSet;
            this._operationResults =new ReadOnlyCollection<ChangeSetEntry>(operationResults.ToList());
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
        /// Gets the <see cref="ChangeSetEntry"/> results that were returned
        /// from the DomainService.
        /// </summary>
        public IReadOnlyCollection<ChangeSetEntry> Results
        {
            get
            {
                return this._operationResults;
            }
        }
    }
}
