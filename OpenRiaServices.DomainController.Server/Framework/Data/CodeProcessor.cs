using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace OpenRiaServices.DomainController.Server
{
    /// <summary>
    /// Base class for all <see cref="CodeProcessor"/> implementations. By associating a <see cref="CodeProcessor"/> Type
    /// with a <see cref="DomainController"/> Type via the <see cref="OpenRiaServices.DomainController.DomainIdentifierAttribute"/>, codegen for the service
    /// Type can be customized.
    /// </summary>
    public abstract class CodeProcessor
    {
        /// <summary>
        /// Private reference to the <see cref="CodeDomProvider"/> used during <see cref="DomainController"/> code generation.
        /// </summary>
        private CodeDomProvider _codeDomProvider;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="codeDomProvider">The <see cref="CodeDomProvider"/> used during <see cref="DomainController"/> code generation.</param>
        protected CodeProcessor(CodeDomProvider codeDomProvider)
        {
            if (codeDomProvider == null)
            {
                throw new ArgumentNullException("codeDomProvider");
            }

            this._codeDomProvider = codeDomProvider;
        }

        /// <summary>
        /// The <see cref="CodeDomProvider"/> used during <see cref="DomainController"/> code generation.
        /// </summary>
        protected CodeDomProvider CodeDomProvider
        {
            get 
            {
                return this._codeDomProvider;
            }
        }

        /// <summary>
        /// Invoked after code generation of the current <see cref="DomainController"/> has completed, allowing for post processing of the <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="DomainControllerDescription">The <see cref="DomainControllerDescription"/> describing the <see cref="DomainController"/> currently being examined.</param>
        /// <param name="codeCompileUnit">The <see cref="CodeCompileUnit"/> that the <see cref="DomainController"/> client code is being generated into.</param>
        /// <param name="typeMapping">A dictionary mapping <see cref="DomainController"/> and related entity types to their corresponding <see cref="CodeTypeDeclaration"/>s.</param>
        public abstract void ProcessGeneratedCode(DomainControllerDescription DomainControllerDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping);
    }
}
