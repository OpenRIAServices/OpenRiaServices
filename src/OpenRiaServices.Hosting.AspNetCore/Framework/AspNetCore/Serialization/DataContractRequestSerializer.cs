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
        /// <summary>
        /// Initializes a serializer configured for text/XML data contract requests for the specified domain operation.
        /// </summary>
        /// <param name="operation">The domain operation metadata this serializer will handle.</param>
        /// <param name="dataContractCache">Cache used to obtain data contract serializers for parameter and return types.</param>
        public TextXmlDataContractRequestSerializer(DomainOperationEntry operation, DataContractCache dataContractCache)
            : base(operation, dataContractCache, isBinary: false)
        {
        }

        /// <summary>
            /// Indicates whether this serializer accepts the specified content type for reading.
            /// </summary>
            /// <param name="contentType">The content-type value to test.</param>
            /// <returns>`true` if the content type starts with the text/XML MIME type, `false` otherwise.</returns>
            public override bool CanRead(ReadOnlySpan<char> contentType)
            => MemoryExtensions.StartsWith(contentType, MimeTypes.TextXml, StringComparison.Ordinal);

        /// <summary>
            /// Determines whether this serializer supports writing the given content type.
            /// </summary>
            /// <param name="contentType">The content type value (typically the request's Content-Type header) to test.</param>
            /// <returns>`true` if <paramref name="contentType"/> starts with the text/XML MIME type; `false` otherwise.</returns>
            public override bool CanWrite(ReadOnlySpan<char> contentType)
            => MemoryExtensions.StartsWith(contentType, MimeTypes.TextXml, StringComparison.Ordinal);
    }

    internal sealed class BinaryXmlDataContractRequestSerializer : DataContractRequestSerializer
    {
        /// <summary>
        /// Initializes a serializer specialized for binary XML requests and responses for the specified domain operation.
        /// </summary>
        /// <param name="operation">The domain operation metadata this serializer will handle.</param>
        /// <param name="dataContractCache">The cache used to obtain data contract serializers for parameter and return types.</param>
        public BinaryXmlDataContractRequestSerializer(DomainOperationEntry operation, DataContractCache dataContractCache)
            : base(operation, dataContractCache, isBinary: true)
        {
        }

        /// <summary>
            /// Checks whether the given content type exactly matches the binary XML MIME type.
            /// </summary>
            /// <param name="contentType">The content-type span to test.</param>
            /// <returns>`true` if <paramref name="contentType"/> equals <c>MimeTypes.BinaryXml</c>, `false` otherwise.</returns>
            public override bool CanRead(ReadOnlySpan<char> contentType)
            => MemoryExtensions.Equals(contentType, MimeTypes.BinaryXml, StringComparison.Ordinal);

        /// <summary>
            /// Determines whether this serializer supports writing for the specified content type.
            /// </summary>
            /// <param name="contentType">The content type to check.</param>
            /// <returns><c>true</c> if <paramref name="contentType"/> exactly equals <see cref="MimeTypes.BinaryXml"/>; <c>false</c> otherwise.</returns>
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

        /// <summary>
        /// Initializes a new DataContractRequestSerializer configured for the specified domain operation and data contract cache.
        /// </summary>
        /// <param name="operation">The domain operation metadata that this serializer will handle.</param>
        /// <param name="dataContractCache">The cache used to obtain data contract serializers for request/response types.</param>
        /// <param name="isBinary">If true, configures the serializer to use binary XML; otherwise uses text/XML.</param>
        /// <exception cref="NotSupportedException">Thrown when the operation type is not Query, Invoke, or a Custom Submit.</exception>
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

        /// <summary>
        /// Reads and parses the HTTP request body into an optional ServiceQuery and the operation's parameter values.
        /// </summary>
        /// <param name="operation">The domain operation description whose parameter names and types are used to deserialize the body.</param>
        /// <returns>
        /// A tuple containing the parsed <see cref="ServiceQuery"/> (or null if none was present) and an array of deserialized parameter values in operation parameter order.
        /// </returns>
        /// <exception cref="BadHttpRequestException">Thrown when the request's Content-Length is invalid.</exception>
        public override async Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation)
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
                using var reader = BinaryMessageReader.Rent(memory, IsBinary);
                return ReadQueryParametersFromBody(reader.XmlDictionaryReader, operation);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(memory.Array!);
            }
        }

        /// <summary>
        /// Reads a SubmitChanges request from the HTTP request body and returns the deserialized change set.
        /// </summary>
        /// <returns>The deserialized sequence of <see cref="ChangeSetEntry"/> contained in the request.</returns>
        public override async Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequest(HttpContext context)
        {
            (_, var parameter) = await ReadParametersFromBodyAsync(context, _operation);
            return (IEnumerable<ChangeSetEntry>)parameter[0]!;
        }

        /// <summary>
        /// Parses an optional service query from a message root and deserializes the operation parameters from the provided XML reader.
        /// </summary>
        /// <param name="reader">An <see cref="XmlDictionaryReader"/> positioned at the start of the request XML content.</param>
        /// <param name="operation">The domain operation metadata that describes the expected parameter elements and types.</param>
        /// <returns>A tuple where the first item is the parsed <see cref="ServiceQuery"/> (or <c>null</c> if none was present) and the second item is an array of parameter values in operation parameter order.</returns>
        /// <exception cref="BadHttpRequestException">Thrown when the XML contains unexpected trailing content or otherwise invalid message structure.</exception>
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
        /// <summary>
        /// Parses consecutive <c>QueryOption</c> elements from the current reader position and constructs a <see cref="ServiceQuery"/>.
        /// </summary>
        /// <param name="reader">An <see cref="XmlReader"/> positioned at the first <c>QueryOption</c> element to read.</param>
        /// <returns>A <see cref="ServiceQuery"/> whose <see cref="ServiceQuery.QueryParts"/> are built from parsed options and whose <see cref="ServiceQuery.IncludeTotalCount"/> is set to <c>true</c> if an option named <c>includeTotalCount</c> is present and parses to <c>true</c>.</returns>
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

        /// <summary>
        /// Advances the <see cref="XmlReader"/> past the current element, consuming its start and end tags and any inner content.
        /// </summary>
        /// <param name="reader">An <see cref="XmlReader"/> positioned on the element to skip; on return the reader is positioned after that element.</param>
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

        /// <summary>
        /// Parses and returns the operation parameter values from the XML reader positioned at the operation element.
        /// </summary>
        /// <param name="reader">An XmlDictionaryReader positioned at the start of the operation element (or at the SubmitChanges element for submit requests).</param>
        /// <param name="operation">The DomainOperationEntry describing the operation and its expected parameters.</param>
        /// <returns>
        /// An object array containing parameter values in the same order as <see cref="DomainOperationEntry.Parameters"/>.
        /// For Submit requests this is a single-element array containing the deserialized changeSet. Returns an empty array when the operation declares no parameters.
        /// </returns>
        /// <exception cref="BadHttpRequestException">Thrown when the XML content is malformed or required elements (e.g., changeSet) are missing.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the reader is not positioned at the expected operation element while parameters are required.</exception>
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

                    if (reader.HasAttributes && reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true")
                    {
                        values[i] = null;
                        ReadElement(reader); // consume element
                    }
                    else
                    {
                        var serializer = _dataContractCache.GetSerializer(parameter.ParameterType);

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

        /// <summary>
        /// Writes a fault envelope for the given DomainServiceFault into the HTTP response and sets appropriate response headers.
        /// </summary>
        /// <param name="context">The current HTTP context whose response will be written.</param>
        /// <param name="fault">The domain service fault to serialize into the response body.</param>
        /// <param name="operation">The domain operation associated with the request.</param>
        /// <returns>A Task that completes when the fault response has been written to the HTTP response.</returns>
        public override Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation)
        {
            var ct = context.RequestAborted;
            if (ct.IsCancellationRequested)
                return Task.CompletedTask;

            var messageWriter = BinaryMessageWriter.Rent(IsBinary);
            try
            {
                WriteFault(fault, messageWriter.XmlWriter);

                using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                messageWriter = null;

                var response = context.Response;
                response.Headers.ContentType = ContentType;
                response.ContentLength = bufferMemory.Length;

                return bufferMemory.WriteTo(response, ct);
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
        /// Writes the given operation result into the HTTP response body using the serializer's XML or binary XML format.
        /// </summary>
        /// <param name="context">The HTTP context whose response will be written to.</param>
        /// <param name="result">The operation result to serialize into the response body; may be null.</param>
        /// <param name="operation">The operation descriptor for which the response is being written.</param>
        /// <returns>A task that completes when the response has been written to the HTTP response.</returns>
        /// <remarks>If the request is already canceled, no output is written. The response's Content-Type and Content-Length are set by this method.</remarks>
        public override Task WriteResponseAsync(HttpContext context, object? result, DomainOperationEntry operation)
        {
            var ct = context.RequestAborted;
            if (ct.IsCancellationRequested)
                return Task.CompletedTask;

            var messageWriter = BinaryMessageWriter.Rent(IsBinary);
            try
            {
                WriteResponse(messageWriter.XmlWriter, result);

                using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                messageWriter = null;

                var response = context.Response;
                response.Headers.ContentType = ContentType;
                response.ContentLength = bufferMemory.Length;
                return bufferMemory.WriteTo(response, ct);
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
        /// <returns>A task that completes when the response has been written to the HTTP response.</returns>
        public override Task WriteSubmitResponse(HttpContext context, IEnumerable<ChangeSetEntry> result)
        {
            return WriteResponseAsync(context, result, _operation);
        }

        /// <summary>
        /// Writes the actual response of a method
        /// <summary>
        /// Writes the XML response envelope for the current operation and serializes the provided result into the response body.
        /// </summary>
        /// <param name="writer">The XmlDictionaryWriter used to emit the response elements.</param>
        /// <param name="result">The operation result to serialize as the response payload; may be null.</param>
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
    }
}