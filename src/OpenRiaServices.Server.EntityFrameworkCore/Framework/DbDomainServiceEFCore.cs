using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.Server;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace OpenRiaServices.EntityFrameworkCore
{
    /// <summary>
    /// Base class for DomainServices operating on LINQ To Entities DbContext based data models
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    [DbDomainServiceEFCoreDescriptionProvider]
    public abstract class DbDomainServiceEFCore<TContext> : DomainService
        where TContext : DbContext, new()
    {
        private TContext _dbContext;

        /// <summary>
        /// Protected constructor for the abstract class.
        /// </summary>
        protected DbDomainServiceEFCore()
        {
        }

        /// <summary>
        /// Initializes the <see cref="DomainService"/>. <see cref="DomainService.Initialize"/> must be called 
        /// prior to invoking any operations on the <see cref="DomainService"/> instance.
        /// </summary>
        /// <param name="context">The <see cref="DomainServiceContext"/> for this <see cref="DomainService"/>
        /// instance. Overrides must call the base method.</param>
        public override void Initialize(DomainServiceContext context)
        {
            base.Initialize(context);
        }

        /// <summary>
        /// Returns the DbContext object.
        /// </summary>
        /// <returns>The created DbContext object.</returns>
        protected virtual TContext CreateDbContext()
        {
            return new TContext();
        }
        
        /// <summary>
        /// Gets the <see cref="DbContext"/>
        /// </summary>
        protected TContext DbContext
        {
            get
            {
                if (this._dbContext == null)
                {
                    this._dbContext = this.CreateDbContext();
                }
                return this._dbContext;
            }
        }

        /// <summary>
        /// Gets the number of rows in an <see cref="IQueryable&lt;T&gt;" />.
        /// </summary>
        /// <typeparam name="T">The element Type of the query.</typeparam>
        /// <param name="query">The query for which the count should be returned.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>The total number of rows.</returns>
        protected override ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            return QueryHelperEFCore.CountAsync(query, cancellationToken);
        }

        /// <summary>
        /// Enumerates the specified enumerable to guarantee eager execution. 
        /// If possible code similar to <c>ToListAsync</c> is used, but with optimizations to start with a larger initial capacity
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="enumerable">The enumerable to enumerate.</param>
        /// <param name="estimatedResultCount">The estimated number of items the enumerable will yield.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>A new enumerable with the results of the enumerated enumerable.</returns>
        protected override ValueTask<IReadOnlyCollection<T>> EnumerateAsync<T>(IEnumerable enumerable, int estimatedResultCount, CancellationToken cancellationToken)
        {
            return base.EnumerateAsync<T>(enumerable, estimatedResultCount, cancellationToken);
        }


        /// <summary>
        /// This method is called to finalize changes after all the operations in the specified changeset
        /// have been invoked. All changes are committed to the DbContext, and any resulting optimistic
        /// concurrency errors are processed.
        /// </summary>
        /// <returns><c>True</c> if the <see cref="ChangeSet"/> was persisted successfully, <c>false</c> otherwise.</returns>
        protected override ValueTask<bool> PersistChangeSetAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<bool>(this.InvokeSaveChangesAsync(true, cancellationToken));
        }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the specified changeset
        /// have been invoked. All changes are committed to the DbContext.
        /// <remarks>If the submit fails due to concurrency conflicts <see cref="ResolveConflicts"/> will be called.
        /// If <see cref="ResolveConflicts"/> returns true a single resubmit will be attempted.
        /// </remarks>
        /// </summary>
        /// <param name="conflicts">The list of concurrency conflicts that occurred</param>
        /// <returns>Returns <c>true</c> if the <see cref="ChangeSet"/> was persisted successfully, <c>false</c> otherwise.</returns>
        protected virtual bool ResolveConflicts(IEnumerable<EntityEntry> conflicts)
        {
            return false;
        }

        /// <summary>
        /// See <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="disposing">A <see cref="Boolean"/> indicating whether or not the instance is currently disposing.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.DbContext != null)
                {
                    this.DbContext.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Called by PersistChangeSet method to save the changes to the database.
        /// </summary>
        /// <param name="retryOnConflict">Flag indicating whether to retry after resolving conflicts.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><c>true</c> if saved successfully and <c>false</c> otherwise.</returns>
        private async Task<bool> InvokeSaveChangesAsync(bool retryOnConflict, CancellationToken cancellationToken)
        {
            try
            {
                await this.DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Map the operations that could have caused a conflict to an entity.
                Dictionary<EntityEntry, ChangeSetEntry> operationConflictMap = new Dictionary<EntityEntry, ChangeSetEntry>();
                foreach (EntityEntry conflict in ex.Entries)
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
                if (retryOnConflict && this.ResolveConflicts(ex.Entries))
                {
                    // clear the conflics from the entries
                    foreach (ChangeSetEntry entry in this.ChangeSet.ChangeSetEntries)
                    {
                        entry.StoreEntity = null;
                        entry.ConflictMembers = null;
                        entry.IsDeleteConflict = false;
                    }

                    // If all conflicts were resolved attempt a resubmit
                    return await this.InvokeSaveChangesAsync(/* retryOnConflict */ false, cancellationToken).ConfigureAwait(false);
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
        /// Updates each entry in the ChangeSet with its corresponding conflict info.
        /// </summary>
        /// <param name="operationConflictMap">Map of conflicts to their corresponding operations entries.</param>
        private void SetChangeSetConflicts(Dictionary<EntityEntry, ChangeSetEntry> operationConflictMap)
        {
            foreach (var conflictEntry in operationConflictMap)
            {
                EntityEntry stateEntry = conflictEntry.Key;                                
                
                if (stateEntry.State == EntityState.Unchanged)
                {
                    continue;
                }

                // Note: we cannot call Refresh StoreWins since this will overwrite Current entity and remove the optimistic concurrency ex.
                ChangeSetEntry operationInConflict = conflictEntry.Value;

                // TODO: Look into DbDomainService.SetChangeSetConflicts it looks quite different and contains other logic such as 
                // loading store entity
                // It might be possible to use "NoTracking" load to find database version
                // - this would include getting keys of entity (using stateEntry.Metadata.GetKeys ? )
                // - then creating an expression comparing an X.Key1 = A && X.Key2 == 2 and so on
                // - and doing a NoTracking load (some if it woud need to be in a generic method for eas of use)
                // another approach would be to create an instance of the same type as X and use 
                //  valeus obtained from stateEntry.GetDatabaseValues() to set it's values
                // throw new NotImplementedException();

                // Determine which members are in conflict by comparing original values to the current DB values
                //                            // TODO: Populate store entity conflictEntry.Value.StoreEntity
                // TODO: make async loading of tate
                var dbValues = stateEntry.GetDatabaseValues();
                operationInConflict.StoreEntity = dbValues?.ToObject();
                operationInConflict.IsDeleteConflict = operationInConflict.StoreEntity == null;

            //    PropertyDescriptorCollection propDescriptors = TypeDescriptor.GetProperties(operationInConflict.Entity.GetType());
                List<string> membersInConflict = new List<string>();
                object originalValue;
                //PropertyDescriptor pd;
                foreach (var prop in stateEntry.OriginalValues.Properties)
                {
                    originalValue = stateEntry.OriginalValues[prop.Name];
                    if (originalValue is DBNull)
                    {
                        originalValue = null;
                    }

                    string propertyName = prop.Name;
                    //pd = propDescriptors[propertyName];
                    //if (pd == null)
                    //{
                    //    // This might happen in the case of a private model
                    //    // member that isn't mapped
                    //    continue;
                    //}

                    if (!object.Equals(originalValue, dbValues[prop.Name]))
                    {
                        membersInConflict.Add(prop.Name);
                    }
                }
                operationInConflict.ConflictMembers = membersInConflict;
            }
        }
    }
}
