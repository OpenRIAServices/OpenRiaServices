using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore;

namespace System.Linq.Dynamic.UnitTests
{
    /// <summary>
    /// Characterization tests for operator and overload resolution in the dynamic query parser.
    /// </summary>
    /// <remarks>
    /// QuerySerializationTests in OpenRiaServices.Client\Test\Client.Test\Data covers round trips
    /// and server parsing, including nullable comparisons, decimal literals, chars, and string operations.
    /// Keep tests here focused on binding details not asserted there: exact types, overloads, conversions,
    /// or parser-only syntax. Exercising the same operator alone does not make those assertions redundant.
    /// </remarks>
    [TestClass]
    public class OperatorResolutionTests
    {
        [TestMethod]
        [DataRow(typeof(int), typeof(long), typeof(long))]
        [DataRow(typeof(int), typeof(uint), typeof(long))]
        [DataRow(typeof(uint), typeof(ulong), typeof(ulong))]
        [DataRow(typeof(byte), typeof(byte), typeof(int))]
        [DataRow(typeof(float), typeof(double), typeof(double))]
        [DataRow(typeof(int), typeof(decimal), typeof(decimal))]
        [DataRow(typeof(int?), typeof(int), typeof(int?))]
        public void ArithmeticPromotion(Type left, Type right, Type expected)
        {
            Expression body = Parse("a + b", left, right);

            Assert.AreEqual(ExpressionType.Add, body.NodeType);
            Assert.AreEqual(expected, body.Type);
        }

        [TestMethod]
        public void ArithmeticPromotion_LongAndULong_IsError()
        {
            Assert.ThrowsExactly<ParseException>(() => Parse("a + b", typeof(long), typeof(ulong)));
        }

        [TestMethod]
        public void DateTimeArithmetic()
        {
            Assert.AreEqual(typeof(TimeSpan), Parse("a - b", typeof(DateTime), typeof(DateTime)).Type);
            Assert.AreEqual(typeof(DateTime), Parse("a + b", typeof(DateTime), typeof(TimeSpan)).Type);
            Assert.AreEqual(typeof(DateTime), Parse("a - b", typeof(DateTime), typeof(TimeSpan)).Type);
            Assert.AreEqual(typeof(DateTimeOffset), Parse("a + b", typeof(DateTimeOffset), typeof(TimeSpan)).Type);
        }

        [TestMethod]
        public void Equality_LiftsToNullable()
        {
            var body = (BinaryExpression)Parse("a == b", typeof(bool), typeof(bool?));

            Assert.AreEqual(typeof(bool?), body.Left.Type);
            Assert.AreEqual(typeof(bool?), body.Right.Type);
        }

        [TestMethod]
        [DataRow(typeof(uint), typeof(long))]
        [DataRow(typeof(byte), typeof(int))]
        [DataRow(typeof(int?), typeof(int?))]
        public void Negation(Type operand, Type expected)
        {
            Expression body = Parse("-a", operand, typeof(int));

            Assert.AreEqual(ExpressionType.Negate, body.NodeType);
            Assert.AreEqual(expected, body.Type);
        }

        [TestMethod]
        public void Not_Nullable()
        {
            Expression body = Parse("!a", typeof(bool?), typeof(int));

            Assert.AreEqual(ExpressionType.Not, body.NodeType);
            Assert.AreEqual(typeof(bool?), body.Type);
        }

        [TestMethod]
        public void IntegerLiteral_IsParsedAsOperandType()
        {
            var body = (BinaryExpression)Parse("a == 1", typeof(long), typeof(int));

            var constant = (ConstantExpression)body.Right;
            Assert.AreEqual(typeof(long), constant.Type);
            Assert.AreEqual(1L, constant.Value);
        }

        [TestMethod]
        public void RealLiteral_IsParsedAsDecimal()
        {
            var body = (BinaryExpression)Parse("a > 1.5", typeof(decimal), typeof(int));

            var constant = (ConstantExpression)body.Right;
            Assert.AreEqual(typeof(decimal), constant.Type);
            Assert.AreEqual(1.5m, constant.Value);
        }

        [TestMethod]
        public void IntegerLiteralComparison_ResolvesAsInt()
        {
            var body = (BinaryExpression)Parse("1 < 2", typeof(int), typeof(int));

            Assert.AreEqual(typeof(bool), body.Type);
            Assert.AreEqual(typeof(int), body.Left.Type);
            Assert.IsInstanceOfType<ConstantExpression>(body.Left);
            Assert.IsInstanceOfType<ConstantExpression>(body.Right);
            Assert.IsTrue(Compile<bool>("1 < 2"));
        }

        /// <summary>Same-type operands must not get any redundant conversions.</summary>
        [TestMethod]
        [DataRow("a < b")]
        [DataRow("a == b")]
        [DataRow("a + b > a")]
        [DataRow("a > 1")]
        public void SameTypeOperands_HaveNoConversions(string expression)
        {
            Expression body = Parse(expression, typeof(int), typeof(int));

            Assert.AreEqual(typeof(bool), body.Type);
            Assert.IsFalse(ContainsConvert(body), body.ToString());
        }

        [TestMethod]
        public void CharComparedWithIntegerLiteral_ResolvesAsChar()
        {
            var body = (BinaryExpression)Parse("a == 65", typeof(char), typeof(int));

            Assert.AreEqual(typeof(char), body.Left.Type);
            Assert.AreEqual('A', ((ConstantExpression)body.Right).Value);
        }

        [TestMethod]
        public void StringComparison_UsesStringCompare()
        {
            var body = (BinaryExpression)Parse("a > b", typeof(string), typeof(string));

            Assert.AreEqual(ExpressionType.GreaterThan, body.NodeType);
            var call = (MethodCallExpression)body.Left;
            Assert.AreEqual(typeof(string).GetMethod(nameof(string.Compare), new[] { typeof(string), typeof(string) }), call.Method);
            Assert.AreEqual(0, ((ConstantExpression)body.Right).Value);
        }

        [TestMethod]
        public void StringConcat_UsesObjectOverload()
        {
            var call = (MethodCallExpression)Parse("a + b", typeof(string), typeof(int));

            Assert.AreEqual(typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(object), typeof(object) }), call.Method);
        }

        [TestMethod]
        public void MethodOverload_PicksBestConversion()
        {
            var call = (MethodCallExpression)Parse("Math.Max(a, b)", typeof(int), typeof(long));

            CollectionAssert.AreEqual(new[] { typeof(long), typeof(long) }, ParameterTypes(call.Method));
            Assert.AreEqual(typeof(long), call.Arguments[0].Type);
        }

        [TestMethod]
        public void MethodOverload_ExactMatch()
        {
            var call = (MethodCallExpression)Parse("Math.Abs(a)", typeof(int), typeof(int));

            CollectionAssert.AreEqual(new[] { typeof(int) }, ParameterTypes(call.Method));
        }

        [TestMethod]
        public void InstanceMethod_IsCaseInsensitive()
        {
            var call = (MethodCallExpression)Parse("a.tostring(\"D2\")", typeof(int), typeof(int));

            Assert.AreEqual(nameof(int.ToString), call.Method.Name);
            CollectionAssert.AreEqual(new[] { typeof(string) }, ParameterTypes(call.Method));
        }

        [TestMethod]
        public void Constructor_IsResolved()
        {
            var body = (NewExpression)Parse("DateTime(2020, 1, 2)", typeof(int), typeof(int));

            CollectionAssert.AreEqual(new[] { typeof(int), typeof(int), typeof(int) }, ParameterTypes(body.Constructor));
        }

        [TestMethod]
        public void Indexer_IsResolved()
        {
            var call = (MethodCallExpression)Parse("a[0]", typeof(string), typeof(int));

            Assert.AreEqual("get_Chars", call.Method.Name);
        }

        [TestMethod]
        public void Property_IsCaseInsensitive()
        {
            var member = (MemberExpression)Parse("a.length", typeof(string), typeof(int));

            Assert.AreEqual(typeof(string).GetProperty(nameof(string.Length)), member.Member);
        }

        [TestMethod]
        public void Aggregate_IsCaseInsensitive()
        {
            var call = (MethodCallExpression)Parse("a.count()", typeof(int[]), typeof(int));

            Assert.AreEqual(typeof(Enumerable), call.Method.DeclaringType);
            Assert.AreEqual(nameof(Enumerable.Count), call.Method.Name);
        }

        [TestMethod]
        public void Aggregate_SumPromotesSelector()
        {
            var call = (MethodCallExpression)Parse("a.Sum(it)", typeof(byte[]), typeof(int));

            Assert.AreEqual(nameof(Enumerable.Sum), call.Method.Name);
            Assert.AreEqual(typeof(int), call.Type);
        }

        [TestMethod]
        public void Aggregate_UnknownName_IsError()
        {
            Assert.ThrowsExactly<ParseException>(() => Parse("a.Reverse()", typeof(int[]), typeof(int)));
        }

        [TestMethod]
        public void RepeatedParsing_GivesSameResult()
        {
            // Reflection data is cached per type, so parsing the same text again must still bind the same members.
            for (int i = 0; i < 2; i++)
            {
                var call = (MethodCallExpression)Parse("Math.Max(a, b)", typeof(int), typeof(long));
                CollectionAssert.AreEqual(new[] { typeof(long), typeof(long) }, ParameterTypes(call.Method));
            }
        }

        private static Type[] ParameterTypes(MethodBase method)
            => method.GetParameters().Select(p => p.ParameterType).ToArray();

        private static T Compile<T>(string expression)
        {
            LambdaExpression lambda = DynamicExpression.ParseLambda(Array.Empty<ParameterExpression>(), typeof(T), expression, null, QueryLimits.Defaults);
            return ((Func<T>)lambda.Compile())();
        }

        private static bool ContainsConvert(Expression expression)
        {
            var finder = new ConvertFinder();
            finder.Visit(expression);
            return finder.Found;
        }

        private sealed class ConvertFinder : ExpressionVisitor
        {
            public bool Found { get; private set; }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                    Found = true;
                return base.VisitUnary(node);
            }
        }

        private static Expression Parse(string expression, Type a, Type b)
        {
            var parameters = new[] { Expression.Parameter(a, "a"), Expression.Parameter(b, "b") };
            return DynamicExpression.ParseLambda(parameters, null, expression, null, QueryLimits.Defaults).Body;
        }
    }
}
