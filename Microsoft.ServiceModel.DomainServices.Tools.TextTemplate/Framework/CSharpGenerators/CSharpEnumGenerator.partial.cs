namespace Microsoft.ServiceModel.DomainServices.Tools.TextTemplate.CSharpGenerators
{
    using System;

    /// <summary>
    /// C# generator for enums.
    /// </summary>
    public partial class CSharpEnumGenerator
    {
        /// <summary>
        /// Generated enums in C#
        /// </summary>
        /// <returns>Generated enum code.</returns>
        protected override string GenerateEnums()
        {
            return this.TransformText();
        }

        private void Generate()
        {
            foreach (Type enumType in this.EnumsToGenerate)
            {
                this.GenerateEnum(enumType);
            }
        }

        /// <summary>
        /// Generates enum in C#.
        /// </summary>
        /// <param name="enumType">Type of enum for which code is to be generated.</param>
        protected virtual void GenerateEnum(Type enumType)
        {
            this.GenerateEnumNamespace(enumType);
            this.GenerateOpeningBrace();
            
            this.GenerateEnumTypeDeclaration(enumType);
            this.GenerateOpeningBrace();
            
            this.GenerateEnumMembers(enumType);
            
            this.GenerateClosingBrace();
            this.GenerateClosingBrace();
        }
    }
}
