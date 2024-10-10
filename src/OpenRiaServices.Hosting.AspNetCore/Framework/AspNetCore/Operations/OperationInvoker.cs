using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using OpenRiaServices.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Operations
{
    abstract class OperationInvoker
    {
        private static readonly WebHttpQueryStringConverter s_queryStringConverter = new();
        private static readonly DataContractSerializer s_faultSerialiser = new DataContractSerializer(typeof(DomainServiceFault));

        protected readonly DomainOperationEntry _operation;
        private readonly DomainOperationType _operationType;
        protected readonly SerializationHelper _serializationHelper;
        private readonly DataContractSerializer _responseSerializer;
        private readonly string _responseName;
        private readonly string _resultName;
        private const string MessageRootElementName = "MessageRoot";
        private const string QueryOptionsListElementName = "QueryOptions";
        private const string QueryOptionElementName = "QueryOption";
        private const string QueryNameAttribute = "Name";
        private const string QueryValueAttribute = "Value";
        private const string QueryIncludeTotalCountOption = "includeTotalCount";
        private const string InvalidContentMessage = "invalid content";

        protected OperationInvoker(DomainOperationEntry operation, DomainOperationType operationType,
            SerializationHelper serializationHelper,
            DataContractSerializer responseSerializer,
            OpenRiaServicesOptions options)
        {
            this._operation = operation;
            this._operationType = operationType;
            this._serializationHelper = serializationHelper;
            this._responseSerializer = responseSerializer;
            Options = options;
            _responseName = OperationName + "Response";
            _resultName = OperationName + "Result";
        }

        public virtual string OperationName => _operation.Name;
        public DomainOperationEntry DomainOperation => _operation;

        public abstract bool HasSideEffects { get; }
        public OpenRiaServicesOptions Options { get; }

        public abstract Task Invoke(HttpContext context);

        protected object[] GetParametersFromUri(HttpContext context)
        {
            var query = context.Request.Query;
            var parameters = _operation.Parameters;
            var inputs = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; ++i)
            {
                if (query.TryGetValue(parameters[i].Name, out var values))
                {
                    var value = Uri.UnescapeDataString(values.FirstOrDefault());
                    inputs[i] = s_queryStringConverter.ConvertStringToValue(value, parameters[i].ParameterType);
                }
            }

            return inputs;
        }

        protected async Task<(ServiceQuery, object[])> ReadParametersFromBodyAsync(HttpContext context)
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
                using var reader = BinaryMessageReader.Rent(memory);
                return ReadParametersFromBody(reader.XmlDictionaryReader);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(memory.Array);
            }
        }

        private (ServiceQuery, object[]) ReadParametersFromBody(XmlDictionaryReader reader)
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

            values = ReadParameters(reader);

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

        protected static void ReadElement(XmlReader reader)
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

        protected virtual object[] ReadParameters(XmlDictionaryReader reader)
        {
            if (reader.IsStartElement(_operation.Name))
            {
                reader.Read();

                var parameters = _operation.Parameters;
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
                if (_operation.Parameters.Count == 0)
                    return Array.Empty<object>();
                else
                    throw new InvalidOperationException();
            }
        }


        /// <summary>
        /// Verifies the reader is at node with LocalName equal to operationName + postfix.
        /// If the reader is at any other node, then a <see cref="DomainOperationException"/> is thrown
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="postfix">The postfix.</param>
        /// <exception cref="DomainOperationException">If reader is not at the expected xml element</exception>
        protected static void VerifyReaderIsAtNode(XmlDictionaryReader reader, string operationName, string postfix)
        {
            // localName should be operationName + postfix
            if (!(reader.LocalName.Length == operationName.Length + postfix.Length
                && reader.LocalName.StartsWith(operationName, StringComparison.Ordinal)
                && reader.LocalName.EndsWith(postfix, StringComparison.Ordinal)))
            {
                throw new BadHttpRequestException(InvalidContentMessage);
            }
        }

        protected Task WriteError(HttpContext context, IEnumerable<ValidationResult> validationErrors)
        {
            var errors = validationErrors.Select(ve => new ValidationResultInfo(ve.ErrorMessage, ve.MemberNames)).ToList();
            bool unsafeShowStackTrace = Options.UnsafeIncludeStackTraceInErrors;

            // if custom errors is turned on, clear out the stacktrace.
            foreach (ValidationResultInfo error in errors)
            {
                if (unsafeShowStackTrace == false)
                {
                    error.StackTrace = null;
                }
            }

            return WriteError(context, new DomainServiceFault { OperationErrors = errors, ErrorCode = StatusCodes.Status422UnprocessableEntity });
        }


        /// <summary>
        /// Transforms the specified exception as appropriate into a fault message that can be sent
        /// back to the client.
        /// </summary>
        /// <param name="ex">The exception that was caught.</param>
        /// <param name="hideStackTrace">same as <see cref="HttpContext.IsCustomErrorEnabled"/> <c>true</c> means dont send stack traces</param>
        /// <returns>The exception to return.</returns>
        protected Task WriteError(HttpContext context, Exception ex)
        {
            var fault = ServiceUtility.CreateFaultException(ex, Options);

            // TODO: Transform error based on options
            // ex = options.TransformError(ex) , or AspNetCoreDomainServiceFault =  options.OnError(context, ex)
            //if (Options.OnError is { } onError)
            //{
            //    onError(fault.ErrorCode, ex);
            //}

            return WriteError(context, fault);
        }

        protected Task WriteError(HttpContext context, DomainServiceFault fault)
        {
            var ct = context.RequestAborted;
            if (ct.IsCancellationRequested)
                return Task.CompletedTask;

            var messageWriter = BinaryMessageWriter.Rent();
            try
            {
                WriteFault(fault, messageWriter.XmlWriter);

                using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                messageWriter = null;

                var response = context.Response;

                response.Headers.ContentType = "application/msbin1";
                // We should be able to use fault.ErrorCode as long as it is not Bad request (400, which result in special WCF client throwing another exception) and not a domainOperation
                response.StatusCode = 500; //  fault.IsDomainException || fault.ErrorCode == 400 ? 500 : fault.ErrorCode;
                response.ContentLength = bufferMemory.Length;
                response.Headers.CacheControl = "private, no-store";

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

        protected Task WriteResponse(HttpContext context, object result)
        {
            var ct = context.RequestAborted;
            if (ct.IsCancellationRequested)
                return Task.CompletedTask;

            var messageWriter = BinaryMessageWriter.Rent();
            try
            {
                var writer = messageWriter.XmlWriter;

                WriteResponse(writer, result);

                using var bufferMemory = BinaryMessageWriter.Return(messageWriter);
                messageWriter = null;

                var response = context.Response;
                response.Headers.ContentType = "application/msbin1";
                response.StatusCode = 200;
                response.ContentLength = bufferMemory.Length;
                response.Headers.CacheControl = "private, no-store";

                return bufferMemory.WriteTo(response, ct);
            }
            catch (Exception)
            {
                messageWriter?.Clear();
                throw;
            }
        }

        private void WriteResponse(XmlDictionaryWriter writer, object result)
        {
            // <GetQueryableRangeTaskResponse xmlns="http://tempuri.org/">
            writer.WriteStartElement(_responseName, "http://tempuri.org/");
            // <GetQueryableRangeTaskResult xmlns:a="DomainServices" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
            writer.WriteStartElement(_resultName);
            //writer.WriteXmlnsAttribute("a", "DomainServices");
            //writer.WriteXmlnsAttribute("i", "http://www.w3.org/2001/XMLSchema-instance");

            _responseSerializer.WriteObjectContent(writer, result);

            writer.WriteEndElement(); // ***Result
            writer.WriteEndElement(); // ***Response
        }

        protected DomainService CreateDomainService(HttpContext context)
        {
            var domainService = (DomainService)context.RequestServices.GetRequiredService(_operation.DomainServiceType);
            var serviceContext = new AspNetDomainServiceContext(context, _operationType);
            domainService.Initialize(serviceContext);
            return domainService;
        }
    }
}
