﻿using System;
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
        #region Member fields

        // By default, events will be dispatched to the context the service is created in
        private readonly SynchronizationContext _synchronizationContext =
            SynchronizationContext.Current ?? new SynchronizationContext();

        private IAsyncResult _asyncResult;

        private readonly AuthenticationService _service;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationOperation"/> class.
        /// </summary>
        /// <param name="service">The service this operation will use to implement Begin, Cancel, and End</param>
        /// <param name="userState">Optional user state.</param>
        internal AuthenticationOperation(AuthenticationService service, object userState) :
            base(userState, false)
        {
            Debug.Assert(service != null, "The service cannot be null.");
            this._service = service;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the async result returned from <see cref="InvokeAsync"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="InvokeAsync"/> has not been called, this may be <c>null</c>.
        /// </remarks>
        protected IAsyncResult AsyncResult
        {
            get { return this._asyncResult; }
        }

        /// <summary>
        /// Gets the service this operation will use to implement Begin, Cancel, and End.
        /// </summary>
        protected AuthenticationService Service
        {
            get { return this._service; }
        }

        /// <summary>
        /// Gets a value that indicates whether the operation supports cancellation.
        /// </summary>
        protected override bool SupportsCancellation
        {
            get { return this.Service.SupportsCancellation; }
        }

        /// <summary>
        /// Gets the result as an <see cref="AuthenticationResult"/>.
        /// </summary>
        protected new AuthenticationResult Result
        {
            get { return (AuthenticationResult)base.Result; }
        }

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

        #endregion

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

            // Many tests throw from InvokeXX directly and expect it to be rethrown
            if (task.IsCompleted)
            {
                InvokeComplete(task, this);
            }
            else
            {
                var scheduler = SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;
                task.ContinueWith(InvokeComplete, this, CancellationToken.None, TaskContinuationOptions.HideScheduler, scheduler);
            }

            static void InvokeComplete(Task<object> res, object state)
            {
                var This = (AuthenticationOperation)state;
                object endResult = null;

                if (res.IsCanceled)
                {
                    This.SetCancelled();
                    return;
                }

                try
                {
                    endResult = res.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    This.SetError(e);
                    This.RaiseCompletionPropertyChanges();

                    if (e.IsFatal())
                    {
                        throw;
                    }

                    return;
                }

                This.Complete(endResult);
                This.RaiseCompletionPropertyChanges();
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

        private protected override void OnCancellationRequested()
        {
            base.SetCancelled();
        }
        #endregion
    }
}
