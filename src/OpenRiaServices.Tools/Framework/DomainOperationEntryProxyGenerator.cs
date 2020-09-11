using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRiaServices;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Proxy generator for a DomainOperationEntry
    /// </summary>
    internal class DomainOperationEntryProxyGenerator : ProxyGenerator
    {
        private const string QuerySuffix = "Query";

        private readonly CodeTypeDeclaration _proxyClass;
        private readonly DomainServiceDescription _domainServiceDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainOperationEntryProxyGenerator"/> class.
        /// </summary>
        /// <param name="clientProxyGenerator">The client proxy generator against which this will generate code.  Cannot be null.</param>
        /// <param name="proxyClass">The class into which to inject the generated code</param>
        /// <param name="domainServiceDescription">The description for the DomainService we're generating for</param>
        public DomainOperationEntryProxyGenerator(CodeDomClientCodeGenerator clientProxyGenerator, CodeTypeDeclaration proxyClass, DomainServiceDescription domainServiceDescription)
            : base(clientProxyGenerator)
        {
            this._proxyClass = proxyClass;
            this._domainServiceDescription = domainServiceDescription;
        }

        /// <summary>
        /// Generates the client proxy code for the domain operation entries
        /// </summary>
        public override void Generate()
        {
            HashSet<Type> visitedEntityTypes = new HashSet<Type>();
            foreach (DomainOperationEntry domainOperationEntry in this._domainServiceDescription.DomainOperationEntries.Where(p => p.Operation == DomainOperation.Query).OrderBy(m => m.Name))
            {
                // Verify legality of query method -- log error if problem
                if (!this.CanGenerateDomainOperationEntry(domainOperationEntry))
                {
                    // Continue so we accumulate multiple errors
                    continue;
                }

                // generate the query factory method corresponding to the server query operation
                this.GenerateEntityQueryMethod(domainOperationEntry);

                Type entityType = TypeUtility.GetElementType(domainOperationEntry.ReturnType);
                if (!visitedEntityTypes.Contains(entityType))
                {
                    visitedEntityTypes.Add(entityType);

                    // We don't generate entity sets for composed Types. However the special
                    // case can arise where a Type is its own parent (and no other Types 
                    // are its parent), in which case we need to generate the set.
                    bool isComposedType = this._domainServiceDescription
                        .GetParentAssociations(entityType).Any(p => p.ComponentType != entityType);

                    // We generate EntitySets only for the root entities
                    Type rootEntityType = this._domainServiceDescription.GetRootEntityType(entityType);
                    if (!isComposedType && rootEntityType == entityType)
                    {
                        this.GenerateEntitySet(entityType);
                    }
                }
            }

            // Generate domain methods.
            foreach (Type entityType in this._domainServiceDescription.EntityTypes.OrderBy(e => e.Name))
            {
                foreach (DomainOperationEntry entry in this._domainServiceDescription.GetCustomMethods(entityType))
                {
                    // Validate domain method is legal -- log error if not
                    if (this.CanGenerateDomainOperationEntry(entry))
                    {
                        this.GenerateDomainOperationEntry(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="DomainOperationEntry"/> can legally
        /// be generated.  Logs warnings or errors as appropriate.
        /// </summary>
        /// <param name="domainOperationEntry">The operation to generate.</param>
        /// <returns><c>false</c> means an error or warning has been logged and it should not be generated.</returns>
        private bool CanGenerateDomainOperationEntry(DomainOperationEntry domainOperationEntry)
        {
            string methodName = (domainOperationEntry.Operation == DomainOperation.Query) ? domainOperationEntry.Name : domainOperationEntry.Name + QuerySuffix;

            // Check for name conflicts.  Log error and exit if collision.
            if (this._proxyClass.Members.Cast<CodeTypeMember>().Any(c => c.Name == methodName))
            {
                this.ClientProxyGenerator.LogError(
                    string.Format(CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_NamingCollision_MemberAlreadyExists,
                        this._proxyClass.Name,
                         methodName));

                return false;
            }

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
                    if (!this.ClientProxyGenerator.CanExposeEnumType(enumType, out errorMessage))
                    {
                        this.ClientProxyGenerator.LogError(string.Format(CultureInfo.CurrentCulture,
                                                                Resource.ClientCodeGen_Domain_Op_Enum_Error,
                                                                domainOperationEntry.Name,
                                                                this._proxyClass.Name,
                                                                enumType.FullName,
                                                                errorMessage));
                        return false;
                    }
                    else
                    {
                        // Register use of this enum type, which could cause deferred generation
                        this.ClientProxyGenerator.RegisterUseOfEnumType(enumType);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Generates a domain method on the domain service.
        /// </summary>
        /// <param name="domainMethod">The domain method to generate code for.</param>
        private void GenerateDomainOperationEntry(DomainOperationEntry domainMethod)
        {
            // ----------------------------------------------------------------
            // Method decl
            // ----------------------------------------------------------------
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = domainMethod.Name;
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            // ----------------------------------------------------------------
            // generate domain method body:
            //    entity.<methodName>(params);
            // ----------------------------------------------------------------
            List<CodeExpression> invokeParams = new List<CodeExpression>();

            // The domain method parameter list is the same as the domain operation entries.
            DomainOperationParameter[] paramInfos = domainMethod.Parameters.ToArray();

            // Generate the <summary> and <param> doc comments
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainClient_Custom_Method_Summary_Comment, domainMethod.Name, domainMethod.AssociatedType.Name);
            method.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <param> doc comment for all the parameters
            // The first param is the entity
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_Custom_Method_Entity_Parameter_Comment, domainMethod.AssociatedType.Name);
            method.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment(paramInfos[0].Name, comment, this.ClientProxyGenerator.IsCSharp));

            // All subsequent params
            for (int i = 1; i < paramInfos.Length; ++i)
            {
                comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_Custom_Method_Parameter_Comment, paramInfos[i].Name);
                method.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment(paramInfos[i].Name, comment, this.ClientProxyGenerator.IsCSharp));
            }

            // Create an expression for each parameter
            for (int i = 0; i < paramInfos.Length; i++)
            {
                DomainOperationParameter paramInfo = paramInfos[i];

                // build up the method parameter signature from the DomainOperationEntry.MethodInfo
                CodeParameterDeclarationExpression paramDecl = new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(
                        CodeGenUtilities.TranslateType(paramInfo.ParameterType),
                        this.ClientProxyGenerator,
                        this._proxyClass),
                    paramInfo.Name);

                method.Parameters.Add(paramDecl);

                // Skip the entity parameter.
                if (i > 0)
                {
                    // build up the invoke call parameters
                    invokeParams.Add(new CodeVariableReferenceExpression(paramInfo.Name));
                }
            }

            method.Statements.Add(
                new CodeExpressionStatement(
                    new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression(paramInfos[0].Name), domainMethod.Name, invokeParams.ToArray())));

            this._proxyClass.Members.Add(method);
        }

        /// <summary>
        /// Generates the property getter for the given entity type from the given domain operation entry
        /// </summary>
        /// <param name="entityType">The type of the entity being exposed</param>
        private void GenerateEntitySet(Type entityType)
        {
            string propertyName = Naming.MakePluralName(entityType.Name);

            // Check for name conflicts
            if (this._proxyClass.Members.Cast<CodeTypeMember>().Any(c => c.Name == propertyName))
            {
                this.ClientProxyGenerator.LogError(
                    string.Format(CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_NamingCollision_MemberAlreadyExists,
                        this._proxyClass.Name,
                        propertyName));

                return;
            }

            // Generate the generic parameter list for the entity type (e.g. <Customer>
            var entityTypeRef = CodeGenUtilities.GetTypeReference(entityType, this.ClientProxyGenerator, this._proxyClass);
            var genericParameters = new CodeTypeReference[] { entityTypeRef };

            // ----------------------------------------------------------------
            // EntitySet<entityType> generic type reference for return type of property
            // ----------------------------------------------------------------
            var returnType = CodeGenUtilities.GetTypeReference(TypeConstants.EntitySetTypeFullName, entityType.Namespace, false);
            returnType.TypeArguments.AddRange(genericParameters);
            

            // ----------------------------------------------------------------
            // this.EntityContainer property reference (Entities will be defined later)
            // ----------------------------------------------------------------
            var entityContainerProperty = new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), "EntityContainer");

            // ----------------------------------------------------------------
            // this.EntityContainer.GetEntitySet<entityType>()
            // ----------------------------------------------------------------
            var methodRef = new CodeMethodReferenceExpression(entityContainerProperty, "GetEntitySet", genericParameters);
            var methodCall = new CodeMethodInvokeExpression(methodRef, Array.Empty<CodeExpression>());

            // ----------------------------------------------------------------
            // return this.EntityContainer.GetEntitySet<entityType>()
            // ----------------------------------------------------------------
            var returnStmt = new CodeMethodReturnStatement(methodCall);

            // ----------------------------------------------------------------
            // Property getter
            // ----------------------------------------------------------------
            var property = new CodeMemberProperty();
            property.Name = propertyName;
            property.Type = returnType;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final; // Final needed, else becomes virtual
            property.GetStatements.Add(returnStmt);

            this._proxyClass.Members.Add(property);

            // Add the <summary> doc comment
            property.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_EntitySet_Property_Summary_Comment, entityType.Name, this._proxyClass.Name), this.ClientProxyGenerator.IsCSharp));
        }

        /// <summary>
        /// Generates the query method for the specified query operation
        /// </summary>
        /// <param name="domainOperationEntry">DomainOperationEntry for which we are generating the query methods</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Should be rewritten at a later time.")]
        private void GenerateEntityQueryMethod(DomainOperationEntry domainOperationEntry)
        {
            string queryMethodName = domainOperationEntry.Name + QuerySuffix;

            Type entityType = TypeUtility.GetElementType(domainOperationEntry.ReturnType);

            CodeMemberMethod queryMethod = new CodeMemberMethod();
            queryMethod.Name = queryMethodName;
            queryMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final; // Final needed, else becomes virtual

            queryMethod.ReturnType = CodeGenUtilities.GetTypeReference(TypeConstants.EntityQueryTypeFullName, this._domainServiceDescription.DomainServiceType.Namespace, false);
            queryMethod.ReturnType.TypeArguments.Add(CodeGenUtilities.GetTypeReference(entityType.FullName, this._domainServiceDescription.DomainServiceType.Namespace, true));

            DomainOperationParameter[] domainOperationEntryParameters = domainOperationEntry.Parameters.ToArray();

            // Generate <summary> doc comment
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_ConstructorComments_Summary_DomainContext, entityType.Name, domainOperationEntry.Name);
            queryMethod.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <param> doc comments
            foreach (DomainOperationParameter paramInfo in domainOperationEntryParameters)
            {
                comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Query_Method_Parameter_Comment, paramInfo.Name);
                queryMethod.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment(paramInfo.Name, comment, this.ClientProxyGenerator.IsCSharp));
            }

            // Generate <returns> doc comments
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Query_Method_Returns_Comment, domainOperationEntry.AssociatedType.Name);
            queryMethod.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Propagate custom validation attributes
            IEnumerable<Attribute> methodAttributes = domainOperationEntry.Attributes.Cast<Attribute>();
            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                this._proxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeMethod, ex.Message, queryMethod.Name, this._proxyClass.Name, ex.InnerException.Message),
                methodAttributes,
                queryMethod.CustomAttributes,
                queryMethod.Comments);

            // add any domain operation entry parameters first

            CodeVariableReferenceExpression paramsRef = new CodeVariableReferenceExpression("parameters");
            if (domainOperationEntryParameters.Length > 0)
            {
                // need to generate the user parameters dictionary
                CodeTypeReference dictionaryTypeReference = CodeGenUtilities.GetTypeReference(
                    typeof(Dictionary<string, object>),
                    this.ClientProxyGenerator,
                    this._proxyClass);

                CodeVariableDeclarationStatement paramsDef = new CodeVariableDeclarationStatement(
                    dictionaryTypeReference,
                    "parameters",
                    new CodeObjectCreateExpression(dictionaryTypeReference, Array.Empty<CodeExpression>()));
                queryMethod.Statements.Add(paramsDef);
            }
            foreach (DomainOperationParameter paramInfo in domainOperationEntryParameters)
            {
                CodeParameterDeclarationExpression paramDecl = new CodeParameterDeclarationExpression(
                        CodeGenUtilities.GetTypeReference(
                            CodeGenUtilities.TranslateType(paramInfo.ParameterType),
                            this.ClientProxyGenerator,
                            this._proxyClass),
                        paramInfo.Name);

                // Propagate parameter level validation attributes
                IEnumerable<Attribute> paramAttributes = paramInfo.Attributes.Cast<Attribute>();

                string commentHeader =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_Attribute_Parameter_FailedToGenerate,
                        paramInfo.Name);
                
                CustomAttributeGenerator.GenerateCustomAttributes(
                    this.ClientProxyGenerator, 
                    this._proxyClass,
                    ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeMethodParameter, ex.Message, paramDecl.Name, queryMethod.Name, this._proxyClass.Name, ex.InnerException.Message),
                    paramAttributes, 
                    paramDecl.CustomAttributes, 
                    queryMethod.Comments,
                    commentHeader);

                // add the parameter to the query method
                queryMethod.Parameters.Add(paramDecl);

                // add the parameter and value to the params dictionary
                queryMethod.Statements.Add(new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(paramsRef, "Add"),
                    new CodePrimitiveExpression(paramInfo.Name),
                    new CodeVariableReferenceExpression(paramInfo.Name)));
            }

            // add argument for queryName
            CodeExpressionCollection arguments = new CodeExpressionCollection();
            arguments.Add(new CodePrimitiveExpression(domainOperationEntry.Name));

            // add argument for parameters
            if (domainOperationEntryParameters.Length > 0)
            {
                arguments.Add(paramsRef);
            }
            else
            {
                arguments.Add(new CodePrimitiveExpression(null));
            }

            // add argument for hasSideEffects
            QueryAttribute queryAttribute = (QueryAttribute)domainOperationEntry.OperationAttribute;
            arguments.Add(new CodePrimitiveExpression(queryAttribute.HasSideEffects));

            // add argument for isComposable
            arguments.Add(new CodePrimitiveExpression(queryAttribute.IsComposable));

            // this.ValidateMethod("methodName", parameters);
            CodeExpression paramsExpr = new CodePrimitiveExpression(null);
            if (domainOperationEntryParameters.Length > 0)
            {
                paramsExpr = paramsRef;
            }
            CodeExpressionStatement validateMethodCall = new CodeExpressionStatement(
                new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(),
                    "ValidateMethod",
                    new CodeExpression[] 
                    {
                        new CodePrimitiveExpression(queryMethodName), 
                        paramsExpr
                    }));

            queryMethod.Statements.Add(validateMethodCall);

            // ----------------------------------------------------------------
            // method call: base.CreateQuery(arguments...)
            // ----------------------------------------------------------------
            CodeTypeReference entityTypeRef = CodeGenUtilities.GetTypeReference(entityType.FullName, this._domainServiceDescription.DomainServiceType.Namespace, true);
            CodeMethodReferenceExpression createQueryMethod = new CodeMethodReferenceExpression(new CodeBaseReferenceExpression(), "CreateQuery", entityTypeRef);
            CodeMethodReturnStatement createQueryCall = new CodeMethodReturnStatement(new CodeMethodInvokeExpression(createQueryMethod, arguments.Cast<CodeExpression>().ToArray()));

            queryMethod.Statements.Add(createQueryCall);

            this._proxyClass.Members.Add(queryMethod);
        }
    }
}
