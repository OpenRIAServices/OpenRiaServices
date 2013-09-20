namespace Microsoft.ServiceModel.DomainServices.Tools.TextTemplate
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Generator for an enum.
    /// </summary>
    public abstract partial class EnumGenerator
    {
        /// <summary>
        /// Gets the set of enums to generate.
        /// </summary>
        protected HashSet<Type> EnumsToGenerate { get; private set; }

        /// <summary>
        /// Gets the ClientCodeGenerator object.
        /// </summary>
        protected ClientCodeGenerator ClientCodeGenerator { get; private set; }

        /// <summary>
        /// Generates enums on the client. It calls the GenerateEnums method to generate the actual code in a specific language.
        /// </summary>
        /// <param name="enumsToGenerate">The list of all the enums to generate.</param>
        /// <param name="clientCodeGenerator">The ClientCodeGenerator object for this instance.</param>
        /// <returns>The generated enum code.</returns>
        public string GenerateEnums(HashSet<Type> enumsToGenerate, ClientCodeGenerator clientCodeGenerator)
        {
            this.EnumsToGenerate = enumsToGenerate;
            this.ClientCodeGenerator = clientCodeGenerator;
            return this.GenerateEnums();
        }

        /// <summary>
        /// When overridden in a derived class, it generates the actual enum code in a specific language.
        /// </summary>
        /// <returns>The generated enum code.</returns>
        protected abstract string GenerateEnums();
    }
}
