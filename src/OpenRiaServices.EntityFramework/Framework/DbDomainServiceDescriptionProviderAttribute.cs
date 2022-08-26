#if EFCORE
using System;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace OpenRiaServices.Server.EntityFrameworkCore
#else
using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Globalization;
using OpenRiaServices.Server;

namespace OpenRiaServices.EntityFramework
#endif
{
    /// <summary>
    /// Attribute applied to a <see cref="DbDomainService{DbContext}"/> that exposes LINQ to Entities mapped
    /// Types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class DbDomainServiceDescriptionProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        private Type _dbContextType;

        /// <summary>
        /// Default constructor. Using this constructor, the Type of the LINQ To Entities
        /// DbContext will be inferred from the <see cref="DomainService"/> the
        /// attribute is applied to.
        /// </summary>
        public DbDomainServiceDescriptionProviderAttribute()
#if EFCORE
            : base(typeof(EFCoreDescriptionProvider))
#else
            : base(typeof(LinqToEntitiesDomainServiceDescriptionProvider))
#endif
        {
        }

        /// <summary>
        /// Constructs an attribute for the specified LINQ To Entities
        /// DbContext Type.
        /// </summary>
        /// <param name="dbContextType">The LINQ To Entities ObjectContext Type.</param>
        public DbDomainServiceDescriptionProviderAttribute(Type dbContextType)
#if EFCORE
            : base(typeof(EFCoreDescriptionProvider))
#else
            : base(typeof(LinqToEntitiesDomainServiceDescriptionProvider))
#endif
        {
            _dbContextType = dbContextType;
        }

        /// <summary>
        /// The Linq To Entities DbContext Type.
        /// </summary>
        public Type DbContextType
        {
            get
            {
                return _dbContextType;
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

            if (_dbContextType == null)
            {
                _dbContextType = GetContextType(domainServiceType);
            }

            if (!typeof(DbContext).IsAssignableFrom(_dbContextType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    DbResource.InvalidDbDomainServiceDescriptionProviderSpecification,
                    _dbContextType));
            }
#if EFCORE
            return new EFCoreDescriptionProvider(domainServiceType, _dbContextType, parent);
#else
            return new LinqToEntitiesDomainServiceDescriptionProvider(domainServiceType, _dbContextType, parent);
#endif
        }

        /// <summary>
        /// Extracts the context type from the specified <paramref name="domainServiceType"/>.
        /// </summary>
        /// <param name="domainServiceType">A LINQ to Entities domain service type.</param>
        /// <returns>The type of the object context.</returns>
        private static Type GetContextType(Type domainServiceType)
        {
            Type efDomainServiceType = domainServiceType.BaseType;
            while (!efDomainServiceType.IsGenericType || efDomainServiceType.GetGenericTypeDefinition() != typeof(DbDomainService<>))
            {
                if (efDomainServiceType == typeof(object))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resource.InvalidMetadataProviderSpecification,
                    typeof(DbDomainServiceDescriptionProviderAttribute).Name, domainServiceType.Name, typeof(DbDomainService<>).Name));
                }
                efDomainServiceType = efDomainServiceType.BaseType;
            }

            return efDomainServiceType.GetGenericArguments()[0];
        }
    }
}
