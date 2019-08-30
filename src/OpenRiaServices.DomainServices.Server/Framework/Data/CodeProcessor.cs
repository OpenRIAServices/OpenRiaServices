using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Base class for all <see cref="CodeProcessor"/> implementations. By associating a <see cref="CodeProcessor"/> Type
    /// with a <see cref="DomainService"/> Type via the <see cref="OpenRiaServices.DomainServices.DomainIdentifierAttribute"/>, codegen for the service
    /// Type can be customized.
    /// </summary>
    public abstract class CodeProcessor
    {
        /// <summary>
        /// Private reference to the <see cref="CodeDomProvider"/> used during <see cref="DomainService"/> code generation.
        /// </summary>
        private readonly CodeDomProvider _codeDomProvider;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="codeDomProvider">The <see cref="CodeDomProvider"/> used during <see cref="DomainService"/> code generation.</param>
        protected CodeProcessor(CodeDomProvider codeDomProvider)
        {
            if (codeDomProvider == null)
            {
                throw new ArgumentNullException(nameof(codeDomProvider));
            }

            this._codeDomProvider = codeDomProvider;
        }

        /// <summary>
        /// The <see cref="CodeDomProvider"/> used during <see cref="DomainService"/> code generation.
        /// </summary>
        protected CodeDomProvider CodeDomProvider
        {
            get 
            {
                return this._codeDomProvider;
            }
        }

        /// <summary>
        /// Invoked after code generation of the current <see cref="DomainService"/> has completed, allowing for post processing of the <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="domainServiceDescription">The <see cref="DomainServiceDescription"/> describing the <see cref="DomainService"/> currently being examined.</param>
        /// <param name="codeCompileUnit">The <see cref="CodeCompileUnit"/> that the <see cref="DomainService"/> client code is being generated into.</param>
        /// <param name="typeMapping">A dictionary mapping <see cref="DomainService"/> and related entity types to their corresponding <see cref="CodeTypeDeclaration"/>s.</param>
        public abstract void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping);
    }
}
