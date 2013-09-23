using System;
using System.ServiceModel;

namespace OpenRiaServices.DomainServices.Hosting.OData
{
    #region Namespaces
    using System.ComponentModel;
    using System.Net;
    using System.Reflection;
    using System.ServiceModel.Dispatcher;
    using OpenRiaServices.DomainServices.Server;
    using System.Web;

    #endregion

    /// <summary>Base class for all operation invokers supported on the domain data service endpoint.</summary>
    internal abstract class DomainDataServiceOperationInvoker : IOperationInvoker
    {
        /// <summary>Operation type.</summary>
        private DomainOperationType operationType;

        /// <summary>Constructs an invoker instance.</summary>
        /// <param name="operationType">Operation type.</param>
        internal DomainDataServiceOperationInvoker(DomainOperationType operationType)
        {
            this.operationType = operationType;
        }

        /// <summary>
        /// Gets a value that specifies whether the Invoke or InvokeBegin method is called by the dispatcher.
        /// </summary>
        public bool IsSynchronous
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns an array of parameter objects.
        /// </summary>
        /// <returns>The parameters that are to be used as arguments to the operation.</returns>
        public abstract object[] AllocateInputs();

        /// <summary>
        /// Returns an object and a set of output objects from an instance and set of input objects. 
        /// </summary>
        /// <param name="instance">The object to be invoked.</param>
        /// <param name="inputs">The inputs to the method.</param>
        /// <param name="outputs">The outputs from the method.</param>
        /// <returns>The return value.</returns>
        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            object result = null;
            DomainService domainService = null;

            try
            {
                // Instantiate the domain service.
                domainService = this.GetDomainService(instance);

                // Validate the requst.
                DomainDataServiceOperationInvoker.VerifyRequest(domainService);

                // Obtain the result.
                this.ConvertInputs(inputs);
                result = this.InvokeCore(domainService, inputs, out outputs);
                result = this.ConvertReturnValue(result);
            }
            catch (DomainDataServiceException)
            {
                // if the exception has already been transformed to DomainDataServiceException just rethrow it.
                throw;
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                else
                {
                    // We need to ensure that any time an exception is thrown by the service it is transformed to a 
                    // properly sanitized/configured DomainDataService exception.
                    throw new DomainDataServiceException(Resource.DomainDataService_General_Error, ex);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts input parameters in place.
        /// </summary>
        /// <param name="inputs">Input parameters.</param>
        protected virtual void ConvertInputs(object[] inputs)
        {
        }

        /// <summary>
        /// Converts the return value.
        /// </summary>
        /// <param name="returnValue">Return value.</param>
        /// <returns>Converted return value.</returns>
        protected virtual object ConvertReturnValue(object returnValue)
        {
            return returnValue;
        }

        /// <summary>
        /// Derived classes override this method to provide custom invocation behavior.
        /// </summary>
        /// <param name="instance">Instance to invoke the invoker against.</param>
        /// <param name="inputs">Input parameters post conversion.</param>
        /// <param name="outputs">Optional out parameters.</param>
        /// <returns>Result of invocation.</returns>
        protected abstract object InvokeCore(object instance, object[] inputs, out object[] outputs);

        /// <summary>
        /// An asynchronous implementation of the Invoke method.
        /// </summary>
        /// <param name="instance">The object to be invoked.</param>
        /// <param name="inputs">The inputs to the method.</param>
        /// <param name="callback">The asynchronous callback object.</param>
        /// <param name="state">Associated state data.</param>
        /// <returns>A System.IAsyncResult used to complete the asynchronous call.</returns>
        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// The asynchronous end method.
        /// </summary>
        /// <param name="instance">The object invoked.</param>
        /// <param name="outputs">The outputs from the method.</param>
        /// <param name="result">The System.IAsyncResult object.</param>
        /// <returns>The return value.</returns>
        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Validate the current request.
        /// </summary>
        /// <param name="domainService">Domain service instance for which request was sent.</param>
        private static void VerifyRequest(DomainService domainService)
        {
            EnableClientAccessAttribute ecaAttribute = (EnableClientAccessAttribute)TypeDescriptor.GetAttributes(domainService)[typeof(EnableClientAccessAttribute)];
            System.Diagnostics.Debug.Assert(ecaAttribute != null, "The OData Endpoint shouldn't be created if EnableClientAccess attribute is missing on the domain service type.");

            if (ecaAttribute.RequiresSecureEndpoint)
            {
                if (HttpContext.Current != null)
                {
                    if (HttpContext.Current.Request.IsSecureConnection)
                    {
                        return;
                    }
                }
                else if (OperationContext.Current != null)
                {
                    // DEVNOTE(wbasheer): See what the RIA people do here and match.
                }

                throw new DomainDataServiceException((int)HttpStatusCode.Forbidden, Resource.DomainDataService_Enable_Client_Access_Require_Secure_Connection);
            }
        }

        /// <summary>
        /// Instatiates a DomainService instance along with the DomainServiceContext.
        /// </summary>
        /// <param name="instance">Wrapper representing the instance passed to invocation.</param>
        /// <returns>New DomainService instance.</returns>
        private DomainService GetDomainService(object instance)
        {
            // Create and initialize the DomainService for this request.
            DomainDataServiceContractBehavior.DomainDataServiceInstanceInfo instanceInfo =
                (DomainDataServiceContractBehavior.DomainDataServiceInstanceInfo)instance;

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
                // If the exception has already been transformed to a DomainServiceException just rethrow it.
                if (tie.InnerException != null)
                {
                    throw new DomainDataServiceException(Resource.DomainDataService_General_Error, tie.InnerException);
                }

                throw new DomainDataServiceException(Resource.DomainDataService_General_Error, tie);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                else
                {
                    // We need to ensure that any time an exception is thrown by the service it is transformed to a 
                    // properly sanitized/configured DomainDataService exception.
                    throw new DomainDataServiceException(Resource.DomainDataService_General_Error, ex);
                }
            }
        }
    }
}
