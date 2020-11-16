using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Web;

namespace OpenRiaServices.Hosting.Wcf.Behaviors
{
    /// <summary>
    /// A REST endpoint behavior which injects a message inspector that parses query headers.
    /// </summary>
    internal class DomainServiceWebHttpBehavior : WebHttpBehavior
    {
        /// <summary>
        /// Gets the query string converter.
        /// </summary>
        /// <param name="operationDescription">The service operation.</param>
        /// <returns>A <see cref="System.ServiceModel.Dispatcher.QueryStringConverter"/> instance.</returns>
        protected override QueryStringConverter GetQueryStringConverter(OperationDescription operationDescription)
        {
            return new WebHttpQueryStringConverter();
        }

        /// <summary>
        /// Adds server-side error handlers.
        /// </summary>
        /// <param name="endpoint">The endpoint for which error handlers are added.</param>
        /// <param name="endpointDispatcher">The dispatcher to which error handlers are added.</param>
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new WebHttpErrorHandler(this.DefaultOutgoingResponseFormat));
        }

        /// <summary>
        /// Modifies the request dispatch formatter to read query parameters from the message body.
        /// </summary>
        /// <param name="operationDescription">The specified operation description.</param>
        /// <param name="endpoint">The specified endpoint.</param>
        /// <returns>The request dispatch formatter for the specified operation description and endpoint.</returns>
        protected override IDispatchMessageFormatter GetRequestDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            IDispatchMessageFormatter formatter = base.GetRequestDispatchFormatter(operationDescription, endpoint);
            IQueryOperationSettings querySettings = operationDescription.Behaviors.Find<IQueryOperationSettings>();
            if (querySettings != null)
            {
                formatter = new WebHttpQueryDispatchMessageFormatter(formatter, querySettings.HasSideEffects);
            }
            return formatter;
        }

        /// <summary>
        /// This method returns a ServiceQuery for the specified URL and query string.
        /// <remarks>
        /// This method must ensure that the original ordering of the query parts is maintained
        /// in the results. We want to do this without doing any custom URL parsing. The approach
        /// taken is to use HttpUtility to parse the query string, and from those results we search
        /// in the full URL for the relative positioning of those elements.
        /// </remarks>
        /// </summary>
        /// <param name="queryString">The query string portion of the URL</param>
        /// <param name="fullRequestUrl">The full request URL</param>
        /// <returns>The corresponding ServiceQuery</returns>
        internal static ServiceQuery GetServiceQuery(string queryString, string fullRequestUrl)
        {
            NameValueCollection queryPartCollection = HttpUtility.ParseQueryString(queryString);
            bool includeTotalCount = false;

            // Reconstruct a list of all key/value pairs
            List<string> queryParts = new List<string>();
            foreach (string queryPart in queryPartCollection)
            {
                if (queryPart == null || !queryPart.StartsWith("$", StringComparison.Ordinal))
                {
                    // not a special query string
                    continue;
                }

                if (queryPart.Equals("$includeTotalCount", StringComparison.OrdinalIgnoreCase))
                {
                    string value = queryPartCollection.GetValues(queryPart).First();
                    Boolean.TryParse(value, out includeTotalCount);
                    continue;
                }

                foreach (string value in queryPartCollection.GetValues(queryPart))
                {
                    queryParts.Add(queryPart + "=" + value);
                }
            }

            string decodedQueryString = HttpUtility.UrlDecode(fullRequestUrl);

            // For each query part, find all occurrences of it in the Url (could be duplicates)
            List<KeyValuePair<string, int>> keyPairIndicies = new List<KeyValuePair<string, int>>();
            foreach (string queryPart in queryParts.Distinct())
            {
                int idx;
                int endIdx = 0;
                while (((idx = decodedQueryString.IndexOf(queryPart, endIdx, StringComparison.Ordinal)) != -1) &&
                        (endIdx < decodedQueryString.Length - 1))
                {
                    // We found a match, however, we must ensure that the match is exact. For example,
                    // The string "$take=1" will be found twice in query string "?$take=10&$orderby=Name&$take=1",
                    // but the first match should be discarded. Therefore, before adding the match, we ensure
                    // the next character is EOS or the param separator '&'.
                    endIdx = idx + queryPart.Length - 1;
                    if ((endIdx == decodedQueryString.Length - 1) ||
                        (endIdx < decodedQueryString.Length - 1 && (decodedQueryString[endIdx + 1] == '&')))
                    {
                        keyPairIndicies.Add(new KeyValuePair<string, int>(queryPart, idx));
                    }
                }
            }

            // create the list of ServiceQueryParts in order, ordered by
            // their location in the query string
            IEnumerable<string> orderedParts = keyPairIndicies.OrderBy(p => p.Value).Select(p => p.Key);
            IEnumerable<ServiceQueryPart> serviceQueryParts =
                from p in orderedParts
                let idx = p.IndexOf('=')
                select new ServiceQueryPart(p.Substring(1, idx - 1), p.Substring(idx + 1));

            ServiceQuery serviceQuery = new ServiceQuery()
            {
                QueryParts = serviceQueryParts.ToList(),
                IncludeTotalCount = includeTotalCount
            };

            return serviceQuery;
        }

        /// <summary>
        /// A formatter for deserializing query requests which may have query parameters present in
        /// the To uri or message body.
        /// </summary>
        private class WebHttpQueryDispatchMessageFormatter : IDispatchMessageFormatter
        {
            private readonly IDispatchMessageFormatter _innerDispatchMessageFormatter;
            private readonly bool _queryHasSideEffects;

            public WebHttpQueryDispatchMessageFormatter(IDispatchMessageFormatter innerDispatchMessageFormatter, bool queryHasSideEffects)
            {
                this._innerDispatchMessageFormatter = innerDispatchMessageFormatter;
                this._queryHasSideEffects = queryHasSideEffects;
            }

            /// <summary>
            /// Deserializes the message into requests parameters. Also parses query parameters
            /// from the request and stores the results in the original message.
            /// </summary>
            /// <param name="message">The incoming message to deserialize.</param>
            /// <param name="parameters">The parameters that are passed to the query operation.
            /// </param>
            void IDispatchMessageFormatter.DeserializeRequest(Message message, object[] parameters)
            {
                Message originalMessage = message;
                var isPost = MessageUtility.IsHttpPOSTMethod(message.Properties);

                // If user tried to GET a query with side-effects then fail
                // Note: This should never ever happen since it would fail earlier in the WCF pipeline since the method should be "POST"-only
                if (_queryHasSideEffects && !isPost)
                    throw new FaultException("Must use POST to for queries with side effects");

                if (isPost)
                {
                    // If the HTTP Method is POST, get the query from the message body instead from the URL
                    ServiceQuery serviceQuery = MessageUtility.GetServiceQuery(ref message);
                    if (serviceQuery != null)
                    {
                        // Since a new message is returned by the GetServiceQueryFromMessageBody, the OperationContext does not find the property
                        // if set on the new message. So we set the property directly on the current OperationContext IncomingMessageProperties.
                        OperationContext.Current.IncomingMessageProperties[ServiceQuery.QueryPropertyName] = serviceQuery;
                    }
                }
                else if (!String.IsNullOrEmpty(message.Properties.Via.Query))
                {
                    string query = HttpUtility.UrlDecode(message.Properties.Via.Query);
                    string fullRequestUrl = HttpUtility.UrlDecode(HttpContext.Current.Request.RawUrl);
                    message.Properties[ServiceQuery.QueryPropertyName] = DomainServiceWebHttpBehavior.GetServiceQuery(query, fullRequestUrl);
                }

                try
                {
                    this._innerDispatchMessageFormatter.DeserializeRequest(message, parameters);
                }
                finally
                {
                    // The original message belongs to the service model pipeline. We cannot
                    // dispose it. On the other hand we could have just created a new message. If
                    // that is the case we are responsible for disposing the new message.
                    if (message != originalMessage)
                    {
                        message.Properties.Clear();
                        message.Headers.Clear();
                        message.Close();
                    }
                }
            }

            Message IDispatchMessageFormatter.SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
            {
                // The formatter is used for request deserialization only.
                throw new NotImplementedException();
            }
        }

        // NOTE: We should sync this with the .NET v.next implementation once that's there.
        private class WebHttpErrorHandler : IErrorHandler
        {
            private readonly WebContentFormat format;

            public WebHttpErrorHandler(WebMessageFormat format)
            {
                if (format == WebMessageFormat.Json)
                {
                    this.format = WebContentFormat.Json;
                }
                else
                {
                    this.format = WebContentFormat.Xml;
                }
            }

            public bool HandleError(Exception error)
            {
                return (error is FaultException<DomainServiceFault>);
            }

            public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
            {
                FaultException<DomainServiceFault> faultError = error as FaultException<DomainServiceFault>;
                if (faultError != null)
                {
                    if (this.format == WebContentFormat.Json)
                    {
                        // Create a fault message containing our FaultContract object.
                        fault = Message.CreateMessage(version, faultError.Action, faultError.Detail, new DataContractJsonSerializer(faultError.Detail.GetType()));
                        fault.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(this.format));

                        // Return custom error code.
                        var rmp = new HttpResponseMessageProperty();
                        rmp.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                        rmp.Headers[System.Net.HttpResponseHeader.ContentType] = "text/json";
                        fault.Properties.Add(HttpResponseMessageProperty.Name, rmp);
                    }
                    else
                    {
                        MessageFault messageFault = MessageFault.CreateFault(faultError.Code, faultError.Reason, faultError.Detail);
                        fault = Message.CreateMessage(MessageVersion.None, messageFault, null);
                    }
                }
            }
        }
    }
}
