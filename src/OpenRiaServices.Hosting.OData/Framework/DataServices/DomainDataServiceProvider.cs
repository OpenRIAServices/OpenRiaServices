using System;

namespace OpenRiaServices.Hosting.OData
{
    #region Namespaces
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Services.Providers;
    using System.Diagnostics;
    using System.Linq;
    using OpenRiaServices.Server;

    #endregion

    /// <summary>
    /// Data Service provider implementation for a domain service.
    /// </summary>
    internal class DomainDataServiceProvider : IDataServiceMetadataProvider, IDataServiceQueryProvider
    {
        /// <summary>Metadata information.</summary>
        private readonly DomainDataServiceMetadata metadata;

        /// <summary>Constructs the data service provider object.</summary>
        /// <param name="domainServiceDataServiceMetadata">Metadata information for the current instance.</param>
        /// <param name="result">Root queryable for current request.</param>
        internal DomainDataServiceProvider(DomainDataServiceMetadata domainServiceDataServiceMetadata, object result)
        {
            this.metadata = domainServiceDataServiceMetadata;
            this.CurrentDataSource = new object();
            this.Result = result;
        }

        /// <summary>Root queryable object.</summary>
        internal object Result
        {
            get;
            private set;
        }

        #region IDataServiceMetadataProvider

        /// <summary>Namespace name for the EDM container.</summary>
        public string ContainerNamespace
        {
            get
            {
                Debug.Assert(!string.IsNullOrEmpty(this.metadata.ContainerNamespace), "!string.IsNullOrEmpty(this.metadata.ContainerNamespace)");
                return this.metadata.ContainerNamespace;
            }
        }

        /// <summary>Name of the EDM container</summary>
        public string ContainerName
        {
            get
            {
                Debug.Assert(!string.IsNullOrEmpty(this.metadata.ContainerName), "!string.IsNullOrEmpty(this.metadata.ContainerName)");
                return this.metadata.ContainerName;
            }
        }

        /// <summary>Gets all available resource containers.</summary>
        public IEnumerable<ResourceSet> ResourceSets
        {
            get
            {
                return this.metadata.Sets.Values;
            }
        }

        /// <summary>Returns all the types in this data source.</summary>
        public IEnumerable<ResourceType> Types
        {
            get
            {
                return this.metadata.Types.Values;
            }
        }

        /// <summary>Returns all the service operations in this data source.</summary>
        public IEnumerable<ServiceOperation> ServiceOperations
        {
            get
            {
                return this.metadata.ServiceOperations.Values;
            }
        }

        /// <summary>Given the specified name, tries to find a resource set.</summary>
        /// <param name="name">Name of the resource set to resolve.</param>
        /// <param name="resourceSet">Returns the resolved resource set, null if no resource set for the given name was found.</param>
        /// <returns>True if resource set with the given name was found, false otherwise.</returns>
        public bool TryResolveResourceSet(string name, out ResourceSet resourceSet)
        {
            foreach (ResourceSet s in this.metadata.Sets.Values)
            {
                if (s.Name == name)
                {
                    resourceSet = s;
                    return true;
                }
            }

            resourceSet = null;
            return false;
        }

        /// <summary>
        /// Gets the ResourceAssociationSet instance when given the source association end.
        /// </summary>
        /// <param name="resourceSet">Resource set of the source association end.</param>
        /// <param name="resourceType">Resource type of the source association end.</param>
        /// <param name="resourceProperty">Resource property of the source association end.</param>
        /// <returns>ResourceAssociationSet instance.</returns>
        public ResourceAssociationSet GetResourceAssociationSet(ResourceSet resourceSet, ResourceType resourceType, ResourceProperty resourceProperty)
        {
            return null;
        }

        /// <summary>Given the specified name, tries to find a type.</summary>
        /// <param name="name">Name of the type to resolve.</param>
        /// <param name="resourceType">Returns the resolved resource type, null if no resource type for the given name was found.</param>
        /// <returns>True if we found the resource type for the given name, false otherwise.</returns>
        public bool TryResolveResourceType(string name, out ResourceType resourceType)
        {
            foreach (ResourceType t in this.metadata.Types.Values)
            {
                if (t.FullName == name)
                {
                    resourceType = t;
                    return true;
                }
            }

            resourceType = null;
            return false;
        }

        /// <summary>
        /// The method must return a collection of all the types derived from <paramref name="resourceType"/>.
        /// The collection returned should NOT include the type passed in as a parameter.
        /// An implementer of the interface should return null if the type does not have any derived types (ie. null == no derived types).
        /// </summary>
        /// <param name="resourceType">Resource to get derived resource types from.</param>
        /// <returns>
        /// A collection of resource types (<see cref="ResourceType"/>) derived from the specified <paramref name="resourceType"/> 
        /// or null if there no types derived from the specified <paramref name="resourceType"/> exist.
        /// </returns>
        public IEnumerable<ResourceType> GetDerivedTypes(ResourceType resourceType)
        {
            var derivedTypes = this.metadata.DerivedTypes[resourceType];
            if (derivedTypes != null)
            {
                foreach (ResourceType derivedType in derivedTypes)
                {
                    yield return derivedType;
                    foreach (ResourceType derivedType2 in this.GetDerivedTypes(derivedType))
                    {
                        yield return derivedType2;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if <paramref name="resourceType"/> represents an Entity Type which has derived Entity Types, else false.
        /// </summary>
        /// <param name="resourceType">instance of the resource type in question.</param>
        /// <returns>True if <paramref name="resourceType"/> represents an Entity Type which has derived Entity Types, else false.</returns>
        public bool HasDerivedTypes(ResourceType resourceType)
        {
            return this.metadata.DerivedTypes[resourceType] != null;
        }

        /// <summary>Given the specified name, tries to find a service operation.</summary>
        /// <param name="name">Name of the service operation to resolve.</param>
        /// <param name="serviceOperation">Returns the resolved service operation, null if no service operation was found for the given name.</param>
        /// <returns>True if we found the service operation for the given name, false otherwise.</returns>
        public bool TryResolveServiceOperation(string name, out ServiceOperation serviceOperation)
        {
            DomainDataServiceMetadata.DomainDataServiceOperation op;
            bool result = this.metadata.ServiceOperations.TryGetValue(name, out op);
            serviceOperation = op;
            return result;
        }

        #endregion

        #region IDataServiceQueryProvider

        /// <summary>The data source from which data is provided.</summary>
        public object CurrentDataSource
        {
            get;
            set;
        }

        /// <summary>Gets a value indicating whether null propagation is required in expression trees.</summary>
        public bool IsNullPropagationRequired
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the IQueryable that represents the resource set.
        /// </summary>
        /// <param name="resourceSet">resource set representing the entity set.</param>
        /// <returns>
        /// An IQueryable that represents the set; null if there is 
        /// no set for the specified name.
        /// </returns>
        public IQueryable GetQueryRootForResourceSet(ResourceSet resourceSet)
        {
            Debug.Assert(this.Result != null, "this.Result != null");
            return (this.Result as IEnumerable).AsQueryable();
        }

        /// <summary>Gets the <see cref="ResourceType"/> for the specified <paramref name="target"/>.</summary>
        /// <param name="target">Target instance to extract a <see cref="ResourceType"/> from.</param>
        /// <returns>The <see cref="ResourceType"/> that describes this <paramref name="target"/> in this provider.</returns>
        public ResourceType GetResourceType(object target)
        {
            bool isEnumerable;
            return this.metadata.ResolveResourceType(target.GetType(), out isEnumerable);
        }

        /// <summary>
        /// Get the value of the strongly typed property.
        /// </summary>
        /// <param name="target">instance of the type declaring the property.</param>
        /// <param name="resourceProperty">resource property describing the property.</param>
        /// <returns>value for the property.</returns>
        public object GetPropertyValue(object target, ResourceProperty resourceProperty)
        {
            return this.metadata.GetPropertyValue(target, resourceProperty);
        }

        /// <summary>
        /// Get the value of the open property.
        /// </summary>
        /// <param name="target">instance of the type declaring the open property.</param>
        /// <param name="propertyName">name of the open property.</param>
        /// <returns>value for the open property.</returns>
        public object GetOpenPropertyValue(object target, string propertyName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the name and values of all the properties defined in the given instance of an open type.
        /// </summary>
        /// <param name="target">instance of a open type.</param>
        /// <returns>collection of name and values of all the open properties.</returns>
        public IEnumerable<KeyValuePair<string, object>> GetOpenPropertyValues(object target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoke the given service operation and returns the results.
        /// </summary>
        /// <param name="serviceOperation">service operation to invoke.</param>
        /// <param name="parameters">value of parameters to pass to the service operation.</param>
        /// <returns>returns the result of the service operation. If the service operation returns void, then this should return null.</returns>
        public object InvokeServiceOperation(ServiceOperation serviceOperation, object[] parameters)
        {
            // Find the corresponding entry in metadata.
            DomainDataServiceMetadata.DomainDataServiceOperation op = this.metadata
                                                                          .ServiceOperations
                                                                          .Single(e => e.Key == serviceOperation.Name)
                                                                          .Value;

            // Since query operations always return sequence of values even if there is a singleton result, we need
            // to extract the value out of the single element comtaining sequence and hand that over to the data
            // service runtime.
            if (this.Result != null &&
                op.OperationKind == DomainOperation.Query &&
                op.ResultKind == ServiceOperationResultKind.DirectValue && 
                op.ResourceSet != null)
            {
                return (this.Result as IEnumerable<object>).Single();
            }

            return this.Result;
        }

        #endregion
    }
}