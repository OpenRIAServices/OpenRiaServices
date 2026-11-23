using System.Collections.Generic;

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
}
