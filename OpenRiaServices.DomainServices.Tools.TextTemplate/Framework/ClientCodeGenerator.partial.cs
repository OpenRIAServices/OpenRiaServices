namespace OpenRiaServices.DomainServices.Tools.TextTemplate
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using OpenRiaServices.DomainServices;
    using OpenRiaServices.DomainServices.Server;
    using System.Text;
    using OpenRiaServices.DomainServices.Tools;
    using OpenRiaServices.DomainServices.Tools.TextTemplate.CSharpGenerators;

    /// <summary>
    /// Generator class to generate a domain service proxy using Text Templates.
    /// </summary>
    public abstract partial class ClientCodeGenerator : IDomainServiceClientCodeGenerator
    {
        private IEnumerable<DomainServiceDescription> _domainServiceDescriptions;
        private ICodeGenerationHost _codeGenerationHost;
        private ClientCodeGenerationOptions _options;
        private HashSet<Type> _enumTypesToGenerate;

        /// <summary>
        /// Gets the generator object that generates entity classes on the client.
        /// </summary>
        protected abstract EntityGenerator EntityGenerator { get; }

        /// <summary>
        /// Gets the generator object that generates complex types on the client.
        /// </summary>
        protected abstract ComplexObjectGenerator ComplexObjectGenerator { get; }

        /// <summary>
        /// Gets the generator object that generates DomainContexts on the client.
        /// </summary>
        protected abstract DomainContextGenerator DomainContextGenerator { get; }

        /// <summary>
        /// Gets the generator object that generates the application wide WebContext class on the client.
        /// </summary>
        protected abstract WebContextGenerator WebContextGenerator { get; }

        /// <summary>
        /// Gets the generator object that generates enums.
        /// </summary>
        protected abstract EnumGenerator EnumGenerator { get; }

        /// <summary>
        /// Gets the code generation options. 
        /// </summary>
        public ClientCodeGenerationOptions Options
        {
            get
            {
                return this._options;
            }
        }

        /// <summary>
        /// Gets the code generation host for this code generation instance.
        /// </summary>
        public ICodeGenerationHost CodeGenerationHost
        {
            get
            {
                return this._codeGenerationHost;
            }
        }

        /// <summary>
        /// Gets the list of all DomainServiceDescriptions.
        /// </summary>
        public IEnumerable<DomainServiceDescription> DomainServiceDescriptions
        {
            get
            {
                return this._domainServiceDescriptions;
            }
        }

        /// <summary>
        /// This method is part of the <see cref="IDomainServiceClientCodeGenerator" /> interface. The RIA Services Code Generation process uses this method as the entry point into the code generator.
        /// </summary>
        /// <param name="codeGenerationHost">The code generation host for this instance.</param>
        /// <param name="domainServiceDescriptions">The list of all the DomainServiceDescription objects.</param>
        /// <param name="options">The code generation objects.</param>
        /// <returns>The generated code.</returns>
        public string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            this._codeGenerationHost = codeGenerationHost;
            this._domainServiceDescriptions = domainServiceDescriptions;
            this._options = options;

            this._enumTypesToGenerate = new HashSet<Type>();

            if (this.EntityGenerator == null)
            {
                this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, TextTemplateResource.EntityGeneratorNotFound));
            }
            if (this.ComplexObjectGenerator == null)
            {
                this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, TextTemplateResource.ComplexObjectGeneratorNotFound));
            }
            if (this.DomainContextGenerator == null)
            {
                this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, TextTemplateResource.DomainContextGeneratorNotFound));
            }
            if (this.WebContextGenerator == null)
            {
                this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, TextTemplateResource.WebContextGeneratorNotFound));
            }
            if (this.EnumGenerator == null)
            {
                this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, TextTemplateResource.EnumGeneratorNotFound));
            }

            if (!this.CodeGenerationHost.HasLoggedErrors)
            {
                return this.GenerateCode();
            }
            return null;
        }

        /// <summary>
        /// When overridden in the derived class generates code in specific to a language (C#, VB etc).
        /// </summary>
        /// <returns>The generated code.</returns>
        protected abstract string GenerateCode();

        internal void AddEnumTypeToGenerate(Type enumType)
        {
            if (this._enumTypesToGenerate != null)
            {
                if (!this._enumTypesToGenerate.Contains(enumType) && this.NeedToGenerateEnumType(enumType))
                {
                    this._enumTypesToGenerate.Add(enumType);
                }
            }
        }

        internal void GenerateProxyClass()
        {
            List<Type> generatedEntities = new List<Type>();
            List<Type> generatedComplexObjects = new List<Type>();
            foreach (DomainServiceDescription dsd in this._domainServiceDescriptions.OrderBy(d => d.DomainServiceType.Name))
            {
                CodeMemberShareKind domainContextShareKind = this.GetDomainContextTypeShareKind(dsd);
                if ((domainContextShareKind & CodeMemberShareKind.Shared) != 0)
                {
                    continue;
                }

                this.Write(this.DomainContextGenerator.Generate(dsd, this));

                this.GenerateEntitiesIfNotGenerated(dsd, generatedEntities);

                this.GenerateComplexObjectsIfNotGenerated(dsd, generatedComplexObjects); 
            }

            if (this.Options.IsApplicationContextGenerationEnabled)
            {
                this.Write(this.WebContextGenerator.Generate(this._domainServiceDescriptions, this));
            }

            this.Write(this.EnumGenerator.GenerateEnums(this._enumTypesToGenerate, this));
        }

        private void GenerateEntitiesIfNotGenerated(DomainServiceDescription dsd, List<Type> generatedEntities)
        {
            foreach (Type t in dsd.EntityTypes.OrderBy(e => e.Name))
            {
                if (generatedEntities.Contains(t))
                {
                    continue;
                }
                CodeMemberShareKind typeShareKind = this.GetTypeShareKind(t);
                if ((typeShareKind & CodeMemberShareKind.SharedByReference) != 0)
                {
                    this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, "Type already shared", t));
                    continue;
                }
                generatedEntities.Add(t);
                this.Write(this.EntityGenerator.Generate(t, this._domainServiceDescriptions, this));
            }
        }

        private void GenerateComplexObjectsIfNotGenerated(DomainServiceDescription dsd, List<Type> generatedComplexObjects)
        {
            foreach (Type t in dsd.ComplexTypes.OrderBy(e => e.Name))
            {
                if (generatedComplexObjects.Contains(t))
                {
                    continue;
                }
                CodeMemberShareKind typeShareKind = this.GetTypeShareKind(t);
                if ((typeShareKind & CodeMemberShareKind.SharedByReference) != 0)
                {
                    this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, "Type already shared", t));
                    continue;
                }
                generatedComplexObjects.Add(t);
                this.Write(this.ComplexObjectGenerator.Generate(t, dsd, this));
            }
        }

        private void PreprocessProxyTypes()
        {
            foreach (DomainServiceDescription dsd in this._domainServiceDescriptions)
            {
                // Check if the DomainService is nested
                if (dsd.DomainServiceType.IsNested)
                {
                    this.CodeGenerationHost.LogError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_DomainService_CannotBeNested,
                            dsd.DomainServiceType));
                }

                // Register the DomainService type name
                this.RegisterTypeName(dsd.DomainServiceType, dsd.DomainServiceType.Namespace);

                // Register all associated Entity type names
                foreach (Type entityType in dsd.EntityTypes)
                {
                    this.RegisterTypeName(entityType, dsd.DomainServiceType.Namespace);
                }
            }
        }

        private void RegisterTypeName(Type type, string containingNamespace)
        {
            if (string.IsNullOrEmpty(type.Namespace))
            {
                this.CodeGenerationHost.LogError(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Namespace_Required, type));
                return;
            }

            // Check if we're in conflict
            // Here we register all the names of the types to be generated. We can check if we have conflicting names using this list.            
            if (!CodeGenUtilities.RegisterTypeName(type, containingNamespace))
            {
                // Aggressively check for potential conflicts across other DomainService entity types.
                IEnumerable<Type> potentialConflicts =
                    // Entity types with namespace matches
                    this._domainServiceDescriptions
                        .SelectMany<DomainServiceDescription, Type>(d => d.EntityTypes)
                            .Where(entity => entity.Namespace == type.Namespace)
                                .Concat(
                    // DomainService types with namespace matches
                                    this._domainServiceDescriptions
                                        .Select(d => d.DomainServiceType)
                                            .Where(dst => dst.Namespace == type.Namespace)).Distinct();

                foreach (Type potentialConflict in potentialConflicts)
                {
                    // Register potential conflicts so we qualify type names correctly
                    // later during codegen.
                    CodeGenUtilities.RegisterTypeName(potentialConflict, containingNamespace);
                }
            }
        }

        internal string ClientProjectName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this._options.ClientProjectPath);
            }
        }

        internal CodeMemberShareKind GetDomainContextTypeShareKind(DomainServiceDescription domainServiceDescription)
        {
            Type domainServiceType = domainServiceDescription.DomainServiceType;
            string domainContextTypeName = DomainContextGenerator.GetDomainContextTypeName(domainServiceDescription);
            string fullTypeName = domainServiceType.Namespace + "." + domainContextTypeName;
            CodeMemberShareKind shareKind = this._codeGenerationHost.GetTypeShareKind(fullTypeName);
            return shareKind;
        }

        internal CodeMemberShareKind GetPropertyShareKind(Type type, string propertyName)
        {
            return this._codeGenerationHost.GetPropertyShareKind(type.AssemblyQualifiedName, propertyName);
        }

        internal CodeMemberShareKind GetMethodShareKind(MethodBase methodBase)
        {
            IEnumerable<string> parameterTypeNames = methodBase.GetParameters().Select<ParameterInfo, string>(p => p.ParameterType.AssemblyQualifiedName);
            return this._codeGenerationHost.GetMethodShareKind(methodBase.DeclaringType.AssemblyQualifiedName, methodBase.Name, parameterTypeNames);
        }

        internal bool NeedToGenerateEnumType(Type enumType)
        {
           if (!this._enumTypesToGenerate.Contains(enumType))
            {
                if ((this.GetTypeShareKind(enumType) & CodeMemberShareKind.Shared) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        internal bool CanExposeEnumType(Type enumType, out string errorMessage)
        {
            errorMessage = null;

            if (!enumType.IsPublic || enumType.IsNested)
            {
                errorMessage = Resource.Enum_Type_Must_Be_Public;
                return false;
            }

            // Determine whether it is visible to the client.  If so,
            // it is legal to expose without generating it.
            if ((this.GetTypeShareKind(enumType) & CodeMemberShareKind.Shared) != 0)
            {
                return true;
            }

            // The enum is not shared (i.e. not visible to the client).
            // We block attempts to generate anything from system assemblies
            if (enumType.Assembly.IsSystemAssembly())
            {
                errorMessage = Resource.Enum_Type_Cannot_Gen_System;
                return false;
            }

            return true;
        }

        internal CodeMemberShareKind GetTypeShareKind(Type type)
        {
            return this._codeGenerationHost.GetTypeShareKind(type.AssemblyQualifiedName);
        }        
    }
}
