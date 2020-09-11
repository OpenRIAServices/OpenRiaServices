using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Class representing an asynchronous operation.
    /// </summary>
    public abstract class OperationBase : INotifyPropertyChanged
    {
        private object _result;
        private Exception _error;
        private bool _canceled;
        private bool _completed;
        private readonly object _userState;
        private PropertyChangedEventHandler _propChangedHandler;
        private EventHandler _completedEventHandler;
        private bool _isErrorHandled;
        private readonly CancellationTokenSource _cancellationTokenSource;


        /// <summary>
        /// Initializes a new instance of the <see cref="OperationBase"/> class.
        /// </summary>
        /// <param name="userState">Optional user state.</param>
        /// <param name="supportCancellation"><c>true</c> to setup cancellationTokenSource and use that to handle cancellation</param>
        protected OperationBase(object userState, bool supportCancellation)
        {
            this._userState = userState;
            if (supportCancellation)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operation error has been marked as
        /// handled by calling <see cref="MarkErrorAsHandled"/>.
        /// </summary>
        public bool IsErrorHandled => this._isErrorHandled;

        /// <summary>
        /// Event raised when the operation completes.
        /// </summary>
        public event EventHandler Completed
        {
            add
            {
                if (this.IsComplete)
                {
                    // if the operation has already completed, invoke the
                    // handler immediately
                    value(this, EventArgs.Empty);
                }
                else
                {
                    this._completedEventHandler = (EventHandler)Delegate.Combine(this._completedEventHandler, value);
                }
            }
            remove
            {
                this._completedEventHandler = (EventHandler)Delegate.Remove(this._completedEventHandler, value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this operation supports cancellation.
        /// </summary>
        protected bool SupportsCancellation => this._cancellationTokenSource != null;

        /// <summary>
        /// Gets a value indicating whether Cancel has been called on this operation.
        /// </summary>
        public bool IsCancellationRequested => this._cancellationTokenSource?.IsCancellationRequested == true;

        /// <summary>
        /// Gets a <see cref="System.Threading.CancellationToken"/> which is cancelled 
        /// when this operation is cancelled. 
        /// It is valid if the operation support cancellation se constructor.
        /// </summary>
        internal CancellationToken CancellationToken => this._cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Gets a value indicating whether this operation is currently in a state
        /// where it can be canceled.
        /// <remarks>If <see cref="SupportsCancellation"/> is false,
        /// this operation doesn't support cancellation, and <see cref="CanCancel"/>
        /// will always return false.</remarks>
        /// </summary>
        public bool CanCancel
        {
            get
            {
                // can be canceled if cancellation is supported and
                // the operation hasn't already completed
                // and Cancel has not already been cancelled
                return this.SupportsCancellation && !this._completed && !IsCancellationRequested;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this operation has been canceled.
        /// </summary>
        /// <remarks>
        /// Note that successful cancellation of this operation does not guarantee 
        /// state changes were prevented from happening on the server.
        /// </remarks>
        public bool IsCanceled => this._canceled;

        /// <summary>
        /// Gets the operation error if the operation failed.
        /// </summary>
        public Exception Error => this._error;

        /// <summary>
        /// Gets a value indicating whether the operation has failed. If
        /// true, inspect the Error property for details.
        /// </summary>
        public bool HasError => this._error != null;

        /// <summary>
        /// Gets a value indicating whether this operation has completed.
        /// </summary>
        public bool IsComplete => this._completed;

        /// <summary>
        /// Gets the result of the async operation.
        /// </summary>
        private protected object Result => this._result;

        /// <summary>
        /// Gets the optional user state for this operation.
        /// </summary>
        public object UserState => this._userState;

        /// <summary>
        /// For an operation where <see cref="HasError"/> is <c>true</c>, this method marks the error as handled.
        /// If this method is not called for a failed operation, an exception will be thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="HasError"/> is <c>false</c>.</exception>
        public void MarkErrorAsHandled()
        {
            if (this._error == null)
            {
                throw new InvalidOperationException(Resource.Operation_HasErrorMustBeTrue);
            }

            if (!this._isErrorHandled)
            {
                this._isErrorHandled = true;
                this.RaisePropertyChanged(nameof(IsErrorHandled));
            }
        }

        /// <summary>
        /// Cancels the operation.
        /// </summary>
        /// <remarks>
        /// Upon completion of the operation, check the IsCanceled property to determine whether 
        /// or not the operation was successfully canceled. Note that successful cancellation
        /// does not guarantee state changes were prevented from happening on the server.
        /// </remarks>
        /// <exception cref="NotSupportedException"> is thrown when <see cref="SupportsCancellation"/>
        /// is <c>false</c>.
        /// </exception>
        public void Cancel()
        {
            if (!this.SupportsCancellation)
            {
                throw new NotSupportedException(Resources.AsyncOperation_CancelNotSupported);
            }

            this.EnsureNotCompleted();
            if (this._cancellationTokenSource != null)
            {
                try
                {
                    this._cancellationTokenSource?.Cancel();
                }
                catch (AggregateException ex)
                {
                    throw ExceptionHandlingUtility.GetUnwrappedException(ex); ;
                }
            }
        }

        /// <summary>
        /// Transition the operation into the Cancelled state
        /// </summary>
        protected internal void SetCancelled()
        {
            // must flag completion before callbacks or events are raised
            this._completed = true;
            this._canceled = true;

            // callback is called even for a canceled operation
            try
            {
                this.InvokeCompleteCallbacks();
            }
            finally
            {
                this.RaisePropertyChanged(nameof(IsCanceled));
                this.RaisePropertyChanged(nameof(CanCancel));
                this.RaisePropertyChanged(nameof(IsComplete));
            }
        }

        /// <summary>
        /// Successfully completes the operation.
        /// </summary>
        /// <param name="result">The operation result.</param>
        protected void Complete(object result)
        {
            this.EnsureNotCompleted();

            bool prevCanCancel = this.CanCancel;
            this._result = result;

            // must flag completion before callbacks or events are raised
            this._completed = true;

            try
            {
                this.InvokeCompleteCallbacks();
            }
            finally
            {
                this.RaisePropertyChanged(nameof(IsComplete));
                if (prevCanCancel == true)
                    this.RaisePropertyChanged(nameof(CanCancel));
            }
        }

        private void InvokeCompletedEvent()
        {
            var handler = _completedEventHandler;
            if (handler == null)
                return;

            Delegate[] invocations = handler.GetInvocationList();
            EventArgs eventArgs = EventArgs.Empty;
            int i = 0;
            try
            {
                for (; i < invocations.Length; ++i)
                    ((EventHandler)invocations[i]).Invoke(this, eventArgs);
            }
            catch (Exception ex) when (i + 1 < invocations.Length)
            {
                // Once we have an exception continue invoking the rest of the callbacks
                // and add any additional exceptions to a list
                // so we can raise an AggregateException in case of multiple exceptions
                ++i;
                var exceptions = new List<Exception>() { ex };
                for (; i < invocations.Length; ++i)
                {
                    try
                    {
                        ((EventHandler)invocations[i]).Invoke(this, eventArgs);
                    }
                    catch (Exception ex2)
                    {
                        exceptions.Add(ex2);
                    }
                }

                if (exceptions.Count == 1)
                {
                    // Rethrow original exception if we only had one
                    throw;
                }
                else
                {
                    throw new AggregateException(exceptions);
                }
            }
        }

        /// <summary>
        /// Completes the operation with the specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        protected void SetError(Exception error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            this.EnsureNotCompleted();

            bool prevCanCancel = this.CanCancel;
            this._error = error;

            // must flag completion before callbacks or events are raised
            this._completed = true;

            // callback is called even in error case
            try
            {
                this.InvokeCompleteCallbacks();
            }
            finally
            {
                this.RaisePropertyChanged(nameof(Error));
                this.RaisePropertyChanged(nameof(HasError));
                this.RaisePropertyChanged(nameof(IsComplete));

                if (prevCanCancel == true)
                {
                    this.RaisePropertyChanged(nameof(CanCancel));
                }
            }

            if (!this.IsErrorHandled)
            {
                throw error;
            }
        }

        /// <summary>
        /// Invokes both the Complete action passed to constructor
        /// as well as all Completed event handlers even if exceptions
        /// occurs
        /// </summary>
        /// <exception cref="AggregateException" />
        /// <exception cref="Exception" />
        private void InvokeCompleteCallbacks()
        {
            try
            {
                this.InvokeCompleteAction();
            }
            catch (Exception ex)
            {
                try
                {
                    this.InvokeCompletedEvent();
                }
                catch (AggregateException ex2)
                {
                    var exceptions = new List<Exception>(ex2.InnerExceptions);
                    exceptions.Add(ex);
                    throw new AggregateException(exceptions);
                }
                catch (Exception ex2)
                {
                    throw new AggregateException(ex, ex2);
                }

                // Only a single exception so rethrow it as is
                throw;
            }

            InvokeCompletedEvent();
        }

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected abstract void InvokeCompleteAction();

        /// <summary>
        /// Ensures an operation has not been completed or canceled. If
        /// it has been completed, an exception is thrown.
        /// </summary>
        private void EnsureNotCompleted()
        {
            if (this._completed)
            {
                throw new InvalidOperationException(Resources.AsyncOperation_AlreadyCompleted);
            }
        }

        /// <summary>
        /// Called to raise the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            this._propChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region INotifyPropertyChanged Members

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                this._propChangedHandler = (PropertyChangedEventHandler)Delegate.Combine(this._propChangedHandler, value);
            }
            remove
            {
                this._propChangedHandler = (PropertyChangedEventHandler)Delegate.Remove(this._propChangedHandler, value);
            }
        }

        #endregion
    }
}
