using System;

namespace OpenRiaServices.Hosting.WCF.OData
{
    #region Namespaces
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Data.Services.Providers;
    using System.Diagnostics;
    using System.Net;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;    

    #endregion

    /// <summary>Selects the domain service operation to be invoked by WCF corresponding to current request.</summary>
    class DomainDataServiceOperationSelector : WebHttpDispatchOperationSelector
    {
        /// <summary>
        /// Base URI of the end point.
        /// </summary>
        readonly Uri baseUri;

        /// <summary>
        /// Mapping between resource set names and their corresponding query operations.
        /// </summary>
        readonly Dictionary<string, string> serviceRootQueryOperations;

        /// <summary>Constructs the operation selector for runtime.</summary>
        /// <param name="endpoint">End point.</param>
        /// <param name="metadata">Domain data service metadata.</param>
        public DomainDataServiceOperationSelector(
            ServiceEndpoint endpoint,
            DomainDataServiceMetadata metadata)
            : base(DomainDataServiceOperationSelector.ExtractNonRootQueryServiceOperations(endpoint, metadata))
        {
            this.baseUri = endpoint.ListenUri;
            this.serviceRootQueryOperations = new Dictionary<string, string>();

            // Collect all the query operations, since they are to be handled by selector in this class.
            foreach (var od in endpoint.Contract.Operations)
            {
                string resourceSetName = DomainDataServiceOperationSelector.GetRootQueryOperation(od, metadata);
                if (!String.IsNullOrEmpty(resourceSetName))
                {
                    Debug.Assert(!this.serviceRootQueryOperations.ContainsKey(resourceSetName), "There should only be 1 default query operation per set.");

                    // Note the fact that requests on resourceSet correspond to the given operation.
                    this.serviceRootQueryOperations.Add(resourceSetName, od.Name);
                }
            }
        }

        /// <summary>
        /// Selects the service operation to call.
        /// </summary>
        /// <param name="message">The Message object sent to invoke a service operation.</param>
        /// <param name="uriMatched">A value that specifies whether the URI matched a specific service operation.</param>
        /// <returns>The name of the service operation to call.</returns>
        protected override string SelectOperation(ref Message message, out bool uriMatched)
        {
            uriMatched = false;

            string[] segments = UriUtils.EnumerateSegments(message.Properties.Via, this.baseUri);
            Debug.Assert(segments != null, "We should be getting a non-null segments collection.");

            object property;
            HttpRequestMessageProperty httpRequestMessageProperty = null;
            if (message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out property))
            {
                httpRequestMessageProperty = (HttpRequestMessageProperty)property;
            }

            if (httpRequestMessageProperty == null)
            {
                return base.SelectOperation(ref message, out uriMatched);
            }

            // Dis-allow query options.
            DomainDataServiceOperationSelector.DisallowQueryOptions(message.Properties.Via);

            string identifier = null;

            if (segments.Length > 0 && UriUtils.ExtractSegmentIdentifier(segments[0], out identifier))
            {
                // Disallow selection of entries within response sets.
                DomainDataServiceOperationSelector.DisallowEntrySelection(identifier, segments[0]); 
            }

            // Service description or metadata request.
            if (0 == segments.Length || (1 == segments.Length && identifier == ServiceUtils.MetadataOperationName))
            {
                DomainDataServiceOperationSelector.DisallowNonGetRequests(httpRequestMessageProperty.Method);

                // Metadata requests only available through GET.
                uriMatched = true;
                Collection<UriTemplateMatch> matches = DomainDataServiceOperationSelector.CreateAstoriaTemplate(this.baseUri).Match(message.Properties.Via);
                if (matches.Count > 0)
                {
                    message.Properties.Add(
                        ServiceUtils.UriTemplateMatchResultsPropertyName,
                        matches[0]);

                    // Dis-allow json requests for metadata documents.
                    DomainDataServiceOperationSelector.DisallowJsonRequests(
                        identifier != null ? RequestKind.MetadataDocument : RequestKind.ServiceDocument, 
                        httpRequestMessageProperty.Headers[HttpRequestHeader.Accept]);

                    return identifier ?? ServiceUtils.ServiceDocumentOperationName;
                }
                else
                {
                    // Let the base error with Endpoint not found.
                    return base.SelectOperation(ref message, out uriMatched);
                }
            }
            else
            {
                if (segments.Length > 1)
                {
                    // More than 1 segments e.g. navigation is not supported.
                    throw new DomainDataServiceException((int)HttpStatusCode.BadRequest, Resource.DomainDataService_MultipleSegments_NotAllowed);
                }

                // Remove the trailing parentheses from the URI.
                message.Headers.To = UriUtils.ReplaceLastSegment(message.Headers.To, identifier);

                string operationName;
                if (this.serviceRootQueryOperations.TryGetValue(identifier, out operationName))
                {
                    // Only allow GETs on resource sets.
                    DomainDataServiceOperationSelector.DisallowNonGetRequests(httpRequestMessageProperty.Method);

                    // Check if a resource set request matches current request.
                    uriMatched = true;
                    message.Properties.Add(
                        ServiceUtils.UriTemplateMatchResultsPropertyName,
                        DomainDataServiceOperationSelector.CreateAstoriaTemplate(this.baseUri).Match(message.Properties.Via)[0]);

                    // Dis-allow json requests for resource sets.
                    DomainDataServiceOperationSelector.DisallowJsonRequests(RequestKind.ResourceSet, httpRequestMessageProperty.Headers[HttpRequestHeader.Accept]);

                    return operationName;
                }
            }

            string result;
            try
            {
                // Delegate to base for all non-root query operations.
                result = base.SelectOperation(ref message, out uriMatched);
            }
            catch (Exception innerException)
            {
                if (innerException.IsFatal())
                {
                    throw;
                }
                else
                {
                    throw new DomainDataServiceException((int)HttpStatusCode.NotFound, Resource.DomainDataService_Selection_Error, innerException);
                }
            }

            if (uriMatched == false)
            {
                throw new DomainDataServiceException((int)HttpStatusCode.NotFound, Resource.DomainDataService_Operation_NotFound);
            }
            else
            if (String.IsNullOrEmpty(result))
            {
                DomainDataServiceException e = new DomainDataServiceException((int)HttpStatusCode.MethodNotAllowed, Resource.DomainDataService_Operation_Method_NotAllowed);
                e.ResponseAllowHeader = httpRequestMessageProperty.Method == ServiceUtils.HttpGetMethodName ? ServiceUtils.HttpPostMethodName : ServiceUtils.HttpGetMethodName;
                throw e;
            }

            // Dis-allow json returning service operation requests.
            DomainDataServiceOperationSelector.DisallowJsonRequests(RequestKind.ServiceOperation, httpRequestMessageProperty.Headers[HttpRequestHeader.Accept]);

            return result;
        }

        /// <summary>
        /// Obtains the service operations available on the model and hands over all those
        /// operations to the base class for processing. The root query operations are
        /// handled by this class itself.
        /// </summary>
        /// <param name="endpoint">Endpoint on which operations are defined.</param>
        /// <param name="metadata">Metadata of the domain service.</param>
        /// <returns>Endpoint that contains all the operations that base class needs to process.</returns>
        private static ServiceEndpoint ExtractNonRootQueryServiceOperations(ServiceEndpoint endpoint, DomainDataServiceMetadata metadata)
        {
            ContractDescription cd = new ContractDescription(endpoint.Contract.Name);

            // Provide all the non-root query operations to the base class, only provide those operations that
            // have some representation on the domain data service.
            foreach (OperationDescription od in endpoint.Contract.Operations)
            {
                if (metadata.ServiceOperations.Keys.Contains(od.Name))
                {
                    Debug.Assert(
                        String.IsNullOrEmpty(DomainDataServiceOperationSelector.GetRootQueryOperation(od, metadata)),
                        "Service operation must not be a root query operation.");
                    cd.Operations.Add(od);
                }
            }

            return new ServiceEndpoint(cd, endpoint.Binding, endpoint.Address)
            {
                ListenUri = endpoint.ListenUri
            };
        }

        /// <summary>
        /// Given an operation descriptions, detects if it corresponds to a root query operation.
        /// </summary>
        /// <param name="od">Given operation description.</param>
        /// <param name="metadata">Metadata for the domain data service.</param>
        /// <returns>null if operation does not correspond to root query operation, otherwise the name of the resource set.</returns>
        private static string GetRootQueryOperation(OperationDescription od, DomainDataServiceMetadata metadata)
        {
            ResourceSet resourceSet;

            if (metadata.Sets.TryGetValue(od.Name, out resourceSet))
            {
                Debug.Assert(resourceSet != null, "resourceSet != null");
                return resourceSet.Name;
            }

            return null;
        }
        
        /// <summary>
        /// Create UriTemplate collection that universally accepts all query operations.
        /// </summary>
        /// <param name="baseUri">Base endpoint URI.</param>
        /// <returns>Table that contains a universal matcher.</returns>
        private static UriTemplateTable CreateAstoriaTemplate(Uri baseUri)
        {
            UriTemplateTable table = new UriTemplateTable();
            table.BaseAddress = baseUri;
            table.KeyValuePairs.Add(new KeyValuePair<UriTemplate, object>(new UriTemplate(ServiceUtils.MatchAllWildCard), null));
            return table;
        }

        /// <summary>Disallows query options for WCF Data Service endpoint.</summary>
        /// <param name="requestUri">Message request URI.</param>
        private static void DisallowQueryOptions(Uri requestUri)
        {
            UriTemplateMatch m = new UriTemplateMatch { RequestUri = requestUri };
            NameValueCollection collection = m.QueryParameters;

            foreach (string key in collection.Keys)
            {
                if (key.TrimStart().StartsWith("$", StringComparison.Ordinal))
                {
                    throw new DomainDataServiceException((int)HttpStatusCode.BadRequest, Resource.DomainDataService_QueryOptions_NotAllowed);
                }
            }
        }

        /// <summary>
        /// Checks if the HTTP request is a GET request and throws otherwise.
        /// </summary>
        /// <param name="httpMethod">Request HTTP method.</param>
        private static void DisallowNonGetRequests(string httpMethod)
        {
            if (httpMethod != ServiceUtils.HttpGetMethodName)
            {
                DomainDataServiceException e = new DomainDataServiceException((int)HttpStatusCode.MethodNotAllowed, Resource.DomainDataService_ResourceSets_Metadata_OnlyAllowedGet);
                e.ResponseAllowHeader = ServiceUtils.HttpGetMethodName;
                throw e;
            }
        }

        /// <summary>
        /// Check if the request URI is attempting to select a single element fromo request and disallows selection on key values.
        /// </summary>
        /// <param name="identifier">Identitifer representing root query including possible key values.</param>
        /// <param name="segment">Identifier representing root query excluding anything after the name.</param>
        private static void DisallowEntrySelection(string identifier, string segment)
        {
            if (segment.Substring(identifier.Length) != "()")
            {
                throw new DomainDataServiceException((int)HttpStatusCode.BadRequest, Resource.DomainDataServices_Selection_KeyNotSupported);
            }
        }

        /// <summary>Disallows requests that would like the response in json format.</summary>
        /// <param name="requestKind">Type of request.</param>
        /// <param name="acceptHeader">Accept header value.</param>
        private static void DisallowJsonRequests(RequestKind requestKind, string acceptHeader)
        {
            if (HttpProcessUtils.IsJsonRequest(requestKind, acceptHeader))
            {
                throw new DomainDataServiceException((int)HttpStatusCode.UnsupportedMediaType, Resource.HttpProcessUtility_UnsupportedMediaType);
            }
        }
    }
}