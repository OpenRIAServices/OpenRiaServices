using System;
using System.Collections.Generic;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Interface for the host object for code generation.
    /// </summary>
    /// <remarks>
    /// Code generators are provided with an object instance that implements this interface.
    /// They use this host instance to log errors, warnings and messages back to the
    /// environment.  
    /// <para>
    /// The generators can also use this host instance to determine which types or
    /// members are shared by both the source and target projects.  In this context,
    /// the "source" project contains the types from which code needs to be generated.
    /// The "target" project is the one into which the generated code will be inserted.
    /// </para>
    /// <para>
    /// This service to determine shared code members is intended to be used by the
    /// code generators so they can know which member references or declarations are safe
    /// to make from the generated code.
    /// </para>
    /// </remarks>
    public interface ICodeGenerationHost : ILogger
    {
        /// <summary>
        /// Logs the given message as an error, together with information about the source location.
        /// </summary>
        /// <param name="message">The message to log as an error.</param>
        /// <param name="subcategory">The optional description of the error type.</param>
        /// <param name="errorCode">The optional error code.</param>
        /// <param name="helpKeyword">The optional help keyword.</param>
        /// <param name="file">The optional path to the file containing the error.</param>
        /// <param name="lineNumber">The zero-relative line number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="columnNumber">The zero-relative column number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="endLineNumber">The zero-relative line number in the <paramref name="file"/> where the error ends.</param>
        /// <param name="endColumnNumber">The zero-relative column number in the <paramref name="file"/> where the error ends.</param>
        void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber);

        /// <summary>
        /// Logs the given message as an warning, together with information about the source location.
        /// </summary>
        /// <param name="message">The message to log as an error.</param>
        /// <param name="subcategory">The optional description of the error type.</param>
        /// <param name="errorCode">The optional error code.</param>
        /// <param name="helpKeyword">The optional help keyword.</param>      
        /// <param name="file">The optional path to the file containing the error.</param>
        /// <param name="lineNumber">The zero-relative line number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="columnNumber">The zero-relative column number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="endLineNumber">The zero-relative line number in the <paramref name="file"/> where the error ends.</param>
        /// <param name="endColumnNumber">The zero-relative column number in the <paramref name="file"/> where the error ends.</param>
        void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber);

        /// <summary>
        /// Returns a value indicating whether the <see cref="Type"/> specified by <paramref name="typeName"/>
        /// from the source project is also visible to the target project.
        /// </summary>
        /// <param name="typeName">The <see cref="Type.AssemblyQualifiedName"/> of the <see cref="Type"/> from the source project.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        CodeMemberShareKind GetTypeShareKind(string typeName);

        /// <summary>
        /// Returns a value indicating whether the a property named <paramref name="propertyName"/>
        /// exposed by the <see cref="Type"/> specified by <paramref name="typeName"/>
        /// from the source project is also visible to the target project.
        /// </summary>
        /// <param name="typeName">The <see cref="Type.AssemblyQualifiedName"/> of the <see cref="Type"/> from the source project.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        CodeMemberShareKind GetPropertyShareKind(string typeName, string propertyName);

        /// <summary>
        /// Returns a value indicating whether a method named <paramref name="methodName"/>
        /// exposed by the <see cref="Type"/> specified by <paramref name="typeName"/>
        /// from the source project is also visible to the target project.
        /// </summary>
        /// <param name="typeName">The <see cref="Type.AssemblyQualifiedName"/> of the <see cref="Type"/> from the source project.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="parameterTypeNames">The <see cref="Type.AssemblyQualifiedName"/> names of the method parameters, in the order they must be declared.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        CodeMemberShareKind GetMethodShareKind(string typeName, string methodName, IEnumerable<string> parameterTypeNames);
    }
}
