using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.WindowsAzure.StorageClient;

namespace OpenRiaServices.WindowsAzure
{
    /// <summary>
    /// Set of <see cref="TableServiceEntity"/>s that can be updated, added to, or removed
    /// from before persisting the changes using the specified <see cref="TableServiceContext"/>.
    /// </summary>
    public abstract class TableEntitySet : IEnumerable
    {
        // This default tells a TableServiceContext to do an unconditional update
        internal const string DefaultETag = "*";

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntitySet"/>
        /// </summary>
        /// <param name="tableServiceContext">The <see cref="TableServiceContext"/> to use</param>
        /// <param name="tableName">The table name to use</param>
        protected TableEntitySet(TableServiceContext tableServiceContext, string tableName)
        {
            if (tableServiceContext == null)
            {
                throw new ArgumentNullException("tableServiceContext");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }

            this.TableServiceContext = tableServiceContext;
            this.TableName = tableName;
        }

        /// <summary>
        /// Gets or sets the partition key to use with this set
        /// </summary>
        /// <remarks>
        /// When this key is set, the entity set will operate in 'single-key' mode. It will add the key to
        /// each new entity if the key has not already been specified. Also, it will use the key to apply an
        /// optimizing filter to the default query.
        /// When this key is <c>null</c>, the entity set will operate in 'unique-key' mode. It will generate a
        /// unique partition key for each new entity if the key has not already been specified.
        /// </remarks>
        public string PartitionKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the context this set uses
        /// </summary>
        public TableServiceContext TableServiceContext
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Table name that this set operates against
        /// </summary>
        public string TableName
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds a new entity to the <see cref="TableServiceContext"/> this entity set uses
        /// </summary>
        /// <remarks>
        /// If either the partition key or row key is not set when the entity is added, 
        /// a default value will be used for that key.
        /// </remarks>
        /// <param name="entity">The entity to add</param>
        public void Add(TableEntity entity)
        {
            // Make sure the TableServiceEntity keys are set
            if (string.IsNullOrEmpty(entity.PartitionKey))
            {
                entity.PartitionKey = this.GetNewEntityPartitionKey();
            }
            if (string.IsNullOrEmpty(entity.RowKey))
            {
                entity.RowKey = this.GetNewEntityRowKey();
            }

            this.EnsureDetached(entity);
            this.TableServiceContext.AddObject(this.TableName, entity);
        }

        /// <summary>
        /// Deletes an existing entity from the <see cref="TableServiceContext"/> this entity set uses
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        public void Delete(TableEntity entity)
        {
            this.EnsureAttached(entity);
            this.TableServiceContext.DeleteObject(entity);
        }

        /// <summary>
        /// Deletes an existing entity from the <see cref="TableServiceContext"/> this entity set uses
        /// </summary>
        /// <remarks>
        /// This delete will check for concurrency conflicts using the specified <paramref name="etag"/>
        /// </remarks>
        /// <param name="entity">The entity to delete</param>
        /// <param name="etag">The etag associated with the entity</param>
        public void Delete(TableEntity entity, string etag)
        {
            this.EnsureAttached(entity, etag);
            this.TableServiceContext.DeleteObject(entity);
        }

        /// <summary>
        /// Updates an existing entity in the <see cref="TableServiceContext"/> this entity set uses
        /// </summary>
        /// <param name="entity">The entity to update</param>
        public void Update(TableEntity entity)
        {
            this.EnsureAttached(entity);
            this.TableServiceContext.UpdateObject(entity);
        }

        /// <summary>
        /// Updates an existing entity in the <see cref="TableServiceContext"/> this entity set uses
        /// </summary>
        /// <remarks>
        /// This update will check for concurrency conflicts using the specified <paramref name="etag"/>
        /// </remarks>
        /// <param name="entity">The entity to update</param>
        /// <param name="etag">The etag associated with the entity</param>
        public void Update(TableEntity entity, string etag)
        {
            this.EnsureAttached(entity, etag);
            this.TableServiceContext.UpdateObject(entity);
        }

        /// <summary>
        /// Ensures the entity is attached
        /// </summary>
        /// <remarks>
        /// The entity will be attached using the default etag and will not check for concurrency conflicts
        /// </remarks>
        /// <param name="entity">The entity to ensure is attached</param>
        protected void EnsureAttached(TableEntity entity)
        {
            this.EnsureAttached(entity, TableEntitySet.DefaultETag);
        }

        /// <summary>
        /// Ensures the entity is attached
        /// </summary>
        /// <param name="entity">The entity to ensure is attached</param>
        /// <param name="etag">The etag associated with the entity</param>
        protected void EnsureAttached(TableEntity entity, string etag)
        {
            EntityDescriptor descriptor = this.TableServiceContext.GetEntityDescriptor(entity);
            if ((descriptor == null) || (descriptor.State == EntityStates.Detached))
            {
                this.TableServiceContext.AttachTo(this.TableName, entity, etag);
            }
        }

        /// <summary>
        /// Ensures the entity is detached
        /// </summary>
        /// <param name="entity">The entity to ensure is detached</param>
        protected void EnsureDetached(TableEntity entity)
        {
            EntityDescriptor descriptor = this.TableServiceContext.GetEntityDescriptor(entity);
            if ((descriptor != null) && (descriptor.State != EntityStates.Detached))
            {
                this.TableServiceContext.Detach(entity);
            }
        }

        /// <summary>
        /// Returns the <see cref="IEnumerator"/> for this entity set
        /// </summary>
        /// <returns>The <see cref="IEnumerator"/> for this entity set</returns>
        protected abstract IEnumerator GetEnumeratorCore();

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumeratorCore();
        }

        /// <summary>
        /// Returns a partition key for a new entity
        /// </summary>
        /// <remarks>
        /// This will only be called for entities where the partition key is not already set
        /// </remarks>
        /// <returns>A new partition key</returns>
        protected virtual string GetNewEntityPartitionKey()
        {
            return this.PartitionKey ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Returns a row key for a new entity
        /// </summary>
        /// <remarks>
        /// This will only be called for entities where the row key is not already set
        /// </remarks>
        /// <returns>A new row key</returns>
        protected virtual string GetNewEntityRowKey()
        {
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Set of entities that can be updated, added to, or removed from before persisting
    /// the changes using the specified <see cref="TableServiceContext"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity in the set</typeparam>
    public class TableEntitySet<TEntity> : TableEntitySet, IQueryable<TEntity> where TEntity : TableEntity
    {
        private IQueryable<TEntity> _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntitySet{T}"/>
        /// </summary>
        /// <param name="tableServiceContext">The <see cref="TableServiceContext"/> to use</param>
        public TableEntitySet(TableServiceContext tableServiceContext)
            : this(tableServiceContext, typeof(TEntity).Name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntitySet{T}"/>
        /// </summary>
        /// <param name="tableServiceContext">The <see cref="TableServiceContext"/> to use</param>
        /// <param name="tableName">The table name to use</param>
        public TableEntitySet(TableServiceContext tableServiceContext, string tableName)
            : base(tableServiceContext, tableName)
        {
        }

        /// <summary>
        /// The query against windows azure storage provided by the <see cref="TableServiceContext"/>
        /// </summary>
        protected IQueryable<TEntity> Query
        {
            get
            {
                if (this._query == null)
                {
                    this._query = this.CreateQuery();
                }
                return this._query;
            }
        }

        /// <summary>
        /// Returns a query against the backing table
        /// </summary>
        /// <remarks>
        /// When <see cref="TableEntitySet.PartitionKey"/> is set, the query will be optimized to load from
        /// that specific partition
        /// </remarks>
        /// <returns>A query against the backing table</returns>
        protected virtual IQueryable<TEntity> CreateQuery()
        {
            if (string.IsNullOrEmpty(this.PartitionKey))
            {
                return this.TableServiceContext.CreateQuery<TEntity>(this.TableName);
            }
            else
            {
                return this.TableServiceContext.CreateQuery<TEntity>(this.TableName)
                    .Where(e => e.PartitionKey == this.PartitionKey);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return this.Query.GetEnumerator();
        }

        /// <summary>
        /// Returns the <see cref="IEnumerator"/> for this entity set
        /// </summary>
        /// <returns>The <see cref="IEnumerator"/> for this entity set</returns>
        protected override IEnumerator GetEnumeratorCore()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Gets the type of the elements that are returned when the expression tree is executed
        /// </summary>
        Type IQueryable.ElementType
        {
            get { return this.Query.ElementType; }
        }

        /// <summary>
        /// Gets the expression tree
        /// </summary>
        Expression IQueryable.Expression
        {
            get { return this.Query.Expression; }
        }

        /// <summary>
        /// Gets the query provider
        /// </summary>
        IQueryProvider IQueryable.Provider
        {
            get { return this.Query.Provider; }
        }
    }
}
