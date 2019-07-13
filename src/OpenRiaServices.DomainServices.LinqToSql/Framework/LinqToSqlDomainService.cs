using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Server;
using ChangeSet = OpenRiaServices.DomainServices.Server.ChangeSet;

namespace OpenRiaServices.DomainServices.LinqToSql
{
    /// <summary>
    /// Base class for DomainServices operating on LINQ To SQL data models
    /// </summary>
    /// <typeparam name="TContext">Type of DomainContext to instantiate the LinqToSqlDomainService with</typeparam>
    [LinqToSqlDomainServiceDescriptionProvider]
    public abstract class LinqToSqlDomainService<TContext> : DomainService where TContext : DataContext, new()
    {
        private TContext _dataContext;

        /// <summary>
        /// Protected constructor
        /// </summary>
        protected LinqToSqlDomainService()
        {
        }

        /// <summary>
        /// Gets the DataContext for this service
        /// </summary>
        /// <value>This property always gets the current DataContext.  If it has not yet been created,
        /// it will create one.
        /// </value>
        protected internal TContext DataContext
        {
            get
            {
                if (this._dataContext == null)
                {
                    this._dataContext = this.CreateDataContext();
                }
                return this._dataContext;
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
                this.DataContext.DeferredLoadingEnabled = false;
            }
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
        /// See <see cref="IDisposable"/>
        /// </summary>
        /// <param name="disposing">A <see cref="Boolean"/> indicating whether or not the instance is being disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && this._dataContext != null)
            {
                this._dataContext.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the specified changeset
        /// have been invoked. All changes are committed to the DataContext.
        /// <remarks>If the submit fails due to concurrency conflicts <see cref="ResolveConflicts"/> will be called.
        /// If <see cref="ResolveConflicts"/> returns true a single resubmit will be attempted.
        /// </remarks>
        /// </summary>
        /// <returns>Returns <c>true</c> if the <see cref="ChangeSet"/> was persisted successfully, <c>false</c> otherwise.</returns>
        protected override async Task<bool> PersistChangeSetAsync()
        {
            return this.InvokeSubmitChanges(true);
        }

        private bool InvokeSubmitChanges(bool retryOnConflict)
        {
            try
            {
                this.DataContext.SubmitChanges(ConflictMode.ContinueOnConflict);
            }
            catch (ChangeConflictException ex)
            {
                // Map conflicts to operation entries
                Dictionary<ObjectChangeConflict, ChangeSetEntry> operationConflictMap = new Dictionary<ObjectChangeConflict, ChangeSetEntry>();
                foreach (ObjectChangeConflict conflict in this.DataContext.ChangeConflicts)
                {
                    ChangeSetEntry entry = this.ChangeSet.ChangeSetEntries.SingleOrDefault(p => p.Entity == conflict.Object);
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
                if (retryOnConflict && this.ResolveConflicts(this.DataContext.ChangeConflicts))
                {
                    // clear the conflics from the entries
                    foreach (ChangeSetEntry entry in this.ChangeSet.ChangeSetEntries)
                    {
                        entry.StoreEntity = null;
                        entry.ConflictMembers = null;
                        entry.IsDeleteConflict = false;
                    }

                    return this.InvokeSubmitChanges(/* retryOnConflict */ false);
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
        /// This method will be called if Submit fails due to optimistic concurrency conflicts.
        /// Override this method to handle optimistic concurrency conflicts. The base implementation
        /// returns false.
        /// </summary>
        /// <param name="conflicts">The collection of conflicts to resolve.</param>
        /// <returns>True if all conflicts were resolved, false otherwise. If true is
        /// returned a resubmit will be attempted.</returns>
        protected virtual bool ResolveConflicts(ChangeConflictCollection conflicts)
        {
            return false;
        }

        /// <summary>
        /// Updates each entry in the ChangeSet with its corresponding conflict info.
        /// </summary>
        /// <param name="operationConflictMap">Map of conflicts to their corresponding operations entries.</param>
        private void SetChangeSetConflicts(Dictionary<ObjectChangeConflict, ChangeSetEntry> operationConflictMap)
        {
            foreach (var conflictEntry in operationConflictMap)
            {
                ObjectChangeConflict occ = conflictEntry.Key;
                ChangeSetEntry operationInConflict = conflictEntry.Value;
                PropertyDescriptorCollection properties = null;

                // only create a StoreEntity if the object in conflict is not deleted in the store
                if (!occ.IsDeleted)
                {
                    // Calling GetOriginalEntityState will create a copy of the original
                    // object for us
                    ITable table = this.DataContext.GetTable(occ.Object.GetType());
                    operationInConflict.StoreEntity = table.GetOriginalEntityState(occ.Object);

                    properties = TypeDescriptor.GetProperties(operationInConflict.StoreEntity);
                    string[] propertyNamesInConflict = new string[occ.MemberConflicts.Count];
                    for (int i = 0; i < occ.MemberConflicts.Count; i++)
                    {
                        MemberChangeConflict mcc = occ.MemberConflicts[i];
                        string propertyName = mcc.Member.Name;

                        // Store the members that are in conflict.
                        propertyNamesInConflict[i] = propertyName;

                        Debug.Assert(properties[propertyName] != null, "Expected to find property.");

                        // if the entity is not deleted in the store, also let the client know what the store state is.
                        properties[propertyName].SetValue(operationInConflict.StoreEntity, mcc.DatabaseValue);
                    }

                    operationInConflict.ConflictMembers = propertyNamesInConflict;
                }
                else
                {
                    operationInConflict.IsDeleteConflict = true;
                }
            }
        }

        /// <summary>
        /// Creates and returns the DataContext instance that will be used by this service.
        /// </summary>
        /// <returns>The DomainContext</returns>
        protected virtual TContext CreateDataContext()
        {
            return new TContext();
        }
    }
}
