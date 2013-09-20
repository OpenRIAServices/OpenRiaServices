using System;
using System.Data.Linq.Mapping;
using System.Reflection;

namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Subclass of <see cref="BusinessLogicEntity"/> to describe a LinqToSql entity
    /// </summary>
    internal class LinqToSqlEntity : BusinessLogicEntity
    {
        private string _tablePropertyName;
        private bool _hasTimestampMember;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="context">The owning context</param>
        /// <param name="metaType">The metatype of the entity</param>
        public LinqToSqlEntity(LinqToSqlContext context, MetaType metaType)
            : base(context, metaType.Name, metaType.Type)
        {
            System.Diagnostics.Debug.Assert(metaType.IsEntity == true, "MetaType must be for an entity.");
            this._hasTimestampMember = metaType.VersionMember != null;
        }

        /// <summary>
        /// Gets the value indicating whether it is legal to include
        /// this entity type for user selection.
        /// </summary>
        /// <remarks>
        /// This method is overridden here to exclude any entity type
        /// that does not have a backing table property.  This eliminates
        /// derived types because they use their base type's table.
        /// We do this currently because we do not support the generation
        /// of query and CUD methods on derived types.
        /// </remarks>
        public override bool CanBeIncluded
        {
            get
            {
                return this.TablePropertyName != null && base.CanBeIncluded;
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
        /// Gets the name of the property returning the table containing entities of this type
        /// </summary>
        public string TablePropertyName
        {
            get
            {
                if (this._tablePropertyName == null)
                {
                    this._tablePropertyName = this.FindTablePropertyName();
                }
                return this._tablePropertyName;
            }
        }

        /// <summary>
        /// Locates the name of the property returning the table containing entities of this type
        /// </summary>
        /// <returns>The name of the property or null</returns>
        private string FindTablePropertyName()
        {
            PropertyInfo[] propertyInfos = this.BusinessLogicContext.ContextType.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                Type t = propertyInfo.PropertyType;

                // Looking for property returning System.Data.Linq.Table<entityType> where entityType == our MetaType's type
                if (!t.IsGenericType)
                {
                    continue;
                }

                Type[] genericArguments = t.GetGenericArguments();
                if (genericArguments.Length != 1)
                {
                    continue;
                }

                if (this.ClrType != genericArguments[0])
                {
                    continue;
                }

                if (t.GetGenericTypeDefinition() != typeof(System.Data.Linq.Table<>))
                {
                    continue;
                }

                return propertyInfo.Name;
            }
            return null;
        }
    }
}
