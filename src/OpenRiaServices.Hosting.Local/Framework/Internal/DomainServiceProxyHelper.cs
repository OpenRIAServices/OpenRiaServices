using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Local
{
    /// <summary>
    /// Used to perform Submit, Invoke and Query operations on a <see cref="DomainService"/> proxy instances.
    /// </summary>
    internal static class DomainServiceProxyHelper
    {
        static readonly MethodInfo s_queryGeneric = typeof(DomainServiceProxyHelper).GetMethod(nameof(DomainServiceProxyHelper.QueryCore), BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Helper method performs a query operation against a given proxy instance.
        /// </summary>
        /// <param name="domainService">The type of <see cref="DomainService"/> to perform this query operation against.</param>
        /// <param name="context">The current context.</param>
        /// <param name="domainServiceInstances">The list of tracked <see cref="DomainService"/> instances that any newly created
        /// <see cref="DomainService"/> will be added to.</param>
        /// <param name="queryName">The name of the query to invoke.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The query results. May be null if there are no query results.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="context"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="queryName"/> is null or an empty string.</exception>
        /// <exception cref="InvalidOperationException">if no match query operation exists on the <paramref name="context"/>.</exception>
        /// <exception cref="OperationException">if operation errors are thrown during execution of the query operation.</exception>
        public static IEnumerable Query(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, string queryName, object[] parameters)
        {
            context = new DomainServiceContext(context, context.User, DomainOperationType.Query);
            DomainService service = CreateDomainServiceInstance(domainService, context, domainServiceInstances);
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(service.GetType());
            DomainOperationEntry queryOperation = serviceDescription.GetQueryMethod(queryName);

            if (queryOperation == null)
            {
                string errorMessage =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resource.DomainServiceProxy_QueryOperationNotFound,
                        queryName,
                        domainService);

                throw new InvalidOperationException(errorMessage);
            }

            object[] parameterValues = parameters ?? Array.Empty<object>();
            QueryDescription queryDescription = new QueryDescription(queryOperation, parameterValues);

            var actualMethod = s_queryGeneric.MakeGenericMethod(queryDescription.Method.AssociatedType);

            try
            {
                return (IEnumerable)actualMethod.Invoke(null, new object[] { service, queryDescription });
            }
            catch (TargetInvocationException tie) when (tie.InnerException is object)
            {
                throw tie.InnerException;
            }
        }

        private static IEnumerable QueryCore<T>(DomainService service, QueryDescription queryDescription)
        {
            // TODO: Look into removing this blocking Wait
            var queryResult = service.QueryAsync<T>(queryDescription, CancellationToken.None)
                .GetAwaiter().GetResult();

            var validationErrors = queryResult.ValidationErrors;
            var result = queryResult.Result;

            if (validationErrors != null && validationErrors.Any())
            {
                IEnumerable<ValidationResultInfo> operationErrors = validationErrors.Select(ve => new ValidationResultInfo(ve.ErrorMessage, ve.MemberNames));
                throw new OperationException(Resource.DomainServiceProxy_OperationError, operationErrors);
            }

            return result;
        }

        /// <summary>
        /// Helper method performs a submit operation against a given proxy instance.
        /// </summary>
        /// <param name="domainService">The type of <see cref="DomainService"/> to perform this query operation against.</param>
        /// <param name="context">The current context.</param>
        /// <param name="domainServiceInstances">The list of tracked <see cref="DomainService"/> instances that any newly created
        /// <see cref="DomainService"/> will be added to.</param>
        /// <param name="currentOriginalEntityMap">The mapping of current and original entities used with the utility <see cref="DomainServiceProxy.AssociateOriginal"/> method.</param>
        /// <param name="entity">The entity being submitted.</param>
        /// <param name="operationName">The name of the submit operation. For CUD operations, this can be null.</param>
        /// <param name="parameters">The submit operation parameters.</param>
        /// <param name="domainOperation">The type of submit operation.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="context"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="entity"/> is null.</exception>
        /// <exception cref="OperationException">if operation errors are thrown during execution of the submit operation.</exception>
        public static void Submit(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, IDictionary<object, object> currentOriginalEntityMap, object entity, string operationName, object[] parameters, DomainOperation domainOperation)
        {
            context = new DomainServiceContext(context, context.User, DomainOperationType.Submit);
            DomainService service = CreateDomainServiceInstance(domainService, context, domainServiceInstances);

            object originalEntity = null;
            currentOriginalEntityMap.TryGetValue(entity, out originalEntity);

            // if this is an update operation, regardless of whether original
            // values have been specified, we need to mark the operation as
            // modified
            bool hasMemberChanges = domainOperation == DomainOperation.Update;

            // when custom methods are invoked, the operation type
            // is Update
            if (domainOperation == DomainOperation.Custom)
            {
                domainOperation = DomainOperation.Update;
            }

            ChangeSetEntry changeSetEntry = new ChangeSetEntry(1, entity, originalEntity, domainOperation);
            changeSetEntry.HasMemberChanges = hasMemberChanges;
            if (!string.IsNullOrEmpty(operationName))
            {
                changeSetEntry.EntityActions = new List<Serialization.KeyValue<string, object[]>>(); 
                changeSetEntry.EntityActions.Add(new Serialization.KeyValue<string, object[]>(operationName, parameters));
            }

            ChangeSet changeSet = new ChangeSet(new[] { changeSetEntry });

            service.SubmitAsync(changeSet, CancellationToken.None)
                .GetAwaiter().GetResult();

            if (changeSetEntry.HasError)
            {
                throw new OperationException(Resource.DomainServiceProxy_OperationError, changeSetEntry.ValidationErrors);
            }
        }

        /// <summary>
        /// Helper method performs a invoke operation against a given proxy instance.
        /// </summary>
        /// <param name="domainService">The type of <see cref="DomainService"/> to perform this query operation against.</param>
        /// <param name="context">The current context.</param>
        /// <param name="domainServiceInstances">The list of tracked <see cref="DomainService"/> instances that any newly created
        /// <see cref="DomainService"/> will be added to.</param>
        /// <param name="name">The name of the operation to invoke.</param>
        /// <param name="parameters">The operation parameters.</param>
        /// <returns>The result of the invoke operation.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="context"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="name"/> is null or an empty string.</exception>
        /// <exception cref="OperationException">if operation errors are thrown during execution of the invoke operation.</exception>
        public static object Invoke(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, string name, object[] parameters)
        {
            context = new DomainServiceContext(context,  context.User, DomainOperationType.Invoke);
            DomainService service = CreateDomainServiceInstance(domainService, context, domainServiceInstances);
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(service.GetType());
            DomainOperationEntry method = serviceDescription.GetInvokeOperation(name);

            InvokeDescription invokeDescription = new InvokeDescription(method, parameters);
            // TODO: Look into removing this blocking Wait
            var loadResult = service.InvokeAsync(invokeDescription, CancellationToken.None)
                .GetAwaiter().GetResult();
            if (loadResult.HasValidationErrors)
            {
                IEnumerable<ValidationResultInfo> operationErrors = loadResult.ValidationErrors.Select(ve => new ValidationResultInfo(ve.ErrorMessage, ve.MemberNames));
                throw new OperationException(Resource.DomainServiceProxy_OperationError, operationErrors);
            }

            return loadResult.Result;
        }

        /// <summary>
        /// Creates a <see cref="DomainService"/> instance for a given <see cref="DomainServiceContext"/>.
        /// </summary>
        /// <param name="domainService">The <see cref="DomainService"/> <see cref="Type"/> to create.</param>
        /// <param name="context">The <see cref="DomainServiceContext"/> to provide to the <see cref="DomainService.Factory"/>.</param>
        /// <param name="domainServiceInstances">The list used to track <see cref="DomainService"/> instances.</param>
        /// <returns>A <see cref="DomainService"/> instance.</returns>
        private static DomainService CreateDomainServiceInstance(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances)
        {
            DomainService service = DomainService.Factory.CreateDomainService(domainService, context);
            domainServiceInstances.Add(service);
            return service;
        }
    }
}
