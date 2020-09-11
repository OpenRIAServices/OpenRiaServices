using System;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Contract for codegen attribute builders.
    /// </summary>    
    internal interface ICustomAttributeBuilder
    {
        /// <summary>
        /// Returns a representative <see cref="AttributeDeclaration"/> for a given <see cref="Attribute"/> instance.
        /// </summary>
        /// <param name="attribute">An <see cref="Attribute"/> instance to create a <see cref="AttributeDeclaration"/> for.</param>
        /// <returns>A <see cref="AttributeDeclaration"/> representing the <paramref name="attribute"/>.</returns>
        /// <exception cref="ArgumentException">if it cannot generate the provided <paramref name="attribute"/>.</exception>
        AttributeDeclaration GetAttributeDeclaration(Attribute attribute);
    }
}