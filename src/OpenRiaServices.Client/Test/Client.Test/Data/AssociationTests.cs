using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DataTests.AdventureWorks.LTS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;

namespace OpenRiaServices.DomainServices.Client.Test
{
    public class CatalogEntityContainer : EntityContainer {
        public CatalogEntityContainer() {
            CreateEntitySet<Product>(EntitySetOperations.Add|EntitySetOperations.Edit|EntitySetOperations.Remove);
            CreateEntitySet<PurchaseOrder>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<PurchaseOrderDetail>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<Employee>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
        }
    }

    [TestClass]
    public class AssociationTests : UnitTestBase
    {
        private PurchaseOrder TestOrder;
        private List<PurchaseOrderDetail> TestDetails;
        private int NumNotifications = 0;
        private int purchaseOrderDetailIDSequence = 1;

        [TestInitialize]
        public void TestSetup() {
            TestOrder = new PurchaseOrder
            {
                PurchaseOrderID = 1
            };

            TestDetails = new List<PurchaseOrderDetail> {
                new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() },
                new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() },
                new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() },
                new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() },
                new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() },
            };
        }

        private int GetUniquePurchaseOrderID() {
            return purchaseOrderDetailIDSequence++;
        }

        internal sealed class ScenariosEntityTestContainer : EntityContainer
        {
            public ScenariosEntityTestContainer()
            {
                this.CreateEntitySet<A>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                this.CreateEntitySet<C>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                this.CreateEntitySet<D>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            }
        }

        /// <summary>
        /// Repro test for Bug #588085, which was a stack overflow caused by infinite
        /// recursion in the generated EntityRef association code.
        /// </summary>
        [TestMethod]
        public void TestAssociation_OneToOne()
        {
            ScenariosEntityTestContainer ec = new ScenariosEntityTestContainer();

            // set up association, C pointing to D
            D d1 = new D
            {
                ID = 1
            };
            C c1 = new C
            {
                ID = 1,
                DID_Ref1 = 1
            };
            ec.GetEntitySet<D>().Attach(d1);
            ec.GetEntitySet<C>().Attach(c1);

            // now point another C at the above D that is already
            // part of an association
            C c2 = new C
            {
                ID = 2
            };

            // this line was causing the recursion
            c2.D_Ref1 = d1;

            Assert.AreSame(d1, c2.D_Ref1);
            Assert.AreEqual(d1.ID, c2.DID_Ref1);
        }

        [TestMethod]
        [WorkItem(591588)]
        public void Bug591588_TestAssociation_OneToOne()
        {
            ScenariosEntityTestContainer ec = new ScenariosEntityTestContainer();

            // set up 2 associations
            D d1 = new D { ID = 1 };
            C c1 = new C { ID = 1, DID_Ref1 = 1 };

            D d2 = new D { ID = 2 };
            C c2= new C { ID = 2, DID_Ref1 = 2 };
            ec.GetEntitySet<D>().Attach(d1);
            ec.GetEntitySet<D>().Attach(d2);
            ec.GetEntitySet<C>().Attach(c1);
            ec.GetEntitySet<C>().Attach(c2);

            Assert.AreSame(d1.C, c1);
            Assert.AreSame(d2.C, c2);

            ec.GetEntitySet<C>().Remove(c1);
            d1.C = d2.C;

            Assert.AreSame(d1.C, c2);
            Assert.IsNull(d2.C);
        }

        [TestMethod]
        public void TestEntityCollectionCaching()
        {
            CatalogEntityContainer container = new CatalogEntityContainer();

            PurchaseOrder order = new PurchaseOrder
            {
                PurchaseOrderID = 1
            };
            PurchaseOrderDetail detail1 = new PurchaseOrderDetail 
            { 
                PurchaseOrderID = 1, PurchaseOrderDetailID = 1 
            };
            PurchaseOrderDetail detail2 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1, PurchaseOrderDetailID = 2
            };
            container.LoadEntities(new Entity[] { order, detail1, detail2 });

            Assert.AreEqual(2, order.PurchaseOrderDetails.Count);

            // now add a couple new details
            PurchaseOrderDetail detail3 = new PurchaseOrderDetail();
            PurchaseOrderDetail detail4 = new PurchaseOrderDetail();
            order.PurchaseOrderDetails.Add(detail3);
            order.PurchaseOrderDetails.Add(detail4);

            Assert.AreEqual(4, order.PurchaseOrderDetails.Count);
            
            // now modify the parent FK, which will cause the cached
            // results to be reset, but we expect the explicitly added
            // entities to be retained
            // Here we're using ApplyState to allow us to set a PK member w/o validation failure
            // since PK members cannot be changed. This test should really be based on an association
            // not involving PK, but the test is still valid this way.
            order.ApplyState(new Dictionary<string, object> { { "PurchaseOrderID", 2 } });
            Assert.AreEqual(2, order.PurchaseOrderDetails.Count);
            Assert.IsTrue(order.PurchaseOrderDetails.Contains(detail3));
            Assert.IsTrue(order.PurchaseOrderDetails.Contains(detail4));
        }

        [TestMethod]
        public void TestEntityRefCaching() {
            CatalogEntityContainer container = new CatalogEntityContainer();

            PurchaseOrderDetail detail = new PurchaseOrderDetail {
                PurchaseOrderDetailID = 1,
                PurchaseOrderID = 1
            };
            PurchaseOrder order = new PurchaseOrder {
                PurchaseOrderID = 1
            };
            PurchaseOrder order2 = new PurchaseOrder {
                PurchaseOrderID = 2
            };

            container.LoadEntities(new Entity[] { order, order2});
            container.LoadEntities(new Entity[] { detail });

            // force the EntityRef to cache
            Assert.AreSame(order, detail.PurchaseOrder);

            // clear the entity set to verify that the cached
            // entity is cleared
            EntitySet purchaseOrderSet = container.GetEntitySet<PurchaseOrder>();
            purchaseOrderSet.Clear();
            Assert.AreEqual(0, purchaseOrderSet.Count);

            // after the set has been cleared, we expect null
            Assert.IsNull(detail.PurchaseOrder);

            // change the FK and verify that we requery again, getting no match
            // since all orders have been cleared from the set
            detail.PurchaseOrderID = 2;
            Assert.AreSame(null, detail.PurchaseOrder);

            // Reload the order entities and verify we get the
            // correct order
            container.LoadEntities(new Entity[] { order, order2 });
            Assert.AreSame(order2, detail.PurchaseOrder);

            // reset the FK and verify that we requery to get the
            // right entity
            detail.PurchaseOrderID = 1;
            Assert.AreSame(order, detail.PurchaseOrder);
        }

        [TestMethod]
        public void TestEntityCaching_NewEntities()
        {
            CatalogEntityContainer container = new CatalogEntityContainer();

            // add two orders and a detail
            PurchaseOrder order1 = new PurchaseOrder();
            PurchaseOrder order2 = new PurchaseOrder();
            PurchaseOrderDetail detail = new PurchaseOrderDetail();
            container.GetEntitySet<PurchaseOrder>().Add(order1);
            container.GetEntitySet<PurchaseOrder>().Add(order2);
            container.GetEntitySet<PurchaseOrderDetail>().Add(detail);

            // examine the order ref of the detail - ensure that
            // no result is returned, since a FK query would match
            // BOTH orders
            Assert.IsNull(detail.PurchaseOrder);

            // now that we've cached a null, make sure that if more
            // new entities are added, the ref doesn't change
            container.GetEntitySet<PurchaseOrder>().Add(new PurchaseOrder());
            Assert.IsNull(detail.PurchaseOrder);

            // now assign order1, and remove order2 - make sure that our
            // ref to order1 remains
            detail.PurchaseOrder = order1;
            Assert.AreSame(order1, detail.PurchaseOrder);
            container.GetEntitySet<PurchaseOrder>().Remove(order2);
            Assert.AreSame(order1, detail.PurchaseOrder);

            container.GetEntitySet<PurchaseOrder>().Remove(order1);
            Assert.IsNull(detail.PurchaseOrder);
        }

        [TestMethod]
        public void TestEntityRefCaching_Detach()
        {
            CatalogEntityContainer container = new CatalogEntityContainer();

            PurchaseOrderDetail detail = new PurchaseOrderDetail
            {
                PurchaseOrderDetailID = 1,
                PurchaseOrderID = 1
            };
            PurchaseOrder order = new PurchaseOrder
            {
                PurchaseOrderID = 1
            };

            container.LoadEntities(new Entity[] { order, detail });

            Assert.AreSame(order, detail.PurchaseOrder);

            // now detach the detail and verify that the
            // cached entity is still returned
            container.GetEntitySet<PurchaseOrderDetail>().Detach(detail);
            Assert.AreSame(order, detail.PurchaseOrder);
        }

        [TestMethod]
        public void EntityRefCaching_MultipartKeys()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            B b = new B
            {
                ID1 = 1, ID2 = 2
            };
            A a = new A { ID = 1 };

            ctxt.EntityContainer.LoadEntities(new Entity[] { a, b });

            int propChangeCount = 0;
            a.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "B")
                {
                    propChangeCount++;
                }
            };

            a.B = b;
            Assert.AreSame(b, a.B);
            Assert.AreEqual(2, propChangeCount);

            // if we set the FK member directly, we expect the
            // cached reference to be reset
            a.BID2 = 0;
            Assert.IsNull(a.B);
            Assert.AreEqual(3, propChangeCount);

            // if we set all values of the multipart
            // key back to valid values we expect to
            // get the valid entity
            a.BID1 = 1;
            a.BID2 = 2;
            Assert.AreSame(b, a.B);
            Assert.AreEqual(4, propChangeCount);
        }

        [TestMethod]
        public void TestCollectionQuery_DetachedEntity() {
            CatalogEntityContainer ec = new CatalogEntityContainer();

            // with the order not part of any EntityContainer/Set,
            // its collection returns empty
            Assert.IsNull(TestOrder.EntitySet);
            Assert.AreEqual(0, TestOrder.PurchaseOrderDetails.Count());

            ((INotifyCollectionChanged)TestOrder.PurchaseOrderDetails).CollectionChanged -= EntityCollectionChanged;
        }

        [TestMethod]
        public void TestCollectionQuery_SubscribeBeforeAttach() {
            CatalogEntityContainer ec = new CatalogEntityContainer();
            NumNotifications = 0;

            // here we subscribe to the event BEFORE the entity is added
            // to the container
            ((INotifyCollectionChanged)TestOrder.PurchaseOrderDetails).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e) {
                NumNotifications++;
            };

            ec.LoadEntities(new PurchaseOrder[] { TestOrder });

            TestNotifications(ec);
        }

        [TestMethod]
        public void TestCollectionQuery_SubscribeAfterAttach() {
            CatalogEntityContainer ec = new CatalogEntityContainer();
            NumNotifications = 0;

            ec.LoadEntities(new PurchaseOrder[] { TestOrder });

            // here we subscribe to the event AFTER the entity is added
            // to the container
            ((INotifyCollectionChanged)TestOrder.PurchaseOrderDetails).CollectionChanged += new NotifyCollectionChangedEventHandler(EntityCollectionChanged);

            TestNotifications(ec);
        }

        private void EntityCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            NumNotifications++;
        }

        [TestMethod]
        public void TestCollectionQuery_DetachParent() {
            CatalogEntityContainer ec = new CatalogEntityContainer();
            NumNotifications = 0;

            ec.LoadEntities(new PurchaseOrder[] { TestOrder });

            ((INotifyCollectionChanged)TestOrder.PurchaseOrderDetails).CollectionChanged += new NotifyCollectionChangedEventHandler(EntityCollectionChanged);

            Assert.AreEqual(0, TestOrder.PurchaseOrderDetails.Count);

            // load a detail and verify we are notified
            ec.LoadEntities(new PurchaseOrderDetail[] { new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() } });
            Assert.AreEqual(1, NumNotifications);
            
            // detach the parent entity and verify we no longer receive notifications
            NumNotifications = 0;
            TestOrder.EntitySet = null;
            ec.LoadEntities(new PurchaseOrderDetail[] { new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() } });
            Assert.AreEqual(0, NumNotifications);
        }

        private void TestNotifications(CatalogEntityContainer ec) {
            // with only the order in the container
            // its collection returns empty
            Assert.IsNotNull(TestOrder.EntitySet);
            Assert.AreEqual(0, TestOrder.PurchaseOrderDetails.Count());
            Assert.AreEqual(0, NumNotifications);

            // after we load some entities, we expect a change notification
            ec.LoadEntities(TestDetails);
            Assert.AreEqual(TestDetails.Count, NumNotifications);
            Assert.IsTrue(TestDetails.SequenceEqual(TestOrder.PurchaseOrderDetails));

            // now add an entity that doesn't match the predicate and
            // verify we don't get notified
            NumNotifications = 0;
            ec.LoadEntities(new PurchaseOrderDetail[] { new PurchaseOrderDetail { PurchaseOrderID = 9, PurchaseOrderDetailID = GetUniquePurchaseOrderID() } });
            Assert.AreEqual(0, NumNotifications);

            // now load one matching and verify we get notified
            ec.LoadEntities(new PurchaseOrderDetail[] { new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() } });
            Assert.AreEqual(1, NumNotifications);

            // verify we get notified if the set is cleared
            NumNotifications = 0;
            EntitySet<PurchaseOrderDetail> entitySet = ec.GetEntitySet<PurchaseOrderDetail>();
            entitySet.Clear();
            Assert.AreEqual(1, NumNotifications);

            // verify that we can reuse the set and continue getting notifications
            NumNotifications = 0;
            ec.LoadEntities(new PurchaseOrderDetail[] { new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = GetUniquePurchaseOrderID() } });
            Assert.AreEqual(1, NumNotifications);
        }
    }
}