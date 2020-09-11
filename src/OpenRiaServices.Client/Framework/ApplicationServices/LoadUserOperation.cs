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
        private readonly Action<LoadUserOperation> _completeAction;

        internal LoadUserOperation(AuthenticationService service, Action<LoadUserOperation> completeAction, object userState)
            : base(service, userState)
        {
            this._completeAction = completeAction;
        }

        /// <summary>
        /// Begins a load operation
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The async result for the operation</returns>
        protected internal override Task<AuthenticationResult> InvokeAsync(CancellationToken cancellationToken)
        {
            return CastTaskResult(this.Service.LoadUserAsync(cancellationToken));
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
