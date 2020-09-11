using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// Operation type returned from <c>Login</c> operations on <see cref="AuthenticationService"/>.
    /// </summary>
    public sealed class LoginOperation : AuthenticationOperation
    {
        private readonly Action<LoginOperation> _completeAction;
        private readonly LoginParameters _loginParameters;

        internal LoginOperation(AuthenticationService service, LoginParameters loginParameters, Action<LoginOperation> completeAction, object userState) :
            base(service, userState)
        {
            this._loginParameters = loginParameters;
            this._completeAction = completeAction;
        }

        private new LoginResult Result
        {
            get { return (LoginResult)base.Result; }
        }

        /// <summary>
        /// Gets the login parameters used when invoking this operation.
        /// </summary>
        public LoginParameters LoginParameters
        {
            get { return this._loginParameters; }
        }

        /// <summary>
        /// Gets a value indicating whether this operation was able to successfully login.
        /// </summary>
        /// <remarks>
        /// This value will be <c>false</c> before the operation completes, if the operation
        /// is canceled, or if the operation has errors.
        /// </remarks>
        public bool LoginSuccess
        {
            get { return (this.Result == null) ? false : this.Result.LoginSuccess; }
        }

        /// <summary>
        /// Begins a login operation
        /// </summary>
        /// <returns>The async result for the operation</returns>
        protected internal override Task<AuthenticationResult> InvokeAsync(CancellationToken cancellationToken)
        {
            return CastTaskResult(this.Service.LoginAsync(this.LoginParameters, cancellationToken));
        }

        internal override void Complete(AuthenticationResult endResult)
        {
            base.Complete(endResult);
            if (this.LoginSuccess)
            {
                this.RaisePropertyChanged(nameof(LoginSuccess));
            }
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
