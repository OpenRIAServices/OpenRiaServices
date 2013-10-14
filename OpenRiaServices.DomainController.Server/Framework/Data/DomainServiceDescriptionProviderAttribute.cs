using System;
using System.Globalization;

namespace OpenRiaServices.DomainController.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainController"/> type to specify the <see cref="DomainControllerDescriptionProvider"/>
    /// for the type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DomainControllerDescriptionProviderAttribute : Attribute
    {
        private Type _DomainControllerDescriptionProviderType;

        /// <summary>
        /// Initializes a new instance of the DomainControllerDescriptionProviderAttribute class
        /// </summary>
        /// <param name="DomainControllerDescriptionProviderType">The <see cref="DomainControllerDescriptionProvider"/> type</param>
        public DomainControllerDescriptionProviderAttribute(Type DomainControllerDescriptionProviderType)
        {
            if (DomainControllerDescriptionProviderType == null)
            {
                throw new ArgumentNullException("DomainControllerDescriptionProviderType");
            }

            this._DomainControllerDescriptionProviderType = DomainControllerDescriptionProviderType;
        }

        /// <summary>
        /// Gets the <see cref="DomainControllerDescriptionProvider"/> type
        /// </summary>
        public Type DomainControllerDescriptionProviderType
        {
            get
            {
                return this._DomainControllerDescriptionProviderType;
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
        /// This method creates an instance of the <see cref="DomainControllerDescriptionProvider"/>. Subclasses can override this
        /// method to provide their own construction logic.
        /// </summary>
        /// <param name="DomainControllerType">The <see cref="DomainController"/> type to create a description provider for.</param>
        /// <param name="parent">The parent description provider. May be null.</param>
        /// <returns>The description provider</returns>
        public virtual DomainControllerDescriptionProvider CreateProvider(Type DomainControllerType, DomainControllerDescriptionProvider parent)
        {
            if (DomainControllerType == null)
            {
                throw new ArgumentNullException("DomainControllerType");
            }

            if (!typeof(DomainController).IsAssignableFrom(DomainControllerType))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidType, DomainControllerType.FullName, typeof(DomainController).FullName), "DomainControllerType");
            }

            if (!typeof(DomainControllerDescriptionProvider).IsAssignableFrom(this._DomainControllerDescriptionProviderType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidType, this._DomainControllerDescriptionProviderType.FullName, typeof(DomainControllerDescriptionProvider).FullName));
            }

            // Verify the type has a .ctor(Type, DomainControllerDescriptionProvider).
            if (this._DomainControllerDescriptionProviderType.GetConstructor(new Type[] { typeof(Type), typeof(DomainControllerDescriptionProvider) }) == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainControllerDescriptionProviderAttribute_MissingConstructor, this._DomainControllerDescriptionProviderType.FullName));
            }

            DomainControllerDescriptionProvider descriptionProvider = (DomainControllerDescriptionProvider)Activator.CreateInstance(
                this._DomainControllerDescriptionProviderType, DomainControllerType, parent);

            return descriptionProvider;
        }
    }
}
