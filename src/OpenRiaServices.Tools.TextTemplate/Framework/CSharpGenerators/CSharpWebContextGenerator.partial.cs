namespace OpenRiaServices.Tools.TextTemplate.CSharpGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using OpenRiaServices;
    using OpenRiaServices.Server;
    using OpenRiaServices.Server.Authentication;

    /// <summary>
    /// C# generator for the WebContext class.
    /// </summary>
    public partial class CSharpWebContextGenerator
    {
        /// <summary>
        /// Generates WebContext class code in C#.
        /// </summary>
        /// <returns>Generated WebContext code.</returns>
        protected override string GenerateWebContextClass()
        {
            return this.TransformText();
        }

        private void Generate()
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
        /// Generates WebContext class body.
        /// </summary>
        /// <remarks>
        /// The default implementation of this method invokes <see cref="GenerateConstructor"/>,
        /// <see cref="GenerateExtensibilityMethods"/> and <see cref="GenerateProperties"/>.
        /// </remarks>
        protected virtual void GenerateBody()
        {
            this.GenerateConstructor();
            this.GenerateExtensibilityMethods();
            this.GenerateProperties();
        }

        private DomainServiceDescription GetDefaultAuthDescription()
        {
            DomainServiceDescription defaultAuthDescription = null;
            IEnumerable<DomainServiceDescription> authDescriptions =
                this.DomainServiceDescriptions.Where(d => d.IsAuthenticationService());
            if (authDescriptions.Count() > 1)
            {
                this.ClientCodeGenerator.CodeGenerationHost.LogMessage(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resource.WebContext_ManyAuthServices,
                        string.Join(",", authDescriptions.Select(d => d.DomainServiceType.Name).ToArray())));
            }
            else
            {
                defaultAuthDescription = authDescriptions.FirstOrDefault();
            }
            return defaultAuthDescription;
        }
    }
}
