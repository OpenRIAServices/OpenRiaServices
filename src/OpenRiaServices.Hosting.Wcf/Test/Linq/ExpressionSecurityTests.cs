using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;

namespace System.Linq.Dynamic.UnitTests
{
    [TestClass]
    public class ExpressionSecurityTests
    {
        [TestMethod]
        public void ParenthesisDepthBoundary()
        {
            ParseBool(Nest("true", ExpressionParser.MaxExpressionDepth - 1));
            AssertTooComplex(() => ParseBool(Nest("true", ExpressionParser.MaxExpressionDepth)));
        }

        [TestMethod]
        public void UnaryDepthBoundary()
        {
            ParseBool(new string('!', ExpressionParser.MaxExpressionDepth - 1) + "true");
            AssertTooComplex(() => ParseBool(new string('!', ExpressionParser.MaxExpressionDepth) + "true"));
        }

        [TestMethod]
        [DataRow("parentheses")]
        [DataRow("unary")]
        [DataRow("conditional")]
        [DataRow("method")]
        [DataRow("aggregate")]
        public void RecursiveSyntaxIsRejected(string syntax)
        {
            string expression = syntax == "method" ? "1" : "true";
            for (int i = 0; i < ExpressionParser.MaxExpressionDepth + 1; i++)
            {
                expression = syntax switch
                {
                    "parentheses" => "(" + expression + ")",
                    "unary" => "!" + expression,
                    "conditional" => "iif(true," + expression + ",false)",
                    "method" => "Math.Abs(" + expression + ")",
                    "aggregate" => "it.Children.Any(" + expression + ")",
                    _ => throw new InvalidOperationException()
                };
            }

            if (syntax == "method")
                expression += " > 0";
            Type itType = syntax == "aggregate" ? typeof(RecursiveEntity) : typeof(int);
            AssertTooComplex(() => DynamicExpression.ParseLambda(itType, typeof(bool), expression, null, QueryLimits.Defaults));
        }

        [TestMethod]
        public void FlatOperatorAndMemberChainsAreRejected()
        {
            int count = ExpressionParser.MaxExpressionDepth + 1;
            AssertTooComplex(() => ParseBool(string.Join(" and ", Enumerable.Repeat("true", count))));
            string memberChain = "it" + string.Concat(Enumerable.Repeat(".Date", count)) + " == it";
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(DateTime), typeof(bool), memberChain, null, QueryLimits.Defaults));
        }

        [TestMethod]
        public void ClientGeneratedOrChainBelowLimitIsAccepted()
        {
            // The client QuerySerializer wraps every binary expression in parentheses,
            // so "x.Id == 1 || x.Id == 2 || ..." nests one level per term.
            int terms = ExpressionParser.MaxExpressionDepth - 10;
            string expression = "(it==0)";
            for (int i = 1; i < terms; i++)
                expression = "(" + expression + "||(it==" + i + "))";

            ParseBool(expression);
        }

        [TestMethod]
        public void NodeBudgetBoundary()
        {
            string expression = "1";
            for (int i = 0; i < 11; i++)
                expression = "(" + expression + "+" + expression + ")";

            // 2048 constants + 2047 additions + one negation.
            DynamicExpression.ParseLambda(typeof(int), typeof(int), "-(" + expression + ")", null, QueryLimits.Defaults);
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(int), typeof(int), "-(-(" + expression + "))", null, QueryLimits.Defaults));
        }

        [TestMethod]
        public void ExpressionLengthBoundary()
        {
            ParseBool("true".PadRight(ExpressionParser.MaxExpressionLength));
            AssertTooComplex(() => ParseBool("true".PadRight(ExpressionParser.MaxExpressionLength + 1)));
        }

        [TestMethod]
        public void OrderingIsAlsoBounded()
        {
            var parser = new ExpressionParser([Expression.Parameter(typeof(int), "")], Nest("it", ExpressionParser.MaxExpressionDepth + 1), null, QueryLimits.Defaults);
            AssertTooComplex(() => parser.ParseOrdering().ToArray());
            AssertTooComplex(() => new[] { 1 }.AsQueryable()
                .OrderBy(string.Join(",", Enumerable.Repeat("it", 1000)), null, QueryLimits.Defaults));
        }

        [TestMethod]
        public void ConfiguredDepthAppliesToParserAndTree()
        {
            var limits = new QueryLimits { MaxExpressionDepth = 10 };
            DynamicExpression.ParseLambda(typeof(int), typeof(bool), Nest("true", 9), null, limits);
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(int), typeof(bool), Nest("true", 10), null, limits));
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(int), typeof(bool),
                string.Join(" and ", Enumerable.Repeat("true", 11)), null, limits));
            var ordering = new ExpressionParser([Expression.Parameter(typeof(int), "")], Nest("it", 10), null, limits);
            AssertTooComplex(() => ordering.ParseOrdering().ToArray());
            ParseBool(Nest("true", 10));
        }

        [TestMethod]
        [DataRow(1, 3, 3)]
        [DataRow(2, 3, 9)]
        [DataRow(3, 3, 27)]
        [DataRow(2, 10, 100)]
        public void GrowingReplaceWithinBudgetIsAccepted(int calls, int replacementLength, int expectedLength)
        {
            string expression = ReplaceChain(calls, "a", new string('a', replacementLength));
            var lambda = (Expression<Func<string, string>>)DynamicExpression.ParseLambda(typeof(string), typeof(string), expression, null, QueryLimits.Defaults);
            Assert.AreEqual(expectedLength, lambda.Compile()("a").Length);
        }

        [TestMethod]
        public void GrowingReplaceBeyondBudgetIsRejected()
        {
            AssertTooComplex(() => ParseString(ReplaceChain(4, "a", "aa")));
            AssertTooComplex(() => ParseString(ReplaceChain(2, "a", new string('a', 11))));
            AssertTooComplex(() => ParseString(ReplaceChain(3, "a", new string('a', 5))));
        }

        [TestMethod]
        public void ReplaceFactorUsesOldValueLengthAndRoundsUp()
        {
            ParseString(ReplaceChain(3, "aa", "aaaaaa")); // 3 * 3 * 3 = 27.
            AssertTooComplex(() => ParseString(ReplaceChain(3, "aa", new string('a', 9)))); // ceil(9/2)^3 = 125.
            ParseString(ReplaceChain(2, "aa", new string('a', 20))); // 10 * 10 = 100.
        }

        [TestMethod]
        public void NonGrowingReplaceDoesNotConsumeBudget()
        {
            ParseString(ReplaceChain(10, "aa", "a"));
            ParseString(ReplaceChain(10, "a", "a"));
            ParseString(ReplaceChain(10, "a", ""));
            ParseString("it.Replace(\"a\",null).Replace(\"a\",null).Replace(\"a\",null).Replace(\"a\",null)");
            ParseString("it.Replace('a','b').Replace('b','c').Replace('c','d').Replace('d','e')");
            ParseString(ReplaceChain(10, "aa", "a") + ".Replace(\"a\",\"aaa\").Replace(\"a\",\"aaa\").Replace(\"a\",\"aaa\")");
        }

        [TestMethod]
        public void ReplaceBudgetIncludesDynamicSearchAndSeparateBranches()
        {
            ParseString("it.Replace(it.Substring(0,1),\"aaa\").Replace(\"a\",\"aaa\").Replace(\"a\",\"aaa\")");
            AssertTooComplex(() => ParseString("it.Replace(it,\"aaaaaaaaaaa\").Replace(it,\"aaaaaaaaaaa\")"));
            AssertTooComplex(() => ParseString(
                "String.Concat(it.Replace(\"a\",\"aaa\"),it.Replace(\"a\",\"aaa\"),it.Replace(\"a\",\"aaa\"),it.Replace(\"a\",\"aaa\"))"));
            Assert.ThrowsExactly<ParseException>(() => ParseString(
                "it.Replace(\"a\",\"aaa\").Replace(\"a\",it.Replace(\"a\",\"aaa\"))"));
        }

        [TestMethod]
        public void OrderingUsesReplaceBudgetToo()
        {
            var source = new[] { "a" }.AsQueryable();
            source.OrderBy(ReplaceChain(3, "a", "aaa"), null, QueryLimits.Defaults).Cast<string>().ToArray();
            AssertTooComplex(() => source.OrderBy(ReplaceChain(4, "a", "aaa"), null, QueryLimits.Defaults));
            AssertTooComplex(() => source.OrderBy(
                string.Join(",", Enumerable.Repeat("it.Replace(\"a\",\"aaa\")", 4)), null, QueryLimits.Defaults));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("D")]
        [DataRow("D100")]
        [DataRow("D000100")]
        [DataRow("f100")]
        [DataRow("G100")]
        [DataRow("X100")]
        [DataRow("0.00")]
        [DataRow("#,##0.00;(#,##0.00);zero")]
        [DataRow("'D101'0")]
        [DataRow("D101#")]
        public void BoundedNumericFormatsPreserveResults(string format)
        {
            string argument = $"\"{format}\"";
            var lambda = (Expression<Func<int, string>>)DynamicExpression.ParseLambda(
                typeof(int), typeof(string), $"it.ToString({argument})", null, QueryLimits.Defaults);
            Assert.AreEqual(12.ToString(format), lambda.Compile()(12));
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
        public void NumericFormattingIsCheckedForEveryNumericType(Type type)
        {
            DynamicExpression.ParseLambda(type, typeof(string), "it.ToString()", null, QueryLimits.Defaults);
            DynamicExpression.ParseLambda(type, typeof(string), "it.ToString(\"F100\")", null, QueryLimits.Defaults);
            Assert.ThrowsExactly<ParseException>(() =>
                DynamicExpression.ParseLambda(type, typeof(string), "it.ToString(\"F101\")", null, QueryLimits.Defaults));
        }

        [TestMethod]
        [DataRow("\"D101\"")]
        [DataRow("\"d000101\"")]
        [DataRow("\"G101\"")]
        [DataRow("\"B101\"")]
        [DataRow("\"D2147483648\"")]
        [DataRow("it.ToString()")]
        [DataRow("String.Concat(\"D\",\"2\")")]
        [DataRow("\"D\" + \"2\"")]
        [DataRow("iif(true,\"D2\",\"D3\")")]
        public void UnboundedOrComputedNumericFormatsAreRejectedBeforeExecution(string argument)
        {
            Assert.ThrowsExactly<ParseException>(() => DynamicExpression.ParseLambda(
                typeof(int), typeof(string), $"it.ToString({argument})", null, QueryLimits.Defaults));
        }

        [TestMethod]
        public void NumericFormatLengthPrecisionAndTerminatorsAreCheckedWithoutFormatting()
        {
            var method = typeof(int).GetMethod(nameof(int.ToString), [typeof(string)]);
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(method, [Expression.Constant(null, typeof(string))], QueryLimits.Defaults));
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(method, [Expression.Constant(new string('0', 64))], QueryLimits.Defaults));
            foreach (string format in new[] { new string('0', 65), "D" + new string('9', 99), "D101\0suffix" })
            {
                Assert.IsFalse(ExpressionParser.IsMemberAccessAllowed(method, [Expression.Constant(format)], QueryLimits.Defaults), format);
            }
            Assert.IsTrue(ExpressionParser.IsMemberAccessAllowed(method, [Expression.Constant("D100\0suffix")], QueryLimits.Defaults));
        }

        [TestMethod]
        public void StringFormatIsNotAllowedForClientQueries()
        {
            Assert.ThrowsExactly<ParseException>(() => DynamicExpression.ParseLambda(
                typeof(int), typeof(string), "String.Format(\"{0:D999999999}\", it)", null, QueryLimits.Defaults));
        }

        [TestMethod]
        public void ConfiguredNumericLimitsApplyToFilteringOrderingAndQueryParts()
        {
            var limits = new QueryLimits { MaxNumericFormatPrecision = 3, MaxNumericFormatLength = 4 };
            var source = new[] { 12 }.AsQueryable();
            Assert.AreEqual(1, source.Where("it.ToString(\"D3\").Length == 3", null, limits).Cast<int>().Count());
            source.OrderBy("it.ToString(\"0000\")", null, limits).Cast<int>().ToArray();
            foreach (string format in new[] { "D4", "00000" })
            {
                Assert.ThrowsExactly<ParseException>(() => source.Where($"it.ToString(\"{format}\") != null", null, limits));
                Assert.ThrowsExactly<ParseException>(() => source.OrderBy($"it.ToString(\"{format}\")", null, limits));

                var description = DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService));
                var entities = Array.Empty<EntityWithExcludedMember>().AsQueryable();
                Assert.ThrowsExactly<ParseException>(() => QueryDeserializer.Deserialize(description, entities,
                    [new ServiceQueryPart("where", $"Int32(1).ToString(\"{format}\") != null")], limits));
                Assert.ThrowsExactly<ParseException>(() => QueryDeserializer.Deserialize(description, entities,
                    [new ServiceQueryPart("orderby", $"Int32(1).ToString(\"{format}\")")], limits));
            }
            source.Where("it.ToString(\"D4\") != null", null, QueryLimits.Defaults).Cast<int>().ToArray();
            DynamicExpression.ParseLambda(typeof(int), typeof(string), "it.ToString(\"F0\")", null,
                new QueryLimits { MaxNumericFormatPrecision = 0 });
        }

        [TestMethod]
        [DataRow(typeof(DateTime))]
        [DataRow(typeof(DateTimeOffset))]
        [DataRow(typeof(TimeSpan))]
        [DataRow(typeof(Guid))]
        public void NonNumericFormattingStillAcceptsDynamicFormats(Type type)
        {
            DynamicExpression.ParseLambda(
                [Expression.Parameter(type, "value"), Expression.Parameter(typeof(string), "format")],
                typeof(string), "value.ToString(format)", null, QueryLimits.Defaults);
        }

        private static string ReplaceChain(int calls, string oldValue, string newValue) =>
            "it" + string.Concat(Enumerable.Repeat($".Replace(\"{oldValue}\",\"{newValue}\")", calls));

        private static LambdaExpression ParseString(string expression) =>
            DynamicExpression.ParseLambda(typeof(string), typeof(string), expression, null, QueryLimits.Defaults);

        [TestMethod]
        public void QueryPartCountAndCombinedLengthAreBounded()
        {
            var description = DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService));
            var source = Array.Empty<EntityWithExcludedMember>().AsQueryable();
            var parts = Enumerable.Repeat(new ServiceQueryPart("take", "1"), QueryDeserializer.MaxQueryParts).ToArray();
            QueryDeserializer.Deserialize(description, source, parts, QueryLimits.Defaults);
            AssertTooComplex(() => QueryDeserializer.Deserialize(description, source, parts.Append(new ServiceQueryPart("take", "1")), QueryLimits.Defaults));

            var part = new ServiceQueryPart("where", "true".PadRight(ExpressionParser.MaxExpressionLength));
            QueryDeserializer.Deserialize(description, source, Enumerable.Repeat(part, 4), QueryLimits.Defaults);
            AssertTooComplex(() => QueryDeserializer.Deserialize(description, source, Enumerable.Repeat(part, 5), QueryLimits.Defaults));
        }

        [TestMethod]
        public void DeepInputOnSmallStackIsRejected()
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    ParseBool(Nest("true", 5000));
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            }, 256 * 1024);
            thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(10)));
            Assert.IsInstanceOfType<ParseException>(error);
        }

        private static LambdaExpression ParseBool(string expression) =>
            DynamicExpression.ParseLambda(typeof(int), typeof(bool), expression, null, QueryLimits.Defaults);

        private static string Nest(string expression, int depth) =>
            new string('(', depth) + expression + new string(')', depth);

        private static void AssertTooComplex(Action action)
        {
            var error = Assert.ThrowsExactly<ParseException>(action);
            StringAssert.Contains(error.Message, Resource.ExpressionTooComplex);
        }

        private sealed class RecursiveEntity
        {
            public IEnumerable<RecursiveEntity> Children { get; set; }
        }
    }
}
