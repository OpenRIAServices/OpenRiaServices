using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Subclass of <see cref="BusinessLogicContext"/> for the LinqToSql domain service
    /// </summary>
    internal class LinqToSqlContext : BusinessLogicContext
    {
        // This class supports LinqToSqlDomainService code gen only when the RIA Services Toolkit is installed.

        private const string ToolkitDomainServiceWizardRegKey = @"SOFTWARE\Microsoft\WCFRIAServices\v1.0\Toolkit\DomainServiceWizard";
        private const string AssemblyPathKeyValueName = "AssemblyPath";
        private const string EnableDataContextKeyValueName = "EnableDataContext";

        private static string linqToSqlDomainServiceAssemblyPath;

        private DataContext _dataContext;
        private MetaModel _metaModel;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="contextType">The CLR type of the <see cref="DomainContext"/></param>
        public LinqToSqlContext(Type contextType) : base(contextType, contextType.Name)
        {
        }

        /// <summary>
        /// Gets or sets a value that indicates whether DataContext types are enabled as candidates for code
        /// generation. This should only be set to <c>true</c> in the unit tests when the LinqToSql assembly
        /// is available.
        /// </summary>
        private static bool EnableDataContextTypes_TestOverride { get; set; }

        /// <summary>
        /// Determines whether DataContext types are enabled as candidates for DomainServices code generation.
        /// When true, LinqToSqlDomainService type are created for the DataContext types and can be displayed
        /// by the VS DomainService Wizard as candidates types to be added to the project.
        /// </summary>
        public static bool EnableDataContextTypes
        {
            get
            {
                if (LinqToSqlContext.EnableDataContextTypes_TestOverride)
                {
                    return true;
                }
                // NOTE: We don't cache the value of this property given the dynamic nature of the registry

                LinqToSqlContext.linqToSqlDomainServiceAssemblyPath = null;
                RegistryKey wizardRegKey = Registry.LocalMachine.OpenSubKey(LinqToSqlContext.ToolkitDomainServiceWizardRegKey);

                if (wizardRegKey != null)
                {
                    object enableDataContextValue = wizardRegKey.GetValue(EnableDataContextKeyValueName, string.Empty);

                    int result = 0;
                    
                    if (((enableDataContextValue != null) && int.TryParse(enableDataContextValue.ToString(), out result)) && (result == 1))
                    {
                        string assemblyFilePath = wizardRegKey.GetValue(AssemblyPathKeyValueName, null) as string;

                        if (File.Exists(assemblyFilePath))
                        {
                            try
                            {
                                // Test that the assembly can be loaded. 
                                // If for any reason the assembly is deleted between this property call and when it needs to be used, 
                                // the VS assembly loader will display a load error message (no security or reliability implications).
                                Assembly.LoadFrom(assemblyFilePath);
                                LinqToSqlContext.linqToSqlDomainServiceAssemblyPath = assemblyFilePath;
                            }
                            catch (FileNotFoundException)
                            {
                            }
                            catch (BadImageFormatException)
                            {
                            }
                            catch (FileLoadException)
                            { 
                            }
                        }
                    }

                    wizardRegKey.Close();
                }

                return (linqToSqlDomainServiceAssemblyPath != null);
            }
        }

        /// <summary>
        /// Gets the path to the Linq to Sql domain service or <c>null</c> if the toolkit has not been installed.
        /// </summary>
        internal static string LinqToSqlDomainServiceAssemblyPath
        {
            get
            {
                return (LinqToSqlContext.EnableDataContextTypes) ? LinqToSqlContext.linqToSqlDomainServiceAssemblyPath : null;
            }
        }

        /// <summary>
        /// Gets the name of the DAL technology of this context
        /// </summary>
        public override string DataAccessLayerName
        {
            get
            {
                return Resources.BusinessLogicClass_LinqToSql;
            }
        }

        /// <summary>
        /// Overrides the assembly path to the LinqToSql domain service assembly. This should only be
        /// used at test time.
        /// </summary>
        /// <param name="assemblyPath">The path for the LinqToSql assembly. Passing in <c>null</c> will
        /// reset the override.
        /// </param>
        internal static void OverrideAssemblyPath(string assemblyPath)
        {
            LinqToSqlContext.EnableDataContextTypes_TestOverride = !string.IsNullOrEmpty(assemblyPath);
            LinqToSqlContext.linqToSqlDomainServiceAssemblyPath = assemblyPath;
        }

        /// <summary>
        /// Called to create the entities known to this <see cref="DomainContext"/>
        /// </summary>
        /// <returns>The list of entities</returns>
        protected override IEnumerable<BusinessLogicEntity> CreateEntities()
        {
            MetaModel mm = this.MetaModel;
            IEnumerable<MetaTable> tables = mm.GetTables().Where(t => t.RowType.IsEntity);
            List<BusinessLogicEntity> entities = new List<BusinessLogicEntity>();

            foreach (MetaTable table in tables)
            {
                MetaType metaType = table.RowType;
                entities.Add(new LinqToSqlEntity(this, metaType));
            }

            return entities;
        }

        /// <summary>
        /// Creates the business logic class.  Overridden to add imports, base class, etc.
        /// </summary>
        /// <param name="codeGenContext">The code gen context.></param>
        /// <param name="codeNamespace">The namespace name.</param>
        /// <param name="className">The class name.</param>
        /// <returns>the new type</returns>
        protected override CodeTypeDeclaration CreateBusinessLogicClass(CodeGenContext codeGenContext, CodeNamespace codeNamespace, string className)
        {
            Debug.Assert(LinqToSqlContext.linqToSqlDomainServiceAssemblyPath != null, "Unexpected method call when LinqToSqlDomainService assembly path has not been initialized!");

            if (LinqToSqlContext.linqToSqlDomainServiceAssemblyPath == null)
            {
                return null;
            }

            // Add an import for our domain service
            foreach (string import in BusinessLogicClassConstants.LinqToSqlImports)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(import));
            }

            // Add an import for the namespace of the DomainContext
            if (this.ContextType.Namespace != codeNamespace.Name)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(this.ContextType.Namespace));
            }

            // Add to the set of known references
            codeGenContext.AddReference(typeof(DataContext).Assembly.FullName);
            codeGenContext.AddReference(LinqToSqlContext.linqToSqlDomainServiceAssemblyPath);

            CodeTypeDeclaration businessLogicClass = CodeGenUtilities.CreateTypeDeclaration(className, codeNamespace.Name);
            CodeTypeReference baseClass = new CodeTypeReference(BusinessLogicClassConstants.LinqToSqlDomainServiceTypeName, new CodeTypeReference(this.ContextType.Name));
            businessLogicClass.BaseTypes.Add(baseClass);
            return businessLogicClass;
        }

        /// <summary>
        /// Generates the select domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code gen context.></param>
        /// <param name="businessLogicClass">The business logic class.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The newly created method</returns>
        protected override CodeMemberMethod GenerateSelectMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            CodeMemberMethod method = null;
            LinqToSqlEntity ltsEntity = entity as LinqToSqlEntity;

            if (ltsEntity != null && ltsEntity.TablePropertyName != null)
            {
                // public IQueryable<$entityType$> GetEntities()
                method = new CodeMemberMethod();
                businessLogicClass.Members.Add(method);

                // Add developer comment explaining they can add additional parameters
                method.Comments.Add(new CodeCommentStatement(Resources.BusinessLogicClass_Query_Method_Remarks, false));
            
                method.Name = "Get" + CodeGenUtilities.MakeLegalEntityName(ltsEntity.TablePropertyName);
                method.ReturnType = new CodeTypeReference("IQueryable", new CodeTypeReference(entity.Name));
                method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

                // return this.DataContext.$TablePropertyName$
                CodeExpression contextExpr = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DataContext");
                CodeExpression expr = new CodePropertyReferenceExpression(contextExpr, ltsEntity.TablePropertyName);
                CodeMethodReturnStatement returnStmt = new CodeMethodReturnStatement(expr);
                method.Statements.Add(returnStmt);
            }
            return method;
        }

        /// <summary>
        /// Generates the insert domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code gen context.></param>
        /// <param name="businessLogicClass">The business logic class.</param>
        /// <param name="entity">The entity.</param>
        protected override void GenerateInsertMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            // public void Insert$EntityName$($entityType$ $entityName$)
            CodeMemberMethod method = new CodeMemberMethod();
            businessLogicClass.Members.Add(method);

            string parameterName = CodeGenUtilities.MakeLegalParameterName(entity.Name);

            LinqToSqlEntity ltsEntity = (LinqToSqlEntity)entity;
            method.Name = "Insert" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, parameterName));

            // this.DataContext.$TablePropertyName$.InsertOnSubmit($entity$)
            CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DataContext");
            CodeExpression tableRef = new CodePropertyReferenceExpression(contextRef, ltsEntity.TablePropertyName);
            CodeMethodInvokeExpression insertCall = new CodeMethodInvokeExpression(tableRef, "InsertOnSubmit", new CodeArgumentReferenceExpression(parameterName));
            method.Statements.Add(insertCall);
        }

        /// <summary>
        /// Generates the update domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code gen context.></param>
        /// <param name="businessLogicClass">The business logic class.</param>
        /// <param name="entity">The entity.</param>
        protected override void GenerateUpdateMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            string currentParameterName = "current" + entity.ClrType.Name;

            // public void Update$EntityName$($entityType$ current)
            CodeMemberMethod method = new CodeMemberMethod();
            businessLogicClass.Members.Add(method);

            LinqToSqlEntity ltsEntity = (LinqToSqlEntity)entity;
            method.Name = "Update" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, currentParameterName));

            if (!ltsEntity.HasTimestampMember)
            {
                // this.ChangeSet.GetOriginal(current)
                CodeExpression changeSetRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "ChangeSet");
                CodeMethodReferenceExpression getOrigMethodRef = new CodeMethodReferenceExpression(changeSetRef, "GetOriginal");
                CodeMethodInvokeExpression changeSetGetOrig = new CodeMethodInvokeExpression(getOrigMethodRef, new CodeArgumentReferenceExpression(currentParameterName));

                // this.DataContext.$TablePropertyName$.Attach(current, this.ChangeSet.GetOriginal(current))
                CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DataContext");
                CodeExpression tableRef = new CodePropertyReferenceExpression(contextRef, ltsEntity.TablePropertyName);
                CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(tableRef, "Attach", new CodeArgumentReferenceExpression(currentParameterName), changeSetGetOrig);
                method.Statements.Add(attachCall);
            }
            else
            {
                // this.DataContext.$TablePropertyName$.Attach(current, true)
                CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DataContext");
                CodeExpression tableRef = new CodePropertyReferenceExpression(contextRef, ltsEntity.TablePropertyName);
                CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(tableRef, "Attach", new CodeArgumentReferenceExpression(currentParameterName), new CodePrimitiveExpression(true));
                method.Statements.Add(attachCall);
            }
        }

        /// <summary>
        /// Generates the delete domain operation entry
        /// </summary>
        /// <param name="codeGenContext">The code gen context.></param>
        /// <param name="businessLogicClass">The business logic class.</param>
        /// <param name="entity">The entity.</param>
        protected override void GenerateDeleteMethod(CodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, BusinessLogicEntity entity)
        {
            string parameterName = CodeGenUtilities.MakeLegalParameterName(entity.Name);

            // public void Delete$EntityName$($entityType$ $entityName$)
            CodeMemberMethod method = new CodeMemberMethod();
            businessLogicClass.Members.Add(method);

            LinqToSqlEntity ltsEntity = (LinqToSqlEntity)entity;
            method.Name = "Delete" + CodeGenUtilities.MakeLegalEntityName(entity.Name);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;   // final needed to prevent virtual

            // parameter declaration
            method.Parameters.Add(new CodeParameterDeclarationExpression(entity.ClrType.Name, parameterName));

            // this.DataContext.$TablePropertyName$.Attach(current)
            CodeExpression contextRef = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "DataContext");
            CodeExpression tableRef = new CodePropertyReferenceExpression(contextRef, ltsEntity.TablePropertyName);
            CodeMethodInvokeExpression attachCall = new CodeMethodInvokeExpression(tableRef, "Attach", new CodeArgumentReferenceExpression(parameterName));
            method.Statements.Add(attachCall);

            // this.DataContext.$TablePropertyName$.DeleteOnSubmit(current)
            CodeMethodInvokeExpression deleteCall = new CodeMethodInvokeExpression(tableRef, "DeleteOnSubmit", new CodeArgumentReferenceExpression(parameterName));
            method.Statements.Add(deleteCall);
        }

        /// <summary>
        /// Gets the current <see cref="DataContext"/>
        /// </summary>
        private DataContext DataContext
        {
            get
            {
                if (this._dataContext == null)
                {
                    try
                    {
                        this._dataContext = (System.Data.Linq.DataContext)Activator.CreateInstance(this.ContextType, String.Empty);
                    }
                    catch (TargetInvocationException tie)
                    {
                        if (tie.InnerException != null)
                        {
                            throw tie.InnerException;
                        }

                        throw;
                    }
                }
                return this._dataContext;
            }
        }

        /// <summary>
        /// Gets the current <see cref="MetaModel"/>
        /// </summary>
        private MetaModel MetaModel
        {
            get
            {
                if (this._metaModel == null)
                {
                    this._metaModel = this.DataContext.Mapping;
                }
                return this._metaModel;
            }
        }
    }
}
