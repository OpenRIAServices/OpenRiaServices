using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack
{
    abstract class MessagePackRequestEnvelopeBase
    {
        public MessagePackMethodParameters? Parameters { get; set; }
    }

    sealed class MessagePackQueryRequestEnvelope : MessagePackRequestEnvelopeBase
    {
        public List<ServiceQueryPart>? QueryOptions { get; set; }
        public bool IncludeTotalCount { get; set; }
    }

    sealed class MessagePackInvokeRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    sealed class MessagePackSubmitRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    abstract class MessagePackResponseEnvelopeBase
    {
        public DomainServiceFault? Fault { get; set; }
        public abstract object? GetResult();
    }


    sealed class MessagePackQueryResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public TResult? Result { get; set; }
        public override object? GetResult() => Result;
    }

    sealed class MessagePackInvokeResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public TResult? Result { get; set; }
        public override object? GetResult() => Result;
    }

    sealed class MessagePackSubmitResponseEnvelope : MessagePackResponseEnvelopeBase
    {
        public IEnumerable<ChangeSetEntry?>? Result { get; set; }
        public override object? GetResult() => Result;
    }
}
