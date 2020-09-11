namespace OpenRiaServices.Tools.TextTemplate
{
    using System.Collections.Generic;
    using OpenRiaServices.Server;

    /// <summary>
    /// Generator class that generates WebContext classes.
    /// </summary>
    public abstract partial class WebContextGenerator
    {
        /// <summary>
        /// Gets the ClientCodeGenerator object.
        /// </summary>
        protected ClientCodeGenerator ClientCodeGenerator { get; private set; }
        
        /// <summary>
        /// Gets the list of all the DomainServiceDescription objects.
        /// </summary>
        protected IEnumerable<DomainServiceDescription> DomainServiceDescriptions { get; private set; }

        /// <summary>
        /// Generates the WebContext class. It calls the GenerateWebContextClass method to generate the actual code in a specific language.
        /// </summary>
        /// <param name="domainServiceDescriptions">The list of all the DomainServiceDesctiption objects.</param>
        /// <param name="clientCodeGenerator">The ClientCodeGenerator object for this instance.</param>
        /// <returns>The generated code.</returns>
        public string Generate(IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerator clientCodeGenerator)
        {
            this.ClientCodeGenerator = clientCodeGenerator;
            this.DomainServiceDescriptions = domainServiceDescriptions;

            return this.GenerateWebContextClass();
        }

        /// <summary>
        /// When overridden in a derived class, generates WebContext class in a specific language.
        /// </summary>
        /// <returns>The generated code.</returns>
        protected abstract string GenerateWebContextClass();
    }
}
