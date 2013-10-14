extern alias SSmDsClient;

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DataTests.Northwind.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices.LTS;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    /// <summary>
    /// ChangeSet computation tests that are independent of actual provider communication
    /// </summary>
    [TestClass]
    public class ChangeSetTests : UnitTestBase
    {
        /// <summary>
        /// Verify that original values sent to the server respect
        /// RoundtripOriginal attribute
        /// </summary>
        [TestMethod]
        public void RoundtripOriginal_VerifyPartialObjects()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            TestDomainServices.RoundtripOriginal_TestEntity entity = new TestDomainServices.RoundtripOriginal_TestEntity { ID = 1, RoundtrippedMember = 1, NonRoundtrippedMember = 1 };
            ctxt.EntityContainer.LoadEntities(new Entity[] { entity });

            // make a change
            entity.NonRoundtrippedMember += 1;
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            TestDomainServices.RoundtripOriginal_TestEntity original = (TestDomainServices.RoundtripOriginal_TestEntity)cs.GetChangeSetEntries().First().OriginalEntity;

            // verify member with roundtrip has an original value
            Assert.AreEqual(1, entity.RoundtrippedMember);

            // verify member without roundtrip doesn't have it's original value set
            Assert.AreEqual(0, original.NonRoundtrippedMember);
        }

        /// <summary>
        /// Verify that original values sent to the server respect
        /// RoundtripOriginal attribute
        /// </summary>
        [TestMethod]
        public void RoundtripOriginalOnClass_VerifyPartialObjects()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            TestDomainServices.RoundtripOriginal_TestEntity2 entity = new TestDomainServices.RoundtripOriginal_TestEntity2 { ID = 1, RoundtrippedMember1 = 1, RoundtrippedMember2 = 1 };
            ctxt.EntityContainer.LoadEntities(new Entity[] { entity });

            entity.RoundtrippedMember1 += 1;
            entity.RoundtrippedMember2 += 2;
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            TestDomainServices.RoundtripOriginal_TestEntity2 original = (TestDomainServices.RoundtripOriginal_TestEntity2)cs.GetChangeSetEntries().First().OriginalEntity;

            // verify members with roundtrip have the original value
            Assert.AreEqual(2, entity.RoundtrippedMember1);
            Assert.AreEqual(3, entity.RoundtrippedMember2);
            Assert.AreEqual(1, original.RoundtrippedMember1);
            Assert.AreEqual(1, original.RoundtrippedMember2);
        }

        [TestMethod]
        public void ChangeSet_DontLoadUnloadedAssociations()
        {
            NorthwindEntityContainer entities = new NorthwindEntityContainer();
            EntitySet<Order> orders = entities.GetEntitySet<Order>();
            EntitySet<Order_Detail> details = entities.GetEntitySet<Order_Detail>();

            // add a few existing entities
            Order order = new Order
            {
                OrderID = 1
            };
            Order_Detail detail = new Order_Detail
            {
                OrderID = 1, ProductID = 1
            };
            entities.LoadEntities(new Entity[] { order, detail });

            // modify both entities
            order.Freight = 5;
            detail.Quantity = 5;

            IEntityRef er = detail.GetEntityRef("Order");
            Assert.IsNull(er);
            IEntityCollection ec = order.Order_Details;
            Assert.IsFalse(ec.HasValues);

            EntityChangeSet cs = entities.GetChanges();
            Assert.AreEqual(2, cs.ModifiedEntities.Count);

            // after computing the changeset, no association members
            // should have been loaded
            er = detail.GetEntityRef("Order");
            Assert.IsNull(er);
            Assert.IsFalse(ec.HasValues);

            // after building the operation list, no association members
            // should have been loaded
            ChangeSetBuilder.Build(cs);
            er = detail.GetEntityRef("Order");
            Assert.IsNull(er);
            Assert.IsFalse(ec.HasValues);
        }

        /// <summary>
        /// Make sure that inferred Attach behavior doesn't prevent users
        /// from doing things manually if they wish
        /// </summary>
        [TestMethod]
        public void AttachAnInferredEntity()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            EntitySet<Order> orders = ec.GetEntitySet<Order>();
            EntitySet<Order_Detail> details = ec.GetEntitySet<Order_Detail>();

            // add a few existing entities
            Order order1 = new Order
            {
                OrderID = 1
            };
            Order_Detail detail1 = new Order_Detail
            {
                Order = order1, ProductID = 1
            };

            // this attaches both entities
            details.Attach(detail1);
            Assert.IsTrue(details.Contains(detail1));
            Assert.IsTrue(orders.Contains(order1));

            // this should work, since the order was inferred
            // it should no-op
            orders.Attach(order1);
        }

        [TestMethod]
        public void Bug619552_DontInferAttachDeletedEntities()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            EntitySet<Order> orders = ec.GetEntitySet<Order>();
            EntitySet<Order_Detail> details = ec.GetEntitySet<Order_Detail>();

            // add a few existing entities
            Order order = new Order
            {
                OrderID = 1
            };
            Order_Detail detail = new Order_Detail
            {
                OrderID = 1, ProductID = 1
            };

            // attach
            details.Attach(detail);
            orders.Attach(order);
            EntityChangeSet cs = ec.GetChanges();
            Assert.IsTrue(cs.IsEmpty);

            // remove
            details.Remove(detail);
            orders.Remove(order);
            cs = ec.GetChanges();
            Assert.IsTrue(cs.AddedEntities.Count == 0 && cs.ModifiedEntities.Count == 0 && cs.RemovedEntities.Count == 2);

            // This line should only add the detail as Unmodified
            // Since both entities are known by the container, they
            // are not infer Added
            details.Add(detail);

            // This line adds the order back as Unmodified
            orders.Add(order);

            cs = ec.GetChanges();
            Assert.IsTrue(cs.IsEmpty);
        }

        [TestMethod]
        public void EntityRef_DontInferAddDeletedEntities()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            EntitySet<Order> orders = ec.GetEntitySet<Order>();
            EntitySet<Order_Detail> details = ec.GetEntitySet<Order_Detail>();

            // add a few existing entities
            Order order = new Order
            {
                OrderID = 1
            };
            Order_Detail detail = new Order_Detail
            {
                OrderID = 1, ProductID = 1
            };
            details.Attach(detail);
            orders.Attach(order);

            // Verify that EntityRef doesn't infer Add deleted
            // entities
            orders.Remove(order);
            detail.Order = order;
            Assert.IsFalse(order.IsInferred);
            Assert.IsFalse(orders.Contains(order));
        }

        [TestMethod]
        public void EntityCollection_DontInferAddDeletedEntities()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            EntitySet<Order> orders = ec.GetEntitySet<Order>();
            EntitySet<Order_Detail> details = ec.GetEntitySet<Order_Detail>();

            // add a few existing entities
            Order order = new Order
            {
                OrderID = 1
            };
            Order_Detail detail = new Order_Detail
            {
                OrderID = 1, ProductID = 1
            };
            details.Attach(detail);
            orders.Attach(order);

            // Verify that EntityCollections don't infer Add deleted
            // entities
            details.Remove(detail);
            order.Order_Details.Add(detail);
            Assert.IsFalse(detail.IsInferred);
            Assert.IsFalse(details.Contains(detail));
        }

        [TestMethod]
        public void Bug635474_IsSubmittingStateManagement()
        {
            Northwind nw = new Northwind(TestURIs.LTS_Northwind);
            Product prod = new Product
            {
                ProductID = 1,
                ProductName = "Tasty O's"
            };
            nw.Products.Attach(prod);

            // verify that IsSubmitting is reset when the submit
            // is cancelled
            prod.ProductName += "x";
            SubmitOperation so = nw.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            so.Cancel();
            Assert.IsFalse(nw.IsSubmitting);
        }

        /// <summary>
        /// Verify that any entities added to an EntityCollection that haven't been explicitly
        /// attached to an EntitySet are inferred as new entities and Added to their entity
        /// set immediately
        /// </summary>
        [TestMethod]
        public void InferredAdd_EntityCollection()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();

            // add a few existing entities
            Order order1 = new Order
            {
                OrderID = 1
            };
            Order order2 = new Order
            {
                OrderID = 2
            };
            Order_Detail detail1 = new Order_Detail
            {
                OrderID = 2, ProductID = 1
            };
            ec.LoadEntities(new Entity[] { order1, order2, detail1 });
            ((IChangeTracking)ec).AcceptChanges();
            Assert.IsFalse(ec.HasChanges);

            // build a detached graph of a new category and 2 products,
            // referenced by a detail
            Order_Detail newDetail1 = new Order_Detail
            {
                OrderID = 1, ProductID = 2
            };
            Category newCategory = new Category
            {
                CategoryID = 1
            };
            Product newProduct1 = new Product
            {
                ProductID = 3
            };
            Product newProduct2 = new Product
            {
                ProductID = 4
            };
            newCategory.Products.Add(newProduct1);
            newCategory.Products.Add(newProduct2);
            newDetail1.Product = newProduct1;

            EntityChangeSet cs = ec.GetChanges();
            Assert.IsTrue(cs.IsEmpty);

            // verify that adding an unattached entity to an EC results in
            // the expected inferred Adds
            order1.Order_Details.Add(newDetail1);
            cs = ec.GetChanges();
            Assert.AreEqual(4, cs.AddedEntities.Count);
            Assert.IsTrue(cs.AddedEntities.Contains(newDetail1));  // the entity added directly
            Assert.IsTrue(cs.AddedEntities.Contains(newProduct1)); // inferred via Detail.Product ER
            Assert.IsTrue(cs.AddedEntities.Contains(newCategory)); // inferred via Product.Category ER
            Assert.IsTrue(cs.AddedEntities.Contains(newProduct2)); // inferred via Category.Products EC

            // verify that inferred Adds can be state transitioned via subsequent
            // calls to Attach
            ec.GetEntitySet<Product>().Attach(newProduct2);
            newProduct2.ProductName += "x";
            cs = ec.GetChanges();
            Assert.AreEqual(3, cs.AddedEntities.Count);
            Assert.AreEqual(1, cs.ModifiedEntities.Count);
            Assert.IsFalse(cs.AddedEntities.Contains(newProduct2));
            Assert.IsTrue(cs.ModifiedEntities.Contains(newProduct2));
        }

        /// <summary>
        /// Verify that if the entity referenced by an EntityRef hasn't been explicitly
        /// attached to an EntitySet it is inferred as a new entity and is added to it's
        /// entity set immediately
        /// </summary>
        [TestMethod]
        public void InferredAdd_EntityRefs()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();

            // add a few existing entities
            Order order1 = new Order
            {
                OrderID = 1
            };
            Order_Detail detail1 = new Order_Detail
            {
                OrderID = 2, ProductID = 1
            };
            ec.LoadEntities(new Entity[] { order1, detail1 });
            ((IChangeTracking)ec).AcceptChanges();
            Assert.IsFalse(ec.HasChanges);

            // build a detached graph of a new category and 2 products
            Category newCategory = new Category
            {
                CategoryID = 1
            };
            Product newProduct1 = new Product
            {
                ProductID = 3
            };
            Product newProduct2 = new Product
            {
                ProductID = 4
            };
            newCategory.Products.Add(newProduct1);
            newCategory.Products.Add(newProduct2);

            // set the the Product reference on the existing detail to
            // one of the new detached products - we expect the entire
            // graph to be infer added
            EntityChangeSet cs = ec.GetChanges();
            Assert.IsTrue(cs.IsEmpty);
            detail1.Product = newProduct1;
            cs = ec.GetChanges();
            Assert.AreEqual(3, cs.AddedEntities.Count);
            Assert.IsTrue(cs.AddedEntities.Contains(newProduct1)); // the entity set directly
            Assert.IsTrue(cs.AddedEntities.Contains(newCategory)); // inferred via Product.Category ER
            Assert.IsTrue(cs.AddedEntities.Contains(newProduct2)); // inferred via Category.Products EC

            // verify that inferred Adds can be state transitioned via subsequent
            // calls to Attach
            ec.GetEntitySet<Product>().Attach(newProduct2);
            newProduct2.ProductName += "x";
            cs = ec.GetChanges();
            Assert.AreEqual(2, cs.AddedEntities.Count);
            Assert.AreEqual(2, cs.ModifiedEntities.Count);
            Assert.IsFalse(cs.AddedEntities.Contains(newProduct2));
            Assert.IsTrue(cs.ModifiedEntities.Contains(newProduct2));
        }

        /// <summary>
        /// Verify that EntitySet.Add is recursive, and that all unattached entities reacheable
        /// from the root are infer Added. Also verify that entities that were inferred can
        /// be transitioned via subsequent calls to Attach
        /// </summary>
        [TestMethod]
        public void InferredAdd_RecursiveOnAdd()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            EntitySet<Order> ordersSet = ec.GetEntitySet<Order>();

            Order order = new Order
            {
                OrderID = 1
            };
            Order_Detail detail1 = new Order_Detail
            {
                OrderID = 1
            };
            Order_Detail detail2 = new Order_Detail
            {
                OrderID = 1
            };
            Product product1 = new Product
            {
                ProductID = 1
            };
            Product product2 = new Product
            {
                ProductID = 2
            };
            order.Order_Details.Add(detail1);
            order.Order_Details.Add(detail2);
            detail1.Product = product1;
            detail2.Product = product2;

            // when we add, we expect all reachable unattached entities
            // to be infer added
            ordersSet.Add(order);
            EntityChangeSet cs = ec.GetChanges();
            Assert.AreEqual(5, cs.AddedEntities.Count);
            Assert.IsTrue(cs.AddedEntities.Contains(order));
            Assert.IsTrue(cs.AddedEntities.Contains(detail1));
            Assert.IsTrue(cs.AddedEntities.Contains(detail2));
            Assert.IsTrue(cs.AddedEntities.Contains(product1));
            Assert.IsTrue(cs.AddedEntities.Contains(product2));

            // the root entity wasn't infer added, so it can't be Attached
            InvalidOperationException expectedException = null;
            try
            {
                ordersSet.Attach(order);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(Resource.EntitySet_EntityAlreadyAttached, expectedException.Message);

            // entities that were infer Added can be Attached
            ec.GetEntitySet<Product>().Attach(product1);
            product1.ProductName += "x";
            cs = ec.GetChanges();
            Assert.AreEqual(4, cs.AddedEntities.Count);
            Assert.IsFalse(cs.AddedEntities.Contains(product1));
            Assert.IsTrue(cs.ModifiedEntities.Contains(product1));

            // verify that after an inferred Add has been Attached, it can't be
            // reattached
            expectedException = null;
            try
            {
                ec.GetEntitySet<Product>().Attach(product1);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(Resource.EntitySet_DuplicateIdentity, expectedException.Message);

            // verify that when changes are accepted, all Inferred state
            // is reset for entities
            cs = ec.GetChanges();
            IEnumerable<Entity> entities = cs.AddedEntities.Concat(cs.ModifiedEntities).Concat(cs.RemovedEntities);
            Assert.AreEqual(3, entities.Count(p => p.IsInferred));
            ((IChangeTracking)ec).AcceptChanges();
            Assert.IsFalse(entities.Any(p => p.IsInferred));
        }

        /// <summary>
        /// Verify that EntitySet.Attach is recursive, and that all unattached entities reacheable
        /// from the root are infer Added. Also verify that entities that were inferred can
        /// be transitioned via subsequent calls to Add/Remove
        /// </summary>
        [TestMethod]
        public void InferredAdd_RecursiveOnAttach()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            EntitySet<Order> ordersSet = ec.GetEntitySet<Order>();

            Order order = new Order
            {
                OrderID = 1
            };
            Order_Detail detail1 = new Order_Detail
            {
                OrderID = 1
            };
            Order_Detail detail2 = new Order_Detail
            {
                OrderID = 1
            };
            Product product1 = new Product
            {
                ProductID = 1
            };
            Product product2 = new Product
            {
                ProductID = 2
            };
            order.Order_Details.Add(detail1);
            order.Order_Details.Add(detail2);
            detail1.Product = product1;
            detail2.Product = product2;

            // when we attach, we expect all reachable unattached entities
            // to be attached
            ordersSet.Attach(order);
            Assert.IsTrue(ec.GetChanges().IsEmpty);
            Assert.IsTrue(ec.GetEntitySet<Order>().Contains(order));
            Assert.IsTrue(ec.GetEntitySet<Order_Detail>().Contains(detail1));
            Assert.IsTrue(ec.GetEntitySet<Order_Detail>().Contains(detail2));
            Assert.IsTrue(ec.GetEntitySet<Product>().Contains(product1));
            Assert.IsTrue(ec.GetEntitySet<Product>().Contains(product2));

            // All attached entities (including the root) can subsequently be transitioned by
            // calling Add/Remove. After the transition, they are no longer "inferred" and cannot
            // be transitioned again
            Assert.IsTrue(product2.IsInferred);
            ec.GetEntitySet<Product>().Remove(product2);
            Assert.IsFalse(product2.IsInferred);
            ec.GetEntitySet<Product>().Add(product2);  // this undoes the remove, making it Unmodified again
            Assert.AreEqual(EntityState.Unmodified, product2.EntityState);

            Assert.IsTrue(product1.IsInferred);
            ec.GetEntitySet<Product>().Add(product1);
            Assert.IsFalse(product1.IsInferred);

            ec.GetEntitySet<Order>().Remove(order);
        }

        /// <summary>
        /// Verify that when entities are detached or removed, they are removed
        /// from the ID cache
        /// </summary>
        [TestMethod]
        public void IdentityCacheCleanup()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            ec.LoadEntities(new Entity[] { new Product { ProductID = 1 }, new Product { ProductID = 2 }, new Product { ProductID = 3 } });
            EntitySet<Product> productSet = ec.GetEntitySet<Product>();

            // Delete an entity
            Product prod = productSet.First();
            int key = prod.ProductID;
            productSet.Remove(prod);
            ((IChangeTracking)ec).AcceptChanges();
            // After the delete, the entity is moved back into the default state
            Assert.AreEqual(EntityState.Detached, prod.EntityState);

            // After it has been deleted, we should be able to add
            // a new entity with the same key
            Product newProduct = new Product
            {
                ProductID = key
            };
            productSet.Add(newProduct);
            Assert.AreEqual(EntityState.New, newProduct.EntityState);
            ((IChangeTracking)ec).AcceptChanges();
            Product requeriedProduct = productSet.Single(p => p.ProductID == key);
            Assert.AreSame(newProduct, requeriedProduct);  // make sure instances are same

            // Bug 526544 repro case - delete and submit, then attempt
            // to re-add
            prod = productSet.First();
            key = prod.ProductID;
            productSet.Remove(prod);
            ((IChangeTracking)ec).AcceptChanges();
            productSet.Add(prod);
            Assert.AreEqual(EntityState.New, prod.EntityState);
            ((IChangeTracking)ec).AcceptChanges();

            // verify that when an entity is Detached, it is removed from ID cache
            // after the detach, attaching an entity with the same key should succeed
            prod = productSet.First();
            key = prod.ProductID;
            productSet.Detach(prod);
            newProduct = new Product
            {
                ProductID = key
            };
            productSet.Attach(newProduct);
            requeriedProduct = productSet.Single(p => p.ProductID == key);
            Assert.AreSame(newProduct, requeriedProduct);  // make sure instances are same
        }

        [TestMethod]
        public void UncommitedEntityEdits()
        {
            Northwind ctxt = new Northwind(TestURIs.LTS_Northwind);
            Product prod = new Product
            {
                ProductID = 1,
                ProductName = "Cheezy Tots"
            };
            ctxt.EntityContainer.LoadEntities(new Entity[] { prod });

            // start an edit session and calculate
            // changes w/o ending the session
            IEditableObject eo = (IEditableObject)prod;
            eo.BeginEdit();
            prod.ProductName = "Chikn Crisps";
            Assert.IsTrue(prod.HasChanges);
            Assert.IsTrue(prod.IsEditing);
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Assert.AreEqual(1, cs.ModifiedEntities.Count);

            // however, attempting to call submit will result in
            // an exception
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            }, string.Format(Resource.Entity_UncommittedChanges, prod));

            // end the session
            eo.EndEdit();
            Assert.IsFalse(prod.IsEditing);
            cs = ctxt.EntityContainer.GetChanges();
            Assert.AreEqual(1, cs.ModifiedEntities.Count);
        }

        [TestMethod]
        public void TestAssociations_GraphDelete()
        {
            NorthwindEntityContainer entities = new NorthwindEntityContainer();

            #region Create a test graph
            Customer cust = new Customer
            {
                CustomerID = "ALFKI"
            };
            Order order = new Order
            {
                OrderID = 1
            };
            order.Customer = cust;
            order.Order_Details.Add(new Order_Detail
            {
                ProductID = 1
            });
            order.Order_Details.Add(new Order_Detail
            {
                ProductID = 2
            });
            order.Order_Details.Add(new Order_Detail
            {
                ProductID = 3
            });

            entities.LoadEntities(new Entity[] { cust, order });
            entities.LoadEntities(order.Order_Details);
            ((IRevertibleChangeTracking)entities).AcceptChanges();
            #endregion

            // now delete the graph
            // TODO : currently this has to be done in this specific order
            // with association modifications being done while the parent
            // is attached (before it is removed from set)
            foreach (Order_Detail detail in order.Order_Details)
            {
                order.Order_Details.Remove(detail);
                entities.GetEntitySet<Order_Detail>().Remove(detail);
            }
            cust.Orders.Remove(order);
            entities.GetEntitySet<Order>().Remove(order);
            entities.GetEntitySet<Customer>().Remove(cust);

            // verify the changeset
            EntityChangeSet changeSet = entities.GetChanges();
            Assert.AreEqual(5, changeSet.RemovedEntities.Count);

            // build the operation list and verify it
            List<ChangeSetEntry> operations = ChangeSetBuilder.Build(changeSet);

            // verify that the association collections for the Order operation are null
            ChangeSetEntry orderOperation = operations.Single(p => p.Entity == order);
            ChangeSetEntry custOperation = operations.Single(p => p.Entity == cust);
            Assert.IsNull(orderOperation.Associations);
            Assert.IsNull(orderOperation.OriginalAssociations);
            Assert.IsNotNull(orderOperation.OriginalEntity);
            
            // verify that the association collections for the Customer operation are null
            Assert.IsNull(custOperation.OriginalEntity);
            Assert.IsNull(custOperation.Associations);
            Assert.IsNull(custOperation.OriginalAssociations);

            // verify that deleted OrderDetails have null associations as well
            ChangeSetEntry detailOperation = operations.First(p => p.Entity.GetType() == typeof(Order_Detail));
            Assert.IsNotNull(detailOperation.OriginalEntity);
            Assert.IsNull(detailOperation.Associations);
            Assert.IsNull(detailOperation.OriginalAssociations);
        }

        /// <summary>
        /// Verify that associations to new entities are sent correctly to the
        /// server.
        /// This bug was a regression from the fx changes made for bug 898909.
        /// </summary>
        [TestMethod]
        [WorkItem(188476)]
        public void TestAssociations_UpdatedReferencingNew()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            Product p1 = new Product { ProductID = 1, CategoryID = 1 };
            Product p2 = new Product { ProductID = 2, CategoryID = 2 };
            Category c1 = new Category { CategoryID = 1 };
            Category c2 = new Category { CategoryID = 2 };
            ec.LoadEntities(new Entity[] { p1, p2, c1, c2 });

            // take two existing parents (the FK side of the association)
            // access their existing children
            Category prevCat = p1.Category;
            Assert.IsNotNull(prevCat);
            prevCat = p2.Category;
            Assert.IsNotNull(prevCat);

            // create two new children
            Category newCat1 = new Category { CategoryID = 3 };
            Category newCat2 = new Category { CategoryID = 4 };

            // assign the two new children
            p1.Category = newCat1;
            p2.Category = newCat2;

            EntityChangeSet cs = ec.GetChanges();
            Assert.AreEqual(2, cs.AddedEntities.Count);
            Assert.AreEqual(2, cs.ModifiedEntities.Count);

            List<ChangeSetEntry> entries = ChangeSetBuilder.Build(cs);
            ChangeSetEntry entry = entries.Single(p => p.Entity == p1);

            // the bug was that we weren't populating the association map in this
            // scenario since previously we required BOTH parent and child to be new.
            // We've relaxed that to ensure that if the child is new, the association
            // shows up in the map.
            Assert.IsNotNull(entry.Associations);
            int[] ids = entry.Associations["Category"];
            Category referenced = (Category)entries.Single(p => p.Id == ids.Single()).Entity;
            Assert.AreSame(newCat1, referenced);
        }

        [TestMethod]
        [Asynchronous]
        public void EmptyChangeSet_SubmitChanges()
        {
            Northwind ctxt = new Northwind(TestURIs.LTS_Northwind_CUD);
            
            SubmitOperation so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);
                Assert.IsNull(so.Error);
                Assert.IsTrue(so.ChangeSet.IsEmpty);
            });

            EnqueueTestComplete();
        }
    }

    public class NorthwindEntityContainer : EntityContainer
    {
        public NorthwindEntityContainer()
        {
            this.CreateEntitySet<DataTests.Northwind.LTS.Product>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Order>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Order_Detail>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Customer>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Category>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
        }
    }
}
