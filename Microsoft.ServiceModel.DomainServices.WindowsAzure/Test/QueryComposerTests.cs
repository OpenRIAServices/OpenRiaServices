using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure.Test
{
    [TestClass]
    public class QueryComposerTests
    {
        [TestMethod]
        [Description("Tests that a query can be rebased from one root to another")]
        public void Rebase()
        {
            IQueryable<string> queryRoot = new[] { "query", "root" }.AsQueryable();
            IQueryable<string> query = queryRoot.Where(s => true);

            MethodCallExpression mce = query.Expression as MethodCallExpression;
            Assert.IsNotNull(mce,
                "Expression should be a method call.");
            Assert.AreEqual("Where", mce.Method.Name,
                "Expression should be a Where call.");
            Assert.AreEqual(2, mce.Arguments.Count,
                "Expression should have 2 arguments.");

            ConstantExpression ce = mce.Arguments[0] as ConstantExpression;
            Assert.IsNotNull(ce,
                "The first expression argument should be constant.");
            Assert.AreEqual(queryRoot, ce.Value,
                "The first expression argument should be the query root.");

            IQueryable<string> newQueryRoot = new[] { "new", "query", "root" }.AsQueryable();

            IQueryable rebasedQuery = QueryComposer.Compose(newQueryRoot, query);

            mce = rebasedQuery.Expression as MethodCallExpression;
            Assert.IsNotNull(mce,
                "Rebased expression should be a method call.");
            Assert.AreEqual("Where", mce.Method.Name,
                "Rebased expression should be a Where call.");
            Assert.AreEqual(2, mce.Arguments.Count,
                "Rebased expression should have 2 arguments.");

            ce = mce.Arguments[0] as ConstantExpression;
            Assert.IsNotNull(ce,
                "The first rebased expression argument should be constant.");
            Assert.AreEqual(newQueryRoot, ce.Value,
                "The first rebased expression argument should be the new query root.");
        }

        [TestMethod]
        [Description("Tests that a fully supported query will return a supported part but no unsupported part")]
        public void SplitSupportedQuery()
        {
            IQueryable<string> queryRoot = new[] { "query", "root" }.AsQueryable();
            IQueryable<string> query = queryRoot.Where(s => true).Take(5);

            IQueryable supportedQuery;
            IQueryable unsupportedQuery;

            supportedQuery = QueryComposer.Split(query, out unsupportedQuery);

            MethodCallExpression mce = supportedQuery.Expression as MethodCallExpression;
            Assert.IsNotNull(mce,
                "The first supported expression should be a method call.");
            Assert.AreEqual("Take", mce.Method.Name,
                "The first supported expression should be a Take call.");
            Assert.AreEqual(2, mce.Arguments.Count,
                "The first supported expression should have 2 arguments.");

            MethodCallExpression mce2 = mce.Arguments[0] as MethodCallExpression;
            Assert.IsNotNull(mce2,
                "The second supported expression should be a method call.");
            Assert.AreEqual("Where", mce2.Method.Name,
                "The second supported expression should be a Where call.");
            Assert.AreEqual(2, mce2.Arguments.Count,
                "The second supported expression should have 2 arguments.");

            ConstantExpression ce = mce2.Arguments[0] as ConstantExpression;
            Assert.IsNotNull(ce,
                "The first argument in the second supported expression should be constant.");

            Assert.IsNull(unsupportedQuery,
                "The unsupported query should be null.");
        }

        [TestMethod]
        [Description("Tests that a partially supported query will return both supported and unsupported parts")]
        public void SplitPartiallySupportedQuery()
        {
            IQueryable<string> queryRoot = new[] { "query", "root" }.AsQueryable();
            IQueryable<string> query = queryRoot.Where(s => true).Take(5).OrderBy(s => s).ThenBy(s => s);

            IQueryable supportedQuery;
            IQueryable unsupportedQuery;

            supportedQuery = QueryComposer.Split(query, out unsupportedQuery);

            MethodCallExpression mce = supportedQuery.Expression as MethodCallExpression;
            Assert.IsNotNull(mce,
                "The first supported expression should be a method call.");
            Assert.AreEqual("Take", mce.Method.Name,
                "The first supported expression should be a Take call.");
            Assert.AreEqual(2, mce.Arguments.Count,
                "The first supported expression should have 2 arguments.");

            MethodCallExpression mce2 = mce.Arguments[0] as MethodCallExpression;
            Assert.IsNotNull(mce2,
                "The second supported expression should be a method call.");
            Assert.AreEqual("Where", mce2.Method.Name,
                "The second supported expression should be a Where call.");
            Assert.AreEqual(2, mce2.Arguments.Count,
                "The second supported expression should have 2 arguments.");

            ConstantExpression ce = mce2.Arguments[0] as ConstantExpression;
            Assert.IsNotNull(ce,
                "The first argument in the second supported expression should be constant.");

            mce = unsupportedQuery.Expression as MethodCallExpression;
            Assert.IsNotNull(mce,
                "The first unsupported expression should be a method call.");
            Assert.AreEqual("ThenBy", mce.Method.Name,
                "The first unsupported expression should be a ThenBy call.");
            Assert.AreEqual(2, mce.Arguments.Count,
                "The first unsupported expression should have 2 arguments.");

            mce2 = mce.Arguments[0] as MethodCallExpression;
            Assert.IsNotNull(mce2,
                "The second unsupported expression should be a method call.");
            Assert.AreEqual("OrderBy", mce2.Method.Name,
                "The second unsupported expression should be an OrderBy call.");
            Assert.AreEqual(2, mce2.Arguments.Count,
                "The second unsupported expression should have 2 arguments.");

            ce = mce2.Arguments[0] as ConstantExpression;
            Assert.IsNotNull(ce,
                "The first argument in the second unsupported expression should be constant.");
        }

        [TestMethod]
        [Description("Tests that an unsupported query will return an unsupported part but no supported part")]
        public void SplitUnsupportedQuery()
        {
            IQueryable<string> queryRoot = new[] { "query", "root" }.AsQueryable();
            IQueryable<string> query = queryRoot.OrderBy(s => s).ThenBy(s => s);

            IQueryable supportedQuery;
            IQueryable unsupportedQuery;

            supportedQuery = QueryComposer.Split(query, out unsupportedQuery);

            Assert.IsNull(supportedQuery,
                "The supported query should be null.");

            MethodCallExpression mce = unsupportedQuery.Expression as MethodCallExpression;
            Assert.IsNotNull(mce,
                "The first unsupported expression should be a method call.");
            Assert.AreEqual("ThenBy", mce.Method.Name,
                "The first unsupported expression should be a ThenBy call.");
            Assert.AreEqual(2, mce.Arguments.Count,
                "The first unsupported expression should have 2 arguments.");

            MethodCallExpression mce2 = mce.Arguments[0] as MethodCallExpression;
            Assert.IsNotNull(mce2,
                "The second unsupported expression should be a method call.");
            Assert.AreEqual("OrderBy", mce2.Method.Name,
                "The second unsupported expression should be an OrderBy call.");
            Assert.AreEqual(2, mce2.Arguments.Count,
                "The second unsupported expression should have 2 arguments.");

            ConstantExpression ce = mce2.Arguments[0] as ConstantExpression;
            Assert.IsNotNull(ce,
                "The first argument in the second unsupported expression should be constant.");
        }
    }
}
