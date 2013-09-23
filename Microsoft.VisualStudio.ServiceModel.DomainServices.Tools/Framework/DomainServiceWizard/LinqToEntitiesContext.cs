using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Data.Metadata.Edm;
using System.Globalization;
using System.IO;
using OpenRiaServices.DomainServices.EntityFramework;
using System.Text;

namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Subclass of <see cref="LinqToEntitiesContextBase"/> that handles the LinqToEntities type of domain services based on ObjectContext
    /// </summary>
    internal class LinqToEntitiesContext : LinqToEntitiesContextBase
    {
        // Name of helper method we generate when we see POCO entities
        private const string GetEntityStateHelperMethodName = "GetEntityState";
        private const string EntityStatePropertyName = "EntityState";

        private MetadataWorkspace _metadataWorkspace;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="contextType">The CLR type of the <see cref="ObjectContext>"/></param>
        public LinqToEntitiesContext(Type contextType)
            : base(contextType)
        {
        }

        /// <summary>
        /// Gets a <see cref="MetadataWorkspace"/> for this context. It is created once and cached.
        /// </summary>
        public override MetadataWorkspace MetadataWorkspace
        {
            get
            {
                if (this._metadataWorkspace == null)
                {
                    this._metadataWorkspace = this.GetMetadataWorkspace();
                }
                return this._metadataWorkspace;
            }
        }

        private MetadataWorkspace GetMetadataWorkspace()
        {
            MetadataWorkspace metadataWorkspace = System.Data.Mapping.MetadataWorkspaceUtilities.CreateMetadataWorkspace(this.ContextType);
            metadataWorkspace.LoadFromAssembly(this.ContextType.Assembly);
            return metadataWorkspace;
        }

        /// <summary>
        /// Generates the business logic class type.  We override this to control the base class and imports
        /// </summary>
        /// <param name="codeGenContext">The context to use to generate code.</param>
        /// <param name="codeNamespace">The namespace into which to generate code.</param>
        /// <param name="className">The name to use for the class.</param>
        /// <returns>The new type</returns>
        protected override CodeTypeDeclaration CreateBusinessLogicClass(CodeGenContext codeGenContext, CodeNamespace codeNamespace, string className)
        {
            // Add an import for our domain service
            foreach (string import in BusinessLogicClassConstants.LinqToEntitiesImports)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(import));
            }

            // Add an import for the namespace of the DomainContext
            if (this.ContextType.Namespace != codeNamespace.Name)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(this.ContextType.Namespace));
            }

            // Add to the set of known references
            codeGenContext.AddReference(typeof(System.Data.Objects.ObjectContext).Assembly.FullName);
            codeGenContext.AddReference(typeof(LinqToEntitiesDomainService<>).Assembly.FullName);

            CodeTypeDeclaration businessLogicClass = CodeGenUtilities.CreateTypeDeclaration(className, codeNamespace.Name);
            CodeTypeReference baseClass = new CodeTypeReference(BusinessLogicClassConstants.LinqToEntitiesDomainServiceTypeName, new CodeTypeReference(this.ContextType.Name));
            businessLogicClass.BaseTypes.Add(baseClass);
            return businessLogicClass;
        }
        
        /// <summary>
        /// Generates the select domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code gen context.</param>
        /// <param name="businessLogicClass">Contains the business logic.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The newly created method.</returns>
        protected override CodeMemberMethod GenerateSelectMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            CodeMemberMethod method = null;
            LinqToEntitiesEntity efEntity = entity as LinqToEntitiesEntity;

            if (efEntity != null && efEntity.DefaultObjectSetName != null)
            {
                // public IQueryable<$entityType$> GetEntities()
                method = new CodeMemberMethod();
                businessLogicClass.Members.Add(method);

                // Add developer comment explaining they can add additional parameters
                method.Comments.Add(new CodeCommentStatement(Resources.BusinessLogicClass_Query_Method_Remarks, false));

                // And for EF, we add an additional comment warning they need to add ordering if they want paging
                string queryComment = String.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Query_Method_EF_Remarks, efEntity.DefaultObjectSetName);
                method.Comments.Add(new CodeCommentStatement(queryComment, false));

                method.Name = "Get" + CodeGenUtilities.MakeLegalEntityName(efEntity.DefaultObjectSetName);
                method.ReturnType = new CodeTypeReference("IQueryable", new CodeTypeReference(entity.Name));
                method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

                // return this.ObjectContext.$TablePropertyName$
                CodeExpression contextExpr = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ObjectContext");
                CodeExpression expr = new CodePropertyReferenceExpression(contextExpr, efEntity.DefaultObjectSetName);
                CodeMethodReturnStatement returnStmt = new CodeMethodReturnStatement(expr);
                method.Statements.Add(returnStmt);
            }
            return method;
        }

        /// <summary>
        /// Generates the insert domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code gen context.</param>
        /// <param name="businessLogicClass">Contains the business logic.</param>
        /// <param name="entity">The entity.</param>
        protected override void GenerateInsertMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            // public void Insert$EntityName$($entityType$ $entityName$)
            CodeMemberMethod method = new CodeMemberMethod();
            businessLogicClass.Members.Add(method);

            string parameterName = CodeGenUtilities.MakeLegalParameterName(entity.Name);

            LinqToEntitiesEntity efEntity = (LinqToEntitiesEntity)entity;
            method.Name = "Insert" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, parameterName));

            // Below we're generating the following method body
            // if ($entity$.EntityState != EntityState.Detached)
            // {
            //     this.ObjectContext.ObjectStateManager.ChangeObjectState($entity$, EntityState.Added);
            // }
            // else
            // {
            //     this.ObjectContext.$AddEntityMethodName$($entity$);
            // }
            //
            // In the case of POCO objects, we use "this.GetEntityState(entity)"
            // rather than referring to the entity's EntityState property

            CodeArgumentReferenceExpression entityArgRef = new CodeArgumentReferenceExpression(parameterName);
            CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ObjectContext");

            // If this is a POCO class, we need to generate a call to a helper method to get
            // the EntityState, otherwise we can directly de-reference it on the entity.
            // If this entity does not have an EntityState member, we need to use a helper method instead.
            // This call tells us whether we need this helper and, if so, generates it.
            bool useGetEntityStateHelper = LinqToEntitiesContext.GenerateGetEntityStateIfNecessary(codeGenContext, businessLogicClass, entity);

            CodeExpression getEntityStateExpr;
            if (useGetEntityStateHelper)
            {
                // this.GetEntityState($entity$)...
                getEntityStateExpr = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), LinqToEntitiesContext.GetEntityStateHelperMethodName, entityArgRef);
            }
            else
            {
                // $entity$.EntityState...
                getEntityStateExpr = new CodePropertyReferenceExpression(entityArgRef, LinqToEntitiesContext.EntityStatePropertyName);
            }

            CodeFieldReferenceExpression addedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Added));
            CodeFieldReferenceExpression detachedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Detached));
            CodePropertyReferenceExpression objectStateMgrRef = new CodePropertyReferenceExpression(contextRef, "ObjectStateManager");
            CodeMethodInvokeExpression changeStateExpr = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(objectStateMgrRef, "ChangeObjectState"), entityArgRef, addedStateRef);
            CodeExpression detachedStateTest = CodeGenUtilities.MakeNotEqual(typeof(EntityState), getEntityStateExpr, detachedStateRef, codeGenContext.IsCSharp);
            CodePropertyReferenceExpression objectSetRef = new CodePropertyReferenceExpression(contextRef, efEntity.DefaultObjectSetName);
            CodeMethodInvokeExpression insertCall = new CodeMethodInvokeExpression(objectSetRef, "AddObject", entityArgRef);
            CodeConditionStatement changeStateOrAddStmt = 
                new CodeConditionStatement(detachedStateTest, 
                    new CodeStatement[] { new CodeExpressionStatement(changeStateExpr) },
                    new CodeStatement[] { new CodeExpressionStatement(insertCall) });

            method.Statements.Add(changeStateOrAddStmt);
        }

        /// <summary>
        /// Generates the update domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The context to use</param>
        /// <param name="businessLogicClass">The business logic class into which to generate it</param>
        /// <param name="entity">The entity for which to generate the method</param>
        protected override void GenerateUpdateMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            string currentParameterName = "current" + entity.ClrType.Name;

            // public void Update$EntityName$($entityType$ current)
            CodeMemberMethod method = new CodeMemberMethod();
            businessLogicClass.Members.Add(method);

            //LinqToEntitiesEntity efEntity = (LinqToEntitiesEntity)entity;
            method.Name = "Update" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, currentParameterName));

            LinqToEntitiesEntity efEntity = (LinqToEntitiesEntity)entity;
            if (!efEntity.HasTimestampMember)
            {
                // this.ChangeSet.GetOriginal(current)
                CodeExpression changeSetRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ChangeSet");
                CodeMethodReferenceExpression getOrigMethodRef = new CodeMethodReferenceExpression(changeSetRef, "GetOriginal");
                CodeMethodInvokeExpression changeSetGetOrig = new CodeMethodInvokeExpression(getOrigMethodRef, new CodeArgumentReferenceExpression(currentParameterName));

                // this.ObjectContext.$ObjectSetName$.AttachAsModified($current$, this.ChangeSet.GetOriginal(current));
                CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ObjectContext");
                CodePropertyReferenceExpression objectSetRef = new CodePropertyReferenceExpression(contextRef, efEntity.DefaultObjectSetName);
                CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(objectSetRef, "AttachAsModified", new CodeArgumentReferenceExpression(currentParameterName), changeSetGetOrig);
                CodeExpressionStatement attachStmt = new CodeExpressionStatement(attachCall);
                method.Statements.Add(attachStmt);
            }
            else
            {
                // this.ObjectContext.$ObjectSetName$.AttachAsModified($current$);
                CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ObjectContext");
                CodePropertyReferenceExpression objectSetRef = new CodePropertyReferenceExpression(contextRef, efEntity.DefaultObjectSetName);
                CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(objectSetRef, "AttachAsModified", new CodeArgumentReferenceExpression(currentParameterName));
                CodeExpressionStatement attachStmt = new CodeExpressionStatement(attachCall);
                method.Statements.Add(attachStmt);
            }
        }

        /// <summary>
        /// Generates the delete domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The context to use</param>
        /// <param name="businessLogicClass">The business logic class into which to generate it</param>
        /// <param name="entity">The entity for which to generate the method</param>
        protected override void GenerateDeleteMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            string parameterName = CodeGenUtilities.MakeLegalParameterName(entity.Name);

            // public void Delete$EntityName$($entityType$ $entityName$)
            CodeMemberMethod method = new CodeMemberMethod();
            businessLogicClass.Members.Add(method);

            //LinqToEntitiesEntity efEntity = (LinqToEntitiesEntity)entity;
            method.Name = "Delete" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, parameterName));

            // if ($entity$.EntityState != EntityState.Detached) 
            // {
            //      ObjectContext.ObjectStateManager.ChangeObjectState($entity$, EntityState.Deleted);
            // } 
            // else 
            // { 
            //      ObjectContext.Products.Attach($entity$); 
            //      ObjectContext.Products.DeleteObject($entity$);    
            // }
            //
            // In the case of POCO objects, we use "this.GetEntityState(entity)"
            // rather than referring to the entity's EntityState property

            LinqToEntitiesEntity efEntity = (LinqToEntitiesEntity)entity;
            CodeArgumentReferenceExpression entityExpr = new CodeArgumentReferenceExpression(parameterName);

            // If this is a POCO class, we need to generate a call to a helper method to get
            // the EntityState, otherwise we can directly de-reference it on the entity
            // If this entity does not have an EntityState member, we need to use a helper method instead.
            // This call tells us whether we need this helper and, if so, generates it.
            bool useGetEntityStateHelper = LinqToEntitiesContext.GenerateGetEntityStateIfNecessary(codeGenContext, businessLogicClass, entity);

            CodeExpression getEntityStateExpr;
            if (useGetEntityStateHelper)
            {
                // this.GetEntityState($entity$)...
                getEntityStateExpr = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), LinqToEntitiesContext.GetEntityStateHelperMethodName, entityExpr);
            }
            else
            {
                // $entity$.EntityState...
                getEntityStateExpr = new CodePropertyReferenceExpression(entityExpr, LinqToEntitiesContext.EntityStatePropertyName);
            }

            CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ObjectContext");
            CodePropertyReferenceExpression objectSetRef = new CodePropertyReferenceExpression(contextRef, efEntity.DefaultObjectSetName);
            CodeFieldReferenceExpression detachedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Detached));
            CodeExpression equalTest = CodeGenUtilities.MakeNotEqual(typeof(EntityState), getEntityStateExpr, detachedStateRef, codeGenContext.IsCSharp);

            CodeFieldReferenceExpression deletedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Deleted));
            CodePropertyReferenceExpression objectStateMgrRef = new CodePropertyReferenceExpression(contextRef, "ObjectStateManager");
            CodeMethodInvokeExpression changeStateExpr = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(objectStateMgrRef, "ChangeObjectState"), entityExpr, deletedStateRef);
            CodeStatement[] trueStatements = new CodeStatement[] { new CodeExpressionStatement(changeStateExpr) };

            CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(objectSetRef, "Attach", entityExpr);
            CodeMethodInvokeExpression deleteCall = new CodeMethodInvokeExpression(objectSetRef, "DeleteObject", entityExpr);
            CodeStatement[] falseStatements = new CodeStatement[] { new CodeExpressionStatement(attachCall), new CodeExpressionStatement(deleteCall) };

            CodeConditionStatement ifStmt = new CodeConditionStatement(equalTest, trueStatements, falseStatements);
            method.Statements.Add(ifStmt);
        }        

        /// <summary>
        /// Generates the GetEntityState helper method that allows POCO types to retrieve their
        /// entity state from the contect.  It is not available on the POCO types directly.
        /// </summary>
        /// <param name="codeGenContext">The context in which we are generating code.</param>
        /// <param name="businessLogicClass">The class we are generating.</param>
        /// <returns>The <see cref="CodeTypeMember"/> containing the helper method.</returns>
        private static CodeTypeMember GenerateGetEntityState(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass)
        {
            // Add an import for System.Data.Objects
            CodeNamespace codeNamespace = codeGenContext.GetNamespace(businessLogicClass);
            if (codeNamespace != null)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport("System.Data.Objects"));
            }

            //private EntityState GetEntityState(object entity)
            //{
            //    ObjectStateEntry stateEntry = null;
            //    if (!this.ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out stateEntry))
            //    {
            //        return EntityState.Detached;
            //    }
            //    return stateEntry.State;
            //}

            // Declaration
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = LinqToEntitiesContext.GetEntityStateHelperMethodName;
            method.ReturnType = new CodeTypeReference(typeof(EntityState).Name);
            method.Attributes = MemberAttributes.Private;
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "entity"));

            // ObjectStateEntry stateEntry = null;
            method.Statements.Add(new CodeVariableDeclarationStatement("ObjectStateEntry", "stateEntry", new CodePrimitiveExpression(null)));

            CodeArgumentReferenceExpression entityArgRef = new CodeArgumentReferenceExpression("entity");
            CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ObjectContext");

            CodeFieldReferenceExpression detachedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Detached));
            CodePropertyReferenceExpression objectStateMgrRef = new CodePropertyReferenceExpression(contextRef, "ObjectStateManager");

            CodeVariableReferenceExpression entityStateRef = new CodeVariableReferenceExpression("stateEntry");

            // The "_out_" prefix will be replaced below with the language-appropriate modifier to make an out param.
            // CodeDom does not support this, so we must do some string manipulation
            CodeVariableReferenceExpression outEntityStateRef = new CodeVariableReferenceExpression("_out_stateEntry");

            // this.ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out stateEntry)
            CodeMethodInvokeExpression getObjectStateEntryCall = new CodeMethodInvokeExpression(objectStateMgrRef, "TryGetObjectStateEntry", entityArgRef, outEntityStateRef);

            // if (...TryGet())
            CodeExpression tryGetTest = CodeGenUtilities.MakeEqual(typeof(bool), getObjectStateEntryCall, new CodePrimitiveExpression(false), codeGenContext.IsCSharp);

            // if (...TryGet..) { return EntityState.Detached; }
            CodeMethodReturnStatement returnDetached = new CodeMethodReturnStatement(detachedStateRef);
            CodeConditionStatement ifTryGet = new CodeConditionStatement(tryGetTest, returnDetached);
            method.Statements.Add(ifTryGet);

            // Return entityState.State;
            method.Statements.Add(new CodeMethodReturnStatement(new CodePropertyReferenceExpression(entityStateRef, "State")));

            // CodeDom does not support specifying 'out' parameters at the method call site.
            // So convert the entire method into a snippet of text

            StringBuilder snippet = null;
            CodeDomProvider provider = codeGenContext.Provider;
            using (StringWriter snippetWriter = new StringWriter(System.Globalization.CultureInfo.CurrentCulture))
            {
                provider.GenerateCodeFromMember(method, snippetWriter, codeGenContext.CodeGeneratorOptions);
                snippet = snippetWriter.GetStringBuilder();
            }

            // Our convention above is that "_out_" will be replaced by the language-appropriate "out" parameter modifier.
            // In the case of VB, it is the default
            snippet.Replace("_out_", codeGenContext.IsCSharp ? "out " : string.Empty);

            // We need to indent the entire snippet 2 levels
            string indent = codeGenContext.CodeGeneratorOptions.IndentString;
            indent += indent;

            string snippetText = indent + snippet.ToString().Replace(Environment.NewLine, Environment.NewLine + indent).TrimEnd(' ');
            CodeSnippetTypeMember methodAsText = new CodeSnippetTypeMember(snippetText);

            return methodAsText;
        }

        /// <summary>
        /// Tests whether we need to generate the GetEntityState helper method.  We do for POCO types.
        /// If we determine we need to generate that helper, this method generates it and adds it to
        /// the list of helper methods that will be appended to the generated code.
        /// </summary>
        /// <param name="codeGenContext">The context in which we are generating code.</param>
        /// <param name="businessLogicClass">The class containing the generated code.</param>
        /// <param name="entity">The entity that we need to test to determine whether the helper is needed.</param>
        /// <returns><c>true</c> means the helper should be used.</returns>
        private static bool GenerateGetEntityStateIfNecessary(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            if (typeof(System.Data.Objects.DataClasses.EntityObject).IsAssignableFrom(entity.ClrType))
            {
                return false;
            }
            BusinessLogicContext.GenerateHelperMemberIfNecessary(codeGenContext, businessLogicClass, LinqToEntitiesContext.GetEntityStateHelperMethodName, () =>
            {
                return LinqToEntitiesContext.GenerateGetEntityState(codeGenContext, businessLogicClass);
            });
            return true;
        }
    }
}
