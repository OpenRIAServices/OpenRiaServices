using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// Represents an asynchronous submit operation
    /// </summary>
    public sealed class SubmitOperation : OperationBase
    {
        private readonly EntityChangeSet _changeSet;
        private readonly Action<SubmitOperation> _cancelAction;
        private readonly Action<SubmitOperation> _completeAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitOperation"/> class.
        /// </summary>
        /// <param name="changeSet">The changeset being submitted.</param>
        /// <param name="completeAction">Optional action to invoke when the operation completes.</param>
        /// <param name="userState">Optional user state to associate with the operation.</param>
        /// <param name="cancelAction">Optional action to invoke when the operation is canceled. If null, cancellation will not be supported.</param>
        internal SubmitOperation(EntityChangeSet changeSet,
            Action<SubmitOperation> completeAction, object userState,
            Action<SubmitOperation> cancelAction)
            : base(userState, cancelAction != null)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException("changeSet");
            }
            this._cancelAction = cancelAction;
            this._completeAction = completeAction;
            this._changeSet = changeSet;
        }

        /// <summary>
        /// Gets a value indicating whether this operation supports cancellation.
        /// </summary>
        protected override bool SupportsCancellation
        {
            get
            {
                return (this._cancelAction != null);
            }
        }

        /// <summary>
        /// The changeset being submitted.
        /// </summary>
        public EntityChangeSet ChangeSet
        {
            get
            {
                return this._changeSet;
            }
        }

        /// <summary>
        /// Returns any entities in error after the submit operation completes.
        /// </summary>
        public IEnumerable<Entity> EntitiesInError
        {
            get
            {
                return this._changeSet.Where(p => p.EntityConflict != null || p.HasValidationErrors);
            }
        }

        /// <summary>
        /// Invokes the cancel callback.
        /// </summary>
        protected override void CancelCore()
        {
            this._cancelAction(this);
        }

        /// <summary>
        /// Successfully complete the submit operation.
        /// </summary>
        internal void Complete()
        {
            // SubmitOperation doesn't have a result - all results
            // are specified on Entities in the changeset.
            base.Complete((object)null);
        }

        /// <summary>
        /// Complete the submit operation with the specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        internal new void Complete(Exception error)
        {
            if (typeof(DomainException).IsAssignableFrom(error.GetType()))
            {
                // DomainExceptions should not be modified
                base.Complete(error);
                return;
            }

            string message = string.Format(CultureInfo.CurrentCulture,
                Resource.DomainContext_SubmitOperationFailed, error.Message);

            DomainOperationException domainOperationException = error as DomainOperationException;
            if (domainOperationException != null)
            {
                error = new SubmitOperationException(ChangeSet, message, domainOperationException);
            }
            else
            {
                error = new SubmitOperationException(ChangeSet, message, error);
            }

            base.Complete(error);
        }

        internal void Complete(OperationErrorStatus errorStatus)
        {
            SubmitOperationException error = null;
            if (errorStatus == OperationErrorStatus.ValidationFailed)
            {
                error = new SubmitOperationException(ChangeSet, Resource.DomainContext_SubmitOperationFailed_Validation, OperationErrorStatus.ValidationFailed);
            }
            else if (errorStatus == OperationErrorStatus.Conflicts)
            {
                error = new SubmitOperationException(ChangeSet, Resource.DomainContext_SubmitOperationFailed_Conflicts, OperationErrorStatus.Conflicts);
            }
            else
            {
                // This can never happen, all paths here supply either 
                // ValidationFailed or Conflicts
                throw new ArgumentException("Unsupported OperationErrorStatus","errorStatus");
            }

            base.Complete(error);
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            if (this._completeAction != null)
            {
                this._completeAction(this);
            }
        }
    }
}
