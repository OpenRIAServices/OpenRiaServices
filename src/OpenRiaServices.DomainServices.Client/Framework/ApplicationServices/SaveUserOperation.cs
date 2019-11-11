using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// Operation type returned from <c>SaveUser</c> operations on <see cref="AuthenticationService"/>.
    /// </summary>
    public sealed class SaveUserOperation : AuthenticationOperation
    {
        #region Member fields

        private readonly Action<SaveUserOperation> _completeAction;

        #endregion

        #region Constructors

        internal SaveUserOperation(AuthenticationService service, Action<SaveUserOperation> completeAction, object userState) :
            base(service, userState)
        {
            this._completeAction = completeAction;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Begins a save operation
        /// </summary>
        /// <param name="callback">The callback invoked when the operation completes</param>
        /// <returns>The async result for the operation</returns>
        protected async override Task<object> InvokeAsync(CancellationToken cancellationToken)
        {
            return await this.Service.SaveUserAsync(this.User, cancellationToken);
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
