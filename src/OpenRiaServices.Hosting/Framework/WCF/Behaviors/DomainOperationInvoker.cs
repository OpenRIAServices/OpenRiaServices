using System;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;
using System.Web;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.WCF.Behaviors
{
    internal abstract class DomainOperationInvoker : IOperationInvoker
    {
        private readonly DomainOperationType operationType;

        public DomainOperationInvoker(DomainOperationType operationType)
        {
            this.operationType = operationType;
        }

        public bool IsSynchronous
        {
            get
            {
                return false;
            }
        }

        protected abstract string Name
        {
            get;
        }

        public abstract object[] AllocateInputs();

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            throw new NotSupportedException();
        }

        protected virtual void ConvertInputs(object[] inputs)
        {
        }

        protected virtual object ConvertReturnValue(object returnValue)
        {
            return returnValue;
        }

        protected abstract ValueTask<object> InvokeCoreAsync(DomainService instance, object[] inputs, bool disableStackTraces);

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            return TaskExtensions.BeginApm(InvokeAsync(instance, inputs), callback, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            outputs = ServiceUtility.EmptyObjectArray;
            return TaskExtensions.EndApm<object>(result);
        }

        private async ValueTask<object> InvokeAsync(object instance, object[] inputs)
        {
            long startTicks = DiagnosticUtility.GetTicks();
            bool disableStackTraces = true;
            var operationContext = OperationContext.Current;
            try
            {
                WcfDomainServiceContext context = new WcfDomainServiceContext((IServiceProvider)operationContext.Host, this.operationType);
                disableStackTraces = context.DisableStackTraces;

                DiagnosticUtility.OperationInvoked(this.Name, operationContext);
                DomainService domainService = this.GetDomainService(instance, context);

                // invoke the operation and process the result
                this.ConvertInputs(inputs);
                var result = await this.InvokeCoreAsync(domainService, inputs, disableStackTraces).ConfigureAwait(false);
                result = this.ConvertReturnValue(result);

                DiagnosticUtility.OperationCompleted(this.Name, DiagnosticUtility.GetDuration(startTicks), operationContext);

                return result;
            }
            catch (FaultException)
            {
                DiagnosticUtility.OperationFaulted(this.Name, DiagnosticUtility.GetDuration(startTicks), operationContext);

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
                DiagnosticUtility.OperationFailed(this.Name, DiagnosticUtility.GetDuration(startTicks), operationContext);

                // We need to ensure that any time an exception is thrown by the
                // service it is transformed to a properly sanitized/configured
                // fault exception.
                throw ServiceUtility.CreateFaultException(ex, disableStackTraces);
            }
        }

        private DomainService GetDomainService(object instance, WcfDomainServiceContext context)
        {
            // create and initialize the DomainService for this request
            DomainServiceBehavior.DomainServiceInstanceInfo instanceInfo =
                (DomainServiceBehavior.DomainServiceInstanceInfo)instance;

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
                    throw ServiceUtility.CreateFaultException(tie.InnerException, context.DisableStackTraces);
                }

                throw ServiceUtility.CreateFaultException(tie, context.DisableStackTraces);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                throw ServiceUtility.CreateFaultException(ex, context.DisableStackTraces);
            }
        }
    }
}
