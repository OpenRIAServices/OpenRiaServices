using System.ServiceModel;

namespace OpenRiaServices.Tools.TextTemplate
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using OpenRiaServices;
    using OpenRiaServices.Hosting;
    using OpenRiaServices.Server;
    using OpenRiaServices.Tools.SharedTypes;

    /// <summary>
    /// Proxy generator for DomainServices.
    /// </summary>
    public abstract partial class DomainContextGenerator
    {
        private const string QuerySuffix = "Query";

        internal const string DefaultActionSchema = "http://tempuri.org/{0}/{1}";
        internal const string DefaultReplyActionSchema = "http://tempuri.org/{0}/{1}Response";
        internal const string DefaultFaultActionSchema = "http://tempuri.org/{0}/{1}{2}";

        internal string DomainContextTypeName { get; set; }
        internal string ContractInterfaceName { get; set; }
        internal AttributeCollection Attributes { get; set; }

        private List<Type> _entitySets;
        private List<DomainOperationEntry> _queryMethods;
        private List<DomainOperationEntry> _domainOperations;

        /// <summary>
        /// Gets the DomainServiceDescription for the DomainService to be generated.
        /// </summary>
        protected DomainServiceDescription DomainServiceDescription { get; private set; }

        /// <summary>
        /// Gets the ClientCodeGenerator object.
        /// </summary>
        protected ClientCodeGenerator ClientCodeGenerator { get; private set; }

        /// <summary>
        /// Geterates the DomainContext class.
        /// </summary>
        /// <param name="domainServiceDescription">DomainServcieDescription for the domain service for which the proxy is to be generated.</param>
        /// <param name="clientCodeGenerator">ClientCodeGenerator object for this instance.</param>
        /// <returns>The generated DomainContext class code.</returns>
        public string Generate(DomainServiceDescription domainServiceDescription, ClientCodeGenerator clientCodeGenerator)
        {
            this.DomainServiceDescription = domainServiceDescription;
            this.ClientCodeGenerator = clientCodeGenerator;
            return this.GenerateDomainContextClass();
        }

        /// <summary>
        /// Generates the DomainContext code in a specific language.
        /// </summary>
        /// <returns>The generated code.</returns>
        protected abstract string GenerateDomainContextClass();

        internal virtual void Initialize()
        {            
            string domainServiceName = this.DomainServiceDescription.DomainServiceType.Name;
            this.ContractInterfaceName = "I" + domainServiceName + "Contract";

            this.Attributes = this.DomainServiceDescription.Attributes;
            this.InitDomainContextData();
        }

        internal IEnumerable<DomainOperationEntry> QueryMethods
        {
            get
            {
                if (this._queryMethods == null)
                {
                    this._queryMethods = new List<DomainOperationEntry>();
                }
                return this._queryMethods;
            }
        }

        internal IEnumerable<DomainOperationEntry> DomainOperations
        {
            get
            {
                if (this._domainOperations == null)
                {
                    this._domainOperations = new List<DomainOperationEntry>();
                }
                return this._domainOperations;
            }
        }

        internal IEnumerable<Type> EntitySets
        {
            get
            {
                if (this._entitySets == null)
                {
                    this._entitySets = new List<Type>();
                }
                return this._entitySets;
            }
        }

        private void InitDomainContextData()
        {
            HashSet<Type> visitedEntityTypes = new HashSet<Type>();
            this._queryMethods = new List<DomainOperationEntry>();
            this._domainOperations = new List<DomainOperationEntry>();
            this._entitySets = new List<Type>();
            foreach (DomainOperationEntry domainOperationEntry in this.DomainServiceDescription.DomainOperationEntries.Where(p => p.Operation == DomainOperation.Query).OrderBy(m => m.Name))
            {
                if (!this.CanGenerateDomainOperationEntry(domainOperationEntry))
                {
                    continue;
                }

                this._queryMethods.Add(domainOperationEntry);

                Type entityType = TypeUtility.GetElementType(domainOperationEntry.ReturnType);
                if (!visitedEntityTypes.Contains(entityType))
                {
                    visitedEntityTypes.Add(entityType);

                    bool isComposedType = this.DomainServiceDescription
                        .GetParentAssociations(entityType).Any(p => p.ComponentType != entityType);

                    Type rootEntityType = this.DomainServiceDescription.GetRootEntityType(entityType);
                    if (!isComposedType && rootEntityType == entityType)
                    {
                        this._entitySets.Add(entityType);
                    }
                }
            }

            foreach (Type entityType in this.DomainServiceDescription.EntityTypes.OrderBy(e => e.Name))
            {
                foreach (DomainOperationEntry entry in this.DomainServiceDescription.GetCustomMethods(entityType))
                {
                    if (this.CanGenerateDomainOperationEntry(entry))
                    {
                        this._domainOperations.Add(entry);
                    }
                }
            }
        }

        internal static string GetDomainContextTypeName(DomainServiceDescription domainServiceDescription)
        {
            string domainContextTypeName = domainServiceDescription.DomainServiceType.Name;
            if (domainContextTypeName.EndsWith("Service", StringComparison.Ordinal))
            {
                domainContextTypeName = domainContextTypeName.Substring(0, domainContextTypeName.Length - 7 /* "Service".Length */) + "Context";
            }
            return domainContextTypeName;
        }

        internal bool GetRequiresSecureEndpoint()
        {
            EnableClientAccessAttribute enableClientAccessAttribute = this.Attributes.OfType<EnableClientAccessAttribute>().Single();
            return enableClientAccessAttribute.RequiresSecureEndpoint;
        }

        internal bool RegisterEnumTypeIfNecessary(Type type, DomainOperationEntry domainOperationEntry)
        {
            Type enumType = TypeUtility.GetNonNullableType(type);
            if (enumType.IsEnum)
            {
                string errorMessage = null;
                if (!this.ClientCodeGenerator.CanExposeEnumType(enumType, out errorMessage))
                {
                    this.ClientCodeGenerator.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture,
                                                            Resource.ClientCodeGen_Domain_Op_Enum_Error,
                                                            domainOperationEntry.Name,
                                                            this.DomainContextTypeName,
                                                            enumType.FullName,
                                                            errorMessage));
                    return false;
                }
                else
                {
                    // Register use of this enum type, which could cause deferred generation
                    this.ClientCodeGenerator.AddEnumTypeToGenerate(enumType);
                }
            }
            return true;
        }

        internal List<Attribute> GetContractServiceKnownTypes(DomainOperationEntry operation, HashSet<Type> registeredTypes)
        {
            List<Attribute> knownTypeAttributes = new List<Attribute>();

            foreach (DomainOperationParameter parameter in operation.Parameters)
            {
                Type t = CodeGenUtilities.TranslateType(parameter.ParameterType);

                // All Nullable<T> types are unwrapped to the underlying non-nullable, because
                // that is they type we need to represent, not typeof(Nullable<T>)
                t = TypeUtility.GetNonNullableType(t);

                if (TypeUtility.IsPredefinedListType(t) || TypeUtility.IsComplexTypeCollection(t))
                {
                    Type elementType = TypeUtility.GetElementType(t);
                    if (elementType != null)
                    {
                        t = elementType.MakeArrayType();
                    }
                }

                // Check if the type is a simple type or already registered
                if (registeredTypes.Contains(t) || !this.TypeRequiresRegistration(t))
                {
                    continue;
                }

                // Record the type to prevent redundant [ServiceKnownType]'s.
                // This loop executes within a larger loop over multiple
                // DomainOperationEntries that may have already processed it.
                registeredTypes.Add(t);

                // If we determine we need to generate this enum type on the client,
                // then we need to register that intent and conjure a virtual type
                // here in our list of registered types to account for the fact it
                // could get a different root namespace on the client.
                if (t.IsEnum && this.ClientCodeGenerator.NeedToGenerateEnumType(t))
                {
                    // Request deferred generation of the enum
                    this.ClientCodeGenerator.AddEnumTypeToGenerate(t);

                    // Compose a virtual type that will reflect the correct namespace
                    // on the client when the [ServiceKnownType] is created.
                    t = new VirtualType(t.Name, CodeGenUtilities.TranslateNamespace(t), t.Assembly, t.BaseType);
                }

                knownTypeAttributes.Add(new ServiceKnownTypeAttribute(t));
            }

            return knownTypeAttributes;
        }

        private bool TypeRequiresRegistration(Type type)
        {
            if (type.IsPrimitive || type == typeof(string))
            {
                return false;
            }

            if (this.DomainServiceDescription.EntityTypes.Contains(type))
            {
                return false;
            }

            return true;
        }

        internal static bool OperationHasSideEffects(DomainOperationEntry operation)
        {
            if (operation.Operation == DomainOperation.Query)
            {
                return ((QueryAttribute)operation.OperationAttribute).HasSideEffects;
            }
            else if (operation.Operation == DomainOperation.Invoke)
            {
                return ((InvokeAttribute)operation.OperationAttribute).HasSideEffects;
            }
            return false;
        }        

        internal bool CanGenerateDomainOperationEntry(DomainOperationEntry domainOperationEntry)
        {
            string methodName = (domainOperationEntry.Operation == DomainOperation.Query) ? domainOperationEntry.Name : domainOperationEntry.Name + QuerySuffix;

            // Check each parameter type to see if is enum
            DomainOperationParameter[] paramInfos = domainOperationEntry.Parameters.ToArray();
            for (int i = 0; i < paramInfos.Length; i++)
            {
                DomainOperationParameter paramInfo = paramInfos[i];

                // If this is an enum type, we need to ensure it is either shared or
                // can be generated.  Failure logs an error.  The test for legality also causes
                // the enum to be generated if required.
                Type enumType = TypeUtility.GetNonNullableType(paramInfo.ParameterType);
                if (enumType.IsEnum)
                {
                    string errorMessage = null;
                    if (!this.ClientCodeGenerator.CanExposeEnumType(enumType, out errorMessage))
                    {
                        this.ClientCodeGenerator.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture,
                                                                Resource.ClientCodeGen_Domain_Op_Enum_Error,
                                                                domainOperationEntry.Name,
                                                                this.DomainContextTypeName,
                                                                enumType.FullName,
                                                                errorMessage));
                        return false;
                    }
                    else
                    {
                        // Register use of this enum type, which could cause deferred generation
                        this.ClientCodeGenerator.AddEnumTypeToGenerate(enumType);
                    }
                }
            }
            return true;
        }
    }
}
