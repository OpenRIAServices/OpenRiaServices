using System;
using System.Linq;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    public sealed class OpenRiaServicesOptions
    {
        /// <summary>
        /// Specifies a global exception handler for unhandled exceptions that occur during the processing of a request.
        /// <para>It is similar to <see cref="DomainService.OnError(DomainServiceErrorInfo)"/> but is shared for all DomainServices 
        /// and allows direct modification of the error message and status code sent to the client.</para>
        /// </summary>
        public Action<UnhandledExceptionContext, UnhandledExceptionResponse>? ExceptionHandler { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exception stack trace information should be included in error messages.
        /// <para>This is considered INSECURE since it gives an attacker to much information</para>
        /// </summary>
        public bool IncludeExceptionStackTraceInErrors { get; set; }

        /// <summary>
        /// UNSAFE: Gets or sets a value indicating whether exception message details (from exceptions other than <see cref="DomainException"/>) should be included in error messages.
        /// <para>This is generally considered INSECURE since it can provide an attacker with to much information</para>
        /// </summary>
        public bool IncludeExceptionMessageInErrors { get; set; }

        internal bool EnableTextXmlSerialization { get; set; }

        /// <summary>
        /// List of all registered wire formats on descending order of priority. 
        /// First one is the default used for responses (when client do not specify an matching format)
        /// </summary>
        internal ISerializationProvider[] SerializationProviders { get; set; } = [new BinaryXmlSerializationProvider()];

        /// <summary>
        /// Adds a serialization provider to the list of supported formats.
        /// </summary>
        /// <param name="provider"></param>
        /// <summary>
        /// Registers a serialization provider and updates the ordered list of active providers.
        /// </summary>
        /// <param name="provider">The serialization provider to add.</param>
        /// <param name="defaultProvider">If true, place the provider at highest priority so it becomes the default for responses; otherwise append it to the end of the priority list.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
        internal void AddSerializationProvider(ISerializationProvider provider, bool defaultProvider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            if (provider is DataContractSerializationProvider dataContractSerializationProvider
                && SerializationProviders.OfType<DataContractSerializationProvider>().FirstOrDefault() is { } existingDcs)
            {
                // Share the DataContractCache between the two providers to avoid duplicate work
                dataContractSerializationProvider._perDomainServiceDataContractCache = existingDcs._perDomainServiceDataContractCache;
            }

            if (defaultProvider)
            {
                SerializationProviders = [provider, .. SerializationProviders];
            }
            else
            {
                SerializationProviders = [.. SerializationProviders, provider];
            }
        }

        /// <summary>
        /// Enables Text based XML wire format (application/xml) in addition to built in binary XML (application/msbin1).
        /// It does not change the default format.
        /// </summary>
        /// <remarks>Request should specify mime-type <c>application/xml</c> using <c>Content-Type</c> or <c>Accept</c> headers
        /// </remarks>
        /// <summary>
        /// Enables text/XML (application/xml) wire-format serialization by registering a TextXmlSerializationProvider.
        /// </summary>
        /// <param name="defaultProvider">If <see langword="true"/>, make the Text XML provider the highest-priority provider so responses default to XML; otherwise add it with lower priority.</param>
        public void AddTextXmlSerializer(bool defaultProvider = false)
        {
            // Good parameter candidates for options to pass in would include 
            // XmlDictionaryReaderQuotas, XmlDictionaryWriterQuotas (pass on to MessageReader, MessageWriter caches)

            AddSerializationProvider(new TextXmlSerializationProvider(), defaultProvider);
        }

        /// <summary>
        /// Removes the default Binary XML (application/msbin1) wire format.
        /// <summary>
        /// Removes all registered BinaryXmlSerializationProvider instances from the active SerializationProviders list.
        /// </summary>
        public void RemoveBinaryXmlSerializer()
        {
            SerializationProviders = [.. SerializationProviders.Where(sp => sp is not BinaryXmlSerializationProvider)];
        }

        /* ************ SOME POSSIBLE FUTURE OPTIONS ************ 
         * 
         * int MaxReceiveSize / MaxRequestSize { get; set; }
         * int MaxResponseSize { get; set; }
         * 
         * int RouteOrder { get; set; }
         * */
    }
}