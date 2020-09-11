using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq.Dynamic
{
    internal partial class ExpressionParser
    {
        /// <summary>
        /// Determines if the method is allowed to be executed based on an allow list.
        /// </summary>
        /// <param name="method">The method to check for access allowance.</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <returns><c>true</c> when the member access is allowed, otherwise <c>false</c>.</returns>
        internal static bool IsMemberAccessAllowed(MethodBase method, Expression[] args)
        {
            var type = GetNonNullableType(method.DeclaringType);
            var parameterTypes = method.GetParameters()
                                       .Select(p => p.ParameterType)
                                       .ToArray();

            if (method.IsConstructor)
            {
                if (type == typeof(String))
                {
                    return
                        ArrayEqual(parameterTypes, new[] { typeof(Char[]), typeof(Int32), typeof(Int32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Char[]) });
                }

                if (type == typeof(DateTime))
                {
                    return
                        ArrayEqual(parameterTypes, new[] { typeof(Int64) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int64), typeof(DateTimeKind) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(DateTimeKind) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(DateTimeKind) });
                }

                if (type == typeof(DateTimeOffset))
                {
                    return
                        ArrayEqual(parameterTypes, new[] { typeof(Int64), typeof(TimeSpan) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(DateTime) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(DateTime), typeof(TimeSpan) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(TimeSpan) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(TimeSpan) });
                }

                if (type == typeof(TimeSpan))
                {
                    return
                        ArrayEqual(parameterTypes, new[] { typeof(Int64) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32) });
                }

                if (type == typeof(Guid))
                {
                    return
                        ArrayEqual(parameterTypes, new[] { typeof(Byte[]) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(UInt32), typeof(UInt16), typeof(UInt16), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int16), typeof(Int16), typeof(Byte[]) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int16), typeof(Int16), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(String) });
                }

                if (type == typeof(Uri))
                {
                    return
                        ArrayEqual(parameterTypes, new[] { typeof(String) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Boolean) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(String), typeof(UriKind) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Uri), typeof(String) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Uri), typeof(String), typeof(Boolean) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Uri), typeof(Uri) });
                }

                if (type == typeof(Decimal))
                {
                    return
                        ArrayEqual(parameterTypes, new[] { typeof(Int32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(UInt32) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int64) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(UInt64) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Single) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Double) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32[]) }) ||
                        ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32), typeof(Int32), typeof(Boolean), typeof(Byte) });
                }
            }
            else
            {
                string methodName = method.Name;
                if (method.DeclaringType == typeof(Char))
                {
                    return
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("IsDigit", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsLetter", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsWhiteSpace", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsUpper", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsLower", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsPunctuation", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsLetterOrDigit", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToUpper", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToUpperInvariant", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToLower", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToLowerInvariant", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsControl", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsControl", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsDigit", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsLetter", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsLetterOrDigit", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsLower", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsNumber", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsNumber", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsPunctuation", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsSeparator", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsSeparator", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsSurrogate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsSurrogate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsSymbol", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsSymbol", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsUpper", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsWhiteSpace", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("GetNumericValue", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("GetNumericValue", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsHighSurrogate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsHighSurrogate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsLowSurrogate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsLowSurrogate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsSurrogatePair", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IsSurrogatePair", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char), typeof(Char) })) ||
                    (methodName.Equals("ConvertFromUtf32", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ConvertToUtf32", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char), typeof(Char) })) ||
                    (methodName.Equals("ConvertToUtf32", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) }));
                }

                if (method.DeclaringType == typeof(String))
                {
                    return
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(StringComparison) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String), typeof(StringComparison) })) ||
                    (methodName.Equals("IsNullOrEmpty", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("IsNullOrWhiteSpace", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Substring", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("Substring", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String), typeof(Boolean) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String), typeof(StringComparison) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32), typeof(Boolean) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32), typeof(StringComparison) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("CompareOrdinal", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String) })) ||
                    (methodName.Equals("CompareOrdinal", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("Contains", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("EndsWith", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("EndsWith", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(StringComparison) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char), typeof(Int32) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(StringComparison) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(StringComparison) })) ||
                    (methodName.Equals("IndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(Int32), typeof(StringComparison) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char), typeof(Int32) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(StringComparison) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(StringComparison) })) ||
                    (methodName.Equals("LastIndexOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32), typeof(Int32), typeof(StringComparison) })) ||
                    (methodName.Equals("StartsWith", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("StartsWith", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(StringComparison) })) ||
                    (methodName.Equals("ToLower", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToLowerInvariant", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToUpper", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToUpperInvariant", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Trim", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Trim", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]) })) ||
                    (methodName.Equals("TrimStart", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]) })) ||
                    (methodName.Equals("TrimEnd", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]) })) ||
                    (methodName.Equals("Insert", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(String) })) ||
                    (methodName.Equals("Remove", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("Remove", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("IndexOfAny", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]) })) ||
                    (methodName.Equals("IndexOfAny", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]), typeof(Int32) })) ||
                    (methodName.Equals("IndexOfAny", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("LastIndexOfAny", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]) })) ||
                    (methodName.Equals("LastIndexOfAny", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]), typeof(Int32) })) ||
                    (methodName.Equals("LastIndexOfAny", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char[]), typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("get_Chars", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) || // String Indexer
                    (methodName.Equals("Concat", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String) })) ||
                    (methodName.Equals("Concat", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String), typeof(String) })) ||
                    (methodName.Equals("Concat", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String), typeof(String), typeof(String) })) ||
                    (
                        methodName.Equals("Replace", StringComparison.OrdinalIgnoreCase) &&
                        (
                            (ArrayEqual(parameterTypes, new[] { typeof(Char), typeof(Char) }) && args.Length == 2 && args[1].NodeType == ExpressionType.Constant) ||
                            (ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String) }) && args.Length == 2 && args[1].NodeType == ExpressionType.Constant && GetStringArgumentLength((ConstantExpression)args[1]) <= 100)
                        )
                    );

                }

                if (method.DeclaringType == typeof(DateTime))
                {
                    return
                    (methodName.Equals("Add", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("AddDays", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddHours", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddMilliseconds", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddMinutes", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddMonths", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("AddSeconds", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddTicks", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("AddYears", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(DateTime), typeof(DateTime) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("DaysInMonth", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(DateTime), typeof(DateTime) })) ||
                    (methodName.Equals("FromBinary", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("FromFileTime", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("FromFileTimeUtc", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("FromOADate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("IsDaylightSavingTime", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("SpecifyKind", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(DateTime), typeof(DateTimeKind) })) ||
                    (methodName.Equals("ToBinary", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("IsLeapYear", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Subtract", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("Subtract", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("ToOADate", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToFileTime", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToFileTimeUtc", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToLocalTime", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToLongDateString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToLongTimeString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToShortDateString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToShortTimeString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToUniversalTime", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0);
                }

                if (method.DeclaringType == typeof(DateTimeOffset))
                {
                    return
                    (methodName.Equals("ToOffset", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("Add", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("AddDays", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddHours", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddMilliseconds", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddMinutes", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddMonths", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("AddSeconds", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("AddTicks", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("AddYears", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(DateTimeOffset), typeof(DateTimeOffset) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTimeOffset) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTimeOffset) })) ||
                    (methodName.Equals("EqualsExact", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTimeOffset) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(DateTimeOffset), typeof(DateTimeOffset) })) ||
                    (methodName.Equals("FromFileTime", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Subtract", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTimeOffset) })) ||
                    (methodName.Equals("Subtract", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("ToFileTime", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToLocalTime", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToUniversalTime", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0);
                }

                if (method.DeclaringType == typeof(TimeSpan))
                {
                    return
                    (methodName.Equals("Add", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan), typeof(TimeSpan) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("FromDays", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Duration", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan), typeof(TimeSpan) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("FromHours", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("FromMilliseconds", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("FromMinutes", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Negate", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("FromSeconds", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Subtract", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(TimeSpan) })) ||
                    (methodName.Equals("FromTicks", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) }));
                }

                if (method.DeclaringType == typeof(Guid))
                {
                    return
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ParseExact", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(String) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Guid) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Guid) })) ||
                    (methodName.Equals("NewGuid", StringComparison.OrdinalIgnoreCase) && method.IsStatic && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) }));
                }

                if (method.DeclaringType == typeof(Uri))
                {
                    return
                    (methodName.Equals("CheckSchemeName", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("FromHex", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("GetLeftPart", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UriPartial) })) ||
                    (methodName.Equals("HexEscape", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsHexDigit", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("IsHexEncoding", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("MakeRelative", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Uri) })) ||
                    (methodName.Equals("MakeRelativeUri", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Uri) })) ||
                    (methodName.Equals("IsBaseOf", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Uri) })) ||
                    (methodName.Equals("GetComponents", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UriComponents), typeof(UriFormat) })) ||
                    (methodName.Equals("IsWellFormedOriginalString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("IsWellFormedUriString", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(UriKind) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Uri), typeof(Uri), typeof(UriComponents), typeof(UriFormat), typeof(StringComparison) })) ||
                    (methodName.Equals("UnescapeDataString", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("EscapeUriString", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("EscapeDataString", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) }));
                }

                if (method.DeclaringType == typeof(SByte))
                {
                    return
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(Byte))
                {
                    return
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) }));
                }

                if (method.DeclaringType == typeof(Int16))
                {
                    return
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(UInt16))
                {
                    return
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(Int32))
                {
                    return
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(UInt32))
                {
                    return
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(Int64))
                {
                    return
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(Boolean))
                {
                    return
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) }));
                }

                if (method.DeclaringType == typeof(UInt64))
                {
                    return
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(Single))
                {
                    return
                    (methodName.Equals("IsInfinity", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("IsNaN", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("IsPositiveInfinity", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("IsNegativeInfinity", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(Double))
                {
                    return
                    (methodName.Equals("IsInfinity", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("IsPositiveInfinity", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("IsNegativeInfinity", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("IsNaN", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) }));
                }

                if (method.DeclaringType == typeof(Decimal))
                {
                    return
                    (methodName.Equals("ToOACurrency", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("FromOACurrency", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("Add", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("Ceiling", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Compare", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("CompareTo", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Divide", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("Equals", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("Floor", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(NumberStyles) })) ||
                    (methodName.Equals("Remainder", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("Multiply", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("Negate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Int32) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(MidpointRounding) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Int32), typeof(MidpointRounding) })) ||
                    (methodName.Equals("Subtract", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Truncate", StringComparison.OrdinalIgnoreCase) && method.IsStatic && ArrayEqual(parameterTypes, new[] { typeof(Decimal) }));
                }

                if (OpenRiaServices.TypeUtility.IsNullableType(method.DeclaringType))
                {
                    return
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0);
                }

                if (method.DeclaringType == typeof(Math))
                {
                    return
                    (methodName.Equals("Acos", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Asin", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Atan", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Atan2", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Double) })) ||
                    (methodName.Equals("Ceiling", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Ceiling", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Cos", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Cosh", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Floor", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Floor", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Sin", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Tan", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Sinh", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Tanh", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Int32) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(MidpointRounding) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Int32), typeof(MidpointRounding) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Int32) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(MidpointRounding) })) ||
                    (methodName.Equals("Round", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Int32), typeof(MidpointRounding) })) ||
                    (methodName.Equals("Truncate", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Truncate", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Sqrt", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Log", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Log10", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Exp", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Pow", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Double) })) ||
                    (methodName.Equals("IEEERemainder", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Double) })) ||
                    (methodName.Equals("Abs", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("Abs", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("Abs", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("Abs", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("Abs", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("Abs", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Abs", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte), typeof(SByte) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte), typeof(Byte) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16), typeof(Int16) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16), typeof(UInt16) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32), typeof(UInt32) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64), typeof(Int64) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64), typeof(UInt64) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single), typeof(Single) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Double) })) ||
                    (methodName.Equals("Max", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte), typeof(SByte) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte), typeof(Byte) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16), typeof(Int16) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16), typeof(UInt16) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32), typeof(UInt32) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64), typeof(Int64) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64), typeof(UInt64) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single), typeof(Single) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Double) })) ||
                    (methodName.Equals("Min", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal), typeof(Decimal) })) ||
                    (methodName.Equals("Log", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double), typeof(Double) })) ||
                    (methodName.Equals("Sign", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("Sign", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("Sign", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("Sign", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("Sign", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("Sign", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("Sign", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("BigMul", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0);
                }

                if (method.DeclaringType == typeof(Convert))
                {
                    return
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToBoolean", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToChar", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToSingle", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToDouble", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToDecimal", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToDateTime", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Boolean) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Char) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(SByte) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt16) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(UInt64) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Single) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Double) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Decimal) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(DateTime) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String) })) ||
                    (methodName.Equals("ToByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToSByte", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToUInt16", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToUInt32", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToUInt64", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(String), typeof(Int32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Byte), typeof(Int32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int16), typeof(Int32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int32), typeof(Int32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && ArrayEqual(parameterTypes, new[] { typeof(Int64), typeof(Int32) })) ||
                    (methodName.Equals("ToString", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0) ||
                    (methodName.Equals("GetHashCode", StringComparison.OrdinalIgnoreCase) && parameterTypes.Length == 0);
                }
            }

            return false;
        }

        private static int GetStringArgumentLength(ConstantExpression argument)
        {
            string stringArgument = argument.Value as string;
            return stringArgument != null ? stringArgument.Length : 0;
        }

        private static bool ArrayEqual(Type[] a, Type[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
