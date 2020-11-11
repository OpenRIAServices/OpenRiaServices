using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Hosting.WCF;
using OpenRiaServices.Server;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Base class for a context that can be used to construct a business logic class.
    /// </summary>
    /// <remarks>
    /// We expect different data access layers to provide their own subclasses of this type.
    /// This base class can be used for a blank business logic class.
    /// </remarks>
    public class BusinessLogicContext
    {
        // Key into CodeTypeDeclaration.UserData to dictionary of generated helper methods
        private const string UserDataHelperMethods = "HelperMethods";

        private readonly string _name;
        private readonly Type _contextType;
        private List<BusinessLogicEntity> _entities;
        private ContextData _contextData;
        private static int uniqueContextID = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessLogicContext"/> class.
        /// </summary>
        /// <param name="contextType">The type of the DAL's context.  It may be null for the empty context case.</param>
        /// <param name="name">The user visible name.</param>
        public BusinessLogicContext(Type contextType, string name)
        {
            this._contextType = contextType;
            this._name = name;
        }

        /// <summary>
        /// Gets or sets the mutable state shared with <see cref="ContextViewModel"/>
        /// across AppDomain boundaries.
        /// </summary>
        public ContextData ContextData
        {
            get
            {
                if (this._contextData == null)
                {
                    this._contextData = new ContextData()
                    {
                        Name = this.NameAndDataAccessLayerName,
                        IsClientAccessEnabled = true,
                        ID = uniqueContextID++
                    };
                }
                return this._contextData;
            }
            set
            {
                this._contextData = value;
            }
        }

        /// <summary>
        /// Gets the user visible name of the context (typically the type name)
        /// </summary>
        public string Name
        {
            get
            {
                return this.ContextData.Name;
            }
        }

        /// <summary>
        /// Gets the name of the DAL technology of this context
        /// </summary>
        public virtual string DataAccessLayerName
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the name of the context and the DAL technology it uses
        /// </summary>
        public string NameAndDataAccessLayerName
        {
            get
            {
                string name = this._name;
                string dalName = this.DataAccessLayerName;
                return string.IsNullOrEmpty(dalName)
                        ? name
                        : String.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Name_And_Technology, name, dalName);
            }
        }

        /// <summary>
        /// Gets the type of the context (e.g. subclass of ObjectContext or DomainContext for EDM and LinqToSql, respectively).
        /// </summary>
        public Type ContextType
        {
            get
            {
                return this._contextType;
            }
        }

        /// <summary>
        /// Gets the value indicating whether [RequiresClientAccess] will be generated
        /// </summary>
        public bool IsClientAccessEnabled
        {
            get
            {
                return this.ContextData.IsClientAccessEnabled;
            }
        }

        /// <summary>
        /// Gets the value indicating whether an OData endpoint will be exposed
        /// </summary>
        public bool IsODataEndpointEnabled
        {
            get
            {
                return this.ContextData.IsODataEndpointEnabled;
            }
        }

        /// <summary>
        /// Gets the collection of entities exposed by this context, sorted by name
        /// </summary>
        public IEnumerable<BusinessLogicEntity> Entities
        {
            get
            {
                if (this._entities == null)
                {
                    try
                    {
                        this._entities = new List<BusinessLogicEntity>(this.CreateEntities());
                    }
                    catch (Exception ex)
                    {
                        // Note: ThreadAbortException cannot be ignored and will be rethrown after the catch.
                        // OOM is the only real exception that threatens both this AppDomain as well as VS,
                        // so we will allow it to propagate.
                        if (ex is OutOfMemoryException)
                        {
                            throw;
                        }

                        // Defense for catching and ignoring Exception: this code is running within a 2nd AppDomain
                        // used strictly for providing a view of the entities and for generating code.   We tear
                        // down this AppDomain when we are done with the wizard, so no memory corruption can
                        // occur back in the root AppDomain.  Yet an exception here attempting to open an
                        // arbitrary DAL model's metadata will be fatal to VS because it could be running
                        // from the data binding engine.  Consequently, we have decided it is acceptable to
                        // ignore fatal exceptions for this release and to return an empty entity list.
                        this._entities = new List<BusinessLogicEntity>();
                    }
                    this._entities.Sort(new Comparison<BusinessLogicEntity>((x, y) => String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)));
                }
                return this._entities;
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="EntityData"/> data objects for all the
        /// entities that belong to the current context.
        /// </summary>
        /// <remarks>
        /// This property does lazy evaluation so that the view models can populate
        /// the UI quickly but pay for the cost of scanning the context only when
        /// the UI requires it.
        /// </remarks>
        public IEnumerable<EntityData> EntityDataItems
        {
            get
            {
                return this.Entities.Select<BusinessLogicEntity, EntityData>(ble => ble.EntityData);
            }
        }

        /// <summary>
        /// Creates all the domain operation entries for the given entity.
        /// </summary>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="businessLogicClass">The class into which to generate the method</param>
        /// <param name="entity">The entity which will be affected by this method</param>
        public void GenerateEntityDomainOperationEntries(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            CodeMemberMethod selectMethod = this.GenerateSelectMethod(codeGenContext, businessLogicClass, entity);
            if (selectMethod != null)
            {
                // If OData endpoint is requested, generate [Query(IsDefault=true)].
                if (this.IsODataEndpointEnabled)
                {
                    CodeAttributeDeclaration attributeDeclaration = new CodeAttributeDeclaration(
                                                                        new CodeTypeReference("Query"),
                                                                        new CodeAttributeArgument[] { new CodeAttributeArgument("IsDefault", new CodePrimitiveExpression(true)) });
                    selectMethod.CustomAttributes.Add(attributeDeclaration);
                }
            }

            if (entity.IsEditable)
            {
                this.GenerateInsertMethod(codeGenContext, businessLogicClass, entity);
                this.GenerateUpdateMethod(codeGenContext, businessLogicClass, entity);
                this.GenerateDeleteMethod(codeGenContext, businessLogicClass, entity);
            }
        }

        /// <summary>
        /// Virtual method to extract the entities exposed by this context.
        /// </summary>
        /// <returns>An unsorted list of entities</returns>
        protected virtual IEnumerable<BusinessLogicEntity> CreateEntities()
        {
            return Array.Empty<BusinessLogicEntity>();
        }

        /// <summary>
        /// Creates the "select" domain operation entry for the entity
        /// </summary>
        /// <remarks>This base class unconditionally returns <c>null</c> and should not be called by derived classes.
        /// This allows the base class to be used for the blank <see cref="DomainService"/>.</remarks>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="businessLogicClass">The class into which to generate the method</param>
        /// <param name="entity">The entity which will be affected by this method</param>
        /// <returns>The newly created method</returns>
        protected virtual CodeMemberMethod GenerateSelectMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            return null;
        }

        /// <summary>
        /// Creates the "update" method for the entity
        /// </summary>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="businessLogicClass">The class into which to generate the method</param>
        /// <param name="entity">The entity which will be affected by this method</param>
        protected virtual void GenerateUpdateMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
        }

        /// <summary>
        /// Creates the "insert" method for the entity
        /// </summary>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="businessLogicClass">The class into which to generate the method</param>
        /// <param name="entity">The entity which will be affected by this method</param>
        protected virtual void GenerateInsertMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
        }

        /// <summary>
        /// Creates the "delete" method for the entity
        /// </summary>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="businessLogicClass">The class into which to generate the method</param>
        /// <param name="entity">The entity which will be affected by this method</param>
        protected virtual void GenerateDeleteMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
        }

        /// <summary>
        /// Creates the entire business logic class in the specified namespace
        /// </summary>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="codeNamespace">The namespace object into which the type should be defined.</param>
        /// <param name="className">The name of the class.  It cannot be null.</param>
        /// <returns>A new CodeTypeDeclaration for the generated class.</returns>
        protected virtual CodeTypeDeclaration CreateBusinessLogicClass(CodeGenContext codeGenContext, CodeNamespace codeNamespace, string className)
        {
            CodeTypeDeclaration businessLogicClass = CodeGenUtilities.CreateTypeDeclaration(className, codeNamespace.Name);
            businessLogicClass.BaseTypes.Add(BusinessLogicClassConstants.DomainServiceTypeName);
            return businessLogicClass;
        }

        /// <summary>
        /// Generates the code for the domain service class.
        /// </summary>
        /// <param name="language">The language to use.</param>
        /// <param name="className">The name of the class.</param>
        /// <param name="namespaceName">The namespace to use for the class.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        public GeneratedCode GenerateBusinessLogicClass(string language, string className, string namespaceName, string rootNamespace)
        {
            using (CodeGenContext codeGenContext = new CodeGenContext(language, rootNamespace))
            {
                this.GenerateBusinessLogicClass(codeGenContext, className, namespaceName);
                return codeGenContext.GenerateCode();
            }
        }

        /// <summary>
        /// Generates the code for the domain service class.
        /// </summary>
        /// <param name="language">The language to use.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <param name="optionalSuffix">If nonblank, the suffix to append to namespace and class names for testing</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        public GeneratedCode GenerateMetadataClasses(string language, string rootNamespace, string optionalSuffix)
        {
            using (CodeGenContext codeGenContext = new CodeGenContext(language, rootNamespace))
            {
                bool generatedAnyCode = this.GenerateMetadataClasses(codeGenContext, optionalSuffix);
                if (generatedAnyCode)
                {
                    GeneratedCode generatedCode = codeGenContext.GenerateCode();
                    return generatedCode;
                }
            }

            // Did not generate any code -- return empty tuple
            return new GeneratedCode();
        }

        /// <summary>
        /// Creates the entire business logic class within the specified namespace name
        /// </summary>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="className">The name of the class to generate.  It cannot be null or empty.</param>
        /// <param name="namespaceName">The namespace to use for the generated code.  It cannot be empty.</param>
        protected void GenerateBusinessLogicClass(CodeGenContext codeGenContext, string className, string namespaceName)
        {
            if (codeGenContext == null)
            {
                throw new ArgumentNullException("codeGenContext");
            }
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className");
            }
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentNullException("namespaceName");
            }

            // namespace XXX { }
            CodeNamespace ns = codeGenContext.GetOrGenNamespace(namespaceName);

            // public class $classname$ { }
            CodeTypeDeclaration businessLogicClass = this.CreateBusinessLogicClass(codeGenContext, ns, className);
            ns.Types.Add(businessLogicClass);

            // Class-level Xml comments
            // Empty class gets its own comment because it has no ContextType to describe
            string remarksComment;
            if (this.ContextType == null)
            {
                remarksComment = Resources.BusinessLogicClass_Class_Remarks_Empty;
            }
            else
            {
                remarksComment = String.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Class_Remarks, this.ContextType.Name);
            }

            // Add developer comment explaining what this class does
            businessLogicClass.Comments.Add(new CodeCommentStatement(remarksComment, false));

            // Add [RequiresAuthentication] as a comment
            if (this.ContextType != null)
            {
                remarksComment = codeGenContext.IsCSharp
                                    ? Resources.BusinessLogicClass_RequiresAuthentication_CSharp
                                    : Resources.BusinessLogicClass_RequiresAuthentication_VB;
                businessLogicClass.Comments.Add(new CodeCommentStatement(remarksComment, false));
            }

            if (this.IsClientAccessEnabled)
            {
                // [EnableClientAccess]
                CodeAttributeDeclaration attr = CodeGenUtilities.CreateAttributeDeclaration(BusinessLogicClassConstants.EnableClientAccessAttributeTypeName);
                businessLogicClass.CustomAttributes.Add(attr);
            }
            else
            {
                // if not enabled, add a comment explaining how to enable it
                businessLogicClass.Comments.Add(new CodeCommentStatement(Resources.BusinessLogicClass_EnableClientAccess_Comment));
            }

            // Gen all domain operation entries
            // Sort by name for baseline predictability
            foreach (BusinessLogicEntity entity in this.Entities.OrderBy(e => e.Name))
            {
                if (entity.IsIncluded)
                {
                    // Add an import for this entity's namespace if needed
                    // This is necessary only when entities exist in a different namespace from the context
                    CodeGenUtilities.AddImportIfNeeded(ns, entity.ClrType.Namespace);

                    this.GenerateEntityDomainOperationEntries(codeGenContext, businessLogicClass, entity);
                }
            }

            // If any private helper methods were generated, append them now.
            // We sort by their keys to give baseline predictability.
            Dictionary<string, CodeTypeMember> helpers = BusinessLogicContext.GetHelperMemberDictionary(businessLogicClass);
            foreach (string key in helpers.Keys.OrderBy(s => s))
            {
                businessLogicClass.Members.Add(helpers[key]);
            }

            // If we exposed an OData endpoint, add a reference to the OData assembly
            // so it appears in the server project, allowing the user to chose 
            // CopyLocal=true for bin deploy scenarios
            if (this.IsODataEndpointEnabled)
            {
                codeGenContext.AddReference(typeof(ODataEndpointFactory).Assembly.FullName);
            }
        }

        /// <summary>
        /// Generates the metadata classes for all entities for the current context.
        /// </summary>
        /// <param name="codeGenContext">The context to use to generate code.</param>
        /// <param name="optionalSuffix">If nonblank, the suffix to append to namespace and class names for testing</param>
        /// <returns><c>true</c> means at least some code was generated.</returns>
        protected bool GenerateMetadataClasses(CodeGenContext codeGenContext, string optionalSuffix)
        {
            bool generatedCode = false;
            if (this.NeedToGenerateMetadataClasses)
            {
                // Sort by entity name for baseline predictability
                foreach (BusinessLogicEntity entity in this.Entities.OrderBy(e => e.Name))
                {
                    if (entity.IsIncluded)
                    {
                        generatedCode |= this.GenerateMetadataClass(codeGenContext, optionalSuffix, entity.ClrType);

                        generatedCode |= this.GenerateAdditionalMetadataClasses(codeGenContext, optionalSuffix, entity);
                    }
                }
            }
            return generatedCode;
        }

        public virtual bool NeedToGenerateMetadataClasses
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Generates additional metadata classes for the given entity if necessary.
        /// </summary>
        /// <param name="codeGenContext">The context to use to generate code.</param>
        /// <param name="optionalSuffix">If not null, optional suffix to class name and namespace</param>
        /// <param name="entity">The entity for which to generate the additional metadata.</param>
        /// <returns><c>true</c> means at least some code was generated.</returns>
        /// <remarks>
        /// This default implementation of the virtual method does not generate any additional classes. It needs to be overridden in the derived
        /// classes to generate additional metadata classes if necessary.
        /// </remarks>
        protected virtual bool GenerateAdditionalMetadataClasses(CodeGenContext codeGenContext, string optionalSuffix, BusinessLogicEntity entity)
        {
            return false;
        }

        /// <summary>
        /// Generates the metadata class for the given object (entity or complex object)
        /// </summary>
        /// <param name="codeGenContext">The context to use to generate code.</param>
        /// <param name="optionalSuffix">If not null, optional suffix to class name and namespace</param>
        /// <param name="type">The type of the object for which to generate the metadata class.</param>
        /// <returns><c>true</c> means at least some code was generated.</returns>
        public bool GenerateMetadataClass(CodeGenContext codeGenContext, string optionalSuffix, Type type)
        {
            // If already have a buddy class, bypass all this logic
            // Use soft dependency (string name) to avoid static dependency on DataAnnotations.
            // We do this because this wizard must run from the GAC, and DataAnnotations will not necessarily be in the GAC too.
            Type buddyClassType = TypeUtilities.GetAssociatedMetadataType(type);
            if (buddyClassType != null)
            {
                return false;
            }

            string className = type.Name;
            string classNamespace = type.Namespace;

            bool addSuffix = !string.IsNullOrEmpty(optionalSuffix);
            if (addSuffix)
            {
                className += optionalSuffix;
                classNamespace += optionalSuffix;
            }

            // Every object could have a unique namespace (odd, but true)
            // So we logically create a new namespace for each object.  Those
            // sharing a namespace will reuse the CodeNamespace.
            // We allow the caller to specify in case it needs to override that
            // namespace.  Unit testing is such a scenario
            CodeNamespace codeNamespace = codeGenContext.GetOrGenNamespace(classNamespace);

            // If we redirected to a different namespace than the object, import the real object's namespace
            if (addSuffix)
            {
                CodeGenUtilities.AddImportIfNeeded(codeNamespace, type.Namespace);
            }

            // Name of buddy class is $objectClassName$Metadata (e.g. Orders --> OrdersMetadata)
            string buddyClassName = className + "Metadata";

            // We use the full outer.inner type naming convention for VB because they cannot resolve it otherwise.
            // C# can currently resolve it due to a bug in the compiler, but it is safer to use the legal syntax here.
            string fullBuddyClassName = className + "." + buddyClassName;

            CodeTypeDeclaration objectClass = null;

            // public class $objectType$ { }
            objectClass = CodeGenUtilities.CreateTypeDeclaration(className, classNamespace);
            objectClass.IsPartial = true;
            objectClass.TypeAttributes = TypeAttributes.Public;

            // Add explanatory comments about what the [MetadataTypeAttribute] does
            objectClass.Comments.Add(new CodeCommentStatement(String.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Entity_Partial_Class_Remarks, buddyClassName, className), false));

            // [MetadataType(typeof($objectType$.$objectType$_Metadata))]
            CodeAttributeDeclaration attr = CodeGenUtilities.CreateAttributeDeclaration(BusinessLogicClassConstants.MetadataTypeAttributeTypeName);

            CodeAttributeArgument attrArg = new CodeAttributeArgument(new CodeTypeOfExpression(fullBuddyClassName));
            attr.Arguments.Add(attrArg);
            objectClass.CustomAttributes.Add(attr);

            // public sealed class $objectType$_Metadata { }
            // (note: cannot set 'static' modified from CodeDom.)
            CodeTypeDeclaration buddyClass = CodeGenUtilities.CreateTypeDeclaration(buddyClassName, classNamespace);

            // Both VB and C# use a friend/public buddy class.  A private buddy class does not
            // compile in VB, and it compiles in C# only due to a bug.
            buddyClass.TypeAttributes = TypeAttributes.Sealed | TypeAttributes.NestedAssembly;
            bool generatedProperty = false;

            // Generate a developer comment describing what this class does
            buddyClass.Comments.Add(new CodeCommentStatement(String.Format(CultureInfo.CurrentCulture, Resources.Buddy_Class_Remarks, type.Name)));

            // Add a language-specific example
            string explanation = codeGenContext.IsCSharp ? Resources.Buddy_Class_Remarks_CSharp : Resources.Buddy_Class_Remarks_VB;
            buddyClass.Comments.Add(new CodeCommentStatement(explanation, false));

            // Generate a private ctor to make it impossible to instantiate this class
            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Private;
            ctor.Comments.Add(new CodeCommentStatement(Resources.BusinessLogicClass_Private_Ctor_Comment));
            buddyClass.Members.Add(ctor);

            // Sort by name order for baseline predictability
            foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).OrderBy(p => p.Name))
            {
                // CodeDom does not support auto-implemented properties, so we will generate fields and then transform them into properties
                Type propType = propertyInfo.PropertyType;
                if (propType.IsVisible && propertyInfo.GetGetMethod() != null && this.CanGeneratePropertyOfType(propType))
                {
                    // Add an import for this property type's namespace if needed
                    CodeGenUtilities.AddImportIfNeeded(codeNamespace, propertyInfo.PropertyType.Namespace);

                    CodeSnippetTypeMember property = CodeGenUtilities.CreateAutomaticPropertyDeclaration(codeGenContext, buddyClass, propertyInfo, !string.IsNullOrEmpty(codeNamespace.Name) /* insideNamespace */);

                    buddyClass.Members.Add(property);
                    generatedProperty = true;
                }
            }

            // Don't generate anything if the buddy class is empty
            if (generatedProperty)
            {
                // Add the partial object class to the namespace
                codeNamespace.Types.Add(objectClass);

                // Add the metadata class as a nested class inside the partial object class
                objectClass.Members.Add(buddyClass);
            }

            // false if no properties were generated, indicating no code should be emitted
            return generatedProperty;
        }

        /// <summary>
        /// Determines whether a property of the given type should be generated
        /// in the associated metadata class.
        /// </summary>
        /// <remarks>
        /// This logic is meant to strip out DAL-level properties that will not appear
        /// on the client.
        /// </remarks>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if it is legal to generate this property type in the business logic class.</returns>
        protected virtual bool CanGeneratePropertyOfType(Type type)
        {
            // Get the implied element type (e.g. int32[], Nullable<int32>, IEnumerable<int32>)
            // If the ultimate element type is not allowed, it's not acceptable, no matter whether
            // this is an array, Nullable<T> or whatever
            type = TypeUtilities.GetElementType(type);

            // Predefined simple types are always accepted
            if (TypeUtilities.IsPredefinedType(type))
            {
                return true;
            }

            // Allow any entity references to appear
            foreach (BusinessLogicEntity entity in this.Entities)
            {
                if (entity.ClrType == type)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves a dictionary containing all the helper members generated during code generation.
        /// These are keyed by the member name and contain a <see cref="CodeTypeMember"/> that must
        /// appear in the generated code.
        /// </summary>
        /// <param name="businessLogicClass">The class containing generated code.</param>
        /// <returns>The dictionary.  An empty one will be created if it does not yet exist.</returns>
        protected static Dictionary<string, CodeTypeMember> GetHelperMemberDictionary(CodeTypeDeclaration businessLogicClass)
        {
            System.Diagnostics.Debug.Assert(businessLogicClass != null, "BusinessLogicClass cannot be null");
            System.Diagnostics.Debug.Assert(businessLogicClass.UserData != null, "BusinessLogicClass.UserData cannot be null");

            Dictionary<string, CodeTypeMember> memberDictionary = null;
            if (businessLogicClass.UserData.Contains(BusinessLogicContext.UserDataHelperMethods))
            {
                memberDictionary = businessLogicClass.UserData[BusinessLogicContext.UserDataHelperMethods] as Dictionary<string, CodeTypeMember>;
            }
            else
            {
                memberDictionary = new Dictionary<string, CodeTypeMember>();
                businessLogicClass.UserData[BusinessLogicContext.UserDataHelperMethods] = memberDictionary;
            }
            return memberDictionary;
        }

        /// <summary>
        /// Tests whether the specified helper member has been generated, and if not, invokes
        /// a callback to generate it.
        /// </summary>
        /// <param name="codeGenContext">The context in which we are generating code.</param>
        /// <param name="businessLogicClass">The class containing the generated code.</param>
        /// <param name="helperMemberName">The name of the helper member.</param>
        /// <param name="generatorCallback">Callback that will create this helper if it does not yet exist.</param>
        public static void GenerateHelperMemberIfNecessary(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, string helperMemberName, Func<CodeTypeMember> generatorCallback)
        {
            System.Diagnostics.Debug.Assert(codeGenContext != null, "CodeGenContext cannot be null"); 
            System.Diagnostics.Debug.Assert(businessLogicClass != null, "BusinessLogicClass cannot be null");
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(helperMemberName), "Helper member name cannot be empty.");
            System.Diagnostics.Debug.Assert(generatorCallback != null, "callback cannot be null");

            Dictionary<string, CodeTypeMember> memberDictionary = BusinessLogicContext.GetHelperMemberDictionary(businessLogicClass);
            if (!memberDictionary.ContainsKey(helperMemberName))
            {
                CodeTypeMember member = generatorCallback();
                if (member != null)
                {
                    memberDictionary[helperMemberName] = member;
                }
            }
        }

#if DEBUG
        /// <summary>
        /// Overrides <see cref="Object.ToString()"/> to  facilitate debugging
        /// </summary>
        /// <returns>The name of the context.</returns>
        public override string ToString()
        {
            return this.Name;
        }
#endif
    }
}
