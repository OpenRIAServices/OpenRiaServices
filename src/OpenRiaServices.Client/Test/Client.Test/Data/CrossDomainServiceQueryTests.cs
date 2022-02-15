using System;
using System.Collections.Generic;
using System.Linq;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices.LTS;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Test
{
    /// <summary>
    /// Any test cases added to this class will be run against the set of all domain services,
    /// for example EF, LTS, etc. Add provider specific tests to the appropriate derived class.
    /// Note: we're using the same generated client for both EF and LTS tests, varying only the
    /// provider string that the tests use.
    /// </summary>
#if !SILVERLIGHT
    [TestClass]
#endif
    public abstract class CrossDomainServiceQueryTests : DomainContextTestBase<Catalog>
    {
        public CrossDomainServiceQueryTests(Uri serviceUri, ProviderType providerType)
            : base(serviceUri, providerType)
        {
        }

        protected abstract Northwind CreateNorthwind();

        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        [Asynchronous]
        public void TestCalculatedField()
        {
            Northwind nw = CreateNorthwind();

            LoadOperation lo = nw.Load(nw.GetOrdersQuery().Take(1), false);

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.IsNull(lo.Error);

                Assert.AreEqual(1, nw.Orders.Count);
                DataTests.Northwind.LTS.Order order = (DataTests.Northwind.LTS.Order)nw.Orders.List[0];
                Assert.AreEqual("OrderID: " + order.OrderID.ToString(), order.FormattedName);
            });
            EnqueueTestComplete();
        }

        /// <summary>
        /// Test translation and execution of queries using EntityCollection.Count (mapping to server
        /// side EntitySet/EntityCollection count members)
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestEntityCollectionCountQuery()
        {
            Catalog catalog = CreateDomainContext();

            Load(catalog.GetPurchaseOrdersQuery().Where(p => p.PurchaseOrderDetails.Count == 3).Take(5));

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                foreach (PurchaseOrder order in catalog.PurchaseOrders)
                {
                    Assert.AreEqual(3, order.PurchaseOrderDetails.Count);
                }
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestSelfReference()
        {
            Catalog catalog = CreateDomainContext();

            Load(catalog.GetEmployeesQuery());

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // the boss reports to no-one
                Employee theBoss = catalog.Employees.Single(p => p.EmployeeID == 109);
                Assert.IsNull(theBoss.Manager);
                Assert.AreEqual(6, theBoss.Reports.Count());
                foreach (Employee report in theBoss.Reports)
                {
                    Assert.AreEqual(theBoss.EmployeeID, report.ManagerID);
                }

                // everyone else has a manager - verify the reference
                foreach (Employee emp in catalog.Employees.Where(p => p.EmployeeID != 109))
                {
                    Employee manager = emp.Manager;
                    Assert.IsNotNull(manager);
                    Assert.AreEqual(emp.Manager.EmployeeID, emp.ManagerID);

                    foreach (Employee report in emp.Reports)
                    {
                        Assert.AreEqual(emp.EmployeeID, report.ManagerID);
                    }
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestProjectionInclude()
        {
            Northwind ctxt = CreateNorthwind();

            LoadOperation lo = ctxt.Load(ctxt.GetProductsQuery().Where(p => p.ProductID != 1), false);

            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(null, lo.Error, "Load should succeed without error");
                Assert.AreNotEqual(0, ctxt.Products.Count);

                foreach (DataTests.Northwind.LTS.Product product in ctxt.Products)
                {
                    // All rows except product 1 should have references to supplier and category
                    Assert.IsTrue(!string.IsNullOrEmpty(product.SupplierName), "Supplier not loaded");
                    Assert.IsTrue(!string.IsNullOrEmpty(product.CategoryName), "Category not loaded");
                }
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test eager loading of association properties as well as FK expansion for EF
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestAssociations_OneToMany()
        {
            Catalog catalog = CreateDomainContext();

            Load(catalog.GetPurchaseOrdersQuery().Take(5));
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                List<PurchaseOrder> orders = catalog.PurchaseOrders.ToList();
                Assert.IsTrue(orders.Count > 0);
                PurchaseOrder order = orders.First();
                PurchaseOrderDetail detail = order.PurchaseOrderDetails.First();
                Product product = detail.Product;
                Assert.IsNotNull(product);
                Assert.AreEqual(product.ProductID, detail.ProductID);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_NoQuery()
        {
            Catalog catalog = CreateDomainContext();
            Load(catalog.GetProductsQuery());
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                List<Product> products = catalog.Products.ToList();
                Assert.IsTrue(products.Count == 504);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_TotalCount_WithPagingAndPredicate()
        {
            Catalog catalog = CreateDomainContext();
            var query = from p in catalog.GetProductsQuery()
                        where p.Weight < 10.5M
                        orderby p.Weight ascending
                        select p;
            query.IncludeTotalCount = true;

            Load(query);
            int totalCountWithoutPaging = 0;
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                totalCountWithoutPaging = catalog.Products.Count;
                catalog.Products.Clear();
            });
            EnqueueCallback(delegate
            {
                Load(query.Skip(2).Take(4));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(totalCountWithoutPaging, LoadOperation.TotalEntityCount);
                Assert.AreEqual(4, LoadOperation.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_TotalCount_WithFirstPageAndPredicate()
        {
            Catalog catalog = CreateDomainContext();
            var query = from p in catalog.GetProductsQuery()
                        where p.Weight < 10.5M
                        orderby p.Weight ascending
                        select p;
            query.IncludeTotalCount = true;

            Load(query);
            int totalCountWithoutPaging = 0;
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                totalCountWithoutPaging = catalog.Products.Count;
                catalog.Products.Clear();
            });
            EnqueueCallback(delegate
            {
                Load(query.Take(4));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(totalCountWithoutPaging, LoadOperation.TotalEntityCount);
                Assert.AreEqual(4, LoadOperation.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_TotalCount_WithPredicate()
        {
            Catalog catalog = CreateDomainContext();

            var query = (from p in catalog.GetProductsQuery()
                         where p.Weight < 10.5M
                         orderby p.Weight ascending
                         select p);
            query.IncludeTotalCount = true;
            Load(query);

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(LoadOperation.TotalEntityCount, LoadOperation.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_TotalCount()
        {
            Catalog catalog = CreateDomainContext();

            Load(catalog.GetProductsQuery());

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(LoadOperation.TotalEntityCount, LoadOperation.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_NoTotalCount_WithPagingAndPredicate()
        {
            Catalog catalog = CreateDomainContext();
            var query = (from p in catalog.GetProductsQuery()
                         where p.Weight < 10.5M
                         orderby p.Weight ascending
                         select p)
                        .Skip(2)
                        .Take(4);

            Assert.AreEqual<bool>(false, query.IncludeTotalCount);

            Load(query);
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual<int>(-1, LoadOperation.TotalEntityCount);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_NoTotalCount_WithFirstPageAndPredicate()
        {
            Catalog catalog = CreateDomainContext();
            var query = (from p in catalog.GetProductsQuery()
                         where p.Weight < 10.5M
                         orderby p.Weight ascending
                         select p)
                        .Take(4);

            Assert.AreEqual<bool>(false, query.IncludeTotalCount);

            Load(query);
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual<int>(-1, LoadOperation.TotalEntityCount);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_NoTotalCount_WithPredicate()
        {
            Catalog catalog = CreateDomainContext();

            var query = (from p in catalog.GetProductsQuery()
                         where p.Weight < 10.5M
                         orderby p.Weight ascending
                         select p);

            Assert.AreEqual<bool>(false, query.IncludeTotalCount);

            Load(query);

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual<int>(-1, LoadOperation.TotalEntityCount);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_NoTotalCount()
        {
            Catalog catalog = CreateDomainContext();

            var query = catalog.GetProductsQuery();

            Assert.AreEqual<bool>(false, query.IncludeTotalCount);

            Load(query);

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual<int>(LoadOperation.TotalEntityCount, LoadOperation.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_PagingAndCustomTotalCount()
        {
            Catalog catalog = CreateDomainContext();

            var query = (from p in catalog.GetProductsWithCustomTotalCountQuery()
                         where p.Weight < 10.5M
                         orderby p.Weight ascending
                         select p);

            Load(query.Skip(2).Take(4));

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(6, LoadOperation.TotalEntityCount);
                Assert.AreEqual(4, LoadOperation.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_CustomTotalCount()
        {
            Catalog catalog = CreateDomainContext();
            int totalCountWithoutPaging = 0;
            var query = catalog.GetProductsWithCustomTotalCountQuery();

            Load(query);

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                totalCountWithoutPaging = LoadOperation.TotalEntityCount;
                catalog.Products.Clear();
            });
            EnqueueCallback(delegate
            {
                Load(query.OrderBy(p => p.ProductID).Skip(2).Take(4));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(totalCountWithoutPaging, LoadOperation.TotalEntityCount);
                Assert.AreEqual(4, LoadOperation.Entities.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_WithQuery()
        {
            Catalog catalog = CreateDomainContext();

            var query = from p in catalog.GetPurchaseOrdersQuery()
                        where p.Freight > 50
                        select p;

            Load(query.Take(10));

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                List<PurchaseOrder> orders = catalog.PurchaseOrders.ToList();
                Assert.IsTrue(orders.Count > 0);
                PurchaseOrder order = orders.First();
                Assert.IsTrue(order.PurchaseOrderDetails.Count() > 0);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestLoad_WithParameters()
        {
            Catalog catalog = CreateDomainContext();

            Load(catalog.GetProductsByCategoryQuery(21));

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(catalog.Products.Count == 8);
            });
            EnqueueTestComplete();
        }
    }

    [TestClass]
    public class LTSQueryTests : CrossDomainServiceQueryTests
    {
        public LTSQueryTests()
            : base(TestURIs.LTS_Catalog, ProviderType.LTS)
        {
        }

        protected override Northwind CreateNorthwind()
        {
            return new Northwind(TestURIs.LTS_Northwind);
        }
    }

    [TestClass]
    public class EFQueryTests : CrossDomainServiceQueryTests
    {
        public EFQueryTests()
            : base(TestURIs.EF_Catalog, ProviderType.EF)
        {
        }

        protected override Northwind CreateNorthwind()
        {
            return new Northwind(TestURIs.EF_Northwind);
        }

        /// <summary>
        /// Test queries on projected FK members to make sure they work
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestFKProjectionQuery()
        {
            Catalog catalog = new Catalog(TestURIs.EF_Catalog);
            LoadOperation lo1 = catalog.Load(catalog.GetEmployeesQuery().Where(p => p.ManagerID == 3), false);

            // test both Where and OrderBy expressions involving projected members
            LoadOperation lo2 = catalog.Load(catalog.GetPurchaseOrdersQuery().Where(p => p.EmployeeID > 5).OrderBy(p => p.EmployeeID).Take(1), false);

            EnqueueConditional(delegate
            {
                return !catalog.IsLoading;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(lo1.HasError || lo2.HasError);
                Assert.IsTrue(catalog.Employees.Count > 0, "catalog.Employees.Count should be greater than 0");
                Assert.IsTrue(catalog.PurchaseOrders.Count > 0, "catalog.PurchaseOrders.Count should be greater than 0");
            });
            EnqueueTestComplete();
        }
    }

    [TestClass]
    public class EFDbCtxQueryTests : CrossDomainServiceQueryTests
    {
        public EFDbCtxQueryTests()
            : base(TestURIs.DbCtx_Catalog, ProviderType.EF)
        {
        }

        protected override Northwind CreateNorthwind()
        {
            return new Northwind(TestURIs.DbCtx_Northwind);
        }
    }
    /** TODO: 
     * 
    [TestClass]
    public class EFCoreQueryTests : CrossDomainServiceQueryTests
    {
        public EFCoreQueryTests()
            : base(TestURIs.EFCore_Catalog, ProviderType.EFCore)
        {
        }

        protected override Northwind CreateNorthwind()
        {
            return new Northwind(TestURIs.EFCore_Northwind);
        }
    }
    **/
}
