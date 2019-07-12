using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosting
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

            //protected override object InvokeCore(object instance, object[] inputs, out object[] outputs)
            protected override async ValueTask<object> InvokeCoreAsync(object instance, object[] inputs)
            {
                DomainService domainService = (DomainService)instance;
                IEnumerable<ChangeSetEntry> changeSetEntries = (IEnumerable<ChangeSetEntry>)inputs[0];

                try
                {
                    return ChangeSetProcessor.Process(domainService, changeSetEntries);
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    throw ServiceUtility.CreateFaultException(ex);
                }
            }

            public override object[] AllocateInputs()
            {
                return new object[1];
            }
        }
    }
}
