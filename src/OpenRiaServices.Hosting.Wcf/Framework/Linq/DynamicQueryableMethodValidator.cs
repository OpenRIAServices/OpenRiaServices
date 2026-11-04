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
        internal enum MemberRule
        {
            Denied,
            Allowed,
            NumericFormat,
            ReplaceCharacter,
            ReplaceString
        }

        /// <summary>Determines whether a member signature is permitted for its registered declaring type.</summary>
        /// <param name="method">The member to check.</param>
        /// <param name="parameters">The member's parameter types.</param>
        /// <returns>Whether the signature is permitted.</returns>
        internal delegate bool MemberPolicy(MethodBase method, Type[] parameters);

        /// <summary>Policy-permitted method and constructor signatures, independent of entity data-member lookup.</summary>
        internal sealed class InvocationMembers
        {
            public static readonly InvocationMembers Empty = new();

            InvocationMembers()
            {
                InstanceMethods = FrozenDictionary<string, MethodSignature[]>.Empty;
                StaticMethods = FrozenDictionary<string, MethodSignature[]>.Empty;
                Constructors = Array.Empty<MethodSignature>();
            }

            /// <summary>Creates invocation metadata using the registered type's policy.</summary>
            /// <param name="type">The supported declaring type.</param>
            /// <param name="policy">The policy for member signatures declared by the type.</param>
            /// <param name="numericFormat">Whether numeric formatting requires argument validation.</param>
            internal InvocationMembers(Type type, MemberPolicy policy, bool numericFormat)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                MethodSignature[] methods = GetSignatures(type.GetMethods(flags), type, policy, numericFormat).ToArray();
                InstanceMethods = GroupMethods(methods.Where(s => !s.Method.IsStatic));
                StaticMethods = GroupMethods(methods.Where(s => s.Method.IsStatic));
                Constructors = GetSignatures(type.GetConstructors(), type, policy, numericFormat).ToArray();
            }

            public FrozenDictionary<string, MethodSignature[]> InstanceMethods { get; }
            public FrozenDictionary<string, MethodSignature[]> StaticMethods { get; }
            public MethodSignature[] Constructors { get; }

            static FrozenDictionary<string, MethodSignature[]> GroupMethods(IEnumerable<MethodSignature> methods)
                => methods.GroupBy(s => s.Method.Name, StringComparer.OrdinalIgnoreCase)
                    .ToFrozenDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);

            static IEnumerable<MethodSignature> GetSignatures(IEnumerable<MethodBase> methods, Type type, MemberPolicy policy, bool numericFormat)
            {
                foreach (MethodBase method in methods)
                {
                    if (method.DeclaringType != type)
                        continue;
                    Type[] parameters = Array.ConvertAll(method.GetParameters(), p => p.ParameterType);
                    MemberRule rule = ResolveMemberRule(method, parameters, policy, numericFormat);
                    if (rule != MemberRule.Denied)
                        yield return new MethodSignature(method, parameters, rule);
                }
            }

            static MemberRule ResolveMemberRule(MethodBase method, Type[] parameters, MemberPolicy policy, bool numericFormat)
            {
                if (parameters.Any(p => p.IsByRef) || !policy(method, parameters))
                    return MemberRule.Denied;
                if (numericFormat && !method.IsStatic && method.Name == "ToString" && ParametersMatch(parameters, typeof(string)))
                    return MemberRule.NumericFormat;
                if (method.DeclaringType == typeof(string) && method.Name == "Replace")
                    return parameters[1] == typeof(char) ? MemberRule.ReplaceCharacter : MemberRule.ReplaceString;
                return MemberRule.Allowed;
            }
        }

        private static readonly FrozenDictionary<Type, Lazy<InvocationMembers>> s_invocationMembers = CreateInvocationMembers();

        /// <summary>
        /// Gets the allowed members to call for a specified type
        /// </summary>
        /// <param name="type">The type whose invocation metadata to retrieve.</param>
        /// <remarks>
        /// Only members declared on registered types are permitted; base types and interfaces are not searched.
        /// It is unlikely that someone will both subclass <see cref="Uri"/> and need to use one of the few allowed instance methods in a query.
        /// </remarks>
        /// <returns><see cref="InvocationMembers"/> listing all allowed members, <see cref="InvocationMembers.Empty"/> if type is not allowed.</returns>
        internal static InvocationMembers GetInvocationMembers(Type type)
            => s_invocationMembers.TryGetValue(type, out var members) ? members.Value : InvocationMembers.Empty;

        private static FrozenDictionary<Type, Lazy<InvocationMembers>> CreateInvocationMembers()
        {
            var policies = new Dictionary<Type, (MemberPolicy Policy, bool NumericFormat)>();
            policies.Add(typeof(Char), (CharMemberPolicy, false));
            policies.Add(typeof(String), (StringMemberPolicy, false));
            policies.Add(typeof(DateTime), (DateTimeMemberPolicy, false));
            policies.Add(typeof(DateTimeOffset), (DateTimeOffsetMemberPolicy, false));
            policies.Add(typeof(TimeSpan), (TimeSpanMemberPolicy, false));
            policies.Add(typeof(Guid), (GuidMemberPolicy, false));
            policies.Add(typeof(Uri), (UriMemberPolicy, false));
            policies.Add(typeof(SByte), (NumericMemberPolicy, true));
            policies.Add(typeof(Byte), (NumericMemberPolicy, true));
            policies.Add(typeof(Int16), (NumericMemberPolicy, true));
            policies.Add(typeof(UInt16), (NumericMemberPolicy, true));
            policies.Add(typeof(Int32), (NumericMemberPolicy, true));
            policies.Add(typeof(UInt32), (NumericMemberPolicy, true));
            policies.Add(typeof(Int64), (NumericMemberPolicy, true));
            policies.Add(typeof(Boolean), (BooleanMemberPolicy, false));
            policies.Add(typeof(UInt64), (NumericMemberPolicy, true));
            policies.Add(typeof(Single), (NumericMemberPolicy, true));
            policies.Add(typeof(Double), (NumericMemberPolicy, true));
            policies.Add(typeof(Decimal), (DecimalMemberPolicy, true));
            policies.Add(typeof(Math), (MathMemberPolicy, false));
            policies.Add(typeof(Convert), (ConvertMemberPolicy, false));
#if NET
            policies.Add(typeof(DateOnly), (DateOnlyMemberPolicy, false));
            policies.Add(typeof(TimeOnly), (TimeOnlyMemberPolicy, false));
#endif
            return policies.ToFrozenDictionary(static pair => pair.Key,
                static pair => new Lazy<InvocationMembers>(() => new InvocationMembers(pair.Key, pair.Value.Policy, pair.Value.NumericFormat)));
        }

        /// <summary>ONLY USED BY TESTS</summary>
        /// <param name="method">The bound member to check.</param>
        /// <param name="args">The current argument expressions.</param>
        /// <param name="limits">The current query limits.</param>
        /// <returns>Whether the member and its arguments are permitted.</returns>
        internal static bool IsMemberAccessAllowed(MethodBase method, Expression[] args, QueryLimits limits)
        {
            if (method.IsConstructor)
            {
                InvocationMembers members = GetInvocationMembers(method.DeclaringType);
                foreach (MethodSignature signature in members.Constructors)
                {
                    if (signature.Method.Equals(method))
                        return ApplyMemberRule(signature.Rule, args, limits);
                }
                return false;
            }

            var parser = new ExpressionParser([Expression.Parameter(typeof(int), "")], "1", null, QueryLimits.Defaults);
            if (parser.FindMethod(method.DeclaringType, method.Name, staticAccess: method.IsStatic, args, out var methodSignature) == 1)
            {
                return ApplyMemberRule(methodSignature.Rule, args, limits);
            }
            return false;
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

        private static bool ParametersMatch(Type[] actual, params ReadOnlySpan<Type> expected)
        {
            if (actual.Length != expected.Length)
                return false;
            for (int i = 0; i < actual.Length; i++)
            {
                if (actual[i] != expected[i])
                    return false;
            }
            return true;
        }

        private static bool NumericMemberPolicy(MethodBase method, Type[] parameters)
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
                _ => false
            };
        }

        private static bool CharMemberPolicy(MethodBase method, Type[] parameters)
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

        private static bool StringMemberPolicy(MethodBase method, Type[] parameters)
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
                "Contains" => ParametersMatch(parameters, typeof(String)),
                "EndsWith" => ParametersMatch(parameters, typeof(String))
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison)),
                "IndexOf" => ParametersMatch(parameters, typeof(Char))
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
                    || ParametersMatch(parameters, typeof(String), typeof(StringComparison)),
                "ToLower" => parameters.Length == 0,
                "ToLowerInvariant" => parameters.Length == 0,
                "ToUpper" => parameters.Length == 0,
                "ToUpperInvariant" => parameters.Length == 0,
                "ToString" => parameters.Length == 0,
                "Trim" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(Char[])),
                "TrimStart" => ParametersMatch(parameters, typeof(Char[])),
                "TrimEnd" => ParametersMatch(parameters, typeof(Char[])),
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

        private static bool DateTimeMemberPolicy(MethodBase method, Type[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int64), typeof(DateTimeKind))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(DateTimeKind))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(DateTimeKind));
            return method.Name switch
            {
                "Add" => ParametersMatch(parameters, typeof(TimeSpan)),
                "AddDays" => ParametersMatch(parameters, typeof(Double)),
                "AddHours" => ParametersMatch(parameters, typeof(Double)),
                "AddMilliseconds" => ParametersMatch(parameters, typeof(Double)),
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

        private static bool DateTimeOffsetMemberPolicy(MethodBase method, Type[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int64), typeof(TimeSpan))
                    || ParametersMatch(parameters, typeof(DateTime))
                    || ParametersMatch(parameters, typeof(DateTime), typeof(TimeSpan))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(TimeSpan))
                    || ParametersMatch(parameters,
                        typeof(int), typeof(int), typeof(int), typeof(int),
                        typeof(int), typeof(int), typeof(int), typeof(TimeSpan));
            return method.Name switch
            {
                "ToOffset" => ParametersMatch(parameters, typeof(TimeSpan)),
                "Add" => ParametersMatch(parameters, typeof(TimeSpan)),
                "AddDays" => ParametersMatch(parameters, typeof(Double)),
                "AddHours" => ParametersMatch(parameters, typeof(Double)),
                "AddMilliseconds" => ParametersMatch(parameters, typeof(Double)),
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
                "GetHashCode" => parameters.Length == 0,
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "Subtract" => ParametersMatch(parameters, typeof(DateTimeOffset))
                    || ParametersMatch(parameters, typeof(TimeSpan)),
                "ToFileTime" => parameters.Length == 0,
                "ToLocalTime" => parameters.Length == 0,
                "ToString" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(String)),
                "ToUniversalTime" => parameters.Length == 0,
                _ => false
            };
        }

        private static bool TimeSpanMemberPolicy(MethodBase method, Type[] parameters)
        {
            if (method.IsConstructor)
                return ParametersMatch(parameters, typeof(Int64))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32))
                    || ParametersMatch(parameters, typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32), typeof(Int32));
            return method.Name switch
            {
                "Add" => ParametersMatch(parameters, typeof(TimeSpan)),
                "Compare" => method.IsStatic && ParametersMatch(parameters, typeof(TimeSpan), typeof(TimeSpan)),
                "CompareTo" => ParametersMatch(parameters, typeof(TimeSpan)),
                "FromDays" => method.IsStatic && ParametersMatch(parameters, typeof(Double)),
                "Duration" => parameters.Length == 0,
                "Equals" => ParametersMatch(parameters, typeof(TimeSpan))
                    || method.IsStatic && ParametersMatch(parameters, typeof(TimeSpan), typeof(TimeSpan)),
                "GetHashCode" => parameters.Length == 0,
                "FromHours" => method.IsStatic && ParametersMatch(parameters, typeof(Double)),
                "FromMilliseconds" => method.IsStatic && ParametersMatch(parameters, typeof(Double)),
                "FromMinutes" => method.IsStatic && ParametersMatch(parameters, typeof(Double)),
                "Negate" => parameters.Length == 0,
                "FromSeconds" => method.IsStatic && ParametersMatch(parameters, typeof(Double)),
                "Subtract" => ParametersMatch(parameters, typeof(TimeSpan)),
                "FromTicks" => method.IsStatic && ParametersMatch(parameters, typeof(Int64)),
                "Parse" => method.IsStatic && ParametersMatch(parameters, typeof(String)),
                "ToString" => parameters.Length == 0
                    || ParametersMatch(parameters, typeof(String)),
                _ => false
            };
        }

        private static bool GuidMemberPolicy(MethodBase method, Type[] parameters)
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

        private static bool UriMemberPolicy(MethodBase method, Type[] parameters)
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

        private static bool BooleanMemberPolicy(MethodBase method, Type[] parameters)
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

        private static bool DecimalMemberPolicy(MethodBase method, Type[] parameters)
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

        private static bool MathMemberPolicy(MethodBase method, Type[] parameters)
        {
            if (method.IsConstructor)
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

        private static bool ConvertMemberPolicy(MethodBase method, Type[] parameters)
        {
            if (method.IsConstructor)
                return false;
            bool singleArgument = parameters.Length == 1 && IsConversionInput(parameters[0]);
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
        private static bool DateOnlyMemberPolicy(MethodBase method, Type[] parameters)
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

        private static bool TimeOnlyMemberPolicy(MethodBase method, Type[] parameters)
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
