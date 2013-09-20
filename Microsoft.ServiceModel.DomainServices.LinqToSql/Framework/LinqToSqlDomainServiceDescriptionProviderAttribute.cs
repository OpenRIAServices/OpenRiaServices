using System;
using System.Data.Linq;
using System.Globalization;
using System.ServiceModel.DomainServices.Server;

namespace Microsoft.ServiceModel.DomainServices.LinqToSql
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> that exposes LINQ to SQL mapped
    /// Types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class LinqToSqlDomainServiceDescriptionProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        private Type _dataContextType;

        /// <summary>
        /// Default constructor. Using this constructor, the Type of the LINQ to SQL
        /// DataContext will be inferred from the <see cref="DomainService"/> the
        /// attribute is applied to.
        /// </summary>
        public LinqToSqlDomainServiceDescriptionProviderAttribute()
            : base(typeof(LinqToSqlDomainServiceDescriptionProvider))
        {
        }

        /// <summary>
        /// Constructs an attribute for the specified LINQ to SQL
        /// DataContext Type.
        /// </summary>
        /// <param name="dataContextType">The LINQ to SQL DataContext Type.</param>
        public LinqToSqlDomainServiceDescriptionProviderAttribute(Type dataContextType)
            : base(typeof(LinqToSqlDomainServiceDescriptionProvider))
        {
            this._dataContextType = dataContextType;
        }

        /// <summary>
        /// The LINQ to SQL DataContext Type.
        /// </summary>
        public Type DataContextType
        {
            get
            {
                return this._dataContextType;
            }
        }

        /// <summary>
        /// This method creates an instance of the <see cref="LinqToSqlDomainServiceDescriptionProvider"/>.
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

            if (this._dataContextType == null)
            {
                this._dataContextType = GetContextType(domainServiceType);
            }

            if (!typeof(DataContext).IsAssignableFrom(this._dataContextType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, 
                    Resource.InvalidLinqToSqlDomainServiceDescriptionProviderSpecification, 
                    this._dataContextType));
            }

            return new LinqToSqlDomainServiceDescriptionProvider(domainServiceType, this._dataContextType, parent);
        }

        /// <summary>
        /// Extracts the context type from the specified <paramref name="domainServiceType"/>.
        /// </summary>
        /// <param name="domainServiceType">A LINQ to SQL domain service type.</param>
        /// <returns>The type of the data context.</returns>
        private static Type GetContextType(Type domainServiceType)
        {
            Type ltsDomainServiceType = domainServiceType.BaseType;
            while (!ltsDomainServiceType.IsGenericType || ltsDomainServiceType.GetGenericTypeDefinition() != typeof(LinqToSqlDomainService<>))
            {
                if (ltsDomainServiceType == typeof(object))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        Resource.InvalidMetadataProviderSpecification,
                        typeof(LinqToSqlDomainServiceDescriptionProviderAttribute).Name, domainServiceType.Name, typeof(LinqToSqlDomainService<>).Name));
                }
                ltsDomainServiceType = ltsDomainServiceType.BaseType;
            }

            return ltsDomainServiceType.GetGenericArguments()[0];
        }
    }
}
