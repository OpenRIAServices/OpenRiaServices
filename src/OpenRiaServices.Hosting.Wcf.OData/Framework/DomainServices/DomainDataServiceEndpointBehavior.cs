namespace OpenRiaServices.Hosting.Wcf.OData
{
    #region Namespaces
    
    using System.Diagnostics;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    
    #endregion

    /// <summary>
    /// WCF behavior for data service end point on top of a domain service.
    /// </summary>
    internal class DomainDataServiceEndpointBehavior : WebHttpBehavior
    {
        /// <summary>Data service factory used to create data service instances.</summary>
        private DomainDataServiceFactory serviceFactory;

        /// <summary>Data Service metadata object corresponding to domain service description.</summary>
        public DomainDataServiceMetadata DomainDataServiceMetadata
        {
            get;
            internal set;
        }

        /// <summary>Apply the behavior to the service end point.</summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">Endpoint dispatcher to which behaviors are applied.</param>
        public override void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            Debug.Assert(this.DomainDataServiceMetadata != null, "this.DomainDataServiceMetadata != null");
            this.serviceFactory = new DomainDataServiceFactory(this.DomainDataServiceMetadata);

            // Add operation to handle service document request.
            endpointDispatcher.DispatchRuntime.Operations.Add(
                new DispatchOperation(endpointDispatcher.DispatchRuntime, ServiceUtils.ServiceDocumentOperationName, null, null)
                {
                    Invoker = new NullOperationInvoker(),
                    Formatter = new DomainDataServiceDispatchMessageFormatter(this.serviceFactory)
                });

            // Add operation to handle metadata request.
            endpointDispatcher.DispatchRuntime.Operations.Add(
                new DispatchOperation(endpointDispatcher.DispatchRuntime, ServiceUtils.MetadataOperationName, null, null)
                {
                    Invoker = new NullOperationInvoker(),
                    Formatter = new DomainDataServiceDispatchMessageFormatter(this.serviceFactory)
                });

            // Add the base behavior.
            base.ApplyDispatchBehavior(endpoint, endpointDispatcher);
        }

        /// <summary>Get the operation selector for the endpoint.</summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <returns>Operation selector for the given endpoint.</returns>
        protected override WebHttpDispatchOperationSelector GetOperationSelector(ServiceEndpoint endpoint)
        {
            Debug.Assert(this.serviceFactory != null, "Must have a valid service factory.");
            return new DomainDataServiceOperationSelector(endpoint, this.serviceFactory.DomainDataServiceMetadata);
        }

        /// <summary>
        /// Gets the query string converter.
        /// </summary>
        /// <param name="operationDescription">The service operation.</param>
        /// <returns>Query string converter instance.</returns>
        protected override QueryStringConverter GetQueryStringConverter(OperationDescription operationDescription)
        {
            Debug.Assert(
                this.serviceFactory.DomainDataServiceMetadata.Sets.ContainsKey(operationDescription.Name) ||
                this.serviceFactory.DomainDataServiceMetadata.ServiceOperations.ContainsKey(operationDescription.Name),
                "Expect only operations exposed by the OData metadata.");

            return new DomainDataServiceQueryStringConverter();
        }

        /// <summary>Get the reply formatter for an operation belonging to the endpoint.</summary>
        /// <param name="operationDescription">Operation description.</param>
        /// <param name="endpoint">End point.</param>
        /// <returns>Message formatter.</returns>
        protected override IDispatchMessageFormatter GetReplyDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            Debug.Assert(this.serviceFactory != null, "Must have a valid service factory instance.");
            return new DomainDataServiceDispatchMessageFormatter(this.serviceFactory);
        }

        /// <summary>
        /// Adds server-side error handlers.
        /// </summary>
        /// <param name="endpoint">The endpoint for which error handlers are added.</param>
        /// <param name="endpointDispatcher">The dispatcher to which error handlers are added.</param>
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new DomainDataServiceErrorHandler());
        }
    }
}