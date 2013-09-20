using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure
{
    /// <summary>
    /// <see cref="TableServiceContext"/> that simplifies the task of creating entity
    /// sets for each entity and its backing table.
    /// </summary>
    public abstract class TableEntityContext : TableServiceContext
    {
        private readonly CloudTableClient _tableClient;
        private readonly IDictionary<Type, TableEntitySet> _entitySets = new Dictionary<Type, TableEntitySet>();
        private readonly IDictionary<string, Type> _tablesToTypes = new Dictionary<string, Type>();
        private string _partitionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityContext"/> for testing
        /// </summary>
        /// <param name="baseAddress">The base context address</param>
        /// <param name="credentials">The storage credentials</param>
        internal TableEntityContext(string baseAddress, StorageCredentials credentials)
            : base(baseAddress, credentials)
        {
            this.ResolveType = this.ResolveEntityTypePrivate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityContext"/> using the specified
        /// connection string
        /// </summary>
        /// <param name="connectionString">The table storage connection string</param>
        protected TableEntityContext(string connectionString) :
            this(CloudStorageAccount.Parse(connectionString))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableEntityContext"/> using the spcified
        /// account
        /// </summary>
        /// <param name="account">The cloud storage account to connect to</param>
        protected TableEntityContext(CloudStorageAccount account)
            : base(account.TableEndpoint.ToString(), account.Credentials)
        {
            this._tableClient = account.CreateCloudTableClient();
            this.ResolveType = this.ResolveEntityTypePrivate;
        }

        /// <summary>
        /// Gets or sets the partition key to use with each <see cref="TableEntitySet"/> this context contains
        /// </summary>
        public string PartitionKey
        {
            get
            {
                return this._partitionKey;
            }

            set
            {
                if (this._partitionKey != value)
                {
                    this._partitionKey = value;
                    foreach (TableEntitySet set in this._entitySets.Values)
                    {
                        set.PartitionKey = value;
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the entity <see cref="Type"/> for the specified table name
        /// </summary>
        /// <param name="fullTableName">The full table name in the form of 'AccountName.TableName'</param>
        /// <returns>The type for the specified table name</returns>
        private Type ResolveEntityTypePrivate(string fullTableName)
        {
            if (!this._tablesToTypes.ContainsKey(fullTableName))
            {
                // Transform the full name to the table name by stripping off all but the last bit
                string tableName = fullTableName;
                int index = tableName.LastIndexOf('.');
                if (index >= 0)
                {
                    tableName = tableName.Substring(index + 1);
                }
                this._tablesToTypes[fullTableName] = this.ResolveEntityType(tableName);
            }
            return this._tablesToTypes[fullTableName];
        }

        /// <summary>
        /// Resolves the entity type from the <paramref name="tableName"/>
        /// </summary>
        /// <param name="tableName">The table to get the entity type for</param>
        /// <returns>The type for the specified table name</returns>
        protected virtual Type ResolveEntityType(string tableName)
        {
            return this._tablesToTypes[tableName];
        }

        /// <summary>
        /// Gets the <see cref="TableEntitySet{TEntity}"/> for the specified type
        /// </summary>
        /// <remarks>
        /// This method will create the set using <see cref="CreateEntitySet{T}"/> if necessary.
        /// </remarks>
        /// <typeparam name="TEntity">The type of the <see cref="TableEntitySet{T}"/> to get</typeparam>
        /// <returns>A <see cref="TableEntitySet{T}"/> for the specified type</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the table for the
        /// <see cref="TableEntitySet{T}"/> does not exist and cannot be created.
        /// </exception>
        protected TableEntitySet<TEntity> GetEntitySet<TEntity>() where TEntity : TableEntity
        {
            if (!this._entitySets.ContainsKey(typeof(TEntity)))
            {
                this.SetEntitySet<TEntity>(this.CreateEntitySet<TEntity>());
            }
            return (TableEntitySet<TEntity>)this._entitySets[typeof(TEntity)];
        }

        /// <summary>
        /// Sets the <see cref="TableEntitySet{T}"/> to use for the specified entity type
        /// </summary>
        /// <typeparam name="TEntity">The type the <see cref="TableEntitySet{T}"/> will be used for</typeparam>
        /// <param name="entitySet">The <see cref="TableEntitySet{T}"/> to use</param>
        /// <exception cref="InvalidOperationException"> is thrown if the table for the
        /// <see cref="TableEntitySet{T}"/> does not exist and cannot be created.
        /// </exception>
        protected void SetEntitySet<TEntity>(TableEntitySet<TEntity> entitySet) where TEntity : TableEntity
        {
            this.EnsureTableExists(entitySet.TableName);
            this._entitySets[typeof(TEntity)] = entitySet;
            this._tablesToTypes[entitySet.TableName] = typeof(TEntity);
        }

        /// <summary>
        /// Creates the <see cref="TableEntitySet{T}"/> for the specified type
        /// </summary>
        /// <typeparam name="TEntity">The type of the <see cref="TableEntitySet{T}"/> to create</typeparam>
        /// <returns>A new instance of the <see cref="TableEntitySet{T}"/> for the specified type</returns>
        protected virtual TableEntitySet<TEntity> CreateEntitySet<TEntity>() where TEntity : TableEntity
        {
            return new TableEntitySet<TEntity>(this) { PartitionKey = this.PartitionKey };
        }

        /// <summary>
        /// Ensures the <paramref name="tableName"/> refers to a valid table and creates the
        /// table if it does not already exist.
        /// </summary>
        /// <remarks>
        /// To optimize performance in applications where the tables are guaranteed to exist,
        /// this method should be overridden and do nothing.
        /// </remarks>
        /// <param name="tableName">The table name to ensure exists</param>
        /// <exception cref="InvalidOperationException"> is thrown if the table does not
        /// exist and cannot be created.
        /// </exception>
        protected virtual void EnsureTableExists(string tableName)
        {
            this._tableClient.CreateTableIfNotExist(tableName);
        }
    }
}
