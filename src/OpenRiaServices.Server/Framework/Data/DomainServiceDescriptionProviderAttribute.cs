using System;
using System.Globalization;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> type to specify the <see cref="DomainServiceDescriptionProvider"/>
    /// for the type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DomainServiceDescriptionProviderAttribute : Attribute
    {
        private readonly Type _domainServiceDescriptionProviderType;

        /// <summary>
        /// Initializes a new instance of the DomainServiceDescriptionProviderAttribute class
        /// </summary>
        /// <param name="domainServiceDescriptionProviderType">The <see cref="DomainServiceDescriptionProvider"/> type</param>
        public DomainServiceDescriptionProviderAttribute(Type domainServiceDescriptionProviderType)
        {
            if (domainServiceDescriptionProviderType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceDescriptionProviderType));
            }

            this._domainServiceDescriptionProviderType = domainServiceDescriptionProviderType;
        }

        /// <summary>
        /// Gets the <see cref="DomainServiceDescriptionProvider"/> type
        /// </summary>
        public Type DomainServiceDescriptionProviderType
        {
            get
            {
                return this._domainServiceDescriptionProviderType;
            }
        }

        /// <summary>
        /// Gets a unique identifier for this attribute.
        /// </summary>
        public override object TypeId
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// This method creates an instance of the <see cref="DomainServiceDescriptionProvider"/>. Subclasses can override this
        /// method to provide their own construction logic.
        /// </summary>
        /// <param name="domainServiceType">The <see cref="DomainService"/> type to create a description provider for.</param>
        /// <param name="parent">The parent description provider. May be null.</param>
        /// <returns>The description provider</returns>
        public virtual DomainServiceDescriptionProvider CreateProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceType));
            }

            if (!typeof(DomainService).IsAssignableFrom(domainServiceType))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidType, domainServiceType.FullName, typeof(DomainService).FullName), nameof(domainServiceType));
            }

            if (!typeof(DomainServiceDescriptionProvider).IsAssignableFrom(this._domainServiceDescriptionProviderType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidType, this._domainServiceDescriptionProviderType.FullName, typeof(DomainServiceDescriptionProvider).FullName));
            }

            // Verify the type has a .ctor(Type, DomainServiceDescriptionProvider).
            if (this._domainServiceDescriptionProviderType.GetConstructor(new Type[] { typeof(Type), typeof(DomainServiceDescriptionProvider) }) == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescriptionProviderAttribute_MissingConstructor, this._domainServiceDescriptionProviderType.FullName));
            }

            DomainServiceDescriptionProvider descriptionProvider = (DomainServiceDescriptionProvider)Activator.CreateInstance(
                this._domainServiceDescriptionProviderType, domainServiceType, parent);

            return descriptionProvider;
        }
    }
}
