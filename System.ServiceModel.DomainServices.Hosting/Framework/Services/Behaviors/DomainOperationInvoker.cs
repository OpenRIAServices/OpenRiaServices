using System.Reflection;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.DomainServices.Server;

namespace System.ServiceModel.DomainServices.Hosting
{
    internal abstract class DomainOperationInvoker : IOperationInvoker
    {
        private DomainOperationType operationType;

        public DomainOperationInvoker(DomainOperationType operationType)
        {
            this.operationType = operationType;
        }

        public bool IsSynchronous
        {
            get
            {
                return true;
            }
        }

        protected abstract string Name
        {
            get;
        }

        public abstract object[] AllocateInputs();

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            string operationName = this.Name;
            long startTicks = DateTime.UtcNow.Ticks;
            object result = null;
            DomainService domainService = null;
            try
            {
                DiagnosticUtility.OperationInvoked(operationName);

                domainService = this.GetDomainService(instance);

                // invoke the operation and process the result
                this.ConvertInputs(inputs);
                result = this.InvokeCore(domainService, inputs, out outputs);
                result = this.ConvertReturnValue(result);

                DiagnosticUtility.OperationCompleted(operationName, DiagnosticUtility.GetDuration(startTicks));
            }
            catch (FaultException)
            {
                DiagnosticUtility.OperationFaulted(operationName, DiagnosticUtility.GetDuration(startTicks));

                // if the exception has already been transformed to a fault
                // just rethrow it
                throw;
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                DiagnosticUtility.OperationFailed(operationName, DiagnosticUtility.GetDuration(startTicks));

                // We need to ensure that any time an exception is thrown by the
                // service it is transformed to a properly sanitized/configured
                // fault exception.
                throw ServiceUtility.CreateFaultException(ex);
            }

            return result;
        }

        protected virtual void ConvertInputs(object[] inputs)
        {
        }

        protected virtual object ConvertReturnValue(object returnValue)
        {
            return returnValue;
        }

        protected abstract object InvokeCore(object instance, object[] inputs, out object[] outputs);

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            throw new NotSupportedException();
        }

        private DomainService GetDomainService(object instance)
        {
            // create and initialize the DomainService for this request
            DomainServiceBehavior.DomainServiceInstanceInfo instanceInfo =
                (DomainServiceBehavior.DomainServiceInstanceInfo)instance;

            IServiceProvider serviceProvider = (IServiceProvider)OperationContext.Current.Host;
            DomainServiceContext context = new DomainServiceContext(serviceProvider, this.operationType);

            try
            {
                DomainService domainService = DomainService.Factory.CreateDomainService(instanceInfo.DomainServiceType, context);
                instanceInfo.DomainServiceInstance = domainService;
                return domainService;
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw ServiceUtility.CreateFaultException(tie.InnerException);
                }

                throw ServiceUtility.CreateFaultException(tie);
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
    }
}
