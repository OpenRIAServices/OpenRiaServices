using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Wcf
{
    /// <summary>
    /// Class encapsulating query deserialization and operation execution, including
    /// result flattening and other processing. Basically this is the bridge between
    /// the service layer and the domain layer.
    /// </summary>
    internal static class QueryProcessor
    {
        // keyed by entity type, returns a bool that indicates whether a query result of that type requires flattening
        private static ConcurrentDictionary<Type, bool> requiresFlatteningByType = new ConcurrentDictionary<Type, bool>();

        public static async ValueTask<QueryResult<TEntity>> ProcessAsync<TEntity>(DomainService domainService, DomainOperationEntry queryOperation, object[] parameters, ServiceQuery serviceQuery)
        {
            DomainServiceDescription domainServiceDescription = DomainServiceDescription.GetDescription(domainService.GetType());

            // deserialize the query if specified
            IQueryable query = null;
            bool includeTotalCount = false;
            if (serviceQuery != null)
            {
                query = GetQueryable<TEntity>(domainServiceDescription, serviceQuery);
                includeTotalCount = serviceQuery.IncludeTotalCount;
            }

            // invoke the query operation
            QueryDescription queryDescription = new QueryDescription(queryOperation, parameters, includeTotalCount, query);
            var res = await domainService.QueryAsync<TEntity>(queryDescription,  domainService.ServiceContext.CancellationToken);

            if (res.HasValidationErrors)
            {
                return new QueryResult<TEntity>(res.ValidationErrors);
            }
            IEnumerable<TEntity> results = res.Result;
            int totalCount = res.TotalCount;

            // Performance optimization: if there are no included associations, we can assume we don't need to flatten.
            if (!QueryProcessor.RequiresFlattening(domainServiceDescription, typeof(TEntity)))
            {
                // if the root entity type doesn't have any included associations
                // return the results immediately, bypassing the flattening operation
                return new QueryResult<TEntity>(results, totalCount);
            }

            List<TEntity> rootResults = null;
            if (!(results is ICollection<TEntity>))
            {
                // Not an ICollection<TEntity>... Copy over the items into a list of root results.
                rootResults = new List<TEntity>();
            }

            // flatten the results
            List<object> includedResults = new List<object>();
            FlattenGraph(results, rootResults, includedResults, new HashSet<object>(), domainServiceDescription);

            return new QueryResult<TEntity>(rootResults ?? results, totalCount)
            {
                IncludedResults = includedResults
            };
        }

        /// <summary>
        /// Traverse the result object graph, flattening into a single list. Note that this
        /// flattening must maintain relative ordering for the top level elements, since the client might 
        /// have passed an order expression.
        /// </summary>
        /// <typeparam name="TEntity">The root entity Type of the query.</typeparam>
        /// <param name="list">The list of entities to add to the results.</param>
        /// <param name="rootResults">The root entities. The value can be <value>null</value> if the list of root results is already known.</param>
        /// <param name="includedResults">The included entities.</param>
        /// <param name="visited">Map used for the lifetime of the flattening to ensure that each entity
        /// is added to the results only once.</param>
        /// <param name="domainServiceDescription">description for the DomainService.</param>
        private static void FlattenGraph<TEntity>(IEnumerable list, List<TEntity> rootResults, List<object> includedResults, HashSet<object> visited, DomainServiceDescription domainServiceDescription)
        {
            if (list == null)
            {
                return;
            }

            // Queue used for breadth-first scan
            Queue<IEnumerable> resultsQueue = new Queue<IEnumerable>();
            resultsQueue.Enqueue(list);

            IList result = rootResults;
            while (resultsQueue.Count > 0)
            {
                foreach (object entity in resultsQueue.Dequeue())
                {
                    if (!visited.Add(entity))
                    {
                        continue;
                    }

                    // If we already know the root results, then we don't need to copy them over to a new list.
                    if (result != null)
                    {
                        result.Add(entity);
                    }

                    // make sure to use the correct entity Type, taking inheritance into account
                    Type entityType = domainServiceDescription.GetSerializationType(entity.GetType());
                    PropertyDescriptorCollection properties = MetaType.GetMetaType(entityType).IncludedAssociations;
                    foreach (PropertyDescriptor pd in properties)
                    {
                        IEnumerable value = null;
                        if (typeof(IEnumerable).IsAssignableFrom(pd.PropertyType))
                        {
                            value = (IEnumerable)pd.GetValue(entity);
                        }
                        else
                        {
                            // singleton association
                            object singleton = pd.GetValue(entity);
                            if (singleton != null)
                            {
                                value = new object[] { singleton };
                            }
                        }

                        if (value != null)
                        {
                            resultsQueue.Enqueue(value);
                        }
                    }
                }

                // From now on, add everything to includedResults.
                result = includedResults;
            }
        }

        private static IQueryable GetQueryable<TEntity>(DomainServiceDescription domainServiceDescription, ServiceQuery query)
        {
            if (query != null && query.QueryParts != null && query.QueryParts.Any())
            {
                IQueryable<TEntity> queryable = Enumerable.Empty<TEntity>().AsQueryable();
                return QueryDeserializer.Deserialize(domainServiceDescription, queryable, query.QueryParts);
            }

            return null;
        }

        /// <summary>
        /// Determines whether a query result of the given type requires flattening.
        /// </summary>
        /// <remarks>
        /// This method exists to support a performance optimization to skip graph flattening
        /// of a query result when we discover the entity has no included associations.  It takes
        /// inheritance into account by checking for included associations on the given type as well
        /// as all types derived from it.
        /// 
        /// This method evaluates only once per type and caches the result.
        /// </remarks>
        /// <param name="domainServiceDescription">The <see cref="DomainServiceDescription"/> to use to examine the entity hierarchy.</param>
        /// <param name="entityType">The entity type to analyze</param>
        /// <returns><c>true</c> if a query result of the given <paramref name="entityType"/> requires flattening.</returns>
        internal static bool RequiresFlattening(DomainServiceDescription domainServiceDescription, Type entityType)
        {
            System.Diagnostics.Debug.Assert(domainServiceDescription != null, "domainServiceDescription cannot be null");
            System.Diagnostics.Debug.Assert(entityType != null, "entityType cannot be null");

            return requiresFlatteningByType.GetOrAdd(entityType, type =>
            {
                return (MetaType.GetMetaType(type).IncludedAssociations.Count > 0) ||
                        QueryProcessor.GetEntityDerivedTypes(domainServiceDescription, type).Any(t => MetaType.GetMetaType(t).IncludedAssociations.Count > 0);
            });
        }

        /// <summary>
        /// Returns the collection of all entity types derived from <paramref name="entityType"/>
        /// </summary>
        /// <remarks>
        /// When entities are shared, because all DomainServices exposing entityType must expose the same least derived entity,
        /// and  DomainServiceDescription always caches all KnownTypes, the DomainServiceDescription.EntityTypes property
        /// exposes the same set of derived types regardless of which DomainServiceDescription we examine.
        /// </remarks>
        /// <param name="domainServiceDescription">The <see cref="DomainServiceDescription"/>.</param>
        /// <param name="entityType">The entity type whose derived types are needed.</param>
        /// <returns>The collection of derived types.  It may be empty.</returns>
        private static IEnumerable<Type> GetEntityDerivedTypes(DomainServiceDescription domainServiceDescription, Type entityType)
        {
            return domainServiceDescription.EntityTypes.Where(et => et != entityType && entityType.IsAssignableFrom(et));
        }
    }
}
