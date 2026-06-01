using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Nerdbank.MessagePack;
using OpenRiaServices.Server;
using PolyType;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack
{
    [GenerateShape]
    [DataContract]
    internal partial class MessagePackResponseEnvelopeBase
    {
        [DataMember]
        public DomainServiceFault? Fault { get; set; }
    }

    [DataContract]
    //[GenerateShape<]
    internal sealed partial class MessagePackQueryResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public MessagePackQueryResponseEnvelope() { }

        public MessagePackQueryResponseEnvelope(QueryResult<TResult> result)
            => Result = result;

        [DataMember]
        [Include]
        [PropertyShape()]
        public QueryResult<TResult>? Result { get; set; }
    }

    [DataContract]
    internal sealed class MessagePackInvokeResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public MessagePackInvokeResponseEnvelope(TResult result)
            => Result = result;

        [DataMember]
        public TResult? Result { get; set; }
    }

    [DataContract]
    //[GenerateShape]
    internal sealed partial class MessagePackSubmitResponseEnvelope : MessagePackResponseEnvelopeBase
    {
        public MessagePackSubmitResponseEnvelope() { }
        public MessagePackSubmitResponseEnvelope(IEnumerable<ChangeSetEntry> result)
        {
            Result = result;
        }

        [DataMember]
        public IEnumerable<ChangeSetEntry>? Result { get; set; }
    }


    internal abstract class MessagePackRequestEnvelopeBase
    {
        [MessagePackConverter(typeof(MessagePack.Converters.MethodParametersConverter))]
        public MethodParameters? Parameters { get; set; } = new();
    }

    [DataContract]
    //[GenerateShape]
    internal sealed partial class MessagePackQueryRequestEnvelope : MessagePackRequestEnvelopeBase
    {
        [DataMember]
        public List<ServiceQueryPart>? QueryOptions { get; set; }
        [DataMember]
        public bool IncludeTotalCount { get; set; }
    }

    [DataContract]
    //[GenerateShape]
    internal sealed partial class MessagePackInvokeRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    [DataContract]
    //[GenerateShape]
    internal sealed partial class MessagePackSubmitRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    
    internal sealed class MethodParameters
    {
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.Ordinal);
    }
}
