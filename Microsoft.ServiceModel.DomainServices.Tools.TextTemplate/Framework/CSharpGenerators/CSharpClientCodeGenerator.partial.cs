using System;
using System.Globalization;
namespace Microsoft.ServiceModel.DomainServices.Tools.TextTemplate.CSharpGenerators
{
    /// <summary>
    /// Generator to generate DomainService proxies in C#.
    /// </summary>
    [DomainServiceClientCodeGeneratorAttribute(typeof(CSharpClientCodeGenerator), "C#")]
    public partial class CSharpClientCodeGenerator
    {
        private EntityGenerator _entityProxyGenerator;
        private ComplexObjectGenerator _complexObjectGenerator;
        private DomainContextGenerator _domainContextGenerator;
        private WebContextGenerator _webContextGenerator;
        private EnumGenerator _enumGenerator;

        /// <summary>
        /// Gets the C# entity generator.
        /// </summary>
        protected override EntityGenerator EntityGenerator { get { return this._entityProxyGenerator; } }

        /// <summary>
        /// Gets the C# complex object generator.
        /// </summary>
        protected override ComplexObjectGenerator ComplexObjectGenerator { get { return this._complexObjectGenerator; } }

        /// <summary>
        /// Gets the C# DomainContext generator.
        /// </summary>
        protected override DomainContextGenerator DomainContextGenerator { get { return this._domainContextGenerator; } }

        /// <summary>
        /// Gets the C# WebContext generator.
        /// </summary>
        protected override WebContextGenerator WebContextGenerator { get { return this._webContextGenerator; } }

        /// <summary>
        /// Gets the C# enum generator.
        /// </summary>
        protected override EnumGenerator EnumGenerator { get { return this._enumGenerator; } }

        /// <summary>
        /// Default constructor for CSharpClientCodeGenerator. It initializes contained generators with their default values.
        /// </summary>
        public CSharpClientCodeGenerator()
        {
            this._webContextGenerator = new CSharpWebContextGenerator();
            this._entityProxyGenerator = new CSharpEntityGenerator();
            this._complexObjectGenerator = new CSharpComplexObjectGenerator();
            this._domainContextGenerator = new CSharpDomainContextGenerator();
            this._enumGenerator = new CSharpEnumGenerator();
        }

        /// <summary>
        /// Generates proxy class code in C#.
        /// </summary>
        /// <returns>Generated C# code.</returns>
        protected override string GenerateCode()
        {
            if (!this.Options.Language.Equals("C#", StringComparison.OrdinalIgnoreCase))
            {
                this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, TextTemplateResource.NonCSharpLanguageNotSupported));
                return null;
            }

            return this.TransformText();
        }
    }
}
