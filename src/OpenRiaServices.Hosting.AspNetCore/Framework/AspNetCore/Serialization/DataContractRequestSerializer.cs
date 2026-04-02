using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal sealed class TextXmlDataContractRequestSerializer : DataContractRequestSerializer
    {
        public TextXmlDataContractRequestSerializer(DomainOperationEntry operation, DataContractCache dataContractCache)
            : base(operation, dataContractCache, isBinary: false)
        {
        }

        public override bool CanRead(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.TextXml);

        public override bool CanWrite(ReadOnlySpan<char> contentType)
            => MatchesMediaType(contentType, MimeTypes.TextXml);
    }

    internal sealed class BinaryXmlDataContractRequestSerializer : DataContractRequestSerializer
    {
        public BinaryXmlDataContractRequestSerializer(DomainOperationEntry operation, DataContractCache dataContractCache)
            : base(operation, dataContractCache, isBinary: true)
        {
        }

        public override bool CanRead(ReadOnlySpan<char> contentType)
            => MemoryExtensions.Equals(contentType, MimeTypes.BinaryXml, StringComparison.Ordinal);

        public override bool CanWrite(ReadOnlySpan<char> contentType)
            => MemoryExtensions.Equals(contentType, MimeTypes.BinaryXml, StringComparison.Ordinal);
    }

    internal abstract class DataContractRequestSerializer : RequestSerializer
    {
        private static readonly DataContractSerializer s_faultSerialiser = new DataContractSerializer(typeof(DomainServiceFault));
        private readonly DataContractCache _dataContractCache;
        private readonly DataContractSerializer _responseSerializer;
        private readonly DomainOperationEntry _operation;
        private readonly string _responseName;
        private readonly string _resultName;
        private const string MessageRootElementName = "MessageRoot";
        private const string QueryOptionsListElementName = "QueryOptions";
        private const string QueryOptionElementName = "QueryOption";
        private const string QueryNameAttribute = "Name";
        private const string QueryValueAttribute = "Value";
        private const string QueryIncludeTotalCountOption = "includeTotalCount";
        private const string InvalidContentMessage = "invalid content";

        private bool IsBinary { get; }
        private string ContentType => IsBinary ? MimeTypes.BinaryXml : MimeTypes.TextXml;

        protected DataContractRequestSerializer(DomainOperationEntry operation, DataContractCache dataContractCache, bool isBinary)
        {
            Type actualReturnType = operation.Operation switch
            {
                DomainOperation.Query => typeof(QueryResult<>).MakeGenericType(operation.AssociatedType),
                DomainOperation.Invoke => operation.ReturnType,
                DomainOperation.Custom when operation.Name == "Submit" => typeof(IEnumerable<ChangeSetEntry>),
                _ => throw new NotSupportedException()
            };

            this.IsBinary = isBinary;
            this._dataContractCache = dataContractCache;
            this._operation = operation;
            this._responseName = operation.Name + "Response";
            this._resultName = operation.Name + "Result";
            this._responseSerializer = dataContractCache.GetSerializer(actualReturnType);

            if (operation.Operation == DomainOperation.Custom)
            {
                _responseName = "SubmitChangesResponse";
                _resultName = "SubmitChangesResult";
            }
        }

        public override async Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation)
        {
            var request = context.Request;

            int initialCapacity = request.ContentLength switch
            {
                long contentLength and >= 0 and <= int.MaxValue => Math.Min((int)contentLength, 4096),
                null => 4096,
                _ => throw new BadHttpRequestException("invalid length", (int)System.Net.HttpStatusCode.BadRequest)
            };

            // To prevent DOS attacks where an attacker can allocate arbitary large memory by setting content-length to a large value
            // We only allocate a maximum of 4K directly
            using var ms = new ArrayPoolStream(ArrayPool<byte>.Shared, maxBlockSize: 4 * 1024 * 1024);
            ms.Reset(initialCapacity); // Initial capacity up to 4K

            await request.BodyReader.CopyToAsync(ms).ConfigureAwait(false);
            ArraySegment<byte> memory = ms.GetRentedArrayAndClear();

            try
            {
                using var reader = BinaryMessageReader.Rent(memory, IsBinary);
                return ReadQueryParametersFromBody(reader.XmlDictionaryReader, operation);
            }
            catch (Exception ex) when (ex is not BadHttpRequestException && !ExceptionHandlingUtility.IsFatal(ex))
            {
                throw new BadHttpRequestException($"Failed to read body: {ex.Message}", ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(memory.Array!);
            }
        }

        public override async Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequestAsync(HttpContext context)
        {
            (_, var parameter) = await ReadParametersFromBodyAsync(context, _operation);
            return (IEnumerable<ChangeSetEntry>)parameter[0]!;
        }

        private (ServiceQuery?, object?[]) ReadQueryParametersFromBody(XmlDictionaryReader reader, DomainOperationEntry operation)
        {
            ServiceQuery? serviceQuery = null;
            object?[] values;
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
                string? name = reader.GetAttribute(QueryNameAttribute);
                string? value = reader.GetAttribute(QueryValueAttribute);
                if (string.Equals(name, QueryIncludeTotalCountOption, StringComparison.OrdinalIgnoreCase))
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

        object?[] ReadParameters(XmlDictionaryReader reader, DomainOperationEntry operation)
        {
            if (operation.Name == "Submit")
            {
                return ReadSubmitRequest(reader);
            }

            if (reader.IsStartElement(operation.Name))
            {
                if (operation.Parameters.Count == 0)
                {
                    bool isEmpty = reader.IsEmptyElement;
                    reader.Read();
                    // For TextXml we get an empty element here so we cannot call ReadEndElement below
                    if (!isEmpty)
                        reader.ReadEndElement();
                    return [];
                }

                reader.Read();
                var parameters = operation.Parameters;
                object?[] values = new object?[parameters.Count];
                for (int i = 0; i < parameters.Count; ++i)
                {
                    var parameter = parameters[i];
                    if (!reader.IsStartElement(parameter.Name))
                        throw new BadHttpRequestException(InvalidContentMessage);

                    if (reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true")
                    {
                        if (parameters[i].IsNullable)
                        {
                            values[i] = null;
                            ReadElement(reader); // consume element
                        }
                        else // missing value for required parameter
                        {
                            throw new BadHttpRequestException($"Null value provided for parameter '{parameters[i].Name}'");
                        }
                    }
                    else
                    {
                        var serializer = _dataContractCache.GetSerializer(parameter.ParameterType);

                        // XmlElement returns the "ResultNode" unless we step into the contents
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


            //TODO: Se if this can be merged above, will have to change operation.Name to return SubmitChanges instead of Submit
            object?[] ReadSubmitRequest(System.Xml.XmlDictionaryReader reader)
            {
                reader.ReadStartElement("SubmitChanges");
                if (!reader.IsStartElement("changeSet"))
                {
                    throw new BadHttpRequestException("missing changeSet");
                }

                var changeSet = _dataContractCache.GetSerializer(typeof(IEnumerable<ChangeSetEntry>))
                    .ReadObject(reader, verifyObjectName: false);

                reader.ReadEndElement();
                return new object?[] { changeSet };
            }
        }

        public override async Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation)
        {
            var ct = context.RequestAborted;
            if (ct.IsCancellationRequested)
                return;

            var messageWriter = BinaryMessageWriter.Rent(IsBinary);
            try
            {
                WriteFault(fault, messageWriter.XmlWriter);

                using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                messageWriter = null;

                var response = context.Response;
                response.Headers.ContentType = ContentType;
                response.ContentLength = bufferMemory.Length;

                await bufferMemory.WriteTo(response, ct);
            }
            catch (Exception)
            {
                messageWriter?.Clear();
                throw;
            }
        }

        /// <summary>
        /// Writes a SOAP-style Fault XML envelope for the specified DomainServiceFault into the provided XmlDictionaryWriter.
        /// </summary>
        /// <param name="fault">The DomainServiceFault containing the error message and detail to include in the fault payload.</param>
        /// <param name="writer">The XmlDictionaryWriter used to write the Fault elements and serialized fault detail.</param>
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


        /// <summary>
        /// Writes the given operation result into the HTTP response body
        /// </summary>
        /// <param name="context">The HTTP context whose response will be written to.</param>
        /// <param name="result">The operation result to serialize into the response body; may be null.</param>
        /// <param name="operation">The operation descriptor for which the response is being written.</param>
        /// <returns>A task that completes when the response has been written to the HTTP response.</returns>
        /// <remarks>If the request is already canceled, no output is written. The response's Content-Type and Content-Length are set by this method.</remarks>
        public override async Task WriteResponseAsync(HttpContext context, object? result, DomainOperationEntry operation)
        {
            var ct = context.RequestAborted;
            if (ct.IsCancellationRequested)
                return;

            var messageWriter = BinaryMessageWriter.Rent(IsBinary);
            try
            {
                WriteResponse(messageWriter.XmlWriter, result);

                using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                messageWriter = null;

                var response = context.Response;
                response.Headers.ContentType = ContentType;
                response.ContentLength = bufferMemory.Length;
                await bufferMemory.WriteTo(response, ct);
            }
            catch (Exception)
            {
                messageWriter?.Clear();
                throw;
            }
        }

        /// <summary>
        /// Writes the submit operation response for the given change set to the HTTP response.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="result">The change set entries to include in the submit response.</param>
        public override Task WriteSubmitResponseAsync(HttpContext context, IEnumerable<ChangeSetEntry> result)
        {
            return WriteResponseAsync(context, result, _operation);
        }

        /// <summary>
        /// Writes the XML response envelope for the current operation and serializes the provided result into the response body.
        /// </summary>
        private void WriteResponse(XmlDictionaryWriter writer, object? result)
        {
            // <GetQueryableRangeTaskResponse xmlns="http://tempuri.org/">
            writer.WriteStartElement(_responseName, "http://tempuri.org/");
            // <GetQueryableRangeTaskResult xmlns:a="DomainServices" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
            writer.WriteStartElement(_resultName);

            _responseSerializer.WriteObjectContent(writer, result);

            writer.WriteEndElement(); // ***Result
            writer.WriteEndElement(); // ***Response
        }

        /// <summary>
        /// Determines whether the specified media type matches the expected media type, ignoring any parameters.
        /// </summary>
        /// <param name="value">The media type to compare, represented as a read-only span of characters. This value may include parameters, which
        /// will be ignored in the comparison.</param>
        /// <param name="expected">The expected media type to match against. This string is compared in a case-insensitive manner.</param>
        /// <returns>true if the media type matches the expected media type; otherwise, false.</returns>
        protected static bool MatchesMediaType(ReadOnlySpan<char> value, ReadOnlySpan<char> expected)
        {
            int separator = value.IndexOf(';');
            if (separator >= 0)
                value = value[..separator];

            return value.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
