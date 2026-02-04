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
/// Selects a RequestSerializer appropriate for the specified domain operation.
/// </summary>
/// <param name="operation">Metadata for the domain operation used to choose the serializer.</param>
/// <returns>A RequestSerializer suitable for handling requests and responses for the given operation.</returns>
public RequestSerializer GetRequestSerializer(DomainOperationEntry operation);
    }

    internal abstract class RequestSerializer
    {
        /// <summary>
/// Determines whether the serializer can read requests with the specified content type.
/// </summary>
/// <param name="contentType">The content type to test (e.g., the request's Content-Type header value).</param>
/// <returns>`true` if the serializer can read requests with the given content type, `false` otherwise.</returns>
public abstract bool CanRead(ReadOnlySpan<char> contentType);

        /// <summary>
/// Determines whether this serializer can write responses for the specified content type.
/// </summary>
/// <param name="contentType">The content type to check (for example, "application/json").</param>
/// <returns>`true` if this serializer can write responses with the given content type, `false` otherwise.</returns>
public abstract bool CanWrite(ReadOnlySpan<char> contentType);

        /// <summary>
/// Reads and deserializes parameters for the specified domain operation from the HTTP request body.
/// </summary>
/// <param name="context">The HTTP context containing the request to read.</param>
/// <param name="operation">Metadata for the domain operation that guides parameter deserialization.</param>
/// <returns>A tuple with an optional <see cref="ServiceQuery"/> (or null) and an array of deserialized parameter values.</returns>
public abstract Task<(ServiceQuery?, object?[])> ReadParametersFromBodyAsync(HttpContext context, DomainOperationEntry operation);

        /// <summary>
/// Parses a submit (change-set) request from the provided HTTP context and returns the contained change-set entries.
/// </summary>
/// <param name="context">The HTTP context containing the submit request to read.</param>
/// <returns>An enumerable of <see cref="ChangeSetEntry"/> representing the parsed change-set entries.</returns>
public abstract Task<IEnumerable<ChangeSetEntry>> ReadSubmitRequest(HttpContext context);
        /// <summary>
/// Writes an HTTP submit (change-set) response to the provided <see cref="HttpContext"/> using the supplied change-set results.
/// </summary>
/// <param name="context">The HTTP context to write the response to.</param>
/// <param name="result">The change-set entries representing the results of the submit operation.</param>
public abstract Task WriteSubmitResponse(HttpContext context, IEnumerable<ChangeSetEntry> result);

        /// <summary>
/// Writes an error response describing a domain service fault to the HTTP context for the specified operation.
/// </summary>
/// <param name="context">The HTTP context to write the error response to.</param>
/// <param name="fault">The domain service fault information to serialize into the response.</param>
/// <param name="operation">Metadata for the domain operation associated with the fault, used to shape the response.</param>
public abstract Task WriteErrorAsync(HttpContext context, DomainServiceFault fault, DomainOperationEntry operation);

        /// <summary>
/// Writes a successful operation result to the HTTP response for the specified domain operation.
/// </summary>
/// <param name="context">The HTTP context to which the response will be written.</param>
/// <param name="result">The operation result to serialize into the response; may be null for no content.</param>
/// <param name="operation">Metadata for the domain operation that guides serialization and response formatting.</param>
public abstract Task WriteResponseAsync(HttpContext context, object? result, DomainOperationEntry operation);
    }

}