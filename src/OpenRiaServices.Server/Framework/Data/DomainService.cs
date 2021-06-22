using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using DataAnnotationsResources = OpenRiaServices.Server.Resource;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Base class for all <see cref="DomainService"/>s.
    /// </summary>
    public abstract class DomainService : IDisposable
    {
        #region Fields

        private const int DefaultEstimatedQueryResultCount = 128;
        internal const int TotalCountUndefined = -1;
        private const int TotalCountEqualsResultSetCount = -2;

        private static IDomainServiceFactory s_domainServiceFactory;

        private ChangeSet _changeSet;
        private DomainServiceContext _serviceContext;
        private DomainServiceDescription _serviceDescription;
        private ValidationContext _validationContext;
        #endregion // Fields

        /// <summary>
        /// Protected constructor
        /// </summary>
        protected DomainService()
        {
        }

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="IDomainServiceFactory"/> used to create new <see cref="DomainService"/> instances.
        /// </summary>
        public static IDomainServiceFactory Factory
        {
            get
            {
                if (DomainService.s_domainServiceFactory == null)
                {
                    DomainService.s_domainServiceFactory = new DefaultDomainServiceFactory();
                }

                return DomainService.s_domainServiceFactory;
            }
            set
            {
                DomainService.s_domainServiceFactory = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="DomainServiceDescription"/> for this <see cref="DomainService"/>.
        /// </summary>
        protected DomainServiceDescription ServiceDescription
        {
            get
            {
                if (this._serviceDescription == null)
                {
                    this._serviceDescription = DomainServiceDescription.GetDescription(this.GetType());
                }
                return this._serviceDescription;
            }
        }

        /// <summary>
        /// Gets the active <see cref="DomainServiceContext"/> for this <see cref="DomainService"/>.
        /// </summary>
        public DomainServiceContext ServiceContext
        {
            get
            {
                this.EnsureInitialized();
                return this._serviceContext;
            }
        }

        /// <summary>
        /// Gets or sets the optional <see cref="ValidationContext"/> to use
        /// for all validation operations invoked by the <see cref="DomainService"/>.
        /// </summary>
        /// <value>
        /// This value may be set by the developer at any time to be used as the backing
        /// <see cref="IServiceProvider"/> and source of
        /// <see cref="System.ComponentModel.DataAnnotations.ValidationContext.Items"/>,
        /// making these services and items available to each
        /// <see cref="ValidationAttribute"/> involved in validation.
        /// </value>
        protected ValidationContext ValidationContext
        {
            get { return this._validationContext; }
            set { this._validationContext = value; }
        }

        /// <summary>
        /// Gets or sets the optional template <see cref="AuthorizationContext"/> to use
        /// for <see cref="IsAuthorized"/>.
        /// </summary>
        /// <value>
        /// This value may be set by the developer at any time to serve as the template
        /// for authorization of each <see cref="DomainOperationEntry"/>.  
        /// The <see cref="Initialize"/> method is the preferred
        /// place to set this value.  The recommended construction pattern is to
        /// specify <see cref="ServiceContext"/> as the template's <see cref="IServiceProvider"/>.
        /// <para>
        /// If this value is not set by the user, a default one will be
        /// created following that pattern.
        /// </para>
        /// <para>
        /// This property is intended to allow a developer to provide additional state information
        /// or services in the <see cref="AuthorizationContext"/> that can be used by the
        /// <see cref="AuthorizationAttribute.IsAuthorized"/> implementation logic for all
        /// <see cref="AuthorizationAttribute"/> subclasses.
        /// </para>
        /// <para>
        /// This optional template value is not passed directly to <see cref="IsAuthorized"/>
        /// but instead is used as the source from which to clone the actual <see cref="AuthorizationContext"/>.
        /// The template will be used as the parent <see cref="System.IServiceProvider"/>.
        /// </para>
        /// <para>Because <see cref="AuthorizationContext"/> implements <see cref="IDisposable"/>, the
        /// value set in this property must be disposed explicitly by the developer.
        /// </para>
        /// </value>
        protected AuthorizationContext AuthorizationContext { get; set; }

        /// <summary>
        /// Requests authorization for the given <paramref name="domainOperationEntry"/>.
        /// </summary>
        /// <param name="domainOperationEntry">The <see cref="DomainOperationEntry"/> to authorize.</param>
        /// <param name="entity">Optional entity instance to authorize.  
        /// A <c>null</c> is acceptable for queries or when determining whether an operation can be performed
        /// outside the context of a submit.  During a submit or invoke, however, if an entity instance
        /// is available, this value will not be <c>null</c>.
        /// </param>
        /// <returns>The results of authorization.  <see cref="AuthorizationResult.Allowed"/> indicates the
        /// authorization request is allowed.  Any other value indicates it was denied.
        /// </returns>
        public AuthorizationResult IsAuthorized(DomainOperationEntry domainOperationEntry, object entity)
        {
            if (domainOperationEntry == null)
            {
                throw new ArgumentNullException(nameof(domainOperationEntry));
            }

            // A null entity is not permitted in a Submit.
            // Queries are always null.
            // Invokes may be null.
            // Metadata requests are always null.
            if (entity == null && this.ServiceContext.OperationType == DomainOperationType.Submit)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Quick return if there is no authorization to perform
            if (!domainOperationEntry.RequiresAuthorization)
            {
                return AuthorizationResult.Allowed;
            }

            // We provide an "Operation" parameter which corresponds to the name provided by the developer.
            // The "OperationType" parameter is a computed property based on the DomainOperation enum.
            string operation = domainOperationEntry.Name;
            string operationType = domainOperationEntry.OperationType;

            // Formulate an AuthorizationContext from the optional template provided by the user
            AuthorizationContext contextTemplate = this.AuthorizationContext;
            AuthorizationResult result = null;

            // If the developer specified a template, we will clone from it and use it as the IServiceProvider.
            // If the user did not, we create a new instance and use the ServiceContext as the IServiceProvider.
            // Note: AurhorizationContext is IDisposable for its service container.
            using (AuthorizationContext context = ((contextTemplate == null)
                ? new AuthorizationContext(entity, operation, operationType, this.ServiceContext, /*items*/ null)
                : new AuthorizationContext(entity, operation, operationType, contextTemplate)))
            {
                // By convention, we pass the entity type through the dictionary.  Note that Invoke may be null here.
                context.Items[typeof(Type)] = domainOperationEntry.AssociatedType;

                // The principal is retrieved through the DomainServiceContext as a service
                IPrincipal principal = this.ServiceContext != null ? this.ServiceContext.User : null;

                // Null principal is denied before going any further -- it is contractually required for the
                // authorization attributes.
                // In this case, we deny the operation with the standard default message.
                if (principal == null)
                {
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DataAnnotationsResources.AuthorizationAttribute_Default_Message, operation);
                    return new AuthorizationResult(errorMessage);
                }

                // Evaluate both type and method level attributes.
                IEnumerable<AuthorizationAttribute> typeLevelAttributes = this.ServiceDescription.Attributes.OfType<AuthorizationAttribute>().ToArray();
                IEnumerable<AuthorizationAttribute> methodLevelAttributes = domainOperationEntry.Attributes.OfType<AuthorizationAttribute>().ToArray();
                result = DomainService.EvaluateAuthorization(typeLevelAttributes, principal, context);
                if (result == AuthorizationResult.Allowed)
                {
                    result = DomainService.EvaluateAuthorization(methodLevelAttributes, principal, context);
                }
            }
            return result;
        }

        /// <summary>
        /// Dispose this <see cref="DomainService"/>.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the current <see cref="ChangeSet"/>. Returns null if no change operations are being performed.
        /// </summary>
        protected ChangeSet ChangeSet
        {
            get
            {
                return this._changeSet;
            }
        }

        #endregion // Properties

        /// <summary>
        /// Initializes this <see cref="DomainService"/>. <see cref="DomainService.Initialize"/> must be called 
        /// prior to invoking any operations on the <see cref="DomainService"/> instance.
        /// </summary>
        /// <param name="context">The <see cref="DomainServiceContext"/> for this <see cref="DomainService"/>
        /// instance. Overrides must call the base method.</param>
        public virtual void Initialize(DomainServiceContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (this._serviceContext != null)
            {
                throw new InvalidOperationException(Resource.DomainService_AlreadyInitialized);
            }

            this._serviceContext = context;
        }

        /// <summary>
        /// Performs the query operation indicated by the specified <see cref="QueryDescription"/>
        /// and returns the results. If the query returns a singleton, it should still be returned
        /// as an <see cref="IEnumerable"/> containing the single result.
        /// </summary>
        /// <param name="queryDescription">The description of the query to perform.
        /// any paging applied to it.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>The query results. May be null if there are no query results.</returns>
        public async virtual ValueTask<ServiceQueryResult<T>> QueryAsync<T>(QueryDescription queryDescription, CancellationToken cancellationToken)
        {
            IEnumerable enumerableResult = null;
            IReadOnlyCollection<T> enumeratedResult = null;
            List<ValidationResult> validationErrorList = null;
            int totalCount;

            try
            {
                if (queryDescription == null)
                {
                    throw new ArgumentNullException(nameof(queryDescription));
                }

                this.EnsureInitialized();
                this.CheckOperationType(DomainOperationType.Query);

                object[] parameters = queryDescription.ParameterValues;

                // Authentication check - will throw if unauthorized
                if (!this.ValidateMethodCall(queryDescription.Method, parameters, out List<ValidationResult> errors))
                {
                    validationErrorList = new List<ValidationResult>();
                    foreach (ValidationResult error in errors)
                    {
                        validationErrorList.Add(new ValidationResult(error.ErrorMessage, error.MemberNames));
                    }

                    return new ServiceQueryResult<T>(validationErrorList);
                }

                object result = null;

                this.ServiceContext.Operation = queryDescription.Method;
                try
                {
                    try
                    {
                        result = await queryDescription.Method.InvokeAsync(this, parameters, out totalCount, this.ServiceContext.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = ExceptionHandlingUtility.GetUnwrappedException(tie);
                        if (e is ValidationException)
                        {
                            throw e;
                        }

                        throw;
                    }
                    catch (AggregateException ae)
                    {
                        Exception e = ExceptionHandlingUtility.GetUnwrappedException(ae);
                        if (e is ValidationException)
                        {
                            throw e;
                        }

                        throw;
                    }
                }
                catch (ValidationException vex)
                {
                    if (validationErrorList == null)
                    {
                        validationErrorList = new List<ValidationResult>();
                    }

                    validationErrorList.Add(new ValidationResult(vex.ValidationResult.ErrorMessage, vex.ValidationResult.MemberNames));
                    return new ServiceQueryResult<T>(validationErrorList);
                }
                finally
                {
                    this.ServiceContext.Operation = null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // One or more results were returned. If the result is enumerable, compose
                // any specified query operators, otherwise just return the singleton instance.
                enumerableResult = result as IEnumerable;
                if (enumerableResult != null)
                {
                    // If there are additional filtering, sorting and paging parameters to apply
                    // we'll need to compose a query
                    if (queryDescription.Query != null)
                    {
                        // Compose the query over the results
                        enumerableResult = QueryComposer.Compose(enumerableResult.AsQueryable(), queryDescription.Query);
                        if (totalCount == DomainService.TotalCountUndefined)
                        {
                            totalCount = await this.GetTotalCountForQueryAsync<T>(queryDescription, (IQueryable)enumerableResult, /* skipPagingCheck */ false, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else if (totalCount == DomainService.TotalCountUndefined)
                    {
                        // Assume the result-set's number of rows is the total count.
                        totalCount = DomainService.TotalCountEqualsResultSetCount;
                    }

                    IEnumerable limitedResults;
                    if (QueryComposer.TryComposeWithLimit(enumerableResult, queryDescription.Method, out limitedResults))
                    {
                        if (totalCount == DomainService.TotalCountEqualsResultSetCount)
                        {
                            totalCount = await this.GetTotalCountForQueryAsync<T>(queryDescription, enumerableResult.AsQueryable(), /* skipPagingCheck */ true, cancellationToken).ConfigureAwait(false);
                        }

                        enumerableResult = limitedResults;
                    }

                    // Enumerate the query.
                    enumeratedResult = await EnumerateAsync<T>(enumerableResult, DomainService.DefaultEstimatedQueryResultCount, cancellationToken).ConfigureAwait(false);
                    if (totalCount == DomainService.TotalCountEqualsResultSetCount)
                    {
                        totalCount = enumeratedResult.Count;
                    }
                }
                else
                {
                    // create a strongly typed array for the singleton return
                    if (result != null)
                    {
                        enumeratedResult = new T[] { (T)result };
                    }
                }
            }
            catch (Exception e)
            {
                if (e.IsFatal())
                {
                    throw;
                }
                Exception exceptionToReport = ExceptionHandlingUtility.GetUnwrappedException(e);
                DomainServiceErrorInfo error = new DomainServiceErrorInfo(exceptionToReport);
                this.OnError(error);

                if (error.Error != exceptionToReport)
                {
                    throw error.Error;
                }

                // Error wasn't changed, so re-throw and preserve the stack trace.
                throw;
            }

            return new ServiceQueryResult<T>(enumeratedResult, totalCount);
        }

        /// <summary>
        /// Invokes the specified invoke operation.
        /// </summary>
        /// <param name="invokeDescription">The description of the invoke operation to perform.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>The return value of the invocation.</returns>
        public virtual async ValueTask<ServiceInvokeResult> InvokeAsync(InvokeDescription invokeDescription, CancellationToken cancellationToken)
        {
            try
            {
                if (invokeDescription == null)
                {
                    throw new ArgumentNullException(nameof(invokeDescription));
                }

                this.EnsureInitialized();
                this.CheckOperationType(DomainOperationType.Invoke);

                if (!this.ValidateMethodCall(invokeDescription.Method, invokeDescription.ParameterValues, out List<ValidationResult> errors))
                {
                    var validationErrorsList = new List<ValidationResult>();
                    foreach (ValidationResult error in errors)
                    {
                        validationErrorsList.Add(new ValidationResult(error.ErrorMessage, error.MemberNames));
                    }
                    return new ServiceInvokeResult(validationErrorsList);
                }

                this.ServiceContext.Operation = invokeDescription.Method;
                try
                {
                    object returnValue = await invokeDescription.Method.InvokeAsync(this, invokeDescription.ParameterValues, cancellationToken)
                        .ConfigureAwait(false);

                    return new ServiceInvokeResult(returnValue);
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = ExceptionHandlingUtility.GetUnwrappedException(tie);
                    if (e is ValidationException)
                    {
                        throw e;
                    }

                    throw;
                }
            }
            catch (ValidationException e)
            {
                var validationErrorsList = new List<ValidationResult>();
                validationErrorsList.Add(new ValidationResult(e.ValidationResult.ErrorMessage, e.ValidationResult.MemberNames));
                return new ServiceInvokeResult(validationErrorsList);
            }
            catch (Exception e)
            {
                if (e.IsFatal())
                {
                    throw;
                }
                Exception exceptionToReport = ExceptionHandlingUtility.GetUnwrappedException(e);
                DomainServiceErrorInfo error = new DomainServiceErrorInfo(exceptionToReport);
                this.OnError(error);

                if (error.Error != exceptionToReport)
                {
                    throw error.Error;
                }

                // Error wasn't changed, so re-throw and preserve the stack trace.
                throw;
            }
            finally
            {
                this.ServiceContext.Operation = null;
            }
        }

        /// <summary>
        /// Performs the operations indicated by the specified <see cref="ChangeSet"/> by invoking
        /// the corresponding domain operations for each.
        /// </summary>
        /// <param name="changeSet">The changeset to submit</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>True if the submit was successful, false otherwise.</returns>
        public virtual async ValueTask<bool> SubmitAsync(ChangeSet changeSet, CancellationToken cancellationToken)
        {
            try
            {
                if (changeSet == null)
                {
                    throw new ArgumentNullException(nameof(changeSet));
                }
                this._changeSet = changeSet;

                this.EnsureInitialized();
                this.CheckOperationType(DomainOperationType.Submit);
                this.ResolveOperations();

                if (!this.AuthorizeChangeSet())
                {
                    // Don't try to save if there were any errors.
                    return false;
                }

                // Before invoking any operations, validate the entire changeset
                if (!await this.ValidateChangeSetAsync(cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();
                // Now that we're validated, proceed to invoke the domain operation entries.
                if (!await this.ExecuteChangeSetAsync(cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }

                // persist the changes
                if (!await this.PersistChangeSetAsyncInternal(cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }

                // Apply entity transforms and commit any final replacements
                changeSet.ApplyAssociatedStoreEntityTransforms();
                changeSet.CommitReplacedEntities();
            }
            catch (Exception e)
            {
                if (e.IsFatal())
                {
                    throw;
                }
                Exception exceptionToReport = ExceptionHandlingUtility.GetUnwrappedException(e);
                DomainServiceErrorInfo error = new DomainServiceErrorInfo(exceptionToReport);
                this.OnError(error);

                if (error.Error != exceptionToReport)
                {
                    throw error.Error;
                }

                // Error wasn't changed, so re-throw and preserve the stack trace.
                throw;
            }

            return true;
        }

        /// <summary>
        /// Helper method to invoke the <see cref="AuthorizationAttribute.Authorize"/> method on a collection
        /// of <see cref="AuthorizationAttribute"/>s.
        /// </summary>
        /// <remarks>
        /// The <see cref="RequiresAuthenticationAttribute"/> will be evaluated first if present.
        /// </remarks>
        /// <param name="attributes">The collection of attributes to test.  It may be empty.</param>
        /// <param name="principal">The <see cref="IPrincipal"/> to use.</param>
        /// <param name="authorizationContext">The <see cref="AuthorizationContext"/> to use.</param>
        /// <returns>The <see cref="AuthorizationResult"/>.  The value of <see cref="AuthorizationResult.Allowed"/> 
        /// indicates it is allowed, any other non-null value indicates it was denied.
        /// </returns>
        private static AuthorizationResult EvaluateAuthorization(IEnumerable<AuthorizationAttribute> attributes, IPrincipal principal, AuthorizationContext authorizationContext)
        {
            System.Diagnostics.Debug.Assert(attributes != null, "Authorization attributes cannot be null");
            System.Diagnostics.Debug.Assert(principal != null, "Principal cannot be null");
            System.Diagnostics.Debug.Assert(authorizationContext != null, "AuthorizationContext cannot be null");

            // 2 passes.
            // Pass 1 does [RequiresAuthentication] so we ensure it is always first.  The idea is that if it is present, that is the most informative.
            // Pass 2 does the rest
            foreach (AuthorizationAttribute attribute in attributes)
            {
                if (attribute is RequiresAuthenticationAttribute)
                {
                    AuthorizationResult result = attribute.Authorize(principal, authorizationContext);
                    if (result != AuthorizationResult.Allowed)
                    {
                        return result;
                    }
                    break;
                }
            }

            // Pass 2
            foreach (AuthorizationAttribute attribute in attributes)
            {
                if (!(attribute is RequiresAuthenticationAttribute))
                {
                    AuthorizationResult result = attribute.Authorize(principal, authorizationContext);
                    if (result != AuthorizationResult.Allowed)
                    {
                        return result;
                    }
                }
            }

            return AuthorizationResult.Allowed;
        }

        /// <summary>
        /// Performs object, method and property validation and sets any recoverable
        /// errors encountered on the given <see cref="ChangeSetEntry"/> list.
        /// </summary>
        /// <param name="operations">The list of operations to validate.</param>
        /// <param name="domainServiceDescription">The <see cref="DomainServiceDescription"/> for the operation being validated.</param>
        /// <param name="validationContextRoot">An optional <see cref="ValidationContext"/> to use for services and items, or <c>null</c>.</param>
        /// <returns><c>true</c> if all the operations in the specified list are valid.</returns>
        internal static bool ValidateOperations(IEnumerable<ChangeSetEntry> operations, DomainServiceDescription domainServiceDescription, ValidationContext validationContextRoot)
        {
            bool success = true;
            IEnumerable<ChangeSetEntry> operationsToValidate = operations.Where(
                op => (op.DomainOperationEntry != null && op.Operation != DomainOperation.None)
                || (op.EntityActions != null && op.EntityActions.Any()));

            foreach (ChangeSetEntry operation in operationsToValidate)
            {
                object entity = operation.Entity;
                DomainOperationEntry domainOperation = operation.DomainOperationEntry;
                MetaType metaType = MetaType.GetMetaType(entity.GetType());
                bool hasCustomMethod = operation.EntityActions != null && operation.EntityActions.Any();

                if (!metaType.RequiresValidation && domainOperation != null && !domainOperation.RequiresValidation && !hasCustomMethod)
                {
                    continue;
                }

                // The entity is specified as the object instance, even for method level validation, since
                // logically the operation is on that entity
                ValidationContext validationContext = ValidationUtilities.CreateValidationContext(entity, validationContextRoot);
                List<ValidationResult> validationResults = new List<ValidationResult>();

                if (operation.Operation != DomainOperation.Delete && metaType.RequiresValidation)
                {
                    // First validate the entity. We don't perform entity level validation for deleted entities
                    ValidationUtilities.TryValidateObject(entity, validationContext, validationResults);
                }

                // If the method has method level validation, perform it
                if (domainOperation != null && domainOperation.RequiresValidation)
                {
                    TryValidateOperation(operation.DomainOperationEntry, validationContext, new object[] { entity }, validationResults);
                }

                // if the entity has a custom method invocation, validate the all method calls
                if (hasCustomMethod)
                {
                    Dictionary<string, DomainOperationEntry> invokedActions = null;
                    for (int i = 0; i < operation.EntityActions.Count; ++i)
                    {
                        var action = operation.EntityActions[i];

                        DomainOperationEntry customMethodOperation = null;
                        bool alreadyInvoked = invokedActions != null && invokedActions.TryGetValue(action.Key, out customMethodOperation);

                        if (!alreadyInvoked) // equivalent to (customMethodOperation != null)
                            customMethodOperation = domainServiceDescription.GetCustomMethod(operation.Entity.GetType(), action.Key);

                        // Of more that one action is queued check for multiple invocations, this way we don't have to allocate
                        // a dictionary unless necessary
                        if (operation.EntityActions.Count > 1)
                        {
                            if (invokedActions == null) // alreadyInvoked will be false since this is first action
                                invokedActions = new Dictionary<string, DomainOperationEntry>();

                            if (!alreadyInvoked)
                            {
                                invokedActions.Add(action.Key, customMethodOperation);
                            }
                            else if (((EntityActionAttribute)customMethodOperation.OperationAttribute).AllowMultipleInvocations == false)
                            {
                                throw new InvalidOperationException(
                                    string.Format(Resource.DomainService_MultipleEntityActionsNotAllowedFor,
                                                operation.Entity.GetType().Name,
                                                action.Key));
                            }
                        }

                        if (customMethodOperation.RequiresValidation)
                        {
                            object[] parameters = DomainService.GetCustomMethodParams(customMethodOperation, operation.Entity, action.Value);
                            TryValidateOperation(customMethodOperation, validationContext, parameters, validationResults);
                        }
                    }
                }

                // set errors collection if any
                if (validationResults.Count > 0)
                {
                    operation.ValidationErrors = new List<ValidationResultInfo>(validationResults.Select(err => new ValidationResultInfo(err.ErrorMessage, err.MemberNames)).Distinct(EqualityComparer<ValidationResultInfo>.Default));
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Gets the result count for the specified <see cref="IQueryable&lt;T&gt;" />. <see cref="DomainService" />s should 
        /// override this method to implement support for total-counts of paged result-sets. Overrides shouldn't 
        /// call the base method.
        /// </summary>
        /// <typeparam name="T">The element <see cref="Type"/> of the query.</typeparam>
        /// <param name="query">The query for which the count should be returned.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>The total result count if total-counts are supported; -1 otherwise.</returns>
        protected virtual ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            return new ValueTask<int>(DomainService.TotalCountUndefined);
        }

        /// <summary>
        /// Disposes this <see cref="DomainService"/>.
        /// </summary>
        /// <param name="disposing">True if we're currently disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Verifies the user is authorized to submit the current <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> is authorized, false otherwise.</returns>
        protected virtual bool AuthorizeChangeSet()
        {
            foreach (ChangeSetEntry op in this.ChangeSet.ChangeSetEntries)
            {
                object entity = op.Entity;

                // Authentication check - will throw if unauthorized
                if (op.DomainOperationEntry != null)
                {
                    this.ValidateMethodPermissions(op.DomainOperationEntry, entity);
                }

                // if there is a custom method invocation for this operation
                // we need to authorize that as well
                if (op.EntityActions != null && op.EntityActions.Any())
                {
                    foreach (var entityAction in op.EntityActions)
                    {
                        DomainOperationEntry customMethodOperation = this.ServiceDescription.GetCustomMethod(entity.GetType(), entityAction.Key);
                        this.ValidateMethodPermissions(customMethodOperation, entity);
                    }
                }
            }

            return !this.ChangeSet.HasError;
        }

        /// <summary>
        /// Validates the current <see cref="ChangeSet"/>. Any errors should be set on the individual <see cref="ChangeSetEntry"/>s
        /// in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns><c>True</c> if all operations in the <see cref="ChangeSet"/> passed validation, <c>false</c> otherwise.</returns>
        protected virtual ValueTask<bool> ValidateChangeSetAsync(CancellationToken cancellationToken)
        {
            // Perform validation on the each of the operations.
            var result = ValidateOperations(this.ChangeSet.ChangeSetEntries, this.ServiceDescription, this.ValidationContext);
            return new ValueTask<bool>(result);
        }

        /// <summary>
        /// This method invokes the <see cref="DomainOperationEntry"/> for each operation in the current <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> was processed successfully, false otherwise.</returns>
        protected virtual async ValueTask<bool> ExecuteChangeSetAsync(CancellationToken cancellationToken)
        {
            await this.InvokeCudOperationsAsync()
                .ConfigureAwait(false);
            await this.InvokeCustomOperations()
                .ConfigureAwait(false);

            return !this.ChangeSet.HasError;
        }

        /// <summary>
        /// This method is called whenever an unrecoverable error occurs during
        /// the processing of a <see cref="DomainService"/> operation.
        /// Override this method to perform exception logging, or to inspect or transform
        /// server errors before results are sent back to the client.
        /// </summary>
        /// <param name="errorInfo">Information on the error that occurred.</param>
        protected virtual void OnError(DomainServiceErrorInfo errorInfo) { }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the current <see cref="ChangeSet"/>
        /// have been invoked. This method should commit the changes as necessary to the data store.
        /// Any errors should be set on the individual <see cref="ChangeSetEntry"/>s in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>True if the <see cref="ChangeSet"/> was persisted successfully, false otherwise.</returns>
        protected virtual ValueTask<bool> PersistChangeSetAsync(CancellationToken cancellationToken) { return new ValueTask<bool>(true); }

        /// <summary>
        /// For all operations in the current changeset, validate that the operation exists, and
        /// set the operation entry.
        /// </summary>
        private void ResolveOperations()
        {
            // Resolve and set the DomainOperationEntry for each operation in the changeset
            foreach (ChangeSetEntry changeSetEntry in this.ChangeSet.ChangeSetEntries)
            {
                // resolve the DomainOperationEntry
                Type entityType = changeSetEntry.Entity.GetType();
                DomainOperationEntry domainOperationEntry = null;
                if (changeSetEntry.Operation == DomainOperation.Insert ||
                    changeSetEntry.Operation == DomainOperation.Update ||
                    changeSetEntry.Operation == DomainOperation.Delete)
                {
                    domainOperationEntry = this.ServiceDescription.GetSubmitMethod(entityType, changeSetEntry.Operation);
                }

                // if a custom method invocation is specified, validate that the
                // method exists
                bool isNamedUpdate = false;
                if (changeSetEntry.EntityActions != null && changeSetEntry.EntityActions.Any())
                {
                    foreach (var entityAction in changeSetEntry.EntityActions)
                    {
                        DomainOperationEntry customMethodOperation = this.ServiceDescription.GetCustomMethod(entityType, entityAction.Key);
                        if (customMethodOperation == null)
                        {
                            throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resource.DomainService_InvalidDomainOperationEntry,
                                entityAction.Key,
                                entityType.Name));
                        }

                        // if the primary operation for an update is null and there is a valid
                        // custom method, its considered a "named update"
                        isNamedUpdate = isNamedUpdate || (domainOperationEntry == null && customMethodOperation != null);
                    }
                }

                // if we were unable to find the primary operation entry and the type isn't
                // composed (composed Types aren't required to have an explicit update
                // operation) or a named update throw an exception
                bool isComposedType = this.ServiceDescription.IsComposedEntityType(entityType);
                if (domainOperationEntry == null && !isComposedType && !isNamedUpdate)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.DomainService_InvalidDomainOperationEntry,
                            changeSetEntry.Operation.ToString(),
                            entityType.Name));
                }

                // Composed children can only be updated if their parent is also part of
                // the changeset.
                if (isComposedType && changeSetEntry.ParentOperation == null)
                {
                    // In the case of self composing Types, we can't distinguish between a child update w/o a parent
                    // and a parent update w/o children. Therefore we must skip validation in that case.
                    bool isSelfReferentialComposition = this.ServiceDescription.GetParentAssociations(entityType).Any(p => p.ComponentType == entityType);
                    if (!isSelfReferentialComposition)
                    {
                        string errorMsg =
                            string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet,
                            string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeset_UpdateChildWithoutParent, changeSetEntry.Entity.GetType()));
                        throw new InvalidOperationException(errorMsg);
                    }
                }

                changeSetEntry.DomainOperationEntry = domainOperationEntry;
            }
        }

        /// <summary>
        /// Verifies that the service has been initialized for the specified
        /// operation type, and throws an exception if now.
        /// </summary>
        /// <param name="operationType">The current operation type.</param>
        private void CheckOperationType(DomainOperationType operationType)
        {
            if (this.ServiceContext.OperationType != operationType)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainService_InvalidOperationType, this.ServiceContext.OperationType, operationType));
            }
        }

        /// <summary>
        /// Gets the total count for a query.
        /// </summary>
        /// <param name="queryDescription">The query description.</param>
        /// <param name="queryable">The query.</param>
        /// <param name="skipPagingCheck"><c>true</c> if the paging check can be skipped; <c>false</c> otherwise.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>The total count.</returns>
        private ValueTask<int> GetTotalCountForQueryAsync<T>(QueryDescription queryDescription, IQueryable queryable, bool skipPagingCheck, CancellationToken cancellationToken)
        {
            // Only try to get the total count if the query description requested it.
            if (!queryDescription.IncludeTotalCount)
                return new ValueTask<int>(DomainService.TotalCountUndefined);

            IQueryable totalCountQuery = queryable;
            if (skipPagingCheck || QueryComposer.TryComposeWithoutPaging(queryable, out totalCountQuery))
            {
                return CountAsync<T>((IQueryable<T>)totalCountQuery, cancellationToken);
            }
            else
            {
                return new ValueTask<int>(DomainService.TotalCountEqualsResultSetCount);
            }
        }

        /// <summary>
        /// Process parameter values and attempt to convert them to the types 
        /// defined by the method signature. On successful completion, the
        /// <paramref name="parameterValues"/> collection will contain the 
        /// the converted values.
        /// </summary>
        /// <param name="method"><see cref="DomainOperationEntry"/> that defines the 
        /// expected parameter types.</param>
        /// <param name="parameterValues">The raw parameter values.</param>
        /// <exception cref="InvalidOperationException">is thrown if the number 
        /// of parameter values provided does not match the number of method 
        /// parameter arguments.</exception>
        private static void ConvertMethodParameters(DomainOperationEntry method, object[] parameterValues)
        {
            if (method.Parameters.Count != parameterValues.Length)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.Method_Parameter_Count_Wrong, method.Name, method.Parameters.Count));
            }

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                DomainOperationParameter parameter = method.Parameters[i];
                try
                {
                    parameterValues[i] = SerializationUtility.GetServerValue(parameter.ParameterType, parameterValues[i]);
                }
                catch (InvalidCastException ex)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.IncorrectParameterType, parameterValues[i].GetType(), parameter.Name, parameter.ParameterType), ex);
                }
            }
        }

        /// <summary>
        /// Returns the properly converted and configured parameter array for the specified object and
        /// parameter values. This is formed by using the <paramref name="entity"/> as the first argument,
        /// then adding the rest of the invocation parameters from the client.
        /// </summary>
        /// <remarks>
        /// The <paramref name="entity"/> passed to the method will either be the original client modified entity as
        /// contained in the operation or a new associated entity if a user called the <see cref="ChangeSet"/> Replace
        /// method prior to invoking this operation.
        /// </remarks>
        /// <param name="customMethodEntry">The custom method operation entry.</param>
        /// <param name="entity">The object the custom method is invoked on.</param>
        /// <param name="parameters">The raw custom method parameters.</param>
        /// <returns>Returns an array of custom method call parameters.</returns>
        private static object[] GetCustomMethodParams(DomainOperationEntry customMethodEntry, object entity, IEnumerable<object> parameters)
        {
            List<object> customMethodParams = new List<object>();
            customMethodParams.Add(entity);
            if (parameters != null)
            {
                customMethodParams.AddRange(parameters);
            }

            // process the method parameters so that they conform to the expected types
            object[] paramValues = customMethodParams.ToArray();
            ConvertMethodParameters(customMethodEntry, paramValues);
            return paramValues;
        }

        /// <summary>
        /// This method invokes the user overridable <see cref="PersistChangeSetAsync"/> method wrapping the call
        /// with the appropriate exception handling logic. All framework calls to <see cref="PersistChangeSetAsync"/>
        /// must go through this method. Some data sources have their own validation hook points,
        /// so if a <see cref="ValidationException"/> is thrown at that level, we want to capture it.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> was persisted successfully, false otherwise.</returns>
        private async Task<bool> PersistChangeSetAsyncInternal(CancellationToken cancellationToken)
        {
            try
            {
                await this.PersistChangeSetAsync(cancellationToken);
            }
            catch (ValidationException e)
            {
                // if a validation exception is thrown for one of the entities in the changeset
                // set the error on the corresponding ChangeSetEntry
                if (e.Value != null && e.ValidationResult != null)
                {
                    IEnumerable<ChangeSetEntry> updateOperations =
                        this.ChangeSet.ChangeSetEntries.Where(
                            p => p.Operation == DomainOperation.Insert ||
                                 p.Operation == DomainOperation.Update ||
                                 p.Operation == DomainOperation.Delete);

                    ChangeSetEntry operation = updateOperations.SingleOrDefault(p => object.ReferenceEquals(p.Entity, e.Value));
                    if (operation != null)
                    {
                        ValidationResultInfo error = new ValidationResultInfo(e.ValidationResult.ErrorMessage, e.ValidationResult.MemberNames);
                        error.StackTrace = e.StackTrace;
                        operation.ValidationErrors = new List<ValidationResultInfo>() { error };
                    }
                }
                else
                {
                    throw;
                }
            }

            return !this.ChangeSet.HasError;
        }

        /// <summary>
        /// Ensures the <see cref="DomainService"/> has been initialized properly.
        /// </summary>
        /// <exception cref="InvalidOperationException">if this service instance hasn't been initialized.</exception>
        private void EnsureInitialized()
        {
            if (this._serviceContext == null)
            {
                throw new InvalidOperationException(Resource.DomainService_NotInitialized);
            }
        }

        /// <summary>
        /// Validate the permissions for the specified <paramref name="domainOperationEntry"/>. If the authorization check
        /// fails, an <see cref="UnauthorizedAccessException"/> will be thrown.
        /// </summary>
        /// <param name="domainOperationEntry">The <see cref="DomainOperationEntry"/> to validate.</param>
        /// <param name="entity">The optional entity instance being authorized.</param>
        private void ValidateMethodPermissions(DomainOperationEntry domainOperationEntry, object entity)
        {
            AuthorizationResult result = this.IsAuthorized(domainOperationEntry, entity);
            if (result != AuthorizationResult.Allowed)
            {
                throw new UnauthorizedAccessException(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Invokes all CUD operations in the current <see cref="ChangeSet"/>.
        /// </summary>
        private async Task InvokeCudOperationsAsync()
        {
            object[] parameters = new object[1];
            foreach (ChangeSetEntry operation in this.ChangeSet.ChangeSetEntries
                .Where(op => op.Operation == DomainOperation.Insert ||
                             op.Operation == DomainOperation.Update ||
                             op.Operation == DomainOperation.Delete))
            {
                if (operation.DomainOperationEntry == null)
                {
                    // in the case of composed operations, the operation might
                    // be null
                    continue;
                }

                if (operation.Operation == DomainOperation.Update && !operation.HasMemberChanges &&
                    !this.ChangeSet.HasChildChanges(operation.Entity))
                {
                    // only call update operations if the entity is actually modified or if it has
                    // child changes to process (in the case of composition).
                    continue;
                }

                parameters[0] = operation.Entity;

                await this.InvokeDomainOperationEntryAsync(operation.DomainOperationEntry, parameters, operation)
                    .ConfigureAwait(false);

                // Remove any associated entities if an error occurred.
                if (operation.HasError)
                {
                    this.ChangeSet.EntitiesToReplace.Remove(operation.Entity);
                }
            }

            // Commit associated entities in the changeset so that 
            // subsequent operations see the appropriate entities.
            this.ChangeSet.CommitReplacedEntities();
        }

        /// <summary>
        /// Invokes all Custom operations in the <see cref="ChangeSet"/>.
        /// </summary>
        private async Task InvokeCustomOperations()
        {
            foreach (ChangeSetEntry operation in this.ChangeSet.ChangeSetEntries.Where(op => op.EntityActions != null && op.EntityActions.Any()))
            {
                foreach (var entityAction in operation.EntityActions)
                {
                    var customMethodOperation = this.ServiceDescription.GetCustomMethod(operation.Entity.GetType(), entityAction.Key);
                    object[] parameters = DomainService.GetCustomMethodParams(customMethodOperation, operation.Entity, entityAction.Value);
                    await this.InvokeDomainOperationEntryAsync(customMethodOperation, parameters, operation)
                        .ConfigureAwait(false);

                    // Remove any associated entities if an error occurred.
                    if (operation.HasError)
                    {
                        this.ChangeSet.EntitiesToReplace.Remove(operation.Entity);
                    }
                }
            }

            // Commit all associated entities in the changeset.
            this.ChangeSet.CommitReplacedEntities();
        }

        /// <summary>
        /// Invokes the given <see cref="DomainOperationEntry"/>. If a non-recoverable exception
        /// is encountered during the invocation, the exception is
        /// re-thrown. Otherwise, the error is appended to the operation's
        /// errors list.
        /// </summary>
        /// <param name="domainOperationEntry">The domain operation entry to invoke.</param>
        /// <param name="parameters">The parameters to invoke domain operation entry with.</param>
        /// <param name="operation">The <see cref="ChangeSetEntry"/> object associated with the domain operation entry for logging errors (if any).</param>
        /// <returns>The result of the <see cref="DomainOperationEntry"/>.</returns>
        private async ValueTask<object> InvokeDomainOperationEntryAsync(DomainOperationEntry domainOperationEntry, object[] parameters, ChangeSetEntry operation)
        {
            // invoke the domain operation entry and catch continuable errors if any
            this.ServiceContext.Operation = domainOperationEntry;
            string stackTrace = null;
            try
            {
                try
                {
                    return await domainOperationEntry.InvokeAsync(this, parameters, this.ServiceContext.CancellationToken)
                        .ConfigureAwait(false);
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = tie;
                    while (e.InnerException != null && e is TargetInvocationException)
                    {
                        e = e.InnerException;
                    }

                    // Cache the real stacktrace.
                    stackTrace = e.StackTrace;
                    throw e;
                }
            }
            catch (ValidationException vex)
            {
                // We may have already gotten a more useful stacktrace if a TargetInvocationException
                // was thrown.
                if (stackTrace == null)
                {
                    stackTrace = vex.StackTrace;
                }

                ValidationResultInfo error = new ValidationResultInfo(vex.Message, 0, stackTrace, vex.ValidationResult.MemberNames);
                if (operation.ValidationErrors != null)
                {
                    operation.ValidationErrors = operation.ValidationErrors.Concat(new ValidationResultInfo[] { error }).ToArray();
                }
                else
                {
                    operation.ValidationErrors = new ValidationResultInfo[] { error };
                }
            }
            finally
            {
                this.ServiceContext.Operation = null;
            }

            return new ValueTask<object>((object)null);
        }

        /// <summary>
        /// Validates a method call.
        /// </summary>
        /// <param name="domainOperationEntry">The <see cref="DomainOperationEntry"/> to validate.</param>
        /// <param name="parameters">The parameters to pass to the method.</param>
        /// <param name="validationResults">The collection to which we can append validation results.</param>
        /// <returns><c>true</c> if the parameters are valid, <c>false</c> otherwise.</returns>
        private bool ValidateMethodCall(DomainOperationEntry domainOperationEntry, object[] parameters, out List<ValidationResult> validationResults)
        {
            // First do an authentication check and throw immediately if unauthorized
            this.ValidateMethodPermissions(domainOperationEntry, /* entity */ null);

            // Then do validaiton only if required
            if (!domainOperationEntry.RequiresValidation)
            {
                validationResults = null;
                return true;
            }

            validationResults = new List<ValidationResult>();
            ValidationContext validationContext = ValidationUtilities.CreateValidationContext(this, this.ValidationContext);

            // First do method level and simple parameter validation
            bool success = TryValidateOperation(domainOperationEntry, validationContext, parameters, validationResults);

            DomainServiceDescription desc = this.ServiceDescription;

            // for any entity parameters, do full Type level validation
            foreach (object entityParameter in parameters.Where(p => p != null && desc.IsKnownEntityType(p.GetType())))
            {
                ValidationContext context = ValidationUtilities.CreateValidationContext(entityParameter, validationContext);
                if (!ValidationUtilities.TryValidateObject(entityParameter, context, validationResults))
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Validates the specified method, returning any validation errors
        /// </summary>
        /// <param name="operation">The operation to validate.</param>
        /// <param name="validationContext">The validation context.</param>
        /// <param name="parameters">The parameter values.</param>
        /// <param name="validationResults">Collection of ValidationResults to accumulate into.</param>
        /// <returns><c>True</c> if the method is valid, <c>false</c> otherwise.</returns>
        private static bool TryValidateOperation(DomainOperationEntry operation, ValidationContext validationContext, object[] parameters, List<ValidationResult> validationResults)
        {
            return ValidationUtilities.TryValidateMethodCall(operation, validationContext, parameters, validationResults);
        }

        /// <summary>
        /// Enumerates the specified enumerable to guarantee eager execution. This method is similar to 
        /// Enumerable.ToArray, except it contains a few additional optimizations, such as using 
        /// bigger arrays to reduce the number of resizes required for most scenarios.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="enumerable">The enumerable to enumerate.</param>
        /// <param name="estimatedResultCount">The estimated number of items the enumerable will yield.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> which may be used by hosting layer to request cancellation</param>
        /// <returns>A new enumerable with the results of the enumerated enumerable.</returns>
        protected virtual ValueTask<IReadOnlyCollection<T>> EnumerateAsync<T>(IEnumerable enumerable, int estimatedResultCount, CancellationToken cancellationToken)
        {
            // No need to enumerate arrays.
            if (enumerable is T[] array)
            {
                return new ValueTask<IReadOnlyCollection<T>>(array);
            }

            if (enumerable is ICollection<T> collection)
            {
                // readonlyCollection is covariant so type checking is much slower, but if it is a collection
                // if will probably also be a IReadOnlyCollection so we dont have to allocate at all
                if (collection is IReadOnlyCollection<T> readonlyCollection)
                {
                    return new ValueTask<IReadOnlyCollection<T>>(readonlyCollection);
                }
                else
                {
                    array = new T[collection.Count];
                    collection.CopyTo(array, 0);
                    return new ValueTask<IReadOnlyCollection<T>>(array);
                }
            }

            var list = new List<T>(estimatedResultCount);
            foreach (T item in enumerable)
            {
                list.Add(item);
            }
            return new ValueTask<IReadOnlyCollection<T>>(list);
        }

        #region Nested Types

        /// <summary>
        /// Default <see cref="IDomainServiceFactory"/> implementation.
        /// </summary>
        private class DefaultDomainServiceFactory : IDomainServiceFactory
        {
            /// <summary>
            /// Creates a new <see cref="DomainService"/> instance.
            /// </summary>
            /// <param name="domainServiceType">The <see cref="Type"/> of <see cref="DomainService"/> to create.</param>
            /// <param name="context">The current <see cref="DomainServiceContext"/>.</param>
            /// <returns>A <see cref="DomainService"/> instance.</returns>
            public DomainService CreateDomainService(Type domainServiceType, DomainServiceContext context)
            {
                if (!typeof(DomainService).IsAssignableFrom(domainServiceType))
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, Resource.DomainService_Factory_InvalidDomainServiceType, domainServiceType),
                        nameof(domainServiceType));
                }

                DomainService domainService = null;
                try
                {
                    domainService = (DomainService)Activator.CreateInstance(domainServiceType);
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    throw;
                }

                domainService.Initialize(context);
                return domainService;
            }

            /// <summary>
            /// Releases an existing <see cref="DomainService"/> instance.
            /// </summary>
            /// <param name="domainService">A <see cref="DomainService"/> instance to release.</param>
            public void ReleaseDomainService(DomainService domainService)
            {
                if (domainService == null)
                {
                    throw new ArgumentNullException(nameof(domainService));
                }
                domainService.Dispose();
            }
        }

        #endregion Nested Types
    }
}
