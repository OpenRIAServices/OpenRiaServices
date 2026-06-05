using System;
using System.Collections.Generic;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.DomainClients.MessagePack
{
    sealed class MessagePackMethodParameters
    {
        public MessagePackMethodParameters(MethodParameters methodParameters, IDictionary<string, object> values)
        {
            MethodParameters = methodParameters ?? throw new ArgumentNullException(nameof(methodParameters));
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public IDictionary<string, object> Values { get; }

        public MethodParameters MethodParameters { get; }
    }
}
