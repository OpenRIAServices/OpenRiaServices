using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client;
using OpenRiaServices.Hosting.AspNetCore;

namespace System.Linq.Dynamic.UnitTests
{
    [TestClass]
    public class MemberValidationCharacterizationTests
    {
        [TestMethod]
        [DataRow(typeof(char), "IsDigit", true, typeof(char))]
        [DataRow(typeof(string), "Substring", true, typeof(int))]
        [DataRow(typeof(string), "Equals", true, typeof(string))]
        [DataRow(typeof(string), "Equals", false, typeof(object))]
        [DataRow(typeof(string), "Split", false, typeof(char[]))]
        [DataRow(typeof(string), "Concat", true, typeof(string), typeof(string))]
        [DataRow(typeof(string), "Concat", false, typeof(object), typeof(object))]
        [DataRow(typeof(DateTime), "AddDays", true, typeof(double))]
        [DataRow(typeof(DateTime), "Equals", false, typeof(object))]
        [DataRow(typeof(DateTimeOffset), "ToOffset", true, typeof(TimeSpan))]
        [DataRow(typeof(TimeSpan), "FromDays", true, typeof(double))]
        [DataRow(typeof(Guid), "NewGuid", true)]
        [DataRow(typeof(Uri), "EscapeDataString", true, typeof(string))]
        [DataRow(typeof(Math), "Round", true, typeof(decimal), typeof(int), typeof(MidpointRounding))]
        [DataRow(typeof(Convert), "ToString", true, typeof(long), typeof(int))]
        [DataRow(typeof(Convert), "ToString", false, typeof(object), typeof(IFormatProvider))]
        [DataRow(typeof(Environment), "GetEnvironmentVariable", false, typeof(string))]
        public void ExistingMethodPermissions(Type type, string name, bool allowed, params Type[] parameters)
        {
            MethodInfo method = type.GetMethod(name, parameters);
            Assert.AreEqual(allowed, Allows(method), method.ToString());
        }

        [TestMethod]
        [DataRow(typeof(string), true, typeof(char[]))]
        [DataRow(typeof(string), false, typeof(char), typeof(int))]
        [DataRow(typeof(DateTime), true, typeof(long))]
        [DataRow(typeof(DateTime), true, typeof(int), typeof(int), typeof(int))]
        [DataRow(typeof(DateTimeOffset), true, typeof(DateTime))]
        [DataRow(typeof(TimeSpan), true, typeof(long))]
        [DataRow(typeof(Guid), true, typeof(string))]
        [DataRow(typeof(Uri), true, typeof(string))]
        [DataRow(typeof(decimal), true, typeof(int))]
        [DataRow(typeof(object), false)]
        public void ExistingConstructorPermissions(Type type, bool allowed, params Type[] parameters)
        {
            ConstructorInfo constructor = type.GetConstructor(parameters);
            Assert.IsNotNull(constructor);
            Assert.AreEqual(allowed, Allows(constructor), constructor.ToString());
        }

        [TestMethod]
        [DataRow(typeof(sbyte))]
        [DataRow(typeof(byte))]
        [DataRow(typeof(short))]
        [DataRow(typeof(ushort))]
        [DataRow(typeof(int))]
        [DataRow(typeof(uint))]
        [DataRow(typeof(long))]
        [DataRow(typeof(ulong))]
        [DataRow(typeof(float))]
        [DataRow(typeof(double))]
        [DataRow(typeof(decimal))]
        public void NumericPolicyIsExplicitAndOverloadSpecific(Type type)
        {
            Assert.IsTrue(Allows(type.GetMethod("ToString", Type.EmptyTypes)));
            Assert.IsTrue(Allows(type.GetMethod("CompareTo", [type])));
            Assert.IsTrue(Allows(type.GetMethod("Equals", [type])));
            Assert.IsFalse(Allows(type.GetMethod("CompareTo", [typeof(object)])));
            Assert.IsFalse(Allows(type.GetMethod("Equals", [typeof(object)])));
            Assert.IsTrue(Allows(type.GetMethod("Parse", [typeof(string), typeof(NumberStyles)])));
            Assert.IsFalse(Allows(type.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)])));
        }

        [TestMethod]
        public void StringAdditions()
        {
#if NET
            AssertRoundTrip<string>(s => s.Contains('a') && s.Contains("A", StringComparison.OrdinalIgnoreCase)
                && s.Contains('A', StringComparison.OrdinalIgnoreCase), "abc", "zzz");
            AssertRoundTrip<string>(s => s.StartsWith('a') && s.EndsWith('c')
                && s.IndexOf('A', StringComparison.OrdinalIgnoreCase) == 0, "abc", "zzz");
            AssertRoundTrip<string>(s => s.Trim('a') == "bc" && s.TrimStart('a') == "bc"
                && s.TrimEnd('c') == "ab", "abc", "zzz");
            AssertRoundTrip<string>(s => s.TrimStart() == "abc " && s.TrimEnd() == " abc", " abc ", "zzz");
#else
            Assert.ThrowsExactly<ParseException>(() => DynamicExpression.ParseLambda(
                typeof(string), typeof(string), "it.TrimStart()", null, QueryLimits.Defaults));
#endif
            Assert.IsFalse(Allows(typeof(string).GetMethod("StartsWith", [typeof(string), typeof(bool), typeof(CultureInfo)])));
            Assert.IsFalse(Allows(typeof(string).GetConstructor([typeof(char), typeof(int)])));
        }

        [TestMethod]
        public void CharAdditions()
        {
            AssertRoundTrip<char>(c => char.GetUnicodeCategory(c) == UnicodeCategory.UppercaseLetter, 'A', '1');
            AssertRoundTrip<string>(s => char.GetUnicodeCategory(s, 0) == UnicodeCategory.LowercaseLetter, "a", "1");
#if NET
            AssertRoundTrip<char>(c => char.IsAscii(c) && char.IsAsciiDigit(c), '1', '\u0100');
            AssertRoundTrip<char>(c => char.IsAsciiLetter(c) && char.IsAsciiLetterLower(c)
                && char.IsAsciiLetterOrDigit(c), 'a', '1');
            AssertRoundTrip<char>(c => char.IsAsciiLetterUpper(c) && char.IsAsciiHexDigit(c)
                && char.IsAsciiHexDigitUpper(c), 'A', 'z');
            AssertRoundTrip<char>(c => char.IsAsciiHexDigitLower(c) && char.IsBetween(c, 'a', 'f'), 'a', 'z');
#endif
        }

        [TestMethod]
        public void DateTimeAdditions()
        {
#if NET
            AssertRoundTrip<DateTime>(d => d.AddMicroseconds(2).Ticks > d.Ticks, new DateTime(2020, 1, 2));
            AssertParsed("DateTime(DateOnly(2020,1,2),TimeOnly(3,4,5))", () => new DateTime(new DateOnly(2020, 1, 2), new TimeOnly(3, 4, 5)));
            AssertParsed("DateTime(DateOnly(2020,1,2),TimeOnly(3,4,5),\"Utc\")",
                () => new DateTime(new DateOnly(2020, 1, 2), new TimeOnly(3, 4, 5), DateTimeKind.Utc));
            AssertParsed("DateTime(2020,1,2,3,4,5,6,7)", () => new DateTime(2020, 1, 2, 3, 4, 5, 6, 7));
            AssertParsed("DateTime(2020,1,2,3,4,5,6,7,\"Utc\")", () => new DateTime(2020, 1, 2, 3, 4, 5, 6, 7, DateTimeKind.Utc));
            AssertParsed("DateTime(2020,1,2,3,4,5,6,\"Utc\")", () => new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));
            AssertParsed("DateTime(2020,1,2,3,4,5,6,0)", () => new DateTime(2020, 1, 2, 3, 4, 5, 6, 0));
#endif
            foreach (ConstructorInfo constructor in typeof(DateTime).GetConstructors()
                .Where(c => c.GetParameters().Any(p => p.ParameterType == typeof(Calendar))))
                Assert.IsFalse(Allows(constructor), constructor.ToString());
        }

        [TestMethod]
        public void DateTimeOffsetAdditions()
        {
            AssertRoundTrip<long>(n => DateTimeOffset.FromUnixTimeSeconds(n).ToUnixTimeSeconds() == n, 0, -123, 123);
            AssertRoundTrip<long>(n => DateTimeOffset.FromUnixTimeMilliseconds(n).ToUnixTimeMilliseconds() == n, 0, -123, 123);
#if NET
            AssertRoundTrip<DateTimeOffset>(d => d.AddMicroseconds(2).Ticks > d.Ticks, new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero));
            AssertParsed("DateTimeOffset(2020,1,2,3,4,5,6,7,TimeSpan(0))", () => new DateTimeOffset(2020, 1, 2, 3, 4, 5, 6, 7, TimeSpan.Zero));
#endif
#if NET10_0_OR_GREATER
            AssertParsed("DateTimeOffset(DateOnly(2020,1,2),TimeOnly(3,4,5),TimeSpan(0))",
                () => new DateTimeOffset(new DateOnly(2020, 1, 2), new TimeOnly(3, 4, 5), TimeSpan.Zero));
#endif
            foreach (ConstructorInfo constructor in typeof(DateTimeOffset).GetConstructors()
                .Where(c => c.GetParameters().Any(p => p.ParameterType == typeof(Calendar))))
                Assert.IsFalse(Allows(constructor), constructor.ToString());
        }

        [TestMethod]
        public void TimeSpanAdditions()
        {
            AssertRoundTrip<int>(n => TimeSpan.FromDays(n).TotalDays == n && TimeSpan.FromHours(n).TotalHours == n
                && TimeSpan.FromMinutes(n).TotalMinutes == n && TimeSpan.FromSeconds(n).TotalSeconds == n
                && TimeSpan.FromMilliseconds(n).TotalMilliseconds == n, -1, 0, 1);
            AssertRoundTrip<double>(n => TimeSpan.FromDays(n).TotalDays == n && TimeSpan.FromHours(n).TotalHours == n
                && TimeSpan.FromMinutes(n).TotalMinutes == n && TimeSpan.FromSeconds(n).TotalSeconds == n
                && TimeSpan.FromMilliseconds(n).TotalMilliseconds == n, -1, 0, 1);
#if NET
            AssertRoundTrip<int>(n => TimeSpan.FromMicroseconds(n).Ticks == n * 10, -1, 0, 1);
            AssertRoundTrip<double>(n => TimeSpan.FromMicroseconds(n).Ticks == n * 10, -1, 0, 1);
            AssertRoundTrip<TimeSpan>(t => t.Multiply(2).Divide(2) == t && t.Divide(TimeSpan.FromTicks(1)) == t.Ticks,
                TimeSpan.FromTicks(10), TimeSpan.FromTicks(-10));
            AssertParsed("TimeSpan(1,2,3,4,5,6)", () => new TimeSpan(1, 2, 3, 4, 5, 6));
#endif
#if NET10_0_OR_GREATER
            AssertRoundTrip<long>(n => TimeSpan.FromMinutes(n).TotalMinutes == n
                && TimeSpan.FromSeconds(n).TotalSeconds == n && TimeSpan.FromMilliseconds(n).TotalMilliseconds == n
                && TimeSpan.FromMicroseconds(n).Ticks == n * 10, -1, 0, 1);
            AssertRoundTrip<int>(n => TimeSpan.FromDays(n, 2, 3, 4, 5, 6).Ticks == new TimeSpan(1, 2, 3, 4, 5, 6).Ticks, 1, 0);
            AssertRoundTrip<int>(n => TimeSpan.FromHours(n, 3, 4, 5, 6).Ticks == new TimeSpan(0, 2, 3, 4, 5, 6).Ticks, 2, 0);
            AssertRoundTrip<long>(n => TimeSpan.FromMinutes(n, 4, 5, 6).Ticks == new TimeSpan(0, 0, 3, 4, 5, 6).Ticks, 3, 0);
            AssertRoundTrip<long>(n => TimeSpan.FromSeconds(n, 5, 6).Ticks == new TimeSpan(0, 0, 0, 4, 5, 6).Ticks, 4, 0);
            AssertRoundTrip<long>(n => TimeSpan.FromMilliseconds(n, 6).Ticks == new TimeSpan(0, 0, 0, 0, 5, 6).Ticks, 5, 0);
            Assert.ThrowsExactly<ParseException>(() => DynamicExpression.ParseLambda(
                typeof(int), typeof(TimeSpan), "TimeSpan.FromDays(it,2)", null, QueryLimits.Defaults));
#endif
        }

        [TestMethod]
        [DataRow(typeof(sbyte))]
        [DataRow(typeof(byte))]
        [DataRow(typeof(short))]
        [DataRow(typeof(ushort))]
        [DataRow(typeof(int))]
        [DataRow(typeof(uint))]
        [DataRow(typeof(long))]
        [DataRow(typeof(ulong))]
        [DataRow(typeof(float))]
        [DataRow(typeof(double))]
        [DataRow(typeof(decimal))]
        [DataRow(typeof(Math))]
        public void NumericAdditions(Type type)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name is "Abs" or "Min" or "Max" or "Clamp")
                {
                    Type scalar = method.GetParameters()[0].ParameterType;
                    bool allowed = scalar != typeof(IntPtr) && scalar != typeof(UIntPtr);
                    Assert.AreEqual(allowed, Allows(method), method.ToString());
                    if (allowed)
                    {
                        var parameter = Expression.Parameter(scalar, "");
                        Expression call = Expression.Call(method, method.GetParameters().Select(_ => (Expression)parameter));
                        Array values = Array.CreateInstance(scalar, 3);
                        for (int i = 0; i < values.Length; i++)
                            values.SetValue(Convert.ChangeType(i, scalar, CultureInfo.InvariantCulture), i);
                        var predicate = Expression.Lambda(Expression.Equal(call, parameter), parameter);
                        AssertRoundTrip(values.AsQueryable(), predicate);
                    }
                }
                if (method.Name is "GetBits" or "GetTypeCode" or "CopySign" or "IsEvenInteger" or "PopCount" or "Acosh")
                    Assert.IsFalse(Allows(method), method.ToString());
                if (method.GetParameters().Any(p => p.ParameterType == typeof(IFormatProvider)))
                    Assert.IsFalse(Allows(method), method.ToString());
            }
        }

        [TestMethod]
        public void UriAdditions()
        {
            AssertRoundTrip<string>(s => Uri.CheckHostName(s) == UriHostNameType.Dns, "example.org", "127.0.0.1", "?");
        }

        [TestMethod]
        public void ArgumentsAndMutableLimitsAreNeverCached()
        {
            MethodInfo method = typeof(int).GetMethod("ToString", [typeof(string)]);
            var limits = new QueryLimits { MaxNumericFormatPrecision = 2 };
            Expression[] arguments = [Expression.Constant("D3")];
            Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(method, arguments, limits));
            limits.MaxNumericFormatPrecision = 3;
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(method, arguments, limits));
            limits.MaxNumericFormatLength = 1;
            Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(method, arguments, limits));
            limits.MaxNumericFormatLength = 64;
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(method, arguments, limits));
            arguments[0] = Expression.Parameter(typeof(string));
            Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(method, arguments, limits));
            arguments[0] = Expression.Constant(null, typeof(string));
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(method, arguments, limits));
        }

        [TestMethod]
        public void ReplaceChecksEveryReplacementArgument()
        {
            MethodInfo text = typeof(string).GetMethod("Replace", [typeof(string), typeof(string)]);
            MethodInfo character = typeof(string).GetMethod("Replace", [typeof(char), typeof(char)]);
            var limits = new QueryLimits();
            Expression[] arguments = [Expression.Parameter(typeof(string)), Expression.Constant(new string('a', 100))];
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(text, arguments, limits));
            arguments[1] = Expression.Constant(new string('a', 101));
            Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(text, arguments, limits));
            arguments[1] = Expression.Parameter(typeof(string));
            Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(text, arguments, limits));
            arguments[1] = Expression.Constant(null, typeof(string));
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(text, arguments, limits));
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(character, [Expression.Constant('a'), Expression.Constant('b')], limits));
            Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(character, [Expression.Constant('a'), Expression.Parameter(typeof(char))], limits));
        }

        [TestMethod]
        public void InheritedAndNullableMembersKeepTheirDeclaringTypeSemantics()
        {
            MethodInfo inherited = typeof(string).GetMethod("GetType", Type.EmptyTypes);
            Assert.AreEqual(typeof(object), inherited.DeclaringType);
            Assert.IsFalse(Allows(inherited));
            Assert.IsTrue(Allows(typeof(int).GetMethod("ToString", Type.EmptyTypes)));
            Assert.IsTrue(Allows(typeof(int?).GetMethod("ToString", Type.EmptyTypes)));
            Assert.IsFalse(Allows(typeof(int?).GetMethod("GetValueOrDefault", Type.EmptyTypes)));
            DynamicExpression.ParseLambda(typeof(int?), typeof(string), "it.Value.ToString(\"D2\")", null, QueryLimits.Defaults);
        }

        [TestMethod]
        public void ParserMemberNamesRemainCaseInsensitive()
        {
            DynamicExpression.ParseLambda(typeof(int), typeof(string), "it.tOsTrInG(\"D2\")", null, QueryLimits.Defaults);
            DynamicExpression.ParseLambda(typeof(int), typeof(int), "mAtH.aBs(it)", null, QueryLimits.Defaults);
            DynamicExpression.ParseLambda(typeof(string), typeof(string), "it.rEpLaCe(\"a\",\"b\")", null, QueryLimits.Defaults);
        }

        [TestMethod]
        public void ConcurrentCallsDoNotShareArgumentOrLimitDecisions()
        {
            MethodInfo method = typeof(int).GetMethod("ToString", [typeof(string)]);
            Parallel.For(0, 256, i =>
            {
                var limits = new QueryLimits { MaxNumericFormatPrecision = i % 2 };
                Assert.AreEqual(i % 2 == 1, ExpressionParser.IsMemberAccessAllowed(method, [Expression.Constant("D1")], limits));
                Assert.IsFalse(Allows(typeof(Environment).GetMethod("GetEnvironmentVariable", [typeof(string)])));
            });
        }

        [TestMethod]
        public void UnknownAndNullableMembersDoNotGrowTheRuleTable()
        {
            Assert.IsTrue(Allows(typeof(int).GetMethod("ToString", Type.EmptyTypes)));
            Type holder = typeof(ExpressionParser).GetNestedType("MemberRules", BindingFlags.NonPublic);
            FieldInfo field = holder.GetField("Values", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            object rules = field.GetValue(null);
            PropertyInfo count = rules.GetType().GetProperty("Count");
            int originalCount = (int)count.GetValue(rules);
            Type[] types = [typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(bool), typeof(Guid), typeof(DateTime)];
            Parallel.For(0, 256, i =>
            {
                Type argument = types[i % types.Length];
                Type unknown = typeof(UnknownMember<>).MakeGenericType(argument);
                Assert.IsFalse(Allows(unknown.GetMethod("ToString", [typeof(string)])));
                Assert.IsFalse(Allows(unknown.GetMethod("Parse", [typeof(string)])));
                Type nullable = typeof(Nullable<>).MakeGenericType(argument);
                Assert.IsTrue(Allows(nullable.GetMethod("ToString", Type.EmptyTypes)));
                Assert.IsFalse(Allows(nullable.GetMethod("GetValueOrDefault", Type.EmptyTypes)));
            });
            Assert.AreSame(rules, field.GetValue(null));
            Assert.AreEqual(originalCount, (int)count.GetValue(rules));
        }

#if NET
        [TestMethod]
        [DataRow(typeof(Half))]
        [DataRow(typeof(Int128))]
        [DataRow(typeof(UInt128))]
        [DataRow(typeof(IntPtr))]
        [DataRow(typeof(UIntPtr))]
        public void UnregisteredNumericTypesRemainDenied(Type type)
        {
            Assert.IsFalse(Allows(type.GetMethod("ToString", Type.EmptyTypes)));
            MethodInfo formatted = type.GetMethod("ToString", [typeof(string)]);
            if (formatted != null)
                Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(formatted, [Expression.Constant("D2")], QueryLimits.Defaults));
        }

        [TestMethod]
        public void ModernDateAndTimeNameOnlyApprovalsAreCharacterized()
        {
            foreach (MethodInfo method in typeof(DateOnly).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name is "AddDays" or "AddMonths" or "AddYears"))
                Assert.IsTrue(Allows(method), method.ToString());
            foreach (MethodInfo method in typeof(TimeOnly).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name is "Add" or "AddHours" or "AddMinutes"))
                Assert.IsTrue(Allows(method), method.ToString());
            Assert.IsTrue(Allows(typeof(TimeOnly).GetMethod("Add", [typeof(TimeSpan), typeof(int).MakeByRefType()])));
            Assert.IsFalse(Allows(typeof(DateOnly).GetMethod("Parse", [typeof(string), typeof(IFormatProvider)])));
            Assert.IsFalse(Allows(typeof(TimeOnly).GetMethod("Parse", [typeof(string), typeof(IFormatProvider)])));
        }
#endif

        private sealed class UnknownMember<T>
        {
            public string ToString(string format) => format;

            public static T Parse(string value) => default;
        }

        private static bool Allows(MethodBase method)
        {
            Assert.IsNotNull(method);
            Expression[] arguments = method.GetParameters().Select(p => (Expression)Expression.Default(
                p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType)).ToArray();
            return ExpressionParser.IsMemberAccessAllowed(method, arguments, QueryLimits.Defaults);
        }

        private static void AssertRoundTrip<T>(Expression<Func<T, bool>> predicate, params T[] values)
            => AssertRoundTrip(values.AsQueryable(), predicate);

        private static void AssertRoundTrip(IQueryable source, LambdaExpression predicate)
        {
            Type holderType = typeof(QueryValue<>).MakeGenericType(source.ElementType);
            object[] values = source.Cast<object>().ToArray();
            Array holders = Array.CreateInstance(holderType, values.Length);
            for (int i = 0; i < values.Length; i++)
                holders.SetValue(Activator.CreateInstance(holderType, [values[i]]), i);
            source = holders.AsQueryable();
            var parameter = Expression.Parameter(holderType, "");
            Expression body = new ParameterSubstitution(predicate.Parameters[0],
                Expression.Property(parameter, "Value")).Visit(predicate.Body);
            predicate = Expression.Lambda(body, parameter);
            IQueryable query = source.Provider.CreateQuery(Expression.Call(typeof(Queryable), "Where",
                [source.ElementType], source.Expression, Expression.Quote(predicate)));
            string text = QuerySerializer.Serialize(query).Single().Expression;
            CollectionAssert.AreEqual(query.Cast<object>().ToArray(),
                source.Where(text, null, QueryLimits.Defaults).Cast<object>().ToArray(), text);
        }

        private static void AssertParsed<T>(string text, Expression<Func<T>> expected)
        {
            LambdaExpression parsed = DynamicExpression.ParseLambda(typeof(int), typeof(T), text, null, QueryLimits.Defaults);
            Assert.AreEqual(((NewExpression)expected.Body).Constructor, ((NewExpression)parsed.Body).Constructor, text);
            T value = expected.Compile()();
            object actual = parsed.Compile().DynamicInvoke(0);
            Assert.AreEqual(value, actual, text);
            if (value is DateTime date)
                Assert.AreEqual(date.Kind, ((DateTime)actual).Kind, text);
            var parameter = Expression.Parameter(typeof(T), "");
            AssertRoundTrip(Expression.Lambda<Func<T, bool>>(Expression.Equal(parameter, parsed.Body), parameter), value);
        }

        private sealed class QueryValue<T>(T value)
        {
            public T Value { get; } = value;
        }

        private sealed class ParameterSubstitution(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
                => node == parameter ? replacement : base.VisitParameter(node);
        }
    }
}
