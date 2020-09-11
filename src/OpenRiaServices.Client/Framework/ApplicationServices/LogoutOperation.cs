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
        private readonly Action<LogoutOperation> _completeAction;

        internal LogoutOperation(AuthenticationService service, Action<LogoutOperation> completeAction, object userState)
            : base(service, userState)
        {
            this._completeAction = completeAction;
        }

        /// <summary>
        /// Begins a logout operation
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The async result for the operation</returns>
        protected internal override Task<AuthenticationResult> InvokeAsync(CancellationToken cancellationToken)
        {
            return CastTaskResult(this.Service.LogoutAsync(cancellationToken));
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
