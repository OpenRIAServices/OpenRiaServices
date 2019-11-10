extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.Test.Utilities;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client.Test
{
    /// <summary>
    /// Sample DomainClient implementation that operates on a set of in memory data for testing purposes.
    /// </summary>
    public class CitiesMockDomainClient : DomainClient
    {
        private readonly Cities.CityData citiesData = new Cities.CityData();
        private bool _isCancellationSupported = true;

        /// <summary>
        /// What to return on next Invoke
        /// </summary>
        public Task<InvokeCompletedResult> InvokeCompletedResult { get; set; }
        public Task<QueryCompletedResult> QueryCompletedResult { get; set; }
        public Task<SubmitCompletedResult> SubmitCompletedResult { get; set; }

        public override bool SupportsCancellation => _isCancellationSupported;
        public void SetSupportsCancellation(bool value) => _isCancellationSupported = value;

        public Func<EntityChangeSet, IEnumerable<ChangeSetEntry>, Task<SubmitCompletedResult>> SubmitCompletedCallback { get; set; }
        protected override Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken)
        {
            if (QueryCompletedResult != null)
                return QueryCompletedResult;

            // load test data and get query result
            IEnumerable<Entity> entities = GetQueryResult(query.QueryName, query.Parameters);
            if (query.Query != null)
            {
                entities = RebaseQuery(entities.AsQueryable(), query.Query).Cast<Entity>().ToList();
            }

            int entityCount = entities.Count();
            QueryCompletedResult results = new QueryCompletedResult(entities, Array.Empty<Entity>(), entityCount, Array.Empty<ValidationResult>());
            return TaskHelper.FromResult(results);
        }

        protected override Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            if (SubmitCompletedResult != null)
                return SubmitCompletedResult;

            IEnumerable<ChangeSetEntry> submitOperations = changeSet.GetChangeSetEntries();

            if (SubmitCompletedCallback != null)
            {
                return SubmitCompletedCallback(changeSet, submitOperations);
            }
            else
            {
                // perform mock submit operations
                SubmitCompletedResult submitResults = new SubmitCompletedResult(changeSet, submitOperations);
                return TaskHelper.FromResult(submitResults);
            }
        }

        protected override Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken)
        {
            if (InvokeCompletedResult != null)
                return InvokeCompletedResult;

            object returnValue = null;
            // do the invoke and get the return value
            if (invokeArgs.OperationName == "Echo")
            {
                returnValue = "Echo: " + (string)invokeArgs.Parameters.Values.First();
            }

            return TaskHelper.FromResult(new InvokeCompletedResult(returnValue));
        }

        /// <summary>
        /// Rebases the specified query with the specified queryable root
        /// </summary>
        /// <param name="root">The new root</param>
        /// <param name="query">The query to insert the root into</param>
        /// <returns>The rebased query</returns>
        private static IQueryable RebaseQuery(IQueryable root, IQueryable query)
        {
            if (root.ElementType != query.ElementType)
            {
                // types not equal, so we need to inject a cast
                System.Linq.Expressions.Expression castExpr = System.Linq.Expressions.Expression.Call(
                    typeof(Queryable), "Cast",
                    new Type[] { query.ElementType },
                    root.Expression);
                root = root.Provider.CreateQuery(castExpr);
            }

            return RebaseInternal(root, query.Expression);
        }

        private static IQueryable RebaseInternal(IQueryable root, System.Linq.Expressions.Expression queryExpression)
        {
            MethodCallExpression mce = queryExpression as MethodCallExpression;
            if (mce != null && (mce.Arguments[0].NodeType == ExpressionType.Constant) &&
               (((ConstantExpression)mce.Arguments[0]).Value != null) &&
                (((ConstantExpression)mce.Arguments[0]).Value is IQueryable))
            {
                // this MethodCall is directly on the query root - replace
                // the root
                mce = ResourceQueryOperatorCall(System.Linq.Expressions.Expression.Constant(root), mce);
                return root.Provider.CreateQuery(mce);
            }

            // make the recursive call to find and replace the root
            root = RebaseInternal(root, mce.Arguments[0]);
            mce = ResourceQueryOperatorCall(root.Expression, mce);

            return root.Provider.CreateQuery(mce);
        }

        /// <summary>
        /// Given a MethodCallExpression, copy the expression, replacing the source with the source provided
        /// </summary>
        /// <param name="source"></param>
        /// <param name="mce"></param>
        /// <returns></returns>
        private static System.Linq.Expressions.MethodCallExpression ResourceQueryOperatorCall(System.Linq.Expressions.Expression source, MethodCallExpression mce)
        {
            List<System.Linq.Expressions.Expression> exprs = new List<System.Linq.Expressions.Expression>();
            exprs.Add(source);
            exprs.AddRange(mce.Arguments.Skip(1));
            return System.Linq.Expressions.Expression.Call(mce.Method, exprs.ToArray());
        }

        private IEnumerable<Entity> GetQueryResult(string operation, IDictionary<string, object> parameters)
        {
            string dataMember = operation.Replace("Get", "");
            PropertyInfo pi = typeof(Cities.CityData).GetProperty(dataMember);
            return ((IEnumerable)pi.GetValue(citiesData, null)).Cast<Entity>();
        }
    }
}
