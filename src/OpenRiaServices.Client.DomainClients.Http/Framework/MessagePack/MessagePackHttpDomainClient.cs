using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.Http;
using PolyType;
using PolyType.ReflectionProvider;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.MessagePack
{
    /// <summary>
    /// <see cref="DomainClient"/> implementation which uses MessagePack over HTTP.
    /// </summary>
    sealed class MessagePackHttpDomainClient : HttpDomainClient
    {
        internal const string MediaType = "application/vnd.msgpack";
        private static readonly HttpMethod s_queryMethod = new("QUERY");
        private readonly MessagePackSerializer _serializer;
        private readonly ITypeShapeProvider _typeShapeProvider;

        public MessagePackHttpDomainClient(HttpClient httpClient, Type serviceInterface, MessagePackHttpDomainClientFactory factory)
            : base(httpClient, serviceInterface, factory)
        {
            _serializer = factory.BaseSerializerSerializer;
            _typeShapeProvider = factory.TypeShapeProver;
        }

        private protected override Task<HttpResponseMessage> PostAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
            => SendWithBodyAsync(HttpMethod.Post, operationName, parameters, queryOptions, cancellationToken);

        private protected override Task<HttpResponseMessage> QueryAsync(string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
            => SendWithBodyAsync(s_queryMethod, operationName, parameters, queryOptions, cancellationToken);

        private async Task<HttpResponseMessage> SendWithBodyAsync(HttpMethod method, string operationName, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, operationName);
            MethodParameters methodParameters = GetMethodParameters(operationName);
            var envelope = CreateRequestEnvelope(method, operationName, methodParameters, parameters, queryOptions);


            MessagePackSerializer operationSerializer = envelope is MessagePackSubmitRequestEnvelope ? GetSubmitSerializer(methodParameters)
                : CreateOperationSerializer(_serializer, methodParameters);

            using var stream = new MemoryStream();
            // TODO: If possible, replace this buffering with a custom HttpContent override of SerializeToStream
            // so the request payload can be serialized asynchronously directly to the outgoing request stream.
            await operationSerializer.SerializeObjectAsync(stream, envelope, _typeShapeProvider.GetTypeShapeOrThrow(envelope.GetType()), cancellationToken).ConfigureAwait(false);

            var bytes = stream.ToArray();
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaType);

            return await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        }

        private MessagePackSerializer GetSubmitSerializer(MethodParameters methodParameters)
        {

            DerivedShapeMapping<Entity> _entityTypes = new DerivedShapeMapping<Entity>();
            AddDerivedTypes(_entityTypes, EntityTypes, t => t.FullName);

            return CreateOperationSerializer(_serializer, methodParameters)
                with { DerivedTypeUnions = [_entityTypes, .. _serializer.DerivedTypeUnions] };

            void AddDerivedTypes(object mapping, IEnumerable<Type> derivedTypes, Func<Type, string> discriminator)
            {
                MethodInfo? addMethodDefinition = mapping.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.Name == "Add" && m.IsGenericMethodDefinition)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 2
                            && parameters[0].ParameterType == typeof(DerivedTypeIdentifier)
                            && parameters[1].ParameterType == typeof(PolyType.ITypeShapeProvider);
                    });

                if (addMethodDefinition == null)
                {
                    throw new InvalidOperationException($"Unable to locate DerivedShapeMapping.Add<TDerived>(..., ...) on {mapping.GetType().FullName}.");
                }


                foreach (Type derivedType in derivedTypes)
                {
                    MethodInfo addMethod = addMethodDefinition.MakeGenericMethod(derivedType);
                    DerivedTypeIdentifier unionIdentifier = new DerivedTypeIdentifier(discriminator(derivedType));
                    addMethod.Invoke(mapping, new object[] { unionIdentifier, _typeShapeProvider });
                }
            }
        }

        private MessagePackRequestEnvelopeBase CreateRequestEnvelope(HttpMethod method, string operationName, MethodParameters methodParameters, IDictionary<string, object> parameters, List<ServiceQueryPart> queryOptions)
        {
            MessagePackMethodParameters requestParameters = (parameters is { Count: > 0 })
                ? new(methodParameters, parameters) : null;

            if (queryOptions is not null && queryOptions.Count > 0)
            {
                var request = new MessagePackQueryRequestEnvelope()
                {
                    QueryOptions = new List<ServiceQueryPart>(queryOptions.Count),
                    Parameters = requestParameters
                };
                foreach (var queryOption in queryOptions)
                {
                    if (string.Equals(queryOption.QueryOperator, "includeTotalCount", StringComparison.OrdinalIgnoreCase)
                        && bool.TryParse(queryOption.Expression, out bool includeTotalCount))
                    {
                        request.IncludeTotalCount = includeTotalCount;
                    }
                    else
                    {
                        request.QueryOptions.Add(queryOption);
                    }
                }
                return request;
            }



            return string.Equals(operationName, "SubmitChanges", StringComparison.Ordinal)
                ? new MessagePackSubmitRequestEnvelope() { Parameters = requestParameters }
                : new MessagePackInvokeRequestEnvelope() { Parameters = requestParameters };
        }

        private protected override async Task<object> ReadResponseAsync(HttpResponseMessage response, string operationName, Type returnType)
        {
            using (response)
            {
                if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType != MediaType)
                {
                    var message = string.Format(CultureInfo.InvariantCulture, Resources.DomainClient_UnexpectedHttpStatusCode, (int)response.StatusCode, response.StatusCode);

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        throw new DomainOperationException(message, OperationErrorStatus.NotSupported, (int)response.StatusCode, null);
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new DomainOperationException(message, OperationErrorStatus.Unauthorized, (int)response.StatusCode, null);
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                        throw new DomainOperationException(message, OperationErrorStatus.NotFound, (int)response.StatusCode, null);
                    else
                        throw new DomainOperationException(message, OperationErrorStatus.ServerError, (int)response.StatusCode, null);
                }

                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                Type envelopeType = GetResponseEnvelopeType(operationName, returnType);
                var typeShape = _typeShapeProvider.GetTypeShapeOrThrow(envelopeType);

                var envelope = (MessagePackResponseEnvelopeBase)await _serializer.DeserializeObjectAsync(stream, typeShape).ConfigureAwait(false)
                    ?? (MessagePackResponseEnvelopeBase)Activator.CreateInstance(envelopeType);

                if (envelope.Fault is not null)
                {
                    throw new FaultException<DomainServiceFault>(
                        envelope.Fault,
                        new FaultReason(new[] { new FaultReasonText(envelope.Fault.ErrorMessage, CultureInfo.CurrentCulture.Name) }),
                        new FaultCode("Sender"),
                        operationName);
                }

                object result = envelope.GetResult();
                if (returnType == typeof(void) || result is null)
                    return null;

                return result;
            }
        }


        private static MessagePackSerializer CreateOperationSerializer(MessagePackSerializer serializer, MethodParameters methodParameters)
        {
            // TODO: Look at how this affect cahces. Keep different serializers ?
            SerializationContext context = serializer.StartingContext;
            context[MessagePackMethodParametersConverter.MethodParametersKey] = methodParameters;
            return serializer with { StartingContext = context };
        }

        private static Type GetResponseEnvelopeType(string operationName, Type returnType)
        {
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(QueryResult<>))
                return typeof(MessagePackQueryResponseEnvelope<>).MakeGenericType(returnType);
            if (string.Equals(operationName, "SubmitChanges", StringComparison.Ordinal))
                return typeof(MessagePackSubmitResponseEnvelope<>).MakeGenericType(returnType);

            return (returnType == typeof(void)) ? typeof(MessagePackInvokeResponseEnvelope<object>)
                : typeof(MessagePackInvokeResponseEnvelope<>).MakeGenericType(returnType);
        }

    }
}
