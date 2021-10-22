using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.Server;

namespace OpenRiaServices.EntityFrameworkCore
{
    /// <summary>
    /// Base class for DomainServices operating on LINQ To Entities data models
    /// </summary>
    /// <typeparam name="TContext">The Type of the LINQ To Entities ObjectContext</typeparam>
    [LinqToEntitiesDomainServiceDescriptionProvider]
    public abstract class LinqToEntitiesDomainService<TContext> : DomainService where TContext : new()
    {
        /// <summary>
        /// Protected constructor because this is an abstract class
        /// </summary>
        protected LinqToEntitiesDomainService()
        {
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
            return base.EnumerateAsync<T>(enumerable, estimatedResultCount, cancellationToken);
        }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the specified changeset
        /// have been invoked. All changes are committed to the ObjectContext, and any resulting optimistic
        /// concurrency errors are processed.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>True if the <see cref="ChangeSet"/> was persisted successfully, false otherwise.</returns>
        protected override ValueTask<bool> PersistChangeSetAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<bool>(this.InvokeSaveChangesAsync(true, cancellationToken));
        }

        private async Task<bool> InvokeSaveChangesAsync(bool retryOnConflict, CancellationToken cancellationToken)
        {
            try
            {
                await this.SubmitAsync(ChangeSet, cancellationToken); 
            }
            catch (OptimisticConcurrencyException ex)
            {
                // Map the operations that could have caused a conflict to an entity.
                //Dictionary<ObjectStateEntry, ChangeSetEntry> operationConflictMap = new Dictionary<ObjectStateEntry, ChangeSetEntry>();
                //foreach (ObjectStateEntry conflict in ex.StateEntries)
                //{
                //    ChangeSetEntry entry = this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => object.ReferenceEquals(p.Entity, conflict.Entity));
                //    if (entry == null)
                //    {
                //        // If we're unable to find the object in our changeset, propagate
                //        // the original exception
                //        throw;
                //    }
                //    operationConflictMap.Add(conflict, entry);
                //}

                //this.SetChangeSetConflicts(operationConflictMap);

                //// Call out to any user resolve code and resubmit if all conflicts
                //// were resolved
                //if (retryOnConflict && this.ResolveConflicts(ex.StateEntries))
                //{
                //    // clear the conflics from the entries
                //    foreach (ChangeSetEntry entry in this.ChangeSet.ChangeSetEntries)
                //    {
                //        entry.StoreEntity = null;
                //        entry.ConflictMembers = null;
                //        entry.IsDeleteConflict = false;
                //    }

                //    // If all conflicts were resolved attempt a resubmit
                //    return await this.InvokeSaveChangesAsync(/* retryOnConflict */ false, cancellationToken).ConfigureAwait(false);
                //}

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
    }
}
