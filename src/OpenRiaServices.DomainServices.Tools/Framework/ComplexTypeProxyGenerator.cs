using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Proxy generator for a complex type.
    /// </summary>
    internal class ComplexTypeProxyGenerator : DataContractProxyGenerator
    {
        readonly DomainServiceDescription _domainServiceDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexTypeProxyGenerator"/> class.
        /// </summary>
        /// <param name="proxyGenerator">The client proxy generator against which this will generate code.  Cannot be null.</param>
        /// <param name="complexType">The complex type.  Cannot be null.</param>
        /// <param name="domainServiceDescription"><see cref="DomainServiceDescription"/> that exposes this complex type.</param>
        /// <param name="typeMapping">A dictionary of <see cref="DomainService"/> and related complex types that maps to their corresponding client-side <see cref="CodeTypeReference"/> representations.</param>
        public ComplexTypeProxyGenerator(CodeDomClientCodeGenerator proxyGenerator, Type complexType, DomainServiceDescription domainServiceDescription, IDictionary<Type, CodeTypeDeclaration> typeMapping)
            : base(proxyGenerator, complexType, typeMapping)
        {
            this._domainServiceDescription = domainServiceDescription;
        }

        // ComplexTypes is used to determine which properties should be exposed. Given that, any DomainServiceDescription would contain
        // all necessary complex types.
        protected override IEnumerable<Type> ComplexTypes
        {
            get
            {
                return this._domainServiceDescription.ComplexTypes;
            }
        }

        protected override bool IsDerivedType
        {
            get
            {
                // Always false since complex types do not support inheritance on the client.
                return false;
            }
        }

        protected override void AddBaseTypes(CodeNamespace ns)
        {
            this.ProxyClass.BaseTypes.Add(CodeGenUtilities.GetTypeReference(TypeConstants.ComplexObjectTypeFullName, ns.Name, false));
        }

        protected override IEnumerable<Type> GetDerivedTypes()
        {
            // Always empty since complex types do not support inheritance on the client.
            return Enumerable.Empty<Type>();
        }

        protected override string GetSummaryComment()
        {
            return string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Complex_Class_Summary_Comment, this.Type.Name);
        }
    }
}
