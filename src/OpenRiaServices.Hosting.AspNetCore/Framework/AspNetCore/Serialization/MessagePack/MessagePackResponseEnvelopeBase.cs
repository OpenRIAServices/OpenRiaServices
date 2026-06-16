using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Nerdbank.MessagePack;
using OpenRiaServices.Server;
using PolyType;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack
{
    [DataContract]
    [GenerateShape]
    partial class MessagePackFaultResponse
    {
        [DataMember]
        public DomainServiceFault? Fault { get; set; }
    }

    class MessagePackResponseEnvelopeBase
    {
    }

    sealed partial class MessagePackQueryResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public MessagePackQueryResponseEnvelope(QueryResult<TResult> result)
            => Result = result;

        public QueryResult<TResult>? Result { get; set; }
    }

    sealed class MessagePackInvokeResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public MessagePackInvokeResponseEnvelope(TResult result)
            => Result = result;

        public TResult? Result { get; set; }
    }

    sealed partial class MessagePackSubmitResponseEnvelope : MessagePackResponseEnvelopeBase
    {
        public MessagePackSubmitResponseEnvelope(IEnumerable<ChangeSetEntry> result)
        {
            Result = result;
        }

        public IEnumerable<ChangeSetEntry>? Result { get; set; }
    }


    class MessagePackRequestEnvelope
    {
        [MessagePackConverter(typeof(Converters.MethodParametersConverter))]
        public MethodParameters? Parameters { get; set; } = new();
    }

    sealed class MessagePackQueryRequestEnvelope : MessagePackRequestEnvelope
    {
        public List<ServiceQueryPart>? QueryOptions { get; set; }
        public bool IncludeTotalCount { get; set; }
    }

    /// <summary>
    /// Helper class to allow custom (de)serialization of method parameters.
    /// <see cref="Converters.MethodParametersConverter"/> contains the important logic
    /// </summary>
    internal sealed class MethodParameters
    {
        // TODO: Look into how this affect security
        [UseComparer(typeof(StringComparer), nameof(StringComparer.Ordinal))]
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.Ordinal);
    }
}
