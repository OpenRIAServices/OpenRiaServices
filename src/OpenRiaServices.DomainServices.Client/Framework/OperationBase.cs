using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace OpenRiaServices.DomainServices.Client
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
        public bool IsErrorHandled
        {
            get
            {
                return this._isErrorHandled;
            }
        }

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
        /// If overridden to return true, <see cref="CancelCore"/> must also be overridden.
        /// </summary>
        protected virtual bool SupportsCancellation
        {
            get
            {
                return this._cancellationTokenSource != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether Cancel has been called on this operation.
        /// </summary>
        public bool IsCancellationRequested => this._cancellationTokenSource?.IsCancellationRequested == true;

        /// <summary>
        /// Gets a <see cref="System.Threading.CancellationToken"/> which is cancelled if 
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
        public bool IsCanceled
        {
            get
            {
                return this._canceled;
            }
        }

        /// <summary>
        /// Gets the operation error if the operation failed.
        /// </summary>
        public Exception Error
        {
            get
            {
                return this._error;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operation has failed. If
        /// true, inspect the Error property for details.
        /// </summary>
        public bool HasError
        {
            get
            {
                return this._error != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this operation has completed.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return this._completed;
            }
        }

        /// <summary>
        /// Gets the result of the async operation.
        /// </summary>
        private protected object Result
        {
            get
            {
                return this._result;
            }
        }

        /// <summary>
        /// Gets the optional user state for this operation.
        /// </summary>
        public object UserState
        {
            get
            {
                return this._userState;
            }
        }

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

            OnCancellationRequested();
        }

        /// <summary>
        /// Called when user calls <see cref="Cancel"/>, the default behaviour
        /// is to mark the operation as completed as Cancelled, but can 
        /// be overriden to prevent that.
        /// </summary>
        private protected virtual void OnCancellationRequested()
        {
            SetCancelled();
        }

        /// <summary>
        /// Transition the operation into the Cancelled state
        /// </summary>
        internal protected void SetCancelled()
        {
            // must flag completion before callbacks or events are raised
            this._completed = true;
            this._canceled = true;

            // invoke the cancel action
            this.CancelCore();

            // callback is called even for a canceled operation
            this.InvokeCompleteAction();

            this._completedEventHandler?.Invoke(this, EventArgs.Empty);

            this.RaisePropertyChanged(nameof(IsCanceled));
            this.RaisePropertyChanged(nameof(CanCancel));
            this.RaisePropertyChanged(nameof(IsComplete));
        }

        /// <summary>
        /// Override this method to provide a Cancel implementation
        /// for operations that support cancellation.
        /// </summary>
        protected virtual void CancelCore()
        {
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

            this.InvokeCompleteAction();

            if (this._completedEventHandler != null)
            {
                this._completedEventHandler(this, EventArgs.Empty);
            }

            this.RaisePropertyChanged(nameof(IsComplete));
            if (prevCanCancel == true)
            {
                this.RaisePropertyChanged(nameof(CanCancel));
            }
        }

        /// <summary>
        /// Completes the operation with the specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        protected void Complete(Exception error)
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
            this.InvokeCompleteAction();

            if (this._completedEventHandler != null)
            {
                this._completedEventHandler(this, EventArgs.Empty);
            }

            this.RaisePropertyChanged(nameof(Error));
            this.RaisePropertyChanged(nameof(HasError));
            this.RaisePropertyChanged(nameof(IsComplete));
            if (prevCanCancel == true)
            {
                this.RaisePropertyChanged(nameof(CanCancel));
            }

            if (!this.IsErrorHandled)
            {
                throw error;
            }
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
