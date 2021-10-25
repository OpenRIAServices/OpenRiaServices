using System;
using System.ComponentModel;
using System.Globalization;
using OpenRiaServices.Server;

namespace OpenRiaServices.EntityFrameworkCore
{

    // TODO: Remove and move code to DB context

    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> that exposes LINQ to Entities mapped
    /// Types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class LinqToEntitiesDomainServiceEFCoreDescriptionProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        private Type _objectContextType;

        /// <summary>
        /// Default constructor. Using this constructor, the Type of the LINQ To Entities
        /// ObjectContext will be inferred from the <see cref="DomainService"/> the
        /// attribute is applied to.
        /// </summary>
        public LinqToEntitiesDomainServiceEFCoreDescriptionProviderAttribute()
            : base(typeof(LinqToEntitiesDomainServiceEFCoreDescriptionProvider))
        {
        }

        /// <summary>
        /// Constructs an attribute for the specified LINQ To Entities
        /// ObjectContext Type.
        /// </summary>
        /// <param name="objectContextType">The LINQ To Entities ObjectContext Type.</param>
        public LinqToEntitiesDomainServiceEFCoreDescriptionProviderAttribute(Type objectContextType)
            : base(typeof(LinqToEntitiesDomainServiceEFCoreDescriptionProvider))
        {
            this._objectContextType = objectContextType;
        }

        /// <summary>
        /// The Linq To Entities ObjectContext Type.
        /// </summary>
        public Type ObjectContextType
        {
            get
            {
                return this._objectContextType;
            }
        }

        /// <summary>
        /// This method creates an instance of the <see cref="TypeDescriptionProvider"/>.
        /// </summary>
        /// <param name="domainServiceType">The <see cref="DomainService"/> Type to create a description provider for.</param>
        /// <param name="parent">The existing parent description provider.</param>
        /// <returns>The description provider.</returns>
        public override DomainServiceDescriptionProvider CreateProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceType));
            }

            if (this._objectContextType == null)
            {
                this._objectContextType = GetContextType(domainServiceType);
            }

            return new LinqToEntitiesDomainServiceEFCoreDescriptionProvider(domainServiceType, this._objectContextType, parent);
        }

        /// <summary>
        /// Extracts the context type from the specified <paramref name="domainServiceType"/>.
        /// </summary>
        /// <param name="domainServiceType">A LINQ to Entities domain service type.</param>
        /// <returns>The type of the object context.</returns>
        private static Type GetContextType(Type domainServiceType)
        {
            Type efDomainServiceType = domainServiceType.BaseType;
            while (!efDomainServiceType.IsGenericType || efDomainServiceType.GetGenericTypeDefinition() != typeof(DomainService))
            {
                if (efDomainServiceType == typeof(object))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resource.InvalidMetadataProviderSpecification,
                    typeof(LinqToEntitiesDomainServiceEFCoreDescriptionProviderAttribute).Name, domainServiceType.Name, typeof(DomainService).Name));
                }
                efDomainServiceType = efDomainServiceType.BaseType;
            }

            return efDomainServiceType.GetGenericArguments()[0];
        }
    }
}
