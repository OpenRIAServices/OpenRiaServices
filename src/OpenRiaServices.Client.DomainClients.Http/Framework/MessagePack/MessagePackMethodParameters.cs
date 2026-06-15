using System;
using System.Collections.Generic;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.DomainClients.MessagePack
{
    /// <summary>
    /// Dictionary wrapper to help with serializing method parameters
    /// </summary>
    sealed class MessagePackMethodParameters
    {
        public MessagePackMethodParameters(MethodParameters methodParameters, IDictionary<string, object> values)
        {
            MethodParameters = methodParameters ?? throw new ArgumentNullException(nameof(methodParameters));
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// Actual values to serialize
        /// </summary>
        public IDictionary<string, object> Values { get; }

        /// <summary>
        /// Method parameters, to help with serializing <see cref="Values"/>
        /// </summary>
        public MethodParameters MethodParameters { get; }
    }
}
