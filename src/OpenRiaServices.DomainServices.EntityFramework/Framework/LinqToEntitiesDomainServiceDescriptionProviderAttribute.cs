using System;
using System.ComponentModel;
#if DBCONTEXT
using System.Data.Entity.Core.Objects;
#else
using System.Data.Objects;
#endif
using System.Globalization;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.EntityFramework
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> that exposes LINQ to Entities mapped
    /// Types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class LinqToEntitiesDomainServiceDescriptionProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        private Type _objectContextType;

        /// <summary>
        /// Default constructor. Using this constructor, the Type of the LINQ To Entities
        /// ObjectContext will be inferred from the <see cref="DomainService"/> the
        /// attribute is applied to.
        /// </summary>
        public LinqToEntitiesDomainServiceDescriptionProviderAttribute()
            : base(typeof(LinqToEntitiesDomainServiceDescriptionProvider))
        {
        }

        /// <summary>
        /// Constructs an attribute for the specified LINQ To Entities
        /// ObjectContext Type.
        /// </summary>
        /// <param name="objectContextType">The LINQ To Entities ObjectContext Type.</param>
        public LinqToEntitiesDomainServiceDescriptionProviderAttribute(Type objectContextType)
            : base(typeof(LinqToEntitiesDomainServiceDescriptionProvider))
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
                throw new ArgumentNullException("domainServiceType");
            }

            if (this._objectContextType == null)
            {
                this._objectContextType = GetContextType(domainServiceType);
            }

            if (!typeof(ObjectContext).IsAssignableFrom(this._objectContextType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resource.InvalidLinqToEntitiesDomainServiceDescriptionProviderSpecification,
                    this._objectContextType));
            }

            return new LinqToEntitiesDomainServiceDescriptionProvider(domainServiceType, this._objectContextType, parent);
        }

        /// <summary>
        /// Extracts the context type from the specified <paramref name="domainServiceType"/>.
        /// </summary>
        /// <param name="domainServiceType">A LINQ to Entities domain service type.</param>
        /// <returns>The type of the object context.</returns>
        private static Type GetContextType(Type domainServiceType)
        {
            Type efDomainServiceType = domainServiceType.BaseType;
            while (!efDomainServiceType.IsGenericType || efDomainServiceType.GetGenericTypeDefinition() != typeof(LinqToEntitiesDomainService<>))
            {
                if (efDomainServiceType == typeof(object))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resource.InvalidMetadataProviderSpecification,
                    typeof(LinqToEntitiesDomainServiceDescriptionProviderAttribute).Name, domainServiceType.Name, typeof(LinqToEntitiesDomainService<>).Name));
                }
                efDomainServiceType = efDomainServiceType.BaseType;
            }

            return efDomainServiceType.GetGenericArguments()[0];
        }
    }
}
