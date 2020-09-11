using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;
using OpenRiaServices.EntityFramework;
using OpenRiaServices.Tools;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Subclass of <see cref="BusinessLogicEntity"/> for the LinqToEntities domain service
    /// </summary>
    public class LinqToEntitiesEntity : BusinessLogicEntity
    {
        private string _defaultObjectSetName;
        private readonly bool _hasTimestampMember;
        private readonly EntityType _entityType;
        private readonly bool _isDbContext;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="context">The linq to entities context owning this entity</param>
        /// <param name="entityType">The entity type in the EDM model</param>
        /// <param name="type">The CLR type of the entity</param>
        public LinqToEntitiesEntity(LinqToEntitiesContextBase context, EntityType entityType, Type type)
            : base(context, entityType.Name, type)
        {
            this._hasTimestampMember = entityType.Members.Count(p => ObjectContextUtilities.IsConcurrencyTimestamp(p)) == 1;
            this._entityType = entityType;
            this._isDbContext = context is LinqToEntitiesDbContext;
        }

        /// <summary>
        /// Gets the value indicating whether it is legal to include
        /// this entity type for user selection.
        /// </summary>
        /// <remarks>
        /// This method is overridden here to exclude any entity type
        /// that does not have a backing <see cref="System.Data.Entity.Core.Objects.ObjectSet{TEntity}"/>.  
        /// This eliminates derived types because they use their base type's set.
        /// We do this currently because we do not support the generation
        /// of query and CUD methods on derived types.
        /// </remarks>
        public override bool CanBeIncluded
        {
            get
            {
                return this.DefaultObjectSetName != null && base.CanBeIncluded;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this entity has a concurrency
        /// timestamp member.
        /// </summary>
        public bool HasTimestampMember
        {
            get
            {
                return this._hasTimestampMember;
            }
        }

        /// <summary>
        /// Gets the <see cref="EntityType"/> of this entity.
        /// </summary>
        public EntityType EntityType
        {
            get
            {
                return this._entityType;
            }
        }

        /// <summary>
        /// Gets the name of the default object set for entities of this type
        /// </summary>
        public string DefaultObjectSetName
        {
            get
            {
                if (this._defaultObjectSetName == null)
                {
                    this._defaultObjectSetName = this.FindDefaultObjectSetName();
                }
                return this._defaultObjectSetName;
            }
        }

        /// <summary>
        /// Finds the name of the object set for the current type.
        /// </summary>
        /// <returns>The name of the property or null if not found</returns>
        private string FindDefaultObjectSetName()
        {
            PropertyInfo[] propertyInfos = this.BusinessLogicContext.ContextType.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                Type t = propertyInfo.PropertyType;

                if (!t.IsGenericType)
                {
                    continue;
                }

                Type[] genericArguments = t.GetGenericArguments();
                if (genericArguments.Length != 1)
                {
                    continue;
                }

                if (!this.IsContextSetType(t))
                {
                        continue;
                }               

                if (this.ClrType != genericArguments[0])
                {
                    continue;
                }

                return propertyInfo.Name;
            }
            return null;
        }

        /// <summary>
        /// This method finds if the type is a set for the particular context type.
        /// For DbContext, it finds if the type is a DbSet.
        /// For ObjectContext, it finds if it is a ObjectSet;
        /// </summary>
        /// <param name="t">Type to be examined.</param>
        /// <returns><c>true</c> if the type is one of the 2 set types, and <c>false</c> otherwise</returns>
        private bool IsContextSetType(Type t)
        {
            if (this._isDbContext)
            {
                return DbContextUtilities.CompareWithSystemType(t, BusinessLogicClassConstants.DbSetTypeName);
            }
            else
            {
                return t.GetGenericTypeDefinition() == typeof(ObjectSet<>);
            }
        }
    }
}
