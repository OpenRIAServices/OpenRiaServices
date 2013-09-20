using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.DomainServices.Server;

namespace System.ServiceModel.DomainServices.Hosting
{
    internal class InvokeOperationBehavior : IOperationBehavior
    {
        private DomainOperationEntry operation;

        public InvokeOperationBehavior(DomainOperationEntry operation)
        {
            this.operation = operation;
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new OperationInvoker(this.operation);
        }

        public void Validate(OperationDescription operationDescription)
        {
        }

        private class OperationInvoker : DomainOperationInvoker
        {
            private DomainOperationEntry operation;

            public OperationInvoker(DomainOperationEntry operation)
                : base(DomainOperationType.Invoke)
            {
                this.operation = operation;
            }

            protected override string Name
            {
                get
                {
                    return this.operation.Name;
                }
            }

            public override object[] AllocateInputs()
            {
                return new object[this.operation.Parameters.Count];
            }

            protected override object InvokeCore(object instance, object[] inputs, out object[] outputs)
            {
                outputs = ServiceUtility.EmptyObjectArray;

                IEnumerable<ValidationResult> validationErrors;
                object result;

                try
                {
                    InvokeDescription invokeDescription = new InvokeDescription(this.operation, inputs);
                    result = ((DomainService)instance).Invoke(invokeDescription, out validationErrors);
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    throw ServiceUtility.CreateFaultException(ex);
                }

                if (validationErrors != null && validationErrors.Any())
                {
                    throw ServiceUtility.CreateFaultException(validationErrors);
                }
                return result;
            }

            protected override void ConvertInputs(object[] inputs)
            {
                for (int i = 0; i < this.operation.Parameters.Count; i++)
                {
                    DomainOperationParameter parameter = this.operation.Parameters[i];
                    inputs[i] = SerializationUtility.GetServerValue(parameter.ParameterType, inputs[i]);
                }
            }

            protected override object ConvertReturnValue(object returnValue)
            {
                return SerializationUtility.GetClientValue(SerializationUtility.GetClientType(this.operation.ReturnType), returnValue);
            }
        }
    }
}
