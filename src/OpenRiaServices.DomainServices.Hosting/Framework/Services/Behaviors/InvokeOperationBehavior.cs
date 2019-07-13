using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosting
{
    internal class InvokeOperationBehavior : IOperationBehavior
    {
        private readonly DomainOperationEntry operation;

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
            private readonly DomainOperationEntry operation;

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

            //protected override object InvokeCore(object instance, object[] inputs, out object[] outputs)
            protected async override ValueTask<object> InvokeCoreAsync(DomainService instance, object[] inputs, bool disableStackTrace)
            {
                try
                {
                    InvokeDescription invokeDescription = new InvokeDescription(this.operation, inputs);
                    ServiceInvokeResult invokeResult = await instance.InvokeAsync(invokeDescription);
                    if (invokeResult.HasValidationErrors)
                    {
                        throw ServiceUtility.CreateFaultException(invokeResult.ValidationErrors, disableStackTrace);
                    }
                    else
                    {
                        return invokeResult.Result;
                    }
                }
                catch(FaultException<DomainServiceFault>) // from validation error
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    throw ServiceUtility.CreateFaultException(ex, disableStackTrace);
                }
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
