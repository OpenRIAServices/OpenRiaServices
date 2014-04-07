// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Objects;
using System.Web.Http;
using OpenRiaServices.DomainControllers.Server.Metadata;
using OpenRiaServices.DomainControllers.Server;

namespace OpenRiaServices.DomainControllers.EntityFramework.Metadata
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainController"/> that exposes LINQ to Entities mapped
    /// Types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class LinqToEntitiesMetadataProviderAttribute : MetadataProviderAttribute
    {
        private Type _objectContextType;

        /// <summary>
        /// Default constructor. Using this constructor, the Type of the LINQ To Entities
        /// ObjectContext will be inferred from the <see cref="DomainController"/> the
        /// attribute is applied to.
        /// </summary>
        public LinqToEntitiesMetadataProviderAttribute()
            : base(typeof(LinqToEntitiesMetadataProvider))
        {
        }

        /// <summary>
        /// Constructs an attribute for the specified LINQ To Entities
        /// ObjectContext Type.
        /// </summary>
        /// <param name="objectContextType">The LINQ To Entities ObjectContext Type.</param>
        public LinqToEntitiesMetadataProviderAttribute(Type objectContextType)
            : base(typeof(LinqToEntitiesMetadataProvider))
        {
            _objectContextType = objectContextType;
        }

        /// <summary>
        /// The Linq To Entities ObjectContext Type.
        /// </summary>
        public Type ObjectContextType
        {
            get { return _objectContextType; }
        }

        /// <summary>
        /// This method creates an instance of the <see cref="MetadataProvider"/>.
        /// </summary>
        /// <param name="controllerType">The <see cref="DomainController"/> Type to create a metadata provider for.</param>
        /// <param name="parent">The existing parent metadata provider.</param>
        /// <returns>The metadata provider.</returns>
        public override MetadataProvider CreateProvider(Type controllerType, MetadataProvider parent)
        {
            if (controllerType == null)
            {
                throw Error.ArgumentNull("controllerType");
            }

            if (_objectContextType == null)
            {
                _objectContextType = GetContextType(controllerType);
            }

            if (!typeof(ObjectContext).IsAssignableFrom(_objectContextType))
            {
                throw Error.InvalidOperation(Resource.InvalidLinqToEntitiesMetadataProviderSpecification, _objectContextType);
            }

            return new LinqToEntitiesMetadataProvider(_objectContextType, parent, false);
        }

        /// <summary>
        /// Extracts the context type from the specified <paramref name="DomainControllerType"/>.
        /// </summary>
        /// <param name="DomainControllerType">A LINQ to Entities data controller type.</param>
        /// <returns>The type of the object context.</returns>
        private static Type GetContextType(Type DomainControllerType)
        {
            Type efDomainControllerType = DomainControllerType.BaseType;
            while (!efDomainControllerType.IsGenericType || efDomainControllerType.GetGenericTypeDefinition() != typeof(LinqToEntitiesDomainController<>))
            {
                if (efDomainControllerType == typeof(object))
                {
                    throw Error.InvalidOperation(Resource.InvalidMetadataProviderSpecification, typeof(LinqToEntitiesMetadataProviderAttribute).Name, DomainControllerType.Name, typeof(LinqToEntitiesDomainController<>).Name);
                }
                efDomainControllerType = efDomainControllerType.BaseType;
            }

            return efDomainControllerType.GetGenericArguments()[0];
        }
    }
}
