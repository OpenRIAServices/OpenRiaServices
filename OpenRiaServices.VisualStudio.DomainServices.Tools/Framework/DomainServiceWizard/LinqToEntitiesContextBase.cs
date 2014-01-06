using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using OpenRiaServices.DomainServices.Tools;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Subclass of <see cref="BusinessLogicContext"/> that handles the LinqToEntities type of domain service
    /// </summary>
    public abstract class LinqToEntitiesContextBase : BusinessLogicContext
    {
        private Dictionary<ComplexType, Type> _complexTypes;
        private HashSet<Type> _visitedComplexTypes = new HashSet<Type>();

        public Dictionary<ComplexType, Type> ComplexTypes
        {
            get
            {
                if (this._complexTypes == null)
                {
                    this._complexTypes = new Dictionary<ComplexType, Type>();
                }
                return this._complexTypes;
            }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="contextType">The CLR type of the <see cref="ObjectContext>"/></param>
        public LinqToEntitiesContextBase(Type contextType)
            : base(contextType, contextType.Name)
        {
            this.PopulateComplexTypesInObjectContext();
        }

        /// <summary>
        /// Gets the name of the DAL technology of this context
        /// </summary>
        public override string DataAccessLayerName
        {
            get
            {
                return Resources.BusinessLogicClass_EntityFramework;
            }
        }

        /// <summary>
        /// Gets a <see cref="MetadataWorkspace"/> for this context.
        /// </summary>
        public abstract MetadataWorkspace MetadataWorkspace
        {
            get;
        }

        /// <summary>
        /// Invoked to create the entities known to this context
        /// </summary>
        /// <returns>The list of entities</returns>
        protected override IEnumerable<IBusinessLogicEntity> CreateEntities()
        {
            List<BusinessLogicEntity> entities = new List<BusinessLogicEntity>();

            Dictionary<EntityType, Type> entitiesInContext = this.GetEntityTypesInContext();
            foreach (KeyValuePair<EntityType, Type> pair in entitiesInContext)
            {
                entities.Add(new LinqToEntitiesEntity(this, pair.Key, pair.Value));
            }
            return entities;
        }

        /// <summary>
        /// Gets a dictionary of all entity types associated with the current DbContext type.
        /// The key is the EntityType (CSpace) and the value is the CLR type of the generated entity.
        /// </summary>
        /// <returns>A dictionary keyed by entity type and returning corresponding CLR type</returns>
        private Dictionary<EntityType, Type> GetEntityTypesInContext()
        {
            Dictionary<EntityType, Type> entities = new Dictionary<EntityType, Type>();
            ObjectItemCollection itemCollection = this.MetadataWorkspace.GetItemCollection(DataSpace.OSpace) as ObjectItemCollection;
            foreach (EntityType objectSpaceEntityType in itemCollection.GetItems<EntityType>())
            {
                Type clrType = itemCollection.GetClrType(objectSpaceEntityType);

                // Skip the EF CodeFirst specific EdmMetadata type
                if (DbContextUtilities.CompareWithSystemType(clrType, BusinessLogicClassConstants.EdmMetadataTypeName))
                {
                    continue;
                }

                StructuralType edmEntityType;
                if (this.MetadataWorkspace.TryGetEdmSpaceType(objectSpaceEntityType, out edmEntityType))
                {
                    entities[(EntityType)edmEntityType] = clrType;
                }
            }
            return entities;
        }



        /// <summary>
        /// Populates the ComplexTypes and GeneratedComplexTypes dictionaries from the MetadataWorkspace.
        /// </summary>
        private void PopulateComplexTypesInObjectContext()
        {
            ObjectItemCollection itemCollection = this.MetadataWorkspace.GetItemCollection(DataSpace.OSpace) as ObjectItemCollection;
            foreach (ComplexType objectSpaceEntityType in itemCollection.GetItems<ComplexType>())
            {
                Type clrType = itemCollection.GetClrType(objectSpaceEntityType);
                StructuralType edmComplexType;
                if (this.MetadataWorkspace.TryGetEdmSpaceType(objectSpaceEntityType, out edmComplexType))
                {
                    this.ComplexTypes[(ComplexType)edmComplexType] = clrType;
                }
            }
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
            if (this.ComplexTypes.ContainsValue(type))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// If the entity contains properties of complex types, then this class generates metadata classes for those complex types.
        /// </summary>
        /// <param name="codeGenContext">The context to use to generate code.</param>
        /// <param name="optionalSuffix">If not null, optional suffix to class name and namespace</param>
        /// <param name="entity">The entity for which to generate the additional metadata.</param>
        /// <returns><c>true</c> means at least some code was generated.</returns>
        protected override bool GenerateAdditionalMetadataClasses(ICodeGenContext codeGenContext, string optionalSuffix, BusinessLogicEntity entity)
        {
            LinqToEntitiesEntity linqToEntitiesEntity = (LinqToEntitiesEntity)entity;
            bool generatedCode = this.GenerateMetadataClassesForComplexTypes(codeGenContext, optionalSuffix, linqToEntitiesEntity.EntityType.Properties);
            return generatedCode;
        }

        /// <summary>
        /// Recursively generates metadata classes for properties of complex types.
        /// </summary>
        /// <param name="codeGenContext">The context to use to generate code.</param>
        /// <param name="optionalSuffix">If not null, optional suffix to class name and namespace</param>
        /// <param name="properties">The list of properties for which to generate metadata classes.</param>
        /// <returns><c>true</c> means at least some code was generated.</returns>
        private bool GenerateMetadataClassesForComplexTypes(ICodeGenContext codeGenContext, string optionalSuffix, IEnumerable<EdmProperty> properties)
        {
            bool generatedCode = false;
            foreach (var prop in properties)
            {
                EdmType propEdmType = prop.TypeUsage.EdmType;
                if (propEdmType.BuiltInTypeKind == BuiltInTypeKind.ComplexType)
                {
                    Type propClrType;
                    ComplexType propComplexType = (ComplexType)propEdmType;
                    if (this.ComplexTypes.TryGetValue(propComplexType, out propClrType) && !this._visitedComplexTypes.Contains(propClrType))
                    {
                        generatedCode |= this.GenerateMetadataClass(codeGenContext, optionalSuffix, propClrType);
                        this._visitedComplexTypes.Add(propClrType);

                        // Recursively generate Metadata classes for properties of complex types on this comples type.
                        generatedCode |= this.GenerateMetadataClassesForComplexTypes(codeGenContext, optionalSuffix, propComplexType.Properties);
                    }
                }
            }
            return generatedCode;
        }
    }
}
