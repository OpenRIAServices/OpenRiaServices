using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    internal interface ISerializationProvider
    {
        /// <summary>
        /// Get <see cref="RequestSerializer "/> to be used for serialization (and deserialization) for the specified domain operation.
        /// </summary>
        /// <param name="operation">The domain operation which the serializer should be able to handle.</param>
        public RequestSerializer GetRequestSerializer(DomainOperationEntry operation);
    }

    internal abstract class RequestSerializer
    {
        /// <summary>
        /// Determines whether the serializer can read requests with the specified format.
        /// </summary>
        /// <param name="contentType">The content type (e.g., the request's Content-Type header value).</param>
        /// <returns><see langword="true"/> if the serializer can read requests with the given content type, otherwise <see langword="false"/>.</returns>
        public abstract bool CanRead(ReadOnlySpan<char> contentType);

        /// <summary>
        /// Determines whether the serializer can write requests with the specified format.
        /// </summary>
        /// <param name="contentType">The content type to use (e.g., based on request's Accept (or Content-Type) header value(s).</param>
        /// <returns><see langword="true"/> if the serializer can write requests with the given content type, otherwise <see langword="false"/>.</returns>
        public abstract bool CanWrite(ReadOnlySpan<char> contentType);

        /// <summary>
        /// Reads parameters for <paramref name="operation"/> from the provided <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext" /> containing the request to read.</param>
        /// <param name="operation">Metadata for the domain operation that guides parameter deserialization.</param>
        public abstract Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation);

        /// <summary>
        /// Parses a Submit request from <paramref name="context"/>
        /// </summary>
        /// <param name="context">The HTTP context containing the submit request to read.</param>
        /// <returns>An enumerable of <see cref="ChangeSetEntry"/> to create a <see cref="ChangeSet"/> from.</returns>
        public abstract Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequestAsync(HttpContext context);

        /// <summary>
        /// Writes the result of a Submit to the provided <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">The HTTP context to write the response to.</param>
        /// <param name="result">The change-set entries representing the results of the submit operation.</param>
        public abstract Task WriteSubmitResponseAsync(HttpContext context, IEnumerable<ChangeSetEntry> result);

        /// <summary>
        /// Writes an error response (<paramref name="fault"/>) to the provided <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">The HTTP context to write the error response to.</param>
        /// <param name="fault">The domain service fault information to serialize into the response.</param>
        /// <param name="operation">Metadata for the domain operation associated with the fault, used to shape the response.</param>
        public abstract Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation);

        /// <summary>
        /// Writes the return value of a successful operation to the provided <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">The HTTP context to which the response will be written.</param>
        /// <param name="result">The operation result to serialize into the response; may be null for no content.</param>
        /// <param name="operation">Metadata for the domain operation that guides serialization and response formatting.</param>
        public abstract Task WriteResponseAsync(HttpContext context, object? result, DomainOperationEntry operation);
    }

}
