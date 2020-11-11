using System;

namespace OpenRiaServices.Hosting.WCF.OData
{
    #region Namespace
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Services.Providers;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using OpenRiaServices.Server;

    #endregion

    /// <summary>Infers data service metadata from domain service description and caches it.</summary>
    internal class DomainDataServiceMetadata
    {
        /// <summary>
        /// Container name.
        /// </summary>
        private string containerName;

        /// <summary>
        /// Container namespace.
        /// </summary>
        private string containerNamespace;

        /// <summary>
        /// Resource types.
        /// </summary>
        private Dictionary<Type, ResourceType> types;

        /// <summary>
        /// Information about each resource type's derived types.
        /// </summary>
        private Dictionary<ResourceType, List<ResourceType>> derivedTypes;

        /// <summary>
        /// Resource sets.
        /// </summary>
        private Dictionary<string, ResourceSet> sets;

        /// <summary>
        /// Mapping b/w resource types and their property descriptors.
        /// </summary>
        private Dictionary<ResourceType, Dictionary<ResourceProperty, PropertyDescriptor>> resourceProperties;

        /// <summary>
        /// Service operations.
        /// </summary>
        private Dictionary<string, DomainDataServiceOperation> serviceOperations;

        /// <summary>
        /// Constructs and instance of metadata holder for the domain data service.
        /// </summary>
        /// <param name="domainServiceDescription">Domain service description.</param>
        internal DomainDataServiceMetadata(DomainServiceDescription domainServiceDescription)
        {
            Debug.Assert(domainServiceDescription.Attributes[typeof(EnableClientAccessAttribute)] != null, "EnableClientAccess attribute must be on the domain service.");
            this.LoadDomainServiceDataServiceMetadata(domainServiceDescription);
        }

        /// <summary>
        /// Namespace of the container.
        /// </summary>
        internal string ContainerNamespace
        {
            get
            {
                return this.containerNamespace;
            }
        }

        /// <summary>
        /// Name of the container.
        /// </summary>
        internal string ContainerName
        {
            get
            {
                return this.containerName;
            }
        }

        /// <summary>
        /// Resource sets exposed by domain data service.
        /// The key-value pair corresponds to the name of the default query operation and the corresponding resource set.
        /// </summary>
        internal IDictionary<string, ResourceSet> Sets
        {
            get
            {
                return this.sets;
            }
        }

        /// <summary>
        /// Types exposed by domain data service.
        /// </summary>
        internal IDictionary<Type, ResourceType> Types
        {
            get
            {
                return this.types;
            }
        }

        /// <summary>
        /// Type derivation information for each resource type.
        /// </summary>
        internal IDictionary<ResourceType, List<ResourceType>> DerivedTypes
        {
            get
            {
                return this.derivedTypes;
            }
        }

        /// <summary>
        /// Service operations exposed by domain data service.
        /// </summary>
        internal IDictionary<string, DomainDataServiceOperation> ServiceOperations
        {
            get
            {
                return this.serviceOperations;
            }
        }

        /// <summary>
        /// Given an input type, detects the corresponding resource type.
        /// </summary>
        /// <param name="targetType">Input type.</param>
        /// <param name="isEnumeration">Returns true if the <paramref name="targetType"/> is IEnumerable or IQueryable.</param>
        /// <returns>Corresponding resource type or null.</returns>
        internal ResourceType ResolveResourceType(Type targetType, out bool isEnumeration)
        {
            ResourceType resourceType;
            isEnumeration = false;

            if (!this.types.TryGetValue(targetType, out resourceType))
            {
                // Must test for primitive types before testing for IEnumerable or IQueryable since
                // some primitive types such as string also implement those APIs.
                resourceType = ResourceType.GetPrimitiveResourceType(targetType);
                if (resourceType == null)
                {
                    Type t = TypeUtils.GetIQueryableElement(targetType);
                    if (t == null)
                    {
                        t = TypeUtils.GetIEnumerableElement(targetType);
                    }
                    
                    if (t != null)
                    {
                        isEnumeration = true;
                        resourceType = ResourceType.GetPrimitiveResourceType(t);
                        
                        if (resourceType == null)
                        {
                            this.types.TryGetValue(t, out resourceType);
                        }
                    }
                }
            }

            return resourceType;
        }

        /// <summary>
        /// Obtains the property value using PropertyDescriptor corresponding to a ResourceProperty.
        /// </summary>
        /// <param name="target">Instance whose property is being requested.</param>
        /// <param name="resourceProperty">Property to be read.</param>
        /// <returns>Value of <paramref name="resourceProperty"/>.</returns>
        internal object GetPropertyValue(object target, ResourceProperty resourceProperty)
        {
            Debug.Assert(this.types.ContainsKey(target.GetType()), "Type must exist in the available types collection.");
            Debug.Assert(this.resourceProperties.ContainsKey(this.types[target.GetType()]), "Mapping for the target type must exist.");
            Debug.Assert(this.resourceProperties[this.types[target.GetType()]].ContainsKey(resourceProperty), "Resource property must have mapping to a PropertyDescriptor.");
            return this.resourceProperties[this.types[target.GetType()]][resourceProperty].GetValue(target);
        }

        /// <summary>
        /// Checks if the given operation is a candidate for resource set.
        /// </summary>
        /// <param name="operationEntry">Input operation entry.</param>
        /// <returns>true if the operation has no params and returns a sequence, false otherwise.</returns>
        private static bool IsRootQueryOperation(DomainOperationEntry operationEntry)
        {
            Debug.Assert(operationEntry.Operation == DomainOperation.Query, "Only expecting query operations.");
            Debug.Assert(operationEntry.OperationAttribute != null && operationEntry.OperationAttribute is QueryAttribute, "operationEntry.OperationAttribute != null && operationEntry.OperationAttribute is QueryAttribute");
            return (operationEntry.OperationAttribute as QueryAttribute).IsDefault;
        }

        /// <summary>
        /// Gets the name from property descriptor based on DataMember attribute.
        /// </summary>
        /// <param name="pi">Property descriptor.</param>
        /// <returns>Name of the property.</returns>
        private static string GetNameFromPropertyDescriptor(PropertyDescriptor pi)
        {
            string propertyName = pi.Name;

            if (TypeUtils.IsDataMember(pi))
            {
                DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)pi.Attributes[typeof(DataMemberAttribute)];
                if (!String.IsNullOrEmpty(dataMemberAttribute.Name))
                {
                    propertyName = dataMemberAttribute.Name;
                }
            }

            return propertyName;
        }

        /// <summary>
        /// Loads the domain service metadata based on domain service description.
        /// </summary>
        /// <param name="domainServiceDescription">Domain service description.</param>
        private void LoadDomainServiceDataServiceMetadata(DomainServiceDescription domainServiceDescription)
        {
            this.containerName = domainServiceDescription.DomainServiceType.Name;

            // Note that in WCF Data Services V1, the reflection provider can return null for ContainerNamespace.
            // When that happens, ContainerName is used as the ContainerNamespce during metadata serialization.
            // For IDSP providers, WCF Data Services require that the ContainerNamespece cannot be null or empty, so
            // we follow the same convention of using the ContainerName here if DomainServiceType.Namespace is null.
            this.containerNamespace = domainServiceDescription.DomainServiceType.Namespace ?? this.containerName;

            this.types = new Dictionary<Type, ResourceType>();
            this.sets = new Dictionary<string, ResourceSet>();
            this.derivedTypes = new Dictionary<ResourceType, List<ResourceType>>();
            this.serviceOperations = new Dictionary<string, DomainDataServiceOperation>();
            this.resourceProperties = new Dictionary<ResourceType, Dictionary<ResourceProperty, PropertyDescriptor>>();

            // Collect all the possible resource sets and their corresponding types.
            this.CollectResourceSets(domainServiceDescription);

            // Load properties for all resource types.
            this.LoadResourceTypeProperties();

            // Make all the resource sets and resource types read-only.
            foreach (ResourceSet rs in this.sets.Values)
            {
                rs.SetReadOnly();
            }

            foreach (ResourceType rt in this.types.Values)
            {
                rt.SetReadOnly();
            }

            // Collect all the possible service operations.
            this.CollectServiceOperations(domainServiceDescription);

            // Make all the service operations read-only.
            foreach (ServiceOperation op in this.serviceOperations.Values)
            {
                op.SetReadOnly();
            }
        }

        /// <summary>
        /// Infers resource sets and resource types corresponding to query operations in domain service.
        /// </summary>
        /// <param name="domainServiceDescription">Domain service description.</param>
        private void CollectResourceSets(DomainServiceDescription domainServiceDescription)
        {
            foreach (var operationEntry in domainServiceDescription.DomainOperationEntries)
            {
                // Only composable operations i.e. those that return IEnumerable<T> are expected to represent root resource sets.
                if (operationEntry.Operation == DomainOperation.Query && DomainDataServiceMetadata.IsRootQueryOperation(operationEntry))
                {
                    Type resourceInstanceType = operationEntry.AssociatedType;

                    // There will always be an entity type associated with query operation, we need to make sure that we have all 
                    // the types in this type's hierarchy added to the provider types.
                    ResourceType resourceType = this.CreateResourceTypeHierarchy(domainServiceDescription, resourceInstanceType);

                    foreach (ResourceSet resourceSet in this.sets.Values)
                    {
                        Type resourceSetInstanceType = resourceSet.ResourceType.InstanceType;

                        if (resourceSetInstanceType.IsAssignableFrom(resourceInstanceType) || 
                            resourceInstanceType.IsAssignableFrom(resourceSetInstanceType))
                        {
                            throw new DomainDataServiceException((int)HttpStatusCode.InternalServerError, Resource.DomainDataService_MEST_NotAllowed);
                        }
                    }

                    ResourceSet rs = new ResourceSet(resourceInstanceType.Name + ServiceUtils.ResourceSetPostFix, resourceType);
                    this.sets.Add(operationEntry.Name, rs);
                }
            }
        }

        /// <summary>
        /// Create resource types corresponding to all types in a given CLR type's hierarchy.
        /// </summary>
        /// <param name="domainServiceDescription">Domain service description.</param>
        /// <param name="resourceInstanceType">Type whose hierarchy is being discovered.</param>
        /// <returns>ResourceType for <paramref name="resourceInstanceType"/>.</returns>
        private ResourceType CreateResourceTypeHierarchy(DomainServiceDescription domainServiceDescription, Type resourceInstanceType)
        {
            Debug.Assert(domainServiceDescription.EntityTypes.Contains(resourceInstanceType), "Resource type must be one of allowed entity types.");

            // Handle inheritance scenarios, need to add all the base types as ResourceTypes here, properties to be read after wards.
            Type rootInstanceType = domainServiceDescription.RootEntityTypes.Single(r => r.IsAssignableFrom(resourceInstanceType));

            // Collect all types in the current type's hierarchy and sort them with bases coming before derived types.
            Type[] allInstanceTypes = domainServiceDescription.EntityTypes
                                                                         .Where(e => rootInstanceType.IsAssignableFrom(e))
                                                                         .OrderBy(e => e, new TypeInheritanceComparer())
                                                                         .ToArray();

            for (int currentTypeIdx = 0; currentTypeIdx < allInstanceTypes.Length; currentTypeIdx++)
            {
                Type currentInstanceType = allInstanceTypes[currentTypeIdx];
                ResourceType resourceType;
                ResourceType baseResourceType = null;
                for (int baseTypeIdx = currentTypeIdx - 1; baseTypeIdx >= 0; baseTypeIdx--)
                {
                    if (allInstanceTypes[baseTypeIdx].IsAssignableFrom(currentInstanceType))
                    {
                        this.types.TryGetValue(allInstanceTypes[baseTypeIdx], out baseResourceType);
                        Debug.Assert(baseResourceType != null, "baseResourceType != null");
                        break;
                    }
                }

                if (!this.types.TryGetValue(currentInstanceType, out resourceType))
                {
                    resourceType = new ResourceType(
                        currentInstanceType,
                        ResourceTypeKind.EntityType,
                        baseResourceType,
                        currentInstanceType.Namespace,
                        currentInstanceType.Name,
                        currentInstanceType.IsAbstract);

                    this.types.Add(currentInstanceType, resourceType);

                    // Add resource property mappings.
                    this.resourceProperties.Add(resourceType, new Dictionary<ResourceProperty, PropertyDescriptor>());

                    // Update inheritance information.
                    this.derivedTypes.Add(resourceType, null);

                    if (baseResourceType != null)
                    {
                        Debug.Assert(this.derivedTypes.ContainsKey(baseResourceType), "Must have already added the base type.");
                        if (this.derivedTypes[baseResourceType] == null)
                        {
                            this.derivedTypes[baseResourceType] = new List<ResourceType>();
                        }

                        this.derivedTypes[baseResourceType].Add(resourceType);
                    }
                }
            }

            return this.types[resourceInstanceType];
        }

        /// <summary>
        /// Loads properties for all the resource types.
        /// </summary>
        private void LoadResourceTypeProperties()
        {
#if DEBUG
            // Remember the types we've visited.
            HashSet<ResourceType> visitedType = new HashSet<ResourceType>(EqualityComparer<ResourceType>.Default);
#endif
            foreach (var resourceType in this.types.Values)
            {
                Type type = resourceType.InstanceType;
                ResourceType parentResourceType = resourceType.BaseType;
#if DEBUG
                Debug.Assert(parentResourceType == null || visitedType.Contains(parentResourceType), "We must always visit the ancestors of a type before visiting that type.");
                visitedType.Add(resourceType);
#endif
                foreach (PropertyDescriptor pi in TypeDescriptor.GetProperties(type))
                {
                    if (!TypeUtils.IsSerializableDataMember(pi))
                    {
                        continue;
                    }

                    // Only primitive properties are currently supported.
                    ResourceType propertyType = ResourceType.GetPrimitiveResourceType(pi.PropertyType);
                    
                    if (null != propertyType)
                    {
                        ResourceProperty rp = parentResourceType == null ?
                            null :
                            this.GetResourceProperty(parentResourceType, DomainDataServiceMetadata.GetNameFromPropertyDescriptor(pi));

                        if (rp == null)
                        {
                            ResourcePropertyKind kind = ResourcePropertyKind.Primitive;
                            if (TypeUtils.IsKeyProperty(pi))
                            {
                                kind |= ResourcePropertyKind.Key;
                            }

                            rp = new ResourceProperty(
                                DomainDataServiceMetadata.GetNameFromPropertyDescriptor(pi),
                                kind,
                                propertyType)
                            {
                                CanReflectOnInstanceTypeProperty = false
                            };

                            // We only need to add rp to the resourceType if rp is not defined on an ancestor type.
                            resourceType.AddProperty(rp);
                        }

                        // We always add rp to this.resourceProperties map even if the property is defined on an ancestor type.
                        this.AddResourcePropertyDescriptor(resourceType, rp, pi);
                    }
                }
            }

#if DEBUG
            foreach (var entry in this.resourceProperties)
            {
                Debug.Assert(entry.Value.Count > 0, "A ResourceType cannot have 0 properties, it should have at least its key property.");
            }
#endif
        }

        /// <summary>
        /// Add to the collection of property descriptors for given resource type.
        /// </summary>
        /// <param name="rt">ResourceType for which to add descriptor.</param>
        /// <param name="rp">ResourceProperty to which PropertyDescriptor corresponds.</param>
        /// <param name="pi">PropertyDescriptor to add.</param>
        private void AddResourcePropertyDescriptor(ResourceType rt, ResourceProperty rp, PropertyDescriptor pi)
        {
            Debug.Assert(rt != null, "rt != null");
            Debug.Assert(rp != null, "rp != null");
            Debug.Assert(pi != null, "pi != null");
            Dictionary<ResourceProperty, PropertyDescriptor> resourcePropertyDescriptors = this.resourceProperties[rt];
            Debug.Assert(resourcePropertyDescriptors != null, "resourcePropertyDescriptors != null");
            resourcePropertyDescriptors.Add(rp, pi);
        }

        /// <summary>
        /// Get the resource property from a type with the maching name
        /// </summary>
        /// <param name="resourceType">Resource type for which to look up the property</param>
        /// <param name="propertyName">Property name to look up</param>
        /// <returns>The resource property.</returns>
        private ResourceProperty GetResourceProperty(ResourceType resourceType, string propertyName)
        {
            Dictionary<ResourceProperty, PropertyDescriptor> resourcePropertyDescriptors = this.resourceProperties[resourceType];
            Debug.Assert(resourcePropertyDescriptors != null, "resourcePropertyDescriptors != null");
            return resourcePropertyDescriptors.Keys.SingleOrDefault(p => p.Name == propertyName);
        }

        /// <summary>
        /// Infers service operations from domain service description.
        /// </summary>
        /// <param name="domainServiceDescription">Domain service description.</param>
        private void CollectServiceOperations(DomainServiceDescription domainServiceDescription)
        {
            foreach (var operationEntry in domainServiceDescription.DomainOperationEntries)
            {
                DomainDataServiceOperation op = null;

                if ((operationEntry.Operation == DomainOperation.Query && !DomainDataServiceMetadata.IsRootQueryOperation(operationEntry)) ||
                    operationEntry.Operation == DomainOperation.Invoke)
                {
                    op = this.CreateServiceOperation(operationEntry);
                }

                if (op != null)
                {
                    this.serviceOperations.Add(op.Name, op);
                }
            }
        }

        /// <summary>
        /// Create a service operation based on a domain operation entry.
        /// </summary>
        /// <param name="operation">Domain operation corresponding to the service operation.</param>
        /// <returns>ServiceOperation instance mapping to the domain operation.</returns>
        private DomainDataServiceOperation CreateServiceOperation(DomainOperationEntry operation)
        {
            List<ServiceOperationParameter> operationParameters = new List<ServiceOperationParameter>();

            foreach (DomainOperationParameter p in operation.Parameters)
            {
                // Only allow primitive type parameters for service operations.
                ResourceType parameterType = ResourceType.GetPrimitiveResourceType(p.ParameterType);
                if (parameterType == null)
                {
                    return null;
                }

                operationParameters.Add(new ServiceOperationParameter(p.Name, parameterType));
            }

            ServiceOperationResultKind resultKind = ServiceOperationResultKind.Void;
            ResourceType resultResourceType = null;

            if (operation.ReturnType != null)
            {
                bool isEnumeration;
                resultResourceType = this.ResolveResourceType(operation.ReturnType, out isEnumeration);

                if (resultResourceType == null)
                {
                    // Ignore service operations for which the appropriate resource type could not be inferred.
                    return null;
                }

                if (isEnumeration)
                {
                    // Will need to distinguish between ServiceOperationResultKind.Enumeration and
                    // ServiceOperationResultKind.QueryWithMultipleResults once we support composition.
                    resultKind = ServiceOperationResultKind.Enumeration;
                }
                else
                {
                    resultKind = ServiceOperationResultKind.DirectValue;
                }
            }

            bool hasSideEffects = false;

            switch (operation.Operation)
            {
                case DomainOperation.Invoke:
                    hasSideEffects = (operation.OperationAttribute as InvokeAttribute).HasSideEffects;
                    break;
                case DomainOperation.Query:
                    hasSideEffects = (operation.OperationAttribute as QueryAttribute).HasSideEffects;
                    break;
                default:
                    break;
            }

            ResourceSet resultSet = null;
            if (resultResourceType.ResourceTypeKind == ResourceTypeKind.EntityType)
            {
                resultSet = this.sets.Values.SingleOrDefault(s => s.ResourceType.InstanceType.IsAssignableFrom(resultResourceType.InstanceType));
                if (resultSet == null)
                {
                    // Only support those service query operations which have a corresponding entity set.
                    return null;
                }
            }

            DomainDataServiceOperation op = new DomainDataServiceOperation(
                operation.Name,
                resultKind,
                resultResourceType,
                resultSet,
                hasSideEffects ? ServiceUtils.HttpPostMethodName : ServiceUtils.HttpGetMethodName,
                operationParameters,
                operation.Operation);

            return op;
        }

        /// <summary>WCF Data Service operation with domain operation information.</summary>
        internal class DomainDataServiceOperation : ServiceOperation
        {
            /// <summary>
            /// Initializes a new <see cref="DomainDataServiceOperation"/> instance.
            /// </summary>
            /// <param name="name">name of the service operation.</param>
            /// <param name="resultKind">Kind of result expected from this operation.</param>
            /// <param name="resultType">Type of element of the method result.</param>
            /// <param name="resultSet">EntitySet of the result expected from this operation.</param>
            /// <param name="method">Protocol (for example HTTP) method the service operation responds to.</param>
            /// <param name="parameters">In-order parameters for this operation.</param>
            /// <param name="operationKind">Kind of domain service operation.</param>
            internal DomainDataServiceOperation(
                string name,
                ServiceOperationResultKind resultKind,
                ResourceType resultType,
                ResourceSet resultSet,
                string method,
                IEnumerable<ServiceOperationParameter> parameters,
                DomainOperation operationKind) : base(name, resultKind, resultType, resultSet, method, parameters)
            {
                this.OperationKind = operationKind;
            }

            internal DomainOperation OperationKind
            {
                get;
                private set;
            }
        }

        /// <summary>
        /// Compares two types based on inheritance relationship.
        /// </summary>
        private class TypeInheritanceComparer : IComparer<Type>
        {
            /// <summary>
            /// Compares two types based on inheritance relationship.
            /// </summary>
            /// <param name="x">Left type.</param>
            /// <param name="y">Right type.</param>
            /// <returns>0 if equal, -1 if left is base of right, 1 otherwise.</returns>
            public int Compare(Type x, Type y)
            {
                return (x == y) ? 0 : x.IsAssignableFrom(y) ? -1 : 1;
            }
        }
    }    
}
