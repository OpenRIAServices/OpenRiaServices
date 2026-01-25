using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

#nullable disable

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal sealed class XmlDataContractSerializationProvider(OpenRiaServicesOptions Options) : SerializationProvider
    {
        ConcurrentDictionary<(Type, string), XmlRequestSerializer> _serializers = new();
        Dictionary<Type, SerializationHelper> _perDomainServiceSerializationHelper = new Dictionary<Type, SerializationHelper>();

        public override RequestSerializer GetRequestSerializer(DomainServiceDescription domainServiceDescription, DomainOperationEntry operation)
        {
            var key = (operation.DomainServiceType, operation.Name);

            if (_serializers.TryGetValue(key, out var serializer))
                return serializer;


            if (!_perDomainServiceSerializationHelper.TryGetValue(operation.DomainServiceType, out var serializationHelper))
            {
                serializationHelper = new SerializationHelper(domainServiceDescription);
            }

            Type actualReturnType = operation.Operation switch
            {
                DomainOperation.Query => typeof(QueryResult<>).MakeGenericType(operation.AssociatedType),
                DomainOperation.Invoke => operation.ReturnType,
                DomainOperation.Custom when operation.Name == "Submit" => typeof(IEnumerable<ChangeSetEntry>),
                _ => throw new NotSupportedException()
            };

            serializer = new XmlRequestSerializer(Options, serializationHelper, serializationHelper.GetSerializer(actualReturnType), operation);
            return _serializers.GetOrAdd(key, serializer);
        }

        internal sealed class XmlRequestSerializer : RequestSerializer
        {
            private static readonly DataContractSerializer s_faultSerialiser = new DataContractSerializer(typeof(DomainServiceFault));
            private readonly SerializationHelper _serializationHelper;
            private readonly DataContractSerializer _responseSerializer;
            private readonly DomainOperationEntry _operation;
            private const string MessageRootElementName = "MessageRoot";
            private const string QueryOptionsListElementName = "QueryOptions";
            private const string QueryOptionElementName = "QueryOption";
            private const string QueryNameAttribute = "Name";
            private const string QueryValueAttribute = "Value";
            private const string QueryIncludeTotalCountOption = "includeTotalCount";
            private const string InvalidContentMessage = "invalid content";

            public XmlRequestSerializer(OpenRiaServicesOptions Options, SerializationHelper _serializationHelper, DataContractSerializer serializer, DomainOperationEntry domainOperationEntry)
            {
                this._serializationHelper = _serializationHelper;
                this._responseSerializer = serializer;
                _operation = domainOperationEntry;
            }

            public override async Task<(ServiceQuery, object[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation)
            {
                var request = context.Request;

                int initialCapacity = request.ContentLength switch
                {
                    long contentLength and >= 0 and <= int.MaxValue => Math.Min((int)contentLength, 4096),
                    null => 4096,
                    _ => throw new BadHttpRequestException("invalid lenght", (int)System.Net.HttpStatusCode.BadRequest)
                };

                // To prevent DOS attacks where an attacker can allocate arbitary large memory by setting content-length to a large value
                // We only allocate a maximum of 4K directly
                using var ms = new ArrayPoolStream(ArrayPool<byte>.Shared, maxBlockSize: 4 * 1024 * 1024);
                ms.Reset(initialCapacity); // Initial capacity up to 4K

                await request.BodyReader.CopyToAsync(ms).ConfigureAwait(false);
                ArraySegment<byte> memory = ms.GetRentedArrayAndClear();

                try
                {
                    using var reader = BinaryMessageReader.Rent(memory, isBinary: context.Request.ContentType != "application/xml");
                    return ReadQueryParametersFromBody(reader.XmlDictionaryReader, operation);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(memory.Array);
                }
            }

            public override async Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequest(HttpContext context)
            {
                (_, var parameter) = await ReadParametersFromBodyAsync(context, _operation);
                return (IEnumerable<ChangeSetEntry>)parameter[0];
            }

            private (ServiceQuery, object[]) ReadQueryParametersFromBody(XmlDictionaryReader reader, DomainOperationEntry operation)
            {
                ServiceQuery serviceQuery = null;
                object[] values;
                reader.MoveToContent();

                bool hasMessageRoot = reader.IsStartElement(MessageRootElementName);
                // Check for QueryOptions which is part of message root
                if (hasMessageRoot)
                {
                    // Go to the <QueryOptions> node.
                    reader.Read();                                               // <MessageRoot>
                    reader.ReadStartElement(QueryOptionsListElementName);        // <QueryOptions>
                    serviceQuery = ReadServiceQuery(reader);                     // <QueryOption></QueryOption>
                                                                                 // Go to the starting node of the original message.
                    reader.ReadEndElement();                                     // </QueryOptions>
                }

                values = ReadParameters(reader, operation);

                if (hasMessageRoot)
                    reader.ReadEndElement();

                // Verify at end 
                if (reader.ReadState != ReadState.EndOfFile)
                    throw new BadHttpRequestException(InvalidContentMessage);

                return (serviceQuery, values);
            }

            /// <summary>
            /// Reads the query options from the given reader and returns the resulting service query.
            /// It assumes that the reader is positioned on a stream containing the query options.
            /// </summary>
            /// <param name="reader">Reader to the stream containing the query options.</param>
            /// <returns>Extracted service query.</returns>
            internal static ServiceQuery ReadServiceQuery(XmlReader reader)
            {
                var serviceQueryParts = new List<ServiceQueryPart>();
                bool includeTotalCount = false;
                while (reader.IsStartElement(QueryOptionElementName))
                {
                    string name = reader.GetAttribute(QueryNameAttribute);
                    string value = reader.GetAttribute(QueryValueAttribute);
                    if (name.Equals(QueryIncludeTotalCountOption, StringComparison.OrdinalIgnoreCase))
                    {
                        bool queryOptionValue = false;
                        if (bool.TryParse(value, out queryOptionValue))
                        {
                            includeTotalCount = queryOptionValue;
                        }
                    }
                    else
                    {
                        serviceQueryParts.Add(new ServiceQueryPart { QueryOperator = name, Expression = value });
                    }

                    ReadElement(reader);
                }

                var serviceQuery = new ServiceQuery()
                {
                    QueryParts = serviceQueryParts,
                    IncludeTotalCount = includeTotalCount
                };
                return serviceQuery;
            }

            static void ReadElement(XmlReader reader)
            {
                if (reader.IsEmptyElement)
                {
                    reader.Read();
                }
                else
                {
                    reader.Read();
                    reader.ReadEndElement();
                }
            }

            object[] ReadParameters(XmlDictionaryReader reader, DomainOperationEntry operation)
            {
                if (operation.Name == "Submit")
                {
                    ReadSubmitRequest(reader);
                }

                if (reader.IsStartElement(operation.Name))
                {
                    reader.Read();

                    var parameters = operation.Parameters;
                    object[] values = new object[parameters.Count];
                    for (int i = 0; i < parameters.Count; ++i)
                    {
                        var parameter = parameters[i];
                        if (!reader.IsStartElement(parameter.Name))
                            throw new BadHttpRequestException(InvalidContentMessage);

                        if (reader.HasAttributes && reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true")
                        {
                            values[i] = null;
                            ReadElement(reader); // consume element
                        }
                        else
                        {
                            var serializer = _serializationHelper.GetSerializer(parameter.ParameterType);

                            // XmlElemtnt returns the "ResultNode" unless we step into the contents
                            bool isXElement = parameter.ParameterType == typeof(System.Xml.Linq.XElement);
                            if (isXElement)
                                reader.ReadStartElement();

                            values[i] = serializer.ReadObject(reader, verifyObjectName: false);

                            if (isXElement)
                            {
                                reader.ReadEndElement();
                                reader.ReadEndElement();
                            }
                        }
                    }

                    reader.ReadEndElement(); // operation.Name
                    return values;
                }
                else
                {
                    if (operation.Parameters.Count == 0)
                        return Array.Empty<object>();
                    else
                        throw new InvalidOperationException();
                }


                //TODO: Se if this can be merged above
                object[] ReadSubmitRequest(System.Xml.XmlDictionaryReader reader)
                {
                    reader.ReadStartElement("SubmitChanges");
                    if (!reader.IsStartElement("changeSet"))
                    {
                        throw new BadHttpRequestException("missing changeSet");
                    }

                    var changeSet = _serializationHelper.GetSerializer(typeof(IEnumerable<ChangeSetEntry>))
                        .ReadObject(reader, verifyObjectName: false);

                    reader.ReadEndElement();
                    return new object[] { changeSet };
                }
            }

            public override Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation)
            {
                var ct = context.RequestAborted;
                if (ct.IsCancellationRequested)
                    return Task.CompletedTask;

                var messageWriter = BinaryMessageWriter.Rent(isBinary: context.Request.ContentType != "application/xml");
                try
                {
                    WriteFault(fault, messageWriter.XmlWriter);

                    using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                    messageWriter = null;

                    var response = context.Response;
                    response.Headers.ContentType = context.Request.ContentType ?? "application/msbin1";
                    response.ContentLength = bufferMemory.Length;

                    return bufferMemory.WriteTo(response, ct);
                }
                catch (Exception)
                {
                    messageWriter?.Clear();
                    throw;
                }
            }

            private static void WriteFault(DomainServiceFault fault, XmlDictionaryWriter writer)
            {
                //<Fault xmlns="http://schemas.microsoft.com/ws/2005/05/envelope/none">
                writer.WriteStartElement("Fault", "http://schemas.microsoft.com/ws/2005/05/envelope/none");
                //<Code><Value>Sender</Value></Code>
                writer.WriteStartElement("Code");
                writer.WriteStartElement("Value");
                writer.WriteString("Sender");
                writer.WriteEndElement();
                writer.WriteEndElement();
                //<Reason ><Text xml:lang="en-US">Access to operation 'GetRangeWithNotAuthorized' was denied.</Text></Reason>
                writer.WriteStartElement("Reason");
                writer.WriteStartElement("Text");
                writer.WriteAttributeString("xml", "lang", null, CultureInfo.CurrentCulture.Name);
                writer.WriteString(fault.ErrorMessage);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("Detail");
                s_faultSerialiser.WriteObject(writer, fault);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            public override Task WriteResponseAsync(HttpContext context, object result, DomainOperationEntry operation)
            {
                var ct = context.RequestAborted;
                if (ct.IsCancellationRequested)
                    return Task.CompletedTask;

                var messageWriter = BinaryMessageWriter.Rent(isBinary: context.Request.ContentType != "application/xml");
                try
                {
                    WriteResponse(messageWriter.XmlWriter, result, operation);

                    using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                    messageWriter = null;

                    var response = context.Response;
                    response.Headers.ContentType = context.Request.ContentType ?? "application/msbin1";
                    response.ContentLength = bufferMemory.Length;
                    // TODO: Move to invokers
                    response.Headers.CacheControl = "private, no-store";

                    return bufferMemory.WriteTo(response, ct);
                }
                catch (Exception)
                {
                    messageWriter?.Clear();
                    throw;
                }
            }

            public override Task WriteSubmitResponse(HttpContext context, IEnumerable<ChangeSetEntry> result)
            {
                return WriteResponseAsync(context, result, _operation);
            }

            private void WriteResponse(XmlDictionaryWriter writer, object result, DomainOperationEntry operation)
            {
                // TODO: Cache name+ reposone

                // <GetQueryableRangeTaskResponse xmlns="http://tempuri.org/">
                writer.WriteStartElement(operation.Name + "Response", "http://tempuri.org/");
                // <GetQueryableRangeTaskResult xmlns:a="DomainServices" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
                writer.WriteStartElement(operation.Name + "Result");
                //writer.WriteXmlnsAttribute("a", "DomainServices");
                //writer.WriteXmlnsAttribute("i", "http://www.w3.org/2001/XMLSchema-instance");

                // TODO: Cache serializationHelper.GetSerializer(operation.ReturnType)
                _responseSerializer.WriteObjectContent(writer, result);

                writer.WriteEndElement(); // ***Result
                writer.WriteEndElement(); // ***Response
            }


        }
    }
}
