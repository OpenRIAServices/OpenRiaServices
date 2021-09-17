﻿using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.HttpDomainClient
{
    class BinaryXmlContent : HttpContent
    {
        private readonly BinaryHttpDomainClient _domainClient;
        private readonly string _operationName;
        private readonly IDictionary<string, object> _parameters;
        private readonly List<ServiceQueryPart> _queryOptions;

        public BinaryXmlContent(BinaryHttpDomainClient domainClient,
            string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions)
        {
            this._domainClient = domainClient;
            this._operationName = operationName;
            this._parameters = parameters;
            this._queryOptions = queryOptions;

            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/msbin1");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using (var writer = System.Xml.XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, ownsStream: false))
            {
                // Write message
                var rootNamespace = "http://tempuri.org/";
                bool hasQueryOptions = (_queryOptions != null && _queryOptions.Count > 0);

                if (hasQueryOptions)
                {
                    writer.WriteStartElement("MessageRoot");
                    writer.WriteStartElement("QueryOptions");
                    foreach (var queryOption in _queryOptions)
                    {
                        writer.WriteStartElement("QueryOption");
                        writer.WriteAttributeString("Name", queryOption.QueryOperator);
                        writer.WriteAttributeString("Value", queryOption.Expression);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteStartElement(_operationName, rootNamespace); // <OperationName>

                // Write all parameters
                if (_parameters != null && _parameters.Count > 0)
                {
                    foreach (var param in _parameters)
                    {
                        writer.WriteStartElement(param.Key);  // <ParameterName>
                        if (param.Value != null)
                        {
                            var serializer = _domainClient.GetSerializer(param.Value.GetType());
                            serializer.WriteObjectContent(writer, param.Value);
                        }
                        else
                        {
                            // Null input
                            writer.WriteAttributeString("i", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
                        }
                        writer.WriteEndElement();            // </ParameterName>
                    }
                }

                writer.WriteEndDocument(); // </OperationName> and </MessageRoot> if present
                writer.Flush();
            }

            return Task.CompletedTask;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
