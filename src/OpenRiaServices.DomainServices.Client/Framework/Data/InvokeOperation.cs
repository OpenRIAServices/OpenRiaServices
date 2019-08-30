using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// Represents an asynchronous invoke operation.
    /// </summary>
    public class InvokeOperation : OperationBase
    {
        private readonly string _operationName;
        private IDictionary<string, object> _parameters;
        private IEnumerable<ValidationResult> _validationErrors;
        private readonly Action<InvokeOperation> _completeAction;

        /// <summary>
        /// Initializes a new instance of the InvokeOperation class
        /// </summary>
        /// <param name="operationName">The operation to invoke.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="completeAction">Optional action to execute when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <param name="supportCancellation"><c>true</c> to enable <see cref="OperationBase.CancellationToken"/> to be cancelled when <see cref="OperationBase.Cancel"/> is called</param>
        internal InvokeOperation(string operationName, IDictionary<string, object> parameters,
            Action<InvokeOperation> completeAction, object userState,
            bool supportCancellation)
            : base(userState, supportCancellation)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentNullException(nameof(operationName));
            }
            this._operationName = operationName;
            this._parameters = parameters;
            this._completeAction = completeAction;
        }

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName
        {
            get
            {
                return this._operationName;
            }
        }

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
        /// Creates a strongly typed <see cref="InvokeOperation"/> for the specified Type.
        /// </summary>
        /// <typeparam name="TValue">The return value Type.</typeparam>
        /// <param name="operationName">The operation to invoke.</param>
        /// <param name="parameters">Optional parameters to the operation. Specify null
        /// if the operation takes no parameters.</param>
        /// <param name="completeAction">Optional action to execute when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <param name="supportCancellation"><c>rtue</c> to enable cancellation support</param>
        /// <returns>The operation instance created.</returns>
        internal static InvokeOperation Create<TValue>(string operationName, IDictionary<string, object> parameters,
            Action<InvokeOperation> completeAction, object userState,
            bool supportCancellation)
        {
            Action<InvokeOperation<TValue>> wrappedCompleteAction = null;
            if (completeAction != null)
            {
                wrappedCompleteAction = arg => completeAction(arg);
            }
            

            return new InvokeOperation<TValue>(operationName, parameters, wrappedCompleteAction, userState, supportCancellation);
        }

        /// <summary>
        /// The <see cref="DomainClientResult"/> for this operation.
        /// </summary>
        private protected new DomainClientResult Result
        {
            get
            {
                return (DomainClientResult)base.Result;
            }
        }

        /// <summary>
        /// Gets the return value for the invoke operation.
        /// </summary>
        public object Value
        {
            get
            {
                if (this.Result == null)
                {
                    return null;
                }
                return this.Result.ReturnValue;
            }
        }

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IEnumerable<ValidationResult> ValidationErrors
        {
            get
            {
                if (this.Result != null)
                {
                    return this.Result.ValidationErrors;
                }
                else
                {
                    // return any errors if set, otherwise return an empty
                    // collection
                    if (this._validationErrors == null)
                    {
                        this._validationErrors = Array.Empty<ValidationResult>();
                    }
                }
                return this._validationErrors;
            }
        }

        /// <summary>
        /// Completes the invoke operation with validation errors.
        /// </summary>
        /// <param name="validationErrors">The validation errors.</param>
        internal void Complete(IEnumerable<ValidationResult> validationErrors)
        {
            this._validationErrors = validationErrors;
            this.RaisePropertyChanged(nameof(ValidationErrors));

            string message = string.Format(CultureInfo.CurrentCulture, 
                Resource.DomainContext_InvokeOperationFailed_Validation, 
                this.OperationName);
            DomainOperationException error = new DomainOperationException(message, validationErrors);

            base.Complete(error);
        }

        /// <summary>
        /// Completes the invoke operation with the specified error.
        /// </summary>
        /// <param name="error">The error.</param>
        internal new void Complete(Exception error)
        {
            if (typeof(DomainException).IsAssignableFrom(error.GetType()))
            {
                // DomainExceptions should not be modified
                base.Complete(error);
                return;
            }

            string message = string.Format(CultureInfo.CurrentCulture,
                Resource.DomainContext_InvokeOperationFailed,
                this.OperationName, error.Message);

            DomainOperationException domainOperationException = error as DomainOperationException;
            if (domainOperationException != null)
            {
                error = new DomainOperationException(message, domainOperationException);
            }
            else
            {
                error = new DomainOperationException(message, error);
            }

            base.Complete(error);
        }

        /// <summary>
        /// Completes the invoke operation with the specified result.
        /// </summary>
        /// <param name="result">The result.</param>
        internal void Complete(DomainClientResult result)
        {
            object prevValue = this.Value;

            base.Complete(result);

            if (this.Result != null && this.Result.ReturnValue != prevValue)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeOperation"/> class.
        /// </summary>
        /// <param name="operationName">The operation to invoke.</param>
        /// <param name="parameters">The parameters to the operation.</param>
        /// <param name="completeAction">Action to execute when the operation completes.</param>
        /// <param name="userState">Optional user state for the operation.</param>
        /// <param name="supportCancellation"><c>true</c> to enable <see cref="OperationBase.CancellationToken"/> to be cancelled when <see cref="OperationBase.Cancel"/> is called</param>
        internal InvokeOperation(string operationName, IDictionary<string, object> parameters,
            Action<InvokeOperation<TValue>> completeAction, object userState,
            bool supportCancellation)
            : base(operationName, parameters, /* completeAction */ null, /* userState */ userState, /* supportCancellation */ supportCancellation)
        {
            this._completeAction = completeAction;
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
                return (TValue)this.Result.ReturnValue;
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
