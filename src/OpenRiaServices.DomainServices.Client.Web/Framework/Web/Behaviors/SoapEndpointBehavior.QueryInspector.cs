using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace OpenRiaServices.DomainServices.Client.Web.Behaviors
{
    partial class SoapEndpointBehavior
    {
        /// <summary>
        ///  Inspector which handles serialization of query parameters and adds them 
        ///  to <see cref="Message.Headers"/> as SOAP header
        /// </summary>
        sealed class QueryInspector : IClientMessageInspector
        {
            private const string IncludeTotalCountPropertyName = "DomainServiceIncludeTotalCount";
            private const string QueryPropertyName = "DomainServiceQuery";

            public QueryInspector()
            {
            }

            object IClientMessageInspector.BeforeSendRequest(ref Message request, IClientChannel channel)
            {
                // Add Query Options if any are set
                var messageProperties = OperationContext.Current?.OutgoingMessageProperties;
                if (messageProperties != null)
                {
                    messageProperties.TryGetValue(QueryPropertyName, out object queryProperty);
                    messageProperties.TryGetValue(IncludeTotalCountPropertyName, out object includeTotalCountProperty);

                    // Add Query Options header if any options were specified
                    if (queryProperty != null || includeTotalCountProperty != null)
                    {
                        var queryParts = (queryProperty != null) ? QuerySerializer.Serialize((IQueryable)queryProperty) : null;
                        var includeTotalCount = (bool?)includeTotalCountProperty;

                        var header = new QueryOptionsHeader(queryParts, includeTotalCount == true);
                        request.Headers.Add(header);
                    }
                }

                return null;
            }

            void IClientMessageInspector.AfterReceiveReply(ref Message reply, object correlationState)
            {
                // Method intentionally left empty.
            }

            /// <summary>
            /// Class represponsible for serialization of query options as a SOAP header
            /// </summary>
            class QueryOptionsHeader : MessageHeader
            {
                private const string QueryOptionElementName = "QueryOption";
                private const string QueryNameAttribute = "Name";
                private const string QueryValueAttribute = "Value";
                private const string QueryIncludeTotalCountOption = "includeTotalCount";
                private const string QueryHeaderName = "DomainServiceQuery";
                private const string QueryHeaderNamespace = "DomainServices";

                private readonly IEnumerable<ServiceQueryPart> _queryParts;
                private readonly bool _includeTotalCount;

                public QueryOptionsHeader(IEnumerable<ServiceQueryPart> queryParts, bool includeTotalCount)
                {
                    _queryParts = queryParts;
                    _includeTotalCount = includeTotalCount;
                }

                public override string Name => QueryHeaderName;

                public override string Namespace => QueryHeaderNamespace;

                protected override void OnWriteHeaderContents(System.Xml.XmlDictionaryWriter writer, MessageVersion messageVersion)
                {
                    if (_queryParts != null)
                    {
                        foreach (var part in _queryParts)
                        {
                            writer.WriteStartElement(QueryOptionElementName);
                            writer.WriteAttributeString(QueryNameAttribute, part.QueryOperator);
                            writer.WriteAttributeString(QueryValueAttribute, part.Expression);
                            writer.WriteEndElement();
                        }
                    }

                    if (_includeTotalCount)
                    {
                        writer.WriteStartElement(QueryOptionElementName);
                        writer.WriteAttributeString(QueryNameAttribute, QueryIncludeTotalCountOption);
                        writer.WriteAttributeString(QueryValueAttribute, "true");
                        writer.WriteEndElement();
                    }
                }
            }
        }
    }
}
