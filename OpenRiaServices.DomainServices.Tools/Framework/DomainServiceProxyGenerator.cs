using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using OpenRiaServices;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.ServiceModel.Web;
using OpenRiaServices.DomainServices.Tools.SharedTypes;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Proxy generator for a DomainService
    /// </summary>
    internal class DomainServiceProxyGenerator : ProxyGenerator
    {
        private const string DefaultActionSchema = "http://tempuri.org/{0}/{1}";
        private const string DefaultReplyActionSchema = "http://tempuri.org/{0}/{1}Response";
        private const string DefaultFaultActionSchema = "http://tempuri.org/{0}/{1}{2}";

        private DomainServiceDescription _domainServiceDescription;
        private IDictionary<Type, CodeTypeDeclaration> _typeMapping;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceProxyGenerator"/> class.
        /// </summary>
        /// <param name="proxyGenerator">The client proxy generator against which this will generate code.  Cannot be null.</param>
        /// <param name="domainServiceDescription">The domain service description to use as source metadata</param>
        /// <param name="typeMapping">A dictionary of <see cref="DomainService"/> and related entity types that maps to their corresponding client-side <see cref="CodeTypeReference"/> representations.</param>
        public DomainServiceProxyGenerator(CodeDomClientCodeGenerator proxyGenerator, DomainServiceDescription domainServiceDescription, IDictionary<Type, CodeTypeDeclaration> typeMapping)
            : base(proxyGenerator)
        {
            this._domainServiceDescription = domainServiceDescription;
            this._typeMapping = typeMapping;
        }

        /// <summary>
        /// Generates the client proxy code for a domain service.
        /// </summary>
        public override void Generate()
        {
            // ----------------------------------------------------------------
            // Namespace
            // ----------------------------------------------------------------
            Type domainServiceType = this._domainServiceDescription.DomainServiceType;
            CodeNamespace ns = this.ClientProxyGenerator.GetOrGenNamespace(domainServiceType);
            AttributeCollection attributes = this._domainServiceDescription.Attributes;

            // Missing namespace bails out of code-gen -- error has been logged
            if (ns == null)
            {
                return;
            }

            // ----------------------------------------------------------------
            // public partial sealed class {Name} : DomainContext
            // ----------------------------------------------------------------
            string clientTypeName = DomainContextTypeName(this._domainServiceDescription);

            CodeTypeDeclaration proxyClass = CodeGenUtilities.CreateTypeDeclaration(clientTypeName, domainServiceType.Namespace);
            proxyClass.IsPartial = true;
            proxyClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
            ns.Types.Add(proxyClass);

            CodeTypeReference domainContextTypeName = CodeGenUtilities.GetTypeReference(TypeConstants.DomainContextTypeFullName, ns.Name, false);
            proxyClass.BaseTypes.Add(domainContextTypeName);

            // Add <summary> xml comment to class
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_Class_Summary_Comment, domainServiceType.Name);
            proxyClass.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // ----------------------------------------------------------------
            // [DomainIdentifier], etc attributes move through metadata pipeline
            // ----------------------------------------------------------------
            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                proxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeType, ex.Message, proxyClass.Name, ex.InnerException.Message),
                attributes.Cast<Attribute>(),
                proxyClass.CustomAttributes,
                proxyClass.Comments);

            // ----------------------------------------------------------------
            // Add default OnCreated partial method
            // ----------------------------------------------------------------
            NotificationMethodGenerator notificationMethodGen = new NotificationMethodGenerator(this.ClientProxyGenerator);
            proxyClass.Members.AddRange(notificationMethodGen.PartialMethodsSnippetBlock);

            // ----------------------------------------------------------------
            // Generate a contract interface for the service.
            // ----------------------------------------------------------------
            CodeTypeDeclaration contractInterface = this.GenerateContract(proxyClass);

            // ----------------------------------------------------------------
            // Generate constructors
            // ----------------------------------------------------------------
            EnableClientAccessAttribute enableClientAccessAttribute = attributes.OfType<EnableClientAccessAttribute>().Single();
            this.GenerateConstructors(proxyClass, contractInterface, enableClientAccessAttribute, notificationMethodGen.OnCreatedMethodInvokeExpression);

            // ----------------------------------------------------------------
            // Separate proxies for each domain operation entry
            // ----------------------------------------------------------------
            DomainOperationEntryProxyGenerator methodProxyGenerator = new DomainOperationEntryProxyGenerator(this.ClientProxyGenerator, proxyClass, this._domainServiceDescription);
            methodProxyGenerator.Generate();

            // ----------------------------------------------------------------
            // Invoke operations
            // ----------------------------------------------------------------
            InvokeOperationProxyGenerator invokeOperationProxyGenerator = new InvokeOperationProxyGenerator(this.ClientProxyGenerator, proxyClass, this._domainServiceDescription);
            invokeOperationProxyGenerator.Generate();

            // ----------------------------------------------------------------
            // EntityContainer instantiation
            // ----------------------------------------------------------------

            // The entity container holds a collection of EntityLists, one per visible entity root type.
            // The derived entity types are stored in their respective root's list and do not get their own.
            this.GenEntityContainer(proxyClass, this._domainServiceDescription.RootEntityTypes, this._domainServiceDescription);

            // Register created CodeTypeDeclaration with mapping
            this._typeMapping[domainServiceType] = proxyClass;
        }

        /// <summary>
        /// Returns the type name we will generate for the DomainContext for the given
        /// <paramref name="domainServiceDescription"/>
        /// </summary>
        /// <param name="domainServiceDescription">The domain service description from which we will generate the DomainContext.</param>
        /// <returns>The simple type name we will use for the DomainContext.  It will not include the namespace.</returns>
        internal static string DomainContextTypeName(DomainServiceDescription domainServiceDescription)
        {
            string clientTypeName = domainServiceDescription.DomainServiceType.Name;
            if (clientTypeName.EndsWith("Service", StringComparison.Ordinal))
            {
                clientTypeName = clientTypeName.Substring(0, clientTypeName.Length - 7 /* "Service".Length */) + "Context";
            }
            return clientTypeName;
        }

        /// <summary>
        /// Generates and adds a constructor to an existing <see cref="CodeTypeDeclaration"/>.
        /// </summary>
        /// <param name="proxyClass">The <see cref="CodeTypeDeclaration"/> to generate a constructor for.</param>
        /// <param name="parameters">A collection of <see cref="CodeParameterDeclarationExpression"/> values to pass to the constructor.</param>
        /// <param name="baseParameters">A collection of <see cref="CodeArgumentReferenceExpression"/> values to pass to the constructor base invocation.</param>
        /// <param name="comments">A collection of <see cref="CodeCommentStatement"/> values to add to the constructor.</param>
        /// <param name="callBaseCtr">Determines whether this ctr is to call the base ctr or one of the overloaded ctrs.</param>
        /// <returns>Returns the generated constructor.</returns>
        private static CodeConstructor GenerateConstructor(
            CodeTypeDeclaration proxyClass,
            IEnumerable<CodeParameterDeclarationExpression> parameters,
            IEnumerable<CodeExpression> baseParameters,
            CodeCommentStatementCollection comments,
            bool callBaseCtr)
        {
            // this generates ctrs of the form depending on the parameter:
            // ctor(...parameters...) : this(...baseParameters...)
            // ctor(...parameters...) : base(...baseParameters...) // when param is a DomainClient

            CodeConstructor proxyCtor = new CodeConstructor();
            proxyClass.Members.Add(proxyCtor);
            proxyCtor.Attributes = MemberAttributes.Public;

            // ctor parameters
            if (parameters != null)
            {
                foreach (CodeParameterDeclarationExpression parameter in parameters)
                {
                    proxyCtor.Parameters.Add(parameter);
                }
            }

            // ctor base parameters
            if (baseParameters != null)
            {
                foreach (CodeExpression parameter in baseParameters)
                {
                    if (callBaseCtr)
                    {
                        proxyCtor.BaseConstructorArgs.Add(parameter);
                    }
                    else
                    {
                        proxyCtor.ChainedConstructorArgs.Add(parameter);
                    }
                }
            }

            // ctor comments
            if (comments != null)
            {
                proxyCtor.Comments.AddRange(comments);
            }

            return proxyCtor;
        }

        /// <summary>
        /// Generates the "EntityContainer" for all the given entities
        /// </summary>
        /// <remarks>
        /// The EntityContainer is the logical store of the entity instances, and we have to generate
        /// code to instantiate entries in this store
        /// </remarks>
        /// <param name="proxyClass">Code into which to generate code</param>
        /// <param name="entityTypes">Set of all known entity types for which we need storage</param>
        /// <param name="domainServiceDescription">The DomainServiceDescription we're code genning for</param>
        private void GenEntityContainer(CodeTypeDeclaration proxyClass, IEnumerable<Type> entityTypes, DomainServiceDescription domainServiceDescription)
        {
            // ----------------------------------------------------------------
            // inner class
            // ----------------------------------------------------------------
            var innerClass = this.GenEntityContainerInnerClass(proxyClass, entityTypes, domainServiceDescription);

            // ----------------------------------------------------------------
            // method decl: protected override EntityContainer CreateEntityContainer()
            // ----------------------------------------------------------------
            var method = new CodeMemberMethod();
            method.Name = "CreateEntityContainer";
            method.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            method.ReturnType = CodeGenUtilities.GetTypeReference(TypeConstants.EntityContainerTypeFullName, proxyClass.UserData["Namespace"] as string, false);

            // Add <summary> and <returns> doc comments
            method.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(Resource.CodeGen_CreateEntityContainer_Method_Summary_Comment, this.ClientProxyGenerator.IsCSharp));
            method.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(Resource.CodeGen_CreateEntityContainer_Method_Returns_Comment, this.ClientProxyGenerator.IsCSharp));

            // ----------------------------------------------------------------
            // method body: return new 'innerClass'
            // ----------------------------------------------------------------
            var innerClassReference = CodeGenUtilities.GetTypeReference(proxyClass.UserData["Namespace"] as string + "." + proxyClass.Name + "." + innerClass.Name, proxyClass.UserData["Namespace"] as string, true);
            var newInnerClassExpr = new CodeObjectCreateExpression(innerClassReference, new CodeExpression[0]);
            var returnStatement = new CodeMethodReturnStatement(newInnerClassExpr);

            method.Statements.Add(returnStatement);
            proxyClass.Members.Add(method);
        }

        /// <summary>
        /// Generate the EntityContainer inner class
        /// </summary>
        /// <param name="proxyClass">Class into which to generate code</param>
        /// <param name="entityTypes">All known entity types that will be stored in this EntityContainer</param>
        /// <param name="domainServiceDescription">The DomainServiceDescription we're code genning for</param>
        /// <returns>the generated EntityContainer inner class</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Temporary exclusion only")]
        //// TODO: we need to refactor this to comply with FxCop
        private CodeTypeDeclaration GenEntityContainerInnerClass(CodeTypeDeclaration proxyClass, IEnumerable<Type> entityTypes, DomainServiceDescription domainServiceDescription)
        {
            // ----------------------------------------------------------------
            // class xxxEntityContainer : EntityContainer
            // ----------------------------------------------------------------
            string containingNamespace = this.ClientProxyGenerator.GetNamespace(proxyClass).Name;
            var innerClass = CodeGenUtilities.CreateTypeDeclaration(proxyClass.Name + "EntityContainer", containingNamespace);
            innerClass.BaseTypes.Add(CodeGenUtilities.GetTypeReference(TypeConstants.EntityContainerTypeFullName, containingNamespace, false));
            innerClass.TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Sealed;
            proxyClass.Members.Add(innerClass);

            // ----------------------------------------------------------------
            // ctor
            // ----------------------------------------------------------------
            var ctor = new CodeConstructor();
            ctor.Attributes = MemberAttributes.Public;
            innerClass.Members.Add(ctor);

            // Convert to a set for faster lookups.
            HashSet<Type> entityTypesToUse = new HashSet<Type>();
            foreach (Type entityType in entityTypes)
            {
                entityTypesToUse.Add(entityType);
            }

            // ----------------------------------------------------------------
            // each entity type gets 'CreateEntitySet<entityType>()' statement in ctor
            // ----------------------------------------------------------------
            foreach (Type entityType in entityTypes.OrderBy(t => t.FullName))
            {
                // Skip entity types which have base classes.
                if (entityTypesToUse.Any(t => t != entityType && t.IsAssignableFrom(entityType)))
                {
                    continue;
                }

                // ----------------------------------------------------------------
                // Build EntitySetOperations enum value
                // ----------------------------------------------------------------
                var enumTypeReference = CodeGenUtilities.GetTypeReference(TypeConstants.EntitySetOperationsTypeFullName, containingNamespace, false);
                CodeExpression entitySetOperations = null;

                // Check to see what update operations are supported, and build up the EntitySetOperations flags expression
                bool canInsert = domainServiceDescription.IsOperationSupported(entityType, DomainOperation.Insert);
                bool canEdit = domainServiceDescription.IsOperationSupported(entityType, DomainOperation.Update);
                bool canDelete = domainServiceDescription.IsOperationSupported(entityType, DomainOperation.Delete);

                CodeTypeReferenceExpression enumTypeReferenceExp = new CodeTypeReferenceExpression(enumTypeReference);

                if (!canInsert && !canEdit && !canDelete)
                {
                    // if no update operations are supported, set to 'None'
                    entitySetOperations = new CodeFieldReferenceExpression(enumTypeReferenceExp, "None");
                }
                else if (canInsert && canEdit && canDelete)
                {
                    // if all operations are supported, set to 'All'
                    entitySetOperations = new CodeFieldReferenceExpression(enumTypeReferenceExp, "All");
                }
                else
                {
                    if (canInsert)
                    {
                        entitySetOperations = new CodeFieldReferenceExpression(enumTypeReferenceExp, "Add");
                    }
                    if (canEdit)
                    {
                        CodeFieldReferenceExpression setOp = new CodeFieldReferenceExpression(enumTypeReferenceExp, "Edit");
                        if (entitySetOperations == null)
                        {
                            entitySetOperations = setOp;
                        }
                        else
                        {
                            entitySetOperations = new CodeBinaryOperatorExpression(entitySetOperations, CodeBinaryOperatorType.BitwiseOr, setOp);
                        }
                    }
                    if (canDelete)
                    {
                        CodeFieldReferenceExpression setOp = new CodeFieldReferenceExpression(enumTypeReferenceExp, "Remove");
                        if (entitySetOperations == null)
                        {
                            entitySetOperations = setOp;
                        }
                        else
                        {
                            entitySetOperations = new CodeBinaryOperatorExpression(entitySetOperations, CodeBinaryOperatorType.BitwiseOr, setOp);
                        }
                    }
                }


                // ----------------------------------------------------------------
                // method call: this.CreateEntitySet<entityType>
                // ----------------------------------------------------------------
                var entityTypeReference = CodeGenUtilities.GetTypeReference(entityType, this.ClientProxyGenerator, proxyClass);
                var methodRef = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "CreateEntitySet", entityTypeReference);
                var methodCall = new CodeMethodInvokeExpression(methodRef, entitySetOperations);
                ctor.Statements.Add(methodCall);
            }

            return innerClass;
        }

        private void GenerateConstructors(CodeTypeDeclaration proxyClass, CodeTypeDeclaration contractInterface, EnableClientAccessAttribute enableClientAccessAttribute, CodeMethodInvokeExpression onCreatedExpression)
        {
            CodeTypeReference uriTypeRef = CodeGenUtilities.GetTypeReference(typeof(Uri), this.ClientProxyGenerator, proxyClass);
            CodeTypeReference uriKindTypeRef = CodeGenUtilities.GetTypeReference(typeof(UriKind), this.ClientProxyGenerator, proxyClass);

            string containingNamespace = proxyClass.UserData["Namespace"] as string;
            CodeTypeReference contractTypeParameter =
                CodeGenUtilities.GetTypeReference(
                    containingNamespace + "." + proxyClass.Name + "." + contractInterface.Name,
                    containingNamespace,
                    true);

            // construct relative URI
            string relativeServiceUri = string.Format(CultureInfo.InvariantCulture, "{0}.svc", this._domainServiceDescription.DomainServiceType.FullName.Replace('.', '-'));
            CodeExpression relativeUriExpression = new CodeObjectCreateExpression(
                uriTypeRef,
                new CodePrimitiveExpression(relativeServiceUri),
                new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(uriKindTypeRef), "Relative"));

            // ----------------------------------------------------------------
            // Default ctor decl (using relative URI)
            // ----------------------------------------------------------------

            // ctor parameters
            List<CodeParameterDeclarationExpression> ctorParams = null;

            // base params
            List<CodeExpression> baseParams = new List<CodeExpression>(1);
            baseParams.Add(relativeUriExpression);

            // add <summary> doc comments
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Default_Constructor_Summary_Comments, proxyClass.Name);
            CodeCommentStatementCollection comments = CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp);

            // <comments>...</comments>
            // public .ctor() : this(new Uri("Foo-Bar.svc", UriKind.Relative))
             GenerateConstructor(proxyClass, ctorParams, baseParams, comments, false);

            // ----------------------------------------------------------------
            // DomainContext(System.Uri serviceUri) ctor decl
            // ----------------------------------------------------------------

            // ctor params
            ctorParams = new List<CodeParameterDeclarationExpression>(1);
            ctorParams.Add(new CodeParameterDeclarationExpression(uriTypeRef, "serviceUri"));

            // add <summary> and <param> comments
            comments = CodeGenUtilities.GenerateSummaryCodeComment(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_ConstructorComments_Summary_ServiceUri, proxyClass.Name), this.ClientProxyGenerator.IsCSharp);
            comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("serviceUri", string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_ConstructorComments_Param_ServiceUri, this._domainServiceDescription.DomainServiceType.Name), this.ClientProxyGenerator.IsCSharp));

            // <comments>...</comments>
            // public .ctor(Uri serviceUri) : this(DomainContext.CreateDomainClient(typeof(TContract), serviceUri, true/false))

            // ctor base parameters
            baseParams = new List<CodeExpression>(1);
            CodeTypeReference domainContextRef = CodeGenUtilities.GetTypeReference(TypeConstants.DomainContextTypeFullName, proxyClass.UserData["Namespace"] as string, false);
            baseParams.Add( new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(domainContextRef), "CreateDomainClient"),
                                new CodeTypeOfExpression(contractTypeParameter),
                                new CodeArgumentReferenceExpression("serviceUri"),
                                new CodePrimitiveExpression(enableClientAccessAttribute.RequiresSecureEndpoint)));

            GenerateConstructor(proxyClass, ctorParams, baseParams, comments, false);

            // -----------------------------------------------------------------------
            // DomainContext(DomainClient domainClient) ctor decl
            // -----------------------------------------------------------------------

            // ctor parameters --[(DomainClient domainClient)]
            ctorParams = new List<CodeParameterDeclarationExpression>(1);
            ctorParams.Add(new CodeParameterDeclarationExpression(CodeGenUtilities.GetTypeReference(TypeConstants.DomainClientTypeFullName, proxyClass.UserData["Namespace"] as string, false), "domainClient"));

            // parameters to invoke on base -- [: base(domainClient)]
            baseParams = new List<CodeExpression>(1);
            baseParams.Add(new CodeArgumentReferenceExpression("domainClient"));

            // add <summary> and <param> comments
            comments = CodeGenUtilities.GenerateSummaryCodeComment(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_ConstructorComments_Summary_DomainClientAccumulating, proxyClass.Name), this.ClientProxyGenerator.IsCSharp);
            comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("domainClient", string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_ConstructorComments_Param_DomainClient), this.ClientProxyGenerator.IsCSharp));

            // <comments>...</comments>
            // public .ctor(DomainClient domainClient) : base(domainClient)]
            CodeConstructor proxyCtor = GenerateConstructor(proxyClass, ctorParams, baseParams, comments, true);
            proxyCtor.Statements.Add(onCreatedExpression);
        }

        private CodeTypeDeclaration GenerateContract(CodeTypeDeclaration proxyClass)
        {
            string domainServiceName = this._domainServiceDescription.DomainServiceType.Name;
            string contractTypeName = "I" + domainServiceName + "Contract";

            CodeTypeDeclaration contractInterface = CodeGenUtilities.CreateTypeDeclaration(contractTypeName, proxyClass.UserData["Namespace"] as string);
            proxyClass.Members.Add(contractInterface);

            // Add <summary> xml comment to interface
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_Summary_Comment, domainServiceName);
            contractInterface.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            contractInterface.IsInterface = true;
            contractInterface.CustomAttributes.Add(
                CodeGenUtilities.CreateAttributeDeclaration(
                    typeof(ServiceContractAttribute),
                    this.ClientProxyGenerator,
                    proxyClass));

            // Used to track types registered with ServiceKnownTypeAttribute (for Custom methods)
            HashSet<Type> registeredServiceTypes = new HashSet<Type>();

            // Generate query methods, invoke operations and custom methods.
            foreach (DomainOperationEntry operation in this._domainServiceDescription.DomainOperationEntries
                .Where(op => op.Operation == DomainOperation.Query || op.Operation == DomainOperation.Invoke || op.Operation == DomainOperation.Custom)
                .OrderBy(op => op.Name))
            {
                if (operation.Operation == DomainOperation.Custom)
                {
                    this.GenerateContractServiceKnownTypes(contractInterface, operation, registeredServiceTypes);
                }
                else
                {
                    this.GenerateContractMethod(contractInterface, operation);
                }
            }

            // Generate submit method if we have CUD operations.
            if (this._domainServiceDescription.DomainOperationEntries
                .Where(op => (op.Operation == DomainOperation.Delete || op.Operation == DomainOperation.Insert 
                            || op.Operation == DomainOperation.Update || op.Operation == DomainOperation.Custom)).Any())
            {
                this.GenerateContractSubmitChangesMethod(contractInterface);
            }

            return contractInterface;
        }

        private void GenerateContractServiceKnownTypes(CodeTypeDeclaration contractInterface, DomainOperationEntry operation, HashSet<Type> registeredTypes)
        {
            List<Attribute> knownTypeAttributes  = new List<Attribute>();

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
                if (t.IsEnum && this.ClientProxyGenerator.NeedToGenerateEnumType(t))
                {
                    // Request deferred generation of the enum
                    this.ClientProxyGenerator.RegisterUseOfEnumType(t);

                    // Compose a virtual type that will reflect the correct namespace
                    // on the client when the [ServiceKnownType] is created.
                    t = new VirtualType(t.Name, CodeGenUtilities.TranslateNamespace(t, this.ClientProxyGenerator), t.Assembly, t.BaseType);
                }

                knownTypeAttributes.Add(new ServiceKnownTypeAttribute(t));
            }

            if (knownTypeAttributes.Count > 0)
            {
                CustomAttributeGenerator.GenerateCustomAttributes(
                    this.ClientProxyGenerator,
                    contractInterface,
                    knownTypeAttributes,
                    contractInterface.CustomAttributes,
                    contractInterface.Comments,
                    true /* force propagation, we want a compile error if these types aren't present on client */);
            }
        }

        private void GenerateContractMethod(CodeTypeDeclaration contractInterface, DomainOperationEntry operation)
        {
            string domainServiceName = this._domainServiceDescription.DomainServiceType.Name;

            CodeMemberMethod beginMethod = new CodeMemberMethod();
            beginMethod.Name = "Begin" + operation.Name;
            beginMethod.ReturnType = CodeGenUtilities.GetTypeReference(typeof(IAsyncResult), this.ClientProxyGenerator, contractInterface);
            contractInterface.Members.Add(beginMethod);

            // Generate <summary> doc comment for the Begin method
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_Summary_Comment, operation.Name);
            beginMethod.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <param> doc comment for all the parameters
            foreach (DomainOperationParameter parameter in operation.Parameters)
            {
                comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_Parameter_Comment, parameter.Name);
                beginMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment(parameter.Name, comment, this.ClientProxyGenerator.IsCSharp));
            }

            // <param> for callback and asyncState
            beginMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("callback", Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_Callback_Parameter_Comment, this.ClientProxyGenerator.IsCSharp));
            beginMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("asyncState", Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_AsyncState_Parameter_Comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <returns> doc comment
            beginMethod.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_Returns_Comment, this.ClientProxyGenerator.IsCSharp));

            this.GenerateContractMethodAttributes(contractInterface, beginMethod, domainServiceName, operation.Name);

            foreach (DomainOperationParameter parameter in operation.Parameters)
            {
                beginMethod.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        CodeGenUtilities.GetTypeReference(CodeGenUtilities.TranslateType(parameter.ParameterType), this.ClientProxyGenerator, contractInterface),
                        parameter.Name));
            }

            beginMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(typeof(AsyncCallback), this.ClientProxyGenerator, contractInterface),
                    "callback"));

            beginMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(typeof(object), this.ClientProxyGenerator, contractInterface),
                    "asyncState"));

            CodeMemberMethod endMethod = new CodeMemberMethod();
            endMethod.Name = "End" + operation.Name;

            bool hasSideEffects = true;
            string returnTypeName = null;
            if (operation.Operation == DomainOperation.Query)
            {
                hasSideEffects = ((QueryAttribute)operation.OperationAttribute).HasSideEffects;
                returnTypeName = "QueryResult";
                if (operation.ReturnType == typeof(void))
                {
                    endMethod.ReturnType = CodeGenUtilities.GetTypeReference(TypeConstants.QueryResultFullName, contractInterface.UserData["Namespace"] as string, false);
                }
                else
                {
                    endMethod.ReturnType = CodeGenUtilities.GetTypeReference(TypeConstants.QueryResultFullName, contractInterface.UserData["Namespace"] as string, false);
                    endMethod.ReturnType.TypeArguments.Add(CodeGenUtilities.GetTypeReference(CodeGenUtilities.TranslateType(operation.AssociatedType), this.ClientProxyGenerator, contractInterface));
                }
            }
            else
            {
                if (operation.Operation == DomainOperation.Invoke)
                {
                    hasSideEffects = ((InvokeAttribute)operation.OperationAttribute).HasSideEffects;
                }
                returnTypeName = CodeGenUtilities.TranslateType(operation.ReturnType).Name;
                endMethod.ReturnType = CodeGenUtilities.GetTypeReference(CodeGenUtilities.TranslateType(operation.ReturnType), this.ClientProxyGenerator, contractInterface, false);
            }

            // Generate [HasSideEffects(...)]. 
            beginMethod.CustomAttributes.Add(new CodeAttributeDeclaration("HasSideEffects",
                                                                            new CodeAttributeArgument(new CodePrimitiveExpression(hasSideEffects))));

            // Generate <summary> doc comment for the End method
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_End_Method_Summary_Comment, beginMethod.Name);
            endMethod.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <param> doc comment for the IAsyncResult parameter
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_End_Method_Parameter_Comment, beginMethod.Name);
            endMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("result", comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <returns> doc comment
            if (operation.ReturnType != typeof(void))
            {
                comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_End_Method_Returns_Comment, returnTypeName, operation.Name);
                endMethod.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(comment, this.ClientProxyGenerator.IsCSharp));
            }

            contractInterface.Members.Add(endMethod);

            endMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(typeof(IAsyncResult), this.ClientProxyGenerator, contractInterface),
                    "result"));
        }

        private void GenerateContractSubmitChangesMethod(CodeTypeDeclaration contractInterface)
        {
            string domainServiceName = this._domainServiceDescription.DomainServiceType.Name;

            // -----------------------------------------------------------------------
            // IAsyncResult BeginSubmitChanges(IEnumerable<EntityOperation> changeSet, AsyncCallback callback, object asyncState)
            // -----------------------------------------------------------------------
            CodeMemberMethod beginQueryMethod = new CodeMemberMethod();
            beginQueryMethod.Name = "BeginSubmitChanges";
            beginQueryMethod.ReturnType = CodeGenUtilities.GetTypeReference(typeof(IAsyncResult), this.ClientProxyGenerator, contractInterface);
            contractInterface.Members.Add(beginQueryMethod);

            // Generate <summary> doc comment for the Begin method
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_Summary_Comment, "SubmitChanges");
            beginQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // <param> for callback and asyncState
            beginQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("changeSet", Resource.CodeGen_DomainContext_ServiceContract_Begin_SubmitMethod_Changeset_Parameter_Comment, this.ClientProxyGenerator.IsCSharp));
            beginQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("callback", Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_Callback_Parameter_Comment, this.ClientProxyGenerator.IsCSharp));
            beginQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("asyncState", Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_AsyncState_Parameter_Comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <returns> doc comment
            beginQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(Resource.CodeGen_DomainContext_ServiceContract_Begin_Method_Returns_Comment, this.ClientProxyGenerator.IsCSharp));

            this.GenerateContractMethodAttributes(contractInterface, beginQueryMethod, domainServiceName, "SubmitChanges");

            CodeTypeReference enumTypeRef = CodeGenUtilities.GetTypeReference(TypeConstants.IEnumerableFullName, contractInterface.UserData["Namespace"] as string, false);
            enumTypeRef.TypeArguments.Add(CodeGenUtilities.GetTypeReference(TypeConstants.ChangeSetEntryTypeFullName, contractInterface.UserData["Namespace"] as string, false));

            beginQueryMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    enumTypeRef,
                    "changeSet"));

            beginQueryMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(typeof(AsyncCallback), this.ClientProxyGenerator, contractInterface),
                    "callback"));

            beginQueryMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(typeof(object), this.ClientProxyGenerator, contractInterface),
                    "asyncState"));

            // -----------------------------------------------------------------------
            // IEnumerable<EntityOperation> EndSubmitChanges(IAsyncResult result)
            // -----------------------------------------------------------------------
            CodeTypeReference resultTypeRef = CodeGenUtilities.GetTypeReference(TypeConstants.DomainServiceFaultFullName, contractInterface.UserData["Namespace"] as string, false);
            resultTypeRef.TypeArguments.Add(CodeGenUtilities.GetTypeReference(TypeConstants.ChangeSetEntryTypeFullName, contractInterface.UserData["Namespace"] as string, false));

            CodeMemberMethod endQueryMethod = new CodeMemberMethod();
            endQueryMethod.Name = "EndSubmitChanges";
            endQueryMethod.ReturnType = enumTypeRef;
            contractInterface.Members.Add(endQueryMethod);

            // Generate <summary> doc comment for the End method
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_End_Method_Summary_Comment, "BeginSubmitChanges");
            endQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <param> doc comment for the IAsyncResult parameter
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_ServiceContract_End_Method_Parameter_Comment, "BeginSubmitChanges");
            endQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("result", comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <returns> doc comment
            endQueryMethod.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(Resource.CodeGen_DomainContext_ServiceContract_End_SubmitMethod_Returns_Comment, this.ClientProxyGenerator.IsCSharp));

            endQueryMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(typeof(IAsyncResult), this.ClientProxyGenerator, contractInterface),
                    "result"));
        }

        private void GenerateContractMethodAttributes(CodeTypeDeclaration contractInterface, CodeMemberMethod beginQueryMethod, string domainServiceName, string operationName)
        {
            CodeAttributeDeclaration operationContractAtt = CodeGenUtilities.CreateAttributeDeclaration(typeof(OperationContractAttribute), this.ClientProxyGenerator, contractInterface);
            operationContractAtt.Arguments.Add(new CodeAttributeArgument("AsyncPattern", new CodePrimitiveExpression(true)));
            operationContractAtt.Arguments.Add(new CodeAttributeArgument("Action", new CodePrimitiveExpression(string.Format(CultureInfo.InvariantCulture, DomainServiceProxyGenerator.DefaultActionSchema, domainServiceName, operationName))));
            operationContractAtt.Arguments.Add(new CodeAttributeArgument("ReplyAction", new CodePrimitiveExpression(string.Format(CultureInfo.InvariantCulture, DomainServiceProxyGenerator.DefaultReplyActionSchema, domainServiceName, operationName))));
            beginQueryMethod.CustomAttributes.Add(operationContractAtt);
        }

        /// <summary>
        /// Determines if a given <see cref="Type"/> should be registered with a <see cref="ServiceKnownTypeAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if the <see cref="Type"/> should be registered, <c>false</c> otherwise.</returns>
        private bool TypeRequiresRegistration(Type type)
        {
            if (type.IsPrimitive || type == typeof(string))
            {
                return false;
            }

            if (this._domainServiceDescription.EntityTypes.Contains(type))
            {
                return false;
            }

            return true;
        }
    }
}
