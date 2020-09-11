namespace OpenRiaServices.Tools.TextTemplate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OpenRiaServices.Server;

    /// <summary>
    /// Proxy generator for a complex object.
    /// </summary>
    public abstract partial class ComplexObjectGenerator
    {
        private const string ComplexObjectBaseTypeFullName = "OpenRiaServices.Client.ComplexObject";

        /// <summary>
        /// Gets the DomainServiceDescription for the domain service associated with this complex type.
        /// </summary>
        protected DomainServiceDescription DomainServiceDescription { get; private set; }

        /// <summary>
        /// Generates complex object code.
        /// </summary>
        /// <param name="complexObjectType">Type of the complex object for which the proxy is to be generates.</param>
        /// <param name="domainServiceDescription">The DomainServiceDescription for the domain service associated with this complex type.</param>
        /// <param name="clientCodeGenerator">ClientCodeGenerator object for this instance.</param>
        /// <returns>The generated complex object code.</returns>
        public string Generate(Type complexObjectType, DomainServiceDescription domainServiceDescription, ClientCodeGenerator clientCodeGenerator)
        {
            this.Type = complexObjectType;
            this.ClientCodeGenerator = clientCodeGenerator;
            this.DomainServiceDescription = domainServiceDescription;

            return this.GenerateDataContractProxy();
        }

        internal override IEnumerable<Type> ComplexTypes
        {
            get
            {
                return this.DomainServiceDescription.ComplexTypes;
            }
        }

        internal override IEnumerable<Type> GetDerivedTypes()
        {
            // Always empty since complex types do not support inheritance on the client.
            return Enumerable.Empty<Type>();
        }

        internal override string GetBaseTypeName()
        {
            return ComplexObjectGenerator.ComplexObjectBaseTypeFullName;
        }

        internal override bool IsDerivedType
        {
            get
            {
                // Always false since complex types do not support inheritance on the client.
                return false;
            }
        }
    }
}
