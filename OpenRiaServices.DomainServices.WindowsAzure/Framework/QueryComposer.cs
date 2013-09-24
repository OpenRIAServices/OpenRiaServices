using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OpenRiaServices.DomainServices.WindowsAzure
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
        /// Splits the input query into two parts; supported and unsupported.
        /// </summary>
        /// <remarks>
        /// Windows Azure Table Storage only supports a subset of the LINQ operators. This
        /// method splits a single query into two parts at the first occurrence of an
        /// unsupported operation. This results in 3 scenarios.
        /// 
        /// 1) The whole query is supported.
        ///     The <paramref name="query"/> will be returned and <paramref name="unsupportedQuery"/> will be <c>null</c>.
        /// 2) The query is split.
        ///     The supported query will be returned and <paramref name="unsupportedQuery"/> will be set.
        /// 3) The whole query is unsupported.
        ///     <c>null</c> will be returned and the <paramref name="unsupportedQuery"/> will be set to the <paramref name="query"/>.
        /// </remarks>
        /// <param name="query">The query to split</param>
        /// <param name="unsupportedQuery">The unsupported part of the query or <c>null</c> if the whole query is supported.</param>
        /// <returns>The supported part of the query or <c>null</c> if the whole query is unsupported.</returns>
        internal static IQueryable Split(IQueryable query, out IQueryable unsupportedQuery)
        {
            return QuerySplitter.Split(query, out unsupportedQuery);
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
                private Expression _root;

                public Visitor(Expression root)
                {
                    this._root = root;
                }

                protected override Expression VisitMethodCall(MethodCallExpression m)
                {
                    if ((m.Arguments.Count > 0 && m.Arguments[0].NodeType == ExpressionType.Constant) &&
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

        /// <summary>
        /// <see cref="ExpressionVisitor"/> that splits a query on the first unsupported LINQ
        /// operator it encounters.
        /// </summary>
        internal class QuerySplitter : ExpressionVisitor
        {
            // This list of query operators supported by Table Storage is taken from 
            // http://msdn.microsoft.com/en-us/library/dd135725.aspx and may need to be
            // updated with future releases
            private static readonly string[] SupportedLinqOperators = new[] { "From", "Where", "Take", "First", "FirstOrDefault" };

            public static IQueryable Split(IQueryable query, out IQueryable unsupportedQuery)
            {
                unsupportedQuery = null;

                if (query == null)
                {
                    return query;
                }

                Visitor visitor = new Visitor(query.ElementType);
                Expression unsupportedExpression = visitor.Visit(query.Expression);

                // Some part of the query is unsupported
                if (visitor.VisitedUnsupportedExpression)
                {
                    if (visitor.SupportedExpression == null)
                    {
                        // No part of the query is supported
                        unsupportedQuery = query;
                        query = null;
                    }
                    else
                    {
                        // Create an IQueryable using only the supported operations
                        query = query.Provider.CreateQuery(visitor.SupportedExpression);

                        // Rebase the unsupported operations
                        IQueryable queryRoot = Array.CreateInstance(query.ElementType, 0).AsQueryable();
                        unsupportedQuery = queryRoot.Provider.CreateQuery(unsupportedExpression);
                    }
                }

                return query;
            }

            private class Visitor : ExpressionVisitor
            {
                private readonly Type _elementType;

                public Visitor(Type elementType)
                {
                    this._elementType = elementType;
                }

                public bool VisitedUnsupportedExpression { get; private set; }
                public MethodCallExpression SupportedExpression { get; private set; }

                protected override Expression VisitMethodCall(MethodCallExpression expression)
                {
                    // The tree we're walking is a reverse of how the expressions would have been specified
                    // We'll start looking once we get to the root
                    MethodCallExpression result = (MethodCallExpression)base.VisitMethodCall(expression);

                    // We're only interested in evaluating LINQ operators. We'll just ignore everything else.
                    if (Visitor.IsLinqOperator(expression))
                    {
                        // We looking for the first unsupported expression
                        if (!QuerySplitter.SupportedLinqOperators.Contains(expression.Method.Name))
                        {
                            // We're only intereted in splitting on the first unsupported expression
                            if (!this.VisitedUnsupportedExpression)
                            {
                                // We'll rebase this node and treat every expression above it as unsupported
                                ConstantExpression expressionRoot = Expression.Constant(Array.CreateInstance(this._elementType, 0).AsQueryable());
                                List<Expression> arguments = new List<Expression> { expressionRoot };
                                arguments.AddRange(result.Arguments.Skip(1));
                                result = Expression.Call(result.Method, arguments.ToArray());
                            }
                            this.VisitedUnsupportedExpression = true;
                        }

                        // As long as we haven't hit an unsupported node, we're still aggregating the expression
                        if (!this.VisitedUnsupportedExpression)
                        {
                            this.SupportedExpression = expression;
                        }
                    }

                    return result;
                }

                private static bool IsLinqOperator(MethodCallExpression expression)
                {
                    return (expression.NodeType == ExpressionType.Call) &&
                        (expression.Arguments.Count > 0) &&
                        (typeof(IQueryable).IsAssignableFrom(expression.Arguments[0].Type));
                }
            }
        }
    }
}
