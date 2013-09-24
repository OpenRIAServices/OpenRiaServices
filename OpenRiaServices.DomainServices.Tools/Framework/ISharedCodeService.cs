using System;
using System.Collections.Generic;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Service interface to reveal which code elements are
    /// shared between two projects.
    /// </summary>
    /// <remarks>Shared code elements are those that are visible
    /// to both projects, either though shared source files
    /// or through assemblies referenced by both projects.
    /// <para>
    /// This service operates from the perspective of a
    /// "reference project" and a "dependent project".  The
    /// code elements are always assumed to reside in the
    /// reference project, and the methods in this service reflect
    /// whether code elements are shared by the dependent project.
    /// </para>
    /// </remarks>
    internal interface ISharedCodeService
    {
        /// <summary>
        /// Returns a value indicating whether the <see cref="Type"/>specified by <paramref name="typeName"/>
        /// from the reference project is also visible to the dependent project.
        /// </summary>
        /// <param name="typeName">The full name of the <see cref="Type"/>from the reference project.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        CodeMemberShareKind GetTypeShareKind(string typeName);

        /// <summary>
        /// Returns a value indicating whether the a property named <paramref name="propertyName"/>
        /// exposed by the <see cref="Type"/> specified by <paramref name="typeName"/>
        /// from the reference project is also visible to the dependent project.
        /// </summary>
        /// <param name="typeName">The full name of the <see cref="Type"/> from the reference project.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        CodeMemberShareKind GetPropertyShareKind(string typeName, string propertyName);

        /// <summary>
        /// Returns a value indicating whether a method named <paramref name="methodName"/>
        /// exposed by the <see cref="Type"/> specified by <paramref name="typeName"/>
        /// from the reference project is also visible to the dependent project.
        /// </summary>
        /// <param name="typeName">The full name of the <see cref="Type"/> from the reference project.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="parameterTypeNames">The full type names of the method parameters, in the order they must be declared.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        CodeMemberShareKind GetMethodShareKind(string typeName, string methodName, IEnumerable<string> parameterTypeNames);
    }
}
