using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
    [GenerateShape]
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


    [DataContract]
    internal abstract class MessagePackRequestEnvelopeBase
    {
        [DataMember]
        public MethodParameters? Parameters { get; set; } = new();
    }

    [DataContract]
    internal sealed class MessagePackQueryRequestEnvelope : MessagePackRequestEnvelopeBase
    {
        [DataMember]
        public List<ServiceQueryPart>? QueryOptions { get; set; }
        [DataMember]
        public bool IncludeTotalCount { get; set; }
    }

    [DataContract]
    internal sealed class MessagePackInvokeRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    [DataContract]
    internal sealed class MessagePackSubmitRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    [DataContract]
    internal sealed class MethodParameters
    {
        [DataMember]
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.Ordinal);
    }
}
