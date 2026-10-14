using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting;
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
            AssertTooComplex(() => DynamicExpression.ParseLambda(itType, typeof(bool), expression, null));
        }

        [TestMethod]
        public void FlatOperatorAndMemberChainsAreRejected()
        {
            int count = ExpressionParser.MaxExpressionDepth + 1;
            AssertTooComplex(() => ParseBool(string.Join(" and ", Enumerable.Repeat("true", count))));
            string memberChain = "it" + string.Concat(Enumerable.Repeat(".Date", count)) + " == it";
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(DateTime), typeof(bool), memberChain, null));
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
            DynamicExpression.ParseLambda(typeof(int), typeof(int), "-(" + expression + ")", null);
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(int), typeof(int), "-(-(" + expression + "))", null));
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
            var parser = new ExpressionParser([Expression.Parameter(typeof(int), "")], Nest("it", ExpressionParser.MaxExpressionDepth + 1), null);
            AssertTooComplex(() => parser.ParseOrdering().ToArray());
            AssertTooComplex(() => new[] { 1 }.AsQueryable()
                .OrderBy(string.Join(",", Enumerable.Repeat("it", 1000)), null, ExpressionParser.MaxExpressionDepth));
        }

        [TestMethod]
        public void ConfiguredDepthAppliesToParserAndTree()
        {
            DynamicExpression.ParseLambda(typeof(int), typeof(bool), Nest("true", 9), null, 10);
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(int), typeof(bool), Nest("true", 10), null, 10));
            AssertTooComplex(() => DynamicExpression.ParseLambda(typeof(int), typeof(bool),
                string.Join(" and ", Enumerable.Repeat("true", 11)), null, 10));
            var ordering = new ExpressionParser([Expression.Parameter(typeof(int), "")], Nest("it", 10), null, 10);
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
            var lambda = (Expression<Func<string, string>>)DynamicExpression.ParseLambda(typeof(string), typeof(string), expression, null);
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
            source.OrderBy(ReplaceChain(3, "a", "aaa"), null, ExpressionParser.MaxExpressionDepth).Cast<string>().ToArray();
            AssertTooComplex(() => source.OrderBy(ReplaceChain(4, "a", "aaa"), null, ExpressionParser.MaxExpressionDepth));
            AssertTooComplex(() => source.OrderBy(
                string.Join(",", Enumerable.Repeat("it.Replace(\"a\",\"aaa\")", 4)), null, ExpressionParser.MaxExpressionDepth));
        }

        private static string ReplaceChain(int calls, string oldValue, string newValue) =>
            "it" + string.Concat(Enumerable.Repeat($".Replace(\"{oldValue}\",\"{newValue}\")", calls));

        private static LambdaExpression ParseString(string expression) =>
            DynamicExpression.ParseLambda(typeof(string), typeof(string), expression, null);

        [TestMethod]
        public void QueryPartCountAndCombinedLengthAreBounded()
        {
            var description = DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService));
            var source = Array.Empty<EntityWithExcludedMember>().AsQueryable();
            var parts = Enumerable.Repeat(new ServiceQueryPart("take", "1"), QueryDeserializer.MaxQueryParts).ToArray();
            QueryDeserializer.Deserialize(description, source, parts);
            AssertTooComplex(() => QueryDeserializer.Deserialize(description, source, parts.Append(new ServiceQueryPart("take", "1"))));

            var part = new ServiceQueryPart("where", "true".PadRight(ExpressionParser.MaxExpressionLength));
            QueryDeserializer.Deserialize(description, source, Enumerable.Repeat(part, 4));
            AssertTooComplex(() => QueryDeserializer.Deserialize(description, source, Enumerable.Repeat(part, 5)));
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
            DynamicExpression.ParseLambda(typeof(int), typeof(bool), expression, null);

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
