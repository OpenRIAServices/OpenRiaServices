using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using OpenRiaServices.Tools;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Handles the LinqToEntities type of domain services based on EF Core DbContext
    /// </summary>
    public class LinqToEntitiesEFCoreDbContext : BusinessLogicContext
    {        
        private bool? _isCodeFirstModel = null;
        private readonly HashSet<Type> _visitedComplexTypes = new HashSet<Type>();

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="contextType">The CLR type of the <see cref="ObjectContext>"/></param>
        public LinqToEntitiesEFCoreDbContext(Type contextType)
            : base(contextType, contextType.Name)
        {
          
        }

        /// <summary>
        /// Gets the name of the DAL technology of this context
        /// </summary>
        public override string DataAccessLayerName
        {
            get
            {
                return "EF Core";
            }
        }

       
        /// <summary>
        /// Invoked to create the entities known to this context
        /// </summary>
        /// <returns>The list of entities</returns>
        protected override IEnumerable<BusinessLogicEntity> CreateEntities()
        {
            List<BusinessLogicEntity> entities = new List<BusinessLogicEntity>();

            return entities;
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
        protected override bool CanGeneratePropertyOfType(Type type)
        {
            if (base.CanGeneratePropertyOfType(type))
            {
                return true;
            }
            return false;
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
           // codeGenContext.AddReference(typeof(EntityState).Assembly.FullName);
            
            // We used to add OpenRiaServices.EntityFramework, but due to
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

      
    }
}
