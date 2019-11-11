using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// Operation type returned from <c>LoadUser</c> operations on <see cref="AuthenticationService"/>.
    /// </summary>
    public sealed class LoadUserOperation : AuthenticationOperation
    {
        #region Member fields

        private readonly Action<LoadUserOperation> _completeAction;

        #endregion

        #region Constructors

        internal LoadUserOperation(AuthenticationService service, Action<LoadUserOperation> completeAction, object userState) :
            base(service, userState)
        {
            this._completeAction = completeAction;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Begins a load operation
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The async result for the operation</returns>
        protected override Task<object> InvokeAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.FromAsync((CancellationToken token, AsyncCallback callback, object state) =>
            {
                var operation = this.Service.BeginLoadUser(callback, state);
                if (token.CanBeCanceled)
                {
                    token.Register(() => this.Service.CancelLoadUser(operation));
                }
                return operation;
            }
            , (op) => (object)this.Service.EndLoadUser(op)
            , cancellationToken
            , null
            , TaskCreationOptions.None);
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            this._completeAction?.Invoke(this);
        }

        #endregion
    }
}
