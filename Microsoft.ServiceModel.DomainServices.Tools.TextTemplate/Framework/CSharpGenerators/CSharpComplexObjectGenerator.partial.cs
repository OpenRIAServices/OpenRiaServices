namespace OpenRiaServices.DomainServices.Tools.TextTemplate.CSharpGenerators
{
    using System;

    /// <summary>
    /// C# generator for complex types.
    /// </summary>
    public partial class CSharpComplexObjectGenerator
    {
        /// <summary>
        /// Generates complex type in C#.
        /// </summary>
        /// <returns>Generated complex type code.</returns>
        protected override string GenerateDataContractProxy()
        {
            return this.TransformText();
        }

        private void Generate()
        {
            this.Initialize();
            this.GenerateComplexObjectClass();
        }

        private void GenerateComplexObjectClass()
        {
            this.GenerateNamespace();
            this.GenerateOpeningBrace();

            this.GenerateClassDeclaration();
            this.GenerateOpeningBrace();

            this.GenerateBody();

            this.GenerateClosingBrace();
            this.GenerateClosingBrace();
        }

        /// <summary>
        /// Generates the body of the complex type.
        /// </summary>
        /// <remarks>
        /// The default implementation of this method invokes <see cref="GenerateConstructor"/>,
        /// <see cref="GenerateProperties"/> and <see cref="GenerateNotificationMethods"/>.
        /// </remarks>
        protected virtual void GenerateBody()
        {
            this.GenerateConstructor();
            this.GenerateProperties();
            this.GenerateNotificationMethods();
        }

        /// <summary>
        /// Generates the type properties.
        /// </summary>
        protected virtual void GenerateProperties()
        {
            this.GeneratePropertiesInternal();
        }
    }
}