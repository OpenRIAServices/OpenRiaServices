using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Objects;
using System.Linq;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.EntityFramework
{
    /// <summary>
    /// Base class for DomainServices operating on LINQ To Entities data models
    /// </summary>
    /// <typeparam name="TContext">The Type of the LINQ To Entities ObjectContext</typeparam>
    [LinqToEntitiesDomainServiceDescriptionProvider]
    public abstract class LinqToEntitiesDomainService<TContext> : DomainService where TContext : ObjectContext, new()
    {
        private TContext _objectContext;
        private TContext _refreshContext;

        /// <summary>
        /// Protected constructor because this is an abstract class
        /// </summary>
        protected LinqToEntitiesDomainService()
        {
        }

        /// <summary>
        /// Gets the <see cref="ObjectContext"/>
        /// </summary>
        protected internal TContext ObjectContext
        {
            get
            {
                if (this._objectContext == null)
                {
                    this._objectContext = this.CreateObjectContext();
                }
                return this._objectContext;
            }
        }

        /// <summary>
        /// Initializes this <see cref="DomainService"/>. <see cref="DomainService.Initialize"/> must be called 
        /// prior to invoking any operations on the <see cref="DomainService"/> instance.
        /// </summary>
        /// <param name="context">The <see cref="DomainServiceContext"/> for this <see cref="DomainService"/>
        /// instance. Overrides must call the base method.</param>
        public override void Initialize(DomainServiceContext context)
        {
            base.Initialize(context);

            // If we're going to process a query, we want to turn deferred loading
            // off, since the framework will access association members marked
            // with IncludeAttribute and we don't want to cause deferred loads. However,
            // for other operation types, we don't want to interfere.
            if (context.OperationType == DomainOperationType.Query)
            {
                this.ObjectContext.ContextOptions.LazyLoadingEnabled = false;
            }

            // We turn this off, since our deserializer isn't going to create
            // the EF proxy types anyways. Proxies only really work if the entities
            // are queried on the server.
            this.ObjectContext.ContextOptions.ProxyCreationEnabled = false;
        }

        /// <summary>
        /// Gets the <see cref="ObjectContext"/> used by retrieving store values
        /// </summary>
        private ObjectContext RefreshContext
        {
            get
            {
                if (this._refreshContext == null)
                {
                    this._refreshContext = this.CreateObjectContext();
                }
                return this._refreshContext;
            }
        }

        /// <summary>
        /// Creates and returns the <see cref="ObjectContext"/> instance that will
        /// be used by this provider.
        /// </summary>
        /// <returns>The ObjectContext</returns>
        protected virtual TContext CreateObjectContext()
        {
            return new TContext();
        }

        /// <summary>
        /// See <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="disposing">A <see cref="Boolean"/> indicating whether or not the instance is currently disposing.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._objectContext != null)
                {
                    this._objectContext.Dispose();
                }
                if (this._refreshContext != null)
                {
                    this._refreshContext.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the number of rows in an <see cref="IQueryable&lt;T&gt;" />.
        /// </summary>
        /// <typeparam name="T">The element Type of the query.</typeparam>
        /// <param name="query">The query for which the count should be returned.</param>
        /// <returns>The total number of rows.</returns>
        protected override int Count<T>(IQueryable<T> query)
        {
            return query.Count();
        }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the specified changeset
        /// have been invoked. All changes are committed to the ObjectContext, and any resulting optimistic
        /// concurrency errors are processed.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> was persisted successfully, false otherwise.</returns>
        protected override bool PersistChangeSet()
        {
            return this.InvokeSaveChanges(true);
        }

        private bool InvokeSaveChanges(bool retryOnConflict)
        {
            try
            {
                this.ObjectContext.SaveChanges();
            }
            catch (OptimisticConcurrencyException ex)
            {
                // Map the operations that could have caused a conflict to an entity.
                Dictionary<ObjectStateEntry, ChangeSetEntry> operationConflictMap = new Dictionary<ObjectStateEntry, ChangeSetEntry>();
                foreach (ObjectStateEntry conflict in ex.StateEntries)
                {
                    ChangeSetEntry entry = this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => object.ReferenceEquals(p.Entity, conflict.Entity));
                    if (entry == null)
                    {
                        // If we're unable to find the object in our changeset, propagate
                        // the original exception
                        throw;
                    }
                    operationConflictMap.Add(conflict, entry);
                }

                this.SetChangeSetConflicts(operationConflictMap);

                // Call out to any user resolve code and resubmit if all conflicts
                // were resolved
                if (retryOnConflict && this.ResolveConflicts(ex.StateEntries))
                {
                    // clear the conflics from the entries
                    foreach (ChangeSetEntry entry in this.ChangeSet.ChangeSetEntries)
                    {
                        entry.StoreEntity = null;
                        entry.ConflictMembers = null;
                        entry.IsDeleteConflict = false;
                    }

                    // If all conflicts were resolved attempt a resubmit
                    return this.InvokeSaveChanges(/* retryOnConflict */ false);
                }

                // if the conflict wasn't resolved, call the error handler
                this.OnError(new DomainServiceErrorInfo(ex));

                // if there was a conflict but no conflict information was
                // extracted to the individual entries, we need to ensure the
                // error makes it back to the client
                if (!this.ChangeSet.HasError)
                {
                    throw;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the specified changeset
        /// have been invoked. All changes are committed to the ObjectContext.
        /// <remarks>If the submit fails due to concurrency conflicts <see cref="ResolveConflicts"/> will be called.
        /// If <see cref="ResolveConflicts"/> returns true a single resubmit will be attempted.
        /// </remarks>
        /// </summary>
        /// <param name="conflicts">The list of concurrency conflicts that occurred</param>
        /// <returns>Returns <c>true</c> if the <see cref="ChangeSet"/> was persisted successfully, <c>false</c> otherwise.</returns>
        protected virtual bool ResolveConflicts(IEnumerable<ObjectStateEntry> conflicts)
        {
            return false;
        }

        /// <summary>
        /// Updates each entry in the ChangeSet with its corresponding conflict info.
        /// </summary>
        /// <param name="operationConflictMap">Map of conflicts to their corresponding operations entries.</param>
        private void SetChangeSetConflicts(Dictionary<ObjectStateEntry, ChangeSetEntry> operationConflictMap)
        {
            object storeValue;
            EntityKey refreshEntityKey;

            foreach (var conflictEntry in operationConflictMap)
            {
                ObjectStateEntry stateEntry = conflictEntry.Key;

                if (stateEntry.State == EntityState.Unchanged)
                {
                    continue;
                }

                // Note: we cannot call Refresh StoreWins since this will overwrite Current entity and remove the optimistic concurrency ex.
                ChangeSetEntry operationInConflict = conflictEntry.Value;
                refreshEntityKey = this.RefreshContext.CreateEntityKey(stateEntry.EntitySet.Name, stateEntry.Entity);
                this.RefreshContext.TryGetObjectByKey(refreshEntityKey, out storeValue);
                operationInConflict.StoreEntity = storeValue;

                // StoreEntity will be null if the entity has been deleted in the store (i.e. Delete/Delete conflict)
                bool isDeleted = (operationInConflict.StoreEntity == null);
                if (isDeleted)
                {
                    operationInConflict.IsDeleteConflict = true;
                }
                else
                {
                    // Determine which members are in conflict by comparing original values to the current DB values
                    PropertyDescriptorCollection propDescriptors = TypeDescriptor.GetProperties(operationInConflict.Entity.GetType());
                    List<string> membersInConflict = new List<string>();
                    object originalValue;
                    PropertyDescriptor pd;
                    for (int i = 0; i < stateEntry.OriginalValues.FieldCount; i++)
                    {
                        originalValue = stateEntry.OriginalValues.GetValue(i);
                        if (originalValue is DBNull)
                        {
                            originalValue = null;
                        }

                        string propertyName = stateEntry.OriginalValues.GetName(i);
                        pd = propDescriptors[propertyName];
                        if (pd == null)
                        {
                            // This might happen in the case of a private model
                            // member that isn't mapped
                            continue;
                        }

                        if (!object.Equals(originalValue, pd.GetValue(operationInConflict.StoreEntity)))
                        {
                            membersInConflict.Add(pd.Name);
                        }
                    }
                    operationInConflict.ConflictMembers = membersInConflict;
                }
            }
        }
    }
}
