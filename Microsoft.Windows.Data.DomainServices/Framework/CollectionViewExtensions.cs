using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceModel.DomainServices.Client;
using System.Windows.Data;

namespace Microsoft.Windows.Data.DomainServices
{
    using Expression = System.Linq.Expressions.Expression;

    /// <summary>
    /// Static extension methods for applying collection view state to a <see cref="QueryBuilder{T}"/>.
    /// </summary>
    public static class CollectionViewExtensions
    {
        #region Methods

        /// <summary>
        /// Orders the query using the group and sort descriptions of the specified <paramref name="collectionView"/>.
        /// </summary>
        /// <typeparam name="TEntity">The generic type of the entity</typeparam>
        /// <param name="query">The query to sort</param>
        /// <param name="collectionView">The view containing the descriptions to sort by</param>
        /// <returns>A query ordered according to the specified <paramref name="collectionView"/></returns>
        public static EntityQuery<TEntity> SortBy<TEntity>(this EntityQuery<TEntity> query, ICollectionView collectionView) where TEntity : Entity
        {
            return CollectionViewExtensions.SortBy(new QueryBuilder<TEntity>(), collectionView).ApplyTo(query);
        }

        /// <summary>
        /// Orders the query using the group and sort descriptions of the specified <paramref name="collectionView"/>.
        /// </summary>
        /// <typeparam name="TEntity">The generic type of the entity</typeparam>
        /// <param name="query">The query to sort</param>
        /// <param name="collectionView">The view containing the descriptions to sort by</param>
        /// <returns>A query ordered according to the specified <paramref name="collectionView"/></returns>
        public static QueryBuilder<TEntity> SortBy<TEntity>(this QueryBuilder<TEntity> query, ICollectionView collectionView) where TEntity : Entity
        {
            return CollectionViewExtensions.SortBy(query, collectionView.GroupDescriptions, collectionView.SortDescriptions);
        }

        /// <summary>
        /// Orders the query using the specified group and sort descriptions.
        /// </summary>
        /// <typeparam name="TEntity">The generic type of the entity</typeparam>
        /// <param name="query">The query to sort</param>
        /// <param name="groupDescriptions">The group descriptions</param>
        /// <param name="sortDescriptions">The sort descriptions</param>
        /// <returns>A query ordered according to the specified descriptions</returns>
        public static QueryBuilder<TEntity> SortBy<TEntity>(
            this QueryBuilder<TEntity> query,
            IEnumerable<GroupDescription> groupDescriptions,
            IEnumerable<SortDescription> sortDescriptions) where TEntity : Entity
        {
            bool isFirst = true;

            // First we'll sort according to the group descriptions
            foreach (PropertyGroupDescription groupDescription in groupDescriptions.OfType<PropertyGroupDescription>().Where(d => !string.IsNullOrEmpty(d.PropertyName)))
            {
                // If we sort by the same property, we need to determine the sort direction
                bool isAscending = true;
                foreach (SortDescription sortDescription in sortDescriptions.Where(d => !string.IsNullOrEmpty(d.PropertyName)))
                {
                    if (groupDescription.PropertyName == sortDescription.PropertyName)
                    {
                        isAscending = (sortDescription.Direction == ListSortDirection.Ascending);
                        break;
                    }
                }

                query = ExpressionUtility.Sort(
                    query,
                    CollectionViewExtensions.GetSortMethodName(isFirst, isAscending),
                    ExpressionUtility.BuildPropertyExpression(typeof(TEntity), groupDescription.PropertyName));

                isFirst = false;
            }

            // Then we'll sort according to the sort descriptions
            foreach (SortDescription sortDescription in sortDescriptions.Where(d => !string.IsNullOrEmpty(d.PropertyName)))
            {
                bool isAscending = (sortDescription.Direction == ListSortDirection.Ascending);

                query = ExpressionUtility.Sort(
                    query,
                    CollectionViewExtensions.GetSortMethodName(isFirst, isAscending),
                    ExpressionUtility.BuildPropertyExpression(typeof(TEntity), sortDescription.PropertyName));

                isFirst = false;
            }

            return query;
        }

        /// <summary>
        /// Returns the method name for sorting according to the given parameters
        /// </summary>
        /// <param name="isFirst">Whether this is the first sort in the sequence</param>
        /// <param name="isAscending">Whether this is an ascending sort</param>
        /// <returns>The method name</returns>
        private static string GetSortMethodName(bool isFirst, bool isAscending)
        {
            if (isFirst)
            {
                if (isAscending)
                {
                    return "OrderBy";
                }
                else
                {
                    return "OrderByDescending";
                }
            }
            else
            {
                if (isAscending)
                {
                    return "ThenBy";
                }
                else
                {
                    return "ThenByDescending";
                }
            }
        }

        /// <summary>
        /// Pages the query using the page size and index of the specified <paramref name="collectionView"/>.
        /// </summary>
        /// <remarks>
        /// The paged query will request the total item count if it is not already known to the specified
        /// <paramref name="collectionView"/>.
        /// </remarks>
        /// <typeparam name="TEntity">The generic type of the entity</typeparam>
        /// <param name="query">The query to page</param>
        /// <param name="collectionView">The view containing the paging information</param>
        /// <returns>A query paged according to the specified <paramref name="collectionView"/></returns>
        public static EntityQuery<TEntity> PageBy<TEntity>(this EntityQuery<TEntity> query, IPagedCollectionView collectionView) where TEntity : Entity
        {
            return CollectionViewExtensions.PageBy(new QueryBuilder<TEntity>(), collectionView).ApplyTo(query);
        }

        /// <summary>
        /// Pages the query using the page size and index of the specified <paramref name="collectionView"/>.
        /// </summary>
        /// <remarks>
        /// The paged query will request the total item count if it is not already known to the specified
        /// <paramref name="collectionView"/>.
        /// </remarks>
        /// <typeparam name="TEntity">The generic type of the entity</typeparam>
        /// <param name="query">The query to page</param>
        /// <param name="collectionView">The view containing the paging information</param>
        /// <returns>A query paged according to the specified <paramref name="collectionView"/></returns>
        public static QueryBuilder<TEntity> PageBy<TEntity>(this QueryBuilder<TEntity> query, IPagedCollectionView collectionView) where TEntity : Entity
        {
            if (collectionView.TotalItemCount == -1)
            {
                query.RequestTotalItemCount = true;
            }
            if (collectionView.PageSize > 0)
            {
                query = query.Skip(collectionView.PageIndex * collectionView.PageSize);
                query = query.Take(collectionView.PageSize);
            }
            return query;
        }

        /// <summary>
        /// Updates the query by calling <see cref="SortBy{TEntity}(EntityQuery{TEntity},ICollectionView)"/> and <see cref="PageBy{TEntity}(EntityQuery{TEntity},IPagedCollectionView)"/> in sequence.
        /// </summary>
        /// <typeparam name="TEntity">The generic type of the entity</typeparam>
        /// <param name="query">The query to update</param>
        /// <param name="collectionView">The collection view to pass to each method</param>
        /// <returns>An updated query</returns>
        public static EntityQuery<TEntity> SortAndPageBy<TEntity>(this EntityQuery<TEntity> query, ICollectionView collectionView) where TEntity : Entity
        {
            return CollectionViewExtensions.SortAndPageBy(new QueryBuilder<TEntity>(), collectionView).ApplyTo(query);
        }

        /// <summary>
        /// Updates the query by calling <see cref="SortBy{TEntity}(QueryBuilder{TEntity},ICollectionView)"/> and <see cref="PageBy{TEntity}(QueryBuilder{TEntity},IPagedCollectionView)"/> in sequence.
        /// </summary>
        /// <typeparam name="TEntity">The generic type of the entity</typeparam>
        /// <param name="query">The query to update</param>
        /// <param name="collectionView">The collection view to pass to each method</param>
        /// <returns>An updated query</returns>
        public static QueryBuilder<TEntity> SortAndPageBy<TEntity>(this QueryBuilder<TEntity> query, ICollectionView collectionView) where TEntity : Entity
        {
            IPagedCollectionView pagedCollectionView = collectionView as IPagedCollectionView;
            if (pagedCollectionView == null)
            {
                throw new ArgumentException(Resources.MustImplementIpcv, "collectionView");
            }

            query = CollectionViewExtensions.SortBy(query, collectionView);
            query = CollectionViewExtensions.PageBy(query, pagedCollectionView);
            return query;
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Static utility for composing Linq expressions and queries
        /// </summary>
        private static class ExpressionUtility
        {
            #region Methods

            /// <summary>
            /// Builds the Linq Expression for the provided propertyPath and type properties
            /// </summary>
            /// <param name="type">Type that exposes the property</param>
            /// <param name="propertyPath">Public property. Can be nested.</param>
            /// <returns>Resulting property expression</returns>
            /// <exception cref="ArgumentException">When property is not found on the specified type.</exception>
            public static Expression BuildPropertyExpression(Type type, string propertyPath)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }
                if (propertyPath == null)
                {
                    throw new ArgumentNullException("propertyPath");
                }

                Expression propertyExpression =
                    ExpressionUtility.GenerateProperty(type, propertyPath, Expression.Parameter(type, string.Empty));

                if (propertyExpression == null)
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.PropertyNotFound,
                        propertyPath,
                        type.Name));
                }

                return propertyExpression;
            }

            /// <summary>
            /// Generates a Linq property expression given a type and a property path
            /// </summary>
            /// <param name="type">Type that contains the property provided</param>
            /// <param name="propertyPath">Property path that can be dotted or not.</param>
            /// <param name="instance">Root expression</param>
            /// <returns>Resulting property expression if path is valid. Null otherwise.</returns>
            private static Expression GenerateProperty(Type type, string propertyPath, Expression instance)
            {
               Expression propertyExpression = instance;
                if (!String.IsNullOrEmpty(propertyPath))
                {
                    string[] propertyNames = propertyPath.Split('.');
                    for (int i = 0; i < propertyNames.Length; i++)
                    {
                        PropertyInfo propertyInfo = type.GetProperty(propertyNames[i]);
                        if (propertyInfo == null)
                        {
                            return null;
                        }

                        propertyExpression = Expression.Property(propertyExpression, propertyInfo);
                        type = propertyInfo.PropertyType;
                    }
                }

                return propertyExpression;
            }

            /// <summary>
            /// Compose an OrderBy, ThenBy, OrderByDescending, or ThenByDescending clause onto an query.
            /// </summary>
            /// <typeparam name="TEntity">The generic type of the entity</typeparam>
            /// <param name="query">The source query</param>
            /// <param name="methodName">The method name to use: OrderBy, ThenBy, OrderByDescending, or ThenByDescending.</param>
            /// <param name="sortExpression">The expression to use for sorting.</param>
            /// <returns>The composed query.</returns>
            public static QueryBuilder<TEntity> Sort<TEntity>(QueryBuilder<TEntity> query, string methodName, Expression sortExpression) where TEntity : Entity
            {
                LambdaExpression lambda = Expression.Lambda(sortExpression, new ParameterExpression[] { Expression.Parameter(typeof(TEntity), string.Empty) });

                MethodInfo method = query.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                MethodInfo genericMethod = method.MakeGenericMethod(sortExpression.Type);

                return (QueryBuilder<TEntity>)genericMethod.Invoke(query, new object[] { lambda });
            }

            #endregion
        }

        #endregion
    }
}