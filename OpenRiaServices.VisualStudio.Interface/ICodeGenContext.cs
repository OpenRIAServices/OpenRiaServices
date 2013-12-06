using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface ICodeGenContext
    {
        /// <summary>
        /// Gets the CodeGeneratorOptions for this context
        /// </summary>
        CodeGeneratorOptions CodeGeneratorOptions { get; }

        /// <summary>
        /// Gets the CodeDomProvider for this context
        /// </summary>
        CodeDomProvider Provider { get; }

        /// <summary>
        /// Gets the value indicating whether the language is C#
        /// </summary>
        bool IsCSharp { get; }

        /// <summary>
        /// Gets the set of assembly references generated code needs to compile
        /// </summary>
        /// <remarks>
        /// This set represents only those additional references required for
        /// the entity types and DAL types appearing in the generated code
        /// </remarks>
        IEnumerable<string> References { get; }

        /// <summary>
        /// Gets the root namespace.  For use with VB codegen only.
        /// </summary>
        string RootNamespace { get; }

        /// <summary>
        /// Adds the given assembly reference to the list of known assembly references
        /// necessary to compile
        /// </summary>
        /// <param name="reference">The full name of the assembly reference.</param>
        void AddReference(string reference);

        /// <summary>
        /// Returns <c>true</c> only if the given identifier is valid for the current language
        /// </summary>
        /// <param name="identifier">The string to check for validity.  Null will cause a <c>false</c> to be returned.</param>
        /// <returns><c>true</c> if the identifier is valid</returns>
        bool IsValidIdentifier(string identifier);

        /// <summary>
        /// Generates code for the current compile unit into a string.  Also strips out auto-generated comments/
        /// </summary>
        /// <returns>The generated code and necessary references.</returns>
        IGeneratedCode GenerateCode();

        /// <summary>
        /// Generates a new CodeNamespace or reuses an existing one of the given name
        /// </summary>
        /// <param name="namespaceName">The namespace in which to generate code.</param>
        /// <returns>namespace with the given name</returns>
        CodeNamespace GetOrGenNamespace(string namespaceName);

        /// <summary>
        /// Gets the <see cref="CodeNamespace"/> for a <see cref="CodeTypeDeclaration"/>.
        /// </summary>
        /// <param name="typeDecl">A <see cref="CodeTypeDeclaration"/>.</param>
        /// <returns>A <see cref="CodeNamespace"/> or null.</returns>
        CodeNamespace GetNamespace(CodeTypeDeclaration typeDecl);

        /// <summary>
        /// Override of IDisposable.Dispose to handle implementation details of dispose
        /// </summary>
        void Dispose();
    }
}