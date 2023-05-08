using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// Abstract subclass of the <see cref="OperationBase"/> class
    /// that is the base operation type for all the operations supported
    /// by <see cref="AuthenticationService"/>.
    /// </summary>
    public abstract class AuthenticationOperation : OperationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationOperation"/> class.
        /// </summary>
        /// <param name="service">The service this operation will use to implement Begin, Cancel, and End</param>
        /// <param name="userState">Optional user state.</param>
        private protected AuthenticationOperation(AuthenticationService service, object userState)
            : base(userState, service.SupportsCancellation ? new CancellationTokenSource() : null)
        {
            Debug.Assert(service != null, "The service cannot be null.");
            this.Service = service;
        }

        /// <summary>
        /// Gets the service this operation will use to implement Begin, Cancel, and End.
        /// </summary>
        protected AuthenticationService Service { get; }

        /// <summary>
        /// Gets the result as an <see cref="AuthenticationResult"/>.
        /// </summary>
        protected new AuthenticationResult Result => (AuthenticationResult)base.Result;

        /// <summary>
        /// Gets the user principal.
        /// </summary>
        /// <remarks>
        /// This value will be <c>null</c> before the operation completes, if the operation
        /// is canceled, or if the operation has errors.
        /// </remarks>
        public IPrincipal User => this.Result?.User;

        #region Methods

        /// <summary>
        /// Template method for invoking the corresponding Begin method in the
        /// underlying async result implementation.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The async result returned by the underlying Begin call</returns>
        protected internal abstract Task<AuthenticationResult> InvokeAsync(CancellationToken cancellationToken);

        internal new void SetError(Exception error)
        {
            base.SetError(error);
        }

        internal virtual void Complete(AuthenticationResult endResult)
        {
            base.Complete(endResult);
            if (this.User != null)
            {
                this.RaisePropertyChanged(nameof(User));
            }
        }

        private protected Task<AuthenticationResult> CastTaskResult<T>(Task<T> task)
            where T : AuthenticationResult
        {
            return task.ContinueWith<AuthenticationResult>(res =>
            {
                return res.GetAwaiter().GetResult();
            }
           , CancellationToken.None
           , TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.ExecuteSynchronously
           , TaskScheduler.Default);
        }

        #endregion
    }
}
