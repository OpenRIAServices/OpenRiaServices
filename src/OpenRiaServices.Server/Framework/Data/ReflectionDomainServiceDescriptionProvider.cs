using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Default reflection based description provider that implements both the attribute
    /// and convention based description models.
    /// </summary>
    internal class ReflectionDomainServiceDescriptionProvider : DomainServiceDescriptionProvider
    {
        private static readonly string[] deletePrefixes = { "Delete", "Remove" };
        private static readonly string[] insertPrefixes = { "Insert", "Add", "Create" };
        private static readonly string[] updatePrefixes = { "Update", "Change", "Modify" };

        public ReflectionDomainServiceDescriptionProvider(Type domainServiceType)
            : base(domainServiceType, null)
        {
        }

        public override DomainServiceDescription GetDescription()
        {
            DomainServiceDescription description = base.GetDescription();

            // get all type-level attributes
            description.Attributes = ReflectionDomainServiceDescriptionProvider.GetDomainServiceTypeAttributes(description.DomainServiceType);

            // get all public candidate methods and create the operations
            List<ReflectionDomainOperationEntry> operationEntries = new List<ReflectionDomainOperationEntry>();
            IEnumerable<MethodInfo> methodsToInspect =
                description.DomainServiceType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => (p.DeclaringType != typeof(DomainService) && (p.DeclaringType != typeof(object))) && !p.IsSpecialName);

            foreach (MethodInfo method in methodsToInspect)
            {
                // We need to ensure the buddy metadata provider is registered before we
                // attempt to do convention, since we rely on IsEntity which relies on
                // KeyAttributes being present
                RegisterAssociatedMetadataProvider(method);

                if (method.GetCustomAttributes(typeof(IgnoreAttribute), false).Length > 0)
                {
                    continue;
                }

                if (method.IsVirtual && method.GetBaseDefinition().DeclaringType == typeof(DomainService))
                {
                    // don't want to infer overrides of DomainService virtual methods as
                    // operations
                    continue;
                }

                ReflectionDomainOperationEntry operation = new ReflectionDomainOperationEntry(description.DomainServiceType, method, (DomainOperation)(-1));
                if (this.ClassifyDomainOperation(operation))
                {
                    operationEntries.Add(operation);
                }
            }

            foreach (DomainOperationEntry operation in operationEntries)
            {
                description.AddOperation(operation);
            }

            return description;
        }

        /// <summary>
        /// Returns true if the Type has at least one member marked with KeyAttribute.
        /// </summary>
        /// <param name="type">The Type to check.</param>
        /// <returns>True if the Type is an entity, false otherwise.</returns>
        public override bool LookupIsEntityType(Type type)
        {
            return TypeDescriptor.GetProperties(type).Cast<PropertyDescriptor>().Any(p => p.Attributes[typeof(KeyAttribute)] != null);
        }

        /// <summary>
        /// Register the associated metadata provider for Types in the signature
        /// of the specified method as required.
        /// </summary>
        /// <param name="methodInfo">The method to register for.</param>
        private static void RegisterAssociatedMetadataProvider(MethodInfo methodInfo)
        {
            Type type = TypeUtility.GetElementType(methodInfo.ReturnType);
            if (type != typeof(void) && type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).Length != 0)
            {
                ReflectionDomainServiceDescriptionProvider.RegisterAssociatedMetadataTypeTypeDescriptor(type);
            }
            foreach (ParameterInfo parameter in methodInfo.GetParameters())
            {
                type = parameter.ParameterType;
                if (type != typeof(void) && type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).Length != 0)
                {
                    ReflectionDomainServiceDescriptionProvider.RegisterAssociatedMetadataTypeTypeDescriptor(type);
                }
            }
        }

        /// <summary>
        /// Verifies that the <see cref="MetadataTypeAttribute"/> reference does not contain a cyclic reference and 
        /// registers the AssociatedMetadataTypeTypeDescriptionProvider in that case.
        /// </summary>
        /// <param name="type">The entity type with the MetadataType attribute.</param>
        private static void RegisterAssociatedMetadataTypeTypeDescriptor(Type type)
        {
            Type currentType = type;
            HashSet<Type> metadataTypeReferences = new HashSet<Type>();
            metadataTypeReferences.Add(currentType);
            while (true)
            {
                MetadataTypeAttribute attribute = (MetadataTypeAttribute)Attribute.GetCustomAttribute(currentType, typeof(MetadataTypeAttribute));
                if (attribute == null)
                {
                    break;
                }
                else
                {
                    currentType = attribute.MetadataClassType;
                    // If we find a cyclic reference, throw an error. 
                    if (metadataTypeReferences.Contains(currentType))
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resource.CyclicMetadataTypeAttributesFound, type.FullName));
                    }
                    else
                    {
                        metadataTypeReferences.Add(currentType);
                    }
                }
            }

            // If the MetadataType reference chain doesn't contain a cycle, register the use of the AssociatedMetadataTypeTypeDescriptionProvider.
            DomainServiceDescription.RegisterCustomTypeDescriptor(new AssociatedMetadataTypeTypeDescriptionProvider(type), type);
        }


        /// <summary>
        /// This method classifies an operation by setting its its operation type.
        /// </summary>
        /// <remarks>Domain operations are either explicitly marked with attributes, or they follow a naming/signature convention.</remarks>
        /// <param name="operation">The domain operation to inspect.</param>
        /// <returns>True if the operation is attributed or matches convention; false otherwise.</returns>
        private bool ClassifyDomainOperation(ReflectionDomainOperationEntry operation)
        {
            DomainOperation operationType;

            // Check if explicit attributes exist.
            QueryAttribute queryAtt = (QueryAttribute)operation.Attributes[typeof(QueryAttribute)];
            if (queryAtt != null)
            {
                // query method
                operation.Operation = DomainOperation.Query;
                return true;
            }
            else if (operation.Attributes[typeof(InsertAttribute)] != null)
            {
                operationType = DomainOperation.Insert;
            }
            else if (operation.Attributes[typeof(UpdateAttribute)] != null)
            {
                operationType = DomainOperation.Update;
            }
            else if (operation.Attributes[typeof(EntityActionAttribute)] != null)
            {
                operationType = DomainOperation.Custom;
            }
            else if (operation.Attributes[typeof(DeleteAttribute)] != null)
            {
                operationType = DomainOperation.Delete;
            }
            else if (operation.Attributes[typeof(InvokeAttribute)] != null)
            {
                operationType = DomainOperation.Invoke;
            }
            else
            {
                return this.TryClassifyImplicitDomainOperation(operation);
            }

            operation.Operation = operationType;

            return true;
        }

        /// <summary>
        /// Classifies a domain operation based on naming convention.
        /// </summary>
        /// <param name="operation">The domain operation to inspect.</param>
        /// <returns>True if the operation matches a convention; false otherwise.</returns>
        private bool TryClassifyImplicitDomainOperation(ReflectionDomainOperationEntry operation)
        {
            DomainOperation operationType = (DomainOperation)(-1);
            if (operation.ReturnType == typeof(void))
            {
                // Check if this looks like an insert, update or delete method.
                if (insertPrefixes.Any(p => operation.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    operationType = DomainOperation.Insert;
                }
                else if (updatePrefixes.Any(p => operation.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    operationType = DomainOperation.Update;
                }
                else if (deletePrefixes.Any(p => operation.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    operationType = DomainOperation.Delete;
                }
                else if (this.IsCustomMethod(operation))
                {
                    operationType = DomainOperation.Custom;
                }
            }
            else if (this.IsQueryMethod(operation))
            {
                operationType = DomainOperation.Query;
            }

            if ((int)operationType == -1 && IsInvokeOperation(operation))
            {
                operationType = DomainOperation.Invoke;
            }

            if ((int)operationType != -1)
            {
                operation.Operation = operationType;
                operation.IsInferred = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the specified operation should be inferred as an
        /// IEnumerable or singleton returning query method. Also checks if the 
        /// base element type or the type of the singleton is a valid entity type.
        /// </summary>
        /// <param name="operation">The operation to inspect.</param>
        /// <returns>True if the operation is a query method, false otherwise.</returns>
        private bool IsQueryMethod(DomainOperationEntry operation)
        {
            Type elementType = TypeUtility.GetElementType(operation.ReturnType);
            return this.IsEntityType(elementType);
        }

        /// <summary>
        /// We need to avoid false positive convention based classification of
        /// Custom methods. This method verifies that candidate Custom methods
        /// have the correct signature.
        /// </summary>
        /// <param name="operation">The operation to inspect</param>
        /// <returns>True if the method has a valid signature</returns>
        private bool IsCustomMethod(ReflectionDomainOperationEntry operation)
        {
            DomainOperationParameter[] parameters = operation.Parameters.ToArray();
            if (parameters.Length == 0 || operation.ReturnType != typeof(void))
            {
                return false;
            }

            bool first = true;
            foreach (DomainOperationParameter parameter in operation.Parameters)
            {
                if (first)
                {
                    // first parameter must be an entity type
                    if (!this.IsEntityType(parameter.ParameterType))
                    {
                        return false;
                    }
                    first = false;
                }
                else if (!IsPredefinedOrComplexType(parameter.ParameterType))
                {
                    // all other parameters must be supported serializable types
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This method returns a collection of attributes representing attributes on the <see cref="DomainService"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="domainServiceType">The <see cref="DomainService"/> <see cref="Type"/>.</param>
        /// <returns>An <see cref="AttributeCollection"/> decorated on the <see cref="Type"/> directly as well as attributes surfaced from interface
        /// implementations.</returns>
        private static AttributeCollection GetDomainServiceTypeAttributes(Type domainServiceType)
        {
            // Get attributes from TypeDescriptor
            List<Attribute> attributes = new List<Attribute>(domainServiceType.Attributes().Cast<Attribute>());

            // All evaluate attributes that exist in interface implementations.
            Type[] interfaces = domainServiceType.GetInterfaces();
            foreach (Type interfaceType in interfaces)
            {
                // Merge attributes together
                ReflectionDomainServiceDescriptionProvider.MergeAttributes(
                    attributes,
                    interfaceType.Attributes().Cast<Attribute>());
            }

            return new AttributeCollection(attributes.ToArray());
        }

        /// <summary>
        /// Returns true if the specified operation should be inferred as an invoke operation.
        /// </summary>
        /// <param name="operation">The operation to inspect.</param>
        /// <returns>True if the operation is an invoke operation, false otherwise.</returns>
        private static bool IsInvokeOperation(DomainOperationEntry operation)
        {
            foreach (DomainOperationParameter parameter in operation.Parameters)
            {
                // All parameters must be supported Types.
                // If there are any entity type parameters, then it's not an invoke operation that follows the 
                // convention. The developer will need to explicitly mark the operation with InvokeAttribute.
                if (!IsPredefinedOrComplexType(parameter.ParameterType))
                {
                    return false;
                }
            }

            // We'll match service operations by convention only if they are void returning,
            // return a simple or complex type, or a collection thereof. 
            // All methods not matching this convention must be explicitly attributed (e.g. those returning entities).
            if (operation.ReturnType == typeof(void) || IsPredefinedOrComplexType(operation.ReturnType))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if the given type one of our supported simple types (or a collection thereof),
        /// or if the type is a complex type (or a collection thereof).
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the type is a primitive or one of the accepted types</returns>
        private static bool IsPredefinedOrComplexType(Type type)
        {
            return TypeUtility.IsPredefinedType(type) || TypeUtility.IsComplexType(type) || TypeUtility.IsComplexTypeCollection(type);
        }

        /// <summary>
        /// Merge attributes existing on a type and interface (implemented by type).
        /// Attributes that allow for multiple are aggregated.  Attributes that allow single use are
        /// effectively overridden by class use.  Attributes that allow single use and exist on multiple
        /// interfaces (but not the implementing class) are merged arbitrarily.
        /// </summary>
        /// <param name="typeAttributes">DomainService type-level attributes.</param>
        /// <param name="interfaceAttributes">Interface type-level attributes to merge.</param>
        private static void MergeAttributes(List<Attribute> typeAttributes, IEnumerable<Attribute> interfaceAttributes)
        {
            foreach (Attribute interfaceAttribute in interfaceAttributes)
            {
                AttributeUsageAttribute attributeUsage = (AttributeUsageAttribute)TypeDescriptor.GetAttributes(interfaceAttribute)[typeof(AttributeUsageAttribute)];

                // Check if the attribute itself is valid on a class.  (It is unlikely but possible for a TD to provide invalid attributes.)
                bool attributeValidOnTarget = (attributeUsage == null) || ((attributeUsage.ValidOn & AttributeTargets.Class) == AttributeTargets.Class);
                if (!attributeValidOnTarget)
                {
                    typeAttributes.Remove(interfaceAttribute);
                }

                // Check if the attribute allows for multiple use
                bool allowMultiple = (attributeUsage != null) && (attributeUsage.AllowMultiple);
                if (!allowMultiple)
                {
                    // If single use only, check for a conflict and conditionally remove
                    if (typeAttributes.Count(attribute => attribute.GetType() == interfaceAttribute.GetType()) > 1)
                    {
                        typeAttributes.Remove(interfaceAttribute);
                    }
                }
            }
        }

        /// <summary>
        /// Reflection based domain operation entry
        /// </summary>
        internal class ReflectionDomainOperationEntry : DomainOperationEntry
        {
            private bool _isInferred;
            private readonly Func<DomainService, object[], object> _method;

            /// <summary>
            /// Creates an instance of a <see cref="ReflectionDomainOperationEntry"/>.
            /// </summary>
            /// <param name="domainServiceType">The DomainService Type the method belongs to.</param>
            /// <param name="methodInfo">The MethodInfo of the method.</param>
            /// <param name="operation">The operation.</param>         
            public ReflectionDomainOperationEntry(Type domainServiceType, MethodInfo methodInfo, DomainOperation operation)
                : base(domainServiceType, methodInfo.Name, operation, methodInfo.ReturnType, GetMethodParameters(methodInfo), GetAttributeCollection(methodInfo))
            {
                if (methodInfo == null)
                {
                    throw new ArgumentNullException(nameof(methodInfo));
                }

                // Generic methods aren’t supported, and will be caught during DomainServiceDescription validation.
                if (!methodInfo.IsGenericMethodDefinition)
                {
                    this._method = DynamicMethodUtility.GetDelegateForMethod(methodInfo);
                }
            }

            /// <summary>
            /// Gets a value indicating whether this operation was inferred, or is explicitly attributed.
            /// </summary>
            public bool IsInferred
            {
                get
                {
                    return this._isInferred;
                }
                set
                {
                    this._isInferred = value;
                }
            }

            /// <summary>
            /// Invokes this <see cref="DomainOperationEntry" />.
            /// </summary>
            /// <param name="domainService">The <see cref="DomainService"/> instance the operation is being invoked on.</param>
            /// <param name="parameters">The parameters to pass to the method.</param>
            /// <param name="cancellationToken">A cancellation token that can be used to signal cancellation of this operation</param>
            /// <remarks>
            ///  Parameter <paramref name="cancellationToken"/> is currently not used.
            /// </remarks>
            /// <returns>The return value of the invoked method.</returns>
            public override ValueTask<object> InvokeAsync(DomainService domainService, object[] parameters, CancellationToken cancellationToken)
            {
                return UnwrapTaskResult(this._method(domainService, parameters));
            }

            private static IEnumerable<DomainOperationParameter> GetMethodParameters(MethodInfo methodInfo)
            {
                ParameterInfo[] actualParameters = methodInfo.GetParameters();
                List<DomainOperationParameter> parameters = new List<DomainOperationParameter>();
                foreach (ParameterInfo parameterInfo in actualParameters)
                {
                    bool isOut = parameterInfo.IsOut && parameterInfo.ParameterType.HasElementType;
                    var Metadata = parameterInfo.ParameterType.GetCustomAttributes(typeof(MetadataTypeAttribute), true).Cast<MetadataTypeAttribute>().FirstOrDefault();
                    DomainOperationParameter parameter = null;
                    if (Metadata == null)
                        parameter = new DomainOperationParameter(
                        parameterInfo.Name,
                        parameterInfo.ParameterType,
                        new AttributeCollection(parameterInfo.GetCustomAttributes(true).Cast<Attribute>().ToArray()),
                        isOut);
                    else
                        parameter = new DomainOperationParameter(
                        parameterInfo.Name,
                        parameterInfo.ParameterType,
                        new AttributeCollection(parameterInfo.GetCustomAttributes(true).Cast<Attribute>().Union(Metadata.MetadataClassType.GetCustomAttributes(true).Cast<Attribute>()).ToArray()),
                        isOut);

                    parameters.Add(parameter);
                }

                return parameters;
            }

            /// <summary>
            /// Returns a collection of attributes that are defined on the <paramref name="methodInfo"/> as well as any
            /// underlying interface definitions.
            /// </summary>
            /// <param name="methodInfo">The <see cref="MethodInfo"/> to return attributes for.</param>
            /// <returns>A collection of attributes.</returns>
            private static AttributeCollection GetAttributeCollection(MethodInfo methodInfo)
            {
                // Get all of the attributes defined on the MethodInfo
                List<Attribute> attributes = new List<Attribute>(methodInfo.GetCustomAttributes(true).Cast<Attribute>());

                // Filter out AsyncStateMachineAttribute for "async" methods
                attributes.RemoveAll(a => a is System.Runtime.CompilerServices.AsyncStateMachineAttribute);

                // Examine interfaces on the type and try to find matching method implmentations.
                foreach (Type interfaceType in methodInfo.DeclaringType.GetInterfaces())
                {
                    InterfaceMapping interfaceMapping = methodInfo.DeclaringType.GetInterfaceMap(interfaceType);
                    for (int i = 0; i < interfaceMapping.TargetMethods.Length; ++i)
                    {
                        if (interfaceMapping.TargetMethods[i].MethodHandle == methodInfo.MethodHandle)
                        {
                            // Merge method attributes with those found on the interface method definition
                            ReflectionDomainOperationEntry.MergeAttributes(
                                attributes,
                                interfaceMapping.InterfaceMethods[i].GetCustomAttributes(true).Cast<Attribute>());
                            break;
                        }
                    }
                }

                return new AttributeCollection(attributes.ToArray());
            }

            /// <summary>
            /// Merge attributes already existing on a MethodInfo with those defined at the interface level. 
            /// Here, we will selectively add interface attributes if they are valid and not in conflict with
            /// any attributes already defined on the method.
            /// </summary>
            /// <param name="methodAttributes">Method-level attributes.</param>
            /// <param name="interfaceAttributes">Interface method-level attributes to merge.</param>
            private static void MergeAttributes(List<Attribute> methodAttributes, IEnumerable<Attribute> interfaceAttributes)
            {
                foreach (Attribute interfaceAttribute in interfaceAttributes)
                {
                    AttributeUsageAttribute attributeUsage = TypeDescriptor.GetAttributes(interfaceAttribute).OfType<AttributeUsageAttribute>().SingleOrDefault();

                    // Check if the attribute is valid on a method. (It's unlikely but possible for a TD to return an invalid attribute.)
                    bool attributeValidOnTarget = (attributeUsage == null) || ((attributeUsage.ValidOn & AttributeTargets.Method) == AttributeTargets.Method);
                    if (!attributeValidOnTarget)
                    {
                        continue;
                    }

                    // Check if the attribute allows for multiple use.
                    bool allowMultiple = (attributeUsage != null) && (attributeUsage.AllowMultiple);
                    if (allowMultiple)
                    {
                        methodAttributes.Add(interfaceAttribute);
                        continue;
                    }

                    // Check if the single-use attribute is in conflict.
                    bool attributeInConflict = methodAttributes.Any(attribute => attribute.GetType() == interfaceAttribute.GetType() && attribute != interfaceAttribute);
                    if (!attributeInConflict)
                    {
                        methodAttributes.Add(interfaceAttribute);
                        continue;
                    }
                }
            }
        }
    }
}
