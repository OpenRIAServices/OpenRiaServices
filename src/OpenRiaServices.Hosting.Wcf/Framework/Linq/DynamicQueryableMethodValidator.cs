using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices.Hosting.AspNetCore;

#nullable disable

namespace System.Linq.Dynamic
{
    internal partial class ExpressionParser
    {
        private enum MemberRule
        {
            Denied,
            Allowed,
            NumericFormat,
            ReplaceCharacter,
            ReplaceString
        }

        private delegate bool MemberPolicy(MethodBase method, ParameterInfo[] parameters);

        private static readonly FrozenDictionary<Type, Func<MethodBase, MemberRule>> s_memberPolicies = CreateMemberPolicies();

        private static FrozenDictionary<Type, Func<MethodBase, MemberRule>> CreateMemberPolicies()
        {
            var policies = new Dictionary<Type, Func<MethodBase, MemberRule>>();
            policies.Add(typeof(Char), static method => ResolveRule(method, CharMemberPolicy, false));
            policies.Add(typeof(String), static method => ResolveRule(method, StringMemberPolicy, false));
            policies.Add(typeof(DateTime), static method => ResolveRule(method, DateTimeMemberPolicy, false));
            policies.Add(typeof(DateTimeOffset), static method => ResolveRule(method, DateTimeOffsetMemberPolicy, false));
            policies.Add(typeof(TimeSpan), static method => ResolveRule(method, TimeSpanMemberPolicy, false));
            policies.Add(typeof(Guid), static method => ResolveRule(method, GuidMemberPolicy, false));
            policies.Add(typeof(Uri), static method => ResolveRule(method, UriMemberPolicy, false));
            policies.Add(typeof(SByte), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Byte), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Int16), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(UInt16), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Int32), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(UInt32), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Int64), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Boolean), static method => ResolveRule(method, BooleanMemberPolicy, false));
            policies.Add(typeof(UInt64), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Single), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Double), static method => ResolveRule(method, NumericMemberPolicy, true));
            policies.Add(typeof(Decimal), static method => ResolveRule(method, DecimalMemberPolicy, true));
            policies.Add(typeof(Math), static method => ResolveRule(method, MathMemberPolicy, false));
            policies.Add(typeof(Convert), static method => ResolveRule(method, ConvertMemberPolicy, false));
#if NET
            policies.Add(typeof(DateOnly), static method => ResolveRule(method, DateOnlyMemberPolicy, false));
            policies.Add(typeof(TimeOnly), static method => ResolveRule(method, TimeOnlyMemberPolicy, false));
#endif
            return policies.ToFrozenDictionary();
        }

        private static class MemberRules
        {
            internal static readonly FrozenDictionary<MethodBase, MemberRule> Values = Create();

            private static FrozenDictionary<MethodBase, MemberRule> Create()
            {
                var rules = new Dictionary<MethodBase, MemberRule>();
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (var policy in s_memberPolicies)
                {
                    foreach (MethodBase member in policy.Key.GetMethods(flags).Cast<MethodBase>().Concat(policy.Key.GetConstructors(flags)))
                    {
                        MemberRule rule = policy.Value(member);
                        if (rule != MemberRule.Denied)
                            rules.Add(member, rule);
                    }
                }
                return rules.ToFrozenDictionary();
            }
        }

        /// <summary>Determines whether a bound member and its current arguments are allowed.</summary>
        /// <param name="method">The bound member to check.</param>
        /// <param name="args">The current argument expressions.</param>
        /// <param name="limits">The current query limits.</param>
        /// <returns>Whether the member and its arguments are permitted.</returns>
        internal static bool IsMemberAccessAllowed(MethodBase method, Expression[] args, QueryLimits limits)
        {
            ArgumentNullException.ThrowIfNull(limits);
            MemberRule rule;
            if (!MemberRules.Values.TryGetValue(method, out rule))
            {
                // Reflected inherited members retain their declaring-type policy without growing the table.
                rule = method.ReflectedType != method.DeclaringType ? ResolveByType(method) : NullableMemberRule(method);
            }
            return ApplyMemberRule(rule, args, limits);
        }

        private static MemberRule ResolveByType(MethodBase method)
        {
            return method.DeclaringType != null && s_memberPolicies.TryGetValue(method.DeclaringType, out var resolver)
                ? resolver(method) : NullableMemberRule(method);
        }

        private static MemberRule NullableMemberRule(MethodBase method)
        {
            return !method.IsConstructor && method.DeclaringType != null
                && OpenRiaServices.TypeUtility.IsNullableType(method.DeclaringType)
                && method.Name == "ToString" && method.GetParameters().Length == 0
                ? MemberRule.Allowed : MemberRule.Denied;
        }

        private static MemberRule ResolveRule(MethodBase method, MemberPolicy policy, bool numericFormat)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (!policy(method, parameters))
                return MemberRule.Denied;
            if (numericFormat && !method.IsStatic && method.Name == "ToString" && ParametersMatch(parameters, typeof(string)))
                return MemberRule.NumericFormat;
            if (method.DeclaringType == typeof(string) && method.Name == "Replace")
                return parameters[1].ParameterType == typeof(char) ? MemberRule.ReplaceCharacter : MemberRule.ReplaceString;
            return MemberRule.Allowed;
        }

        private static bool ApplyMemberRule(MemberRule rule, Expression[] args, QueryLimits limits)
        {
            return rule switch
            {
                MemberRule.Allowed => true,
                MemberRule.NumericFormat => IsNumericFormatAllowed(args[0], limits),
                MemberRule.ReplaceCharacter => args.Length == 2 && args[1].NodeType == ExpressionType.Constant,
                MemberRule.ReplaceString => args.Length == 2 && args[1].NodeType == ExpressionType.Constant
                    && GetStringArgumentLength((ConstantExpression)args[1]) <= 100,
                _ => false
            };
        }

        private static bool ParametersMatch(ParameterInfo[] actual, params ReadOnlySpan<Type> expected)
        {
            if (actual.Length != expected.Length)
                return false;
            for (int i = 0; i < actual.Length; i++)
            {
                if (actual[i].ParameterType != expected[i])
                    return false;
            }
            return true;
        }

        private static bool NumericMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return false;
            Type type = method.DeclaringType;
            return method.Name switch
            {
                "ToString" => parameters.Length == 0 || ParametersMatch(parameters, typeof(string)),
                "GetHashCode" => parameters.Length == 0,
                "CompareTo" or "Equals" => ParametersMatch(parameters, type),
                "Parse" => method.IsStatic && (ParametersMatch(parameters, typeof(string))
                    || ParametersMatch(parameters, typeof(string), typeof(NumberStyles))),
                "IsInfinity" or "IsNaN" or "IsPositiveInfinity" or "IsNegativeInfinity" =>
                    (type == typeof(float) || type == typeof(double)) && method.IsStatic && ParametersMatch(parameters, type),
                "Abs" => method.IsStatic && ParametersMatch(parameters, type),
                "Min" or "Max" => method.IsStatic && ParametersMatch(parameters, type, type),
                "Clamp" => method.IsStatic && ParametersMatch(parameters, type, type, type),
                _ => false
            };
        }

        private static bool CharMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return false;
            return method.Name switch
            {
                "GetHashCode" => parameters.Length == 0,
                "Equals" => ParametersMatch(parameters, typeof(Char)),
                "CompareTo" => ParametersMatch(parameters, typeof(Char)),
                "ToString" => parameters.Length == 0
                    || method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "IsDigit" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsLetter" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsWhiteSpace" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsUpper" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsLower" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsPunctuation" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsLetterOrDigit" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "ToUpper" => method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "ToUpperInvariant" => method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "ToLower" => method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "ToLowerInvariant" => method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "IsControl" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsNumber" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsSeparator" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsSurrogate" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsSymbol" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "GetNumericValue" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "GetUnicodeCategory" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsAscii" or "IsAsciiDigit" or "IsAsciiLetter" or "IsAsciiLetterLower" or "IsAsciiLetterUpper"
                    or "IsAsciiLetterOrDigit" or "IsAsciiHexDigit" or "IsAsciiHexDigitLower" or "IsAsciiHexDigitUpper" =>
                    method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "IsBetween" => method.IsStatic && ParametersMatch(parameters, typeof(Char), typeof(Char), typeof(Char)),
                "IsHighSurrogate" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsLowSurrogate" => method.IsStatic && (ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                "IsSurrogatePair" => method.IsStatic && (ParametersMatch(parameters, typeof(String), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Char), typeof(Char))),
                "ConvertFromUtf32" => method.IsStatic && ParametersMatch(parameters, typeof(Int32)),
                "ConvertToUtf32" => method.IsStatic && (ParametersMatch(parameters, typeof(Char), typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))),
                _ => false
            };
        }

        private static bool StringMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Char[]), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Char[]));
            return method.Name switch
            {
                "Equals" => ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison))
                    || method.IsStatic && (ParametersMatch(parameters, typeof(String), typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(String), typeof(StringComparison))),
                "IsNullOrEmpty" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "IsNullOrWhiteSpace" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "GetHashCode" => parameters.Length == 0,
                "Substring" => ParametersMatch(parameters, typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32)),
                "Compare" => method.IsStatic && (ParametersMatch(parameters, typeof(String), typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(String), typeof(Boolean))
                    || ParametersMatch(parameters, typeof(String), typeof(String), typeof(StringComparison))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32), typeof(Boolean))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32), typeof(StringComparison))),
                "CompareTo" => ParametersMatch(parameters, typeof(String)),
                "CompareOrdinal" => method.IsStatic && (ParametersMatch(parameters, typeof(String), typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(String), typeof(Int32), typeof(Int32))),
                "Contains" => ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison))
                    || ParametersMatch(parameters, typeof(Char), typeof(StringComparison)),
                "EndsWith" => ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison)),
                "IndexOf" => ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(Char), typeof(StringComparison))
                    || ParametersMatch(parameters, typeof(Char), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Char), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(StringComparison))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(Int32), typeof(StringComparison)),
                "LastIndexOf" => ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(Char), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Char), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(StringComparison))
                    || ParametersMatch(parameters, typeof(String), typeof(Int32), typeof(Int32), typeof(StringComparison)),
                "StartsWith" => ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison)),
                "ToLower" => parameters.Length == 0,
                "ToLowerInvariant" => parameters.Length == 0,
                "ToUpper" => parameters.Length == 0,
                "ToUpperInvariant" => parameters.Length == 0,
                "ToString" => parameters.Length == 0,
                "Trim" or "TrimStart" or "TrimEnd" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(Char))
                    || ParametersMatch(parameters, typeof(Char[])),
                "Insert" => ParametersMatch(parameters, typeof(Int32), typeof(String)),
                "Remove" => ParametersMatch(parameters, typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32)),
                "IndexOfAny" => ParametersMatch(parameters, typeof(Char[]))
                    || ParametersMatch(parameters, typeof(Char[]), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Char[]), typeof(Int32), typeof(Int32)),
                "LastIndexOfAny" => ParametersMatch(parameters, typeof(Char[]))
                    || ParametersMatch(parameters, typeof(Char[]), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Char[]), typeof(Int32), typeof(Int32)),
                "get_Chars" => ParametersMatch(parameters, typeof(Int32)),
                "Concat" => method.IsStatic && (ParametersMatch(parameters, typeof(String), typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(String), typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(String), typeof(String), typeof(String))),
                "Replace" => ((ParametersMatch(parameters, typeof(Char), typeof(Char)) || ParametersMatch(parameters, typeof(String), typeof(String)))),
                _ => false
            };
        }

        private static bool DateTimeMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int64), typeof(DateTimeKind))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(DateTimeKind))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(DateTimeKind))
#if NET
                    || ParametersMatch(parameters, typeof(DateOnly), typeof(TimeOnly))
                    || ParametersMatch(parameters, typeof(DateOnly), typeof(TimeOnly), typeof(DateTimeKind))
#endif
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(DateTimeKind));
            return method.Name switch
            {
                "Add" => ParametersMatch(parameters, typeof(TimeSpan)),
                "AddDays" => ParametersMatch(parameters, typeof(Double)),
                "AddHours" => ParametersMatch(parameters, typeof(Double)),
                "AddMilliseconds" => ParametersMatch(parameters, typeof(Double)),
                "AddMicroseconds" => ParametersMatch(parameters, typeof(Double)),
                "AddMinutes" => ParametersMatch(parameters, typeof(Double)),
                "AddMonths" => ParametersMatch(parameters, typeof(Int32)),
                "AddSeconds" => ParametersMatch(parameters, typeof(Double)),
                "AddTicks" => ParametersMatch(parameters, typeof(Int64)),
                "AddYears" => ParametersMatch(parameters, typeof(Int32)),
                "Compare" => method.IsStatic && ParametersMatch(parameters, typeof(DateTime), typeof(DateTime)),
                "CompareTo" => ParametersMatch(parameters, typeof(DateTime)),
                "DaysInMonth" => method.IsStatic && ParametersMatch(parameters, typeof(Int32), typeof(Int32)),
                "Equals" => ParametersMatch(parameters, typeof(DateTime))
                    || method.IsStatic && ParametersMatch(parameters, typeof(DateTime), typeof(DateTime)),
                "FromBinary" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "FromFileTime" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "FromFileTimeUtc" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "FromOADate" => method.IsStatic && ParametersMatch(parameters, typeof(Double)),
                "IsDaylightSavingTime" => parameters.Length == 0,
                "SpecifyKind" => method.IsStatic && ParametersMatch(parameters, typeof(DateTime), typeof(DateTimeKind)),
                "ToBinary" => parameters.Length == 0,
                "GetHashCode" => parameters.Length == 0,
                "IsLeapYear" => method.IsStatic && ParametersMatch(parameters, typeof(Int32)),
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "Subtract" => ParametersMatch(parameters, typeof(DateTime))
                    || ParametersMatch(parameters, typeof(TimeSpan)),
                "ToOADate" => parameters.Length == 0,
                "ToFileTime" => parameters.Length == 0,
                "ToFileTimeUtc" => parameters.Length == 0,
                "ToLocalTime" => parameters.Length == 0,
                "ToLongDateString" => parameters.Length == 0,
                "ToLongTimeString" => parameters.Length == 0,
                "ToShortDateString" => parameters.Length == 0,
                "ToShortTimeString" => parameters.Length == 0,
                "ToString" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(String)),
                "ToUniversalTime" => parameters.Length == 0,
                _ => false
            };
        }

        private static bool DateTimeOffsetMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int64), typeof(TimeSpan))

                    || ParametersMatch(parameters, typeof(DateTime))
                    || ParametersMatch(parameters, typeof(DateTime), typeof(TimeSpan))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(TimeSpan))
                    || ParametersMatch(parameters,
                        typeof(int), typeof(int), typeof(int), typeof(int),
                        typeof(int), typeof(int), typeof(int), typeof(TimeSpan))
#if NET
                    || ParametersMatch(parameters, typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan))
#endif
                    || ParametersMatch(parameters, typeof(int), typeof(int), typeof(int), typeof(int),
                        typeof(int), typeof(int), typeof(int), typeof(int), typeof(TimeSpan));
            return method.Name switch
            {
                "ToOffset" => ParametersMatch(parameters, typeof(TimeSpan)),
                "Add" => ParametersMatch(parameters, typeof(TimeSpan)),
                "AddDays" => ParametersMatch(parameters, typeof(Double)),
                "AddHours" => ParametersMatch(parameters, typeof(Double)),
                "AddMilliseconds" => ParametersMatch(parameters, typeof(Double)),
                "AddMicroseconds" => ParametersMatch(parameters, typeof(Double)),
                "AddMinutes" => ParametersMatch(parameters, typeof(Double)),
                "AddMonths" => ParametersMatch(parameters, typeof(Int32)),
                "AddSeconds" => ParametersMatch(parameters, typeof(Double)),
                "AddTicks" => ParametersMatch(parameters, typeof(Int64)),
                "AddYears" => ParametersMatch(parameters, typeof(Int32)),
                "Compare" => method.IsStatic && ParametersMatch(parameters, typeof(DateTimeOffset), typeof(DateTimeOffset)),
                "CompareTo" => ParametersMatch(parameters, typeof(DateTimeOffset)),
                "Equals" => ParametersMatch(parameters, typeof(DateTimeOffset))
                    || method.IsStatic && ParametersMatch(parameters, typeof(DateTimeOffset), typeof(DateTimeOffset)),
                "EqualsExact" => ParametersMatch(parameters, typeof(DateTimeOffset)),
                "FromFileTime" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "FromUnixTimeSeconds" or "FromUnixTimeMilliseconds" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "GetHashCode" => parameters.Length == 0,
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "Subtract" => ParametersMatch(parameters, typeof(DateTimeOffset))
                    || ParametersMatch(parameters, typeof(TimeSpan)),
                "ToFileTime" => parameters.Length == 0,
                "ToUnixTimeSeconds" or "ToUnixTimeMilliseconds" => !method.IsStatic && parameters.Length == 0,
                "ToLocalTime" => parameters.Length == 0,
                "ToString" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(String)),
                "ToUniversalTime" => parameters.Length == 0,
                _ => false
            };
        }

        private static bool TimeSpanMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32));
            return method.Name switch
            {
                "Add" => ParametersMatch(parameters, typeof(TimeSpan)),
                "Compare" => method.IsStatic && ParametersMatch(parameters, typeof(TimeSpan), typeof(TimeSpan)),
                "CompareTo" => ParametersMatch(parameters, typeof(TimeSpan)),
                "FromDays" => method.IsStatic && (ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64))),
                "Duration" => parameters.Length == 0,
                "Equals" => ParametersMatch(parameters, typeof(TimeSpan))
                    || method.IsStatic && ParametersMatch(parameters, typeof(TimeSpan), typeof(TimeSpan)),
                "GetHashCode" => parameters.Length == 0,
                "FromHours" => method.IsStatic && (ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64))),
                "FromMilliseconds" => method.IsStatic && (ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int64), typeof(Int64))),
                "FromMicroseconds" => method.IsStatic && (ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Int64))),
                "FromMinutes" => method.IsStatic && (ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64))),
                "Negate" => parameters.Length == 0,
                "FromSeconds" => method.IsStatic && (ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int64), typeof(Int64), typeof(Int64))),
                "Multiply" => !method.IsStatic && ParametersMatch(parameters, typeof(Double)),
                "Divide" => !method.IsStatic && (ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(TimeSpan))),
                "Subtract" => ParametersMatch(parameters, typeof(TimeSpan)),
                "FromTicks" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "ToString" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(String)),
                _ => false
            };
        }

        private static bool GuidMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Byte[]))
                    || ParametersMatch(parameters, typeof(UInt32), typeof(UInt16), typeof(UInt16), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int16), typeof(Int16), typeof(Byte[]))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int16), typeof(Int16), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte), typeof(Byte))
                    || ParametersMatch(parameters, typeof(String));
            return method.Name switch
            {
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "ParseExact" => method.IsStatic && ParametersMatch(parameters, typeof(String), typeof(String)),
                "ToString" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(String)),
                "GetHashCode" => parameters.Length == 0,
                "Equals" => ParametersMatch(parameters, typeof(Guid)),
                "CompareTo" => ParametersMatch(parameters, typeof(Guid)),
                "NewGuid" => method.IsStatic && parameters.Length == 0,
                _ => false
            };
        }

        private static bool UriMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(Boolean))
                    || ParametersMatch(parameters, typeof(String), typeof(UriKind))
                    || ParametersMatch(parameters, typeof(Uri), typeof(String))
                    || ParametersMatch(parameters, typeof(Uri), typeof(String), typeof(Boolean))
                    || ParametersMatch(parameters, typeof(Uri), typeof(Uri));
            return method.Name switch
            {
                "CheckSchemeName" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "CheckHostName" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "FromHex" => method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "GetHashCode" => parameters.Length == 0,
                "ToString" => parameters.Length == 0,
                "GetLeftPart" => ParametersMatch(parameters, typeof(UriPartial)),
                "HexEscape" => method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "IsHexDigit" => method.IsStatic && ParametersMatch(parameters, typeof(Char)),
                "IsHexEncoding" => method.IsStatic && ParametersMatch(parameters, typeof(String), typeof(Int32)),
                "MakeRelative" => ParametersMatch(parameters, typeof(Uri)),
                "MakeRelativeUri" => ParametersMatch(parameters, typeof(Uri)),
                "IsBaseOf" => ParametersMatch(parameters, typeof(Uri)),
                "GetComponents" => ParametersMatch(parameters, typeof(UriComponents), typeof(UriFormat)),
                "IsWellFormedOriginalString" => parameters.Length == 0,
                "IsWellFormedUriString" => method.IsStatic && ParametersMatch(parameters, typeof(String), typeof(UriKind)),
                "Compare" => method.IsStatic && ParametersMatch(parameters, typeof(Uri), typeof(Uri), typeof(UriComponents), typeof(UriFormat), typeof(StringComparison)),
                "UnescapeDataString" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "EscapeUriString" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "EscapeDataString" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                _ => false
            };
        }

        private static bool BooleanMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return false;
            return method.Name switch
            {
                "CompareTo" => ParametersMatch(parameters, typeof(Boolean)),
                "Equals" => ParametersMatch(parameters, typeof(Boolean)),
                "GetHashCode" => parameters.Length == 0,
                "ToString" => parameters.Length == 0,
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                _ => false
            };
        }

        private static bool DecimalMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int32))
                    || ParametersMatch(parameters, typeof(UInt32))
                    || ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(UInt64))
                    || ParametersMatch(parameters, typeof(Single))
                    || ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Int32[]))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Boolean), typeof(Byte));
            if (NumericMemberPolicy(method, parameters))
                return true;
            return method.Name switch
            {
                "ToOACurrency" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "FromOACurrency" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "Add" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Ceiling" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "Compare" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Divide" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Equals" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Floor" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "Remainder" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Multiply" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Negate" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "Round" => method.IsStatic && (ParametersMatch(parameters, typeof(Decimal))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(MidpointRounding))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(Int32), typeof(MidpointRounding))),
                "Subtract" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "ToByte" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToSByte" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToInt16" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToDouble" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToInt32" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToInt64" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToUInt16" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToUInt32" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToUInt64" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "ToSingle" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                "Truncate" => method.IsStatic && ParametersMatch(parameters, typeof(Decimal)),
                _ => false
            };
        }

        private static bool MathMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor || !method.IsStatic)
                return false;
            return method.Name switch
            {
                "Acos" => ParametersMatch(parameters, typeof(Double)),
                "Asin" => ParametersMatch(parameters, typeof(Double)),
                "Atan" => ParametersMatch(parameters, typeof(Double)),
                "Atan2" => ParametersMatch(parameters, typeof(Double), typeof(Double)),
                "Ceiling" => ParametersMatch(parameters, typeof(Decimal))
                    || ParametersMatch(parameters, typeof(Double)),
                "Cos" => ParametersMatch(parameters, typeof(Double)),
                "Cosh" => ParametersMatch(parameters, typeof(Double)),
                "Floor" => ParametersMatch(parameters, typeof(Decimal))
                    || ParametersMatch(parameters, typeof(Double)),
                "Sin" => ParametersMatch(parameters, typeof(Double)),
                "Tan" => ParametersMatch(parameters, typeof(Double)),
                "Sinh" => ParametersMatch(parameters, typeof(Double)),
                "Tanh" => ParametersMatch(parameters, typeof(Double)),
                "Round" => ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Double), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Double), typeof(MidpointRounding))
                    || ParametersMatch(parameters, typeof(Double), typeof(Int32), typeof(MidpointRounding))
                    || ParametersMatch(parameters, typeof(Decimal))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(MidpointRounding))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(Int32), typeof(MidpointRounding)),
                "Truncate" => ParametersMatch(parameters, typeof(Decimal))
                    || ParametersMatch(parameters, typeof(Double)),
                "Sqrt" => ParametersMatch(parameters, typeof(Double)),
                "Log" => ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Double), typeof(Double)),
                "Log10" => ParametersMatch(parameters, typeof(Double)),
                "Exp" => ParametersMatch(parameters, typeof(Double)),
                "Pow" => ParametersMatch(parameters, typeof(Double), typeof(Double)),
                "IEEERemainder" => ParametersMatch(parameters, typeof(Double), typeof(Double)),
                "Clamp" => ParametersMatch(parameters, typeof(SByte), typeof(SByte), typeof(SByte))
                    || ParametersMatch(parameters, typeof(Byte), typeof(Byte), typeof(Byte))
                    || ParametersMatch(parameters, typeof(Int16), typeof(Int16), typeof(Int16))
                    || ParametersMatch(parameters, typeof(UInt16), typeof(UInt16), typeof(UInt16))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(UInt32), typeof(UInt32), typeof(UInt32))
                    || ParametersMatch(parameters, typeof(Int64), typeof(Int64), typeof(Int64))
                    || ParametersMatch(parameters, typeof(UInt64), typeof(UInt64), typeof(UInt64))
                    || ParametersMatch(parameters, typeof(Single), typeof(Single), typeof(Single))
                    || ParametersMatch(parameters, typeof(Double), typeof(Double), typeof(Double))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(Decimal), typeof(Decimal)),
                "Abs" => ParametersMatch(parameters, typeof(SByte))
                    || ParametersMatch(parameters, typeof(Int16))
                    || ParametersMatch(parameters, typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Single))
                    || ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Decimal)),
                "Max" => ParametersMatch(parameters, typeof(SByte), typeof(SByte))
                    || ParametersMatch(parameters, typeof(Byte), typeof(Byte))
                    || ParametersMatch(parameters, typeof(Int16), typeof(Int16))
                    || ParametersMatch(parameters, typeof(UInt16), typeof(UInt16))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(UInt32), typeof(UInt32))
                    || ParametersMatch(parameters, typeof(Int64), typeof(Int64))
                    || ParametersMatch(parameters, typeof(UInt64), typeof(UInt64))
                    || ParametersMatch(parameters, typeof(Single), typeof(Single))
                    || ParametersMatch(parameters, typeof(Double), typeof(Double))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Min" => ParametersMatch(parameters, typeof(SByte), typeof(SByte))
                    || ParametersMatch(parameters, typeof(Byte), typeof(Byte))
                    || ParametersMatch(parameters, typeof(Int16), typeof(Int16))
                    || ParametersMatch(parameters, typeof(UInt16), typeof(UInt16))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(UInt32), typeof(UInt32))
                    || ParametersMatch(parameters, typeof(Int64), typeof(Int64))
                    || ParametersMatch(parameters, typeof(UInt64), typeof(UInt64))
                    || ParametersMatch(parameters, typeof(Single), typeof(Single))
                    || ParametersMatch(parameters, typeof(Double), typeof(Double))
                    || ParametersMatch(parameters, typeof(Decimal), typeof(Decimal)),
                "Sign" => ParametersMatch(parameters, typeof(SByte))
                    || ParametersMatch(parameters, typeof(Int16))
                    || ParametersMatch(parameters, typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Single))
                    || ParametersMatch(parameters, typeof(Double))
                    || ParametersMatch(parameters, typeof(Decimal)),
                "BigMul" => ParametersMatch(parameters, typeof(Int32), typeof(Int32)),
                "ToString" => parameters.Length == 0,
                "GetHashCode" => parameters.Length == 0,
                _ => false
            };
        }

        private static bool ConvertMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor || !method.IsStatic)
                return false;
            bool singleArgument = parameters.Length == 1 && IsConversionInput(parameters[0].ParameterType);
            return method.Name switch
            {
                "ToBoolean" or "ToChar" or "ToSingle" or "ToDouble" or "ToDecimal" or "ToDateTime" => singleArgument,
                "ToSByte" or "ToByte" or "ToInt16" or "ToUInt16" or "ToInt32" or "ToUInt32" or "ToInt64" or "ToUInt64" =>
                    singleArgument || ParametersMatch(parameters, typeof(string), typeof(int)),
                "ToString" => singleArgument || parameters.Length == 0
                    || ParametersMatch(parameters, typeof(byte), typeof(int))
                    || ParametersMatch(parameters, typeof(short), typeof(int))
                    || ParametersMatch(parameters, typeof(int), typeof(int))
                    || ParametersMatch(parameters, typeof(long), typeof(int)),
                "GetHashCode" => parameters.Length == 0,
                _ => false
            };
        }

        private static bool IsConversionInput(Type type)
        {
            return type == typeof(bool) || type == typeof(char) || type == typeof(sbyte) || type == typeof(byte)
                || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint)
                || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double)
                || type == typeof(decimal) || type == typeof(string) || type == typeof(DateTime);
        }

#if NET
        private static bool DateOnlyMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Calendar));
            return method.Name switch
            {
                "AddDays" => true,
                "AddMonths" => true,
                "AddYears" => true,
                "CompareTo" => ParametersMatch(parameters, typeof(DateOnly))
                    || ParametersMatch(parameters, typeof(object)),
                "Equals" => ParametersMatch(parameters, typeof(DateOnly))
                    || ParametersMatch(parameters, typeof(object)),
                "FromDateTime" => method.IsStatic && ParametersMatch(parameters, typeof(DateTime)),
                "FromDayNumber" => method.IsStatic && ParametersMatch(parameters, typeof(int)),
                "GetHashCode" => parameters.Length == 0,
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(string)),
                "ParseExact" => method.IsStatic && (ParametersMatch(parameters, typeof(string), typeof(string[]))
                    || ParametersMatch(parameters, typeof(string), typeof(string))),
                "ToDateTime" => ParametersMatch(parameters, typeof(TimeOnly))
                    || ParametersMatch(parameters, typeof(TimeOnly), typeof(DateTimeKind)),
                "ToLongDateString" => parameters.Length == 0,
                "ToShortDateString" => parameters.Length == 0,
                "ToString" => parameters.Length == 0 || ParametersMatch(parameters, typeof(string)),
                _ => false
            };
        }

        private static bool TimeOnlyMemberPolicy(MethodBase method, ParameterInfo[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32));
            return method.Name switch
            {
                "Add" => true,
                "AddHours" => true,
                "AddMinutes" => true,
                "CompareTo" => ParametersMatch(parameters, typeof(object))
                    || ParametersMatch(parameters, typeof(TimeOnly)),
                "Equals" => ParametersMatch(parameters, typeof(object))
                    || ParametersMatch(parameters, typeof(TimeOnly)),
                "FromDateTime" => method.IsStatic && ParametersMatch(parameters, typeof(DateTime)),
                "FromTimeSpan" => method.IsStatic && ParametersMatch(parameters, typeof(TimeSpan)),
                "GetHashCode" => parameters.Length == 0,
                "IsBetween" => ParametersMatch(parameters, typeof(TimeOnly), typeof(TimeOnly)),
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(string)),
                "ParseExact" => method.IsStatic && (ParametersMatch(parameters, typeof(string), typeof(string))
                    || ParametersMatch(parameters, typeof(string), typeof(string[]))),
                "ToLongTimeString" => parameters.Length == 0,
                "ToShortTimeString" => parameters.Length == 0,
                "ToString" => parameters.Length == 0 || ParametersMatch(parameters, typeof(string)),
                "ToTimeSpan" => parameters.Length == 0,
                _ => false
            };
        }

#endif
        private static bool IsNumericFormatAllowed(Expression argument, QueryLimits limits)
        {
            if (argument is not ConstantExpression constant)
                return false;
            if (constant.Value is null)
                return true;
            if (constant.Value is not string format || format.Length > limits.MaxNumericFormatLength)
                return false;

            // The runtime ignores content after a null character when recognizing standard formats.
            ReadOnlySpan<char> effectiveFormat = format.AsSpan();
            int terminator = effectiveFormat.IndexOf('\0');
            if (terminator >= 0)
                effectiveFormat = effectiveFormat.Slice(0, terminator);

            if (effectiveFormat.Length == 0 || !(effectiveFormat[0] is >= 'A' and <= 'Z' or >= 'a' and <= 'z'))
                return true;

            for (int i = 1; i < effectiveFormat.Length; i++)
            {
                if (effectiveFormat[i] is < '0' or > '9')
                    return true; // Custom formats are bounded by their total length.
            }

            long precision = 0;
            for (int i = 1; i < effectiveFormat.Length; i++)
            {
                precision = precision * 10 + effectiveFormat[i] - '0';
                if (precision > limits.MaxNumericFormatPrecision)
                    return false;
            }
            return true;
        }

        private static int GetStringArgumentLength(ConstantExpression argument)
        {
            string stringArgument = argument.Value as string;
            return stringArgument != null ? stringArgument.Length : 0;
        }
    }
}
