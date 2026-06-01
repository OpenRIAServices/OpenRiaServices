using System;
using System.Collections.Generic;

namespace OpenRiaServices.Client.DomainClients.Http.MessagePack
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
