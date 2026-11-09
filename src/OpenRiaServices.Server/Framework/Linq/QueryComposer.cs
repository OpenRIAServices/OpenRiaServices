using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Used to compose two separate queries into a single query
    /// </summary>
    internal static class QueryComposer
    {
        /// <summary>
        /// Composes the specified query with the source provided.
        /// </summary>
        /// <param name="source">The root or source query</param>
        /// <param name="query">The query to compose</param>
        /// <returns>The composed query</returns>
        internal static IQueryable Compose(IQueryable source, IQueryable query)
        {
            return QueryRebaser.Rebase(source, query);
        }

        /// <summary>
        /// Inspects the specified query and removes any trailing Skip and Take paging operators.
        /// </summary>
        /// <param name="query">The query to inspect.</param>
        /// <param name="countQuery">The resulting count query. Null if there is no paging.</param>
        /// <returns>True if a count query is returned, false otherwise.</returns>
        internal static bool TryComposeWithoutPaging(IQueryable query, out IQueryable countQuery)
        {
            Expression countExpr = query.Expression;
            bool hasPaging = false;
            while (countExpr is MethodCallExpression mce
                && mce.Method.DeclaringType == typeof(Queryable)
                && (mce.Method.Name.Equals("take", StringComparison.OrdinalIgnoreCase)
                    || mce.Method.Name.Equals("skip", StringComparison.OrdinalIgnoreCase)))
            {
                countExpr = mce.Arguments[0];
                hasPaging = true;
            }

            countQuery = hasPaging ? query.Provider.CreateQuery(countExpr) : null;
            return hasPaging;
        }

        /// <summary>
        /// If the query operation has a result limit, this operation will compose Take(limit) on top 
        /// of the specified results.
        /// </summary>
        /// <param name="results">The results that may need to be limited.</param>
        /// <param name="queryOperation">The query operation that was invoked to get the results.</param>
        /// <param name="limitedResults">The limited results. It will be <value>null</value> if there is no result limit.</param>
        /// <returns>True if a limited result query is returned, false otherwise.</returns>
        internal static bool TryComposeWithLimit(IEnumerable results, DomainOperationEntry queryOperation, out IEnumerable limitedResults)
        {
            int limit = ((QueryAttribute)queryOperation.OperationAttribute).ResultLimit;
            if (limit > 0)
            {
                IQueryable queryableResult = results.AsQueryable();

                // Compose Take(limit) over the results.
                IQueryable limitQuery = Array.CreateInstance(queryOperation.AssociatedType, 0).AsQueryable();
                limitQuery = limitQuery.Provider.CreateQuery(
                    Expression.Call(
                        typeof(Queryable), "Take",
                        new Type[] { limitQuery.ElementType },
                        limitQuery.Expression, Expression.Constant(limit)));

                limitedResults = QueryComposer.Compose(queryableResult, limitQuery);
                return true;
            }

            limitedResults = null;
            return false;
        }

        /// <summary>
        /// Class used to insert a specified query source into another separate
        /// query, effectively "rebasing" the query source.
        /// </summary>
        internal class QueryRebaser : ExpressionVisitor
        {
            /// <summary>
            /// Rebase the specified query to the specified source
            /// </summary>
            /// <param name="source">The query source</param>
            /// <param name="query">The query to rebase</param>
            /// <returns>Returns the edited query.</returns>
            public static IQueryable Rebase(IQueryable source, IQueryable query)
            {
                Visitor v = new Visitor(source.Expression);
                Expression expr = v.Visit(query.Expression);
                return source.Provider.CreateQuery(expr);
            }

            private class Visitor : ExpressionVisitor
            {
                private readonly Expression _root;

                public Visitor(Expression root)
                {
                    this._root = root;
                }

                protected override Expression VisitMethodCall(MethodCallExpression m)
                {
                    if ((m.Arguments.Count > 0 && m.Arguments[0].NodeType == ExpressionType.Constant) &&
                       (((ConstantExpression)m.Arguments[0]).Value != null) &&
                        (((ConstantExpression)m.Arguments[0]).Value is IQueryable))
                    {
                        // we found the innermost source which we replace with the
                        // specified source
                        List<Expression> exprs = new List<Expression>();
                        exprs.Add(this._root);
                        exprs.AddRange(m.Arguments.Skip(1));
                        return Expression.Call(m.Method, exprs.ToArray());
                    }
                    return base.VisitMethodCall(m);
                }
            }
        }
    }
}
