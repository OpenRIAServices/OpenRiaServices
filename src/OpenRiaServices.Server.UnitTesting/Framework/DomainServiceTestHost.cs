using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Server.UnitTesting
{
    /// <summary>
    /// Host for invoking <see cref="DomainService"/> operations from a test environment
    /// </summary>
    /// <typeparam name="TDomainService">The type of <see cref="DomainService"/> to test</typeparam>
    public class DomainServiceTestHost<TDomainService> where TDomainService : DomainService
    {
        internal static IPrincipal DefaultUser = new GenericPrincipal(new GenericIdentity(string.Empty), Array.Empty<string>());

        #region Member Fields

        private IDomainServiceFactory _factory;
        private IServiceProvider _serviceProvider;
        private IPrincipal _user;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHost{TDomainService}"/>
        /// </summary>
        public DomainServiceTestHost()
            : this(DomainService.Factory, new ServiceProviderStub(), DefaultUser)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHost{TDomainService}"/>
        /// </summary>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="user"/> is <c>null</c></exception>
        public DomainServiceTestHost(IPrincipal user)
            : this(DomainService.Factory, new ServiceProviderStub(user), user)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHost{TDomainService}"/> that creates
        /// <see cref="DomainService"/> instances using the specified function
        /// </summary>
        /// <param name="createDomainService">The function to create <see cref="DomainService"/>s with</param>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="createDomainService"/> is <c>null</c></exception>
        public DomainServiceTestHost(Func<TDomainService> createDomainService)
            : this(new DomainServiceFactory(createDomainService), new ServiceProviderStub(), DefaultUser)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHost{TDomainService}"/> that creates
        /// <see cref="DomainService"/> instances using the specified function and uses the
        /// provided <see cref="IPrincipal"/> for authorization.
        /// </summary>
        /// <param name="createDomainService">The function to create <see cref="DomainService"/>s with</param>
        /// <param name="user">The <see cref="IPrincipal"/> to use for authorization</param>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="createDomainService"/> is <c>null</c></exception>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="user"/> is <c>null</c></exception>
        public DomainServiceTestHost(Func<TDomainService> createDomainService, IPrincipal user)
            : this(new DomainServiceFactory(createDomainService), new ServiceProviderStub(user), user)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHost{TDomainService}"/> that creates
        /// <see cref="DomainService"/> instances using the specified function and uses the
        /// provided <see cref="IPrincipal"/> for authorization.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create <see cref="DomainService"/> instances</param>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="serviceProvider"/> is <c>null</c></exception>
        private DomainServiceTestHost(IServiceProvider serviceProvider)
            : this(null, serviceProvider, DefaultUser)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHost{TDomainService}"/> that creates
        /// <see cref="DomainService"/> instances using the specified function and uses the
        /// provided <see cref="IPrincipal"/> for authorization.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create <see cref="DomainService"/> instances</param>
        /// <param name="user">The <see cref="IPrincipal"/> to use for authorization</param>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="serviceProvider"/> is <c>null</c></exception>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="user"/> is <c>null</c></exception>
        private DomainServiceTestHost(IServiceProvider serviceProvider, IPrincipal user)
            : this(null, serviceProvider, user)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHost{TDomainService}"/> with the specified
        /// factory and service provider
        /// </summary>
        /// <param name="factory">The <see cref="IDomainServiceFactory"/> used to create <see cref="DomainService"/> instances</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used in the creation of <see cref="DomainServiceContext"/> instances</param>
        /// <param name="user">The <see cref="IPrincipal"/> used in the creation of <see cref="DomainServiceContext"/> instances</param>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="serviceProvider"/> is <c>null</c></exception>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="user"/> is <c>null</c></exception>
        private DomainServiceTestHost(IDomainServiceFactory factory, IServiceProvider serviceProvider, IPrincipal user)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            this._factory = factory;
            this._serviceProvider = serviceProvider;
            this._user = user;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="IDomainServiceFactory"/> used to create a new <see cref="DomainService"/>
        /// instance for each operation
        /// </summary>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="value"/> is <c>null</c></exception>
        public IDomainServiceFactory Factory
        {
            get
            {
                return this._factory;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                this._factory = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IServiceProvider"/> used in the creation of a new <see cref="DomainServiceContext"/>
        /// instance for each operation
        /// </summary>
        /// <remarks>
        /// The value returned from <see cref="IServiceProvider.GetService(Type)"/> when type is an <see cref="IPrincipal"/>
        /// will be used to authorize <see cref="DomainService"/> operations
        /// </remarks>
        /// <exception cref="ArgumentNullException">is thrown when <paramref name="value"/> is <c>null</c></exception>
        public IServiceProvider ServiceProvider
        {
            get
            {
                return this._serviceProvider;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                this._serviceProvider = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IPrincipal"/> used in the creation of a new <see cref="DomainServiceContext"/>
        /// instance for each operation
        /// </summary>
        public IPrincipal User
        {
            get => _user;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                this._user = value;
            }
        }

        #endregion

        #region Methods

        #region Query

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> asynchronously and returns the results
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <returns>The entities returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TDomainService, Task<IEnumerable<TEntity>>>> queryOperation, CancellationToken ct = default)
            where TEntity : class
        {
            return this.QueryCoreAsync<TEntity>(queryOperation, ct);
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> asynchronously and returns the results
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <returns>The entities returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TDomainService, Task<IQueryable<TEntity>>>> queryOperation, CancellationToken ct = default)
            where TEntity : class
        {
            return this.QueryCoreAsync<TEntity>(queryOperation, ct);
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> asynchronously and returns the results
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <returns>The entities returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(Expression<Func<TDomainService, IQueryable<TEntity>>> queryOperation, CancellationToken ct = default)
            where TEntity : class
        {
            return this.QueryCoreAsync<TEntity>(queryOperation, ct);
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the results
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <returns>The entities returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public IEnumerable<TEntity> Query<TEntity>(Expression<Func<TDomainService, IEnumerable<TEntity>>> queryOperation) where TEntity : class
        {
            return this.QueryCore<TEntity>(queryOperation);
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the result
        /// </summary>
        /// <remarks>
        /// This method should be used for query signatures that do no return a collection
        /// </remarks>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <returns>The entity returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public TEntity QuerySingle<TEntity>(Expression<Func<TDomainService, TEntity>> queryOperation) where TEntity : class
        {
            return this.QueryCore<TEntity>(queryOperation).SingleOrDefault();
        }


        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> asynchronously and returns the result
        /// </summary>
        /// <remarks>
        /// This method should be used for query signatures that do no return a collection
        /// </remarks>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <returns>The entity returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public async Task<TEntity> QuerySingleAsync<TEntity>(Expression<Func<TDomainService, TEntity>> queryOperation, CancellationToken ct = default) where TEntity : class
        {
            return (await this.QueryCoreAsync<TEntity>(queryOperation, ct)).SingleOrDefault();
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> asynchronously and returns the result
        /// </summary>
        /// <remarks>
        /// This method should be used for query signatures that do no return a collection
        /// </remarks>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <returns>The entity returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public async Task<TEntity> QuerySingleAsync<TEntity>(Expression<Func<TDomainService, Task<TEntity>>> queryOperation, CancellationToken ct = default) where TEntity : class
        {
            return (await this.QueryCoreAsync<TEntity>(queryOperation, ct)).SingleOrDefault();
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the results, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The type of entity in the results</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="results">The entities returned from the specified operation</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryQuery<TEntity>(Expression<Func<TDomainService, IEnumerable<TEntity>>> queryOperation, out IEnumerable<TEntity> results, out IList<ValidationResult> validationErrors) where TEntity : class
        {
            return this.TryQueryCore<TEntity>(queryOperation, out results, out validationErrors);
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the result, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should be used for query signatures that do no return a collection
        /// </remarks>
        /// <typeparam name="TEntity">The type of entity in the result</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="result">The entity returned from the specified operation</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryQuerySingle<TEntity>(Expression<Func<TDomainService, TEntity>> queryOperation, out TEntity result, out IList<ValidationResult> validationErrors) where TEntity : class
        {
            IEnumerable<TEntity> results;
            bool success = this.TryQueryCore<TEntity>(queryOperation, out results, out validationErrors);
            result = (results == null) ? null : results.SingleOrDefault();
            return success;
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the result, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should be used for query signatures that do no return a collection
        /// </remarks>
        /// <typeparam name="TEntity">The type of entity in the result</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="result">The entity returned from the specified operation</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryQuerySingle<TEntity>(Expression<Func<TDomainService, Task<TEntity>>> queryOperation, out TEntity result, out IList<ValidationResult> validationErrors) where TEntity : class
        {
            IEnumerable<TEntity> results;
            bool success = this.TryQueryCore<TEntity>(queryOperation, out results, out validationErrors);
            result = (results == null) ? null : results.SingleOrDefault();
            return success;
        }

        #endregion

        #region Insert

        /// <summary>
        /// Invokes the insert operation for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to insert</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public void Insert<TEntity>(TEntity entity) where TEntity : class
        {
            this.SubmitCore(DomainOperation.Insert, /* submitOperation */ null, entity, /* original */ null);
        }

        /// <summary>
        /// Invokes the insert operation for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to insert</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public Task InsertAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class
        {
            return this.SubmitCoreAsync(DomainOperation.Insert, /* submitOperation */ null, entity, /* original */ null, ct);
        }

        /// <summary>
        /// Invokes the insert operation for the specified entity and returns the validation errors
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to insert</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryInsert<TEntity>(TEntity entity, out IList<ValidationResult> validationErrors) where TEntity : class
        {
            ChangeSet changeSet;
            return this.TrySubmitCore(DomainOperation.Insert, /* submitOperation */ null, entity, /* original */ null, out validationErrors, out changeSet);
        }

        /// <summary>
        /// Invokes the insert operation for the specified entity and returns the <see cref="ChangeSet"/>
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to insert</param>
        /// <param name="changeSet">The operation <see cref="ChangeSet"/></param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryInsert<TEntity>(TEntity entity, out ChangeSet changeSet) where TEntity : class
        {
            IList<ValidationResult> validationErrors;
            return this.TrySubmitCore(DomainOperation.Insert, /* submitOperation */ null, entity, /* original */ null, out validationErrors, out changeSet);
        }

        #endregion

        #region Update

        /// <summary>
        /// Invokes the update operation for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to update</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public Task UpdateAsync<TEntity>(TEntity entity, TEntity original = null, CancellationToken ct = default) where TEntity : class
        {
            return this.SubmitCoreAsync<TEntity>(DomainOperation.Update, /* submitOperation */ null, entity, original, ct);
        }

        /// <summary>
        /// Invokes the update operation for the specified entity
        /// </summary>
        /// <remarks>
        /// This method can be used for custom-named Update operations
        /// </remarks>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="updateOperation">The <see cref="Expression"/> identifying the update operation to invoke</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public Task UpdateAsync<TEntity>(Expression<Action<TDomainService>> updateOperation, TEntity original = null, CancellationToken ct = default)
            where TEntity : class
        {
            return this.SubmitCoreAsync<TEntity>(DomainOperation.Update, updateOperation, /* entity */ null, original, ct);
        }

        /// <summary>
        /// Invokes the update operation for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to update</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public void Update<TEntity>(TEntity entity, TEntity original = null) where TEntity : class
        {
            this.SubmitCore<TEntity>(DomainOperation.Update, /* submitOperation */ null, entity, original);
        }

        /// <summary>
        /// Invokes the update operation for the specified entity
        /// </summary>
        /// <remarks>
        /// This method can be used for custom-named Update operations
        /// </remarks>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="updateOperation">The <see cref="Expression"/> identifying the update operation to invoke</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public void Update<TEntity>(Expression<Action<TDomainService>> updateOperation, TEntity original = null) where TEntity : class
        {
            this.SubmitCore<TEntity>(DomainOperation.Update, updateOperation, /* entity */ null, original);
        }

        /// <summary>
        /// Invokes the update operation for the specified entity and returns the validation errors
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to update</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryUpdate<TEntity>(TEntity entity, TEntity original, out IList<ValidationResult> validationErrors) where TEntity : class
        {
            ChangeSet changeSet;
            return this.TrySubmitCore<TEntity>(DomainOperation.Update, /* submitOperation */ null, entity, original, out validationErrors, out changeSet);
        }

        /// <summary>
        /// Invokes the update operation for the specified entity and returns the <see cref="ChangeSet"/>
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to update</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="changeSet">The operation <see cref="ChangeSet"/></param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryUpdate<TEntity>(TEntity entity, TEntity original, out ChangeSet changeSet) where TEntity : class
        {
            IList<ValidationResult> validationErrors;
            return this.TrySubmitCore<TEntity>(DomainOperation.Update, /* submitOperation */ null, entity, original, out validationErrors, out changeSet);
        }

        /// <summary>
        /// Invokes the update operation for the specified entity and returns the validation errors
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should be used for custom-named Update operations
        /// </remarks>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="updateOperation">The <see cref="Expression"/> identifying the update operation to invoke</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryUpdate<TEntity>(Expression<Action<TDomainService>> updateOperation, TEntity original, out IList<ValidationResult> validationErrors) where TEntity : class
        {
            ChangeSet changeSet;
            return this.TrySubmitCore<TEntity>(DomainOperation.Update, updateOperation, /* entity */ null, original, out validationErrors, out changeSet);
        }

        /// <summary>
        /// Invokes the update operation for the specified entity and returns the <see cref="ChangeSet"/>
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should be used for custom-named Update operations
        /// </remarks>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="updateOperation">The <see cref="Expression"/> identifying the update operation to invoke</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="changeSet">The operation <see cref="ChangeSet"/></param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryUpdate<TEntity>(Expression<Action<TDomainService>> updateOperation, TEntity original, out ChangeSet changeSet) where TEntity : class
        {
            IList<ValidationResult> validationErrors;
            return this.TrySubmitCore<TEntity>(DomainOperation.Update, updateOperation, /* entity */ null, original, out validationErrors, out changeSet);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Invokes the delete operation for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to delete</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public Task DeleteAsync<TEntity>(TEntity entity, TEntity original = null, CancellationToken ct = default) where TEntity : class
        {
            return this.SubmitCoreAsync<TEntity>(DomainOperation.Delete, /* submitOperation */ null, entity, original, ct);
        }

        /// <summary>
        /// Invokes the delete operation for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to delete</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public void Delete<TEntity>(TEntity entity, TEntity original = null) where TEntity : class
        {
            this.SubmitCore<TEntity>(DomainOperation.Delete, /* submitOperation */ null, entity, original);
        }

        /// <summary>
        /// Invokes the delete operation for the specified entity and returns the validation errors
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to delete</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryDelete<TEntity>(TEntity entity, TEntity original, out IList<ValidationResult> validationErrors) where TEntity : class
        {
            ChangeSet changeSet;
            return this.TrySubmitCore<TEntity>(DomainOperation.Delete, /* submitOperation */ null, entity, original, out validationErrors, out changeSet);
        }

        /// <summary>
        /// Invokes the delete operation for the specified entity and returns the <see cref="ChangeSet"/>
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to delete</param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="changeSet">The operation <see cref="ChangeSet"/></param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryDelete<TEntity>(TEntity entity, TEntity original, out ChangeSet changeSet) where TEntity : class
        {
            IList<ValidationResult> validationErrors;
            return this.TrySubmitCore<TEntity>(DomainOperation.Delete, /* submitOperation */ null, entity, original, out validationErrors, out changeSet);
        }

        #endregion

        #region Submit(ChangeSet)
        /// <summary>
        /// Invokes all <see cref="ChangeSetEntry"/> in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <remarks>This method is intended to allow testing Submit with multiple entities at once.
        /// To test CUD operations for single entities have a look at <see cref="Insert{TEntity}(TEntity)"/>, 
        /// <see cref="Update{TEntity}(TEntity, TEntity)"/> and <see cref="Delete{TEntity}(TEntity, TEntity)"/>
        /// </remarks>
        /// <param name="changeSet">The <see cref="ChangeSet"/> to execute</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public void Submit(ChangeSet changeSet)
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Submit);
            SubmitChangeSetCore(context, changeSet);
        }

        /// <summary>
        /// Invokes all <see cref="ChangeSetEntry"/> in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <remarks>This method is intended to allow testing Submit with multiple entities at once.
        /// To test CUD operations for single entities have a look at <see cref="Insert{TEntity}(TEntity)"/>, 
        /// <see cref="Update{TEntity}(TEntity, TEntity)"/> and <see cref="Delete{TEntity}(TEntity, TEntity)"/>
        /// </remarks>
        /// <param name="changeSet">The <see cref="ChangeSet"/> to execute</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public Task SubmitAsync(ChangeSet changeSet, CancellationToken ct = default)
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Submit);
            return SubmitChangeSetCoreAsync(context, changeSet, ct);
        }

        /// <summary>
        /// Invokes all <see cref="ChangeSetEntry"/> in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns><c>true</c> if the Submit was performed without any errors; otherwise <c>false</c></returns>
        /// <remarks>This method is intended to allow testing Submit with multiple entities at once.
        /// To test CUD operations for single entities have a look at <see cref="Insert{TEntity}(TEntity)"/>, 
        /// <see cref="Update{TEntity}(TEntity, TEntity)"/> and <see cref="Delete{TEntity}(TEntity, TEntity)"/>
        /// </remarks>
        /// <param name="changeSet">The <see cref="ChangeSet"/> to execute</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public bool TrySubmit(ChangeSet changeSet)
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Submit);
            IList<ValidationResult> validationErrors;
            return TrySubmitChangeSetCore(context, changeSet, out validationErrors);
        }

        /// <summary>
        /// Invokes all <see cref="ChangeSetEntry"/> in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns><c>true</c> if the Submit was performed without any errors; otherwise <c>false</c></returns>
        /// <remarks>This method is intended to allow testing Submit with multiple entities at once.
        /// To test CUD operations for single entities have a look at <see cref="Insert{TEntity}(TEntity)"/>, 
        /// <see cref="Update{TEntity}(TEntity, TEntity)"/> and <see cref="Delete{TEntity}(TEntity, TEntity)"/>
        /// </remarks>
        /// <param name="changeSet">The <see cref="ChangeSet"/> to execute</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        public bool TrySubmit(ChangeSet changeSet, out IList<ValidationResult> validationErrors)
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Submit);
            return TrySubmitChangeSetCore(context, changeSet, out validationErrors);
        }
        #endregion

        #region Invoke

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> asynchronously
        /// </summary>
        /// <param name="invokeOperation"></param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        public Task InvokeAsync(Expression<Action<TDomainService>> invokeOperation, CancellationToken ct = default)
        {
            return this.InvokeCoreAsync<object>(invokeOperation, ct);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> asynchronously
        /// </summary>
        /// <param name="invokeOperation">The <see cref="Expression"/> with <see cref="Task"/> as return type,
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// identifying the operation to invoke</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public Task InvokeAsync(Expression<Func<TDomainService, Task>> invokeOperation, CancellationToken ct = default)
        {
            return this.InvokeCoreAsync<object>(invokeOperation, ct);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> asynchronously and returns the result
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public Task<TResult> InvokeAsync<TResult>(Expression<Func<TDomainService, TResult>> invokeOperation, CancellationToken ct = default)
        {
            return this.InvokeCoreAsync<TResult>(invokeOperation, ct);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> asynchronously and returns the result
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public Task<TResult> InvokeAsync<TResult>(Expression<Func<TDomainService, Task<TResult>>> invokeOperation, CancellationToken ct = default)
        {
            return this.InvokeCoreAsync<TResult>(invokeOperation, ct);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/>
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public void Invoke(Expression<Action<TDomainService>> invokeOperation)
        {
            this.InvokeCore<object>(invokeOperation);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/>
        /// </summary>
        /// <param name="invokeOperation">The <see cref="Expression"/> with <see cref="Task"/> as return type,
        /// identifying the operation to invoke</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public void Invoke(Expression<Func<TDomainService, Task>> invokeOperation)
        {
            this.InvokeCore<object>(invokeOperation);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public TResult Invoke<TResult>(Expression<Func<TDomainService, TResult>> invokeOperation)
        {
            return this.InvokeCore<TResult>(invokeOperation);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> with <see cref="Task{TResult}"/> as return type,
        /// identifying the operation to invoke</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public TResult Invoke<TResult>(Expression<Func<TDomainService, Task<TResult>>> invokeOperation)
        {
            return this.InvokeCore<TResult>(invokeOperation);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns validation errors
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryInvoke(Expression<Action<TDomainService>> invokeOperation, out IList<ValidationResult> validationErrors)
        {
            object result;
            return this.TryInvokeCore<object>(invokeOperation, out result, out validationErrors);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryInvoke(Expression<Func<TDomainService, Task>> invokeOperation, out IList<ValidationResult> validationErrors)
        {
            return this.TryInvokeCore<object>(invokeOperation, out var _, out validationErrors);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="result">The result of the operation</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryInvoke<TResult>(Expression<Func<TDomainService, TResult>> invokeOperation, out TResult result, out IList<ValidationResult> validationErrors)
        {
            return this.TryInvokeCore<TResult>(invokeOperation, out result, out validationErrors);
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <remarks>
        /// This method should not be used to invoke query, insert, update, or delete operations
        /// </remarks>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="result">The result of the operation</param>
        /// <param name="validationErrors">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        public bool TryInvoke<TResult>(Expression<Func<TDomainService, Task<TResult>>> invokeOperation, out TResult result, out IList<ValidationResult> validationErrors)
        {
            return this.TryInvokeCore<TResult>(invokeOperation, out result, out validationErrors);
        }

        #endregion

        #region Implementations

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the results
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <returns>The entities returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        private IEnumerable<TEntity> QueryCore<TEntity>(Expression queryOperation) where TEntity : class
        {
            return QueryCoreAsync<TEntity>(queryOperation, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the results
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to return</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <returns>The entities returned from the specified operation</returns>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        private async Task<IEnumerable<TEntity>> QueryCoreAsync<TEntity>(Expression queryOperation, CancellationToken ct) where TEntity : class
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Query);

            QueryDescription queryDescription = Utility.GetQueryDescription(context, queryOperation);

            var queryResult = await context.DomainService.QueryAsync<TEntity>(queryDescription, ct);

            ErrorUtility.AssertNoValidationErrors(context, queryResult.ValidationErrors);

            IEnumerable entities = queryResult.Result;
            return (entities == null) ? null : entities.Cast<TEntity>();
        }

        /// <summary>
        /// Invokes the specified <paramref name="queryOperation"/> and returns the results, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The type of entity in the results</typeparam>
        /// <param name="queryOperation">The <see cref="Expression"/> identifying the query operation to invoke</param>
        /// <param name="results">The entities returned from the specified operation</param>
        /// <param name="validationResults">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        private bool TryQueryCore<TEntity>(Expression queryOperation, out IEnumerable<TEntity> results, out IList<ValidationResult> validationResults) where TEntity : class
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Query);

            QueryDescription queryDescription = Utility.GetQueryDescription(context, queryOperation);
            IEnumerable<ValidationResult> validationErrors;

            var queryTask = context.DomainService.QueryAsync<TEntity>(queryDescription, CancellationToken.None);
            // TODO: Remove blocking wait
            var queryResult = Utility.SafeGetResult(queryTask);
            IEnumerable entities = queryResult.Result;
            validationErrors = queryResult.ValidationErrors;

            results = (entities == null) ? null : entities.Cast<TEntity>();
            validationResults = (validationErrors == null) ? null : validationErrors.ToList();

            return (validationResults == null) || (validationResults.Count == 0);
        }

        /// <summary>
        /// Invokes an operation according to the specified <paramref name="operationType"/> and entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="operationType">The type of operation to invoke</param>
        /// <param name="submitOperation">
        /// The <see cref="Expression"/> identifying the operation to invoke. This parameter can be <c>null</c>
        /// as long as <paramref name="entity"/> is not.
        /// </param>
        /// <param name="entity">
        /// The entity to pass to the operation. This parameter can be <c>null</c> as long as
        /// <paramref name="submitOperation"/> is not.
        /// </param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        private void SubmitCore<TEntity>(DomainOperation operationType, Expression submitOperation, TEntity entity, TEntity original) where TEntity : class
        {
            SubmitCoreAsync(operationType, submitOperation, entity, original, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Invokes an operation according to the specified <paramref name="operationType"/> and entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="operationType">The type of operation to invoke</param>
        /// <param name="submitOperation">
        /// The <see cref="Expression"/> identifying the operation to invoke. This parameter can be <c>null</c>
        /// as long as <paramref name="entity"/> is not.
        /// </param>
        /// <param name="entity">
        /// The entity to pass to the operation. This parameter can be <c>null</c> as long as
        /// <paramref name="submitOperation"/> is not.
        /// </param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="ct">The <see cref="CancellationToken"/></param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        private Task SubmitCoreAsync<TEntity>(DomainOperation operationType, Expression submitOperation, TEntity entity, TEntity original, CancellationToken ct)
            where TEntity : class
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Submit);

            ChangeSetEntry changeSetEntry;
            if (operationType == DomainOperation.Update)
            {
                if (submitOperation != null)
                {
                    changeSetEntry = Utility.GetCustomUpdateChangeSetEntry(context, submitOperation, original);
                }
                else
                {
                    changeSetEntry = Utility.GetChangeSetEntry(context, entity, original, operationType);
                    changeSetEntry.HasMemberChanges = true;
                }
            }
            else
            {
                changeSetEntry = Utility.GetChangeSetEntry(context, entity, original, operationType);
            }
            ChangeSet changeSet = Utility.CreateChangeSet(changeSetEntry);

            return SubmitChangeSetCoreAsync(context, changeSet, ct);
        }

        /// <summary>
        /// Invokes an operation according to the specified <paramref name="operationType"/> and entity and returns
        /// the validation errors, the change set, and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="operationType">The type of operation to invoke</param>
        /// <param name="submitOperation">
        /// The <see cref="Expression"/> identifying the operation to invoke. This parameter can be <c>null</c>
        /// as long as <paramref name="entity"/> is not.
        /// </param>
        /// <param name="entity">
        /// The entity to pass to the operation. This parameter can be <c>null</c> as long as
        /// <paramref name="submitOperation"/> is not.
        /// </param>
        /// <param name="original">The original version of the entity. This parameter can be <c>null</c>.</param>
        /// <param name="validationResults">The validation errors that occurred</param>
        /// <param name="changeSet">The change set</param>
        private bool TrySubmitCore<TEntity>(DomainOperation operationType, Expression submitOperation, TEntity entity, TEntity original, out IList<ValidationResult> validationResults, out ChangeSet changeSet) where TEntity : class
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Submit);

            ChangeSetEntry changeSetEntry;
            if (operationType == DomainOperation.Update)
            {
                if (submitOperation != null)
                {
                    changeSetEntry = Utility.GetCustomUpdateChangeSetEntry(context, submitOperation, original);
                }
                else
                {
                    changeSetEntry = Utility.GetChangeSetEntry(context, entity, original, operationType);
                    changeSetEntry.HasMemberChanges = true;
                }
            }
            else
            {
                changeSetEntry = Utility.GetChangeSetEntry(context, entity, original, operationType);
            }
            changeSet = Utility.CreateChangeSet(changeSetEntry);

            return TrySubmitChangeSetCore(context, changeSet, out validationResults);
        }

        /// <summary>
        /// Invokes one or several operation according to the specified <paramref name="changeSet"/>
        /// </summary>
        /// <param name="context"><see cref="OperationContext"/> for the current operation</param>
        /// <param name="changeSet">The <see cref="ChangeSet"/> identifying the operations to invoke.</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        private static void SubmitChangeSetCore(OperationContext context, ChangeSet changeSet)
        {
            SubmitChangeSetCoreAsync(context, changeSet, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Invokes one or several operation according to the specified <paramref name="changeSet"/>
        /// </summary>
        /// <param name="context"><see cref="OperationContext"/> for the current operation</param>
        /// <param name="changeSet">The <see cref="ChangeSet"/> identifying the operations to invoke.</param>
        /// <param name="ct">The <see cref="ChangeSet"/> Cancellationtoken.</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any <see cref="ChangeSet"/> errors</exception>
        private static async Task SubmitChangeSetCoreAsync(OperationContext context, ChangeSet changeSet, CancellationToken ct)
        {
            await context.DomainService.SubmitAsync(changeSet, ct);

            ErrorUtility.AssertNoChangeSetErrors(context, changeSet);
        }

        /// <summary>
        /// Invokes one or several operation according to the specified <paramref name="changeSet"/>
        /// </summary>
        /// <returns><c>true</c> if the Submit was performed without any errors; otherwise <c>false</c></returns>
        /// <param name="context"><see cref="OperationContext"/> for the current operation</param>
        /// <param name="changeSet">The <see cref="ChangeSet"/> identifying the operations to invoke.</param>
        /// <param name="validationResults">The validation errors that occurred</param>
        private static bool TrySubmitChangeSetCore(OperationContext context, ChangeSet changeSet, out IList<ValidationResult> validationResults)
        {
            Utility.SafeGetResult(context.DomainService.SubmitAsync(changeSet, CancellationToken.None));

            validationResults = GetValidationResults(changeSet);
            return !changeSet.HasError;
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        private TResult InvokeCore<TResult>(Expression invokeOperation)
        {
            return InvokeCoreAsync<TResult>(invokeOperation, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="ct">The <see cref="CancellationToken"/> Cancellationtoken</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        private async Task<TResult> InvokeCoreAsync<TResult>(Expression invokeOperation, CancellationToken ct)
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Invoke);

            InvokeDescription invokeDescription = Utility.GetInvokeDescription(context, invokeOperation);

            var invokeResult = await context.DomainService.InvokeAsync(invokeDescription, ct);
            ErrorUtility.AssertNoValidationErrors(context, invokeResult.ValidationErrors);
            TResult result = (TResult)invokeResult.Result;

            return result;
        }

        /// <summary>
        /// Invokes the specified <paramref name="invokeOperation"/> and returns the result, the validation errors,
        /// and whether the operation completed successfully
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="invokeOperation">The <see cref="Expression"/> identifying the operation to invoke</param>
        /// <param name="result">The result of the operation</param>
        /// <param name="validationResults">The validation errors that occurred</param>
        /// <returns>Whether the operation completed without error</returns>
        private bool TryInvokeCore<TResult>(Expression invokeOperation, out TResult result, out IList<ValidationResult> validationResults)
        {
            OperationContext context = this.CreateOperationContext(DomainOperationType.Invoke);
            InvokeDescription invokeDescription = Utility.GetInvokeDescription(context, invokeOperation);

            // TODO: Remove blocking wait
            var invokeResult = Utility.SafeGetResult(context.DomainService.InvokeAsync(invokeDescription, CancellationToken.None));
            result = (TResult)invokeResult.Result;
            validationResults = invokeResult.HasValidationErrors ? invokeResult.ValidationErrors.ToList() : null;

            return (!invokeResult.HasValidationErrors);
        }

        #endregion

        /// <summary>
        /// Creates an <see cref="OperationContext"/> for the specified <see cref="DomainOperationType"/>
        /// </summary>
        /// <param name="operationType">The type of operation context to create</param>
        /// <returns>An operation context for the specified type</returns>
        private OperationContext CreateOperationContext(DomainOperationType operationType)
        {
            // TODO: consider whether this implementation should call dispose
            //  Maybe OperationContext would implement IDisposable...

            DomainServiceContext domainServiceContext = new DomainServiceContext(this.ServiceProvider, User, operationType);

            DomainService domainService;
            if (_factory != null)
            {
                domainService = _factory.CreateDomainService(typeof(TDomainService), domainServiceContext);
            }
            else
            {
                domainService = (DomainService)ServiceProvider.GetService(typeof(TDomainService));
                if (domainService == null) { throw new InvalidOperationException($"Could not resolve type ${typeof(TDomainService).FullName}"); }

                domainService.Initialize(domainServiceContext);
            }

            DomainServiceDescription domainServiceDescription =
                        DomainServiceDescription.GetDescription(typeof(TDomainService));
            return new OperationContext(domainServiceContext, domainService, domainServiceDescription);
        }

        /// <summary>
        /// Returns a list of <see cref="ValidationResult"/>s extracted from the specified <see cref="ChangeSet"/>
        /// </summary>
        /// <param name="changeSet">The <see cref="ChangeSet"/> to get the results from</param>
        /// <returns>A list of <see cref="ValidationResult"/>s or <c>null</c></returns>
        private static IList<ValidationResult> GetValidationResults(ChangeSet changeSet)
        {
            if (changeSet.HasError)
            {
                List<ValidationResult> validationErrors = new List<ValidationResult>();
                foreach (ChangeSetEntry changeSetEntry in changeSet.ChangeSetEntries)
                {
                    if (changeSetEntry.ValidationErrors != null)
                    {
                        validationErrors.AddRange(changeSetEntry.ValidationErrors.Select(vri => new ValidationResult(vri.Message, vri.SourceMemberNames)));
                    }
                }
                return validationErrors;
            }
            return null;
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Implementation of the <see cref="IDomainServiceFactory"/> interface that creates
        /// a <see cref="DomainService"/> using a function
        /// </summary>
        private class DomainServiceFactory : IDomainServiceFactory
        {
            private readonly Func<TDomainService> _createDomainService;

            public DomainServiceFactory(Func<TDomainService> createDomainService)
            {
                if (createDomainService == null)
                {
                    throw new ArgumentNullException(nameof(createDomainService));
                }
                this._createDomainService = createDomainService;
            }

            public DomainService CreateDomainService(Type domainServiceType, DomainServiceContext context)
            {
                if (domainServiceType != typeof(TDomainService))
                {
                    throw new InvalidOperationException("Only DomainServices of type '{0}' can be instantiated with this factory.");
                }
                TDomainService domainService = this._createDomainService();
                domainService.Initialize(context);
                return domainService;
            }

            public void ReleaseDomainService(DomainService domainService)
            {
                domainService.Dispose();
            }
        }

        #endregion
    }
}
