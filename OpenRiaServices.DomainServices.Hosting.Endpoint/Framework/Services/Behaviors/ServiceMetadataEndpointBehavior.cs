using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.Xml;

namespace OpenRiaServices.DomainServices.Hosting
{
    internal class ServiceMetadataEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(endpoint.Contract.ContractType);
            ServiceMetadataGenerator.GenerateEntitiesMetadataJsonMap(description);

            foreach (OperationDescription od in endpoint.Contract.Operations)
            {
                foreach (IOperationBehavior behavior in od.Behaviors)
                {
                    Type behaviorType = behavior.GetType();
                    if (behaviorType.IsGenericType && behaviorType.GetGenericTypeDefinition().Equals(typeof(QueryOperationBehavior<>)))
                    {
                        IDispatchMessageFormatter innerFormatter = endpointDispatcher.DispatchRuntime.Operations[od.Name].Formatter;
                        endpointDispatcher.DispatchRuntime.Operations[od.Name].Formatter = new ServiceMetadataQueryOperationMessageFormatter(innerFormatter, DomainOperationType.Query, description.GetQueryMethod(od.Name).ReturnType);
                    }
                }
            }
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        private class ServiceMetadataQueryOperationMessageFormatter : IDispatchMessageFormatter
        {
            readonly IDispatchMessageFormatter innerFormatter;
            readonly DomainOperationType operationType;
            readonly Type entityType;

            public ServiceMetadataQueryOperationMessageFormatter(IDispatchMessageFormatter innerFormatter, DomainOperationType operationType, Type entityType)
            {
                this.innerFormatter = innerFormatter;
                this.operationType = operationType;
                this.entityType = TypeUtility.GetElementType(entityType);
            }

            public void DeserializeRequest(Message message, object[] parameters)
            {
                this.innerFormatter.DeserializeRequest(message, parameters);
            }

            public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
            {
                Message message = this.innerFormatter.SerializeReply(messageVersion, parameters, result);

                if (!message.IsEmpty)
                {
                    Message originalMessage = message;
                    ServiceMetadataBodyWriter bodyWriter = new ServiceMetadataBodyWriter(originalMessage, this.entityType);
                    message = Message.CreateMessage(messageVersion, null, bodyWriter);
                    message.Headers.CopyHeadersFrom(originalMessage.Headers);
                    message.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));
                }

                return message;
            }
        }

        private class ServiceMetadataBodyWriter : BodyWriter
        {
            readonly Message originalMessage;
            readonly string entityTypeName;

            public ServiceMetadataBodyWriter(Message originalMessage, Type entityType)
                : base(false)
            {
                this.originalMessage = originalMessage;
                this.entityTypeName = entityType.Name;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                XmlDictionaryReader reader = this.originalMessage.GetReaderAtBodyContents();

                // Write root StartElement
                writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                writer.WriteAttributes(reader, true);
                reader.ReadStartElement();

                // Write QueryResult StartElement
                writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                writer.WriteAttributes(reader, true);

                // Write QueryResult content
                string nodeName = reader.LocalName;
                reader.Read();

                while (reader.NodeType != XmlNodeType.EndElement || reader.Name != nodeName)
                {
                    XmlReader subtree = reader.ReadSubtree();
                    writer.WriteNode(subtree, false);
                    reader.ReadEndElement();
                }

                // Insert metadata start
                writer.WriteStartElement("Metadata");
                writer.WriteAttributeString("type", "array");
                // This two foreach loops are to ensure we write the return entity of the query first, then all the rest.
                // This is a requirement of the RIA/JS client side implementation. If modifying this, client side needs update too.
                foreach (ServiceMetadataGenerator.TypeMetadata map in ServiceMetadataGenerator.EntitiesMetadata)
                {
                    if (map.Name == this.entityTypeName)
                    {
                        writer.WriteStartElement("item");
                        writer.WriteAttributeString("type", "object");
                        map.WriteJson(writer);
                        writer.WriteEndElement();
                        break;
                    }
                }
                foreach (ServiceMetadataGenerator.TypeMetadata map in ServiceMetadataGenerator.EntitiesMetadata)
                {
                    if (map.Name != this.entityTypeName)
                    {
                        writer.WriteStartElement("item");
                        writer.WriteAttributeString("type", "object");
                        map.WriteJson(writer);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
                // Insert metadata end

                // Close QueryResult
                writer.WriteEndElement();
                // Close root
                writer.WriteEndElement();
            }
        }
    }
}
