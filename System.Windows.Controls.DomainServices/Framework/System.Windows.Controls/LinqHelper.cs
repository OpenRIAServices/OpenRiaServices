using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Client;
using System.Windows.Common;

namespace System.Windows.Controls
{
    using Expression = System.Linq.Expressions.Expression;

    /// <summary>
    /// Utility class used for all Linq-related tasks
    /// </summary>
    internal static class LinqHelper
    {
        #region Static Fields and Constants

        private static readonly ConstantExpression nullLiteral = Expression.Constant(null);

        private static readonly Dictionary<Type, IEnumerable<Type>> supportedConversions = new Dictionary<Type, IEnumerable<Type>>
        {
            { typeof(Byte),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single), typeof(UInt64), typeof(Int64), typeof(UInt32), typeof(Int32), typeof(UInt16), typeof(Int16) } },
            { typeof(UInt16),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single), typeof(UInt64), typeof(Int64), typeof(UInt32), typeof(Int32), typeof(Int16) } },
            { typeof(UInt32),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single), typeof(UInt64), typeof(Int64) } },
            { typeof(UInt64),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single) } },
            { typeof(SByte),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single), typeof(Int64), typeof(Int32), typeof(Int16) } },
            { typeof(Int16),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single), typeof(Int64), typeof(Int32) } },
            { typeof(Int32),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single), typeof(Int64) } },
            { typeof(Int64),
                new Type[] { typeof(Decimal), typeof(Double), typeof(Single) } },
            { typeof(Single),
                new Type[] { typeof(Double) } },
        };

        private static readonly Dictionary<Type, IEnumerable<FilterOperator>> supportedOperators = new Dictionary<Type, IEnumerable<FilterOperator>>
        {
            { typeof(IComparable), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(String), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan, FilterOperator.StartsWith, FilterOperator.EndsWith, FilterOperator.Contains, FilterOperator.IsContainedIn } },
            { typeof(Uri), new FilterOperator[] { FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo } },
            { typeof(Nullable<Boolean>), new FilterOperator[] { FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo } },
            { typeof(Nullable<Byte>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<SByte>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Int16>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<UInt16>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Int32>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<UInt32>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Int64>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<UInt64>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Single>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Double>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Decimal>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<DateTime>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<DateTimeOffset>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<TimeSpan>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Char>), new FilterOperator[] { FilterOperator.IsLessThan, FilterOperator.IsLessThanOrEqualTo, FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo, FilterOperator.IsGreaterThanOrEqualTo, FilterOperator.IsGreaterThan } },
            { typeof(Nullable<Guid>), new FilterOperator[] { FilterOperator.IsEqualTo, FilterOperator.IsNotEqualTo } },
        };

        #endregion Static Fields and Constants

        #region Public Methods

        /// <summary>
        /// Produces the Linq expression that represents a particular FilterDescriptor
        /// </summary>
        /// <param name="type">Entity type</param>
        /// <param name="propertyPath">Left operand: Property on the entity type</param>
        /// <param name="filterOperator">One of the FilterOperator enum value</param>
        /// <param name="value">Right operand</param>
        /// <param name="isCaseSensitive">Boolean that specifies if the string operations are case sensitive or not</param>
        /// <returns>Resulting linq expression</returns>
        /// <exception cref="ArgumentException">When a filter descriptor references a property that could not be found.</exception>
        /// <exception cref="ArgumentException">When an exception occurs attempting to evaluate a filter descriptor.</exception>
        /// <exception cref="ArgumentException">When the supplied filter value has a type that cannot be compared to the property type.</exception>
        /// <exception cref="NotSupportedException">When attempting to use a property type/operator pair that is not supported.</exception>
        public static Expression BuildFilterExpression(
            Type type,
            string propertyPath,
            FilterOperator filterOperator,
            object value,
            bool isCaseSensitive)
        {
            Debug.Assert(type != null, "Unexpected null type");
            Debug.Assert(propertyPath != null, "Unexpected null propertyPath");

            Expression filterExpression = null;
            PropertyInfo pi;
            Expression propertyExpression;
            Expression valueExpression;

            try
            {
                pi = type.GetPropertyInfo(propertyPath);

                if (pi == null)
                {
                    throw new ArgumentException(string.Format(
                                CultureInfo.InvariantCulture,
                                CommonResources.PropertyNotFound,
                                propertyPath,
                                type.GetTypeName()));
                }
                // TODO: Remove this check.
                // It's a duplicate of one done in the DDS, but it's still required for the BuildFilterExpressions Test.
                else if (!IsSupportedOperator(pi.PropertyType, filterOperator))
                {
                    throw new NotSupportedException(string.Format(
                                CultureInfo.InvariantCulture,
                                DomainDataSourceResources.FilterNotSupported,
                                propertyPath,
                                type.GetTypeName(),
                                pi.PropertyType.GetTypeName(),
                                filterOperator));
                }

                propertyExpression = GenerateProperty(type, propertyPath, Expression.Parameter(type, string.Empty));
                valueExpression = GenerateConstant(value);
                Debug.Assert(propertyExpression != null, "Unexpected null propertyExpression in LinqHelper.BuildFilterExpression");
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.CannotEvaluateDescriptor,
                        propertyPath),
                        ex);
            }

            try
            {
                bool isEquality = filterOperator == FilterOperator.IsEqualTo || filterOperator == FilterOperator.IsNotEqualTo;
                if (isEquality && !propertyExpression.Type.IsValueType && !valueExpression.Type.IsValueType)
                {
                    if (propertyExpression.Type != valueExpression.Type)
                    {
                        if (propertyExpression.Type.IsAssignableFrom(valueExpression.Type))
                        {
                            valueExpression = Expression.Convert(valueExpression, propertyExpression.Type);
                        }
                        else if (valueExpression.Type.IsAssignableFrom(propertyExpression.Type))
                        {
                            propertyExpression = Expression.Convert(propertyExpression, valueExpression.Type);
                        }
                        else
                        {
                            throw new ArgumentException(string.Format(
                                CultureInfo.InvariantCulture,
                                DomainDataSourceResources.IncompatibleOperands,
                                filterOperator.ToString(),
                                propertyExpression.Type.GetTypeName(),
                                valueExpression.Type.GetTypeName()));
                        }
                    }
                }
                else if (propertyExpression.Type.IsEnumType())
                {
                    // Convert the value to compare to the underlying type of the enum,
                    // preserving nullable and following the same rules the C# compiler does.
                    // Examples:
                    //    p.Enum > Enum.A         => p.Enum > 1
                    //    p.Enum > null           => p.Enum > Convert(null, Nullable<int>)
                    //    p.NullableEnum > Enum.A => p.Enum > Convert(Enum.A, Nullable<int>)
                    //    p.NullableEnum > null   => p.Enum > Convert(Enum.A, Nullable<int>)
                    Type underlyingType = Enum.GetUnderlyingType(TypeUtility.GetNonNullableType(propertyExpression.Type));
                    bool propertyIsNullable = propertyExpression.Type.IsNullableType();
                    if (propertyIsNullable)
                    {
                        underlyingType = typeof(Nullable<>).MakeGenericType(underlyingType);
                    }
                    if (valueExpression.Type != underlyingType)
                    {
                        if (value != null && !propertyIsNullable)
                        {
                            // convert to the underlying value and create a new constant
                            value = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                            valueExpression = GenerateConstant(value);
                        }
                        else
                        {
                            // for nulls or comparisons against a nullable enum, we inject
                            // a conversion
                            valueExpression = Expression.Convert(valueExpression, underlyingType);
                        }
                    }

                    // Now that we've converted the enum value, we inject the appropriate conversion
                    // on the property expression
                    if (propertyExpression.Type != valueExpression.Type)
                    {
                        Expression e;
                        if ((e = PromoteExpression(propertyExpression, valueExpression.Type, true)) != null)
                        {
                            propertyExpression = e;
                        }
                        else
                        {
                            throw new ArgumentException(string.Format(
                                CultureInfo.InvariantCulture,
                                DomainDataSourceResources.IncompatibleOperands,
                                filterOperator.ToString(),
                                propertyExpression.Type.GetTypeName(),
                                valueExpression.Type.GetTypeName()));
                        }
                    }
                }
                else if (pi.PropertyType.IsNullableType() && propertyExpression.Type != valueExpression.Type)
                {
                    ConstantExpression ce = valueExpression as ConstantExpression;
                    if (ce != null)
                    {
                        valueExpression = Expression.Constant(ce.Value, propertyExpression.Type);
                    }
                }

                filterExpression = BuildFilterExpression(propertyExpression, filterOperator, valueExpression, isCaseSensitive);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.IncompatibleOperands,
                        filterOperator.ToString(),
                        propertyExpression.Type.GetTypeName(),
                        (valueExpression == nullLiteral) ? "null" : valueExpression.Type.GetTypeName()),
                        ex);
            }

            return filterExpression;
        }

        /// <summary>
        /// Produces the Linq expression representing the entire filter descriptors collection.
        /// </summary>
        /// <param name="filterDescriptors">Collection of filters</param>
        /// <param name="filterOperator">The operator used to combine filters</param>
        /// <param name="expressionCache">Cache for storing built expressions</param>
        /// <returns>Produced linq expression, which can be <c>null</c> if there are no filter descriptors.</returns>
        public static Expression BuildFiltersExpression(
            FilterDescriptorCollection filterDescriptors,
            FilterDescriptorLogicalOperator filterOperator,
            ExpressionCache expressionCache)
        {
            Debug.Assert(filterDescriptors != null, "Unexpected null filterDescriptors");

            Expression filtersExpression = null;

            foreach (FilterDescriptor filterDescriptor in filterDescriptors)
            {
                // Ignored filters will not have a cache entry
                if (expressionCache.ContainsKey(filterDescriptor))
                {
                    Expression filterExpression = expressionCache[filterDescriptor];

                    if (filtersExpression == null)
                    {
                        filtersExpression = filterExpression;
                    }
                    else if (filterOperator == FilterDescriptorLogicalOperator.And)
                    {
                        filtersExpression = Expression.AndAlso(filtersExpression, filterExpression);
                    }
                    else
                    {
                        filtersExpression = Expression.OrElse(filtersExpression, filterExpression);
                    }
                }
            }

            return filtersExpression;
        }

        /// <summary>
        /// Builds the Linq Expression for the provided propertyPath and type properties
        /// </summary>
        /// <param name="type">Type that exposes the property</param>
        /// <param name="propertyPath">Public property. Can be nested.</param>
        /// <returns>Resulting property expression</returns>
        /// <exception cref="ArgumentException">When property is not found on the specified type.</exception>
        public static Expression BuildPropertyExpression(Type type, string propertyPath)
        {
            Debug.Assert(type != null, "Unexpected null type");
            Debug.Assert(propertyPath != null, "Unexpected null propertyPath");

            Expression propertyExpression = GenerateProperty(type, propertyPath, Expression.Parameter(type, string.Empty));

            if (propertyExpression == null)
            {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    CommonResources.PropertyNotFound,
                    propertyPath,
                    type.GetTypeName()));
            }

            return propertyExpression;
        }

        /// <summary>
        /// Gets an <see cref="EntityQuery{T}" /> method by the specified name.
        /// </summary>
        /// <param name="methodName">The name of the method to find and return.</param>
        /// <returns>The <see cref="MethodInfo"/> from <see cref="EntityQuery{T}" />
        /// that matches the specified name, is generic, with 2 parameters, and accepting
        /// and <see cref="EntityQuery{T}" /> as the first parameter.</returns>
        private static MethodInfo GetEntityQueryMethod(string methodName)
        {
            // TODO: JSH - Cache the MethodInfo
            IEnumerable<MethodInfo> orderByMethods = from method in typeof(EntityQueryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                     where method.Name == methodName && method.IsGenericMethod
                                                     let parameters = method.GetParameters()
                                                     where parameters.Length == 2
                                                         && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(EntityQuery<>)
                                                     select method;

            Debug.Assert(orderByMethods.Count() == 1, "There should be exactly 1 matching method for " + methodName);
            return orderByMethods.Single();
        }

        /// <summary>
        /// Composes an <see cref="EntityQuery" /> for sorting and grouping purposes.
        /// </summary>
        /// <param name="source">The queryable source.</param>
        /// <param name="groupDescriptors">The group descriptors.</param>
        /// <param name="sortDescriptors">The sort descriptors.</param>
        /// <param name="expressionCache">Cache for storing built expressions</param>
        /// <returns>The composed <see cref="EntityQuery" />.</returns>
        public static EntityQuery OrderBy(
            EntityQuery source,
            GroupDescriptorCollection groupDescriptors,
            SortDescriptorCollection sortDescriptors,
            ExpressionCache expressionCache)
        {
            Debug.Assert(source != null, "Unexpected null source");
            Debug.Assert(sortDescriptors != null, "Unexpected null sortDescriptors");
            Debug.Assert(groupDescriptors != null, "Unexpected null groupDescriptors");

            bool hasOrderBy = false;

            // check the GroupDescriptors first
            foreach (GroupDescriptor groupDescriptor in groupDescriptors)
            {
                if (groupDescriptor != null && groupDescriptor.PropertyPath != null)
                {
                    Debug.Assert(expressionCache.ContainsKey(groupDescriptor), "There should be a cached group expression");

                    // check to see if we sort by the same parameter in desc order
                    bool sortAsc = true;
                    foreach (SortDescriptor sortDescriptor in sortDescriptors)
                    {
                        if (sortDescriptor != null)
                        {
                            string sortDescriptorPropertyPath = sortDescriptor.PropertyPath;
                            string groupDescriptorPropertyPath = groupDescriptor.PropertyPath;

                            if (sortDescriptorPropertyPath != null &&
                                sortDescriptorPropertyPath.Equals(groupDescriptorPropertyPath))
                            {
                                if (sortDescriptor.Direction == ListSortDirection.Descending)
                                {
                                    sortAsc = false;
                                }

                                break;
                            }
                        }
                    }

                    string orderMethodName = (!hasOrderBy ? "OrderBy" : "ThenBy");
                    if (!sortAsc)
                    {
                        orderMethodName += "Descending";
                    }

                    source = OrderBy(source, orderMethodName, expressionCache[groupDescriptor]);
                    hasOrderBy = true;
                }
            }

            // then check the SortDescriptors
            foreach (SortDescriptor sortDescriptor in sortDescriptors)
            {
                if (sortDescriptor != null)
                {
                    Debug.Assert(expressionCache.ContainsKey(sortDescriptor), "There should be a cached sort expression");

                    string orderMethodName = (!hasOrderBy ? "OrderBy" : "ThenBy");
                    if (sortDescriptor.Direction == ListSortDirection.Descending)
                    {
                        orderMethodName += "Descending";
                    }

                    source = OrderBy(source, orderMethodName, expressionCache[sortDescriptor]);
                    hasOrderBy = true;
                }
            }

            return source;
        }

        /// <summary>
        /// Compose an OrderBy, ThenBy, OrderByDescending, or ThenByDescending clause onto an EntityQuery.
        /// </summary>
        /// <param name="source">The source EntityQuery</param>
        /// <param name="orderMethodName">The order method name to use: OrderBy, ThenBy, OrderByDescending, or ThenByDescending.</param>
        /// <param name="sortExpression">The expression to use for sorting.</param>
        /// <returns>The composed EntityQuery.</returns>
        private static EntityQuery OrderBy(EntityQuery source, string orderMethodName, Expression sortExpression)
        {
            LambdaExpression lambda = Expression.Lambda(sortExpression, new ParameterExpression[] { Expression.Parameter(source.EntityType, string.Empty) });

            MethodInfo orderBy = GetEntityQueryMethod(orderMethodName);
            MethodInfo orderByT = orderBy.MakeGenericMethod(source.EntityType, sortExpression.Type);

            return (EntityQuery)orderByT.Invoke(null, new object[] { source, lambda });
        }

        /// <summary>
        /// Modifies the provided EntityQuery to perform a Skip operation.
        /// </summary>
        /// <param name="source">The EntityQuery to compose.</param>
        /// <param name="count">The number of items to skip.</param>
        /// <returns>The composed EntityQuery.</returns>
        public static EntityQuery Skip(EntityQuery source, int count)
        {
            Debug.Assert(source != null, "Unexpected null source");
            MethodInfo skip = GetEntityQueryMethod("Skip");
            MethodInfo skipT = skip.MakeGenericMethod(source.EntityType);

            return (EntityQuery)skipT.Invoke(null, new object[] { source, count });
        }

        /// <summary>
        /// Modifies the provided EntityQuery to perform a Take operation.
        /// </summary>
        /// <param name="source">The EntityQuery to compose.</param>
        /// <param name="count">The number of items to take.</param>
        /// <returns>The composed EntityQuery.</returns>
        public static EntityQuery Take(EntityQuery source, int count)
        {
            Debug.Assert(source != null, "Unexpected null source");
            MethodInfo take = GetEntityQueryMethod("Take");
            MethodInfo takeT = take.MakeGenericMethod(source.EntityType);

            return (EntityQuery)takeT.Invoke(null, new object[] { source, count });
        }

        /// <summary>
        /// Modifies the provided EntityQuery to restrict the result set according to the <paramref name="filtersExpression"/> argument.
        /// </summary>
        /// <param name="source">EntityQuery to modify.</param>
        /// <param name="filtersExpression">Expression representing the filter to apply.</param>
        /// <returns>Composed EntityQuery</returns>
        public static EntityQuery Where(EntityQuery source, Expression filtersExpression)
        {
            Debug.Assert(source != null, "Unexpected null source");
            Debug.Assert(filtersExpression != null, "Unexpected null filtersExpression");

            MethodInfo where = GetEntityQueryMethod("Where");
            MethodInfo whereT = where.MakeGenericMethod(source.EntityType);

            LambdaExpression lambda = Expression.Lambda(filtersExpression, new ParameterExpression[] { Expression.Parameter(source.EntityType, string.Empty) });
            return (EntityQuery)whereT.Invoke(null, new object[] { source, lambda });
        }

        #endregion Public Methods

        #region Private Methods

        private static Expression BuildFilterExpression(
            Expression propertyExpression,
            FilterOperator filterOperator,
            Expression valueExpression,
            bool isCaseSensitive)
        {
            Expression filterExpression = null;

            if (propertyExpression.Type == typeof(string) && !isCaseSensitive)
            {
                propertyExpression = GenerateToLowerCall(propertyExpression);
                // ToLower cannot be called on a null expression
                if (valueExpression != nullLiteral)
                {
                    valueExpression = GenerateToLowerCall(valueExpression);
                }
            }

            switch (filterOperator)
            {
                case FilterOperator.IsEqualTo:
                    filterExpression = GenerateEqual(propertyExpression, valueExpression);
                    break;
                case FilterOperator.IsGreaterThan:
                    filterExpression = GenerateGreaterThan(propertyExpression, valueExpression);
                    break;
                case FilterOperator.IsGreaterThanOrEqualTo:
                    filterExpression = GenerateGreaterThanEqual(propertyExpression, valueExpression);
                    break;
                case FilterOperator.IsLessThan:
                    filterExpression = GenerateLessThan(propertyExpression, valueExpression);
                    break;
                case FilterOperator.IsLessThanOrEqualTo:
                    filterExpression = GenerateLessThanEqual(propertyExpression, valueExpression);
                    break;
                case FilterOperator.IsNotEqualTo:
                    filterExpression = GenerateNotEqual(propertyExpression, valueExpression);
                    break;
                case FilterOperator.StartsWith:
                    filterExpression = GenerateStartsWith(propertyExpression, valueExpression);
                    break;
                case FilterOperator.EndsWith:
                    filterExpression = GenerateEndsWith(propertyExpression, valueExpression);
                    break;
                case FilterOperator.Contains:
                    filterExpression = GenerateContains(propertyExpression, valueExpression);
                    break;
                case FilterOperator.IsContainedIn:
                    filterExpression = GenerateIsContainedIn(propertyExpression, valueExpression);
                    break;
            }

            return filterExpression;
        }

        private static bool UseCompareMethod(Expression left, Expression right)
        {
            Type underlyingType = UnwrapConversions(left).Type;
            if (underlyingType.IsEnumType())
            {
                // Enum.Compare is not supported
                return false;
            }
            return left.Type == right.Type && typeof(IComparable).IsAssignableFrom(left.Type);
        }

        private static Expression UnwrapConversions(Expression expr)
        {
            while (expr.NodeType == ExpressionType.Convert)
            {
                expr = ((UnaryExpression)expr).Operand;
            }
            return expr;
        }

        private static Expression GenerateCompareMethod(Expression left, Expression right)
        {
            if (right != nullLiteral && right.Type != left.Type)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.IncompatibleOperands,
                    "CompareTo",
                    left.Type.GetTypeName(),
                    right.Type.GetTypeName()));
            }
            else if (right == nullLiteral && left.Type.IsValueType)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.IncompatibleOperands,
                    "CompareTo",
                    left.Type.GetTypeName(),
                    "null"));
            }

            if (left.Type == typeof(string))
            {
                // For strings, use the static compare method
                MethodInfo compare = typeof(string).GetMethod("Compare", new[] { left.Type, right.Type });
                return Expression.Call(null, compare, left, right);
            }
            else
            {
                // Otherwise use the IComparable.CompareTo method on the property
                MethodInfo compare = left.Type.GetMethod("CompareTo", new[] { right.Type });
                return Expression.Call(left, compare, new[] { right });
            }
        }

        private static ConstantExpression GenerateConstant(object value)
        {
            if (value == null)
            {
                return nullLiteral;
            }

            return Expression.Constant(value);
        }

        private static Expression GenerateEqual(Expression left, Expression right)
        {
            if (UseCompareMethod(left, right))
            {
                return Expression.Equal(
                    GenerateCompareMethod(left, right),
                    Expression.Constant(0));
            }

            return Expression.Equal(left, right);
        }

        private static Expression GenerateNotEqual(Expression left, Expression right)
        {
            if (UseCompareMethod(left, right))
            {
                return Expression.NotEqual(
                    GenerateCompareMethod(left, right),
                    Expression.Constant(0));
            }

            return Expression.NotEqual(left, right);
        }

        private static Expression GenerateGreaterThan(Expression left, Expression right)
        {
            if (UseCompareMethod(left, right))
            {
                return Expression.GreaterThan(
                    GenerateCompareMethod(left, right),
                    Expression.Constant(0));
            }

            return Expression.GreaterThan(left, right);
        }

        private static Expression GenerateGreaterThanEqual(Expression left, Expression right)
        {
            if (UseCompareMethod(left, right))
            {
                return Expression.GreaterThanOrEqual(
                    GenerateCompareMethod(left, right),
                    Expression.Constant(0));
            }

            return Expression.GreaterThanOrEqual(left, right);
        }

        private static Expression GenerateLessThan(Expression left, Expression right)
        {
            if (UseCompareMethod(left, right))
            {
                return Expression.LessThan(
                    GenerateCompareMethod(left, right),
                    Expression.Constant(0));
            }

            return Expression.LessThan(left, right);
        }

        private static Expression GenerateLessThanEqual(Expression left, Expression right)
        {
            if (UseCompareMethod(left, right))
            {
                return Expression.LessThanOrEqual(
                    GenerateCompareMethod(left, right),
                    Expression.Constant(0));
            }

            return Expression.LessThanOrEqual(left, right);
        }

        private static Expression GenerateStartsWith(Expression left, Expression right)
        {
            return Expression.Equal(
                GenerateMethodCall("StartsWith", left, right),
                Expression.Constant(true));
        }

        private static Expression GenerateEndsWith(Expression left, Expression right)
        {
            return Expression.Equal(
                GenerateMethodCall("EndsWith", left, right),
                Expression.Constant(true));
        }

        private static Expression GenerateContains(Expression left, Expression right)
        {
            return Expression.Equal(
                GenerateMethodCall("Contains", left, right),
                Expression.Constant(true));
        }

        private static Expression GenerateIsContainedIn(Expression left, Expression right)
        {
            return Expression.Equal(
                GenerateMethodCall("Contains", right, left),
                Expression.Constant(true));
        }

        private static Expression GenerateMethodCall(string methodName, Expression left, Expression right)
        {
            return Expression.Call(left, GetMethod(methodName, left, right), new[] { right });
        }

        private static MethodInfo GetMethod(string methodName, Expression left, Expression right)
        {
            MethodInfo method = left.Type.GetMethod(methodName, new[] { right.Type });

            if (method == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.MemberNotFound,
                    left.Type.Name,
                    DomainDataSourceResources.Method,
                    methodName));
            }

            return method;
        }

        /// <summary>
        /// Generates a Linq property expression given a type and a property path
        /// </summary>
        /// <param name="type">Type that contains the property provided</param>
        /// <param name="propertyPath">Property path that can be dotted or not.</param>
        /// <param name="instance">Root expression</param>
        /// <returns>Resulting property expression if path is valid. Null otherwise.</returns>
        private static Expression GenerateProperty(Type type, string propertyPath, Expression instance)
        {
            Debug.Assert(type != null, "Unexpected null type in LinqHelper.GenerateProperty");
            Debug.Assert(instance != null, "Unexpected null instance in LinqHelper.GenerateProperty");
            Expression propertyExpression = instance;
            if (!String.IsNullOrEmpty(propertyPath))
            {
                string[] propertyNames = propertyPath.Split(TypeHelper.PropertyNameSeparator);
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    PropertyInfo propertyInfo = type.GetProperty(propertyNames[i]);
                    if (propertyInfo == null)
                    {
                        return null;
                    }

                    propertyExpression = Expression.Property(propertyExpression, propertyInfo);
                    type = propertyInfo.PropertyType;
                }
            }

            return propertyExpression;
        }

        private static Expression GenerateToLowerCall(Expression expression)
        {
            return Expression.Call(expression, GetParameterlessMethod("ToLower", expression));
        }

        private static MethodInfo GetParameterlessMethod(string methodName, Expression expression)
        {
            return expression.Type.GetMethod(methodName, Type.EmptyTypes);
        }

        /// <summary>
        /// Determine if the <paramref name="source"/> type can be converted
        /// to the <paramref name="target"/> type.
        /// </summary>
        /// <param name="source">The source type.</param>
        /// <param name="target">The desired target type.</param>
        /// <returns>Whether or not the source <see cref="Type"/> can be
        /// converted to the target <see cref="Type"/>.</returns>
        private static bool IsSupportedConversion(Type source, Type target)
        {
            if (source == target)
            {
                return true;
            }

            if (!target.IsValueType)
            {
                return target.IsAssignableFrom(source);
            }

            Type coreSource = source.GetNonNullableType();
            Type coreTarget = target.GetNonNullableType();

            // If the source is nullable, but the target isn't,
            // then we cannot convert.
            if (coreSource != source && coreTarget == target)
            {
                return false;
            }

            // Enums will be compared using the underlying type
            if (coreSource.IsEnum)
            {
                coreSource = Enum.GetUnderlyingType(coreSource);
            }

            if (coreTarget.IsEnum)
            {
                coreTarget = Enum.GetUnderlyingType(coreTarget);
            }

            return (coreSource == coreTarget) || (supportedConversions.ContainsKey(coreSource) && supportedConversions[coreSource].Contains(coreTarget));
        }

        /// <summary>
        /// Determine if the <paramref name="filterOperator"/> specified is supported for
        /// the <paramref name="type"/> specified.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the property to be filtered.</param>
        /// <param name="filterOperator">The <see cref="FilterOperator"/> to filter with.</param>
        /// <returns>Whether or not the <paramref name="type"/> can be filtered with <paramref name="filterOperator"/>.</returns>
        internal static bool IsSupportedOperator(Type type, FilterOperator filterOperator)
        {
            // We check for support against the underlying type of any enum, including for nullables
            return supportedOperators.Any(p => p.Key.IsAssignableFrom(type.GetUnderlyingEnumType()) && p.Value.Contains(filterOperator));
        }

        private static Expression PromoteExpression(Expression expr, Type type, bool exact)
        {
            if (expr.Type == type)
            {
                return expr;
            }

            ConstantExpression ce = expr as ConstantExpression;
            if (ce != null && ce == nullLiteral)
            {
                if (!type.IsValueType || type.IsNullableType())
                {
                    return Expression.Constant(null, type);
                }
            }

            if (IsSupportedConversion(expr.Type, type))
            {
                if (type.IsValueType || exact)
                {
                    return Expression.Convert(expr, type);
                }

                return expr;
            }

            return null;
        }

        #endregion Private Methods
    }
}
