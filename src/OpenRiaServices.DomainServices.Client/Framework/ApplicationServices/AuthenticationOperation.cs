using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
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
        internal AuthenticationOperation(AuthenticationService service, object userState)
            : base(userState, service.SupportsCancellation)
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
        public IPrincipal User
        {
            get { return (this.Result == null) ? null : this.Result.User; }
        }

        #region Methods

        /// <summary>
        /// Starts the operation.
        /// </summary>
        /// <remarks>
        /// This method will invoke <see cref="InvokeAsync"/> and will allow all
        /// exceptions thrown from <see cref="InvokeAsync"/> to pass through.
        /// </remarks>
        internal void Start()
        {
            var task = this.InvokeAsync(this.CancellationToken);

            // Continue on same SynchronizationContext
            var scheduler = SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;
            task.ContinueWith(InvokeComplete, this, CancellationToken.None, TaskContinuationOptions.HideScheduler, scheduler);

            static void InvokeComplete(Task<object> res, object state)
            {
                var operation = (AuthenticationOperation)state;
                object endResult = null;

                if (res.IsCanceled)
                {
                    operation.SetCancelled();
                    return;
                }

                try
                {
                    endResult = res.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    operation.SetError(e);
                    operation.RaiseCompletionPropertyChanges();

                    if (e.IsFatal())
                    {
                        throw;
                    }

                    return;
                }

                operation.Complete(endResult);
                operation.RaiseCompletionPropertyChanges();
            }
        }

        /// <summary>
        /// Template method for invoking the corresponding Begin method in the
        /// underlying async result implementation.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="Start"/>. Any exceptions thrown
        /// will be passed through.
        /// </remarks>
        /// <param name="cancellationToken"></param>
        /// <returns>The async result returned by the underlying Begin call</returns>
        protected abstract Task<object> InvokeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Raises property changes after the operation has completed.
        /// </summary>
        /// <remarks>
        /// This method is invoked by the callback passed into <see cref="InvokeAsync"/> once
        /// <see cref="OperationBase.Result"/> and <see cref="OperationBase.Error"/> have
        /// been set. Change notifications for any properties that have been affected by the
        /// state changes should occur here.
        /// </remarks>
        protected virtual void RaiseCompletionPropertyChanges()
        {
            if (this.User != null)
            {
                this.RaisePropertyChanged(nameof(User));
            }
        }

        private protected Task<object> CastToObjectTask<T>(Task<T> task)
            where T : class
        {
            return task.ContinueWith(res =>
            {
                return (object)res.GetAwaiter().GetResult();
            }
           , CancellationToken.None
           , TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.ExecuteSynchronously
           , TaskScheduler.Default);
        }
        #endregion
    }
}
