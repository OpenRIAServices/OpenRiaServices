using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenRiaServices.Server;

#nullable enable

namespace OpenRiaServices.Hosting.AspNetCore
{
    /// <summary>
    /// <see cref="UnhandledExceptionResponse"/> can be used to control how errors are passed to the client
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="fault"></param>
    public sealed class UnhandledExceptionResponse(DomainServiceFault fault, HttpContext httpContext)
    {
        // Consider exposing in the future for easier access to HTTP headers (for now the user can use ServiceContext.GetService<IHttpContextAccessor>() )
        private HttpContext HttpContext { get; } = httpContext;

        /// <summary>
        /// Error message to be sent to the client, it will be part of the message of the <see cref="DomainException"/> or <c>DomainOperationException</c> thrown
        /// </summary>
        public string ErrorMessage { get => fault.ErrorMessage; set => fault.ErrorMessage = value; }

        /// <summary>
        /// <inheritdoc cref="DomainException.ErrorCode"/>
        /// </summary>
        public int ErrorCode { get => fault.ErrorCode; set => fault.ErrorCode = value; }

        /// <summary>
        /// Status code to use for the Http response
        /// </summary>
        public System.Net.HttpStatusCode HttpStatusCode { get => (System.Net.HttpStatusCode)HttpContext.Response.StatusCode; set => HttpContext.Response.StatusCode = (int)value; }

        // TO REVIEW: Use getter/seter or Set methods (which makes settable properties stand out a bit, but it is not as straightforward as setters)
        // SetErrorMessage()
        // SetErrorCode
        // SetHttpStatusCode

        /// <summary>
        /// Set a  <see cref="DomainException"/> to be rethrown at the client.
        /// It will update <see cref="ErrorMessage"/> and <see cref="ErrorCode"/>
        /// </summary>
        public void SetFromException(DomainException domainException)
        {
            fault.SetFromDomainException(domainException, HttpContext.RequestServices.GetRequiredService<IOptions<OpenRiaServicesOptions>>().Value.IncludeExceptionStackTraceInErrors);
        }
    };
}
