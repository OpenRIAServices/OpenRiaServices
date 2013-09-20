namespace Microsoft.ServiceModel.DomainServices.Tools.TextTemplate
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security.Principal;
    using System.ServiceModel.DomainServices;
    using System.ServiceModel.DomainServices.Server;
    using System.ServiceModel.DomainServices.Server.ApplicationServices;

    /// <summary>
    /// Proxy generator for an entity.
    /// </summary>
    public abstract partial class EntityGenerator
    {
        private DomainServiceDescriptionAggregate _domainServiceDescriptionAggregate;
        Type _visibleBaseType;
        private List<PropertyDescriptor> _associationProperties;

        /// <summary>
        /// Gets the list of all the DomainServiceDescription objects.
        /// </summary>
        protected IEnumerable<DomainServiceDescription> DomainServiceDescriptions { get; private set; }

        /// <summary>
        /// Generates the entity class on the client.
        /// </summary>
        /// <param name="entityType">The type of the entity to be generated</param>
        /// <param name="domainServiceDescriptions">The list of all the DomainServiceDescription objects.</param>
        /// <param name="clientCodeGenerator">The ClientCodeGenerator object for this instance.</param>
        /// <returns>Generated entity class code.</returns>
        public string Generate(Type entityType, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerator clientCodeGenerator)
        {
            this.Type = entityType;
            this.ClientCodeGenerator = clientCodeGenerator;
            this.DomainServiceDescriptions = domainServiceDescriptions;

            return this.GenerateDataContractProxy();
        }

        internal override void Initialize()
        {
            this._domainServiceDescriptionAggregate = new DomainServiceDescriptionAggregate(this.DomainServiceDescriptions.Where(dsd => dsd.EntityTypes.Contains(this.Type)));
            this._visibleBaseType = this._domainServiceDescriptionAggregate.GetEntityBaseType(this.Type);
            this.GenerateGetIdentity = !this.IsDerivedType;
            this._associationProperties = new List<PropertyDescriptor>();
            base.Initialize();
        }

        internal bool GenerateGetIdentity { get; private set; }

        internal override bool IsDerivedType
        {
            get
            {
                return this._visibleBaseType != null;
            }
        }

        internal override IEnumerable<Type> ComplexTypes
        {
            get
            {
                return this._domainServiceDescriptionAggregate.ComplexTypes;
            }
        }

        internal IEnumerable<PropertyDescriptor> AssociationProperties
        {
            get
            {
                if (this._associationProperties == null)
                {
                    this._associationProperties = new List<PropertyDescriptor>();
                }
                return this._associationProperties;
            }
        }

        internal override bool CanGenerateProperty(PropertyDescriptor propertyDescriptor)
        {
            // Check if it is an excluded property
            if (propertyDescriptor.Attributes[typeof(ExcludeAttribute)] != null)
            {
                return false;
            }

            // Check if it is an external reference.
            if (propertyDescriptor.Attributes[typeof(ExternalReferenceAttribute)] != null)
            {
                return true;
            }

            // The base can't generate the property, it could be an association which we know how to generate.
            if (!base.CanGenerateProperty(propertyDescriptor))
            {
                AttributeCollection propertyAttributes = propertyDescriptor.ExplicitAttributes();
                bool hasKeyAttr = (propertyAttributes[typeof(KeyAttribute)] != null);

                // If we can't generate Key property, log a VS error (this will cancel CodeGen effectively)
                if (hasKeyAttr)
                {
                    // Property must not be serializable based on attributes (e.g. no DataMember), because 
                    // we already checked its type which was fine.
                    this.ClientCodeGenerator.CodeGenerationHost.LogError(string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.EntityCodeGen_EntityKey_PropertyNotSerializable,
                        this.Type, propertyDescriptor.Name));

                    return false;
                }

                // Get the implied element type (e.g. int32[], Nullable<int32>, IEnumerable<int32>)
                // If the ultimate element type is not allowed, it's not acceptable, no matter whether
                // this is an array, Nullable<T> or whatever
                Type elementType = TypeUtility.GetElementType(propertyDescriptor.PropertyType);
                if (!this._domainServiceDescriptionAggregate.EntityTypes.Contains(elementType) || (propertyDescriptor.Attributes[typeof(AssociationAttribute)] == null))
                {
                    // If the base class says we can't generate the property, it is because the property is not serializable.
                    // The only other type entity would serialize is associations. Since it is not, return now.
                    return false;
                }
            }

            // Ensure the property is not virtual, abstract or new
            // If there is a violation, we log the error and keep
            // running to accumulate all such errors.  This function
            // may return an "okay" for non-error case polymorphics.
            if (!this.CanGeneratePropertyIfPolymorphic(propertyDescriptor))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified property belonging to the
        /// current entity is polymorphic, and if so whether it is legal to generate it.
        /// </summary>
        /// <param name="pd">The property to validate</param>
        /// <returns><c>true</c> if it is not polymorphic or it is legal to generate it, else <c>false</c></returns>
        internal override bool CanGeneratePropertyIfPolymorphic(PropertyDescriptor pd)
        {
            PropertyInfo propertyInfo = this.Type.GetProperty(pd.Name);

            if (propertyInfo != null)
            {
                if (this.IsMethodPolymorphic(propertyInfo.GetGetMethod()) ||
                    this.IsMethodPolymorphic(propertyInfo.GetSetMethod()))
                {
                    // This property is polymorphic.  To determine whether it is
                    // legal to generate, we determine whether any of our visible
                    // base types also expose this property.  If so, we cannot generate it.
                    foreach (Type baseType in this.GetVisibleBaseTypes(this.Type))
                    {
                        if (baseType.GetProperty(pd.Name) != null)
                        {
                            return false;
                        }
                    }

                    // If get here, we have not generated an entity in the hierarchy between the
                    // current entity type and the entity that declared this.  This means it is
                    // save to generate
                    return true;
                }
            }
            return true;
        }

        private IEnumerable<Type> GetVisibleBaseTypes(Type entityType)
        {
            List<Type> types = new List<Type>();
            for (Type baseType = this._domainServiceDescriptionAggregate.GetEntityBaseType(entityType);
                 baseType != null;
                 baseType = this._domainServiceDescriptionAggregate.GetEntityBaseType(baseType))
            {
                types.Add(baseType);
            }
            return types;
        }


        private bool IsMethodPolymorphic(MethodInfo methodInfo)
        {
            // Null allowed for convenience.
            // If method is declared on a different entity type, then one of 2
            // things will be true:
            //  1. The declaring type is invisible, in which case it is not a problem
            //  2. The declaring type is visible, in which case the error is reported there.
            if (methodInfo == null || methodInfo.DeclaringType != this.Type)
            {
                return false;
            }

            // Virtual methods are disallowed.
            // But the CLR marks interface methods IsVirtual=true, so the
            // recommended test is this one.
            if (methodInfo.IsVirtual && !methodInfo.IsFinal)
            {
                return true;
            }

            // Detecting the "new" keyword requires a check whether this method is
            // hiding a method with the same signature in a derived type.  IsHideBySig does not do this.
            if (this.Type.BaseType != null)
            {
                Type[] parameterTypes = methodInfo.GetParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray();
                MethodInfo baseMethod = this.Type.BaseType.GetMethod(methodInfo.Name, parameterTypes);
                if (baseMethod != null)
                {
                    return true;
                }
            }

            return false;
        }


        internal override bool ShouldDeclareProperty(PropertyDescriptor pd)
        {
            if (!base.ShouldDeclareProperty(pd))
            {
                return false;
            }

            // Inheritance: when dealing with derived entities, we need to
            // avoid generating a property already on the base.  But we also
            // need to account for flattening (holes in the exposed hiearchy.
            // This helper method encapsulates that logic.
            if (!this.ShouldFlattenProperty(pd))
            {
                return false;
            }

            AttributeCollection propertyAttributes = pd.ExplicitAttributes();
            Type propertyType = pd.PropertyType;

            // The [Key] attribute means this property is part of entity key
            bool hasKeyAttr = (propertyAttributes[typeof(KeyAttribute)] != null);

            if (hasKeyAttr)
            {
                if (!TypeUtility.IsPredefinedSimpleType(propertyType))
                {
                    this.ClientCodeGenerator.CodeGenerationHost.LogError(string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.EntityCodeGen_EntityKey_KeyTypeNotSupported,
                        this.Type, pd.Name, propertyType));
                    return false;
                }
            }

            return true;
        }

        internal override bool HandleNonSerializableProperty(PropertyDescriptor propertyDescriptor)
        {
            AttributeCollection propertyAttributes = propertyDescriptor.ExplicitAttributes();
            AssociationAttribute associationAttr = (AssociationAttribute)propertyAttributes[typeof(AssociationAttribute)];
            bool externalReference = propertyAttributes[typeof(ExternalReferenceAttribute)] != null;

            if (associationAttr != null)
            {
                this.AddAssociationToGenerate(propertyDescriptor);
                return true;
            }

            return false;
        }

        private void AddAssociationToGenerate(PropertyDescriptor pd)
        {
            Type associationType =
                IsCollectionType(pd.PropertyType) ?
                    TypeUtility.GetElementType(pd.PropertyType) :
                    pd.PropertyType;

            if (!CodeGenUtilities.RegisterTypeName(associationType, this.Type.Namespace))
            {
                IEnumerable<Type> potentialConflicts =
                    this.ClientCodeGenerator.DomainServiceDescriptions
                        .SelectMany<DomainServiceDescription, Type>(dsd => dsd.EntityTypes)
                            .Where(entity => entity.Namespace == associationType.Namespace).Distinct();

                foreach (Type potentialConflict in potentialConflicts)
                {
                    CodeGenUtilities.RegisterTypeName(potentialConflict, this.Type.Namespace);
                }
            }

            this._associationProperties.Add(pd);
        }

        internal void GetKeysInfo(out string[] keyNames, out string[] nullableKeyNames)
        {
            var keys = new List<string>();
            var nullableKeys = new List<string>();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this.Type);
            foreach (PropertyDescriptor prop in properties)
            {
                if (prop.Attributes[typeof(KeyAttribute)] as KeyAttribute != null)
                {
                    string propName = CodeGenUtilities.MakeCompliantFieldName(prop.Name);
                    keys.Add(propName);
                    if (TypeUtility.IsNullableType(prop.PropertyType) || !prop.PropertyType.IsValueType)
                    {
                        nullableKeys.Add(propName);
                    }
                }
            }

            keyNames = keys.ToArray();
            nullableKeyNames = nullableKeys.ToArray();
        }

        private bool ShouldFlattenProperty(PropertyDescriptor propertyDescriptor)
        {
            Type declaringType = propertyDescriptor.ComponentType;

            // If this property is declared by the current entity type,
            // the answer is always 'yes'
            if (declaringType == this.Type)
            {
                return true;
            }

            // If this is a projection property (meaning it is declared outside this hierarchy),
            // then the answer is "yes, declare it"
            if (!declaringType.IsAssignableFrom(this.Type))
            {
                return true;
            }

            // If it is declared in any visible entity type, the answer is 'no'
            // because it will be generated with that entity
            if (this._domainServiceDescriptionAggregate.EntityTypes.Contains(declaringType))
            {
                return false;
            }

            // This property is defined in an entity that is not among the known types.
            // We may need to lift it during code generation.  But beware -- if there
            // are multiple gaps in the hierarchy, we want to lift it only if some other
            // visible base type of ours has not already done so.
            Type baseType = this.Type.BaseType;

            while (baseType != null)
            {
                // If we find the declaring type, we know from the test above it is
                // not a known entity type.  The first such non-known type is grounds
                // for us lifting its properties.
                if (baseType == declaringType)
                {
                    return true;
                }

                // The first known type we encounter walking toward the base type
                // will generate it, so we must not.
                if (this._domainServiceDescriptionAggregate.EntityTypes.Contains(baseType))
                {
                    break;
                }

                // Note: we explicitly allow this walkback to cross past
                // the visible root, examining types lower than our root.
                baseType = baseType.BaseType;
            }
            return false;
        }

        internal override bool IsPropertyShared(PropertyDescriptor pd)
        {
            if (base.IsPropertyShared(pd))
            {
                if (pd.ExplicitAttributes()[typeof(KeyAttribute)] != null)
                {
                    // If there are any shared key members, don't generate GetIdentity method. 
                    // Default implementation of GetIdentity on the Entity class will be used.
                    this.GenerateGetIdentity = false;
                }
                return true;
            }

            return false;
        }

        internal override void OnPropertySkipped(PropertyDescriptor pd)
        {
            AttributeCollection propertyAttributes = pd.ExplicitAttributes();
            bool hasKeyAttr = (propertyAttributes[typeof(KeyAttribute)] != null);

            // If we can't generate Key property, log a VS error (this will cancel CodeGen effectively)
            if (hasKeyAttr)
            {
                // Property must not be serializable based on attributes (e.g. no DataMember), because 
                // we already checked its type which was fine.
                this.ClientCodeGenerator.CodeGenerationHost.LogError(string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.EntityCodeGen_EntityKey_PropertyNotSerializable,
                    this.Type, pd.Name));
            }
        }

        internal override IEnumerable<Type> GetDerivedTypes()
        {
            return this._domainServiceDescriptionAggregate.EntityTypes.Where(t => t != this.Type && this.Type.IsAssignableFrom(t));
        }

        internal override string GetBaseTypeName()
        {            
            string baseTypeName = string.Empty;
            if (this._visibleBaseType != null)
            {
                baseTypeName = CodeGenUtilities.GetTypeName(this._visibleBaseType);
            }
            else
            {
                baseTypeName = "System.ServiceModel.DomainServices.Client.Entity";
            }

            // We special case User type to be able to handle the User entity that we define.
            if (this.IsUserType)
            {
                return this.GetBaseTypeNameForUserType(baseTypeName);
            }
            
            return baseTypeName;
        }

        private string GetBaseTypeNameForUserType(string baseTypeName)
        {
            // Add the IIdentity and the IPrincipal interfaces to the User type. 
            baseTypeName = baseTypeName + ", " + CodeGenUtilities.GetTypeNameInGlobalNamespace(typeof(IIdentity));
            baseTypeName = baseTypeName + ", " + CodeGenUtilities.GetTypeNameInGlobalNamespace(typeof(IPrincipal));
            return baseTypeName;
        }

        internal bool IsUserType
        {
            get
            {                
                return typeof(IUser).IsAssignableFrom(this.Type);
            }
        }

        private class DomainServiceDescriptionAggregate
        {
            private HashSet<Type> _complexTypes;
            private HashSet<Type> _entityTypes;

            /// <summary>
            /// Initializes a new instance of the <see cref="DomainServiceDescriptionAggregate"/> class.
            /// </summary>
            /// <param name="domainServiceDescriptions">The descriptions that exposes the entity type.</param>
            internal DomainServiceDescriptionAggregate(IEnumerable<DomainServiceDescription> domainServiceDescriptions)
            {
                this.DomainServiceDescriptions = domainServiceDescriptions;
                this._complexTypes = new HashSet<Type>();
                this._entityTypes = new HashSet<Type>();

                foreach (var dsd in domainServiceDescriptions)
                {
                    foreach (var complexType in dsd.ComplexTypes)
                    {
                        this._complexTypes.Add(complexType);
                    }

                    foreach (var entityType in dsd.EntityTypes)
                    {
                        this._entityTypes.Add(entityType);
                    }
                }
            }

            /// <summary>
            /// Gets all the <see cref="DomainServiceDescription"/> that expose this entity.
            /// </summary>
            internal IEnumerable<DomainServiceDescription> DomainServiceDescriptions
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets an enumerable representing the union of all complex types exposed
            /// by each <see cref="DomainServiceDescription"/>.
            /// </summary>
            internal IEnumerable<Type> ComplexTypes
            {
                get
                {
                    foreach (Type complexType in this._complexTypes)
                    {
                        yield return complexType;
                    }
                }
            }

            /// <summary>
            /// Gets an enumerable representing the union of all entity types exposed
            /// by each <see cref="DomainServiceDescription"/>.
            /// </summary>
            internal IEnumerable<Type> EntityTypes
            {
                get
                {
                    foreach (Type entityType in this._entityTypes)
                    {
                        yield return entityType;
                    }
                }
            }

            /// <summary>
            /// Returns true if the entity is shared.
            /// </summary>
            internal bool IsShared
            {
                get
                {
                    return this.DomainServiceDescriptions.Count() > 1;
                }
            }

            /// <summary>
            /// Gets the base type of the given entity type.
            /// </summary>
            /// <param name="entityType">The entity type whose base type is required.</param>
            /// <returns>The base type or <c>null</c> if the given
            /// <paramref name="entityType"/> had no visible base types.</returns>
            internal Type GetEntityBaseType(Type entityType)
            {
                Type baseType = entityType.BaseType;
                while (baseType != null)
                {
                    if (this.EntityTypes.Contains(baseType))
                    {
                        break;
                    }
                    baseType = baseType.BaseType;
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
            internal Type GetRootEntityType(Type entityType)
            {
                Type rootType = null;
                while (entityType != null)
                {
                    if (this.EntityTypes.Contains(entityType))
                    {
                        rootType = entityType;
                    }
                    entityType = entityType.BaseType;
                }
                return rootType;
            }
        }

        internal static bool IsCollectionType(Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        internal static PropertyDescriptor GetReverseAssociation(PropertyDescriptor propertyDescriptor, AssociationAttribute assocAttrib)
        {
            Type otherType = TypeUtility.GetElementType(propertyDescriptor.PropertyType);

            foreach (PropertyDescriptor entityMember in TypeDescriptor.GetProperties(otherType))
            {
                if (entityMember.Name == propertyDescriptor.Name)
                {
                    // for self associations, both ends of the association are in
                    // the same class and have the same name. Therefore, we need to 
                    // skip the member itself.
                    continue;
                }

                AssociationAttribute otherAssocAttrib = entityMember.Attributes[typeof(AssociationAttribute)] as AssociationAttribute;
                if (otherAssocAttrib != null && otherAssocAttrib.Name == assocAttrib.Name)
                {
                    return entityMember;
                }
            }

            return null;
        }

        internal IEnumerable<DomainOperationEntry> GetEntityCustomMethods()
        {
            Dictionary<string, DomainOperationEntry> entityCustomMethods = new Dictionary<string, DomainOperationEntry>();
            Dictionary<string, DomainServiceDescription> customMethodToDescriptionMap = new Dictionary<string, DomainServiceDescription>();
            string methodName;

            foreach (DomainServiceDescription description in this.DomainServiceDescriptions)
            {
                foreach (DomainOperationEntry customMethod in description.GetCustomMethods(this.Type))
                {
                    methodName = customMethod.Name;
                    if (entityCustomMethods.ContainsKey(methodName))
                    {
                        this.ClientCodeGenerator.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_DuplicateCustomMethodName, methodName, this.Type, customMethodToDescriptionMap[methodName].DomainServiceType, description.DomainServiceType));
                    }
                    else
                    {
                        entityCustomMethods.Add(methodName, customMethod);
                        customMethodToDescriptionMap.Add(methodName, description);
                    }
                }
            }
            return entityCustomMethods.Values.AsEnumerable();
        }
    }
}
