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
        #region Member fields

        private readonly Action<LoginOperation> _completeAction;
        private readonly LoginParameters _loginParameters;

        #endregion

        #region Constructors

        internal LoginOperation(AuthenticationService service, LoginParameters loginParameters, Action<LoginOperation> completeAction, object userState) :
            base(service, userState)
        {
            this._loginParameters = loginParameters;
            this._completeAction = completeAction;
        }

        #endregion

        #region Properties

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

        #endregion

        #region Methods

        /// <summary>
        /// Begins a login operation
        /// </summary>
        /// <returns>The async result for the operation</returns>
        protected override Task<object> InvokeAsync(CancellationToken cancellationToken)
        {
            return CastToObjectTask(this.Service.LoginAsync(this.LoginParameters, cancellationToken));
        }

        /// <summary>
        /// Raises property changes after the operation has completed.
        /// </summary>
        protected override void RaiseCompletionPropertyChanges()
        {
            base.RaiseCompletionPropertyChanges();
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

        #endregion
    }
}
