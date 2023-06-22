using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents an asynchronous invoke operation.
    /// </summary>
    public class InvokeOperation : OperationBase, IInvokeResult
    {
        private IDictionary<string, object> _parameters;
        private IReadOnlyCollection<ValidationResult> _validationErrors;
        private readonly Action<InvokeOperation> _completeAction;

        /// <summary>
        /// Initializes a new instance of the InvokeOperation class
        /// </summary>
        /// <param name="operationName">The operation to invoke.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="completeAction">Optional action to execute when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <param name="cancellationTokenSource"><see cref="CancellationTokenSource"/> which will be used to request cancellation if <see cref="OperationBase.Cancel()"/> is called, if <c>null</c> then cancellation will not be possible</param>
        internal InvokeOperation(string operationName, IDictionary<string, object> parameters,
            Action<InvokeOperation> completeAction, object userState,
            CancellationTokenSource cancellationTokenSource)
            : base(userState, cancellationTokenSource)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentNullException(nameof(operationName));
            }
            this.OperationName = operationName;
            this._parameters = parameters;
            this._completeAction = completeAction;
        }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Gets the collection of parameters to the operation.
        /// </summary>
        public IDictionary<string, object> Parameters
        {
            get
            {
                if (this._parameters == null)
                {
                    this._parameters = new Dictionary<string, object>();
                }
                return this._parameters;
            }
        }

        /// <summary>
        /// The <see cref="IInvokeResult"/> for this operation.
        /// </summary>
        private protected new IInvokeResult Result => (IInvokeResult)base.Result;

        /// <summary>
        /// Gets the return value for the invoke operation.
        /// </summary>
        public object Value => Result?.Value;

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyCollection<ValidationResult> ValidationErrors
        {
            get
            {
                // return any errors if set, otherwise return an empty
                // collection
                if (this._validationErrors == null)
                {
                    this._validationErrors = Array.Empty<ValidationResult>();
                }
                return this._validationErrors;
            }
        }

        /// <summary>
        /// Completes the load operation with the specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        private protected new void SetError(Exception error)
        {
            if (error is DomainOperationException doe
                && doe.ValidationErrors.Any())
            {
                this._validationErrors = doe.ValidationErrors;
                this.RaisePropertyChanged(nameof(ValidationErrors));
            }

            base.SetError(error);
        }

        /// <summary>
        /// Completes the invoke operation with the specified result.
        /// </summary>
        /// <param name="result">The result.</param>
        private protected void SetResult(InvokeResult result)
        {
            System.Diagnostics.Debug.Assert(this.Value is null);
            base.Complete(result);

            if (this.Value is not null)
            {
                this.RaisePropertyChanged(nameof(Value));
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

    /// <summary>
    /// Represents an asynchronous invoke operation.
    /// </summary>
    /// <typeparam name="TValue">The Type of the invoke return value.</typeparam>
    public sealed class InvokeOperation<TValue> : InvokeOperation
    {
        private readonly Action<InvokeOperation<TValue>> _completeAction;

        private bool _hasExceptionOnCompleteTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeOperation"/> class.
        /// </summary>
        /// <param name="operationName">The operation to invoke.</param>
        /// <param name="parameters">The parameters to the operation.</param>
        /// <param name="completeAction">Action to execute when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <param name="invokeResultTask">Task which, when completed, will Complete the operation and set either <see cref="Value"/>, cancelled or error</param>
        /// <param name="cancellationTokenSource"><see cref="CancellationTokenSource"/> which will be used to request cancellation if <see cref="OperationBase.Cancel()"/> is called, if <c>null</c> then cancellation will not be possible</param>
        public InvokeOperation(string operationName, IDictionary<string, object> parameters,
            Action<InvokeOperation<TValue>> completeAction, object userState,
            Task<InvokeResult<TValue>> invokeResultTask,
            CancellationTokenSource cancellationTokenSource)
            : base(operationName, parameters, /* completeAction */ null, /* userState */ userState, /* supportCancellation */ cancellationTokenSource)
        {
            this._completeAction = completeAction;

            if (invokeResultTask.IsCompleted)
                CompleteTask(invokeResultTask);
            else
            {
                var continueTask = invokeResultTask.ContinueWith(static (loadTask, state) =>
                {
                    ((InvokeOperation<TValue>)state).CompleteTask(loadTask);
                }
                , (object)this
                , CancellationToken.None
                , TaskContinuationOptions.HideScheduler
                , CurrentSynchronizationContextTaskScheduler);

                continueTask.GetAwaiter().OnCompleted(() =>
                {
                    if (_hasExceptionOnCompleteTask)
                    {
                        throw continueTask.Exception;
                    }
                });
            }
        }

        /// <summary>
        /// Gets the return value for the invoke operation.
        /// </summary>
        public new TValue Value
        {
            get
            {
                if (this.Result == null)
                {
                    return default(TValue);
                }
                return this.Result.Value;
            }
        }

        /// <summary>
        /// The <see cref="IInvokeResult"/> for this operation.
        /// </summary>
        private new InvokeResult<TValue> Result => (InvokeResult<TValue>)base.Result;

        /// <summary>
        /// Invoke the completion callback.
        /// </summary>
        protected override void InvokeCompleteAction()
        {
            this._completeAction?.Invoke(this);
        }

        internal void CompleteTask(Task<InvokeResult<TValue>> task)
        {
            try
            {
                if (task.IsCanceled)
                {
                    SetCancelled();
                }
                else if (task.Exception != null)
                {
                    SetError(ExceptionHandlingUtility.GetUnwrappedException(task.Exception));
                }
                else
                {
                    SetResult(task.Result);
                }
            }
            catch
            {
                _hasExceptionOnCompleteTask = true;
                throw;
            }
        }
    }
}
