using System;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosting
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
            string operationName = this.Name;
            long startTicks = DiagnosticUtility.GetTicks();
            try
            {
                DiagnosticUtility.OperationInvoked(operationName);

                var domainService = this.GetDomainService(instance);

                // invoke the operation and process the result
                this.ConvertInputs(inputs);
                var result = this.InvokeCore(domainService, inputs, out outputs);
                result = this.ConvertReturnValue(result);

                DiagnosticUtility.OperationCompleted(operationName, DiagnosticUtility.GetDuration(startTicks));
                return result;
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
        }

        protected virtual void ConvertInputs(object[] inputs)
        {
        }

        protected virtual object ConvertReturnValue(object returnValue)
        {
            return returnValue;
        }

        protected virtual object InvokeCore(object instance, object[] inputs, out object[] outputs)
        {
            throw new NotImplementedException();
        }

        protected virtual ValueTask<object> InvokeCoreAsync(object instance, object[] inputs)
        {
            var result = InvokeCore(instance, inputs, out _);
            return new ValueTask<object>(result);
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            return TaskExtensions.BeginApm(InvokeAsync(instance, inputs), callback, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            outputs = ServiceUtility.EmptyObjectArray;
            return TaskExtensions.EndApm<object>(result);
        }

        private async Task<object> InvokeAsync(object instance, object[] inputs)
        {
            string operationName = this.Name;
            long startTicks = DiagnosticUtility.GetTicks();

            try
            {
                DiagnosticUtility.OperationInvoked(operationName);

                DomainService domainService = this.GetDomainService(instance);

                // invoke the operation and process the result
                this.ConvertInputs(inputs);
                var result = await this.InvokeCoreAsync(domainService, inputs).ConfigureAwait(false);
                result = this.ConvertReturnValue(result);

                DiagnosticUtility.OperationCompleted(operationName, DiagnosticUtility.GetDuration(startTicks));

                return result;
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
