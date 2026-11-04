using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        [DataRow(typeof(int?))]
        [DataRow(typeof(DateTime?))]
        public void NullableToStringBindsWithoutInvocationCacheEntries(Type type)
        {
            LambdaExpression lambda = DynamicExpression.ParseLambda(type, typeof(string), "it.ToString()", null, QueryLimits.Defaults);
            var call = (MethodCallExpression)lambda.Body;
            Assert.AreEqual(type, call.Method.DeclaringType);
            Assert.AreEqual("ToString", call.Method.Name);
            Delegate compiled = lambda.Compile();
            Assert.AreEqual(string.Empty, compiled.DynamicInvoke(new object[] { null }));
            object value = Activator.CreateInstance(Nullable.GetUnderlyingType(type));
            Assert.AreEqual(value.ToString(), compiled.DynamicInvoke(value));
        }

        [TestMethod]
        [DataRow(typeof(string))]
        [DataRow(typeof(int))]
        [DataRow(typeof(DateTime))]
        [DataRow(typeof(Uri))]
#if NET
        [DataRow(typeof(TimeOnly))]
#endif
        public void InvocationCacheContainsOnlyPermittedSignatures(Type type)
        {
            ExpressionParser.InvocationMembers members = ExpressionParser.GetInvocationMembers(type);
            ExpressionParser.MethodSignature[] signatures = members.InstanceMethods.Values
                .Concat(members.StaticMethods.Values)
                .SelectMany(s => s)
                .Concat(members.Constructors)
                .ToArray();
            Assert.IsNotEmpty(signatures);
            foreach (ExpressionParser.MethodSignature signature in signatures)
            {
                Assert.AreNotEqual(ExpressionParser.MemberRule.Denied, signature.Rule, signature.Method.ToString());
                Assert.DoesNotContain(p => p.IsByRef, signature.ParameterTypes, signature.Method.ToString());
            }
        }

        [TestMethod]
        public void DeniedOverloadsDoNotParticipateInBinding()
        {
            ParameterExpression[] parameters = [Expression.Parameter(typeof(string), "text"), Expression.Parameter(typeof(object), "value")];
            var denied = Assert.ThrowsExactly<ParseException>(() =>
                DynamicExpression.ParseLambda(parameters, typeof(bool), "text.Equals(value)", null, QueryLimits.Defaults));
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, Resource.NoApplicableMethod, "Equals", nameof(String)), denied.Message);

            LambdaExpression lambda = DynamicExpression.ParseLambda(typeof(int), typeof(string), "it.ToString(null)", null, QueryLimits.Defaults);
            var call = (MethodCallExpression)lambda.Body;
            Assert.AreEqual(typeof(int).GetMethod("ToString", [typeof(string)]), call.Method);
            Assert.AreEqual("42", lambda.Compile().DynamicInvoke(42));
        }

        [TestMethod]
        public void ParserChecksCurrentLimitsAgainstCachedRules()
        {
            var limits = new QueryLimits { MaxNumericFormatPrecision = 2 };
            Assert.ThrowsExactly<ParseException>(() =>
                DynamicExpression.ParseLambda(typeof(int), typeof(string), "it.ToString(\"D3\")", null, limits));
            limits.MaxNumericFormatPrecision = 3;
            DynamicExpression.ParseLambda(typeof(int), typeof(string), "it.ToString(\"D3\")", null, limits);
            limits.MaxNumericFormatPrecision = 2;
            Assert.ThrowsExactly<ParseException>(() =>
                DynamicExpression.ParseLambda(typeof(int), typeof(string), "it.ToString(\"D3\")", null, limits));
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
                Assert.AreEqual(!method.GetParameters().Any(p => p.ParameterType.IsByRef), Allows(method), method.ToString());
            Assert.IsFalse(Allows(typeof(TimeOnly).GetMethod("Add", [typeof(TimeSpan), typeof(int).MakeByRefType()])));
            Assert.IsFalse(Allows(typeof(DateOnly).GetMethod("Parse", [typeof(string), typeof(IFormatProvider)])));
            Assert.IsFalse(Allows(typeof(TimeOnly).GetMethod("Parse", [typeof(string), typeof(IFormatProvider)])));
        }
#endif

        private sealed class UnknownMember<T>
        {
            public string ToString(string format) => format;

            public string tostring(string format) => format;

            public static T Parse(string value) => default;
        }

        private static bool Allows(MethodBase method)
        {
            Assert.IsNotNull(method);
            Expression[] arguments = method.GetParameters().Select(p => (Expression)Expression.Default(
                p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType)).ToArray();
            return ExpressionParser.IsMemberAccessAllowed(method, arguments, QueryLimits.Defaults);
        }
    }
}
