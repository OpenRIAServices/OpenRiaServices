using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// This class provides a metadata description of a <see cref="DomainService"/> and the
    /// types and operations it exposes.
    /// </summary>
    public sealed class DomainServiceDescription
    {
        private static readonly ConcurrentDictionary<Type, DomainServiceDescription> s_domainServiceMap = new ConcurrentDictionary<Type, DomainServiceDescription>();
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> s_typeDescriptionProviderMap = new ConcurrentDictionary<Type, HashSet<Type>>();
        private static readonly Func<Type, DomainServiceDescription> s_createDescriptionFunc = CreateDescription;

        private readonly Dictionary<Type, Dictionary<DomainOperation, DomainOperationEntry>> _submitMethods = new Dictionary<Type, Dictionary<DomainOperation, DomainOperationEntry>>();
        private readonly Dictionary<Type, Dictionary<string, DomainOperationEntry>> _customMethods = new Dictionary<Type, Dictionary<string, DomainOperationEntry>>();
        private readonly Dictionary<string, DomainOperationEntry> _queryMethods = new Dictionary<string, DomainOperationEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DomainOperationEntry> _invokeOperations = new Dictionary<string, DomainOperationEntry>();
        private readonly Dictionary<Type, List<PropertyDescriptor>> _compositionMap = new Dictionary<Type, List<PropertyDescriptor>>();
        private readonly HashSet<Type> _complexTypes = new HashSet<Type>();
        private readonly HashSet<Type> _entityTypes = new HashSet<Type>();
        private HashSet<Type> _rootEntityTypes;
        private Dictionary<Type, HashSet<Type>> _entityKnownTypes;
        private readonly Type _domainServiceType;
        private bool _isInitializing;
        private bool _isInitialized;
        private AttributeCollection _attributes;
        private readonly List<DomainOperationEntry> _operationEntries = new List<DomainOperationEntry>();
        private readonly ConcurrentDictionary<Type, Type> _exposedTypeMap = new ConcurrentDictionary<Type, Type>();
        private readonly Func<Type, Type> _createSerializationType;

        private DomainServiceDescriptionProvider _descriptionProvider;

        /// <summary>
        /// Constructs a description for the specified <see cref="DomainService"/> Type.
        /// </summary>
        /// <param name="domainServiceType">The Type of the DomainService</param>
        internal DomainServiceDescription(Type domainServiceType)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceType));
            }

            this._domainServiceType = domainServiceType;
            this._createSerializationType = (Type type) => this.CreateSerializationType(type);
        }

        /// <summary>
        /// Constructs a description based on the specified <see cref="DomainServiceDescription"/>.
        /// </summary>
        /// <param name="baseDescription">The base <see cref="DomainServiceDescription"/></param>
        internal DomainServiceDescription(DomainServiceDescription baseDescription)
        {
            if (baseDescription == null)
            {
                throw new ArgumentNullException(nameof(baseDescription));
            }

            this._domainServiceType = baseDescription._domainServiceType;
            this._attributes = baseDescription._attributes;
            this._operationEntries.AddRange(baseDescription._operationEntries);
            this._createSerializationType = (Type type) => this.CreateSerializationType(type);
        }

        /// <summary>
        /// Gets the cache associating entity types with the types
        /// identified with <see cref="KnownTypeAttribute"/>
        /// </summary>
        /// <value>
        /// The result is a dictionary, keyed by entity type and containing
        /// the set of other entity types that were declared via a
        /// <see cref="KnownTypeAttribute"/>.  This set contains only entity
        /// types -- extraneous other known types are omitted.  This set also
        /// contains the full closure of known types for every entity type
        /// by rolling up derived type's known types onto their base.
        /// This cache is lazily loaded but stable.  This means that we capture
        /// the list of known types once and are not affected by the semantics
        /// of <see cref="KnownTypeAttribute"/> that permit a runtime method
        /// to return potentially different known types.
        /// </value>
        internal Dictionary<Type, HashSet<Type>> EntityKnownTypes
        {
            get
            {
                if (this._entityKnownTypes == null)
                {
                    this._entityKnownTypes = this.ComputeEntityKnownTypes();
                }
                return this._entityKnownTypes;
            }
        }

        /// <summary>
        /// Gets the complex types exposed by the <see cref="DomainService"/>
        /// </summary>
        public IEnumerable<Type> ComplexTypes
        {
            get
            {
                this.EnsureInitialized();
                return _complexTypes;
            }
        }

        /// <summary>
        /// Gets the Type of the <see cref="DomainService"/>
        /// </summary>
        public Type DomainServiceType
        {
            get
            {
                return this._domainServiceType;
            }
        }

        /// <summary>
        /// Gets the entity types exposed by the <see cref="DomainService"/>
        /// </summary>
        public IEnumerable<Type> EntityTypes
        {
            get
            {
                this.EnsureInitialized();
                return _entityTypes;
            }
        }

        /// <summary>
        /// Gets all the root entity types exposed by the <see cref="DomainService"/>
        /// </summary>
        /// <remarks>A 'root entity type' is the least derived entity type within an
        /// entity hierarchy exposed by a <see cref="DomainService"/>.  It need not be the
        /// actual base type of the hierarchy.
        /// </remarks>
        public IEnumerable<Type> RootEntityTypes
        {
            get
            {
                // Return snapshot
                return this.GetOrCreateRootEntityTypes().ToArray();
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="DomainOperationEntry"/> items for the <see cref="DomainService"/>.
        /// </summary>
        public IEnumerable<DomainOperationEntry> DomainOperationEntries
        {
            get
            {
                return this._operationEntries.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets or sets a collection of attributes for <see cref="DomainService"/> Type.
        /// </summary>
        /// <remarks>This includes attributes decorated on the Type directly as well as attributes surfaced 
        /// from implemented interfaces.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Public setter by design. This is an extensibility point.")]
        public AttributeCollection Attributes
        {
            get
            {
                if (this._attributes == null)
                {
                    this._attributes = new AttributeCollection();
                }

                return this._attributes;
            }
            set
            {
                this.CheckInvalidUpdate();
                this._attributes = value;
            }
        }

        /// <summary>
        /// Add the specified operation to this description.
        /// </summary>
        /// <remarks>
        /// This method can only be called during construction of the <see cref="DomainServiceDescription"/> before
        /// it has been completely initialized. This is done in advanced extensibility scenarios involving custom
        /// <see cref="DomainServiceDescriptionProvider"/>s.
        /// </remarks>
        /// <param name="operation">The operation to add.</param>
        public void AddOperation(DomainOperationEntry operation)
        {
            this.CheckInvalidUpdate();

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (operation.DomainServiceType != this.DomainServiceType)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_IncompatibleOperationParentType, operation.Name, this.DomainServiceType, operation.DomainServiceType), nameof(operation));
            }

            if (!this._operationEntries.Contains(operation))
            {
                this._operationEntries.Add(operation);
            }
        }

        /// <summary>
        /// Returns the <see cref="DomainService"/> query method of the specified name
        /// </summary>
        /// <param name="queryName">The name of the query </param>
        /// <returns>DomainOperationEntry for the specified query name</returns>
        public DomainOperationEntry GetQueryMethod(string queryName)
        {
            this.EnsureInitialized();

            if (string.IsNullOrEmpty(queryName))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntry_ArgumentCannotBeNullOrEmpty, "queryName"));
            }

            DomainOperationEntry method = null;
            this._queryMethods.TryGetValue(queryName, out method);

            return method;
        }

        /// <summary>
        /// Returns the <see cref="DomainService"/> invoke operation of the specified name.
        /// </summary>
        /// <param name="operationName">The name of the operation</param>
        /// <returns><see cref="DomainOperationEntry"/> for the specified operation name.</returns>
        public DomainOperationEntry GetInvokeOperation(string operationName)
        {
            this.EnsureInitialized();

            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntry_ArgumentCannotBeNullOrEmpty, "operationName"));
            }

            DomainOperationEntry method = null;
            this._invokeOperations.TryGetValue(operationName, out method);

            return method;
        }

        /// <summary>
        /// Returns the <see cref="DomainService"/> custom method of the specified name associated with the specified entity type
        /// </summary>
        /// <param name="entityType">The entity type the custom method is associated with</param>
        /// <param name="methodName">The name of the custom method</param>
        /// <returns><see cref="DomainOperationEntry"/> for the custom method if found, null otherwise</returns>
        public DomainOperationEntry GetCustomMethod(Type entityType, string methodName)
        {
            this.EnsureInitialized();

            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntry_ArgumentCannotBeNullOrEmpty, "methodName"));
            }

            DomainOperationEntry method = null;
            for (Type baseType = entityType; baseType != null; baseType = baseType.BaseType)
            {
                Dictionary<string, DomainOperationEntry> entityCustomMethods = null;
                if (this._customMethods.TryGetValue(baseType, out entityCustomMethods))
                {
                    // Break only if we find it walking back the hierarchy.
                    // This may be a domain operation invoked on a derived type 
                    // but declared on the base type
                    if (entityCustomMethods.TryGetValue(methodName, out method))
                    {
                        break;
                    }
                }
            }

            return method;
        }

        /// <summary>
        /// Returns the collection of custom methods defined for the given entity type
        /// </summary>
        /// <param name="entityType">The entity type associated with the custom methods</param>
        /// <returns>The collection of custom methods defined for the given entity type</returns>
        public IEnumerable<DomainOperationEntry> GetCustomMethods(Type entityType)
        {
            this.EnsureInitialized();

            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            Dictionary<string, DomainOperationEntry> entityCustomMethods = null;
            if (this._customMethods.TryGetValue(entityType, out entityCustomMethods))
            {
                return entityCustomMethods.Values.ToList();
            }

            return Array.Empty<DomainOperationEntry>();
        }

        /// <summary>
        /// Gets the submit method for the specified entity type and <see cref="DomainOperation"/>
        /// </summary>
        /// <param name="entityType">The entity type</param>ad of new
        /// <param name="operation">The <see cref="DomainOperation"/> type to get the method for. Must be
        /// an Insert, Update or Delete operation.</param>
        /// <returns>The method if it exists, otherwise null</returns>
        public DomainOperationEntry GetSubmitMethod(Type entityType, DomainOperation operation)
        {
            this.EnsureInitialized();

            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }
            if ((operation != DomainOperation.Insert) &&
                (operation != DomainOperation.Update) &&
                (operation != DomainOperation.Delete))
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }

            for (Type baseType = entityType; baseType != typeof(object) && baseType != null; baseType = baseType.BaseType)
            {
                Dictionary<DomainOperation, DomainOperationEntry> entitySubmitMethods = null;
                if (this._submitMethods.TryGetValue(baseType, out entitySubmitMethods))
                {
                    DomainOperationEntry submitMethod = null;
                    if (entitySubmitMethods.TryGetValue(operation, out submitMethod))
                    {
                        return submitMethod;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// If the specified Type is the child of a compositional relationship
        /// this method returns all the compositional associations that compose the
        /// Type.
        /// </summary>
        /// <param name="entityType">The Type to get parent associations for.</param>
        /// <returns>Collection of <see cref="PropertyDescriptor"/>s for each parent association. May be empty.</returns>
        public IEnumerable<PropertyDescriptor> GetParentAssociations(Type entityType)
        {
            this.EnsureInitialized();

            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            List<PropertyDescriptor> assocList = null;
            if (this._compositionMap.TryGetValue(entityType, out assocList))
            {
                return (IEnumerable<PropertyDescriptor>)assocList.AsReadOnly();
            }

            return Enumerable.Empty<PropertyDescriptor>();
        }

        /// <summary>
        /// For the specified type, this method returns the corresponding
        /// Type exposed by the <see cref="DomainService"/>, taking inheritance into account.
        /// </summary>
        /// <remarks>Any serialization operations operating on Types should
        /// call into this method to get the correct Type definition to operate on.</remarks>
        /// <param name="type">The Type of the object.</param>
        /// <returns>The corresponding type exposed by the DomainService, or null
        /// if the specified type is not exposed by the DomainService.</returns>
        public Type GetSerializationType(Type type)
        {
            this.EnsureInitialized();

            return this._exposedTypeMap.GetOrAdd(type, _createSerializationType);
        }

        private Type CreateSerializationType(Type t)
        {
            // Complex types do not support inheritance, return the type.
            if (this._complexTypes.Contains(t))
            {
                return t;
            }

            // The correct type to serialize is the first entity type that is in the
            // list of known entities, walking back the inheritance chain
            Type baseType;
            for (baseType = t; baseType != null; baseType = baseType.BaseType)
            {
                if (this._entityTypes.Contains(baseType))
                {
                    break;
                }
            }

            return baseType;
        }

        /// <summary>
        /// Returns the root type for the given entity type.
        /// </summary>
        /// <remarks>
        /// The root type is the least derived entity type in the entity type
        /// hierarchy that is exposed through a <see cref="DomainService"/>.
        /// </remarks>
        /// <param name="entityType">The entity type whose root is required.</param>
        /// <returns>The type of the root or <c>null</c> if the given <paramref name="entityType"/>
        /// has no base types.</returns>
        public Type GetRootEntityType(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            EnsureInitialized();
            Type rootType = null;
            while (entityType != null)
            {
                if (_entityTypes.Contains(entityType))
                {
                    rootType = entityType;
                }
                entityType = entityType.BaseType;
            }
            return rootType;
        }

        /// <summary>
        /// Gets the base type of the given entity type.
        /// </summary>
        /// <remarks>
        /// The base type is the closest base type of
        /// the given entity type that is exposed by the
        /// <see cref="DomainService"/>. The entity hierarchy
        /// may contain types that are not exposed, and this
        /// method skips those.
        /// </remarks>
        /// <param name="entityType">The entity type whose base type is required.</param>
        /// <returns>The base type or <c>null</c> if the given
        /// <paramref name="entityType"/> had no visible base types.</returns>
        public Type GetEntityBaseType(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            EnsureInitialized();
            Type baseType = entityType.BaseType;
            while (baseType != null)
            {
                if (_entityTypes.Contains(baseType))
                {
                    break;
                }
                baseType = baseType.BaseType;
            }
            return baseType;
        }

        /// <summary>
        /// Returns the <see cref="DomainServiceDescription"/> for the specified <see cref="DomainService"/> Type.
        /// </summary>
        /// <param name="domainServiceType">The Type of <see cref="DomainService"/> to get description for</param>
        /// <returns>The description for the specified <see cref="DomainService"/> type</returns>
        public static DomainServiceDescription GetDescription(Type domainServiceType)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceType));
            }

            if (domainServiceType.IsAbstract
                || domainServiceType.IsGenericType
                || domainServiceType.IsGenericTypeDefinition
                || !typeof(DomainService).IsAssignableFrom(domainServiceType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainService_InvalidType, domainServiceType.FullName));
            }

            return s_domainServiceMap.GetOrAdd(domainServiceType, s_createDescriptionFunc);
        }

        /// <summary>
        /// Validate and initialize the description. Initialize should be called before the description
        /// is used. Only descriptions that have been created manually need to be initialized. Descriptions
        /// returned by the framework are already initialized.
        /// </summary>
        internal void Initialize()
        {
            if (this._isInitialized == true)
            {
                return;
            }
            this._isInitializing = true;

            // First add all query operations, since we need to know the full set of
            // exposed entity types prior to adding the other operation types, and
            // queue up all other operations for later processing.
            List<DomainOperationEntry> queuedOperations = new List<DomainOperationEntry>();
            foreach (DomainOperationEntry operation in this._operationEntries)
            {
                if (operation.Operation == DomainOperation.Query)
                {
                    this.AddQueryMethod(operation);
                }
                else
                {
                    // all other operations can be handled at the
                    // same time
                    queuedOperations.Add(operation);
                }
            }

            // only AFTER all entities have been identified and have had their
            // TDPs registered can we search for complex types
            this.DiscoverComplexTypes();

            // Now process the operations we queued up, in the correct order
            foreach (DomainOperationEntry operation in queuedOperations)
            {
                switch (operation.Operation)
                {
                    case DomainOperation.Custom:
                        this.AddCustomMethod(operation);
                        break;
                    case DomainOperation.Invoke:
                        this.AddInvokeOperation(operation);
                        break;
                    case DomainOperation.Insert:
                    case DomainOperation.Update:
                    case DomainOperation.Delete:
                        this.AddSubmitMethod(operation);
                        break;
                    default:
                        break;
                }
            }

            // After the entity hierarchy is complete, we have sufficient
            // context to detect subclasses of composition children and
            // to locate inherited parent associations.
            this.FixupCompositionMap();

            // only after all custom type description providers have been registered can we
            // perform validations on entities and complex types, since the PropertyDescriptors
            // must include all extension metadata
            this.ValidateEntityTypes();
            this.ValidateComplexTypes();

            this._isInitialized = true;
            this._isInitializing = false;
        }

        /// <summary>
        /// Enumerates all entity types and operations searching for complex types and
        /// adds them to our complex types collection.
        /// <remarks>
        /// Nowhere on this codepath can MetaTypes be accessed for complex types, since we
        /// require ALL complex types to have their TDPs registered before hand.
        /// </remarks>
        /// </summary>
        private void DiscoverComplexTypes()
        {
            // discover and add complex types that are entity properties
            foreach (Type entityType in this.EntityTypes)
            {
                foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(entityType))
                {
                    if (pd.Attributes[typeof(ExternalReferenceAttribute)] != null ||
                        pd.Attributes[typeof(ExcludeAttribute)] != null ||
                        pd.Attributes[typeof(AssociationAttribute)] != null)
                    {
                        continue;
                    }

                    Type memberType = TypeUtility.GetElementType(pd.PropertyType);
                    if (TypeUtility.IsSupportedComplexType(memberType))
                    {
                        this.AddComplexType(memberType);
                    }
                }
            }

            // Discover complex types in method signatures. Note that we're relying on method
            // validation to actually validate the signatures.
            foreach (DomainOperationEntry entry in this.DomainOperationEntries)
            {
                if (entry.Operation == DomainOperation.Invoke || entry.Operation == DomainOperation.Custom)
                {
                    foreach (DomainOperationParameter parameter in entry.Parameters)
                    {
                        Type paramType = TypeUtility.GetElementType(parameter.ParameterType);
                        if (TypeUtility.IsComplexType(paramType))
                        {
                            this.AddComplexType(paramType);
                        }
                    }
                    if (entry.Operation == DomainOperation.Invoke)
                    {
                        Type returnType = TypeUtility.GetElementType(entry.ReturnType);
                        if (entry.ReturnType != typeof(void) && TypeUtility.IsComplexType(returnType))
                        {
                            this.AddComplexType(returnType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the specified type is a known entity type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is a known entity type; false otherwise.</returns>
        internal bool IsKnownEntityType(Type type)
        {
            return this._entityTypes.Contains(type);
        }

        /// <summary>
        /// Checks whether the specified type is a composed entity type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is a composed entity type; false otherwise.</returns>
        internal bool IsComposedEntityType(Type type)
        {
            // Our convention is that the compositionMap has entries only for types having
            // parent associations, including inherited ones.
            List<PropertyDescriptor> parentAssociations = null;
            return this._compositionMap.TryGetValue(type, out parentAssociations);
        }

        /// <summary>
        /// Verifies that the description is still in an uninitialized state. A description
        /// can only be modified/configured before it has been initialized.
        /// </summary>
        private void CheckInvalidUpdate()
        {
            if (this._isInitialized)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_InvalidUpdate));
            }
        }

        /// <summary>
        /// Call this method to ensure that the description is initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!this._isInitialized && !this._isInitializing)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_Uninitialized));
            }
        }

        /// <summary>
        /// Create the base reflection based description, then call all DomainDescriptionProviders in the inheritance
        /// hierarchy, chaining them, and allowing each of them to replace or modify the description as needed.
        /// </summary>
        /// <param name="domainServiceType">The DomainService Type.</param>
        /// <returns>The description</returns>
        private static DomainServiceDescription CreateDescription(Type domainServiceType)
        {
            DomainServiceDescriptionProvider descriptionProvider = CreateDescriptionProvider(domainServiceType);
            DomainServiceDescription description = descriptionProvider.GetDescription();

            // TODO : better way to do this? Currently the description provider is needed
            // when the description is Initialized for TD registration.
            description._descriptionProvider = descriptionProvider;

            // after the description has been created, allow providers to add additional
            // operation metadata
            foreach (DomainOperationEntry operation in description.DomainOperationEntries)
            {
                AttributeCollection attributes = operation.Attributes;
                AttributeCollection newAttributes = descriptionProvider.GetOperationAttributes(operation);
                if (attributes != newAttributes)
                {
                    operation.Attributes = newAttributes;
                }
            }

            // after all providers have had a chance to participate in the
            // creation of the description, we initialize and validate
            description.Initialize();

            return description;
        }

        /// <summary>
        /// Creates and returns the description provider for the specified domain service Type.
        /// </summary>
        /// <param name="domainServiceType">The domain service Type.</param>
        /// <returns>The description provider.</returns>
        internal static DomainServiceDescriptionProvider CreateDescriptionProvider(Type domainServiceType)
        {
            // construct a list of all types in the inheritance hierarchy for the service
            List<Type> baseTypes = new List<Type>();
            Type currType = domainServiceType;
            while (currType != typeof(DomainService))
            {
                baseTypes.Add(currType);
                currType = currType.BaseType;
            }

            // create our base reflection provider
            List<DomainServiceDescriptionProvider> descriptionProviderList = new List<DomainServiceDescriptionProvider>();
            ReflectionDomainServiceDescriptionProvider reflectionDescriptionProvider = new ReflectionDomainServiceDescriptionProvider(domainServiceType);

            // set the IsEntity function which consults the chain of providers.
            Func<Type, bool> isEntityTypeFunc = (t) => descriptionProviderList.Any(p => p.LookupIsEntityType(t));
            reflectionDescriptionProvider.SetIsEntityTypeFunc(isEntityTypeFunc);

            // Now from most derived to base, create any declared description providers,
            // chaining the instances as we progress. Note that ordering from derived to
            // base is important - we want to ensure that any DSDPs the user has placed on
            // their DomainService directly come before an DAL providers.
            DomainServiceDescriptionProvider currProvider = reflectionDescriptionProvider;
            descriptionProviderList.Add(currProvider);
            for (int i = 0; i < baseTypes.Count; i++)
            {
                currType = baseTypes[i];

                // Reflection rather than TD is used here so we only get explicit
                // Type attributes. TD inherits attributes by default, even if the
                // attributes aren't inheritable.
                foreach (DomainServiceDescriptionProviderAttribute providerAttribute in
                    currType.GetCustomAttributes(typeof(DomainServiceDescriptionProviderAttribute), false))
                {
                    currProvider = providerAttribute.CreateProvider(domainServiceType, currProvider);
                    currProvider.SetIsEntityTypeFunc(isEntityTypeFunc);
                    descriptionProviderList.Add(currProvider);
                }
            }

            return currProvider;
        }

        /// <summary>
        /// Register all required custom type descriptors for the specified type.
        /// </summary>
        /// <param name="type">The type to register.</param>
        private void RegisterCustomTypeDescriptors(Type type)
        {
#if !NETSTANDARD2_0
            // if this type has a metadata class defined, add a 'buddy class' type descriptor. 
            if (type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).Length != 0)
            {
                RegisterCustomTypeDescriptor(new AssociatedMetadataTypeTypeDescriptionProvider(type), type);
            }
#endif

            if (this._descriptionProvider != null)
            {
                RegisterDomainTypeDescriptionProvider(type, this._descriptionProvider);
            }
        }

        /// <summary>
        /// Register our DomainTypeDescriptionProvider for the specfied Type. This provider is responsible for surfacing the
        /// custom TDs returned by description providers.
        /// </summary>
        /// <param name="type">The Type that we should register for.</param>
        /// <param name="descriptionProvider">The description provider.</param>
        private static void RegisterDomainTypeDescriptionProvider(Type type, DomainServiceDescriptionProvider descriptionProvider)
        {
            DomainTypeDescriptionProvider domainTdp = new DomainTypeDescriptionProvider(type, descriptionProvider);
            RegisterCustomTypeDescriptor(domainTdp, type);
        }

        // The JITer enforces CAS. By creating a separate method we can avoid getting SecurityExceptions 
        // when we weren't going to really call TypeDescriptor.AddProvider.
        internal static void RegisterCustomTypeDescriptor(TypeDescriptionProvider tdp, Type type)
        {
            // Check if we already registered provider with the specified type.
            HashSet<Type> existingProviders = s_typeDescriptionProviderMap.GetOrAdd(type, t =>
                {
                    return new HashSet<Type>();
                });

            if (!existingProviders.Contains(tdp.GetType()))
            {
                TypeDescriptor.AddProviderTransparent(tdp, type);
                existingProviders.Add(tdp.GetType());
            }
        }

        /// <summary>
        /// Determines whether a given type may be used as an entity type.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <param name="errorMessage">If this method returns <c>false</c>, the error message</param>
        /// <returns><c>true</c> if the type can legally be used as an entity</returns>
        private static bool IsValidEntityType(Type type, out string errorMessage)
        {
            errorMessage = null;
            if (!type.IsVisible)
            {
                errorMessage = Resource.EntityTypes_Must_Be_Public;
            }
            else if (TypeUtility.IsNullableType(type))
            {
                // why is this check here? can't we just assert that an entity type
                // is not a value type?
                errorMessage = Resource.EntityTypes_Cannot_Be_Nullable;
            }
            else if (type.IsGenericType)
            {
                errorMessage = Resource.EntityTypes_Cannot_Be_Generic;
            }
            else if (TypeUtility.IsPredefinedType(type))
            {
                errorMessage = Resource.EntityTypes_Cannot_Be_Primitives;
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                errorMessage = Resource.EntityTypes_Cannot_Be_Collections;
            }
            else if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) == null)
            {
                // Lack of ctor counts only if not abstract.
                errorMessage = Resource.EntityTypes_Must_Have_Default_Constructor;
            }

            return (errorMessage == null);
        }

        /// <summary>
        /// Gets the entity type returned by the specified query method and determines
        /// whether this query method returns a singleton.
        /// </summary>
        /// <param name="method">The query method</param>
        /// <param name="isSingleton">The output parameter to accept whether this query returns a singleton.</param>
        /// <param name="error">The output parameter to accept the error if the entity return type is illegal.</param>
        /// <returns>The entity type</returns>
        private static Type GetQueryEntityReturnType(DomainOperationEntry method, out bool isSingleton, out Exception error)
        {
            Type returnType = method.ReturnType;
            Type entityType = null;
            isSingleton = false;
            error = null;

            Type enumerableOfT = TypeUtility.FindIEnumerable(returnType);
            if (enumerableOfT != null)
            {
                // IEnumerable<T> returning method
                entityType = enumerableOfT.GetGenericArguments()[0];
            }
            else if (!typeof(IEnumerable).IsAssignableFrom(returnType) && returnType != typeof(void))
            {
                // singleton returning method
                isSingleton = true;
                entityType = returnType;
            }
            else
            {
                error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_InvalidQueryOperationReturnType, returnType, method.Name));
            }
            return entityType;
        }

        /// <summary>
        /// Adds the specified complex Type, including any referenced complex types
        /// recursively.
        /// </summary>
        /// <param name="complexType">The complex type to add</param>
        private void AddComplexType(Type complexType)
        {
            if (this._complexTypes.Contains(complexType))
            {
                return;
            }

            // if the type isn't a complex type so search
            // no further
            if (!this.IsComplexType(complexType))
            {
                return;
            }

            this.RegisterCustomTypeDescriptors(complexType);

            // the custom tdp may change the definition of complex type (e.g. the addition of a Key property).
            if (!this.IsComplexType(complexType))
            {
                return;
            }

            // first add the type itself if it is complex
            this._complexTypes.Add(complexType);

            // now recursively enumerate all members looking for complex types
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(complexType))
            {
                if (pd.Attributes[typeof(ExcludeAttribute)] != null)
                {
                    continue;
                }

                Type memberType = TypeUtility.GetElementType(pd.PropertyType);
                if (TypeUtility.IsSupportedComplexType(memberType))
                {
                    this.AddComplexType(memberType);
                }
            }
        }

        private bool IsComplexType(Type type)
        {
            // Here we're also relying on the entity lookup function of the description
            // provider to determine whether the type is an entity. This is important,
            // because even if a particular type isn't exposed by the service as an entity
            // type, we don't want to interpret it as a complex type.
            bool isEntity = this._entityTypes.Contains(type) ||
                (this._descriptionProvider != null && this._descriptionProvider.LookupIsEntityType(type));
            if (!isEntity && TypeUtility.IsComplexType(type))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validate all complex types
        /// </summary>
        private void ValidateComplexTypes()
        {
            foreach (Type complexType in this.ComplexTypes)
            {
                ValidateComplexType(complexType);

                // We disallow inheritance of complex types from complex types.
                Type childComplexType = this.ComplexTypes.FirstOrDefault(ct => (ct != complexType) && complexType.IsAssignableFrom(ct));
                if (childComplexType != null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidComplexType_Inheritance, this.DomainServiceType, childComplexType, complexType));
                }

                // We disallow inheritance of entities from complex types.
                Type childEntityType = this.EntityTypes.FirstOrDefault(e => complexType.IsAssignableFrom(e));
                if (childEntityType != null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidComplexType_EntityInheritance, this.DomainServiceType, childEntityType, complexType));
                }
            }
        }

        /// <summary>
        /// Perform validation on the complex type
        /// </summary>
        /// <param name="complexType">The complex type to validate</param>
        internal static void ValidateComplexType(Type complexType)
        {
            // KnownTypeAttribute indicates a type may be its derived type. Since we do not support complex type inheritance, disallow this.
            if (KnownTypeUtilities.ImportKnownTypes(complexType, /* inherit */ false).Any(t => complexType.IsAssignableFrom(t)))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidComplexType_KnownTypes, complexType.Name));
            }

            MetaType metaType = MetaType.GetMetaType(complexType);
            foreach (MetaMember metaMember in metaType.DataMembers)
            {
                Debug.Assert(metaMember.Member.Attributes[typeof(KeyAttribute)] == null, "Cannot add a type with a KeyAttribute to the complex type dictionary");

                string propertyAttributeError = null;

                // Cannot have associations.
                if (metaMember.IsAssociationMember)
                {
                    propertyAttributeError = Resource.InvalidComplexType_AssociationMember;
                }

                // Cannot have composition.
                if (metaMember.Member.Attributes[typeof(CompositionAttribute)] != null)
                {
                    propertyAttributeError = Resource.InvalidComplexType_CompositionMember;
                }

                // Cannot have include.
                if (metaMember.Member.Attributes[typeof(IncludeAttribute)] != null)
                {
                    propertyAttributeError = Resource.InvalidComplexType_IncludeMember;
                }

                if (propertyAttributeError != null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidComplexType_PropertyAttribute, metaMember.Member.Name, complexType.Name, propertyAttributeError));
                }
            }
        }

        /// <summary>
        /// Recursively add the specified entity and all entities in its Include graph
        /// to our list of all entities, and register an associated metadata type descriptor
        /// for each.
        /// </summary>
        /// <param name="entityType">type of entity to add</param>
        private void AddEntityType(Type entityType)
        {
            if (this._entityTypes.Contains(entityType))
            {
                // we've already visited this type
                return;
            }

            // Ensure this type can really be used as an entity type
            string errorMessage;
            if (!IsValidEntityType(entityType, out errorMessage))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.Invalid_Entity_Type, entityType.Name, errorMessage), nameof(entityType));
            }

            this._entityTypes.Add(entityType);

            // Any new entity invalidates our cached entity hierarchies
            this._rootEntityTypes = null;
            this._entityKnownTypes = null;

            this.RegisterCustomTypeDescriptors(entityType);

            // visit all properties and do any required validation or initialization processing
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(entityType))
            {
                // All properties marked with [ExternalReference] must also have an [Association].  If not found, throw an exception.
                if (pd.Attributes[typeof(ExternalReferenceAttribute)] != null && pd.Attributes[typeof(AssociationAttribute)] == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        Resource.InvalidExternal_NonAssociationMember, entityType.Name, pd.Name));
                }

                // if the member is marked with the composition attribute,
                // verify that the member is also an association
                if (pd.Attributes[typeof(CompositionAttribute)] != null)
                {
                    if (pd.Attributes[typeof(AssociationAttribute)] == null)
                    {
                        throw new InvalidOperationException(
                            string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_InvalidCompositionMember, pd.ComponentType, pd.Name));
                    }

                    // cache the fact that the element type of the association is
                    // a composed Type
                    Type elementType = TypeUtility.GetElementType(pd.PropertyType);
                    List<PropertyDescriptor> parentAssociations = null;
                    if (!this._compositionMap.TryGetValue(elementType, out parentAssociations))
                    {
                        parentAssociations = new List<PropertyDescriptor>();
                        this._compositionMap[elementType] = parentAssociations;
                    }
                    parentAssociations.Add(pd);
                }

                if (pd.Attributes.OfType<IncludeAttribute>().Any(p => !p.IsProjection))
                {
                    // Validate the Included member is Entity
                    // Note: we check here rather than letting the recursive call check simply because
                    // we can provide a better message about where this entity type comes from.
                    Type includedEntityType = TypeUtility.GetElementType(pd.PropertyType);
                    if (!IsValidEntityType(includedEntityType, out errorMessage))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                            Resource.Invalid_Include_Invalid_Entity, pd.Name, entityType.Name, pd.PropertyType.Name, errorMessage));
                    }

                    // recursively visit the included entity type
                    this.AddEntityType(includedEntityType);
                }
                else if (!TypeUtility.IsPredefinedType(pd.PropertyType))
                {
                    if (pd.Attributes[typeof(ExcludeAttribute)] == null)
                    {
                        Type potentialEntityType = TypeUtility.GetElementType(pd.PropertyType);
                        if (!IsValidEntityType(potentialEntityType, out errorMessage))
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                Resource.Invalid_Entity_Property, entityType.FullName, pd.Name));
                        }
                    }
                }
            }

            // Recursively add any derived entity types specified by [KnownType]
            // attributes
            IEnumerable<Type> knownTypes = KnownTypeUtilities.ImportKnownTypes(entityType, true);
            foreach (Type t in knownTypes)
            {
                if (entityType.IsAssignableFrom(t))
                {
                    this.AddEntityType(t);
                }
            }
        }

        /// <summary>
        /// Validates that the given <paramref name="entityType"/> does not contain
        /// any properties that violate our rules for polymorphism.
        /// </summary>
        /// <remarks>
        /// The only rule currently enforced is that no property can use "new" to
        /// override an existing entity property.
        /// </remarks>
        /// <param name="entityType">The entity type to validate.</param>
        private static void ValidatePropertyPolymorphism(Type entityType)
        {
            System.Diagnostics.Debug.Assert(entityType != null, "entityType is required");

            // We consider only actual properties, not TDP properties, because
            // these are the only ones that can have actual runtime methods
            // that are polymorphic.  We ask only for public instance properties.
            foreach (PropertyInfo propertyInfo in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                //skip indexer properties.
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                // Either the get or the set will suffice
                MethodInfo methodInfo = propertyInfo.GetGetMethod() ?? propertyInfo.GetSetMethod();

                // Detecting the "new" keyword requires a check whether this method is
                // hiding a method with the same signature in a derived type.  IsHideBySig does not do this.
                // A "new" constitutes an illegal use of polymorphism and throws InvalidOperationException.
                // A "new" appears as a non-virtual method that is declared concretely further up the hierarchy.
                if (methodInfo.DeclaringType.BaseType != null && !methodInfo.IsVirtual)
                {
                    Type[] parameterTypes = methodInfo.GetParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray();
                    MethodInfo baseMethod = methodInfo.DeclaringType.BaseType.GetMethod(methodInfo.Name, parameterTypes);
                    if (baseMethod != null && !baseMethod.IsAbstract)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                            Resource.Entity_Property_Redefined,
                                                            entityType,
                                                            propertyInfo.Name,
                                                            baseMethod.DeclaringType));
                    }
                }
            }
        }

        /// <summary>
        /// Validate all entity types exposed by this provider.
        /// </summary>
        private void ValidateEntityTypes()
        {
            foreach (Type entityType in this._entityTypes)
            {
                Type rootType = this.GetRootEntityType(entityType);
                bool isRootType = entityType == rootType;
                string firstKeyProperty = null;
                PropertyDescriptor versionMember = null;

                bool canUpdate = this.IsOperationSupported(entityType, DomainOperation.Update);
                bool canDelete = this.IsOperationSupported(entityType, DomainOperation.Delete);

                PropertyDescriptorCollection pds = TypeDescriptor.GetProperties(entityType);
                foreach (PropertyDescriptor pd in pds)
                {
                    // During first pass, just notice whether any property is marked [Key]
                    if (firstKeyProperty == null)
                    {
                        if (pd.Attributes[typeof(ExcludeAttribute)] == null)
                        {
#if NET6_0_OR_GREATER
                            bool hasKey = false;
                            for (int i = 0; i < pd.Attributes.Count; i++) 
                            {
                                if (pd.Attributes[i].TypeId.ToString() == typeof(KeyAttribute).FullName)
                                    hasKey = true;
                            }
#else
                            bool hasKey = (pd.Attributes[typeof(KeyAttribute)] != null);
#endif

                            // The presence of a [Key] property matters for the root type
                            // regardless if it is actually declared there or on some hidden
                            // base type (ala UserBase).  But for derived entities, it matters
                            // only if they explicitly declare it.
                            if (hasKey && (isRootType || pd.ComponentType == entityType))
                            {
                                firstKeyProperty = pd.Name;
                            }
                        }
                    }

                    foreach (IncludeAttribute include in pd.Attributes.OfType<IncludeAttribute>())
                    {
                        if (!include.IsProjection)
                        {
                            // verify that non-projection Includes are only placed on Association members
                            AssociationAttribute assoc = (AssociationAttribute)pd.Attributes[typeof(AssociationAttribute)];
                            if (assoc == null)
                            {
                                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidInclude_NonAssociationMember, pd.ComponentType.Name, pd.Name));
                            }
                        }
                    }

                    // If a property is excluded implicitly or explicitly, has [ConcurrencyCheck] and this entity can Update or Delete, it is an error.
                    bool isExcluded = !SerializationUtility.IsSerializableDataMember(pd);
                    if (isExcluded && (canUpdate || canDelete) && pd.Attributes[typeof(ConcurrencyCheckAttribute)] != null)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resource.Invalid_Exclude_Property_Concurency_Conflict, entityType, pd.Name));
                    }

                    // Verify that multiple version members are not defined
                    if (pd.Attributes[typeof(TimestampAttribute)] != null && pd.Attributes[typeof(ConcurrencyCheckAttribute)] != null)
                    {
                        if (versionMember != null)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_MultipleVersionMembers, entityType));
                        }
                        versionMember = pd;
                    }
                }

                // validate associations defined in this entity type
                this.ValidateEntityAssociations(entityType, pds);

                // Root entity types need at least one [Key] property.
                if (isRootType && firstKeyProperty == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.Entity_Has_No_Key_Properties, entityType.Name, this._domainServiceType.Name));
                }

                // Non-root entities must not have any [Key] properties
                if (!isRootType && firstKeyProperty != null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DerivedEntityCannotHaveKey, firstKeyProperty, entityType.Name));
                }

                // Validate derived entity types don't attempt illegal polymorphism
                DomainServiceDescription.ValidatePropertyPolymorphism(entityType);

                // Validate that:
                //  - The root type must declare a [KnownType] for every derived type
                //  - Every abstract entity type must specify a [KnownType] for at least one concrete derived type
                this.ValidateKnownTypeAttribute(entityType, /*mustSpecifyAll*/ isRootType);

                // Validate that if the type derives from one that has [DataContract] attribute,
                // then it has a [DataContract] attribute as well.
                ValidateDataContractAttribute(entityType);

                // Validate that all CUD methods are declared on the roots
                this.ValidateDerivedDomainOperations();
            }
        }

        /// <summary>
        /// Fixes the map of composed types so that derived composition types
        /// inherit the parent associations from their base type.
        /// </summary>
        /// <remarks>
        /// This method assumes the composition map has been constructed
        /// to contain the parent associations for each composed type without
        /// regard to inheritance.  Hence, composition subclasses will not have
        /// entries in the map unless they have additional parent associations.
        /// Composition subclasses that have no entry will gain a new entry
        /// that is the accumulation of all their base class's associations.
        /// Composition subclasses that have their own parent associations will
        /// combine those with the accumulated base class's associations.
        /// </remarks>
        private void FixupCompositionMap()
        {
            // Strategy: to accommodate possible splits in the hierarchy and
            // the indeterminate order in which the entities are in our cache,
            // we maintain a hash to guarantee we fix each entry only once.
            HashSet<Type> fixedEntities = new HashSet<Type>();
            foreach (Type entityType in this.EntityTypes)
            {
                this.FixParentAssociationsWalk(entityType, fixedEntities);
            }
        }

        /// <summary>
        /// Helper method to ensure this <paramref name="entityType"/>'s entry in
        /// the composition map combines its explicit parent associations together
        /// with those from all its base type's associations.
        /// </summary>
        /// <remarks>
        /// This algorithm repairs the composition map in-place and reentrantly
        /// fixes the entity's base class entries first.  The map will not be replaced,
        /// but will be updated in-place.  Existing lists in the map will be extended
        /// but not replaced.  Holes in the map will be filled in by sharing the base
        /// class's entry rather than cloning.
        /// </remarks>
        /// <param name="entityType">The entity type to repair.  It may or may not be a composed type.</param>
        /// <param name="fixedEntities">Hash of already repaired entity types.  Used to avoid duplicate fixups.</param>
        /// <returns>The collection of parent associations.</returns>
        private List<PropertyDescriptor> FixParentAssociationsWalk(Type entityType, HashSet<Type> fixedEntities)
        {
            List<PropertyDescriptor> parentAssociations = null;

            // If we have already visited this entity type, its composition map
            // entry is accurate and can be used as is.
            this._compositionMap.TryGetValue(entityType, out parentAssociations);
            if (fixedEntities.Contains(entityType))
            {
                return parentAssociations;
            }

            fixedEntities.Add(entityType);

            // Get the base class's associations.  This will re-entrantly walk back
            // the hierarchy and repair the composition map as it goes.
            Type baseType = this.GetEntityBaseType(entityType);
            List<PropertyDescriptor> inheritedParentAssociations = (baseType == null)
                                                                            ? null
                                                                            : this.FixParentAssociationsWalk(baseType, fixedEntities);

            // If there are no base class associations to inherit, then the map
            // is already accurate for the current entry.  But if we have base
            // class associations, we need either to merge or to share them.
            if (inheritedParentAssociations != null)
            {
                if (parentAssociations == null)
                {
                    // No current associations -- simply share the base class's list
                    parentAssociations = inheritedParentAssociations;
                    this._compositionMap[entityType] = parentAssociations;
                }
                else
                {
                    // Associations for both base and current -- merge them into existing list
                    parentAssociations.AddRange(inheritedParentAssociations);
                }
            }
            return parentAssociations;
        }

        /// <summary>
        /// Validates that the specified root entity type
        /// has a <see cref="KnownTypeAttribute"/> for each of its
        /// derived types.
        /// </summary>
        /// <param name="entityType">The entity type to check.</param>
        /// <param name="mustSpecifyAll">If <c>true</c> this method validates that this entity declares all its derived types via <see cref="KnownTypeAttribute"/>.</param>
        private void ValidateKnownTypeAttribute(Type entityType, bool mustSpecifyAll)
        {
            IEnumerable<Type> knownTypes = this.EntityKnownTypes[entityType];
            IEnumerable<Type> derivedTypes = this.GetEntityDerivedTypes(entityType);
            bool hasConcreteDerivedType = false;

            foreach (Type derivedType in derivedTypes)
            {
                hasConcreteDerivedType |= !derivedType.IsAbstract;

                if (mustSpecifyAll && !knownTypes.Contains(derivedType))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.KnownTypeAttributeRequired, derivedType.Name, entityType.Name));
                }
            }

            // Any abstract entity type is required to use [KnownType] to specify
            // at least one concrete subclass
            if (entityType.IsAbstract && !hasConcreteDerivedType)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.KnownTypeAttributeRequired_Abstract, entityType.Name, this.DomainServiceType.Name));
            }

            this.ValidateComposedTypeOperations(entityType);
        }

        /// <summary>
        /// Validates that if the specified type derives from a type with 
        /// <see cref="DataContractAttribute"/>, then it has a 
        /// <see cref="DataContractAttribute"/> as well.
        /// </summary>
        /// <param name="entityType">The entity type to check.</param>
        private static void ValidateDataContractAttribute(Type entityType)
        {
            if (entityType.Attributes()[typeof(DataContractAttribute)] != null)
            {
                return;
            }

            Type baseType = entityType.BaseType;
            while (baseType != null)
            {
                if (baseType.Attributes()[typeof(DataContractAttribute)] != null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DataContractAttributeRequired, entityType.Name, baseType.Name));
                }
                baseType = baseType.BaseType;
            }
        }

        /// <summary>
        /// Ensures that for composed Types, if they have explicit update operations the
        /// correct corresponding operations are supported on all parent Types.
        /// </summary>
        /// <param name="entityType">The entity type to validate</param>
        private void ValidateComposedTypeOperations(Type entityType)
        {
            IEnumerable<Type> parentTypes = this.GetParentAssociations(entityType).Select(p => p.ComponentType);
            if (!parentTypes.Any())
            {
                return;
            }

            bool supportsUpdate =
                this.GetSubmitMethod(entityType, DomainOperation.Update) != null ||
                this.GetCustomMethods(entityType).Any();
            if (supportsUpdate && !parentTypes.All(p => this.IsOperationSupported(p, DomainOperation.Update)))
            {
                // If a child has an explicit update method, all parents must support
                // Update as well
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resource.Composition_ParentsMustSupportUpdate, entityType));
            }

            if (this.GetSubmitMethod(entityType, DomainOperation.Insert) != null &&
                !parentTypes.All(p => (this.IsOperationSupported(p, DomainOperation.Update) ||
                                      this.IsOperationSupported(p, DomainOperation.Insert))))
            {
                // If a child has an explicit Insert method, all parents must support
                // either Update or Insert
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resource.Composition_ParentsMustSupportInsert, entityType));
            }

            if (this.GetSubmitMethod(entityType, DomainOperation.Delete) != null &&
                !parentTypes.All(p => (this.IsOperationSupported(p, DomainOperation.Update) ||
                                      this.IsOperationSupported(p, DomainOperation.Delete))))
            {
                // If a child has an explicit Delete method, all parents must support
                // either Update or Delete
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resource.Composition_ParentsMustSupportDelete, entityType));
            }
        }

        /// <summary>
        /// This method validates the association attributes for the specified entity type
        /// </summary>
        /// <param name="entityType">Type of entity to validate its association attributes for</param>
        /// <param name="entityProperties">collection of entity property descriptors</param>
        private void ValidateEntityAssociations(Type entityType, PropertyDescriptorCollection entityProperties)
        {
            foreach (PropertyDescriptor pd in entityProperties)
            {
                // validate the association attribute (if any)
                AssociationAttribute assocAttrib = pd.Attributes[typeof(AssociationAttribute)] as AssociationAttribute;
                if (assocAttrib == null)
                {
                    continue;
                }

                string assocName = assocAttrib.Name;
                if (string.IsNullOrEmpty(assocName))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_NameCannotBeNullOrEmpty, pd.Name, entityType));
                }
                if (string.IsNullOrEmpty(assocAttrib.ThisKey))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_StringCannotBeNullOrEmpty, assocName, entityType, "ThisKey"));
                }
                if (string.IsNullOrEmpty(assocAttrib.OtherKey))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_StringCannotBeNullOrEmpty, assocName, entityType, "OtherKey"));
                }

                // The number of keys in 'this' and 'other' must be the same
                if (assocAttrib.ThisKeyMembers.Count() != assocAttrib.OtherKeyMembers.Count())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_Key_Count_Mismatch, assocName, entityType, assocAttrib.ThisKey, assocAttrib.OtherKey));
                }

                // check that all ThisKey members exist on this entity type
                foreach (string thisKey in assocAttrib.ThisKeyMembers)
                {
                    if (entityProperties[thisKey] == null)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_ThisKeyNotFound, assocName, entityType, thisKey));
                    }
                }

                // Verify that the association name is unique. In inheritance scenarios, self-referencing associations
                // on the base type should be inheritable by the derived types.
                Type otherEntityType = TypeUtility.GetElementType(pd.PropertyType);
                int otherMemberCount = entityProperties.Cast<PropertyDescriptor>().Count(p => p.Name != pd.Name && p.Attributes.OfType<AssociationAttribute>().Any(a => a.Name == assocAttrib.Name));
                bool isSelfReference = otherEntityType.IsAssignableFrom(entityType);
                if ((!isSelfReference && otherMemberCount > 0) || (isSelfReference && otherMemberCount > 1))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_NonUniqueAssociationName, assocName, entityType));
                }

                // Verify that the type of FK associations return singletons.
                if (assocAttrib.IsForeignKey && (otherEntityType != pd.PropertyType))
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.InvalidAssociation_FKNotSingleton,
                        assocName, entityType));
                }

                // Associations are not allowed to be marked as [Required], because we don't guarantee 
                // that we set the association on the server. In many cases it's possible that we simply 
                // associate entities based on FKs.
                if (pd.Attributes[typeof(RequiredAttribute)] != null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resource.Entity_RequiredAssociationNotAllowed, entityType, pd.Name));
                }

                // Throw if the association member has a explicit RoundtripOriginalAttribute on it
                if (pd.ExplicitAttributes()[typeof(RoundtripOriginalAttribute)] != null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_RoundTripOriginal, pd.Name, entityType));
                }

                // if the other entity is also exposed by the service, perform additional validation
                if (this._entityTypes.Contains(otherEntityType))
                {
                    PropertyDescriptorCollection otherEntityProperties = TypeDescriptor.GetProperties(otherEntityType);
                    PropertyDescriptor otherMember = otherEntityProperties.Cast<PropertyDescriptor>().FirstOrDefault(p => p.Name != pd.Name && p.Attributes.OfType<AssociationAttribute>().Any(a => a.Name == assocName));
                    if (otherMember != null)
                    {
                        // Bi-directional association
                        // make sure IsForeignKey is set to true on one and only one side of the association
                        AssociationAttribute otherAssocAttrib = (AssociationAttribute)otherMember.Attributes[typeof(AssociationAttribute)];
                        if (otherAssocAttrib != null &&
                            !((assocAttrib.IsForeignKey != otherAssocAttrib.IsForeignKey)
                             && (assocAttrib.IsForeignKey || otherAssocAttrib.IsForeignKey)))
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_IsFKInvalid, assocName, entityType));
                        }

                        Type otherMemberEntityType = TypeUtility.GetElementType(otherMember.PropertyType);

                        // Verify that the type of the corresponding association points back to this entity
                        // The type of the corresponding association can be one of the parents of the entity, but it cannot be one of its children.
                        if (!otherMemberEntityType.IsAssignableFrom(entityType))
                        {
                            throw new InvalidOperationException(string.Format(
                                CultureInfo.CurrentCulture,
                                Resource.InvalidAssociation_TypesDoNotAlign,
                                assocName, entityType, otherEntityType));
                        }
                    }

                    // check that the OtherKey members exist on the other entity type
                    foreach (string otherKey in assocAttrib.OtherKeyMembers)
                    {
                        if (otherEntityProperties[otherKey] == null)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_OtherKeyNotFound, assocName, entityType, otherKey, otherEntityType));
                        }
                    }
                }
                else
                {
                    // Disallow attempts to place [Association] on simple types
                    if (TypeUtility.IsPredefinedType(otherEntityType))
                    {
                        // Association attributes cannot be attached to properties whose types are not entities
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.Association_Not_Entity_Type, pd.Name, entityType.Name, otherEntityType.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Add a Query method to this domain service.
        /// </summary>
        /// <param name="method"><see cref="DomainOperationEntry"/> for the method to be added</param>
        private void AddQueryMethod(DomainOperationEntry method)
        {
            System.Diagnostics.Debug.Assert(method.Operation == DomainOperation.Query, "Expected a query method");

            ValidateMethodSignature(this, method);
            string methodName = method.Name;

            if (this._queryMethods.ContainsKey(methodName))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntryOverload_NotSupported, methodName));
            }

            // If this query is marked IsDefault=true, validate it is legal
            QueryAttribute queryAttribute = method.Attributes[typeof(QueryAttribute)] as QueryAttribute;
            if (queryAttribute != null && queryAttribute.IsDefault)
            {
                this.ValidateDefaultQuery(method);
            }

            this._queryMethods[methodName] = method;

            this.AddEntityType(method.AssociatedType);
        }

        /// <summary>
        /// Add a CUD method to this domain service.
        /// </summary>
        /// <param name="method"><see cref="DomainOperationEntry"/> for the method to be added</param>
        private void AddSubmitMethod(DomainOperationEntry method)
        {
            ValidateMethodSignature(this, method);
            string methodName = method.Name;

            Dictionary<DomainOperation, DomainOperationEntry> entitySubmitMethods = null;
            Type entityType = method.AssociatedType;
            if (!this._submitMethods.TryGetValue(entityType, out entitySubmitMethods))
            {
                entitySubmitMethods = new Dictionary<DomainOperation, DomainOperationEntry>();
                this._submitMethods[entityType] = entitySubmitMethods;
            }

            if (entitySubmitMethods.ContainsKey(method.Operation))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainService_DuplicateCUDMethod, methodName, entitySubmitMethods[method.Operation].Name));
            }

            entitySubmitMethods[method.Operation] = method;
        }

        /// <summary>
        /// Validates that the specified query method can legally be marked as the default
        /// </summary>
        /// <param name="method">The query method</param>
        private void ValidateDefaultQuery(DomainOperationEntry method)
        {
            // Default queries may not declare any parameters
            if (method.Parameters.Any())
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Params, method.Name));
            }

            // Extract the (logical) entity type returned by this query, factoring out whether it is singleton or IEnumerable.
            bool isSingleton = false;
            Exception error = null;
            Type entityType = DomainServiceDescription.GetQueryEntityReturnType(method, out isSingleton, out error);
            if (error != null)
            {
                throw error;
            }

            // Default queries cannot return singleton values
            if (isSingleton)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Be_Singleton, method.Name));
            }

            // Scan the other queries we have registered.
            // It is illegal for any other query to be marked IsDefault if it returns the same entity type.
            // Queries returning derived types are acceptable.
            foreach (DomainOperationEntry existingQuery in this._queryMethods.Values)
            {
                QueryAttribute queryAttribute = existingQuery.Attributes[typeof(QueryAttribute)] as QueryAttribute;
                if (queryAttribute != null && queryAttribute.IsDefault)
                {
                    Type existingEntityType = DomainServiceDescription.GetQueryEntityReturnType(existingQuery, out isSingleton, out error);

                    // Two default queries returning the same type is not legal
                    if (existingEntityType == entityType)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Multiple, method.Name, existingQuery.Name, entityType));
                    }

                    // Two default queries returning types with an inheritance relationship is not legal.
                    // Handle both directions of inheritance for a good error message.
                    if (existingEntityType.IsAssignableFrom(entityType))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Multiple_Inheritance, method.Name, existingQuery.Name, entityType, existingEntityType));
                    }
                    if (entityType.IsAssignableFrom(existingEntityType))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Multiple_Inheritance, method.Name, existingQuery.Name, existingEntityType, entityType));
                    }
                }
            }
        }

        private static void ValidateMethodSignature(DomainServiceDescription description, DomainOperationEntry method)
        {
            Exception error;
            if (!IsValidMethodSignature(description, method, method.Operation, out error))
            {
                throw error;
            }
        }

        private static bool IsValidMethodSignature(DomainServiceDescription description, DomainOperationEntry operationEntry, DomainOperation operation, out Exception error)
        {
            string methodName = operationEntry.Name;
            ReadOnlyCollection<DomainOperationParameter> parameters = operationEntry.Parameters;
            Type returnType = operationEntry.ReturnType;

            switch (operation)
            {
                case DomainOperation.Delete:
                case DomainOperation.Insert:
                case DomainOperation.Update:
                    {
                        // insert signature check: parameter length must be 1
                        if (parameters.Count != 1)
                        {
                            error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, methodName));
                            return false;
                        }

                        // parameter must be by-value
                        if (!IsByVal(parameters[0]))
                        {
                            error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeByVal, methodName, parameters[0].Name));
                            return false;
                        }

                        if (!description._entityTypes.Contains(parameters[0].ParameterType))
                        {
                            error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainMethod_ParamMustBeEntity, parameters[0].Name, methodName));
                            return false;
                        }

                        break;
                    }
                case DomainOperation.Query:
                    {
                        // Ignore the optional "out count" parameter.
                        IEnumerable<DomainOperationParameter> queryParameters = parameters;
                        DomainOperationParameter lastParameter = queryParameters.LastOrDefault();
                        if (lastParameter != null && lastParameter.IsOut)
                        {
                            queryParameters = queryParameters.Take(queryParameters.Count() - 1).ToArray();
                        }

                        foreach (DomainOperationParameter param in queryParameters)
                        {
                            if (!TypeUtility.IsPredefinedType(param.ParameterType))
                            {
                                error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, methodName, param.Name));
                                return false;
                            }
                        }

                        // Determine the entity Type and validate the return type
                        // (must return IEnumerable<T> or a singleton)
                        bool isSingleton = false;
                        Type entityType = DomainServiceDescription.GetQueryEntityReturnType(operationEntry, out isSingleton, out error);
                        if (error != null)
                        {
                            return false;
                        }

                        // validate the entity Type
                        if (entityType != null || !description._entityTypes.Contains(entityType))
                        {
                            string errorMessage;
                            if (!IsValidEntityType(entityType, out errorMessage))
                            {
                                error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.Invalid_Entity_Type, entityType.Name, errorMessage));
                                return false;
                            }
                        }

                        // Only IEnumerable<T> returning query methods can be marked composable
                        if (isSingleton && ((QueryAttribute)operationEntry.OperationAttribute).IsComposable)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_SingletonQueryMethodCannotCompose, methodName, returnType));
                        }

                        break;
                    }
                case DomainOperation.Custom:
                    {
                        // check that the method signature is conforming to our expectations (Entity, one or more of pre-defined types)
                        if (parameters.Count == 0)
                        {
                            error = new InvalidOperationException(Resource.InvalidCustomMethod_MethodCannotBeParameterless);
                            return false;
                        }
                        bool first = true;
                        foreach (DomainOperationParameter param in parameters)
                        {
                            if (first)
                            {
                                // if first parameter, ensure that its type is one of the Entity types associated with CRUD.
                                if (!description._entityTypes.Contains(param.ParameterType))
                                {
                                    error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainMethod_ParamMustBeEntity, param.Name, methodName));
                                    return false;
                                }
                                first = false;
                            }
                            else if (!TypeUtility.IsPredefinedType(param.ParameterType) && !TypeUtility.IsSupportedComplexType(param.ParameterType))
                            {
                                error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, methodName, param.Name));
                                return false;
                            }
                        }
                        break;
                    }
                case DomainOperation.Invoke:
                    {
                        foreach (DomainOperationParameter param in parameters)
                        {
                            // parameter Type must be one of the predefined types, a supported complex type, an entity or collection of entities
                            if (!description._entityTypes.Contains(param.ParameterType) && !TypeUtility.IsPredefinedType(param.ParameterType) && !TypeUtility.IsSupportedComplexType(param.ParameterType))
                            {
                                // see if the parameter type is a supported collection of entities
                                Type elementType = TypeUtility.GetElementType(param.ParameterType);
                                bool isEntityCollection = description._entityTypes.Contains(elementType) && TypeUtility.IsSupportedCollectionType(param.ParameterType);
                                if (!isEntityCollection)
                                {
                                    error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ParameterType, methodName));
                                    return false;
                                }
                            }
                        }

                        // return Type must be one of the predefined types, an entity or collection of entities
                        if (returnType != typeof(void) && !description._entityTypes.Contains(returnType) && !TypeUtility.IsPredefinedType(returnType) && !TypeUtility.IsSupportedComplexType(returnType))
                        {
                            // see if the return is a supported collection of entities
                            Type elementType = TypeUtility.GetElementType(returnType);
                            bool isEntityCollection = description._entityTypes.Contains(elementType) && TypeUtility.IsSupportedCollectionType(returnType);
                            if (!isEntityCollection)
                            {
                                error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ReturnType, methodName));
                                return false;
                            }
                        }
                        break;
                    }
            }

            // return type should be void for domain operations which are not invoke or query
            if (operation != DomainOperation.Invoke && operation != DomainOperation.Query && returnType != typeof(void))
            {
                error = new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_NonQueryMustReturnVoid, methodName));
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Checks whether a parameter expects a value to be passed by-value.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <returns>True if the parameter expects a by-value value.</returns>
        private static bool IsByVal(DomainOperationParameter parameter)
        {
            return !parameter.IsOut && !parameter.ParameterType.IsByRef;
        }

        /// <summary>
        /// Add a Custom method to this domain service. The first param is assumed to be the entity,
        /// all other params should be one of the predefined types
        /// </summary>
        /// <param name="method">the custom method to be added</param>
        private void AddCustomMethod(DomainOperationEntry method)
        {
            ValidateMethodSignature(this, method);

            string methodName = method.Name;
            Type entityType = method.Parameters[0].ParameterType;

            // We cache a Dictionary<entity type, Dictionary<method name, DomainOperationEntry>> in customMethods so that we can 
            // easily retrieve back the custom method for a particular type later on. Here we also validate that the
            // custom method is not multiply defined for a given entity type
            Dictionary<string, DomainOperationEntry> entityCustomMethods = null;
            if (!this._customMethods.TryGetValue(entityType, out entityCustomMethods))
            {
                entityCustomMethods = new Dictionary<string, DomainOperationEntry>();
                this._customMethods[entityType] = entityCustomMethods;
            }
            else if (entityCustomMethods.ContainsKey(methodName))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntryOverload_NotSupported, methodName));
            }
            entityCustomMethods[methodName] = method;
        }

        /// <summary>
        /// Add an invoke operation to this domain service. All params have to be either of one of the
        /// predefined types, or of an entity type defined on the provider.
        /// </summary>
        /// <param name="method">the invoke operationto be added</param>
        private void AddInvokeOperation(DomainOperationEntry method)
        {
            ValidateMethodSignature(this, method);

            string methodName = method.Name;
            if (this._invokeOperations.ContainsKey(methodName))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntryOverload_NotSupported, methodName));
            }

            this._invokeOperations[methodName] = method;
        }

        /// <summary>
        /// Validates that every CUD method (aka domain operation) exposed on a derived
        /// entity is also exposed on that entity's root.
        /// </summary>
        /// <exception cref="InvalidOperationException">if any derived entity exposes a domain operation not also on the root.</exception>
        private void ValidateDerivedDomainOperations()
        {
            HashSet<Type> rootEntityTypes = GetOrCreateRootEntityTypes();
            IEnumerable<Type> derivedEntityTypes = this.EntityTypes.Where(t => !rootEntityTypes.Contains(t));
            DomainOperation[] allDomainOperations = new DomainOperation[] { DomainOperation.Insert, DomainOperation.Update, DomainOperation.Delete };

            foreach (Type derivedType in derivedEntityTypes)
            {
                Type rootType = this.GetRootEntityType(derivedType);

                // Loop over Insert, Update, Delete for each entity
                foreach (DomainOperation domainOperation in allDomainOperations)
                {
                    DomainOperationEntry derivedOperation = this.GetSubmitMethod(derivedType, domainOperation);
                    if (derivedOperation != null)
                    {
                        DomainOperationEntry rootOperation = this.GetSubmitMethod(rootType, domainOperation);
                        if (rootOperation == null)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainOperation_Required_On_Root, derivedOperation.Name, derivedType.Name, rootType.Name));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified change operation is supported for the specified Type.
        /// If the Type is the child of one or more composition relationships, operation support
        /// takes parent support into account.
        /// </summary>
        /// <param name="entityType">The entity Type to check.</param>
        /// <param name="operationType">The operation Type to check. Must be one of the
        /// change operation types Insert, Update or Delete.</param>
        /// <returns><c>True</c> if the operation is supported, <c>false</c> otherwise.</returns>
        public bool IsOperationSupported(Type entityType, DomainOperation operationType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (operationType != DomainOperation.Insert &&
                operationType != DomainOperation.Update &&
                operationType != DomainOperation.Delete)
            {
                throw new ArgumentOutOfRangeException(nameof(operationType));
            }

            HashSet<Type> visited = new HashSet<Type>();
            return this.IsOperationSupportedInternal(entityType, operationType, false, visited);
        }

        /// <summary>
        /// Recursive helper for <see cref="IsOperationSupported"/>. This method checks support
        /// for the type directly, then checks composing parents as well.
        /// </summary>
        /// <param name="entityType">The entity Type to check.</param>
        /// <param name="operationType">The operation Type to check.</param>
        /// <param name="isParent">True if the check should use compositional parent rules.</param>
        /// <param name="visited">Visited map used during recursion.</param>
        /// <returns><c>True</c> if the operation is supported, <c>false</c> otherwise.</returns>
        private bool IsOperationSupportedInternal(Type entityType, DomainOperation operationType, bool isParent, HashSet<Type> visited)
        {
            if (this.GetSubmitMethod(entityType, operationType) != null)
            {
                // an explicit operation exists
                return true;
            }

            if (operationType == DomainOperation.Update || isParent)
            {
                // when checking operation support for a composed child,
                // if the parent supports Update, all operations are supported
                // for the child
                bool canUpdate = this.GetSubmitMethod(entityType, DomainOperation.Update) != null
                                 || this.GetCustomMethods(entityType).Any();
                if (canUpdate)
                {
                    return true;
                }
            }

            // Avoid infinite recursion in the case of composition cycles
            if (visited.Contains(entityType))
            {
                return false;
            }
            visited.Add(entityType);

            // for compositional children, if the parent supports the operation (or supports
            // Update) the operation is supported.
            foreach (PropertyDescriptor parentAssociation in this.GetParentAssociations(entityType))
            {
                if (this.IsOperationSupportedInternal(parentAssociation.ComponentType, operationType, true, visited))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Computes the closure of known types for all the known entities.
        /// See <see cref="EntityKnownTypes"/>
        /// </summary>
        /// <returns>A dictionary, keyed by entity type and containing all the
        /// declared known types for it, including the transitive closure.
        /// </returns>
        private Dictionary<Type, HashSet<Type>> ComputeEntityKnownTypes()
        {
            Dictionary<Type, HashSet<Type>> closure = new Dictionary<Type, HashSet<Type>>();

            // Gather all the explicit known types from attributes.
            // Because we ask to inherit [KnownType], we will collect the full closure
            foreach (Type entityType in this.EntityTypes)
            {
                // Get all [KnownType]'s and subselect only those that actually derive from this entity
                IEnumerable<Type> knownTypes = KnownTypeUtilities.ImportKnownTypes(entityType, /* inherit */ true).Where(t => entityType.IsAssignableFrom(t));
                closure[entityType] = new HashSet<Type>(knownTypes);
            }

            // 2nd pass -- add all the derived types' known types back to their base so we have the closure
            foreach (Type entityType in this.EntityTypes)
            {
                IEnumerable<Type> knownTypes = closure[entityType];
                for (Type baseType = this.GetEntityBaseType(entityType);
                     baseType != null;
                     baseType = this.GetEntityBaseType(baseType))
                {
                    HashSet<Type> hash = closure[baseType];
                    foreach (Type knownType in knownTypes)
                    {
                        hash.Add(knownType);
                    }
                }
            }
            return closure;
        }

        /// <summary>
        /// Returns the collection of all entity types derived from <paramref name="entityType"/>
        /// </summary>
        /// <param name="entityType">The entity type whose derived types are needed.</param>
        /// <returns>The collection of derived types.  It may be empty.</returns>
        internal IEnumerable<Type> GetEntityDerivedTypes(Type entityType)
        {
            System.Diagnostics.Debug.Assert(entityType != null, "GetEntityDerivedTypes(null) not allowed");
            return this.EntityTypes.Where(et => et != entityType && entityType.IsAssignableFrom(et));
        }

        /// <summary>
        /// Gets all the root entity types exposed by the <see cref="DomainService"/>
        /// </summary>
        private HashSet<Type> GetOrCreateRootEntityTypes()
        {
            if (this._rootEntityTypes == null)
            {
                this._rootEntityTypes = new HashSet<Type>();
                foreach (Type entityType in this.EntityTypes)
                {
                    if (entityType == this.GetRootEntityType(entityType))
                    {
                        this._rootEntityTypes.Add(entityType);
                    }
                }
            }
            return this._rootEntityTypes;
        }
    }
}
