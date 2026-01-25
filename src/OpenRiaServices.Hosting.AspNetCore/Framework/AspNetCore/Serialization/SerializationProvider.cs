using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

#nullable disable

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal abstract class SerializationProvider
    {
        public abstract RequestSerializer GetRequestSerializer(DomainServiceDescription domainServiceDescription, DomainOperationEntry domainOperationEntry);
    }

    internal abstract class RequestSerializer
    {
        public abstract Task<(ServiceQuery, object[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation);

        public abstract Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequest(HttpContext context);
        public abstract Task WriteSubmitResponse(HttpContext context, IEnumerable<ChangeSetEntry> result);

        public abstract Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation);

        public abstract Task WriteResponseAsync(HttpContext context, object result, DomainOperationEntry operation);
    }

}
