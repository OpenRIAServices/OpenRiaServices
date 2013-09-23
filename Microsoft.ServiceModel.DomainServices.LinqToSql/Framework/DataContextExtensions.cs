using System;
using System.Data.Linq;

namespace OpenRiaServices.DomainServices.LinqToSql
{
    /// <summary>
    /// DataContext extension methods useful to LinqToSqlDomainService authors
    /// </summary>
    public static class DataContextExtensions
    {
        /// <summary>
        /// Extension method used to determine if the specified entity is currently attached
        /// </summary>
        /// <param name="table">The entity table</param>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the entity is currently attached, false otherwise</returns>
        public static bool IsAttached(this ITable table, object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            // Only way currently to determine if an entity is attached
            // is to see if original state is null
            return table.GetOriginalEntityState(entity) != null;
        }
    }
}
