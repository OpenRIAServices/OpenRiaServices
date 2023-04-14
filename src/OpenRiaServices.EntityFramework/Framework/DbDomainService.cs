using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

#if EFCORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using DbEntityEntry = Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry;

namespace OpenRiaServices.Server.EntityFrameworkCore
#else
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using OpenRiaServices.Server;

using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

namespace OpenRiaServices.EntityFramework
#endif
{
    /// <summary>
    /// Base class for DomainServices operating on LINQ To Entities DbContext based data models
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    [DbDomainServiceDescriptionProvider]
    public abstract class DbDomainService<TContext> : DomainService
        where TContext : DbContext, new()
    {
        private TContext _dbContext;

        /// <summary>
        /// Protected constructor for the abstract class.
        /// </summary>
        protected DbDomainService()
        {
        }


        /// <summary>
        /// Initialize the domainservice with a specific context.
        /// </summary>
        /// <param name="dbContext">initial value for <see cref="DbContext"/></param>
        protected DbDomainService(TContext dbContext)
        {
            _dbContext = dbContext;
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

#if EFCORE
            // Turn off AutoDetectChanges.
            this.DbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            this.DbContext.ChangeTracker.LazyLoadingEnabled = false;
#else
            ObjectContext objectContext = ((IObjectContextAdapter)this.DbContext).ObjectContext;
            // We turn this off, since our deserializer isn't going to create
            // the EF proxy types anyways. Proxies only really work if the entities
            // are queried on the server.
            objectContext.ContextOptions.ProxyCreationEnabled = false;

            // Turn off DbContext validation.
            this.DbContext.Configuration.ValidateOnSaveEnabled = false;

            // Turn off AutoDetectChanges.
            this.DbContext.Configuration.AutoDetectChangesEnabled = false;

            this.DbContext.Configuration.LazyLoadingEnabled = false;
#endif
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
        protected internal TContext DbContext
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
            return QueryHelper.CountAsync(query, cancellationToken);
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
#if EFCORE
            if (enumerable is IAsyncEnumerable<T> asyncEnumerable)
#else
            if (enumerable is IDbAsyncEnumerable<T> asyncEnumerable)
#endif
            {
                return QueryHelper.EnumerateAsyncEnumerable(asyncEnumerable, estimatedResultCount, cancellationToken);
            }
            else
            {
                return base.EnumerateAsync<T>(enumerable, estimatedResultCount, cancellationToken);
            }
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
        protected virtual bool ResolveConflicts(IEnumerable<DbEntityEntry> conflicts)
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
                Dictionary<DbEntityEntry, ChangeSetEntry> operationConflictMap = new Dictionary<DbEntityEntry, ChangeSetEntry>();
                foreach (DbEntityEntry conflict in ex.Entries)
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

                await SetChangeSetConflictsAsync(operationConflictMap, cancellationToken);

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
        /// <param name="cancellationToken"></param>
        private static async Task SetChangeSetConflictsAsync(Dictionary<DbEntityEntry, ChangeSetEntry> operationConflictMap, CancellationToken cancellationToken)
        {
            foreach (var conflictEntry in operationConflictMap)
            {
                DbEntityEntry stateEntry = conflictEntry.Key;

                if (stateEntry.State == EntityState.Unchanged)
                {
                    continue;
                }

                ChangeSetEntry operationInConflict = conflictEntry.Value;

                // Determine which members are in conflict by comparing original values to the current DB values
                var dbValues = await stateEntry.GetDatabaseValuesAsync(cancellationToken);
                
                PropertyDescriptorCollection propDescriptors = TypeDescriptor.GetProperties(operationInConflict.Entity.GetType());

                // dbValues will be null if the entity has been deleted in the store (i.e. Delete/Delete conflict)
                if (dbValues == null)
                {
                    operationInConflict.IsDeleteConflict = true;
                }
                else
                {
                    operationInConflict.StoreEntity = dbValues.ToObject();

                    // Determine which members are in conflict by comparing original values to the current DB values
                    List<string> membersInConflict = new List<string>();
                    foreach (var property in PropertiesOf(stateEntry.OriginalValues))
                    {
                        if (!object.Equals(stateEntry.OriginalValues[property], dbValues[property]))
                        {
                            string propertyName = NameOf(property);

                            // Excluded properties should be skipped
                            if (propDescriptors[propertyName] is not null)
                                membersInConflict.Add(propertyName);
                        }
                    }
                    operationInConflict.ConflictMembers = membersInConflict;

#if EFCORE
                    static IReadOnlyList<Microsoft.EntityFrameworkCore.Metadata.IProperty> PropertiesOf(PropertyValues pv) => pv.Properties;
                    static string NameOf(Microsoft.EntityFrameworkCore.Metadata.IProperty p) => p.Name;
#else
                    static IEnumerable<string> PropertiesOf(DbPropertyValues pv) => pv.PropertyNames;
                    static string NameOf(string s) => s;
#endif
                }
            }
        }
    }
}
