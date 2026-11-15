using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices;
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;

#nullable enable

namespace System.Linq.Dynamic
{
    /// <summary>
    /// Used to deserialize a set of string based query operations into expressions and
    /// compose them over a specified query.
    /// </summary>
    internal static class QueryDeserializer
    {
        /// <summary>The maximum number of client query operators.</summary>
        internal const int MaxQueryParts = 64;
        /// <summary>The maximum combined length of client query expressions.</summary>
        internal const int MaxQueryLength = 4 * ExpressionParser.MaxExpressionLength;

        internal static IQueryable Deserialize(DomainServiceDescription domainServiceDescription, IQueryable query, IEnumerable<ServiceQueryPart> queryParts, QueryLimits limits)
        {
            return Deserialize(domainServiceDescription, query, queryParts, new DefaultQueryResolver(), limits);
        }

        internal static IQueryable Deserialize(DomainServiceDescription domainServiceDescription, IQueryable query, IEnumerable<ServiceQueryPart> queryParts, QueryResolver queryResolver, QueryLimits limits)
        {
            ArgumentNullException.ThrowIfNull(limits);
            int parts = 0;
            long length = 0;
            foreach (ServiceQueryPart part in queryParts)
            {
                length += part.Expression?.Length ?? 0;
                if (++parts > MaxQueryParts || length > MaxQueryLength)
                    throw new ParseException(Resource.ExpressionTooComplex, 0);
                switch (part.QueryOperator)
                {
                    case "where":
                        ArgumentNullException.ThrowIfNull(part.Expression);
                        query = DynamicQueryable.Where(query, part.Expression, queryResolver, limits);
                        break;
                    case "orderby":
                        ArgumentNullException.ThrowIfNull(part.Expression);
                        query = DynamicQueryable.OrderBy(query, part.Expression, queryResolver, limits);
                        break;
                    case "skip":
                        query = DynamicQueryable.Skip(query, Convert.ToInt32(part.Expression, System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case "take":
                        query = DynamicQueryable.Take(query, Convert.ToInt32(part.Expression, System.Globalization.CultureInfo.InvariantCulture));
                        break;
                }
            }

            // Include query operators and lambda/quote wrappers before recursive post-processing.
            ExpressionParser.ValidateExpression(query.Expression, limits.MaxExpressionDepth, MaxQueryParts + 4);

            // Perform any required post processing transformations to the expression tree.
            Expression expr = PostProcessor.Process(domainServiceDescription, query.Expression);
            query = query.Provider.CreateQuery(expr);

            return query;
        }

        /// <summary>
        /// Any expression tree transformations required after query parsing and composition
        /// are performed externally to the actual query parser.
        /// </summary>
        private class PostProcessor : ExpressionVisitor
        {
            private readonly DomainServiceDescription domainServiceDescription;
            private bool isInProjection;

            private PostProcessor(DomainServiceDescription domainServiceDescription)
            {
                ArgumentNullException.ThrowIfNull(domainServiceDescription);

                this.domainServiceDescription = domainServiceDescription;
            }

            internal static Expression Process(DomainServiceDescription domainServiceDescription, Expression expression)
            {
                return new PostProcessor(domainServiceDescription).Visit(expression);
            }

            protected override Expression VisitMember(MemberExpression m)
            {
                if (!this.isInProjection)
                {
                    if (m.Expression != null && (m.Member.MemberType is MemberTypes.Property or MemberTypes.Field))
                    {
                        Type declaringType = m.Expression.Type;
                        PropertyDescriptor? pd = m.Member.MemberType == MemberTypes.Property
                            ? TypeDescriptor.GetProperties(declaringType)[m.Member.Name]
                            : null;

                        // Let properties for which no PropertyDescriptors exist go through. This happens when we 
                        // deal with structs.
                        bool requiresValidation = !(pd == null && declaringType.IsValueType);
                        if (requiresValidation && !this.IsVisible(pd))
                        {
                            if (!PostProcessor.IsProjectionPath(m))
                            {
                                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.UnknownPropertyOrField, m.Member.Name, ExpressionParser.GetTypeName(declaringType)));
                            }
                            else
                            {
                                this.isInProjection = true;
                                Expression expr = base.VisitMember(m);
                                this.isInProjection = false;
                                return expr;
                            }
                        }
                    }
                }

                return base.VisitMember(m);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (!this.isInProjection && m.Object != null && m.Method.IsSpecialName && m.Method.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    Type declaringType = m.Method.DeclaringType!;
                    // Framework collection indexers are not represented by PropertyDescriptors.
                    // Note: IEnumerable means we detect IList, IList<>, IReadOnlyList<> as well as corresponding Dictionary types
                    bool isFrameworkIndexer = TypeUtility.IsSystemAssembly(declaringType.Assembly)
                        && (declaringType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(declaringType));
                    if (!isFrameworkIndexer)
                    {
                        string propertyName = m.Method.Name.Substring(4);
                        Type receiverType = m.Object.Type;
                        PropertyDescriptor? pd = TypeDescriptor.GetProperties(receiverType)[propertyName];
                        if (!this.IsVisible(pd))
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.UnknownPropertyOrField, propertyName, ExpressionParser.GetTypeName(receiverType)));
                        }
                    }
                }

                return base.VisitMethodCall(m);
            }

            private bool IsVisible(PropertyDescriptor? pd)
            {
                if (pd == null || pd.Attributes[typeof(ExcludeAttribute)] is ExcludeAttribute)
                {
                    return false;
                }

                if (SerializationUtility.IsSerializableDataMember(pd))
                {
                    return true;
                }

                // Even if this property doesn't have [Include], we consider this property to 
                // be visible when its type is a known entity type, as it implies that there 
                // was a query operation for it.
                Type associatedEntityType = TypeUtility.GetElementType(pd.PropertyType);
                if (associatedEntityType != null && this.domainServiceDescription.EntityTypes.Contains(associatedEntityType))
                {
                    return true;
                }

                if (TypeUtility.FindIEnumerable(pd.ComponentType) != null)
                {
                    // REVIEW: Find a better way to deal with this...
                    if (pd.Name.Equals("Count", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsProjectionPath(MemberExpression m)
            {
                while (m.Expression is MemberExpression)
                {
                    m = (MemberExpression)m.Expression;
                }

                PropertyDescriptor? pd = TypeDescriptor.GetProperties(m.Member.DeclaringType!)[m.Member.Name];
                if (pd == null)
                    return false;

                IncludeAttribute? includeAtt = (IncludeAttribute?)pd.Attributes[typeof(IncludeAttribute)];
                return (includeAtt != null && includeAtt.IsProjection);
            }
        }
    }
}
