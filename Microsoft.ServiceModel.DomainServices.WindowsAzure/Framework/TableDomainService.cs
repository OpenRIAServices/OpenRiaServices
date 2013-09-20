using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Services.Client;
using System.Linq;
using System.ServiceModel.DomainServices.Server;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure
{
    /// <summary>
    /// <see cref="DomainService"/> for using with Windows Azure Table Storage.
    /// </summary>
    /// <remarks>
    /// This base class will take care of interacting with the storage layer to handle common
    /// <see cref="DomainService"/> tasks like querying, persisting, validation, and concurrency.
    /// </remarks>
    /// <typeparam name="TEntityContext">The <see cref="TableEntityContext"/> to use to interact
    /// with the table storage
    /// </typeparam>
    [TableMetadataProvider]
    public abstract class TableDomainService<TEntityContext> : DomainService where TEntityContext : TableEntityContext, new()
    {
        private const int DefaultEstimatedQueryResultCount = 128;

        private TEntityContext _entityContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDomainService{T}"/>
        /// </summary>
        protected TableDomainService()
        {
        }

        /// <summary>
        /// Gets the partition key to use with this service
        /// </summary>
        /// <remarks>
        /// When this key is set, the service will operate in 'single-key' mode. It will add the key to
        /// each new entity if the key has not already been specified. Also, it will use the key to optimize
        /// each query. Finally, changes will be submitted to the database in a transactional batch.
        /// When this key is <c>null</c>, the service will operate in 'unique-key' mode. It will generate a
        /// unique partition key for each new entity if the key has not already been specified.
        /// Often it makes sense to operate in 'specific-key' mode. In this case entities may be partitioned
        /// into two or more specific groups. To run in this mode, return <c>null</c> for this value and
        /// always set an entity's parition key before adding it to a <see cref="TableEntitySet"/>.
        /// </remarks>
        protected virtual string PartitionKey
        {
            get { return this.GetType().Name; }
        }

        /// <summary>
        /// Gets the <see cref="TableEntityContext"/> used by this <see cref="DomainService"/>
        /// </summary>
        protected TEntityContext EntityContext
        {
            get
            {
                if (this._entityContext == null)
                {
                    this._entityContext = this.CreateEntityContext();
                }
                return this._entityContext;
            }
        }

        /// <summary>
        /// Creates and initializes the context that will be used by this domain service.
        /// </summary>
        /// <returns>The entity context to use</returns>
        protected virtual TEntityContext CreateEntityContext()
        {
            TEntityContext context = new TEntityContext();

            context.PartitionKey = this.PartitionKey;

            // Use batching semantics unless otherwise specified
            context.SaveChangesDefaultOptions = string.IsNullOrEmpty(this.PartitionKey) ? 
                SaveChangesOptions.None : SaveChangesOptions.Batch;

            return context;
        }

        /// <summary>
        /// Performs the operations indicated by the specified <see cref="ChangeSet"/> by invoking
        /// the corresponding domain operations for each.
        /// </summary>
        /// <param name="changeSet">The changeset to submit</param>
        /// <returns>True if the submit was successful, false otherwise.</returns>
        public override bool Submit(ChangeSet changeSet)
        {
            bool baseResult = base.Submit(changeSet);

            // Make sure our ETags are up-to-date
            this.UpdateETags();

            return baseResult;
        }

        /// <summary>
        /// Performs the query operation indicated by the specified <see cref="QueryDescription"/>
        /// and returns the results. If the query returns a singleton, it should still be returned
        /// as an <see cref="IEnumerable"/> containing the single result.
        /// </summary>
        /// <remarks>
        /// This overridden implementation makes sure query operations that are not supported by Table storage are not applied
        /// to the <see cref="IQueryable"/> returned from the underlying query operation. Instead that <see cref="IQueryable"/>
        /// will first be evaluated, and then the unsupported query operations will be run against the result in memory. This
        /// approach allows the client to specify filters and sorts without having to worry which are supported by the underlying
        /// query provider.
        /// </remarks>
        /// <param name="queryDescription">The description of the query to perform.</param>
        /// <param name="validationErrors">Output parameter that will contain any validation errors encountered. If no validation
        /// errors are encountered, this will be set to <c>null</c>.</param>
        /// <param name="totalCount">Returns the total number of results based on the specified query, but without 
        /// any paging applied to it.</param>
        /// <returns>The query results. May be null if there are no query results.</returns>
        public override IEnumerable Query(QueryDescription queryDescription, out IEnumerable<ValidationResult> validationErrors, out int totalCount)
        {
            IQueryable unsupportedQuery;
            IEnumerable enumerableResult = base.Query(
                new QueryDescription(queryDescription.Method, queryDescription.ParameterValues, queryDescription.IncludeTotalCount, QueryComposer.Split(queryDescription.Query, out unsupportedQuery)),
                out validationErrors,
                out totalCount);

            if (unsupportedQuery != null)
            {
                // Compose the query over the results and eagerly enumerate
                enumerableResult = TableDomainService<TEntityContext>.EnumerateQuery(
                    QueryComposer.Compose(enumerableResult.AsQueryable(), unsupportedQuery),
                    TableDomainService<TEntityContext>.DefaultEstimatedQueryResultCount);
            }

            // Make sure our ETags are up-to-date
            this.UpdateETags();

            return enumerableResult;
        }

        /// <summary>
        /// Enumerates the specified enumerable to guarantee eager execution.
        /// </summary>
        /// <param name="queryable">The queryable to enumerate.</param>
        /// <param name="estimatedResultCount">The estimated number of items the enumerable will yield.</param>
        /// <returns>A new enumerable with the results of the enumerated enumerable.</returns>
        private static IEnumerable EnumerateQuery(IQueryable queryable, int estimatedResultCount)
        {
            ArrayList arrayList = new ArrayList(estimatedResultCount);
            foreach (var item in queryable)
            {
                arrayList.Add(item);
            }
            return arrayList.ToArray(queryable.ElementType);
        }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the current <see cref="ChangeSet"/>
        /// have been invoked. This method commits all the changes made to the <see cref="EntityContext"/>.
        /// </summary>
        /// <exception cref="DataServiceRequestException"> will be thrown in the event of concurrency conflicts.
        /// </exception>
        /// <returns><c>true</c> if the <see cref="ChangeSet"/> was persisted successfully, <c>false</c> otherwise</returns>
        protected override bool PersistChangeSet()
        {
            // Concurrency exceptions here will raise a DataServiceRequestException with the
            // status code of 412 (Precondition Failed). Since we can't identify which entity
            // the precondition failed for or add much other value in this scenario, we'll
            // just let the exception flow through.
            DataServiceResponse response = this.EntityContext.SaveChangesWithRetries();

            return response.All(r => r.Error == null);
        }

        /// <summary>
        /// Updates the eTag values for each entity in the <see cref="EntityContext"/>
        /// </summary>
        /// <remarks>
        /// This method will be called after successful <see cref="Submit"/> and <see cref="Query"/>
        /// operations to update the eTags on the entities that are being returned.
        /// </remarks>
        protected virtual void UpdateETags()
        {
            foreach (EntityDescriptor descriptor in this.EntityContext.Entities)
            {
                TableEntity entity = descriptor.Entity as TableEntity;
                if (entity != null)
                {
                    entity.SetETag(descriptor.ETag);
                }
            }
        }
    }
}
