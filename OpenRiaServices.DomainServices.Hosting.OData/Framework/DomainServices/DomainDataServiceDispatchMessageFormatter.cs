namespace OpenRiaServices.DomainServices.Hosting.OData
{
    #region Namespaces
    
    using System.IO;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    
    #endregion

    /// <summary>
    /// Formatter for serializing domain data service responses.
    /// </summary>
    internal class DomainDataServiceDispatchMessageFormatter : IDispatchMessageFormatter
    {
        /// <summary>
        /// Factory for creating data services on top of a domain service.
        /// </summary>
        private DomainDataServiceFactory serviceFactory;

        /// <summary>
        /// Constructs an instance of message formatter for the domain data service end point.
        /// </summary>
        /// <param name="serviceFactory">Factory for creating domain data service instances.</param>
        public DomainDataServiceDispatchMessageFormatter(DomainDataServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        /// <summary>
        /// Deserializes a message into an array of parameters.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="parameters">The objects that are passed to the operation as parameters.</param>
        public void DeserializeRequest(Message message, object[] parameters)
        {
        }

        /// <summary>
        /// Serializes a reply message from a specified message version, array of parameters, and a return value.
        /// </summary>
        /// <param name="messageVersion">The SOAP message version.</param>
        /// <param name="parameters">The out parameters.</param>
        /// <param name="result">The return value.</param>
        /// <returns>The serialized reply message.</returns>
        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            // Instantiate a new domain data service and ask it to process the incoming request.
            // We already have the result of the invocation handy, so we can simply pass it to
            // the IDSP implementation for domain data service.
            return this.serviceFactory.CreateService(result).ProcessRequestForMessage(new MemoryStream());
        }
    }
}