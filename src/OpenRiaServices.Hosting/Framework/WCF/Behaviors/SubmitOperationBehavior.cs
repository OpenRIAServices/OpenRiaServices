using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting
{
    internal class SubmitOperationBehavior : IOperationBehavior
    {
        #region Operation Behavior members
        public SubmitOperationBehavior()
        {
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new SubmitOperationInvoker();
        }

        public void Validate(OperationDescription operationDescription)
        {
        }
        #endregion

        internal class SubmitOperationInvoker : DomainOperationInvoker
        {
            public SubmitOperationInvoker()
                : base(DomainOperationType.Submit)
            {
            }

            protected override string Name
            {
                get
                {
                    return ServiceUtility.SubmitOperationName;
                }
            }
            protected override async ValueTask<object> InvokeCoreAsync(DomainService instance, object[] inputs, bool disableStackTraces)
            {
                IEnumerable<ChangeSetEntry> changeSetEntries = (IEnumerable<ChangeSetEntry>)inputs[0];

                try
                {
                    return await ChangeSetProcessor.ProcessAsync(instance, changeSetEntries);
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    throw ServiceUtility.CreateFaultException(ex, disableStackTraces);
                }
            }

            public override object[] AllocateInputs()
            {
                return new object[1];
            }
        }
    }
}
