using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Globalization;
using System.Reflection;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Tools;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Subclass of <see cref="LinqToEntitiesContextBase"/> that handles the LinqToEntities type of domain services based on DbContext
    /// </summary>
    public class LinqToEntitiesDbContext : LinqToEntitiesContextBase
    {        
        private MetadataWorkspace _metadataWorkspace;
        private bool? _isCodeFirstModel = null;

        public LinqToEntitiesDbContext(Type contextType)
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
                    this.InitMetadataWorkspace();
                }
                return this._metadataWorkspace;
            }
        }    

        /// <summary>
        /// Returns true is we need to generate a metadata class.
        /// </summary>
        public override bool NeedToGenerateMetadataClasses
        {
            get
            {
                if (this._isCodeFirstModel == null)
                {
                    this._isCodeFirstModel = !this.TryInitMetadataWorkspaceFromResources();
                }
                return !(bool)this._isCodeFirstModel;
            }
        }

        /// <summary>
        /// Try to initialize the MetadataWorkspace from the embedded resources.
        /// </summary>
        /// <returns>If the MetadataWorkspace creation was successful.</returns>
        private bool TryInitMetadataWorkspaceFromResources()
        {
            this._metadataWorkspace = System.Data.Mapping.MetadataWorkspaceUtilities.CreateMetadataWorkspaceFromResources(this.ContextType, DbContextUtilities.DbContextTypeReference);
            return this._metadataWorkspace != null;
        }   

        /// <summary>
        /// Generates the MetadataWorkspace for the Data model. Tries to create the MetadataWorkspace from the csdl, ssdl and msl files. If they do not exist, then tries to
        /// instantiate the DbContext object. Also sets the _isCodeFirstModel field value.
        /// </summary>        
        private void InitMetadataWorkspace()
        {
            if (this._isCodeFirstModel == null)
            {
                this._isCodeFirstModel = !this.TryInitMetadataWorkspaceFromResources();
            }

            // If metadataWorkspace cannot be obtained from the resources, new up the DbContext.
            if ((bool)this._isCodeFirstModel)
            {
                // First set the DatabaseInitializer to null. So do something to the effect of - System.Data.Entity.Database.SetInitializer<DbContextType>(null);
                DbContextUtilities.SetDbInitializer(this.ContextType, DbContextUtilities.DbContextTypeReference, null);

                // For CodeFirst mode, we need the DbContext type to have the default ctor.
                if (this.ContextType.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.LinqToEntitiesDbContext_DefaultCtorNotFound, this.ContextType.FullName));
                }
                
                try
                {
                    // New up the DbContext.
                    object dbContext = Activator.CreateInstance(this.ContextType);
                    if (dbContext != null)
                    {
                        Type dbContextTypeReference = DbContextUtilities.GetDbContextTypeReference(this.ContextType);
                        System.Diagnostics.Debug.Assert(dbContextTypeReference != null, "If we have the DbContext type, then typeof(DbContext) should not be null.");
                        Type objectContextAdapter = DbContextUtilities.LoadTypeFromAssembly(dbContextTypeReference.Assembly, BusinessLogicClassConstants.IObjectContextAdapterTypeName);
                        if (objectContextAdapter != null)
                        {
                            PropertyInfo objectContextProperty = objectContextAdapter.GetProperty("ObjectContext");
                            if (objectContextProperty != null)
                            {
                                ObjectContext objectContext = objectContextProperty.GetValue(dbContext, null) as ObjectContext;
                                if (objectContext != null)
                                {
                                    this._metadataWorkspace = objectContext.MetadataWorkspace;
                                }
                            }
                        }
                    }
                }
                catch (TargetInvocationException tie)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.LinqToEntitiesDbContext_UnableToCreateContext, tie.InnerException ?? tie), tie.InnerException);
                }
                catch (Exception e)
                {
                    if (e.IsFatal())
                    {
                        throw;
                    }
                    
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.LinqToEntitiesDbContext_UnableToCreateContext), e);
                }
            }
            this._metadataWorkspace.LoadFromAssembly(this.ContextType.Assembly);
        }

        /// <summary>
        /// Returns the expression for this.DbContext
        /// </summary>
        /// <returns>The this.DbContext expression.</returns>
        private static CodeExpression GetContextReferenceExpression()
        {
            return new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DbContext");
        }

        /// <summary>
        /// Returns the expression for this.DbContext.$TablePropertyName$.
        /// </summary>
        /// <param name="efDbEntity">The entity in for which the DbSet is needed.</param>
        /// <returns>The this.DbContext.$TablePropertyName$ expression.</returns>
        private static CodeExpression GetDbSetReferenceExpression(LinqToEntitiesEntity efDbEntity)
        {
            CodeExpression contextExpr = LinqToEntitiesDbContext.GetContextReferenceExpression();
            CodeExpression expr = new CodePropertyReferenceExpression(contextExpr, efDbEntity.DefaultObjectSetName);
            return expr;
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
            foreach (string import in BusinessLogicClassConstants.LinqToEntitiesDbImports)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(import));
            }

            // Add an import for the namespace of the DomainContext
            if (this.ContextType.Namespace != codeNamespace.Name)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(BusinessLogicClassConstants.DbContextNamespace));
                codeNamespace.Imports.Add(new CodeNamespaceImport(this.ContextType.Namespace));
            }

            // Add to the set of known references
            codeGenContext.AddReference(typeof(EntityState).Assembly.FullName);
            
            // We used to add OpenRiaServices.DomainServices.EntityFramework, but due to
            // vstfdevdiv/DevDiv2 Bug 442272 - Domain Service Wizard failing when an EF DbContext is selected,
            // we need to avoid doing that.

            if (DbContextUtilities.DbContextTypeReference != null)
            {
                codeGenContext.AddReference(DbContextUtilities.DbContextTypeReference.Assembly.FullName);
            }
            
            CodeTypeDeclaration businessLogicClass = CodeGenUtilities.CreateTypeDeclaration(className, codeNamespace.Name);
            CodeTypeReference baseClass = new CodeTypeReference(BusinessLogicClassConstants.DbDomainServiceTypeName, new CodeTypeReference(this.ContextType.Name));
            businessLogicClass.BaseTypes.Add(baseClass);
            return businessLogicClass;
        }

        /// <summary>
        /// Generates the select domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code generation context.</param>
        /// <param name="businessLogicClass">Contains the business logic.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The newly created method.</returns>
        protected override CodeMemberMethod GenerateSelectMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            CodeMemberMethod method = null;
            LinqToEntitiesEntity efDbEntity = entity as LinqToEntitiesEntity;

            if (efDbEntity != null && efDbEntity.DefaultObjectSetName != null)
            {
                // public IQueryable<$entityType$> GetEntities()
                method = new CodeMemberMethod();
                businessLogicClass.Members.Add(method);

                // Add developer comment explaining they can add additional parameters
                method.Comments.Add(new CodeCommentStatement(Resources.BusinessLogicClass_Query_Method_Remarks, false));

                // And for EF, we add an additional comment warning they need to add ordering if they want paging
                string queryComment = String.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Query_Method_EF_Remarks, efDbEntity.DefaultObjectSetName);
                method.Comments.Add(new CodeCommentStatement(queryComment, false));

                method.Name = "Get" + CodeGenUtilities.MakeLegalEntityName(efDbEntity.DefaultObjectSetName);
                method.ReturnType = new CodeTypeReference("IQueryable", new CodeTypeReference(entity.Name));
                method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

                // return this.DbContext.$TablePropertyName$
                CodeExpression contextExpr = LinqToEntitiesDbContext.GetContextReferenceExpression();
                CodeExpression expr = new CodePropertyReferenceExpression(contextExpr, efDbEntity.DefaultObjectSetName);
                CodeMethodReturnStatement returnStmt = new CodeMethodReturnStatement(expr);
                method.Statements.Add(returnStmt);
            }
            return method;
        }

        /// <summary>
        /// Generates the insert domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code generation context.</param>
        /// <param name="businessLogicClass">Contains the business logic.</param>
        /// <param name="entity">The entity.</param>
        protected override void GenerateInsertMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            // public void Insert$EntityName$($entityType$ $entityName$)
            CodeMemberMethod method = new CodeMemberMethod();
            businessLogicClass.Members.Add(method);

            string parameterName = CodeGenUtilities.MakeLegalParameterName(entity.Name);

            LinqToEntitiesEntity efDbEntity = (LinqToEntitiesEntity)entity;
            method.Name = "Insert" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, parameterName));

            // Below we're generating the following method body
            // DbEntityEntry<$entityType$> entityEntry = this.DbContext.Entry($entity$);
            // if (entityEntry.State != EntityState.Detached)
            // {
            //     entityEntry.State = EntityState.Added;
            // }
            // else
            // {
            //     this.DbContext.$TablePropertyName$.Add($entity$);
            // }

            CodeArgumentReferenceExpression entityArgRef = new CodeArgumentReferenceExpression(parameterName);
            CodeExpression contextRef = LinqToEntitiesDbContext.GetContextReferenceExpression();

            CodeVariableDeclarationStatement entityEntryDeclaration = new CodeVariableDeclarationStatement(
                new CodeTypeReference("DbEntityEntry", new CodeTypeReference(entity.ClrType.Name)),
                "entityEntry",
                new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(contextRef, "Entry"), entityArgRef));
            method.Statements.Add(entityEntryDeclaration);
            
            CodeVariableReferenceExpression entityEntryRef = new CodeVariableReferenceExpression("entityEntry");
            CodePropertyReferenceExpression entityStateRef = new CodePropertyReferenceExpression(entityEntryRef, "State");
            CodeFieldReferenceExpression detachedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Detached));
            CodeExpression detachedStateTestExpr = CodeGenUtilities.MakeNotEqual(typeof(EntityState), entityStateRef, detachedStateRef, codeGenContext.IsCSharp);

            CodeFieldReferenceExpression addedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Added));
            CodeAssignStatement addedStateExpr = new CodeAssignStatement(entityStateRef, addedStateRef);
            CodeMethodInvokeExpression addEntityMethodCall = new CodeMethodInvokeExpression(LinqToEntitiesDbContext.GetDbSetReferenceExpression(efDbEntity), "Add", entityArgRef);

            CodeConditionStatement changeStateOrAddStmt =
                new CodeConditionStatement(detachedStateTestExpr,
                    new CodeStatement[] { addedStateExpr },
                    new CodeStatement[] { new CodeExpressionStatement(addEntityMethodCall) });

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

                // this.DbContext.$ObjectSetName$.AttachAsModified($current$, this.ChangeSet.GetOriginal(current), this.DbContext);
                CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DbContext");
                CodePropertyReferenceExpression objectSetRef = new CodePropertyReferenceExpression(contextRef, efEntity.DefaultObjectSetName);
                CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(objectSetRef, "AttachAsModified", 
                    new CodeArgumentReferenceExpression(currentParameterName), changeSetGetOrig, new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DbContext"));
                CodeExpressionStatement attachStmt = new CodeExpressionStatement(attachCall);
                method.Statements.Add(attachStmt);
            }
            else
            {
                // this.DbContext.$ObjectSetName$.AttachAsModified($current$, this.DbContext);
                CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DbContext");
                CodePropertyReferenceExpression objectSetRef = new CodePropertyReferenceExpression(contextRef, efEntity.DefaultObjectSetName);
                CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(objectSetRef, "AttachAsModified",
                    new CodeArgumentReferenceExpression(currentParameterName), new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DbContext"));
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

            LinqToEntitiesEntity efDbEntity = (LinqToEntitiesEntity)entity;
            method.Name = "Delete" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, parameterName));

            // Below we're generating the following method body
            
            // DbEntityEntry<$entityType$> entityEntry = this.DbContext.Entry($entity$);
            // if (entityEntry.State != EntityState.Detached)
            // {
            //     entityEntry.State = EntityState.Deleted;
            // }
            // else
            // {
            //     this.DbContext.$TablePropertyName$.Attach($entity$);
            //     this.DbContext.$TablePropertyName$.Remove($entity$);
            // }

            CodeArgumentReferenceExpression entityArgRef = new CodeArgumentReferenceExpression(parameterName);
            CodeExpression contextRef = LinqToEntitiesDbContext.GetContextReferenceExpression();

            CodeVariableDeclarationStatement entityEntryDeclaration = new CodeVariableDeclarationStatement(
                new CodeTypeReference("DbEntityEntry", new CodeTypeReference(entity.ClrType.Name)),
                "entityEntry",
                new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(contextRef, "Entry"), entityArgRef));
            method.Statements.Add(entityEntryDeclaration);

            CodeVariableReferenceExpression entityEntryRef = new CodeVariableReferenceExpression("entityEntry");
            CodePropertyReferenceExpression entityStateRef = new CodePropertyReferenceExpression(entityEntryRef, "State");
            CodeFieldReferenceExpression detachedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Deleted));
            CodeExpression detachedStateTestExpr = CodeGenUtilities.MakeNotEqual(typeof(EntityState), entityStateRef, detachedStateRef, codeGenContext.IsCSharp);

            CodeFieldReferenceExpression deletedStateRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EntityState).Name), Enum.GetName(typeof(EntityState), EntityState.Deleted));
            CodeAssignStatement deletedStateExpr = new CodeAssignStatement(entityStateRef, deletedStateRef);
            CodeMethodInvokeExpression attachedEntityMethodCall = new CodeMethodInvokeExpression(LinqToEntitiesDbContext.GetDbSetReferenceExpression(efDbEntity), "Attach", entityArgRef);
            CodeMethodInvokeExpression removedEntityMethodCall = new CodeMethodInvokeExpression(LinqToEntitiesDbContext.GetDbSetReferenceExpression(efDbEntity), "Remove", entityArgRef);

            CodeConditionStatement changeStateOrAddStmt =
                new CodeConditionStatement(detachedStateTestExpr,
                    new CodeStatement[] { deletedStateExpr },
                    new CodeStatement[] { new CodeExpressionStatement(attachedEntityMethodCall), new CodeExpressionStatement(removedEntityMethodCall) });

            method.Statements.Add(changeStateOrAddStmt);
        }        
    }
}