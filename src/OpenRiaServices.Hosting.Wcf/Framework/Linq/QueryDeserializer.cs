using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices;
using OpenRiaServices.Hosting;
using OpenRiaServices.Server;

namespace System.Linq.Dynamic
{
    /// <summary>
    /// Used to deserialize a set of string based query operations into expressions and
    /// compose them over a specified query.
    /// </summary>
    internal static class QueryDeserializer
    {
        internal static IQueryable Deserialize(DomainServiceDescription domainServiceDescription, IQueryable query, IEnumerable<ServiceQueryPart> queryParts)
        {
            return Deserialize(domainServiceDescription, query, queryParts, new DefaultQueryResolver());
        }

        internal static IQueryable Deserialize(DomainServiceDescription domainServiceDescription, IQueryable query, IEnumerable<ServiceQueryPart> queryParts, QueryResolver queryResolver)
        {
            foreach (ServiceQueryPart part in queryParts)
            {
                switch (part.QueryOperator)
                {
                    case "where":
                        query = DynamicQueryable.Where(query, part.Expression, queryResolver);
                        break;
                    case "orderby":
                        query = DynamicQueryable.OrderBy(query, part.Expression, queryResolver);
                        break;
                    case "skip":
                        query = DynamicQueryable.Skip(query, Convert.ToInt32(part.Expression, System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case "take":
                        query = DynamicQueryable.Take(query, Convert.ToInt32(part.Expression, System.Globalization.CultureInfo.InvariantCulture));
                        break;
                }
            }

            // Perform any required post processing transformations to the
            // expression tree
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
                if (domainServiceDescription == null)
                {
                    throw new ArgumentNullException(nameof(domainServiceDescription));
                }

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
                    if (m.Member.MemberType == MemberTypes.Property)
                    {
                        PropertyDescriptor pd = TypeDescriptor.GetProperties(m.Member.DeclaringType)[m.Member.Name];

                        // Let properties for which no PropertyDescriptors exist go through. This happens when we 
                        // deal with structs.
                        bool requiresValidation = !(pd == null && m.Member.DeclaringType.IsValueType);
                        if (requiresValidation && !this.IsVisible(pd))
                        {
                            if (!PostProcessor.IsProjectionPath(m))
                            {
                                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.UnknownPropertyOrField, pd.Name, ExpressionParser.GetTypeName(pd.ComponentType)));
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

            private bool IsVisible(PropertyDescriptor pd)
            {
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

                PropertyDescriptor pd = TypeDescriptor.GetProperties(m.Member.DeclaringType)[m.Member.Name];
                if (pd == null)
                    return false;

                IncludeAttribute includeAtt = (IncludeAttribute)pd.Attributes[typeof(IncludeAttribute)];
                return (includeAtt != null && includeAtt.IsProjection);
            }
        }
    }
}
