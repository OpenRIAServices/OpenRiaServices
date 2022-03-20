using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using OpenRiaServices.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

abstract class OperationInvoker
{
    private static readonly WebHttpQueryStringConverter s_queryStringConverter = new();
    protected readonly DomainOperationEntry operation;
    private readonly DomainOperationType operationType;
    protected readonly SerializationHelper serializationHelper;
    private readonly DataContractSerializer responseSerializer;
    private readonly string _responseName;
    private readonly string _resultName;
    private const string MessageRootElementName = "MessageRoot";
    private const string QueryOptionsListElementName = "QueryOptions";
    private const string QueryOptionElementName = "QueryOption";
    private const string QueryNameAttribute = "Name";
    private const string QueryValueAttribute = "Value";
    private const string QueryIncludeTotalCountOption = "includeTotalCount";

    private static readonly DataContractSerializer s_faultSerialiser = new DataContractSerializer(typeof(DomainServiceFault));

    public OperationInvoker(DomainOperationEntry operation, DomainOperationType operationType,
        SerializationHelper serializationHelper,
        DataContractSerializer responseSerializer)
    {
        this.operation = operation;
        this.operationType = operationType;
        this.serializationHelper = serializationHelper;
        this.responseSerializer = responseSerializer;

        this._responseName = this.Name + "Response";
        this._resultName = this.Name + "Result";
    }

    public virtual string Name => operation.Name;

    public abstract Task Invoke(HttpContext context);

    protected object[] GetParametersFromUri(HttpContext context)
    {
        var query = context.Request.Query;
        var parameters = operation.Parameters;
        var inputs = new object[parameters.Count];
        for (int i = 0; i < parameters.Count; ++i)
        {
            if (query.TryGetValue(parameters[i].Name, out var values))
            {
                var value = values.FirstOrDefault();
                inputs[i] = s_queryStringConverter.ConvertStringToValue(value, parameters[i].ParameterType);
            }
        }

        return inputs;
    }

    protected async Task<(ServiceQuery, object[])> ReadParametersFromBodyAsync(HttpContext context)
    {
        // TODO: determine if settings for max length etc (timings) for DOS protection is needed (or if it should be set on kestrel etc)
        // TODO: Use arraypool or similar instead ?
        var contentLength = context.Request.ContentLength;
        if (!contentLength.HasValue || contentLength < 0 || contentLength > int.MaxValue)
            throw new InvalidOperationException();

        int length = (int)contentLength;
        using var ms = new PooledStream.PooledMemoryStream();
        ms.Reserve(length);

        await context.Request.Body.CopyToAsync(ms);
        // Verify all was read
        if (ms.Length != length)
            throw new InvalidOperationException();

        ms.Seek(0, SeekOrigin.Begin);
        using var reader = XmlDictionaryReader.CreateBinaryReader(ms, null, System.Xml.XmlDictionaryReaderQuotas.Max);
        return ReadParametersFromBody(reader);
    }

    private (ServiceQuery, object[]) ReadParametersFromBody(System.Xml.XmlDictionaryReader reader)
    {
        ServiceQuery serviceQuery = null;
        object[] values;
        reader.MoveToContent();

        bool hasMessageRoot = reader.IsStartElement("MessageRoot");
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
        if (reader.ReadState != System.Xml.ReadState.EndOfFile)
            throw new InvalidOperationException();
        return (serviceQuery, values);
    }

    /// <summary>
    /// Reads the query options from the given reader and returns the resulting service query.
    /// It assumes that the reader is positioned on a stream containing the query options.
    /// </summary>
    /// <param name="reader">Reader to the stream containing the query options.</param>
    /// <returns>Extracted service query.</returns>
    internal static ServiceQuery ReadServiceQuery(System.Xml.XmlReader reader)
    {
        List<ServiceQueryPart> serviceQueryParts = new List<ServiceQueryPart>();
        bool includeTotalCount = false;
        while (reader.IsStartElement(QueryOptionElementName))
        {
            string name = reader.GetAttribute(QueryNameAttribute);
            string value = reader.GetAttribute(QueryValueAttribute);
            if (name.Equals(QueryIncludeTotalCountOption, StringComparison.OrdinalIgnoreCase))
            {
                bool queryOptionValue = false;
                if (Boolean.TryParse(value, out queryOptionValue))
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

        ServiceQuery serviceQuery = new ServiceQuery()
        {
            QueryParts = serviceQueryParts,
            IncludeTotalCount = includeTotalCount
        };
        return serviceQuery;
    }

    protected static void ReadElement(System.Xml.XmlReader reader)
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

    protected virtual object[] ReadParameters(System.Xml.XmlDictionaryReader reader)
    {
        if (reader.IsStartElement(operation.Name))
        {
            reader.Read();

            var parameters = operation.Parameters;
            object[] values = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; ++i)
            {
                var parameter = parameters[i];
                if (!reader.IsStartElement(parameter.Name))
                    throw new InvalidOperationException();

                if (reader.NodeType == System.Xml.XmlNodeType.EndElement 
                    || reader.IsEmptyElement
                    || (reader.HasAttributes && reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance") == "true"))
                {
                    values[i] = null;
                }
                else
                {
                    // TODO: consider knowtypes ?
                    var serializer = serializationHelper.GetSerializer(parameter.ParameterType);
                    values[i] = serializer.ReadObject(reader, verifyObjectName: false);
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
    }


    /// <summary>
    /// Verifies the reader is at node with LocalName equal to operationName + postfix.
    /// If the reader is at any other node, then a <see cref="DomainOperationException"/> is thrown
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="postfix">The postfix.</param>
    /// <exception cref="DomainOperationException">If reader is not at the expected xml element</exception>
    protected static void VerifyReaderIsAtNode(System.Xml.XmlDictionaryReader reader, string operationName, string postfix)
    {
        // localName should be operationName + postfix
        if (!(reader.LocalName.Length == operationName.Length + postfix.Length
            && reader.LocalName.StartsWith(operationName, StringComparison.Ordinal)
            && reader.LocalName.EndsWith(postfix, StringComparison.Ordinal)))
        {
            // TODO:
            throw new InvalidOperationException();
        }
    }

    protected Task WriteError(HttpContext context, IEnumerable<ValidationResult> validationErrors, bool hideStackTrace)
    {
        hideStackTrace = false;
        var errors = validationErrors.Select(ve => new ValidationResultInfo(ve.ErrorMessage, ve.MemberNames)).ToList();

        // if custom errors is turned on, clear out the stacktrace.
        foreach (ValidationResultInfo error in errors)
        {
            if (hideStackTrace)
            {
                error.StackTrace = null;
            }
        }

        return WriteError(context, new DomainServiceFault { OperationErrors = errors, ErrorCode = 422 });
    }


    /// <summary>
    /// Transforms the specified exception as appropriate into a fault message that can be sent
    /// back to the client.
    /// </summary>
    /// <param name="ex">The exception that was caught.</param>
    /// <param name="hideStackTrace">same as <see cref="HttpContext.IsCustomErrorEnabled"/> <c>true</c> means dont send stack traces</param>
    /// <returns>The exception to return.</returns>
    protected Task WriteError(HttpContext context, Exception ex, bool hideStackTrace)
    {
        var fault = ServiceUtility.CreateFaultException(ex, hideStackTrace);
        return WriteError(context, fault);
    }

    protected async Task WriteError(HttpContext context, DomainServiceFault fault)
    {
        var ct = context.RequestAborted;
        ct.ThrowIfCancellationRequested();

        using var ms = new PooledStream.PooledMemoryStream();
        using (var writer = System.Xml.XmlDictionaryWriter.CreateBinaryWriter(ms, null, null, ownsStream: false))
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

        var response = context.Response;
        response.Headers.ContentType = "application/msbin1";
        response.StatusCode = /*fault.ErrorCode*/ 500;
        response.ContentLength = ms.Length;
        await response.Body.WriteAsync(ms.ToMemoryUnsafe());
    }

    protected async Task WriteResponse(HttpContext context, object result)
    {
        var ct = context.RequestAborted;
        ct.ThrowIfCancellationRequested();

        // TODO: Allow setting XmlDictionaryWriter quotas for Read/write
        // TODO: Port BufferManagerStream and related code
        using var ms = new PooledStream.PooledMemoryStream();
        using (var writer = System.Xml.XmlDictionaryWriter.CreateBinaryWriter(ms, null, null, ownsStream: false))
        {
            string operationName = this.Name;
            // <GetQueryableRangeTaskResponse xmlns="http://tempuri.org/">
            writer.WriteStartElement(_responseName, "http://tempuri.org/");
            // <GetQueryableRangeTaskResult xmlns:a="DomainServices" xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
            writer.WriteStartElement(_resultName);
            //writer.WriteXmlnsAttribute("a", "DomainServices");
            //writer.WriteXmlnsAttribute("i", "http://www.w3.org/2001/XMLSchema-instance");

            // TODO: XmlElemtnt  support
            //// XmlElemtnt returns the "ResultNode" unless we step into the contents
            //if (returnType == typeof(System.Xml.Linq.XElement))
            //    reader.ReadStartElement();a

            this.responseSerializer.WriteObjectContent(writer, result);

            writer.WriteEndElement(); // ***Result
            writer.WriteEndElement(); // ***Response

            //      writer.WriteEndDocument();
            writer.Flush();
            ms.Flush();
        }

        var response = context.Response;
        response.Headers.ContentType = "application/msbin1";
        response.StatusCode = 200;
        response.ContentLength = ms.Length;
        await response.Body.WriteAsync(ms.ToMemoryUnsafe());
    }

    protected DomainService CreateDomainService(HttpContext context)
    {
        var domainService = (DomainService)context.RequestServices.GetRequiredService(operation.DomainServiceType);
        var serviceContext = new AspNetDomainServiceContext(context, this.operationType);
        domainService.Initialize(serviceContext);
        return domainService;
    }
}
