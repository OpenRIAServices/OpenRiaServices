using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// Operation type returned from <c>Logout</c> operations on <see cref="AuthenticationService"/>.
    /// </summary>
    public sealed class LogoutOperation : AuthenticationOperation
    {
        #region Member fields

        private readonly Action<LogoutOperation> _completeAction;

        #endregion

        #region Constructors

        internal LogoutOperation(AuthenticationService service, Action<LogoutOperation> completeAction, object userState)
            : base(service, userState)
        {
            this._completeAction = completeAction;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Begins a logout operation
        /// </summary>
        /// <param name="callback">The callback invoked when the operation completes</param>
        /// <returns>The async result for the operation</returns>
        protected override Task<object> InvokeAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.FromAsync((CancellationToken token, AsyncCallback callback, object state) =>
            {
                var operation = this.Service.BeginLogout(callback, state);
                if (token.CanBeCanceled)
                {
                    token.Register(() => this.Service.CancelLogout(operation));
                }
                return operation;
            }
            , (op) => (object)this.Service.EndLogout(op)
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
