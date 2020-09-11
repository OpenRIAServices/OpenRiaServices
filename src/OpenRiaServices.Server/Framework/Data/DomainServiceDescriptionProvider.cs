using System;
using System.ComponentModel;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// A <see cref="DomainServiceDescriptionProvider"/> is used to provide the metadata description of a
    /// <see cref="DomainService"/> and the types and operations it exposes.
    /// </summary>
    /// <remarks>
    /// A <see cref="DomainServiceDescriptionProvider"/> is responsible for creation of the <see cref="DomainServiceDescription"/>
    /// as well as custom <see cref="TypeDescriptor"/>s for types returned from the service. A provider can be declaratively
    /// associated with a service using the <see cref="DomainServiceDescriptionProviderAttribute"/>.
    /// <see cref="DomainServiceDescriptionProvider"/>s are chained together by passing in the parent provider on construction.
    /// </remarks>
    public abstract class DomainServiceDescriptionProvider
    {
        private readonly Type _domainServiceType;
        private readonly DomainServiceDescriptionProvider _parentDescriptionProvider;
        private Func<Type, bool> _isEntityTypeFunc;

        /// <summary>
        /// Protected Constructor
        /// </summary>
        /// <param name="domainServiceType">The <see cref="DomainService"/> type this provider will create a description for.</param>
        /// <param name="parent">The existing parent description provider. May be null.</param>
        protected DomainServiceDescriptionProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceType));
            }

            this._domainServiceType = domainServiceType;
            this._parentDescriptionProvider = parent;
        }

        /// <summary>
        /// Gets the parent description provider.
        /// </summary>
        internal DomainServiceDescriptionProvider ParentProvider
        {
            get
            {
                return this._parentDescriptionProvider;
            }
        }

        /// <summary>
        /// Gets the <see cref="DomainServiceDescription"/> for the <see cref="DomainService"/> Type. Overrides should
        /// call base and either extend the <see cref="DomainServiceDescription"/> returned or use it as input in creating
        /// an entirely new <see cref="DomainServiceDescription"/>.
        /// </summary>
        /// <remarks>
        /// This method can extend the base <see cref="DomainServiceDescription"/> with new operations by calling
        /// <see cref="DomainServiceDescription.AddOperation"/>.
        /// </remarks>
        /// <returns>The <see cref="DomainServiceDescription"/> for the <see cref="DomainService"/> Type.</returns>
        public virtual DomainServiceDescription GetDescription()
        {
            // If we have a parent provider, we need to return its description, otherwise
            // we create must new one.
            if (this._parentDescriptionProvider != null)
            {
                return this._parentDescriptionProvider.GetDescription();
            }

            return this.CreateDescription();
        }

        /// <summary>
        /// Gets the <see cref="TypeDescriptor"/> for the specified Type, using the specified parent descriptor
        /// as the base. Overrides should call base to ensure the <see cref="TypeDescriptor"/>s are chained properly.
        /// </summary>
        /// <param name="type">The Type to return a descriptor for.</param>
        /// <param name="parent">The parent descriptor.</param>
        /// <returns>The <see cref="TypeDescriptor"/> for the specified Type.</returns>
        public virtual ICustomTypeDescriptor GetTypeDescriptor(Type type, ICustomTypeDescriptor parent)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (this._parentDescriptionProvider != null)
            {
                return this._parentDescriptionProvider.GetTypeDescriptor(type, parent);
            }

            return parent;
        }

        /// <summary>
        /// Determines if the specified <see cref="Type"/> should be considered an entity <see cref="Type"/>.
        /// The base implementation returns <c>false</c>.
        /// </summary>
        /// <remarks>Effectively, the return from this method is this provider's vote as to whether the specified
        /// Type is an entity. The votes from this provider and all other providers in the chain are used
        /// by <see cref="IsEntityType"/> to make it's determination.</remarks>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns>Returns <c>true</c> if the <see cref="Type"/> should be considered an entity,
        /// <c>false</c> otherwise.</returns>
        public virtual bool LookupIsEntityType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return false;
        }

        /// <summary>
        /// This method is called after the <see cref="DomainServiceDescription"/> has been created, and allows
        /// additional metadata to be added to the specified operation. Overrides should call base to get the
        /// initial set of attributes, and any additional attributes should be added to those.
        /// </summary>
        /// <param name="operation">The operation to return attributes for.</param>
        /// <returns>The operation attributes.</returns>
        public virtual AttributeCollection GetOperationAttributes(DomainOperationEntry operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (this._parentDescriptionProvider != null)
            {
                return this._parentDescriptionProvider.GetOperationAttributes(operation);
            }

            return operation.Attributes;
        }

        /// <summary>
        /// Determines if the specified <see cref="Type"/> is an entity <see cref="Type"/> by consulting
        /// the <see cref="LookupIsEntityType"/> method of all <see cref="DomainServiceDescriptionProvider"/>s
        /// in the provider chain for the <see cref="DomainService"/> being described.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns>Returns <c>true</c> if the <see cref="Type"/> is an entity, <c>false</c> otherwise.</returns>
        protected internal bool IsEntityType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (this._isEntityTypeFunc != null)
            {
                return this._isEntityTypeFunc(type);
            }

            return false;
        }

        /// <summary>
        /// Sets the internal entity lookup function for this provider. The function consults
        /// the entire provider chain to make its determination.
        /// </summary>
        /// <param name="isEntityTypeFunc">The entity function.</param>
        internal void SetIsEntityTypeFunc(Func<Type, bool> isEntityTypeFunc)
        {
            this._isEntityTypeFunc = isEntityTypeFunc;
        }

        /// <summary>
        /// Factory method used to create an empty <see cref="DomainServiceDescription"/>.
        /// </summary>
        /// <returns>A new description.</returns>
        protected DomainServiceDescription CreateDescription()
        {
            return new DomainServiceDescription(this._domainServiceType);
        }

        /// <summary>
        /// Factory method used to create <see cref="DomainServiceDescription"/> based on the specified description.
        /// </summary>
        /// <param name="baseDescription">The base description.</param>
        /// <returns>A new description based on the base description.</returns>
        protected DomainServiceDescription CreateDescription(DomainServiceDescription baseDescription)
        {
            if (baseDescription == null)
            {
                throw new ArgumentNullException(nameof(baseDescription));
            }
            return new DomainServiceDescription(baseDescription);
        }
    }   
}
