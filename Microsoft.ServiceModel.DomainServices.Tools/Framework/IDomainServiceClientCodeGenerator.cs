using System.Collections.Generic;
using System.ServiceModel.DomainServices.Server;

namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Common interface for code generators that produce client code from 
    /// <see cref="System.ServiceModel.DomainServices.Server.DomainServiceDescription"/> instances.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are expected to be stateless objects
    /// that can be invoked to generate code on demand.   A single instance of this
    /// class may be used multiple times with different sets of inputs.
    /// </remarks>
    public interface IDomainServiceClientCodeGenerator
    {
        /// <summary>
        /// Generates the source code for the client classes
        /// for the given <paramref name="domainServiceDescriptions"/>.
        /// </summary>
        /// <remarks>
        /// Errors and warnings should be reported using the <paramref name="codeGenerationHost"/>.
        /// </remarks>
        /// <param name="codeGenerationHost">The <see cref="ICodeGenerationHost"/> object hosting code generation.</param>
        /// <param name="domainServiceDescriptions">The set of <see cref="System.ServiceModel.DomainServices.Server.DomainServiceDescription"/> 
        /// instances for which code generation is required.</param>
        /// <param name="options">The options for code generation.</param>
        /// <returns>The generated code.  This value may be empty or <c>null</c> if errors occurred or there was no work to do.</returns>
        string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options);
    }
}
