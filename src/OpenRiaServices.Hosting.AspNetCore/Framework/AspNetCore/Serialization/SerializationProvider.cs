using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal interface ISerializationProvider
    {
        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation);
    }

    internal abstract class RequestSerializer
    {
        public abstract bool CanRead(ReadOnlySpan<char> contentType);

        public abstract bool CanWrite(ReadOnlySpan<char> contentType);

        public abstract Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation);

        public abstract Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequest(HttpContext context);
        public abstract Task WriteSubmitResponse(HttpContext context, IEnumerable<ChangeSetEntry> result);

        public abstract Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation);

        public abstract Task WriteResponseAsync(HttpContext context, object? result, DomainOperationEntry operation);
    }

}
