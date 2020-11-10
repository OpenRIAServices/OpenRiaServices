using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// Operation type returned from <c>SaveUser</c> operations on <see cref="AuthenticationService"/>.
    /// </summary>
    public sealed class SaveUserOperation : AuthenticationOperation
    {
        private readonly Action<SaveUserOperation> _completeAction;

        internal SaveUserOperation(AuthenticationService service, Action<SaveUserOperation> completeAction, object userState) :
            base(service, userState)
        {
            this._completeAction = completeAction;
        }

        /// <summary>
        /// Begins a save operation
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The async result for the operation</returns>
        protected internal override Task<AuthenticationResult> InvokeAsync(CancellationToken cancellationToken)
        {
            return CastTaskResult(this.Service.SaveUserAsync(this.Service.User, cancellationToken));
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            this._completeAction?.Invoke(this);
        }
    }
}
