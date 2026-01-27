using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

#nullable disable

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal interface ISerializationProvider
    {
        public bool CanRead(string contentType);

        public bool CanWrite(string contentType);

        public RequestSerializer GetRequestSerializer(DomainServiceDescription domainServiceDescription, DomainOperationEntry domainOperationEntry);
    }

    internal abstract class RequestSerializer
    {
        // AspNetMVC also has HttpContext, object and actual contentType selected as part of API
        // TODO: Consider content type to write API ?

        public abstract bool CanRead(ReadOnlySpan<char> contentType);

        public abstract bool CanWrite(ReadOnlySpan<char> contentType);

        public abstract Task<(ServiceQuery, object[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation);

        public abstract Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequest(HttpContext context);
        public abstract Task WriteSubmitResponse(HttpContext context, IEnumerable<ChangeSetEntry> result);

        public abstract Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation);

        public abstract Task WriteResponseAsync(HttpContext context, object result, DomainOperationEntry operation);
    }

}
