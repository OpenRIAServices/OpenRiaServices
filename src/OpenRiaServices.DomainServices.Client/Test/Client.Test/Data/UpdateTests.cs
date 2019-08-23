extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using DataTests.Northwind.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices.LTS;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    /// <summary>
    /// End to end update scenario tests. These tests do a one time TestClass level test database
    /// initialization, creating the isolation database that will be used for all the tests. After
    /// all tests have run, and assembly level clean-up method disposes the test database.
    /// Note: we're using the same generated client for both EF and LTS tests, varying only the
    /// provider string that the tests use.
    /// </summary>
#if !SILVERLIGHT
    [TestClass]
#endif
    public abstract class UpdateTests : DomainContextTestBase<Northwind>
    {
        private static TestDatabase testDatabase = new TestDatabase("Northwind");
        private static int custIdSequence = 0;
        private static int categoryIdSequence = 0; // start with 1000 to avoid collisions with common northwind data
        private static int regionIdSequence = 100;

        public UpdateTests(Uri serviceUri, ProviderType providerType)
            : base(serviceUri, providerType)
        {

        }

        public static TestDatabase TestDatabase
        {
            get
            {
                return testDatabase;
            }
        }

        /// <summary>
        /// Returns a customer ID unique in a test run app domain for use
        /// in update tests (to avoid collisions)
        /// </summary>
        protected static string GetUniqueCustID()
        {
            Debug.Assert(custIdSequence < 100000, "Used all the available IDs!");
            string id = custIdSequence++.ToString();
            return id.PadLeft(5, '0');
        }

        /// <summary>
        /// Returns a category ID unique in a test run app domain for use
        /// in resolve tests (to avoid collisions)
        /// </summary>
        protected static string GetUniqueCategoryName()
        {
            // note CategoryName is 15 chars max
            Debug.Assert(categoryIdSequence < 100000, "Used all the available IDs!");
            return string.Format("test {0}", categoryIdSequence++);
        }

        /// <summary>
        /// Returns a Region ID unique in a test run app domain (to avoid collisions)
        /// </summary>
        protected static int GetUniqueRegionID()
        {
            return regionIdSequence++;
        }

        /// <summary>
        /// Returns a Territory ID unique in a test run app domain (to avoid collisions)
        /// </summary>
        protected static string GetUniqueTerritoryID()
        {
            return Guid.NewGuid().ToString().Substring(0, 20);
        }

        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(898909)]
        public void AddNewWithReferenceToDeleted()
        {
            Northwind ctxt = CreateDomainContext();

            Customer newCust = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                string newCustID = GetUniqueCustID();
                newCust = new Customer();
                ctxt.Customers.Add(newCust);
                newCust.CustomerID = newCustID;
                newCust.CompanyName = DateTime.Now.ToString();

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // now add a new order referencing the new customer
                Order o = new Order();
                ctxt.Orders.Add(o);
                o.Customer = newCust;
                o.OrderDate = DateTime.Now;

                // remove the customer
                ctxt.Customers.Remove(newCust);
                Assert.IsNull(o.Customer);

                // verify changeset - association maps should be null for
                // both entities
                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                IEnumerable<ChangeSetEntry> entries = cs.GetChangeSetEntries();
                ChangeSetEntry orderEntry = entries.Single(p => p.Operation == EntityOperationType.Insert);
                Assert.IsNull(orderEntry.Associations);
                Assert.IsNull(orderEntry.OriginalAssociations);
                ChangeSetEntry customerEntry = entries.Single(p => p.Operation == EntityOperationType.Delete);
                Assert.IsNull(customerEntry.Associations);
                Assert.IsNull(customerEntry.OriginalAssociations);

                if (this.ProviderType == Test.ProviderType.LTS)
                {
                    // EF handles setting the order FK to customer to null when
                    // the order is removed server side, but LTS doesn't. Without
                    // this, the delete will fail since we're attempting to add a new entity
                    // that references it!
                    o.CustomerID = null;
                }

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify the customer was deleted
                Load(ctxt.GetCustomersQuery().Where(p => p.CustomerID == newCust.CustomerID));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Assert.IsFalse(ctxt.Customers.Any(p => p.CustomerID == newCust.CustomerID));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(795525)]
        public void UpdateAssociationForWhichAProjectionExists()
        {
            Northwind ctxt = CreateDomainContext();
            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetCategoriesQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                Load(ctxt.GetOrderDetailsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                Product p1 = ctxt.Products.FirstOrDefault(p => p.Category == ctxt.Categories.First());
                Assert.IsNotNull(p1);

                // Update an association (Category) for which a projection exists (CategoryName).
                p1.Category = ctxt.Categories.Skip(1).First();
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                // Verify the submit succeeded.
                AssertSuccess();
            });
            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify an end to end composition update
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void CompositionUpdate()
        {
            Northwind ctxt = CreateDomainContext();
            Region region = null;
            int initialTerritoryCount = 0;
            string updatedDescription = Guid.NewGuid().ToString();
            List<Territory> updated = new List<Territory>();
            List<Territory> deleted = new List<Territory>();
            List<Territory> added = new List<Territory>();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetRegionsQuery().Where(p => p.Territories.Count > 4));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                region = ctxt.Regions.First();
                initialTerritoryCount = region.Territories.Count;
                Assert.IsTrue(initialTerritoryCount > 4);

                // modify a couple of existing entities
                Territory[] territories = region.Territories.ToArray();
                Territory terr = territories[0];
                terr.TerritoryDescription = updatedDescription;
                updated.Add(terr);

                terr = territories[1];
                terr.TerritoryDescription = updatedDescription;
                updated.Add(terr);

                // delete a couple of existing entities
                terr = territories[2];
                region.Territories.Remove(terr);
                deleted.Add(terr);

                terr = territories[3];
                region.Territories.Remove(terr);
                deleted.Add(terr);

                // add a couple new territories
                terr = new Territory
                {
                    TerritoryID = GetUniqueTerritoryID(), TerritoryDescription = "Soccer Mom City"
                };
                added.Add(terr);
                region.Territories.Add(terr);

                terr = new Territory
                {
                    TerritoryID = GetUniqueTerritoryID(), TerritoryDescription = "Urban Sprawl Nightmare"
                };
                added.Add(terr);
                region.Territories.Add(terr);

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.AddedEntities.Count == 2 && cs.ModifiedEntities.Count == 3 && cs.RemovedEntities.Count == 2);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // reload the data and verify submit succeeded
                ctxt = CreateDomainContext();
                Load(ctxt.GetRegionByIdQuery(region.RegionID));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                region = ctxt.Regions.Single();

                // make sure the count reflects the 2 deletes and 2 adds
                Assert.AreEqual(initialTerritoryCount, region.Territories.Count);

                // verify inserts
                foreach (Territory t in added)
                {
                    Territory terr = region.Territories.SingleOrDefault(p => p.TerritoryID == t.TerritoryID);
                    Assert.IsNotNull(terr);
                }

                // verify deletes by making sure each entity we deleted
                // no longer exists in the child collection
                foreach (Territory t in deleted)
                {
                    Assert.IsNull(region.Territories.SingleOrDefault(p => p.TerritoryID == t.TerritoryID));
                }

                // verify updates
                foreach (Territory t in updated)
                {
                    Assert.AreEqual(updatedDescription, t.TerritoryDescription);
                }
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify an end to end composition delete
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void CompositionDelete()
        {
            Northwind submitCtx = CreateDomainContext();
            Northwind ctxt = CreateDomainContext();
            Region region = null;
            Region savedRegion = null;
            List<Territory> deleted = new List<Territory>();
            SubmitOperation submitOperation = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                int newRegionId = GetUniqueRegionID();
                // Setup data
                savedRegion = new Region
                {
                    RegionID = newRegionId,
                    RegionDescription = "Happy Ville"
                };
                savedRegion.Territories.Add(new Territory
                {
                    TerritoryID = GetUniqueTerritoryID(),
                    TerritoryDescription = "Desc1"
                });
                savedRegion.Territories.Add(new Territory
                {
                    TerritoryID = GetUniqueTerritoryID(),
                    TerritoryDescription = "Desc2"
                });

                submitCtx.Regions.Add(savedRegion);
                submitOperation = submitCtx.SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return submitOperation.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.AreEqual(null, submitOperation.Error, "submit should be successfull");

                Load(ctxt.GetRegionsQuery().Where(r => r.RegionID == savedRegion.RegionID));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                region = ctxt.Regions.First();

                // expect the region and all its territories to be deleted
                int territoryCount = region.Territories.Count;
                ctxt.Regions.Remove(region);
                
                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.AreEqual(1 + territoryCount, cs.RemovedEntities.Count);
                Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 0);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // reload the data and verify submit succeeded
                ctxt = CreateDomainContext();
                Load(ctxt.GetRegionByIdQuery(region.RegionID));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Assert.AreEqual(0, ctxt.Regions.Count);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify an end to end composition insert
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void CompositionInsert()
        {
            Northwind ctxt = CreateDomainContext();
            List<Territory> inserted = new List<Territory>();
            int newRegionId = GetUniqueRegionID();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Region region = new Region
                {
                    RegionID = newRegionId, RegionDescription = "Happy Ville"
                };
                region.Territories.Add(new Territory
                {
                    TerritoryID = GetUniqueTerritoryID(), TerritoryDescription = "Desc1"
                });
                region.Territories.Add(new Territory
                {
                    TerritoryID = GetUniqueTerritoryID(), TerritoryDescription = "Desc2"
                });
                region.Territories.Add(new Territory
                {
                    TerritoryID = GetUniqueTerritoryID(), TerritoryDescription = "Desc3"
                });

                // expect the region and all 3 territories to be added
                ctxt.Regions.Add(region);

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.AreEqual(4, cs.AddedEntities.Count);
                Assert.IsTrue(cs.RemovedEntities.Count == 0 && cs.ModifiedEntities.Count == 0);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // reload the data and verify submit succeeded
                ctxt = CreateDomainContext();
                Load(ctxt.GetRegionByIdQuery(newRegionId));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Assert.AreEqual(1, ctxt.Regions.Count);

                Region region = ctxt.Regions.Single();
                Assert.AreEqual(3, region.Territories.Count);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that projection types (non-DAL types) can be queried and updated
        /// when returned from our DAL DomainServices
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ReadAndUpdateProjection()
        {
            Northwind ctxt = CreateDomainContext();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetProductInfosQuery().Where(p => p.CategoryName == "Beverages"));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                ProductInfo prodInfo = ctxt.ProductInfos.First();
                prodInfo.ProductName = "New Product Name";
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Add a new entity and update an existing entity to point to that entity
        /// in the same changeset
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void Bug514013_UpdateAssociation()
        {
            Northwind ctxt = CreateDomainContext();
            string custId = GetUniqueCustID();
            Customer newCust = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetOrdersQuery().Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                newCust = new Customer
                {
                    CustomerID = custId,
                    CompanyName = "New Customer"
                };
                ctxt.Customers.Add(newCust);

                Order order = ctxt.Orders.First();
                Assert.AreNotEqual(order.CustomerID, newCust.CustomerID);
                order.Customer = newCust;

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(cs.AddedEntities.Contains(newCust));
                Assert.IsTrue(cs.ModifiedEntities.Contains(order));

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // now requery to verify that the association was
                // actually updated
                ctxt.Orders.Clear();
                Load(ctxt.GetOrdersQuery().Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Order order = ctxt.Orders.Single();
                Assert.AreEqual(newCust.CustomerID, order.CustomerID);
                Assert.AreSame(newCust, order.Customer);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public virtual void InvokeDMOnNonExistentEntity_Bug518304()
        {
            Northwind ctxt = new Northwind(this.ServiceUri);
            LoadOperation lo = null;
            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                lo = ctxt.Load(ctxt.GetProductsQuery(), false);
            });
            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                // make sure the products table does not contain the id we're planning to use
                Assert.IsFalse(ctxt.Products.Any(p => p.ProductID == 99999));

                // Now attach this entity which does not exist in the table Northwind.Products
                Product newProduct = new Product() { ProductID = 99999, ProductName = "new product", ResolveMethod = "ReturnFalse" };
                ctxt.Products.Attach(newProduct);

                // and execute a domain Method call on it
                newProduct.DiscontinueProduct();

                Assert.AreEqual(1, ctxt.EntityContainer.GetChanges().ModifiedEntities.Count);
                Assert.AreEqual(0, ctxt.EntityContainer.GetChanges().AddedEntities.Count);

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(so.Error);
                Assert.IsInstanceOfType(so.Error, typeof(DomainOperationException));
                DomainOperationException ex = so.Error as DomainOperationException;

                // verify errorCode is 0 for Conflicts but correct status is set
                Assert.AreEqual(0, ex.ErrorCode);
                Assert.AreEqual(OperationErrorStatus.Conflicts, ex.Status);

                // verify conflict info is returned to the client
                Assert.AreEqual(1, so.EntitiesInError.Count());
                EntityConflict conflict = so.EntitiesInError.First().EntityConflict;
                Assert.IsTrue(conflict.IsDeleted);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that concurrency errors are reported properly for conflicts
        /// in association updates
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void AssociationUpdateConflict()
        {
            Northwind ctxt1 = new Northwind(this.ServiceUri);
            Northwind ctxt2 = new Northwind(this.ServiceUri);
            string custId1 = GetUniqueCustID();
            string custId2 = GetUniqueCustID();
            Customer newCust1 = null;
            Customer newCust2 = null;
            string origCustID = null;

            SubmitOperation so1 = null;
            SubmitOperation so2 = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                ctxt1.Load(ctxt1.GetOrdersQuery().Take(1), false);
                ctxt2.Load(ctxt2.GetOrdersQuery().Take(1), false);
            });
            EnqueueConditional(delegate
            {
                return !ctxt1.IsLoading && !ctxt2.IsLoading;
            });
            EnqueueCallback(delegate
            {
                // update the Order.Customer association in the first context
                newCust1 = new Customer
                {
                    CustomerID = custId1,
                    CompanyName = "New Customer 1"
                };
                ctxt1.Customers.Add(newCust1);
                Order order1 = ctxt1.Orders.Single();
                origCustID = order1.CustomerID;
                Assert.AreNotEqual(order1.CustomerID, newCust1.CustomerID);
                order1.Customer = newCust1;

                // make a conflicting update to Order.Customer in the second context
                newCust2 = new Customer
                {
                    CustomerID = custId2,
                    CompanyName = "New Customer 2"
                };
                ctxt2.Customers.Add(newCust2);
                Order order2 = ctxt2.Orders.Single();
                Assert.AreNotEqual(order2.CustomerID, newCust2.CustomerID);
                order2.Customer = newCust2;

                // submit changes in first context
                so1 = ctxt1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so1.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so1);

                // now attempt to submit conflicting changes made
                // in second context
                so2 = ctxt2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so2.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(so2.Error);
                Order order = (Order)so2.EntitiesInError.Single();
                EntityConflict conflict = order.EntityConflict;
                Assert.AreEqual(1, conflict.PropertyNames.Count());

                string conflictMember = conflict.PropertyNames.Single();
                Assert.AreEqual("CustomerID", conflictMember);

                Assert.AreEqual(custId1, conflict.StoreEntity.ExtractState()[conflictMember]);
                Assert.AreEqual(custId2, conflict.CurrentEntity.ExtractState()[conflictMember]);
                Assert.AreEqual(origCustID, conflict.OriginalEntity.ExtractState()[conflictMember]);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void ReadAndUpdateBinary()
        {
            Northwind ctxt = CreateDomainContext();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetCategoriesQuery().Where(p => p.CategoryID == 1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Category category = ctxt.Categories.Single();
                Assert.AreEqual(10746, category.Picture.Length);

                // now modify one of the Pictures
                byte[] updatedPicture = category.Picture.Reverse().ToArray();
                category.Picture = updatedPicture;

                Assert.IsTrue(ctxt.EntityContainer.GetChanges().ModifiedEntities.Contains(category));

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Remove an Order and all its OrderDetails and verify that
        /// the delete is processed properly
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void DeleteGraph()
        {
            // IMPORTANT: This test is run multiple times,
            // once for each derived type.
            // This means that different database rows will be 
            // removed for different invocations

            Northwind ctxt = CreateDomainContext();
            Order order = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                var query = ctxt.GetOrdersQuery().Where(p => p.Customer.CustomerID == "CHOPS").OrderBy(p => p.OrderID).Take(1);
                Load(query);
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                order = ctxt.Orders.Single();
                int numDetails = order.Order_Details.Count;

                Assert.IsTrue(numDetails >= 1, "no order details");  // make sure we have details to delete

                // now do the delete
                foreach (Order_Detail detail in order.Order_Details)
                {
                    order.Order_Details.Remove(detail);
                    ctxt.Order_Details.Remove(detail);
                }
                ctxt.Orders.Remove(order);

                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.AreEqual(numDetails+1, changeSet.RemovedEntities.Count);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that no server side changes are synced back
                // to the client for delete operations. EF sets CustomerID
                // to null on the server
                Assert.IsNotNull(order.CustomerID);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Perform multiple updates on various entities in a connected graph
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void UpdateGraph()
        {
            Northwind ctxt = CreateDomainContext();
            Order order = null;
            Order_Detail newDetail = null;
            int prevDetailCount = 0;
            Dictionary<int, short> prevQuantities = null;
            decimal? prevFreight = 0;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetOrdersQuery().Where(p => p.Customer.CustomerID == "CONSH").OrderBy(p => p.OrderID).Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                order = ctxt.Orders.Single();
                prevDetailCount = order.Order_Details.Count;

                // update the order
                prevFreight = order.Freight;
                order.Freight += 5;

                // update each of the details
                prevQuantities = order.Order_Details.ToDictionary(p => p.ProductID, p => p.Quantity);
                foreach (Order_Detail detail in order.Order_Details)
                {
                    detail.Quantity += 1;
                }

                // also add a new detail
                int newProdId = 1;
                while (order.Order_Details.Any(p => p.ProductID == newProdId))
                {
                    // pick a unique product id
                    newProdId++;
                }
                newDetail = new Order_Detail
                {
                    ProductID = newProdId,
                    Quantity = 1
                };
                order.Order_Details.Add(newDetail);
                ctxt.Order_Details.Add(newDetail);

                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                Assert.AreEqual(prevDetailCount + 1, cs.ModifiedEntities.Count);
                Assert.AreEqual(1, cs.AddedEntities.Count);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // now requery and verify results
                ctxt.EntityContainer.Clear();
                Load(ctxt.GetOrdersQuery().Where(p => p.Customer.CustomerID == "CONSH").OrderBy(p => p.OrderID).Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                order = ctxt.Orders.Single();

                // make sure we got the Add
                Assert.AreEqual(prevDetailCount + 1, order.Order_Details.Count);
                Assert.IsNotNull(order.Order_Details.Single(p => p.ProductID == newDetail.ProductID));

                // make sure all updates were processed
                Assert.AreEqual(prevFreight + 5, order.Freight);
                foreach (Order_Detail detail in order.Order_Details.Where(p => p.ProductID != newDetail.ProductID))
                {
                    Assert.AreEqual(prevQuantities[detail.ProductID] + 1, detail.Quantity);
                }
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Insert graph rooted on customer, with orders and details then
        /// Delete it
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void InsertAndDeleteGraph()
        {
            Northwind ctxt = CreateDomainContext();
            Order order = null;
            Customer cust = null;
            string custId = GetUniqueCustID();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                cust = new Customer
                {
                    CustomerID = custId,
                    CompanyName = "New Customer 1"
                };

                ctxt.Customers.Add(cust);

                order = new Order();
                order.Customer = cust;

                // add some details
                order.Order_Details.Add(new Order_Detail
                {
                    ProductID = 1,
                    Quantity = 1
                });
                order.Order_Details.Add(new Order_Detail
                {
                    ProductID = 2,
                    Quantity = 2
                });
                order.Order_Details.Add(new Order_Detail
                {
                    ProductID = 3,
                    Quantity = 3
                });

                // add the root of the graph - all the child entities
                // will be infer Added
                ctxt.Orders.Add(order);

                // verify that the added entities are in their respective sets
                Assert.IsTrue(ctxt.Orders.Contains(order));
                Assert.IsTrue(ctxt.Customers.Contains(cust));

                // verify that the added entities show up in the EntityCollection
                Assert.AreEqual(3, order.Order_Details.Count());

                // verify the changeset is as expected
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.AreEqual(5, changeSet.AddedEntities.Count);
                Assert.IsTrue(changeSet.AddedEntities.Contains(order));
                foreach (Order_Detail detail in order.Order_Details)
                {
                    Assert.IsTrue(changeSet.AddedEntities.Contains(detail));
                }

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify IDs have been synced back properly
                int orderId = order.OrderID;
                Assert.AreNotEqual(0, orderId);
                foreach (Order_Detail detail in order.Order_Details)
                {
                    Assert.AreEqual(orderId, detail.OrderID);
                }

                Assert.IsTrue(ctxt.EntityContainer.GetChanges().IsEmpty);

                // Now we're going to delete the entire graph
                foreach (Order_Detail detail in order.Order_Details)
                {
                    ctxt.Order_Details.Remove(detail);
                }
                ctxt.Orders.Remove(order);
                ctxt.Customers.Remove(cust);

                Assert.AreEqual(5, ctxt.EntityContainer.GetChanges().RemovedEntities.Count);
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                Assert.IsTrue(ctxt.EntityContainer.GetChanges().IsEmpty);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(858922)]
        public void UpdatePropertyWithNoConcurrencyCheck()
        {
            Northwind ctxt = CreateDomainContext();
            object userState = new object();
            EntityChangeSet changeSet = null;
            int modifiedCategoryID = -1;
            string newCategoryName = "";
            string oldCategoryName = "";

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetCategoriesQuery().OrderBy(p => p.CategoryID).Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // modify an entity.
                Category category = ctxt.Categories.First();
                oldCategoryName = category.CategoryName;
                newCategoryName = category.CategoryName + "*";
                category.CategoryName = newCategoryName;
                modifiedCategoryID = category.CategoryID;

                SubmitChanges(null, userState);
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Category category = ctxt.Categories.First();
                
                // verify that all changes have been accepted
                changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Unmodified, category.EntityState);

                // clear our cache and requery for one of the modified categories
                // and verify that our changes were persisted
                ctxt.EntityContainer.Clear();
                Assert.AreEqual(0, ctxt.Categories.Count);
                Load(ctxt.GetCategoriesQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Category modifiedCategory = ctxt.Categories.Single(p => p.CategoryID == modifiedCategoryID);
                Assert.AreEqual(newCategoryName, modifiedCategory.CategoryName);

                // Change it back.
                modifiedCategory.CategoryName = oldCategoryName;
                SubmitChanges(null, userState);
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void SimpleUpdate()
        {
            Northwind ctxt = CreateDomainContext();
            object userState = new object();
            EntityChangeSet changeSet = null;
            int modifiedProductID = -1;
            string newProductName = "";

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetProductsQuery().OrderBy(p => p.ProductID).Take(5));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // modify a few entities
                Product[] products = ctxt.Products.ToArray();
                products[0].UnitPrice += 5.0M;
                products[1].UnitsInStock -= 1;
                newProductName = products[2].ProductName + "Fo";
                products[2].ProductName = newProductName;
                modifiedProductID = products[2].ProductID;

                SubmitChanges(null, userState);

                // There is a race condition here (until we run tests on a "UI" thread with a proper SynchronizationContext) 
                // so we try to read all properties first before assert
                // and hope for the best
                bool r0 = products[0].IsReadOnly;
                bool r1 = products[1].IsReadOnly;
                bool r2 = products[2].IsReadOnly;
                bool r3 = products[3].IsReadOnly;


                if (SubmitOperation.Error != null &&
                    SubmitOperation.EntitiesInError.Any())
                {
                    foreach (var entity in SubmitOperation.EntitiesInError)
                        Console.WriteLine($"entity {entity.ToString()} has validation errors {string.Join(", ", entity.ValidationErrors.Select(e => $"{e.MemberNames}:{e.ErrorMessage}"))}");
                }
                Assert.AreEqual(null, SubmitOperation.Error, "submit should not have any error");
                Assert.IsFalse(SubmitOperation.IsComplete, "submit should not be complete");
                Assert.IsTrue(ctxt.IsSubmitting, "IsSubmitting should be true");

                // verify that entities are read only after
                // submit is in progress

                Assert.IsTrue(r0, "expected products[0] to be readonly");
                Assert.IsTrue(r1, "expected products[1] to be readonly");
                Assert.IsTrue(r2, "expected products[2] to be readonly");
                Assert.IsFalse(r3, "unmodified product should not be readonly");
                Exception exception = null;
                try
                {
                    products[0].UnitPrice -= 1.0M;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                Assert.AreNotEqual(null, exception, "Trying to change a property on a readonly entity should throw exception");
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Product[] products = ctxt.Products.ToArray();
                Assert.IsFalse(products[0].IsReadOnly, "entity should not be readonly after submit is complete");
                Assert.IsFalse(products[1].IsReadOnly, "entity should not be readonly after submit is complete");
                Assert.IsFalse(products[2].IsReadOnly, "entity should not be readonly after submit is complete");
                Assert.IsFalse(products[3].IsReadOnly, "entity should not be readonly after submit is complete");

                // verify that all changes have been accepted
                changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Unmodified, products[0].EntityState);
                Assert.AreEqual(EntityState.Unmodified, products[1].EntityState);
                Assert.AreEqual(EntityState.Unmodified, products[2].EntityState);

                // clear our cache and requery for one of the modified products
                // and verify that our changes were persisted
                ctxt.EntityContainer.Clear();
                Assert.AreEqual(0, ctxt.Products.Count);
                Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Product modifiedProduct = ctxt.Products.Single(p => p.ProductID == modifiedProductID);
                Assert.AreEqual(newProductName, modifiedProduct.ProductName);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void SimpleUpdateAndInvokeDM()
        {
            Northwind ctxt = CreateDomainContext();
            object userState = new object();
            EntityChangeSet changeSet = null;
            int modifiedProductID = -1;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                // Skip first 5 rows.
                Load(ctxt.GetProductsQuery().Where(p => !p.Discontinued).OrderBy(p => p.ProductID).Skip(5));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // modify an entity and invoke a DM on it
                Product[] products = ctxt.Products.ToArray();
                products[0].UnitPrice += 5.0M;
                products[0].DiscontinueProduct();
                modifiedProductID = products[0].ProductID;

                SubmitChanges(null, userState);
                Assert.IsTrue(products[0].IsReadOnly);

                changeSet = SubmitOperation.ChangeSet;
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                // verify the submitted event
                AssertSuccess();
                Assert.AreSame(userState, SubmitOperation.UserState);
                Assert.AreSame(changeSet, SubmitOperation.ChangeSet);

                Product[] products = ctxt.Products.ToArray();
                Assert.IsFalse(products[0].IsReadOnly);

                // verify that all changes have been accepted
                changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Unmodified, products[0].EntityState);

                // clear our cache and requery for one of the modified products
                // and verify that our changes were persisted
                ctxt.EntityContainer.Clear();
                Assert.AreEqual(0, ctxt.Products.Count);
                Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Product modifiedProduct = ctxt.Products.Single(p => p.ProductID == modifiedProductID);
                Assert.IsTrue(modifiedProduct.Discontinued);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void SimpleInsert()
        {
            Northwind ctxt = CreateDomainContext();
            Product newProduct = null;
            string identifier = Guid.NewGuid().ToString();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                newProduct = new Product
                {
                    ProductName = identifier
                };
                ctxt.Products.Add(newProduct);
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Unmodified, newProduct.EntityState);

                // verify that the ProductID
                // has been synced back
                Assert.AreNotEqual(0, newProduct.ProductID);

                // query in a new context to ensure the product was inserted
                ctxt = CreateDomainContext();
                Load(ctxt.GetProductsQuery().Where(p => p.ProductName == identifier));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                Product requeriedProduct = ctxt.Products.SingleOrDefault();
                Assert.IsNotNull(requeriedProduct);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public virtual void InsertAndInvokeDM()
        {
            Northwind ctxt = CreateDomainContext();
            Product newProduct = null;
            string identifier = Guid.NewGuid().ToString();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                newProduct = new Product
                {
                    ProductName = identifier
                };
                ctxt.Products.Add(newProduct);
                newProduct.DiscontinueProduct();
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Unmodified, newProduct.EntityState);

                // verify that the ProductID
                // has been synced back
                Assert.AreNotEqual(0, newProduct.ProductID);

                // query in a new context to ensure the product was inserted
                ctxt = CreateDomainContext();
                Load(ctxt.GetProductsQuery().Where(p => p.ProductName == identifier));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Product requeriedProduct = ctxt.Products.SingleOrDefault();
                Assert.IsNotNull(requeriedProduct);
                Assert.IsTrue(requeriedProduct.Discontinued);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void SimpleDelete()
        {
            Northwind ctxt = CreateDomainContext();
            Product newProduct = null;
            Product deleteProduct = null;
            int prevCount = 0;
            string identifier = Guid.NewGuid().ToString();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                newProduct = new Product
                {
                    ProductName = identifier
                };

                // add a new product so we can delete it below
                ctxt.Products.Add(newProduct);
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                // create a new context and load all products
                AssertSuccess();
                ctxt = CreateDomainContext();
                Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that the new product is there
                deleteProduct = ctxt.Products.SingleOrDefault(p => p.ProductName == identifier);
                Assert.IsNotNull(deleteProduct);

                // delete it
                prevCount = ctxt.Products.Count;
                ctxt.Products.Remove(deleteProduct);
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Detached, deleteProduct.EntityState);

                // create a new context and load all products
                ctxt = CreateDomainContext();
                Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that the product has been removed
                Assert.AreEqual(prevCount - 1, ctxt.Products.Count);
                Assert.IsNull(ctxt.Products.SingleOrDefault(p => p.ProductName == identifier));
            });

            EnqueueTestComplete();
        }

        [WorkItem(766785)]
        [TestMethod]
        [Asynchronous]
        public void EditAndDeleteSingleEntity()
        {
            Northwind ctxt = CreateDomainContext();
            Product newProduct = null;
            Product deleteProduct = null;
            int prevCount = 0;
            string identifier = Guid.NewGuid().ToString();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                newProduct = new Product
                {
                    ProductName = identifier
                };

                // add a new product so we can delete it below
                ctxt.Products.Add(newProduct);
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                // create a new context and load all products
                AssertSuccess();
                ctxt = CreateDomainContext();
                Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that the new product is there
                deleteProduct = ctxt.Products.SingleOrDefault(p => p.ProductName == identifier);
                Assert.IsNotNull(deleteProduct);

                // edit it
                deleteProduct.ProductName += "*";

                // and now delete it
                prevCount = ctxt.Products.Count;
                ctxt.Products.Remove(deleteProduct);
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Detached, deleteProduct.EntityState);

                // create a new context and load all products
                ctxt = CreateDomainContext();
                Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that the product has been removed
                Assert.AreEqual(prevCount - 1, ctxt.Products.Count);
                Assert.IsNull(ctxt.Products.SingleOrDefault(p => p.ProductName == identifier));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void DeleteFromEntitySetAndThenFromEntityCollection()
        {
            Northwind ctxt = CreateDomainContext();
            Order_Detail detailToDelete = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                var query = (from o in ctxt.GetOrdersQuery() where o.Order_Details.Count > 0 select o).Take(2);
                Load(query);
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                var order = ctxt.Orders.First();
                detailToDelete = order.Order_Details.First();
                ctxt.Order_Details.Remove(detailToDelete);
                order.Order_Details.Remove(detailToDelete);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Detached, detailToDelete.EntityState);

                Assert.IsFalse(ctxt.Order_Details.Any(od => od.OrderID == detailToDelete.OrderID && od.ProductID == detailToDelete.ProductID));

                var query = ctxt.GetOrderDetailsQuery().Where(od => od.OrderID == detailToDelete.OrderID && od.ProductID == detailToDelete.ProductID);
                Load(query);
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                Assert.IsFalse(ctxt.Order_Details.Any(od => od.OrderID == detailToDelete.OrderID && od.ProductID == detailToDelete.ProductID));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void DeleteFromEntityCollectionAndThenFromEntitySet()
        {
            Northwind ctxt = CreateDomainContext();
            Order_Detail detailToDelete = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                var query = (from o in ctxt.GetOrdersQuery() where o.Order_Details.Count > 0 select o).Take(2);
                Load(query);
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                var order = ctxt.Orders.First();
                detailToDelete = order.Order_Details.First();
                order.Order_Details.Remove(detailToDelete);
                ctxt.Order_Details.Remove(detailToDelete);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Detached, detailToDelete.EntityState);

                Assert.IsFalse(ctxt.Order_Details.Any(od => od.OrderID == detailToDelete.OrderID && od.ProductID == detailToDelete.ProductID));

                var query = ctxt.GetOrderDetailsQuery().Where(od => od.OrderID == detailToDelete.OrderID && od.ProductID == detailToDelete.ProductID);
                Load(query);
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Assert.IsFalse(ctxt.Order_Details.Any(od => od.OrderID == detailToDelete.OrderID && od.ProductID == detailToDelete.ProductID));
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public virtual void SimpleUpdateConflict()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);

            LoadOperation nw1load = null, nw2load = null;
            string nw1address = DateTime.Now.Ticks.ToString() + " - nw1";
            string nw2address = DateTime.Now.Ticks.ToString() + " - nw2";
            string nw2addressOriginal = null;
            string nw2address2 = DateTime.Now.Ticks.ToString() + " - nw2.2";

            SubmitOperation so1 = null;
            SubmitOperation so2 = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1load = nw1.Load(nw1.GetCustomersQuery(), false);
                nw2load = nw2.Load(nw2.GetCustomersQuery(), false);
            });
            EnqueueConditional(() => nw1load.IsComplete && nw2load.IsComplete);
            EnqueueCallback(delegate
            {
                if (nw1load.Error != null)
                {
                    Assert.Fail("nw1 error: " + nw1load.Error.ToString());
                }
                if (nw2load.Error != null)
                {
                    Assert.Fail("nw2 error: " + nw2load.Error.ToString());
                }
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(nw1.Customers.Count > 0);
                Assert.IsTrue(nw2.Customers.Count > 0);
                Customer[] customers = nw1.Customers.ToArray();
                customers[0].Address = nw1address;
                so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so1.IsComplete);
            EnqueueCallback(delegate
            {
                Customer[] customers = nw2.Customers.ToArray();
                nw2addressOriginal = customers[0].Address;
                customers[0].Address = nw2address;
                customers[1].Address = nw2address2;
                so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so2.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Conflicts, so2.Error.Message);
                Entity[] entitiesInConflict = so2.EntitiesInError.ToArray();
                Assert.AreEqual(2, so2.ChangeSet.ModifiedEntities.Count);
                Customer customerInConflict = (Customer)entitiesInConflict[0];
                EntityConflict conflict = customerInConflict.EntityConflict;
                Assert.AreEqual(nw1address, conflict.StoreEntity.ExtractState()["Address"]);
                Assert.AreEqual(nw2address, conflict.CurrentEntity.ExtractState()["Address"]);
                Assert.AreEqual(nw2addressOriginal, conflict.OriginalEntity.ExtractState()["Address"]);
                Assert.AreEqual(nw2address, customerInConflict.Address);
                Assert.AreEqual(nw2address2, ((Customer)so2.ChangeSet.ModifiedEntities[1]).Address);
                Assert.AreEqual(1, entitiesInConflict.Length);
                Assert.AreEqual(1, conflict.PropertyNames.Count());

                // resolve all the conflicts and resubmit
                foreach(Entity entityInConflict in entitiesInConflict)
                {
                    conflict = entityInConflict.EntityConflict;
                    var currentState = conflict.CurrentEntity.ExtractState();
                    conflict.Resolve();
                    TestHelperMethods.VerifyEntityState(currentState, conflict.CurrentEntity.ExtractState());
                    Assert.IsNull(entityInConflict.EntityConflict);
                }

                so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so2.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so2.HasError);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void SimpleUpdateDeleteConflict()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);

            short oldQuantity = 0;
            short newQuantity = 0;
            LoadOperation nw1load = null, nw2load = null;
            SubmitOperation so1 = null, so2 = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1load = nw1.Load(nw1.GetOrderDetailsQuery().Take(5), false);
                nw2load = nw2.Load(nw2.GetOrderDetailsQuery().Take(5), false);
            });
            EnqueueConditional(() => nw1load.IsComplete && nw2load.IsComplete);
            EnqueueCallback(delegate
            {
                if (nw1load.Error != null)
                {
                    Assert.Fail("nw1 error: " + nw1load.Error.ToString());
                }
                if (nw2load.Error != null)
                {
                    Assert.Fail("nw2 error: " + nw2load.Error.ToString());
                }
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(nw1.Order_Details.Count > 0);
                Assert.IsTrue(nw2.Order_Details.Count > 0);
                Order_Detail detail1 = nw1.Order_Details.First();
                Order_Detail detail2 = nw2.Order_Details.First();
                Assert.AreEqual(detail1.OrderID, detail2.OrderID, "Unexpected OrderID.");
                Assert.AreEqual(detail1.ProductID, detail2.ProductID, "Unexpected ProductID.");
                oldQuantity = detail1.Quantity;
                newQuantity = ++detail1.Quantity;
                so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so1.IsComplete);
            EnqueueCallback(delegate
            {
                Order_Detail detail2 = nw2.Order_Details.First();
                nw2.Order_Details.Remove(detail2);
                so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so2.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Conflicts, so2.Error.Message);
                Entity[] entitiesInConflict = so2.EntitiesInError.ToArray();
                Assert.AreEqual(1, so2.ChangeSet.RemovedEntities.Count, "Unexpected amount of removed entities.");
                Assert.AreEqual(1, entitiesInConflict.Length, "Unexpected amount of entities in conflict.");
                EntityConflict conflict = entitiesInConflict[0].EntityConflict;
                Assert.AreEqual(1, conflict.PropertyNames.Count(), "Unexpected amount of property names in conflict.");
                string conflictMember = conflict.PropertyNames.Single();
                Assert.AreEqual("Quantity", conflictMember, "Unexpected member in conflict.");
                Assert.AreEqual(oldQuantity, conflict.CurrentEntity.ExtractState()[conflictMember], "Unexpected original quantity.");
                Assert.AreEqual(newQuantity, conflict.StoreEntity.ExtractState()[conflictMember], "Unexpected new quantity.");
            });
            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that an edit followed by a delete still succeeds (doesn't result
        /// in a concurrency failure), by ensuring that the original entity is passed
        /// into the server side delete method.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void DeleteUsesOriginalEntity()
        {
            Northwind nw = new Northwind(this.ServiceUri);
            LoadOperation lo = null;
            SubmitOperation so = null;
            Order_Detail detail = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                lo = nw.Load(nw.GetOrderDetailsQuery().OrderBy(o => o.OrderID).Take(10), false);
            });
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(nw.Order_Details.Count > 0);

                detail = nw.Order_Details.First();

                // modify the detail before the delete to verify
                // that delete trumps edits and that the server
                // delete succeeds using the original values for
                // the delete.
                detail.Discount += 1;
                Assert.IsNotNull(detail.OriginalValues);

                nw.Order_Details.Remove(detail);

                so = nw.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
                Assert.AreEqual(EntityState.Detached, detail.EntityState);
            });
           
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void SimpleDeleteUpdateConflict()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);

            LoadOperation nw1load = null, nw2load = null;
            SubmitOperation so1 = null, so2 = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1load = nw1.Load(nw1.GetOrderDetailsQuery().Take(5), false);
                nw2load = nw2.Load(nw2.GetOrderDetailsQuery().Take(5), false);
            });
            EnqueueConditional(() => nw1load.IsComplete && nw2load.IsComplete);
            EnqueueCallback(delegate
            {
                if (nw1load.Error != null)
                {
                    Assert.Fail("nw1 error: " + nw1load.Error.ToString());
                }
                if (nw2load.Error != null)
                {
                    Assert.Fail("nw2 error: " + nw2load.Error.ToString());
                }
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(nw1.Order_Details.Count > 0);
                Assert.IsTrue(nw2.Order_Details.Count > 0);
                Order_Detail detail1 = nw1.Order_Details.First();
                Order_Detail detail2 = nw2.Order_Details.First();
                Assert.AreEqual(detail1.OrderID, detail2.OrderID);
                Assert.AreEqual(detail1.ProductID, detail2.ProductID);
                nw1.Order_Details.Remove(detail1);
                so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so1.IsComplete);
            EnqueueCallback(delegate
            {
                Order_Detail detail2 = nw2.Order_Details.First();
                detail2.Quantity += 1;
                so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so2.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Conflicts, so2.Error.Message);
                Entity[] entitiesInConflict = so2.EntitiesInError.ToArray();
                Assert.AreEqual(1, so2.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(1, entitiesInConflict.Length);
                Assert.IsTrue(entitiesInConflict[0].EntityConflict.IsDeleted);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public virtual void SimpleUpdateConflictWithResolve()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal origUnitPrice = 0, nw1NewUnitPrice = 0, nw2NewUnitPrice = 0;
            short newReorderLevel = 0;

            LoadOperation nw1load = null, nw2load = null;
            SubmitOperation so1 = null, so2 = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1load = nw1.Load(nw1.GetProductsQuery(), false);
                nw2load = nw2.Load(nw2.GetProductsQuery(), false);
            });
            EnqueueConditional(() => nw1load.IsComplete && nw2load.IsComplete);
            EnqueueCallback(delegate
            {
                if (nw1load.Error != null)
                {
                    Assert.Fail("nw1 error: " + nw1load.Error.ToString());
                }
                if (nw2load.Error != null)
                {
                    Assert.Fail("nw2 error: " + nw2load.Error.ToString());
                }
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(nw1.Products.Count > 0);
                Assert.IsTrue(nw2.Products.Count > 0);
                Product[] products = nw1.Products.ToArray();
                origUnitPrice = products[0].UnitPrice ?? 0;
                nw1NewUnitPrice = origUnitPrice + 1;
                products[0].UnitPrice = nw1NewUnitPrice;
                so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so1.IsComplete);
            EnqueueCallback(delegate
            {
                Product[] products = nw2.Products.ToArray();
                Assert.AreEqual(origUnitPrice, products[0].UnitPrice);
                nw2NewUnitPrice = origUnitPrice + 2;
                products[0].UnitPrice = nw2NewUnitPrice;
                newReorderLevel = products[1].ReorderLevel ?? 0;
                products[1].ReorderLevel = ++newReorderLevel;
                so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so2.IsComplete);
            EnqueueCallback(delegate
            {
                // verify that resolve method implementation successfully got rid of the conflicts
                Product[] products = nw2.Products.ToArray();
                Assert.AreEqual(0, so2.EntitiesInError.Count());
                Assert.IsNull(products[0].EntityConflict);
                Assert.AreEqual(newReorderLevel, products[1].ReorderLevel);
                Assert.AreEqual(nw2NewUnitPrice, products[0].UnitPrice);

                // clear our cache and requery through nw1 context to
                // verify that our changes were persisted
                nw1.EntityContainer.Clear();
                nw1load = nw1.Load(nw1.GetProductsQuery(), false);
            });
            EnqueueConditional(() => nw1load.IsComplete);
            EnqueueCallback(delegate
            {
                // verify that updates from nw2 were succesfully saved
                Product[] products = nw1.Products.ToArray();
                Assert.AreEqual(newReorderLevel, products[1].ReorderLevel);
                Assert.AreEqual(nw2NewUnitPrice, products[0].UnitPrice);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void ServerValidationErrorPreventsSubmit()
        {
            Northwind dc = CreateDomainContext();
            int originalCount = 0;

            var prod = new Product
            {
                ProductName = Guid.NewGuid().ToString(),
                Discontinued = true
            };

            EnqueueConditional(() => TestDatabase.IsInitialized);
            EnqueueCallback(delegate
            {
                Load(dc.GetProductsQuery());
            });
            EnqueueConditional(() => IsLoadComplete);
            EnqueueCallback(delegate
            {
                originalCount = dc.Products.Count;
                dc.Products.Add(prod);
                prod.DiscontinueProduct();
                SubmitChanges();
            });
            EnqueueConditional(() => IsSubmitComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(SubmitOperation.Error);
                Assert.AreEqual(1, SubmitOperation.EntitiesInError.Count());
                Assert.AreEqual(prod, SubmitOperation.EntitiesInError.First());

                dc.EntityContainer.Clear();
                Load(dc.GetProductsQuery());
            });
            EnqueueConditional(() => IsLoadComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(originalCount, dc.Products.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void UpdateEntityAndDeleteAssociatedEntity()
        {
            Northwind ctxt = CreateDomainContext();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                // Get one order.
                Load(ctxt.GetOrdersQuery().OrderBy(o => o.OrderID).Where(o => o.Order_Details.Count > 0).Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                Order order = ctxt.Orders.First();

                // Update the order.
                order.ShipAddress += "*";

                // Delete a related entity.
                var od = order.Order_Details.First();
                ctxt.Order_Details.Remove(od);
                order.Order_Details.Remove(od);

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
            });

            EnqueueTestComplete();
        }

        #region Single entity changeset, different resolve method return values
        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns true but does not remove any conflicts")]
        public virtual void ResolveTest_ReturnTrueNoResolve()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal origUnitPrice0 = 0, nw1NewUnitPrice0 = 0, nw2NewUnitPrice0 = 0;
            short origReorderLevel0 = 0, nw1NewReorderLevel0 = 0;
            SubmitOperation so1 = null, so2 = null;

            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count > 0);
                    Assert.IsTrue(nw2.Products.Count > 0);
                    Product[] products = nw1.Products.ToArray();
                    origUnitPrice0 = products[0].UnitPrice ?? 0;
                    nw1NewUnitPrice0 = origUnitPrice0 + 1;
                    origReorderLevel0 = products[0].ReorderLevel ?? 0;
                    nw1NewReorderLevel0 = (short)(origReorderLevel0 + 1);

                    products[0].ReorderLevel = nw1NewReorderLevel0;
                    products[0].UnitPrice = nw1NewUnitPrice0;
                    so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    Assert.AreEqual(origUnitPrice0, products[0].UnitPrice);
                    nw2NewUnitPrice0 = origUnitPrice0 + 2;
                    products[0].UnitPrice = nw2NewUnitPrice0;
                    products[0].ResolveMethod = "ReturnTrueNoResolve";
                    so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                // verify that resolve method implementation does not get rid of the conflicts
                Assert.IsNotNull(so2.Error);
                Product[] products = nw2.Products.ToArray();
                Assert.AreEqual(1, so2.EntitiesInError.Count());
                Assert.IsNotNull(products[0].EntityConflict);

                // Verify return true without actually resolving anything does not sync client values since ResolveConflicts will return false
                Assert.AreEqual(nw2NewUnitPrice0, products[0].UnitPrice);
                Assert.AreEqual(origReorderLevel0, products[0].ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that updates from nw2 are not persisted since resubmit will not be called
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw1NewUnitPrice0, products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel0, products[0].ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns false and does not remove any conflicts")]
        public virtual void ResolveTest_ReturnFalse()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal origUnitPrice0 = 0, nw1NewUnitPrice0 = 0, nw2NewUnitPrice0 = 0;
            short origReorderLevel0 = 0, nw1NewReorderLevel0 = 0;
            SubmitOperation so1 = null, so2 = null;

            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count > 0);
                    Assert.IsTrue(nw2.Products.Count > 0);
                    Product[] products = nw1.Products.ToArray();
                    origUnitPrice0 = products[0].UnitPrice ?? 0;
                    nw1NewUnitPrice0 = origUnitPrice0 + 1;
                    origReorderLevel0 = products[0].ReorderLevel ?? 0;
                    nw1NewReorderLevel0 = (short)(origReorderLevel0 + 1);

                    products[0].ReorderLevel = nw1NewReorderLevel0;
                    products[0].UnitPrice = nw1NewUnitPrice0;
                    so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    Assert.AreEqual(origUnitPrice0, products[0].UnitPrice);
                    nw2NewUnitPrice0 = origUnitPrice0 + 2;
                    products[0].UnitPrice = nw2NewUnitPrice0;
                    products[0].ResolveMethod = "ReturnFalse";
                    so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                // verify that resolve method implementation does not get rid of the conflicts
                Assert.IsNotNull(so2.Error);
                Product[] products = nw2.Products.ToArray();
                Assert.AreEqual(1, so2.EntitiesInError.Count());
                Assert.IsNotNull(products[0].EntityConflict);

                // Verify return true without actually resolving anything does not sync client values
                Assert.AreEqual(nw2NewUnitPrice0, products[0].UnitPrice);
                Assert.AreEqual(origReorderLevel0, products[0].ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that updates from nw2 are not persisted
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw1NewUnitPrice0, products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel0, products[0].ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns false but conflicts are resolved")]
        public virtual void ResolveTest_ReturnFalseWithResolve()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal origUnitPrice0 = 0, nw1NewUnitPrice0 = 0, nw2NewUnitPrice0 = 0;
            short origReorderLevel0 = 0, nw1NewReorderLevel0 = 0;
            SubmitOperation so1 = null, so2 = null;

            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count > 0);
                    Assert.IsTrue(nw2.Products.Count > 0);
                    Product[] products = nw1.Products.ToArray();
                    origUnitPrice0 = products[0].UnitPrice ?? 0;
                    nw1NewUnitPrice0 = origUnitPrice0 + 1;
                    origReorderLevel0 = products[0].ReorderLevel ?? 0;
                    nw1NewReorderLevel0 = (short)(origReorderLevel0 + 1);

                    products[0].ReorderLevel = nw1NewReorderLevel0;
                    products[0].UnitPrice = nw1NewUnitPrice0;
                    so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    Assert.AreEqual(origUnitPrice0, products[0].UnitPrice);
                    nw2NewUnitPrice0 = origUnitPrice0 + 2;
                    products[0].UnitPrice = nw2NewUnitPrice0;
                    products[0].ResolveMethod = "ReturnFalseWithResolve";
                    so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                // verify that resolve method implementation does not get rid of the conflicts
                Assert.IsNotNull(so2.Error);
                Assert.AreEqual(1, so2.EntitiesInError.Count());
                Product[] products = nw2.Products.ToArray();
                Assert.IsNotNull(products[0].EntityConflict);

                // Verify return false does not sync client values even if it resolves the conflicts
                Assert.AreEqual(nw2NewUnitPrice0, products[0].UnitPrice);
                Assert.AreEqual(origReorderLevel0, products[0].ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that updates from nw2 are not persisted since changes should not be resubmitted on false return
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw1NewUnitPrice0, products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel0, products[0].ReorderLevel);
                });

            EnqueueTestComplete();
        }
        #endregion

        #region Single entity changeset, conflict resolved using resolve utility
        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns true and conflicts are resolved using MergeIntoCurrent policy")]
        public virtual void ResolveTest_MergeIntoCurrent()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal origUnitPrice0 = 0, nw1NewUnitPrice0 = 0, nw2NewUnitPrice0 = 0;
            short origReorderLevel0 = 0, nw1NewReorderLevel0 = 0;
            SubmitOperation so = null;

            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count > 0);
                    Assert.IsTrue(nw2.Products.Count > 0);
                    Product[] products = nw1.Products.ToArray();
                    origUnitPrice0 = products[0].UnitPrice ?? 0;
                    nw1NewUnitPrice0 = origUnitPrice0 + 1;
                    origReorderLevel0 = products[0].ReorderLevel ?? 0;
                    nw1NewReorderLevel0 = (short)(origReorderLevel0 + 1);

                    products[0].ReorderLevel = nw1NewReorderLevel0;
                    products[0].UnitPrice = nw1NewUnitPrice0;
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    Assert.AreEqual(origUnitPrice0, products[0].UnitPrice);
                    nw2NewUnitPrice0 = origUnitPrice0 + 2;
                    products[0].UnitPrice = nw2NewUnitPrice0;
                    products[0].ResolveMethod = "MergeIntoCurrent";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify that resolve method implementation successfully got rid of the conflicts
                Product[] products = nw2.Products.ToArray();
                Assert.IsNull(products[0].EntityConflict);

                // Verify MergeIntoCurrent option resolves by overwriting properties with client changes but updating original values
                // for ones without client changes
                Assert.AreEqual(nw2NewUnitPrice0, products[0].UnitPrice);
                Assert.AreEqual(nw1NewReorderLevel0, products[0].ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that updates from nw2 were succesfully saved
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw2NewUnitPrice0, products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel0, products[0].ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns true and conflicts are resolved using MergeIntoCurrent policy")]
        public virtual void ResolveTest_MergeIntoCurrent_Association()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            int catId1 = 0, catId2 = 0, origCatID = 0;
            string catName1 = GetUniqueCategoryName();
            string catName2 = GetUniqueCategoryName();
            Category newCat1 = null, newCat2 = null;
            short origReorderLevel = 0, nw1NewReorderLevel = 0;

            SubmitOperation so1 = null, so2 = null;
            LoadOperation nw1Load = null, nw2Load = null;

            // the following sets up the new categories for association update use
            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1Load = nw1.Load(nw1.GetCategoriesQuery(), false);
            });
            EnqueueConditional(() => nw1Load.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(nw1Load);

                // create two new categories and insert into the database
                newCat1 = new Category { CategoryName = catName1, Description = catName1 };
                newCat2 = new Category { CategoryName = catName2, Description = catName2 };
                nw1.Categories.Add(newCat1);
                nw1.Categories.Add(newCat2);
                so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so1.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so1);

                // relaod all categories from database
                nw1.Categories.Clear();
                nw1Load = nw1.Load(nw1.GetCategoriesQuery(), false);
                nw2Load = nw2.Load(nw2.GetCategoriesQuery(), false);
            });
            EnqueueConditional(() => nw1Load.IsComplete && nw2Load.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(nw1Load);
                TestHelperMethods.AssertOperationSuccess(nw2Load);

                // get the real autosynced category id (since it's generated by the store during insert)
                newCat1 = nw1.Categories.Single(c => c.CategoryName == catName1);
                catId1 = newCat1.CategoryID;
                newCat2 = nw2.Categories.Single(c => c.CategoryName == catName2);
                catId2 = newCat2.CategoryID;
            });

            // association conflict submission
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    // update association
                    Product product1 = nw1.Products.First();
                    origCatID = product1.CategoryID ?? 0;
                    Assert.AreNotEqual(product1.CategoryID, newCat1.CategoryID);
                    product1.Category = newCat1;

                    // update property
                    origReorderLevel = nw1.Products.First().ReorderLevel ?? 0;
                    nw1NewReorderLevel = (short)(origReorderLevel + 1);
                    nw1.Products.First().ReorderLevel = nw1NewReorderLevel;

                    so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    // conflicting update on association
                    Product product2 = nw2.Products.First();
                    Assert.AreNotEqual(product2.CategoryID, newCat2.CategoryID);
                    Assert.AreEqual(origReorderLevel, product2.ReorderLevel);
                    product2.Category = newCat2;
                    product2.ResolveMethod = "MergeIntoCurrent";
                    so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so2);

                // verify that resolve method implementation successfully got rid of the conflicts
                Assert.IsNull(nw2.Products.First().EntityConflict);
                Assert.AreEqual(catId2, nw2.Products.First().CategoryID);
                Assert.AreEqual(nw1NewReorderLevel, nw2.Products.First().ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    Assert.AreEqual(catId2, nw2.Products.First().CategoryID);
                    Assert.AreEqual(nw1NewReorderLevel, nw1.Products.First().ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns true and conflicts are resolved using KeepCurrent policy")]
        public virtual void ResolveTest_KeepCurrent()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal origUnitPrice0 = 0, nw1NewUnitPrice0 = 0, nw2NewUnitPrice0 = 0;
            short origReorderLevel0 = 0, nw1NewReorderLevel0 = 0;
            SubmitOperation so = null;

            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count > 0);
                    Assert.IsTrue(nw2.Products.Count > 0);
                    Product product = nw1.Products.First();
                    origUnitPrice0 = product.UnitPrice ?? 0;
                    nw1NewUnitPrice0 = origUnitPrice0 + 1;
                    origReorderLevel0 = product.ReorderLevel ?? 0;
                    nw1NewReorderLevel0 = (short)(origReorderLevel0 + 1);

                    product.ReorderLevel = nw1NewReorderLevel0;
                    product.UnitPrice = nw1NewUnitPrice0;
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product product = nw2.Products.First();
                    Assert.AreEqual(origUnitPrice0, product.UnitPrice);
                    nw2NewUnitPrice0 = origUnitPrice0 + 2;
                    product.UnitPrice = nw2NewUnitPrice0;
                    product.ResolveMethod = "KeepCurrent";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify that resolve method implementation successfully got rid of the conflicts
                Product product = nw2.Products.First();
                Assert.IsNull(product.EntityConflict);

                // Verify KeepCurrent option resolves by overwriting properties with client changes, hence
                // the original values on nw2 are perserved
                Assert.AreEqual(nw2NewUnitPrice0, product.UnitPrice);
                Assert.AreEqual(origReorderLevel0, product.ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that client values from nw2 were succesfully saved
                    Product product = nw1.Products.First();
                    Assert.AreEqual(nw2NewUnitPrice0, product.UnitPrice);
                    Assert.AreEqual(origReorderLevel0, product.ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns true and conflicts are resolved using KeepCurrent policy")]
        public virtual void ResolveTest_KeepCurrent_Association()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            int catId1 = 0, catId2 = 0, origCatID = 0;
            string catName1 = GetUniqueCategoryName();
            string catName2 = GetUniqueCategoryName();
            Category newCat1 = null, newCat2 = null;
            decimal origUnitPrice = 0, nw1NewUnitPrice = 0, nw2NewUnitPrice = 0;
            short origReorderLevel = 0, nw1NewReorderLevel = 0;

            SubmitOperation so1 = null, so2 = null;
            LoadOperation nw1Load = null, nw2Load = null;

            // the following sets up the new categories for association update use
            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1Load = nw1.Load(nw1.GetCategoriesQuery(), false);
            });
            EnqueueConditional(() => nw1Load.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(nw1Load);

                // create two new categories and insert into the database
                newCat1 = new Category { CategoryName = catName1, Description = catName1 };
                newCat2 = new Category { CategoryName = catName2, Description = catName2 };
                nw1.Categories.Add(newCat1);
                nw1.Categories.Add(newCat2);
                so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so1.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so1);

                // relaod all categories from database
                nw1.Categories.Clear();
                nw1Load = nw1.Load(nw1.GetCategoriesQuery(), false);
                nw2Load = nw2.Load(nw2.GetCategoriesQuery(), false);
            });
            EnqueueConditional(() => nw1Load.IsComplete && nw2Load.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(nw1Load);
                TestHelperMethods.AssertOperationSuccess(nw2Load);

                // get the real autosynced category id (since it's generated by the store during insert)
                newCat1 = nw1.Categories.Single(c => c.CategoryName == catName1);
                catId1 = newCat1.CategoryID;
                newCat2 = nw2.Categories.Single(c => c.CategoryName == catName2);
                catId2 = newCat2.CategoryID;
            });

            // association conflict submission
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    // update association
                    Product product1 = nw1.Products.First();
                    origCatID = product1.CategoryID ?? 0;
                    Assert.AreNotEqual(product1.CategoryID, newCat1.CategoryID);
                    product1.Category = newCat1;

                    // update property
                    origUnitPrice = product1.UnitPrice ?? 0;
                    nw1NewUnitPrice = origUnitPrice + 1;
                    product1.UnitPrice = nw1NewUnitPrice;

                    origReorderLevel = product1.ReorderLevel ?? 0;
                    nw1NewReorderLevel = (short)(origReorderLevel + 1);
                    product1.ReorderLevel = nw1NewReorderLevel;

                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    // conflicting update on association
                    Product product2 = nw2.Products.First();
                    Assert.AreNotEqual(product2.CategoryID, newCat2.CategoryID);
                    Assert.AreEqual(origReorderLevel, product2.ReorderLevel);
                    product2.Category = newCat2;
                    product2.ResolveMethod = "KeepCurrent";

                    nw2NewUnitPrice = origUnitPrice + 2;
                    product2.UnitPrice = nw2NewUnitPrice;

                    so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so2);

                // verify that resolve method implementation successfully got rid of the conflicts
                Product product2 = nw2.Products.First();
                Assert.IsNull(product2.EntityConflict);

                // Verify KeepCurrent option resolves by overwriting properties with client changes
                Assert.AreEqual(catId2, product2.CategoryID);
                Assert.AreEqual(origReorderLevel, product2.ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that client values from nw2 were succesfully saved
                    Product product1 = nw1.Products.First();
                    Assert.AreEqual(catId2, product1.CategoryID);
                    Assert.AreEqual(origReorderLevel, product1.ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns true and conflicts are resolved using RefreshCurrent policy")]
        public virtual void ResolveTest_RefreshCurrent()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal origUnitPrice0 = 0, nw1NewUnitPrice0 = 0, nw2NewUnitPrice0 = 0;
            short origReorderLevel0 = 0, nw1NewReorderLevel0 = 0;
            SubmitOperation so = null;

            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count > 0);
                    Assert.IsTrue(nw2.Products.Count > 0);
                    Product product = nw1.Products.First();
                    origUnitPrice0 = product.UnitPrice ?? 0;
                    nw1NewUnitPrice0 = origUnitPrice0 + 1;
                    origReorderLevel0 = product.ReorderLevel ?? 0;
                    nw1NewReorderLevel0 = (short)(origReorderLevel0 + 1);

                    product.ReorderLevel = nw1NewReorderLevel0;
                    product.UnitPrice = nw1NewUnitPrice0;
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product product = nw2.Products.First();
                    Assert.AreEqual(origUnitPrice0, product.UnitPrice);
                    nw2NewUnitPrice0 = origUnitPrice0 + 2;
                    product.UnitPrice = nw2NewUnitPrice0;
                    product.ResolveMethod = "RefreshCurrent";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            // after submitted is raised
            EnqueueCallback(delegate
            {
                Assert.IsNull(so.Error);

                // verify that resolve method implementation successfully got rid of the conflicts
                Product product = nw2.Products.First();
                Assert.IsNull(product.EntityConflict);

                // Verify RefreshCurrent option resolves by overwriting client changes
                // with the store values, so nw2 should reflect the values saved by nw1
                Assert.AreEqual(nw1NewUnitPrice0, product.UnitPrice);
                Assert.AreEqual(nw1NewReorderLevel0, product.ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that updates from nw2 were reverted
                    Product product = nw1.Products.First();
                    Assert.AreEqual(nw1NewUnitPrice0, product.UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel0, product.ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Resolve method returns true and conflicts are resolved using RefreshCurrent policy")]
        public virtual void ResolveTest_RefreshCurrent_Association()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            int catId1 = 0, catId2 = 0, origCatID = 0;
            string catName1 = GetUniqueCategoryName();
            string catName2 = GetUniqueCategoryName();
            Category newCat1 = null, newCat2 = null;
            short origReorderLevel = 0, nw1NewReorderLevel = 0;

            SubmitOperation so1 = null, so2 = null;
            LoadOperation nw1Load = null, nw2Load = null;

            // the following sets up the new categories for association update use
            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1Load = nw1.Load(nw1.GetCategoriesQuery(), false);
            });
            EnqueueConditional(() => nw1Load.IsComplete);
            EnqueueCallback(delegate
            {
                nw1Load = null;

                // create two new categories and insert into the database
                newCat1 = new Category { CategoryName = catName1, Description = catName1 };
                newCat2 = new Category { CategoryName = catName2, Description = catName2 };
                nw1.Categories.Add(newCat1);
                nw1.Categories.Add(newCat2);
                so1 = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so1.IsComplete);
            EnqueueCallback(delegate
            {
                if (so1.Error != null)
                {
                    Debug.WriteLine("nw1SubmitResult.Error:");
                    Debug.WriteLine(string.Format("Message: {0}", so1.Error.Message));
                    Debug.WriteLine(string.Format("Exception Type: {0}", so1.Error.GetType().ToString()));
                    Debug.WriteLine(string.Format("Callstack: {0}", so1.Error.StackTrace));
                }
                Assert.IsNull(so1.Error, "category insertion should complete successfully");

                // relaod all categories from database
                nw1.Categories.Clear();
                nw1Load = nw1.Load(nw1.GetCategoriesQuery(), false);
                nw2Load = nw2.Load(nw2.GetCategoriesQuery(), false);
            });
            EnqueueConditional(() => nw1Load.IsComplete && nw2Load.IsComplete);
            EnqueueCallback(delegate
            {
                // get the real autosynced category id (since it's generated by the store during insert)
                newCat1 = nw1.Categories.Single(c => c.CategoryName == catName1);
                catId1 = newCat1.CategoryID;
                newCat2 = nw2.Categories.Single(c => c.CategoryName == catName2);
                catId2 = newCat2.CategoryID;
            });

            // association update submission
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    // update association
                    Product product1 = nw1.Products.First();
                    origCatID = product1.CategoryID ?? 0;
                    Assert.AreNotEqual(product1.CategoryID, newCat1.CategoryID);
                    product1.Category = newCat1;

                    // update property
                    origReorderLevel = product1.ReorderLevel ?? 0;
                    nw1NewReorderLevel = (short)(origReorderLevel + 1);
                    product1.ReorderLevel = nw1NewReorderLevel;

                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    // conflicting update on association
                    Product product2 = nw2.Products.First();
                    Assert.AreNotEqual(product2.CategoryID, newCat2.CategoryID);
                    Assert.AreEqual(origReorderLevel, product2.ReorderLevel);
                    product2.Category = newCat2;
                    product2.ResolveMethod = "RefreshCurrent";
                    so2 = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                Assert.IsNull(so2.Error);

                // verify that resolve method implementation successfully got rid of the conflicts
                Product product2 = nw2.Products.First();
                Assert.IsNull(product2.EntityConflict);

                // Verify RefreshCurrent option resolves by overwriting properties with store changes
                Assert.AreEqual(catId1, product2.CategoryID);
                Assert.AreEqual(nw1NewReorderLevel, product2.ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify that client values from nw2 were not saved
                    Product product1 = nw1.Products.First();
                    Assert.AreEqual(catId1, product1.CategoryID);
                    Assert.AreEqual(nw1NewReorderLevel, product1.ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public virtual void ResolveTest_DeleteUpdateConflict_Bug597087()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            LoadOperation nw1load = null, nw2load = null;
            Product product1 = null, product2 = null;
            decimal newUnitPrice = 0;
            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                // take the last one to avoid conflicts when tests are run together
                nw1load = nw1.Load(nw1.GetProductsQuery().OrderByDescending(p => p.ProductID).Take(1), false);
                nw2load = nw2.Load(nw2.GetProductsQuery().OrderByDescending(p => p.ProductID).Take(1), false);
            });
            EnqueueConditional(() => nw1load.IsComplete && nw2load.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(nw1load);
                TestHelperMethods.AssertOperationSuccess(nw2load);

                Assert.IsTrue(nw1.Products.Count > 0);
                Assert.IsTrue(nw2.Products.Count > 0);

                product1 = nw1.Products.First(p => p.Order_Details.Count == 0);
                product2 = nw2.Products.First(p => p.Order_Details.Count == 0);
                Assert.AreEqual(product1.ProductID, product2.ProductID);

                // now load the order details pertaining to this product id
                nw1load = nw1.Load(nw1.GetOrderDetailsQuery().Where(o => (o.ProductID == product1.ProductID)), false);
            });
            EnqueueConditional(() => nw1load.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(nw1load);

                // remove product in one context, since product is also referenced by OrderDetails, we also need to 
                // remove the corresponding entries (if any) to avoid violating FK constraints
                Order_Detail[] detailsToRemove = nw1.Order_Details.ToArray();
                foreach (Order_Detail detail in detailsToRemove)
                {
                    nw1.Order_Details.Remove(detail);
                }
                Assert.IsTrue(nw1.Order_Details.Count == 0);
                nw1.Products.Remove(product1);
                so = nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);

                // update the same product in another context
                newUnitPrice = (product2.UnitPrice ?? 0) + 1;
                product2.ResolveMethod = "MergeIntoCurrent";
                product2.UnitPrice = newUnitPrice;
                so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(so.Error);
                
                Assert.IsInstanceOfType(so.Error, typeof(DomainOperationException));
                DomainOperationException ex = so.Error as DomainOperationException;

                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Conflicts, so.Error.Message);
                Assert.AreEqual(1, so.EntitiesInError.Count());
                EntityConflict conflict = so.EntitiesInError.Single().EntityConflict;
                Assert.IsTrue(conflict.IsDeleted);
            });
            EnqueueTestComplete();
        }
        #endregion

        #region multiple entities in conflict tests
        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Three entities in conflict and all resolve methods return true using a combination of resolve policies")]
        public virtual void ResolveTest_MultipleInstances_MixedResolveMethods()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal[] origUnitPrice = new decimal[3], nw1NewUnitPrice = new decimal[3], nw2NewUnitPrice = new decimal[3];
            short[] origReorderLevel = new short[3], nw1NewReorderLevel = new short[3];
            SubmitOperation so = null;

            // Create conflicts through submission through 2 contexts
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count >= 3);
                    Assert.IsTrue(nw2.Products.Count >= 3);

                    Product[] products = nw1.Products.ToArray();
                    for (int i = 0; i < 3; i++)
                    {
                        Product prod = products[i];
                        origUnitPrice[i] = prod.UnitPrice ?? 0;
                        nw1NewUnitPrice[i] = origUnitPrice[i] + 1;

                        origReorderLevel[i] = prod.ReorderLevel ?? 0;
                        nw1NewReorderLevel[i] = (short)(origReorderLevel[i] + 1);

                        prod.ReorderLevel = nw1NewReorderLevel[i];
                        prod.UnitPrice = nw1NewUnitPrice[i];
                    }
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    for (int i = 0; i < 3; i++)
                    {
                        Product prod = products[i];
                        Assert.AreEqual(origUnitPrice[i], prod.UnitPrice);
                        nw2NewUnitPrice[i] = origUnitPrice[i] + 2;
                        prod.UnitPrice = nw2NewUnitPrice[i];
                    }
                    products[0].ResolveMethod = "MergeIntoCurrent";
                    products[1].ResolveMethod = "RefreshCurrent";
                    products[2].ResolveMethod = "KeepCurrent";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            // Note: there are different expected behaviors wrt L2S and EF domain services.
            // L2S supports continue on error so all the conflicts should be resolved during the submit
            // EF stops on first error so each subsequent resolve only takes care of one conflict. Since our
            // framework by default only retry once, the first product's conflict will be resolved and on
            // resubmit, the second product will return conflict
            EnqueueCallback(delegate
            {
                Product[] products = nw2.Products.ToArray();
                if (this.ProviderType == ProviderType.EF)
                {
                    // Verify Product0 changes was not synced back to the client since resubmit still fails with conflict
                    Assert.IsNull(products[0].EntityConflict);
                    Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(origReorderLevel[0], products[0].ReorderLevel);

                    // Verify Product1's conflict is non empty and client values did not get updated
                    Assert.IsInstanceOfType(so.Error, typeof(DomainOperationException));
                    DomainOperationException ex = so.Error as DomainOperationException;
                    Assert.AreEqual(OperationErrorStatus.Conflicts, ex.Status);
                    Assert.AreEqual(products[1], so.EntitiesInError.Single());

                    Assert.IsNotNull(products[1].EntityConflict);
                    EntityConflict conflict = products[1].EntityConflict;
                    Assert.AreEqual(2, conflict.PropertyNames.Count());
                    Assert.AreEqual(nw1NewUnitPrice[1], conflict.StoreEntity.ExtractState()["UnitPrice"]);
                    Assert.AreEqual(nw2NewUnitPrice[1], conflict.CurrentEntity.ExtractState()["UnitPrice"]);
                    Assert.AreEqual(origUnitPrice[1], conflict.OriginalEntity.ExtractState()["UnitPrice"]);

                    Assert.AreEqual(nw2NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(origReorderLevel[1], products[1].ReorderLevel);

                    // Verify Product2's conflict is empty and client values did not get updated
                    Assert.IsNull(products[2].EntityConflict);
                    Assert.AreEqual(nw2NewUnitPrice[2], products[2].UnitPrice);
                    Assert.AreEqual(origReorderLevel[2], products[2].ReorderLevel);
                }
                else
                {
                    // Verify MergeIntoCurrent on Product0 merges the client and store values
                    Assert.IsNull(products[0].EntityConflict);
                    Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);

                    // Verify RefreshCurrent on Product1 reverts client changes
                    Assert.IsNull(products[1].EntityConflict);
                    Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);

                    // Verify KeepCurrent on Product2 overwrites store with client changes
                    Assert.IsNull(products[2].EntityConflict);
                    Assert.AreEqual(nw2NewUnitPrice[2], products[2].UnitPrice);
                    Assert.AreEqual(origReorderLevel[2], products[2].ReorderLevel);
                }
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    Product[] products = nw1.Products.ToArray();
                    if (this.ProviderType == ProviderType.EF)
                    {
                        // verify nw2 changes are not persisted
                        Assert.AreEqual(nw1NewUnitPrice[0], products[0].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                        Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                        Assert.AreEqual(nw1NewUnitPrice[2], products[2].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[2], products[2].ReorderLevel);
                    }
                    else
                    {
                        // verify nw2 changes are persisted
                        Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                        Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                        Assert.AreEqual(nw2NewUnitPrice[2], products[2].UnitPrice);
                        Assert.AreEqual(origReorderLevel[2], products[2].ReorderLevel);
                    }
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Two entities in conflict, one return true and the other returns false")]
        public virtual void ResolveTest_MultipleInstances_ReturnTrueFalse()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal[] origUnitPrice = new decimal[2], nw1NewUnitPrice = new decimal[2], nw2NewUnitPrice = new decimal[2];
            short[] origReorderLevel = new short[2], nw1NewReorderLevel = new short[2];
            SubmitOperation so = null;

            // Create conflicts through submission through 2 contexts
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count >= 2);
                    Assert.IsTrue(nw2.Products.Count >= 2);

                    Product[] products = nw1.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        origUnitPrice[i] = products[i].UnitPrice ?? 0;
                        nw1NewUnitPrice[i] = origUnitPrice[i] + 1;

                        origReorderLevel[i] = products[i].ReorderLevel ?? 0;
                        nw1NewReorderLevel[i] = (short)(origReorderLevel[i] + 1);

                        products[i].ReorderLevel = nw1NewReorderLevel[i];
                        products[i].UnitPrice = nw1NewUnitPrice[i];
                    }
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        Assert.AreEqual(origUnitPrice[i], products[i].UnitPrice);
                        nw2NewUnitPrice[i] = origUnitPrice[i] + 2;
                        products[i].UnitPrice = nw2NewUnitPrice[i];
                    }
                    products[0].ResolveMethod = "RefreshCurrent";
                    products[1].ResolveMethod = "ReturnFalse";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                Assert.IsInstanceOfType(so.Error, typeof(DomainOperationException));
                DomainOperationException ex = so.Error as DomainOperationException;
                Assert.AreEqual(OperationErrorStatus.Conflicts, ex.Status);

                Product[] products = nw2.Products.ToArray();
                EntityConflict conflict = null;
                if (this.ProviderType == ProviderType.EF)
                {
                    // EF only supports getting the first conflict so only Product0's conflicts will be generated and only
                    // its resolve method is called. Hence the conflict is resolved successfully the first round. When resubmit
                    // is called, the Product1's conflicts are generated. So client and store values should not be updated 
                    // even for Product0.
                    Assert.IsNull(products[0].EntityConflict);
                }
                else
                {
                    // L2S continues on error so both resolve methods are called. Here, we verify client values are not 
                    // updated since one resolve method returns false
                    conflict = products[0].EntityConflict;
                    Assert.IsNotNull(conflict);
                    Assert.IsTrue(conflict.PropertyNames.Contains("UnitPrice"));
                    Assert.AreEqual(nw1NewUnitPrice[0], ((Product)conflict.StoreEntity).UnitPrice);
                    Assert.AreEqual(nw2NewUnitPrice[0], ((Product)conflict.CurrentEntity).UnitPrice);
                    Assert.AreEqual(origUnitPrice[0], ((Product)conflict.OriginalEntity).UnitPrice);
                }

                // in either case, conflict on product1 reflects one from the resubmit
                conflict = products[1].EntityConflict;
                Assert.IsNotNull(conflict);
                Assert.AreEqual(2, products[1].EntityConflict.PropertyNames.Count());
                Assert.IsTrue(conflict.PropertyNames.Contains("UnitPrice"));
                Assert.AreEqual(nw1NewUnitPrice[1], ((Product)conflict.StoreEntity).UnitPrice);
                Assert.AreEqual(nw2NewUnitPrice[1], ((Product)conflict.CurrentEntity).UnitPrice);
                Assert.AreEqual(origUnitPrice[1], ((Product)conflict.OriginalEntity).UnitPrice);

                // both products should not be changed on client
                Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                Assert.AreEqual(origReorderLevel[0], products[0].ReorderLevel);
                Assert.AreEqual(nw2NewUnitPrice[1], products[1].UnitPrice);
                Assert.AreEqual(origReorderLevel[1], products[1].ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify store values are not changed since resubmit should not be called
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw1NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                    Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Two entities in conflict, both return true")]
        public virtual void ResolveTest_MultipleInstances_ReturnTrueTrue()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal[] origUnitPrice = new decimal[2], nw1NewUnitPrice = new decimal[2], nw2NewUnitPrice = new decimal[2];
            short[] origReorderLevel = new short[2], nw1NewReorderLevel = new short[2];
            SubmitOperation so = null;

            // Create conflicts through submission through 2 contexts
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count >= 2);
                    Assert.IsTrue(nw2.Products.Count >= 2);

                    Product[] products = nw1.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        origUnitPrice[i] = products[i].UnitPrice ?? 0;
                        nw1NewUnitPrice[i] = origUnitPrice[i] + 1;

                        origReorderLevel[i] = products[i].ReorderLevel ?? 0;
                        nw1NewReorderLevel[i] = (short)(origReorderLevel[i] + 1);

                        products[i].ReorderLevel = nw1NewReorderLevel[i];
                        products[i].UnitPrice = nw1NewUnitPrice[i];
                    }
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        Assert.AreEqual(origUnitPrice[i], products[i].UnitPrice);
                        nw2NewUnitPrice[i] = origUnitPrice[i] + 2;
                        products[i].UnitPrice = nw2NewUnitPrice[i];
                    }
                    products[0].ResolveMethod = "MergeIntoCurrent";
                    products[1].ResolveMethod = "RefreshCurrent";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                Product[] products = nw2.Products.ToArray();
                if (this.ProviderType == ProviderType.EF)
                {
                    // EF only supports getting the first conflict so only Product0's conflicts will be generated and only
                    // its resolve method is called. Hence the conflict is resolved successfully the first round. When resubmit
                    // is called, the Product1's conflicts are generated. Since ResolveConflicts is not called again on resubmit,
                    // the Submit processing failed with conflicts.
                    Assert.IsInstanceOfType(so.Error, typeof(DomainOperationException));
                    DomainOperationException ex = so.Error as DomainOperationException;
                    Assert.AreEqual(OperationErrorStatus.Conflicts, ex.Status);

                    Assert.IsNull(products[0].EntityConflict);
                    Assert.IsNotNull(products[1].EntityConflict);
                    Assert.AreEqual(2, products[1].EntityConflict.PropertyNames.Count());
                    EntityConflict conflict = products[1].EntityConflict;
                    Assert.IsTrue(conflict.PropertyNames.Contains("UnitPrice"));
                    Assert.AreEqual(nw1NewUnitPrice[1], ((Product)conflict.StoreEntity).UnitPrice);
                    Assert.AreEqual(nw2NewUnitPrice[1], ((Product)conflict.CurrentEntity).UnitPrice);
                    Assert.AreEqual(origUnitPrice[1], ((Product)conflict.OriginalEntity).UnitPrice);

                    // both products should not be changed on client
                    Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(origReorderLevel[0], products[0].ReorderLevel);
                    Assert.AreEqual(nw2NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(origReorderLevel[1], products[1].ReorderLevel);
                }
                else
                {
                    // L2S continues on error so both resolve methods are called. Here, we verify client values are autosynced
                    // back since the resubmit is successful
                    Assert.IsNull(so.Error);
                    Assert.IsNull(products[0].EntityConflict);
                    Assert.IsNull(products[1].EntityConflict);

                    // verify values are updated
                    Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                    Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                }
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    Product[] products = nw1.Products.ToArray();
                    if (ProviderType == ProviderType.EF)
                    {
                        // verify store values are not changed since submit failed
                        Assert.AreEqual(nw1NewUnitPrice[0], products[0].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                        Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                    }
                    else
                    {
                        // verify store values are updated since resubmit succeeded
                        Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                        Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                        Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                    }
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Two entities in conflict, one throws exception and the other resolves successfully")]
        public virtual void ResolveTest_MultipleInstances_FirstThrowsSecondSucceeds()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal[] origUnitPrice = new decimal[2], nw1NewUnitPrice = new decimal[2], nw2NewUnitPrice = new decimal[2];
            short[] origReorderLevel = new short[2], nw1NewReorderLevel = new short[2];
            SubmitOperation so = null;

            // Create conflicts through submission through 2 contexts
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count >= 2);
                    Assert.IsTrue(nw2.Products.Count >= 2);

                    Product[] products = nw1.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        origUnitPrice[i] = products[i].UnitPrice ?? 0;
                        nw1NewUnitPrice[i] = origUnitPrice[i] + 1;

                        origReorderLevel[i] = products[i].ReorderLevel ?? 0;
                        nw1NewReorderLevel[i] = (short)(origReorderLevel[i] + 1);

                        products[i].ReorderLevel = nw1NewReorderLevel[i];
                        products[i].UnitPrice = nw1NewUnitPrice[i];
                    }
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        Assert.AreEqual(origUnitPrice[i], products[i].UnitPrice);
                        nw2NewUnitPrice[i] = origUnitPrice[i] + 2;
                        products[i].UnitPrice = nw2NewUnitPrice[i];
                    }
                    products[0].ResolveMethod = "ThrowValidationEx";
                    products[1].ResolveMethod = "RefreshCurrent";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                // Since an exception was thrown during resolution, conflicts are not
                // returned
                Assert.IsInstanceOfType(so.Error, typeof(DomainOperationException));
                DomainOperationException ex = so.Error as DomainOperationException;
                Assert.IsTrue(ex.Message.Contains("testing"));
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify store values are not changed since resubmit should not be called
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw1NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                    Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Two entities in conflict, resolution succeeds for and the other throws an exception")]
        public virtual void ResolveTest_MultipleInstances_FirstSucceedsSecondThrows()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal[] origUnitPrice = new decimal[2], nw1NewUnitPrice = new decimal[2], nw2NewUnitPrice = new decimal[2];
            short[] origReorderLevel = new short[2], nw1NewReorderLevel = new short[2];
            SubmitOperation so = null;

            // Create conflicts through submission through 2 contexts
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count >= 2);
                    Assert.IsTrue(nw2.Products.Count >= 2);

                    Product[] products = nw1.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        origUnitPrice[i] = products[i].UnitPrice ?? 0;
                        nw1NewUnitPrice[i] = origUnitPrice[i] + 1;

                        origReorderLevel[i] = products[i].ReorderLevel ?? 0;
                        nw1NewReorderLevel[i] = (short)(origReorderLevel[i] + 1);

                        products[i].ReorderLevel = nw1NewReorderLevel[i];
                        products[i].UnitPrice = nw1NewUnitPrice[i];
                    }
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        Assert.AreEqual(origUnitPrice[i], products[i].UnitPrice);
                        nw2NewUnitPrice[i] = origUnitPrice[i] + 2;
                        products[i].UnitPrice = nw2NewUnitPrice[i];
                    }
                    products[0].ResolveMethod = "RefreshCurrent";
                    products[1].ResolveMethod = "ThrowValidationEx";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                Assert.IsInstanceOfType(so.Error, typeof(DomainOperationException));
                DomainOperationException ex = so.Error as DomainOperationException;

                Product[] products = nw2.Products.ToArray();
                if (this.ProviderType == ProviderType.EF)
                {
                    // EF only supports getting the first conflict, so Product0's conflicts will be generated during first submit,
                    // then they are resolved server side and resubmit is called. Product1's conflicts will then be generated. Since
                    // a second resolve is not performed after resubmit, the exception is not propagated back to the client but
                    // conflicts are. Client values should not be synced back.

                    // validation error should not be reported through Product1. Only conflicts errors are observed.
                    Assert.AreEqual(OperationErrorStatus.Conflicts, ex.Status);
                    Assert.AreEqual(0, products[1].ValidationErrors.Count());

                    // the conflicts returned are from the resubmit
                    Assert.IsNull(products[0].EntityConflict);
                    EntityConflict conflict = products[1].EntityConflict;
                    Assert.IsNotNull(conflict);
                    Assert.AreEqual(2, conflict.PropertyNames.Count());

                }
                else
                {
                    // When resolving the second product the exception is thrown,
                    // so conflicts aren't reported - the exception is
                    Assert.IsTrue(so.HasError);
                    Assert.IsTrue(so.Error.Message.Contains("testing"));
                }

                // client values of Products should not change
                Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                Assert.AreEqual(origReorderLevel[0], products[0].ReorderLevel);
                Assert.AreEqual(nw2NewUnitPrice[1], products[1].UnitPrice);
                Assert.AreEqual(origReorderLevel[1], products[1].ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify store values are not changed since resubmit should not be called
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw1NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                    Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Two entities in conflict, one throws non-continuable ex and the other returns true")]
        public virtual void ResolveTest_MultipleInstances_ThrowNonContinuableExReturnTrue()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);
            decimal[] origUnitPrice = new decimal[2], nw1NewUnitPrice = new decimal[2], nw2NewUnitPrice = new decimal[2];
            short[] origReorderLevel = new short[2], nw1NewReorderLevel = new short[2];
            SubmitOperation so = null;

            // Create conflicts through submission through 2 contexts
            this.ResolveTestHelper_Submit(
                nw1,
                nw2,
                delegate
                {
                    // test logic on nw1 data context
                    Assert.IsTrue(nw1.Products.Count >= 2);
                    Assert.IsTrue(nw2.Products.Count >= 2);

                    Product[] products = nw1.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        origUnitPrice[i] = products[i].UnitPrice ?? 0;
                        nw1NewUnitPrice[i] = origUnitPrice[i] + 1;

                        origReorderLevel[i] = products[i].ReorderLevel ?? 0;
                        nw1NewReorderLevel[i] = (short)(origReorderLevel[i] + 1);

                        products[i].ReorderLevel = nw1NewReorderLevel[i];
                        products[i].UnitPrice = nw1NewUnitPrice[i];
                    }
                    nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                },
                delegate
                {
                    // test logic on nw2 data context
                    Product[] products = nw2.Products.ToArray();
                    for (int i = 0; i < 2; i++)
                    {
                        Assert.AreEqual(origUnitPrice[i], products[i].UnitPrice);
                        nw2NewUnitPrice[i] = origUnitPrice[i] + 2;
                        products[i].UnitPrice = nw2NewUnitPrice[i];
                    }
                    products[0].ResolveMethod = "ThrowDomainServiceEx";
                    products[1].ResolveMethod = "RefreshCurrent";
                    so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                });

            EnqueueCallback(delegate
            {
                Assert.IsInstanceOfType(so.Error, typeof(DomainException));
                DomainException ex = so.Error as DomainException;
                Assert.AreEqual("testing", ex.Message);

                // Verify client values are not updated and conflict info is not available since receiving 
                // DomainServiceException from server will terminate the post-submit processing
                Product[] products = nw2.Products.ToArray();
                Assert.IsNull(products[0].EntityConflict);
                Assert.AreEqual(nw2NewUnitPrice[0], products[0].UnitPrice);
                Assert.AreEqual(origReorderLevel[0], products[0].ReorderLevel);

                Assert.IsNull(products[1].EntityConflict);
                Assert.AreEqual(nw2NewUnitPrice[1], products[1].UnitPrice);
                Assert.AreEqual(origReorderLevel[1], products[1].ReorderLevel);
            });

            this.ResolveTestHelper_ReloadAndVerify(
                nw1,
                delegate
                {
                    // verify store values are not changed since resubmit should not be called
                    Product[] products = nw1.Products.ToArray();
                    Assert.AreEqual(nw1NewUnitPrice[0], products[0].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[0], products[0].ReorderLevel);
                    Assert.AreEqual(nw1NewUnitPrice[1], products[1].UnitPrice);
                    Assert.AreEqual(nw1NewReorderLevel[1], products[1].ReorderLevel);
                });

            EnqueueTestComplete();
        }
        #endregion

        #region Resolve test helpers to reduce amount of code for small changes across different resolve scenarios
        /// <summary>
        /// Helper that creates conflicts using 2 contexts. It'll call nw1SubmitTestAction, then nw2SubmitTestAction. When this
        /// helper returns, one can access nw2.SubmittedChangesEventArgs to get the submitted results
        /// </summary>
        /// <param name="nw1">first data context</param>
        /// <param name="nw2">second data context</param>
        /// <param name="nw1SubmitTestAction">custom test actions to be done on nw1. This should include call to SubmitChanges.</param>
        /// <param name="nw2SubmitTestAction">custom test actions to be done on nw2. This should include call to SubmitChanges.</param>
        private void ResolveTestHelper_Submit(Northwind nw1, Northwind nw2, Action nw1SubmitTestAction, Action nw2SubmitTestAction)
        {
            LoadOperation nw1Load = null, nw2Load = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });

            EnqueueCallback(delegate
            {
                nw1Load = nw1.Load(nw1.GetProductsQuery().Take(3), false);
                nw2Load = nw2.Load(nw2.GetProductsQuery().Take(3), false);
            });
            EnqueueConditional(() => nw1Load.IsComplete && nw2Load.IsComplete);
            EnqueueCallback(delegate
            {
                if (nw1Load.Error != null)
                {
                    Assert.Fail("nw1 error: " + nw1Load.Error.ToString());
                }
                if (nw2Load.Error != null)
                {
                    Assert.Fail("nw2 error: " + nw2Load.Error.ToString());
                }
            });

            // execute custom test logic including submit on nw1 data context
            EnqueueCallback(nw1SubmitTestAction);

            EnqueueConditional(() => !nw1.IsSubmitting);

            // execute custom test logic including submit on nw2 data context
            EnqueueCallback(nw2SubmitTestAction);

            EnqueueConditional(() => !nw2.IsSubmitting);
        }

        /// <summary>
        /// Test helper to reload using the specified data context and executes custom verifications after 
        /// entites are successfully loaded
        /// </summary>
        /// <param name="nw">context to reload</param>
        /// <param name="verificationsOnReload">custom test verifications after load completed</param>
        private void ResolveTestHelper_ReloadAndVerify(Northwind nw, Action verificationsOnReload)
        {
            LoadOperation lo = null;

            EnqueueCallback(delegate
            {
                // clear our cache and requery through nw context to verify that our changes were persisted
                nw.EntityContainer.Clear();
                lo = nw.Load(nw.GetProductsQuery().Take(3), false);
            });
            EnqueueConditional(() => lo.IsComplete);

            // execute custom test logic after nw is refreshed
            EnqueueCallback(verificationsOnReload);
        }
        #endregion
    }

    [TestClass]
    public class LTSUpdateTests : UpdateTests
    {
        public LTSUpdateTests()
            : base(TestURIs.LTS_Northwind_CUD, ProviderType.LTS)
        {
        }

        /// <summary>
        /// Silverlight version of class initializer doesn't take TestContext
        /// </summary>
        [ClassInitialize]
        public static void ClassSetup(
#if !SILVERLIGHT
TestContext testContext
#endif
)
        {
            // ensure that our isolation DB has been created once and only once
            // at the test fixture level
            TestDatabase.Initialize();
        }

        /// <summary>
        /// This is an LTS only test currently since EF doesn't support the entitiy
        /// level OnValidate partial method pattern.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void Bug594068_PersistChangesErrorHandling()
        {
            Northwind ctxt = new Northwind(TestURIs.LTS_Northwind_CUD);
            SubmitOperation so = null;

            Product prod = new Product
            {
                ProductID = 1,
                ProductName = "Crispy Snax",
                ReorderLevel = 5
            };

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(() =>
            {
                ctxt.Products.Attach(prod);

                // create an invalid update
                prod.ReorderLevel = -1;
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(() =>
            {
                Assert.IsNotNull(so.Error);
                Product prodInError = so.EntitiesInError.Cast<Product>().Single();
                var err = prodInError.ValidationErrors.Single();
                Assert.AreEqual("Invalid Product Update!", err.ErrorMessage);
                Assert.IsTrue(err.MemberNames.Contains("ReorderLevel"));
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void ChangeSet_CancelSubmit()
        {
            Product product = null;
            Northwind ctxt = new Northwind(TestURIs.LTS_Northwind_CUD);
            LoadOperation lo = null;
            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(() =>
            {
                lo = ctxt.Load(ctxt.GetProductsQuery().Skip(3).Take(1), false);
            });
            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(() =>
            {
                Assert.IsNull(lo.Error);

                // Verify we received 1 product as expected
                Assert.AreEqual<int>(1, ctxt.Products.Count);
                Assert.IsFalse(ctxt.EntityContainer.HasChanges);

                // Modify product and submit
                product = ctxt.Products.First();
                product.ProductName = "New Product Name";

                Assert.IsTrue(ctxt.EntityContainer.HasChanges);
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
                Assert.IsTrue(so.CanCancel, "Cancellation should be supported.");
            });
            EnqueueCallback(() =>
            {
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.All(e => e.IsSubmitting), "Not all entities are in IsSubmitting state.");

                // cancel the submit
                so.Cancel();
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(() =>
            {
                // Verify event args
                Assert.IsNull(so.Error);
                Assert.IsTrue(so.IsCanceled, "Operation should've been cancelled.");

                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsFalse(changeSet.Any(e => e.IsSubmitting), "Not all entities are out of the IsSubmitting state.");
                Assert.IsTrue(ctxt.EntityContainer.HasChanges);
                Assert.AreSame(product, so.ChangeSet.ModifiedEntities.First());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public override void SimpleUpdateConflict()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);

            LoadOperation nw1load = null, nw2load = null;
            string nw1address = DateTime.Now.Ticks.ToString() + " - nw1";
            string nw2address = DateTime.Now.Ticks.ToString() + " - nw2";
            string nw2addressOriginal = null;
            string nw2address2 = DateTime.Now.Ticks.ToString() + " - nw2.2";

            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1load = nw1.Load(nw1.GetCustomersQuery(), false);
                nw2load = nw2.Load(nw2.GetCustomersQuery(), false);
            });
            EnqueueConditional(() => nw1load.IsComplete&& nw2load.IsComplete);
            EnqueueCallback(delegate
            {
                if (nw1load.Error != null)
                {
                    Assert.Fail("nw1 error: " + nw1load.Error.ToString());
                }
                if (nw2load.Error != null)
                {
                    Assert.Fail("nw2 error: " + nw2load.Error.ToString());
                }
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(nw1.Customers.Count > 0);
                Assert.IsTrue(nw2.Customers.Count > 0);
                Customer[] customers = nw1.Customers.ToArray();
                customers[0].Address = nw1address;
                customers[1].Address = nw1address;
                nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => !nw1.IsSubmitting);
            EnqueueCallback(delegate
            {
                Customer[] customers = nw2.Customers.ToArray();
                nw2addressOriginal = customers[0].Address;
                customers[0].Address = nw2address;
                customers[1].Address = nw2address;
                customers[2].Address = nw2address2;
                so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Conflicts, so.Error.Message);
                Entity[] entitiesInConflict = so.EntitiesInError.ToArray();
                Assert.AreEqual(3, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(2, entitiesInConflict.Length);

                Customer customerInConflict = (Customer)entitiesInConflict[0];
                EntityConflict conflict = customerInConflict.EntityConflict;
                Assert.AreEqual(1, conflict.PropertyNames.Count());
                Assert.IsTrue(conflict.PropertyNames.Contains("Address"));
                Assert.AreEqual(nw1address, ((Customer)conflict.StoreEntity).Address);
                Assert.AreEqual(nw2address, ((Customer)conflict.CurrentEntity).Address);
                Assert.AreEqual(nw2addressOriginal, ((Customer)conflict.OriginalEntity).Address);
                Assert.AreEqual(nw2address, ((Customer)entitiesInConflict[0]).Address);
                Assert.AreEqual(nw2address2, ((Customer)so.ChangeSet.ModifiedEntities[2]).Address);

                // resolve all the conflicts and resubmit
                foreach(Entity entityInConflict in entitiesInConflict)
                {
                    conflict = entityInConflict.EntityConflict;
                    var currentState = conflict.CurrentEntity.ExtractState();
                    conflict.Resolve();
                    TestHelperMethods.VerifyEntityState(currentState, conflict.CurrentEntity.ExtractState());
                    Assert.IsNull(entityInConflict.EntityConflict);
                }

                so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void ReplaceObjectViaInsertDelete()
        {
            Northwind ctxt = CreateDomainContext();

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                Load(ctxt.GetCustomersQuery().Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                var c = ctxt.Customers.First();
                ctxt.Customers.Remove(c);
                ctxt.Customers.Add(new Customer()
                {
                    CustomerID = c.CustomerID,
                    CompanyName = c.CompanyName
                });
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
            });

            EnqueueTestComplete();
        }
    }

    [TestClass]
    public class EFUpdateTests : UpdateTests
    {
        public EFUpdateTests()
            : base(TestURIs.EF_Northwind_CUD, ProviderType.EF)
        {
        }

        /// <summary>
        /// Silverlight version of class initializer doesn't take TestContext
        /// </summary>
        [ClassInitialize]
        public static void ClassSetup(
#if !SILVERLIGHT
            TestContext testContext
#endif
)
        {
            // ensure that our isolation DB has been created once and only once
            // at the test fixture level
            TestDatabase.Initialize();
        }

        #region EF POCO Tests
        /// <summary>
        /// Verify query and update for EF POCO model
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void SimplePOCOUpdate()
        {
            Northwind ctxt = new Northwind(TestURIs.EF_Northwind_POCO_CUD);
            EntityChangeSet changeSet = null;
            int modifiedProductID = -1;
            string newProductName = "";
            LoadOperation lo = null;
            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                lo = ctxt.Load(ctxt.GetProductsQuery().OrderBy(p => p.ProductID).Take(5));
            });
            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                // modify a few entities
                Product[] products = ctxt.Products.ToArray();
                products[0].UnitPrice += 5.0M;
                products[1].UnitsInStock -= 1;
                newProductName = products[2].ProductName + "Foobar";
                products[2].ProductName = newProductName;
                modifiedProductID = products[2].ProductID;

                so = ctxt.SubmitChanges();

                // verify that entities are read only after
                // submit is in progress
                Assert.IsTrue(products[0].IsReadOnly);
                Assert.IsTrue(products[1].IsReadOnly);
                Assert.IsTrue(products[2].IsReadOnly);
                Assert.IsFalse(products[3].IsReadOnly);
                Exception exception = null;
                try
                {
                    products[0].UnitPrice -= 1.0M;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                Assert.IsNotNull(exception);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);

                Product[] products = ctxt.Products.ToArray();
                Assert.IsFalse(products[0].IsReadOnly);
                Assert.IsFalse(products[1].IsReadOnly);
                Assert.IsFalse(products[2].IsReadOnly);
                Assert.IsFalse(products[3].IsReadOnly);

                // verify that all changes have been accepted
                changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Unmodified, products[0].EntityState);
                Assert.AreEqual(EntityState.Unmodified, products[1].EntityState);
                Assert.AreEqual(EntityState.Unmodified, products[2].EntityState);

                // clear our cache and requery for one of the modified products
                // and verify that our changes were persisted
                ctxt.EntityContainer.Clear();
                Assert.AreEqual(0, ctxt.Products.Count);
                lo = ctxt.Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Product modifiedProduct = ctxt.Products.Single(p => p.ProductID == modifiedProductID);
                Assert.AreEqual(newProductName, modifiedProduct.ProductName);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify insert for EF POCO model
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void SimplePOCOInsert()
        {
            Northwind ctxt = new Northwind(TestURIs.EF_Northwind_POCO_CUD);
            Product newProduct = null;
            string identifier = Guid.NewGuid().ToString();
            LoadOperation lo = null;
            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                newProduct = new Product
                {
                    ProductName = identifier
                };
                ctxt.Products.Add(newProduct);
                so = ctxt.SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Unmodified, newProduct.EntityState);

                // verify that the ProductID
                // has been synced back
                Assert.AreNotEqual(0, newProduct.ProductID);

                // query in a new context to ensure the product was inserted
                ctxt = new Northwind(TestURIs.EF_Northwind_POCO_CUD);
                lo = ctxt.Load(ctxt.GetProductsQuery().Where(p => p.ProductName == identifier));
            });
            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);
                Product requeriedProduct = ctxt.Products.SingleOrDefault();
                Assert.IsNotNull(requeriedProduct);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify delete for EF POCO model
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void SimplePOCODelete()
        {
            Northwind ctxt = new Northwind(TestURIs.EF_Northwind_POCO_CUD);
            Product newProduct = null;
            Product deleteProduct = null;
            int prevCount = 0;
            string identifier = Guid.NewGuid().ToString();
            LoadOperation lo = null;
            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                newProduct = new Product
                {
                    ProductName = identifier
                };

                // add a new product so we can delete it below
                ctxt.Products.Add(newProduct);
                so = ctxt.SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                // create a new context and load all products
                TestHelperMethods.AssertOperationSuccess(so);
                ctxt = new Northwind(TestURIs.EF_Northwind_POCO_CUD);
                lo = ctxt.Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                // verify that the new product is there
                deleteProduct = ctxt.Products.SingleOrDefault(p => p.ProductName == identifier);
                Assert.IsNotNull(deleteProduct);

                // delete it
                prevCount = ctxt.Products.Count;
                ctxt.Products.Remove(deleteProduct);
                so = ctxt.SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);

                // verify that all changes have been accepted
                EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
                Assert.IsTrue(changeSet.IsEmpty);
                Assert.AreEqual(EntityState.Detached, deleteProduct.EntityState);

                // create a new context and load all products
                ctxt = new Northwind(TestURIs.EF_Northwind_POCO_CUD);
                lo = ctxt.Load(ctxt.GetProductsQuery());
            });
            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                // verify that the product has been removed
                Assert.AreEqual(prevCount - 1, ctxt.Products.Count);
                Assert.IsNull(ctxt.Products.SingleOrDefault(p => p.ProductName == identifier));
            });

            EnqueueTestComplete();
        }
        #endregion

        [TestMethod]
        [Asynchronous]
        public void SimpleDeleteConflict()
        {
            Northwind nw1 = new Northwind(this.ServiceUri);
            Northwind nw2 = new Northwind(this.ServiceUri);

            LoadOperation nw1load = null, nw2load = null;
            SubmitOperation so = null;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                nw1load = nw1.Load(nw1.GetOrderDetailsQuery().Take(5), false);
                nw2load = nw2.Load(nw2.GetOrderDetailsQuery().Take(5), false);
            });
            EnqueueConditional(() => nw1load.IsComplete && nw2load.IsComplete);
            EnqueueCallback(delegate
            {
                if (nw1load.Error != null)
                {
                    Assert.Fail("nw1 error: " + nw1load.Error.ToString());
                }
                if (nw2load.Error != null)
                {
                    Assert.Fail("nw2 error: " + nw2load.Error.ToString());
                }
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(nw1.Order_Details.Count > 0);
                Assert.IsTrue(nw2.Order_Details.Count > 0);
                Order_Detail detail1 = nw1.Order_Details.First();
                Order_Detail detail2 = nw2.Order_Details.First();
                Assert.AreEqual(detail1.OrderID, detail2.OrderID);
                Assert.AreEqual(detail1.ProductID, detail2.ProductID);
                nw1.Order_Details.Remove(detail1);
                nw1.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => !nw1.IsSubmitting);
            EnqueueCallback(delegate
            {
                Order_Detail detail2 = nw2.Order_Details.First();
                nw2.Order_Details.Remove(detail2);
                so = nw2.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Conflicts, so.Error.Message);
                Entity[] entitiesInConflict = so.EntitiesInError.ToArray();
                Assert.AreEqual(1, so.ChangeSet.RemovedEntities.Count);
                Assert.AreEqual(1, entitiesInConflict.Length);
                Assert.IsTrue(entitiesInConflict[0].EntityConflict.IsDeleted);
                ExceptionHelper.ExpectInvalidOperationException(delegate
                {
                    var x = entitiesInConflict[0].EntityConflict.PropertyNames;
                }, Resource.EntityConflict_IsDeleteConflict);

            });
            EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies that if a FK that is part of a concurrency check is not included 
        /// in our change-set, any update fails with a concurrency violation.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void Bug527568_UpdateConflict()
        {
            Northwind ctxt = CreateDomainContext();
            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                // Order with OrderID=10248 has an employee.
                Load(ctxt.GetOrdersQuery().Where(o => o.OrderID == 10248).Take(1));
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
                Order order = ctxt.Orders.SingleOrDefault();
                Assert.IsNotNull(order);
                order.OrderDate = DateTime.Now;
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void AssociationInsertWithFKFieldsOnly()
        {
            Northwind ctxt = CreateDomainContext();
            int? empId = null;
            int newOrderID = -1;

            EnqueueConditional(delegate
            {
                return TestDatabase.IsInitialized;
            });
            EnqueueCallback(delegate
            {
                empId = 1;

                Order order = new Order();
                order.OrderDate = DateTime.Now;
                order.EmployeeID = empId;
                ctxt.Orders.Add(order);
                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();

                newOrderID = ((Order)SubmitOperation.ChangeSet.AddedEntities.First()).OrderID;
                ctxt.EntityContainer.Clear();
                Load(ctxt.GetOrdersQuery().Where(p => p.OrderID == newOrderID), LoadBehavior.MergeIntoCurrent);
            });
            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                Order addedOrder = ctxt.Orders.Single();
                Assert.AreEqual(empId, addedOrder.EmployeeID);
            });

            EnqueueTestComplete();
        }
    }

    [TestClass]
    public class EFCFUpdateTests : UpdateTests
    {
        public EFCFUpdateTests()
            : base(TestURIs.EFCF_Northwind_CUD, ProviderType.EF)
        {
        }

        /// <summary>
        /// Silverlight version of class initializer doesn't take TestContext
        /// </summary>
        [ClassInitialize]
        public static void ClassSetup(
#if !SILVERLIGHT
            TestContext testContext
#endif
)
        {
            // ensure that our isolation DB has been created once and only once
            // at the test fixture level
            TestDatabase.Initialize();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // make sure our test database is removed on the server after all unit tests
            // have been run
            ((IDisposable)TestDatabase).Dispose();
        }
    }

    [TestClass]
    public class DbCtxUpdateTests : UpdateTests
    {
        public DbCtxUpdateTests()
            : base(TestURIs.DbCtx_Northwind_CUD, ProviderType.EF)
        {
        }

        /// <summary>
        /// Silverlight version of class initializer doesn't take TestContext
        /// </summary>
        [ClassInitialize]
        public static void ClassSetup(
#if !SILVERLIGHT
            TestContext testContext
#endif
)
        {
            // ensure that our isolation DB has been created once and only once
            // at the test fixture level
            TestDatabase.Initialize();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // make sure our test database is removed on the server after all unit tests
            // have been run
            ((IDisposable)TestDatabase).Dispose();
        }
    }

    /// <summary>
    /// Non DAL/DB scenario tests
    /// </summary>
    [TestClass]
    public class ScenarioUpdateTests : DomainContextTestBase<TestDomainServices.TestProvider_Scenarios>
    {
        public ScenarioUpdateTests()
            : base(TestURIs.TestProvider_Scenarios, ProviderType.Unspecified)
        {
        }

        /// <summary>
        /// This test verifies that for entity Types with a concurrency timestamp member, EntityConflict.Resolve
        /// copies the store timestamp value into the current entity.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        [WorkItem(193755)]
        public void EntityConflict_ResolveTimestamp()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            SubmitOperation so = null;
            byte[] initialTimestamp = new byte[] { 3, 2, 1 };
            byte[] expectedServerTimestamp = new byte[] { 1, 2, 3 };

            EnqueueCallback(delegate
            {
                // Create an instance and attach it. The test service overrides PersistChangeSet and uses the
                // "TSConcurrencyFailure" value we're setting below as an indicator to generate a concurrency failure.
                TestDomainServices.TimestampEntityA entity = new TestDomainServices.TimestampEntityA { ID = 1, ValueA = "TSConcurrencyFailure" };
                TestHelperMethods.EnableValidation(entity, false);
                entity.Version = initialTimestamp;
                TestHelperMethods.EnableValidation(entity, true);
                ctxt.TimestampEntityAs.Attach(entity);

                // modify the instance
                entity.ValueB = "ClientUpdated";

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                // verify we got the expected conflict
                Assert.IsTrue(so.HasError);
                TestDomainServices.TimestampEntityA entity = (TestDomainServices.TimestampEntityA)so.EntitiesInError.Single();
                EntityConflict conflict = entity.EntityConflict;
                string memberInConflict = conflict.PropertyNames.Single();
                Assert.AreEqual("Version", memberInConflict);
                Assert.IsTrue(initialTimestamp.SequenceEqual(entity.Version));

                TestDomainServices.TimestampEntityA storeEntity = (TestDomainServices.TimestampEntityA)conflict.StoreEntity;
                Assert.IsTrue(expectedServerTimestamp.SequenceEqual(storeEntity.Version));

                // verify that Resolve copies the store timestamp into the current entity instance
                conflict.Resolve();
                Assert.IsTrue(expectedServerTimestamp.SequenceEqual(entity.Version));
                Assert.IsNull(entity.EntityConflict);
                entity.ValueA = "ConflictResolved";

                // we expect the subsequent update to succeed.
                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that for an entity that specifies a timestamp member,
        /// an original entity is not sent to the server on update
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestTimestampUpdate_OriginalNotRoundtripped()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(new Uri(TestURIs.RootURI, "TestDomainServices-TestProvider_Scenarios.svc"));
            TestDomainServices.TimestampEntityA ts = null;
            SubmitOperation so = null;

            LoadOperation lo = ctxt.Load(ctxt.GetTimestampEntityAsQuery(), false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                ts = ctxt.TimestampEntityAs.First();
                ts.ValueA += "Update";

                // Directly inspect the changeset and verify that original is null.
                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                IEnumerable<ChangeSetEntry> csEntries = ChangeSetBuilder.Build(cs);
                ChangeSetEntry csEntry = csEntries.First();
                Assert.IsNull(csEntry.OriginalEntity);

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
                Assert.IsTrue(ts.ValueB.Contains("ServerUpdated"));
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that for an entity that specifies a timestamp member as
        /// well as other non-timestamp roundtripped members, an original entity
        /// WILL be sent to the server on update
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestTimestampUpdate_OriginalRoundtripped()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(new Uri(TestURIs.RootURI, "TestDomainServices-TestProvider_Scenarios.svc"));
            TestDomainServices.TimestampEntityB ts = null;
            SubmitOperation so = null;

            LoadOperation lo = ctxt.Load(ctxt.GetTimestampEntityBsQuery(), false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                ts = ctxt.TimestampEntityBs.First();
                ts.ValueA += "Update";

                // Directly inspect the changeset and verify that original is not null.
                EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
                IEnumerable<ChangeSetEntry> csEntries = ChangeSetBuilder.Build(cs);
                ChangeSetEntry csEntry = csEntries.First();
                Assert.IsNotNull(csEntry.OriginalEntity);

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
                Assert.IsTrue(ts.ValueB.Contains("ServerUpdated"));
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies that if a rogue client attempts to send a value for a member
        /// that is excluded on the server, that value is ignored by the server
        /// during deserialization.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestExcludedMember()
        {
            TestDomainServices.TestProvider_Scenarios provider = new TestDomainServices.TestProvider_Scenarios(new Uri(TestURIs.RootURI, "TestDomainServices-TestProvider_Scenarios.svc"));

            SubmitOperation so = null;

            LoadOperation lo = provider.Load(provider.GetAsQuery(), false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                TestDomainServices.A a = provider.As.First();

                a.RequiredString = "Foobar";

                // set the member that exists only on the client
                // in an attempt to force it through on the server
                a.ExcludedMember = 5;

                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                // expect the excluded member to be ignored and the
                // update to succeed
                Assert.IsNull(so.Error);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestReadOnlyMemberRoundtrip()
        {
            TestDomainServices.TestProvider_Scenarios provider = new TestDomainServices.TestProvider_Scenarios(new Uri(TestURIs.RootURI, "TestDomainServices-TestProvider_Scenarios.svc"));

            SubmitOperation so = null;

            LoadOperation lo = provider.Load(provider.GetAsQuery(), false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                TestDomainServices.A a = provider.As.First();

                Assert.AreEqual("ReadOnlyData", a.ReadOnlyData_NoReadOnlyAttribute);
                Assert.AreEqual("ReadOnlyData", a.ReadOnlyData_NoSetter);
                Assert.AreEqual("ReadOnlyData", a.ReadOnlyData_WithSetter);

                a.RequiredString = "Foobar";

                so = provider.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                // The update method will throw if it received a projection value.
                Assert.IsNull(so.Error);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        public void VerifyKeyMembersCannotBeChanged()
        {
            Northwind ctxt = new Northwind(TestURIs.LTS_Northwind);

            Order_Detail detail = new Order_Detail
            {
                OrderID = 1,
                ProductID = 1
            };
            ctxt.Order_Details.Attach(detail);

            detail.OrderID = 2;

            InvalidOperationException expectedException = null;
            try
            {
                ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.Entity_KeyMembersCannotBeChanged, "OrderID", "Order_Detail"), expectedException.Message);

            ((IRevertibleChangeTracking)detail).RejectChanges();
            detail.ProductID = 5;

            expectedException = null;
            try
            {
                ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.Entity_KeyMembersCannotBeChanged, "ProductID", "Order_Detail"), expectedException.Message);
        }

        [TestMethod]
        public void Bug516237_OneToOneBiDirectionalReference()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            TestDomainServices.C c = new TestDomainServices.C
            {
                ID = 1
            };
            TestDomainServices.D d = new TestDomainServices.D
            {
                ID = 1
            };
            c.D_Ref1 = d;

            Assert.AreEqual(d, c.D_Ref1);
            Assert.AreEqual(c, d.C);

            ctxt.Cs.Add(c);
            ctxt.Ds.Add(d);

            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Contains(c));
            Assert.IsTrue(cs.AddedEntities.Contains(d));
        }

        [TestMethod]
        public void Bug502481_NullPreviousRef()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet employees = ec.GetEntitySet<DataTests.AdventureWorks.LTS.Employee>();

            DataTests.AdventureWorks.LTS.Employee emp = new DataTests.AdventureWorks.LTS.Employee()
            {
                EmployeeID = 100
            };
            employees.Add(emp);
            ((IChangeTracking)ec).AcceptChanges();

            // change from a previous reference of null by
            // setting a ManagerID
            emp.ManagerID = 1;

            // This was failing before the fix when we attempted to 
            // get the previously referenced entity (which was null)
            EntityChangeSet cs = ec.GetChanges();
            List<ChangeSetEntry> operations = ChangeSetBuilder.Build(cs);
        }

        [TestMethod]
        public void Bug499452_DuplicateIDs()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();

            // create two different customer instances with the 
            // same key values
            Customer cust1 = new Customer
            {
                CustomerID = "ALFKI",
                CompanyName = "Foo"
            };
            Customer cust2 = new Customer
            {  
                CompanyName = "Bar"
            };
            Order order1 = new Order
            {
                OrderID = 1,
                CustomerID = "ALFKI"
            };

            ec.LoadEntities(new Entity[] { cust1, order1 });

            // add the second entity which currently has a null identity
            Assert.IsNull(cust2.GetIdentity());
            EntitySet<Customer> custSet = ec.GetEntitySet<Customer>();
            custSet.Add(cust2);

            // at this point we have both entities in the set, but only
            // one in the ID cache (since the add hasn't been accepted yet)
            ec.LoadEntities(new Entity[] { new Customer { CustomerID = "ALFKI", CompanyName = "Updated" } }, LoadBehavior.MergeIntoCurrent);
            Assert.AreEqual("Updated", cust1.CompanyName);

            // The EntityRef will only return the Non-new Customer 
            Assert.IsNotNull(order1.Customer);

            // simulate server ID being synced back. This will result in a a
            // cache collision on Accept below
            cust2.CustomerID = "ALFKI";

            InvalidOperationException expectedException = null;
            try
            {
                ((IChangeTracking)ec).AcceptChanges();
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(Resource.EntitySet_DuplicateIdentity, expectedException.Message);
        }

        [TestMethod]
        public void Bug479438_Update_NonEditable()
        {
            Bug479438_Update_NonEditable_Container ec = new Bug479438_Update_NonEditable_Container();

            Assert.IsFalse(ec.GetEntitySet<Customer>().CanEdit);
            Assert.IsFalse(ec.GetEntitySet<Order>().CanEdit);

            Customer cust1 = new Customer
            {
                CustomerID = "MATC"
            };
            Customer cust2 = new Customer
            {
                CustomerID = "AMYC"
            };
            Order order1 = new Order
            {
                OrderID = 1,
                CustomerID = "AMYC"
            };
            Order order2 = new Order
            {
                OrderID = 2,
                CustomerID = "AMYC"
            };

            ec.LoadEntities(new Entity[] { cust1, cust2, order1, order2 });

            Customer c1 = order1.Customer;

            NotSupportedException expectedException = null;
            try
            {
                c1.Orders.Remove(order1);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(Resource.EntitySet_UnsupportedOperation, typeof(Order), EntitySetOperations.Edit), expectedException.Message);
        }

        public class Bug479438_Update_NonEditable_Container : EntityContainer
        {
            public Bug479438_Update_NonEditable_Container()
            {
                CreateEntitySet<Customer>(EntitySetOperations.None);
                CreateEntitySet<Order>(EntitySetOperations.None);
            }
        }

        /// <summary>
        /// Verify that when updating an association by reference,
        /// the change is tracked as expected
        /// </summary>
        [TestMethod]
        public void Bug479437_UpdateViaReference()
        {
            NorthwindEntityContainer entities = new NorthwindEntityContainer();

            Customer cust1 = new Customer
            {
                CustomerID = "MATC"
            };
            Customer cust2 = new Customer
            {
                CustomerID = "AMYC"
            };
            Order order1 = new Order
            {
                OrderID = 1,
                CustomerID = "AMYC"
            };
            Order order2 = new Order
            {
                OrderID = 2,
                CustomerID = "AMYC"
            };

            entities.LoadEntities(new Entity[] { cust1, cust2, order1, order2 });

            cust1.City = "FOO";
            order1.Customer = cust1;
            order2.Customer = cust1;

            EntityChangeSet cs = entities.GetChanges();
            Assert.AreEqual(3, cs.ModifiedEntities.Count);
            Assert.IsTrue(cs.ModifiedEntities.Contains(order1));
            Assert.IsTrue(cs.ModifiedEntities.Contains(order2));
        }

        [TestMethod]
        [Asynchronous]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void ReadAndUpdateXElement()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = CreateDomainContext();

            Load(ctxt.GetXElemEntitiesQuery());

            EnqueueConditional(delegate
            {
                return IsLoadComplete;
            });
            EnqueueCallback(delegate
            {
                TestDomainServices.EntityWithXElement entity = ctxt.EntityWithXElements.First(p => p.ID == 1);
                XElement xelem = entity.XElem;
                Assert.IsNotNull(xelem);
                Assert.AreEqual("Who likes to party party", xelem.Value);

                // now update the value
                entity.XElem = XElement.Parse("<FooBar>We likes to party party</FooBar>");
                Assert.IsTrue(ctxt.EntityContainer.GetChanges().ModifiedEntities.Contains(entity));

                SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return IsSubmitComplete;
            });
            EnqueueCallback(delegate
            {
                AssertSuccess();
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void MaskedUpdate()
        {
            // Different strings expected at different points in the round trip
            string originalStateName = "WA";            // Expected from original POCO response
            string clientModifiedStateName = "ZZ";      // Arbitrary string we use to update on client-side
            string updateOpModifiedStateName = "AA";    // Expected result after the masked update completes
            string customOpModifiedStateName = "BB";    // Expected result after the customer operation completes

            TestDomainServices.MockCustomerDomainContext context = new TestDomainServices.MockCustomerDomainContext(TestURIs.MockCustomers);
            context.AddReference(typeof(Cities.City), new Cities.CityDomainContext(TestURIs.Cities));
            TestDomainServices.MockCustomer customer = null;

            SubmitOperation so = null;
            LoadOperation lo = context.Load(context.GetCustomersQuery(), false);

            this.EnqueueConditional(() => lo.IsComplete);
            this.EnqueueCallback(() =>
            {
                customer = context.MockCustomers.First();
                Assert.AreEqual(originalStateName, customer.StateName); // Original state

                customer.StateName = clientModifiedStateName;
                Assert.AreEqual(clientModifiedStateName, customer.StateName); // Client modified state

                context.MockCustomerCustomMethod(customer, updateOpModifiedStateName, originalStateName); // Call a Custom method too
                
                so = context.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueConditional(() => so.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNull(so.Error);
                Assert.IsFalse(so.IsCanceled);

                Assert.AreEqual(0, so.EntitiesInError.Count());
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);

                Assert.AreSame(customer, so.ChangeSet.ModifiedEntities.First());

                Assert.AreEqual(customOpModifiedStateName, customer.StateName); // Server masked state
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void MaskedUpdate_UpdateMethodFailure()
        {
            // Different strings expected at different points in the round trip
            string originalStateName = "WA";            // Expected from original POCO response
            string clientModifiedStateName = "INVALID"; // Arbitrary string we use to update on client-side

            TestDomainServices.MockCustomerDomainContext context = new TestDomainServices.MockCustomerDomainContext(TestURIs.MockCustomers);
            context.AddReference(typeof(Cities.City), new Cities.CityDomainContext(TestURIs.Cities));
            TestDomainServices.MockCustomer customer = null;

            SubmitOperation so = null;
            LoadOperation lo = context.Load(context.GetCustomersQuery(), false);

            this.EnqueueConditional(() => lo.IsComplete);
            this.EnqueueCallback(() =>
            {
                customer = context.MockCustomers.First();
                Assert.AreEqual(originalStateName, customer.StateName); // Original state

                customer.StateName = clientModifiedStateName;
                Assert.AreEqual(clientModifiedStateName, customer.StateName); // Client modified state

                context.MockCustomerCustomMethod(customer, clientModifiedStateName, originalStateName); // Call a Custom method too
                so = context.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueConditional(() => so.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(so.Error);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, so.Error.Message);
                Assert.IsFalse(so.IsCanceled);

                Assert.AreEqual(1, so.EntitiesInError.Count());
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(1, customer.ValidationErrors.Count());

                Assert.AreSame(customer, so.EntitiesInError.First());
                Assert.AreSame(customer, so.ChangeSet.ModifiedEntities.First());

                Assert.AreEqual("Expected state name of length 2", customer.ValidationErrors.First().ErrorMessage);
                Assert.AreEqual(clientModifiedStateName, customer.StateName); // No updates took place
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void MaskedUpdate_CustomMethodFailure()
        {
            // Different strings expected at different points in the round trip
            string originalStateName = "WA";            // Expected from original POCO response
            string clientModifiedStateName = "ZZ";      // Arbitrary string we use to update on client-side

            TestDomainServices.MockCustomerDomainContext context = new TestDomainServices.MockCustomerDomainContext(TestURIs.MockCustomers);
            context.AddReference(typeof(Cities.City), new Cities.CityDomainContext(TestURIs.Cities));
            TestDomainServices.MockCustomer customer = null;

            SubmitOperation so = null;
            LoadOperation lo = context.Load(context.GetCustomersQuery(), false);

            this.EnqueueConditional(() => lo.IsComplete);
            this.EnqueueCallback(() =>
            {
                customer = context.MockCustomers.First();
                Assert.AreEqual(originalStateName, customer.StateName); // Original state

                customer.StateName = clientModifiedStateName;
                Assert.AreEqual(clientModifiedStateName, customer.StateName); // Client modified state

                context.MockCustomerCustomMethod(customer, "EXPECTED_NAME", originalStateName); // Call a Custom method too, triggering a validation exception
                so = context.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueConditional(() => so.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(so.Error);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, so.Error.Message);
                Assert.IsFalse(so.IsCanceled);
                Assert.AreEqual(1, so.EntitiesInError.Count());
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(1, customer.ValidationErrors.Count());

                Assert.AreSame(customer, so.EntitiesInError.First());
                Assert.AreSame(customer, so.ChangeSet.ModifiedEntities.First());

                Assert.AreEqual("Expected state name: 'EXPECTED_NAME'.  Actual: 'AA'.", customer.ValidationErrors.First().ErrorMessage);
                Assert.AreEqual(clientModifiedStateName, customer.StateName); // No updates took place
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void MaskedUpdate_UpdateAndCustomMethodFailure()
        {
            // Different strings expected at different points in the round trip
            string originalStateName = "WA";            // Expected from original POCO response
            string clientModifiedStateName = "INVALID"; // Arbitrary string we use to update on client-side, should trigger an update failure

            TestDomainServices.MockCustomerDomainContext context = new TestDomainServices.MockCustomerDomainContext(TestURIs.MockCustomers);
            context.AddReference(typeof(Cities.City), new Cities.CityDomainContext(TestURIs.Cities));
            TestDomainServices.MockCustomer customer = null;

            SubmitOperation so = null;
            LoadOperation lo = context.Load(context.GetCustomersQuery(), false);

            this.EnqueueConditional(() => lo.IsComplete);
            this.EnqueueCallback(() =>
            {
                customer = context.MockCustomers.First();
                Assert.AreEqual(originalStateName, customer.StateName); // Original state

                customer.StateName = clientModifiedStateName;
                Assert.AreEqual(clientModifiedStateName, customer.StateName); // Client modified state

                context.MockCustomerCustomMethod(customer, "EXPECTED_NAME", originalStateName); // Call a Custom method too, triggering a validation exception
                so = context.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            this.EnqueueConditional(() => so.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(so.Error);
                Assert.AreEqual(Resource.DomainContext_SubmitOperationFailed_Validation, so.Error.Message);
                Assert.IsFalse(so.IsCanceled);
                Assert.AreEqual(1, so.EntitiesInError.Count());
                Assert.AreEqual(0, so.ChangeSet.AddedEntities.Count);
                Assert.AreEqual(0, so.ChangeSet.RemovedEntities.Count);
                Assert.AreEqual(1, so.ChangeSet.ModifiedEntities.Count);
                Assert.AreEqual(2, customer.ValidationErrors.Count());

                Assert.AreSame(customer, so.EntitiesInError.First());
                Assert.AreSame(customer, so.ChangeSet.ModifiedEntities.First());

                Assert.AreEqual("Expected state name of length 2", customer.ValidationErrors.First().ErrorMessage);
                Assert.AreEqual("Expected state name: 'EXPECTED_NAME'.  Actual: 'INVALID'.", customer.ValidationErrors.Last().ErrorMessage);
                Assert.AreEqual(clientModifiedStateName, customer.StateName); // No updates took place
            });
            this.EnqueueTestComplete();
        }
    }
}

namespace TestDomainServices
{
    using System.Runtime.Serialization;

    public partial class A
    {
        private int excludedMember;

        /// <summary>
        /// This member is used in a test to verify that even if the client
        /// sends a value for an excluded member, it is never set.
        /// </summary>
        [DataMember]
        public int ExcludedMember
        {
            get
            {
                return this.excludedMember;
            }
            set
            {
                this.excludedMember = value;
            }
        }
    }
}
