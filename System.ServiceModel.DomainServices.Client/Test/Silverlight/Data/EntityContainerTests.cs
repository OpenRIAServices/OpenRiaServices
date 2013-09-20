extern alias SSmDsClient;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

#if !SILVERLIGHT
using System.Reflection;
#endif

namespace System.ServiceModel.DomainServices.Client.Test
{
    using Cities;
    using Resource = SSmDsClient::System.ServiceModel.DomainServices.Client.Resource;

    [TestClass]
    public class EntityContainerTests : UnitTestBase
    {
        [TestInitialize]
        public void TestSetup()
        {
            // Make sure all entities are detached
            foreach (Entity entity in BaselineTestData.Products)
            {
                entity.EntitySet = null;
            }
        }

        [TestMethod]
        [WorkItem(198501)]
        public void EntityContainer_InferredAddThrows()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<PurchaseOrder> orders = ec.AddEntitySet<PurchaseOrder>(EntitySetOperations.All);
            EntitySet<PurchaseOrderDetail> details = ec.AddEntitySet<PurchaseOrderDetail>(EntitySetOperations.None);

            PurchaseOrder order = new PurchaseOrder { PurchaseOrderID = 1 };
            ec.LoadEntities(new Entity[] { order });

            PurchaseOrderDetail detail = new PurchaseOrderDetail { PurchaseOrderDetailID = 1 };

            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                order.PurchaseOrderDetails.Add(detail);
            }, string.Format(CultureInfo.CurrentCulture, Resource.EntitySet_UnsupportedOperation, typeof(PurchaseOrderDetail), EntitySetOperations.Add));

            Assert.IsFalse(order.PurchaseOrderDetails.Contains(detail));
            Assert.AreEqual(EntityState.Detached, detail.EntityState);
        }

        /// <summary>
        /// This test verifies that a removed entity can be successfully
        /// detached from an entity set.
        /// </summary>
        [TestMethod]
        [WorkItem(193429)]
        public void EntitySet_DetachedDeletedEntity()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            var custs = ec.GetEntitySet<DataTests.Northwind.LTS.Customer>();
            var orders = ec.GetEntitySet<DataTests.Northwind.LTS.Order>();

            DataTests.Northwind.LTS.Customer cust = new DataTests.Northwind.LTS.Customer { CustomerID = "ALFKI" };
            ec.LoadEntities(new Entity[] { cust });

            custs.Remove(cust);

            EntityChangeSet cs = ec.GetChanges();
            Assert.AreEqual(1, cs.RemovedEntities.Count);

            bool changeEventRaised = false;
            ((INotifyCollectionChanged)custs).CollectionChanged += (s, e) =>
                {
                    // we don't expect a change event to be raised for the detach
                    changeEventRaised = true;
                };

            // Here is the repro - previously this would throw, because the entity
            // is no longer in the list (having been removed)
            custs.Detach(cust);

            Assert.IsFalse(custs.Contains(cust));
            Assert.IsNull(cust.EntitySet);
            Assert.IsNull(cust.LastSet);
            cs = ec.GetChanges();
            Assert.AreEqual(0, cs.RemovedEntities.Count);
            Assert.IsFalse(changeEventRaised);
        }

        /// <summary>
        /// V1 behavior for EntityRef/EntitCollection was to reset cached associations ANY
        /// time the FKs governing the association were updated. In SP1 we attempted to make
        /// some optimizations, but those optimizations lead to incorrect results in some cases.
        /// We've removed those optimizations and reverted to V1 behavior in this regard.
        /// </summary>
        [TestMethod]
        [WorkItem(193577)]
        [WorkItem(188727)]
        public void EntityAssociationUpdates_FKUpdatesOnNewEntities()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            var orders = ec.GetEntitySet<DataTests.Northwind.LTS.Order>();

            // start with an existing customer in cache
            DataTests.Northwind.LTS.Customer cust = new DataTests.Northwind.LTS.Customer { CustomerID = "ALFKI" };
            ec.LoadEntities(new Entity[] { cust });

            // create a new order w/o specifying customer ID
            DataTests.Northwind.LTS.Order order = new DataTests.Northwind.LTS.Order();
            orders.Add(order);

            // force the reference to cache and verify it is null
            Assert.AreEqual(null, order.Customer);

            bool receivedChangeNotification = false;
            order.PropertyChanged += (s,e) =>
            {
                if (e.PropertyName == "Customer")
                {
                    receivedChangeNotification = true;
                }
            };

            // simulate an accept changes merging in server state
            DataTests.Northwind.LTS.Order serverOrder = new DataTests.Northwind.LTS.Order() { OrderID = 1, CustomerID = "ALFKI" };
            order.Merge(serverOrder, LoadBehavior.RefreshCurrent);

            ((IChangeTracking)order).AcceptChanges();

            Assert.AreSame(cust, order.Customer);
            Assert.IsTrue(receivedChangeNotification);

            // do the same test for EntityCollection
            ec = new NorthwindEntityContainer();

            // create a new order
            order = new DataTests.Northwind.LTS.Order();
            orders.Add(order);

            // force the entity collection to cache
            Assert.AreEqual(0, order.Order_Details.Count);

            receivedChangeNotification = false;
            ((INotifyCollectionChanged)order.Order_Details).CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    receivedChangeNotification = true;
                }
            };

            // simulate an accept changes merging in server state
            serverOrder = new DataTests.Northwind.LTS.Order() { OrderID = 1 };
            order.Merge(serverOrder, LoadBehavior.RefreshCurrent);

            Assert.AreEqual(0, order.Order_Details.Count);
            Assert.IsTrue(receivedChangeNotification);
        }

        /// <summary>
        /// This bug allowed a Detached entity to exist in an Entity collection. A
        /// bug in EC was skipping the inferred Add that EC normally does when an
        /// entity is added to the collection.
        /// </summary>
        [TestMethod]
        [WorkItem(191649)]
        public void EntityCollection_DetachedEntity()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            var custs = ec.GetEntitySet<DataTests.Northwind.LTS.Customer>();
            var orders = ec.GetEntitySet<DataTests.Northwind.LTS.Order>();

            DataTests.Northwind.LTS.Customer cust = new DataTests.Northwind.LTS.Customer { CustomerID = "ALFKI" };
            ec.LoadEntities(new Entity[] { cust });

            DataTests.Northwind.LTS.Order o = new DataTests.Northwind.LTS.Order();
            o.CustomerID = "VINET";
            orders.Add(o);
            Assert.AreEqual(EntityState.New, o.EntityState);

            // The key to this repro is forcing the EntityCollection to cache
            Assert.AreEqual(0, cust.Orders.Count);

            orders.Remove(o);
            Assert.AreEqual(EntityState.Detached, o.EntityState);
            cust.Orders.Add(o);
            Assert.AreEqual(EntityState.New, o.EntityState);
        }

        /// <summary>
        /// We shouldn't be doing any dynamic association updates for detached entities
        /// </summary>
        [TestMethod]
        [WorkItem(191649)]
        public void EntityCollection_DetachedEntity_AssociationUpdates()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            var custs = ec.GetEntitySet<DataTests.Northwind.LTS.Customer>();
            var orders = ec.GetEntitySet<DataTests.Northwind.LTS.Order>();

            DataTests.Northwind.LTS.Customer cust = new DataTests.Northwind.LTS.Customer { CustomerID = "ALFKI" };
            ec.LoadEntities(new Entity[] { cust });

            DataTests.Northwind.LTS.Order o = new DataTests.Northwind.LTS.Order();
            o.CustomerID = "VINET";

            orders.Add(o);
            orders.Remove(o);

            Assert.AreEqual(0, cust.Orders.Count);

            o.CustomerID = "ALFKI";

            Assert.AreEqual(EntityState.Detached, o.EntityState);
            Assert.IsFalse(cust.Orders.Contains(o));
            Assert.IsFalse(orders.Contains(o));

            // add the entity after making the FK modification - we expect
            // it to show up in the collection
            orders.Attach(o);
            Assert.AreEqual(EntityState.Unmodified, o.EntityState);
            Assert.IsTrue(cust.Orders.Contains(o));
        }

        [TestMethod]
        [WorkItem(188727)]
        public void EntityCollection_AssociationMaintenance_NewEntities()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();
            var custs = ec.GetEntitySet<DataTests.Northwind.LTS.Customer>();
            var orders = ec.GetEntitySet<DataTests.Northwind.LTS.Order>();

            DataTests.Northwind.LTS.Customer anton = new DataTests.Northwind.LTS.Customer { CustomerID = "ANTON" };
            DataTests.Northwind.LTS.Customer alfki = new DataTests.Northwind.LTS.Customer { CustomerID = "ALFKI" };
            ec.LoadEntities(new Entity[] { anton, alfki });
            ((IChangeTracking)ec).AcceptChanges();

            // create a new order
            DataTests.Northwind.LTS.Order order = new DataTests.Northwind.LTS.Order();
            order.Customer = anton;
            orders.Add(order);

            // explicitly set the customer FK
            order.CustomerID = "ALFKI";

            Assert.AreSame(alfki, order.Customer);

            // Do the same test for EntityCollection, verifying that modifying
            // the Parent key members results in the collection being reset
            ec = new NorthwindEntityContainer();
            custs = ec.GetEntitySet<DataTests.Northwind.LTS.Customer>();
            orders = ec.GetEntitySet<DataTests.Northwind.LTS.Order>();

            anton = new DataTests.Northwind.LTS.Customer { CustomerID = "ANTON" };
            custs.Add(anton);
            anton.Orders.Add(new DataTests.Northwind.LTS.Order());
            anton.Orders.Add(new DataTests.Northwind.LTS.Order());
            anton.Orders.Add(new DataTests.Northwind.LTS.Order());
            Assert.AreEqual(3, anton.Orders.Count);

            // Setting the PK explicitly on the parent causes all the added
            // children to be removed, since they no longer match the predicate
            // Order.CustomerID == CustomerID
            anton.CustomerID = "Foo";
            Assert.AreEqual(0, 0);
        }

        /// <summary>
        /// This is a regression test for a customer reported bug where an EntityCollection
        /// was getting out of sync with the underlying EntitySet.
        /// </summary>
        [TestMethod]
        [WorkItem(188727)]
        public void EntityCollection_Bug188727()
        {
            // Important points in this repro:
            // - the association is based on a multipart key
            // - the entities do not support the Edit operation
            DynamicEntityContainer ec = new DynamicEntityContainer();
            var citiesSet = ec.AddEntitySet<City>(EntitySetOperations.Add);
            var zipSet = ec.AddEntitySet<Zip>(EntitySetOperations.Add);

            // Here we create a new parent and child and link them. In the tests below, we expect
            // this parentage to be maintained.
            City city = new City() { Name = "Toledo", CountyName = "Lucas", StateName = "OH" };
            Zip zip = new Zip() { Code = 99999, FourDigit = 0001, CityName = "Toledo", CountyName = "Lucas", StateName = "OH" };
            city.ZipCodes.Add(zip);
            citiesSet.Add(city);

            Assert.AreEqual(2, ec.GetChanges().AddedEntities.Count);

            city.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                // this handler is key to the repro. It forces the city.ZipCodes entity collection to
                // cache itself
                City c = (City)sender;
                c.ZipCodes.ToArray();
            };

            Assert.AreEqual(1, city.ZipCodes.Count);

            // Simulate a submit changes cycle where the auto generated IDs
            // are returned to the client and synced into the instances. We expect
            // the references that we explicitly configured to remain unmodified.
            Zip storeZipState = new Zip() { Code = 99999, FourDigit = 0001, CityName = "Issaquah", CountyName = "King", StateName = "WA" };
            zip.Merge(storeZipState, LoadBehavior.RefreshCurrent);

            City storeCityState = new City() { Name = "Issaquah", CountyName = "King", StateName = "WA" };
            city.Merge(storeCityState, LoadBehavior.RefreshCurrent);

            ((IRevertibleChangeTracking)ec).AcceptChanges();

            Assert.AreEqual(1, city.ZipCodes.Count);
        }

        /// <summary>
        /// Verify that loading an entity type that isn't present in the container results
        /// in the expected exception.
        /// </summary>
        [TestMethod]
        public void EntityContainer_LoadUnregisteredType()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();

            Product prod = new Product { ProductID = 1 };
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ec.LoadEntities(new Entity[] { prod });
            },
            string.Format(CultureInfo.CurrentCulture, Resource.EntityContainerDoesntContainEntityType, typeof(Product)));
        }

        /// <summary>
        /// EntityCollection dynamic membership updates should not be processed when either
        /// the candidate children are New or the parent entity itself is New.
        /// </summary>
        [TestMethod]
        [WorkItem(896953)]
        [WorkItem(896198)]
        public void EntityCollection_DynamicAddBehavior()
        {
            // new parent, new child
            TestEntityContainer ec = new TestEntityContainer();
            PurchaseOrder o = new PurchaseOrder();
            PurchaseOrderDetail d = new PurchaseOrderDetail();
            ec.PurchaseOrders.Add(o);
            Assert.AreEqual(0, o.PurchaseOrderDetails.Count); // force load
            ec.PurchaseOrderDetails.Add(d);
            d.PurchaseOrderID = 1;
            d.PurchaseOrderID = 0;
            Assert.AreEqual(0, o.PurchaseOrderDetails.Count);

            // existing parent, new child
            ec = new TestEntityContainer();
            o = new PurchaseOrder();
            d = new PurchaseOrderDetail();
            ec.LoadEntities(new Entity[] { o });
            Assert.AreEqual(EntityState.Unmodified, o.EntityState);
            Assert.AreEqual(0, o.PurchaseOrderDetails.Count); // force load
            ec.PurchaseOrderDetails.Add(d);
            d.PurchaseOrderID = 1;
            d.PurchaseOrderID = 0;
            Assert.AreEqual(0, o.PurchaseOrderDetails.Count);

            // new parent, existing child
            ec = new TestEntityContainer();
            o = new PurchaseOrder();
            d = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1 };
            ec.LoadEntities(new Entity[] { d });
            ec.PurchaseOrders.Add(o);
            Assert.AreEqual(EntityState.Unmodified, d.EntityState);
            Assert.AreEqual(0, o.PurchaseOrderDetails.Count); // force load
            d.PurchaseOrderID = 1;
            d.PurchaseOrderID = 0;
            Assert.AreEqual(0, o.PurchaseOrderDetails.Count);

            // existing parent, existing child
            ec = new TestEntityContainer();
            o = new PurchaseOrder { PurchaseOrderID = 1 };
            d = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1 };
            ec.LoadEntities(new Entity[] { o, d });
            Assert.AreEqual(1, o.PurchaseOrderDetails.Count); // force load
            d.PurchaseOrderID = 0;
            Assert.AreEqual(0, o.PurchaseOrderDetails.Count);
            ec.LoadEntities(new Entity[] { new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 2 } });
            Assert.AreEqual(1, o.PurchaseOrderDetails.Count);
            d.PurchaseOrderID = 1;
            Assert.AreEqual(2, o.PurchaseOrderDetails.Count);
        }

        /// <summary>
        /// EntityRef dynamic membership updates should not be processed when either
        /// the candidate child is New or the parent entity itself is New.
        /// </summary>
        [TestMethod]
        [WorkItem(896953)]
        [WorkItem(896198)]
        public void EntityRef_DynamicAddBehavior()
        {
            // new parent, new child
            TestEntityContainer ec = new TestEntityContainer();
            PurchaseOrder o = new PurchaseOrder();
            PurchaseOrderDetail d = new PurchaseOrderDetail();
            ec.PurchaseOrderDetails.Add(d);
            Assert.IsNull(d.PurchaseOrder); // force load
            ec.PurchaseOrders.Add(o);
            Assert.IsNull(d.PurchaseOrder);
            o.PurchaseOrderID = 1;
            o.PurchaseOrderID = 0;
            Assert.IsNull(d.PurchaseOrder);

            // existing parent, new child
            ec = new TestEntityContainer();
            o = new PurchaseOrder();
            d = new PurchaseOrderDetail();
            ec.LoadEntities(new Entity[] { d });
            Assert.IsNull(d.PurchaseOrder); // force load
            ec.PurchaseOrders.Add(o);
            Assert.IsNull(d.PurchaseOrder);
            o.PurchaseOrderID = 1;
            o.PurchaseOrderID = 0;
            Assert.IsNull(d.PurchaseOrder);

            // new parent, existing child
            ec = new TestEntityContainer();
            o = new PurchaseOrder();
            d = new PurchaseOrderDetail();
            int eventCount = 0;
            d.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "PurchaseOrder")
                {
                    eventCount++;
                }
            };
            ec.PurchaseOrderDetails.Add(d);
            Assert.IsNull(d.PurchaseOrder); // force load
            ec.LoadEntities(new Entity[] { o });
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(o, d.PurchaseOrder);

            // existing parent, existing child
            ec = new TestEntityContainer();
            o = new PurchaseOrder();
            d = new PurchaseOrderDetail();
            eventCount = 0;
            d.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "PurchaseOrder")
                {
                    eventCount++;
                }
            };
            ec.LoadEntities(new Entity[] { d });
            Assert.IsNull(d.PurchaseOrder); // force load
            ec.LoadEntities(new Entity[] { o });
            Assert.AreEqual(1, eventCount);
            Assert.AreSame(o, d.PurchaseOrder);
        }

        [TestMethod]
        [WorkItem(896953)]
        public void AssociationDynamicAddBehaviorRepro()
        {
            TestEntityContainer ec = new TestEntityContainer();

            // simulate load of a parent/child pair
            PurchaseOrder o3 = new PurchaseOrder { PurchaseOrderID = 1 };
            PurchaseOrderDetail d3 = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1 };
            ec.LoadEntities(new Entity[] { o3, d3 });
            Assert.IsTrue(o3.PurchaseOrderDetails.Contains(d3));

            // Add two parents
            PurchaseOrder o1 = new PurchaseOrder();
            ec.PurchaseOrders.Add(o1);
            PurchaseOrder o2 = new PurchaseOrder();
            ec.PurchaseOrders.Add(o2);

            // force load so collections are cached
            Assert.AreEqual(0, o1.PurchaseOrderDetails.Count);
            Assert.AreEqual(0, o2.PurchaseOrderDetails.Count);

            // removing the child from the existing parent causes detail.PurchaseOrderID
            // to go to 0, matching the two new parents
            o3.PurchaseOrderDetails.Remove(d3);
            Assert.AreEqual(0, d3.PurchaseOrderID);

            // Since the orders are new, the modified detail shouldn't be
            // dynamically added to either child collection.
            Assert.AreEqual(0, o1.PurchaseOrderDetails.Count);
            Assert.AreEqual(0, o2.PurchaseOrderDetails.Count);
        }

        /// <summary>
        /// When in an edit session, association update operations are postponed until
        /// after the session is ended/cancelled. That means that if you change an FK
        /// value such that the entity no longer belongs to the collection, it will still
        /// remain in the collection until it is removed. We need to ensure that explicit
        /// removal still works in that scenario.
        /// </summary>
        [TestMethod]
        [WorkItem(892332)]
        public void EntityCollection_ModifyFKInEditSessionThenRemove()
        {
            TestEntityContainer ec = new TestEntityContainer();
            PurchaseOrder order = new PurchaseOrder { PurchaseOrderID = 1 };
            PurchaseOrderDetail detail = new PurchaseOrderDetail();
            order.PurchaseOrderDetails.Add(detail);

            ec.LoadEntities(new Entity[] { order, detail });

            detail = order.PurchaseOrderDetails.Single();
            ((IEditableObject)detail).BeginEdit();

            // change the FK - the entity no longer belongs in order.PurchaseOrderDetails
            // but won't be removed until the edit session is completed or it is explicitly
            // removed below
            detail.PurchaseOrderID = 5;
            Assert.IsTrue(order.PurchaseOrderDetails.Contains(detail));

            // We should be able to explicitly remove. This was failing before
            // the fix.
            order.PurchaseOrderDetails.Remove(detail);
            Assert.IsFalse(order.PurchaseOrderDetails.Contains(detail));

            ((IEditableObject)detail).EndEdit();
            Assert.IsFalse(order.PurchaseOrderDetails.Contains(detail));
        }

        [TestMethod]
        [WorkItem(896198)]
        public void EntityCollection_NullableFK()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            var parentSet = ec.AddEntitySet<TestDomainServices.NullableFKParent>(EntitySetOperations.All);
            var childSet = ec.AddEntitySet<TestDomainServices.NullableFKChild>(EntitySetOperations.All);

            // Add two parents
            TestDomainServices.NullableFKParent p1 = new NullableFKParent();
            parentSet.Add(p1);
            TestDomainServices.NullableFKParent p2 = new NullableFKParent();
            parentSet.Add(p2);

            // force load
            Assert.AreEqual(0, p1.Children.Count);
            Assert.AreEqual(0, p2.Children.Count);

            TestDomainServices.NullableFKChild child = new NullableFKChild();
            childSet.Add(child);

            // after the child has been added change its FK from
            // null to 0
            Assert.IsNull(child.ParentID);
            child.ParentID = 0;

            // we don't expect it to show up in the collections
            Assert.AreEqual(0, p1.Children.Count);
            Assert.AreEqual(0, p2.Children.Count);

            // explicitly add to one parent - only expect it to show
            // up in that collection
            p1.Children.Add(child);
            Assert.AreEqual(1, p1.Children.Count);
            Assert.AreEqual(0, p2.Children.Count);
        }

        [TestMethod]
        [WorkItem(896198)]
        public void EntityRef_NullableFK()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            var parentSet = ec.AddEntitySet<TestDomainServices.NullableFKParent>(EntitySetOperations.All);
            var childSet = ec.AddEntitySet<TestDomainServices.NullableFKChild>(EntitySetOperations.All);

            // Add two parents
            TestDomainServices.NullableFKParent p1 = new NullableFKParent();
            parentSet.Add(p1);
            TestDomainServices.NullableFKParent p2 = new NullableFKParent();
            parentSet.Add(p2);

            // force load
            Assert.IsNull(p1.Child);
            Assert.IsNull(p2.Child);

            TestDomainServices.NullableFKChild child = new NullableFKChild();
            childSet.Add(child);

            // after the child has been added change its FK from
            // null to 0
            Assert.IsNull(child.ParentID);
            child.ParentID_Singleton = 0;

            // we don't expect it to show up in the references
            Assert.IsNull(p1.Child);
            Assert.IsNull(p2.Child);

            // explicitly add to one parent - only expect it to show
            // up in that reference
            p1.Child = child;
            Assert.AreSame(child, p1.Child); Assert.IsNull(p2.Child); Assert.AreEqual(0, p2.Children.Count);
        }

        /// <summary>
        /// This test differs from the first in that after the entity is moved
        /// via the EC.Add call, its FK is set BACK to what it was before. The end
        /// result should be no change.
        /// </summary>
        [TestMethod]
        [WorkItem(896550)]
        public void EntityCollection_EditSessionManualReAdd2()
        {
            TestEntityContainer ec = new TestEntityContainer();

            PurchaseOrder o1 = new PurchaseOrder { PurchaseOrderID = 1 };
            PurchaseOrderDetail d1 = new PurchaseOrderDetail { PurchaseOrderDetailID = 1 };
            o1.PurchaseOrderDetails.Add(d1);

            PurchaseOrder o2 = new PurchaseOrder { PurchaseOrderID = 2 };
            PurchaseOrderDetail d2 = new PurchaseOrderDetail { PurchaseOrderDetailID = 2 };
            o2.PurchaseOrderDetails.Add(d2);

            ec.LoadEntities(new Entity[] { o1, o2, d1, d2 });

            // take an existing detail from order2
            PurchaseOrderDetail detail = o2.PurchaseOrderDetails.First();
            ((IEditableObject)detail).BeginEdit();

            // transfer the detail to the other order
            o1.PurchaseOrderDetails.Add(detail);
            Assert.IsTrue(o1.PurchaseOrderDetails.Contains(detail));

            // change FK to point back to the original order
            detail.PurchaseOrderID = o2.PurchaseOrderID;
            Assert.IsTrue(o1.PurchaseOrderDetails.Contains(detail));

            ((IEditableObject)detail).EndEdit();

            // we expect the order NOT to have moved
            Assert.AreEqual(o2.PurchaseOrderID, detail.PurchaseOrderID);
            Assert.IsTrue(o2.PurchaseOrderDetails.Contains(detail));
            Assert.IsFalse(o1.PurchaseOrderDetails.Contains(detail));
        }

        [TestMethod]
        [WorkItem(893566)]
        public void EntityCollection_EditSessionManualReAdd()
        {
            TestEntityContainer ec = new TestEntityContainer();

            PurchaseOrder o1 = new PurchaseOrder { PurchaseOrderID = 1 };
            PurchaseOrderDetail d1 = new PurchaseOrderDetail { PurchaseOrderDetailID = 1 };
            o1.PurchaseOrderDetails.Add(d1);

            PurchaseOrder o2 = new PurchaseOrder { PurchaseOrderID = 2 };
            PurchaseOrderDetail d2 = new PurchaseOrderDetail { PurchaseOrderDetailID = 2 };
            o2.PurchaseOrderDetails.Add(d2);

            ec.LoadEntities(new Entity[] { o1, o2, d1, d2 });

            // take an existing detail from order2
            PurchaseOrderDetail detail = o2.PurchaseOrderDetails.First();
            ((IEditableObject)detail).BeginEdit();
            // change FK to point to order1
            detail.PurchaseOrderID = o1.PurchaseOrderID;
            // manually add BACK to order2
            o2.PurchaseOrderDetails.Add(detail);
            ((IEditableObject)detail).EndEdit();
            Assert.AreEqual(o2.PurchaseOrderID, detail.PurchaseOrderID);
            Assert.IsTrue(o2.PurchaseOrderDetails.Contains(detail));
            Assert.IsFalse(o1.PurchaseOrderDetails.Contains(detail));
        }

        [TestMethod]
        [WorkItem(888468)]
        [WorkItem(892336)]
        [WorkItem(893455)]
        public void EntityCollection_EditSessionNotificationScenarios()
        {
            TestEntityContainer ec = new TestEntityContainer();

            PurchaseOrder o1 = new PurchaseOrder { PurchaseOrderID = 1 };
            PurchaseOrderDetail d1 = new PurchaseOrderDetail { PurchaseOrderDetailID = 1 };
            o1.PurchaseOrderDetails.Add(d1);

            PurchaseOrder o2 = new PurchaseOrder { PurchaseOrderID = 2 };
            PurchaseOrderDetail d2 = new PurchaseOrderDetail { PurchaseOrderDetailID = 2 };
            o2.PurchaseOrderDetails.Add(d2);

            ec.LoadEntities(new Entity[] { o1, o2, d1, d2 });

            List<string> notifications = new List<string>();
            ((INotifyCollectionChanged)o1.PurchaseOrderDetails).CollectionChanged += (s, e) =>
            {
                notifications.Add("Order1_" + e.Action.ToString());
            };
            ((INotifyCollectionChanged)o2.PurchaseOrderDetails).CollectionChanged += (s, e) =>
            {
                notifications.Add("Order2_" + e.Action.ToString());
            };

            // scenario 1
            PurchaseOrderDetail newDetail = new PurchaseOrderDetail();
            ((IEditableObject)newDetail).BeginEdit();
            o1.PurchaseOrderDetails.Add(newDetail);
            o2.PurchaseOrderDetails.Add(newDetail);
            ((IEditableObject)newDetail).EndEdit();
            Assert.AreEqual(3, notifications.Count);
            Assert.AreEqual("Order1_Add", notifications[0]);
            Assert.AreEqual("Order1_Remove", notifications[1]);
            Assert.AreEqual("Order2_Add", notifications[2]);
            ec.PurchaseOrderDetails.Remove(newDetail);
            Assert.IsFalse(o2.PurchaseOrderDetails.Contains(newDetail));

            // do scenario 1 again this time w/o the edit session. Should be the same
            // result
            notifications.Clear();
            newDetail = new PurchaseOrderDetail();
            o1.PurchaseOrderDetails.Add(newDetail);
            o2.PurchaseOrderDetails.Add(newDetail);
            Assert.AreEqual(3, notifications.Count);
            Assert.AreEqual("Order1_Add", notifications[0]);
            Assert.AreEqual("Order1_Remove", notifications[1]);
            Assert.AreEqual("Order2_Add", notifications[2]);
            ec.PurchaseOrderDetails.Remove(newDetail);
            Assert.IsFalse(o1.PurchaseOrderDetails.Contains(newDetail));
            Assert.IsFalse(o2.PurchaseOrderDetails.Contains(newDetail));

            // scenario 3 - no edit session, with an intervening FK update. Again
            // we expect the same results.
            notifications.Clear();
            newDetail = new PurchaseOrderDetail();
            o1.PurchaseOrderDetails.Add(newDetail);
            newDetail.PurchaseOrderID = o2.PurchaseOrderID;
            o2.PurchaseOrderDetails.Add(newDetail);
            Assert.AreEqual(3, notifications.Count);
            Assert.AreEqual("Order1_Add", notifications[0]);
            Assert.AreEqual("Order1_Remove", notifications[1]);
            Assert.AreEqual("Order2_Add", notifications[2]);
            ec.PurchaseOrderDetails.Remove(newDetail);
            Assert.IsFalse(o1.PurchaseOrderDetails.Contains(newDetail));
            Assert.IsFalse(o2.PurchaseOrderDetails.Contains(newDetail));

            // repro scenario - what happens is since the FK is modified before the second add to point
            // to the second order, the PurchaseOrderDetail.PurchaseOrder setter code noops, since
            // based on the update FK the detail already points to the correct order. This means the
            // previous.PurchaseOrderDetails.Remove code is not executed. It is not until the edit session
            // is completed that the remove is finally fired (it is postponed).
            notifications.Clear();
            newDetail = new PurchaseOrderDetail();
            ((IEditableObject)newDetail).BeginEdit();
            o1.PurchaseOrderDetails.Add(newDetail);
            newDetail.PurchaseOrderID = o2.PurchaseOrderID;
            o2.PurchaseOrderDetails.Add(newDetail);
            ((IEditableObject)newDetail).EndEdit();
            Assert.AreEqual(3, notifications.Count);
            Assert.AreEqual("Order1_Add", notifications[0]);
            Assert.AreEqual("Order2_Add", notifications[1]);
            Assert.AreEqual("Order1_Remove", notifications[2]);
            ec.PurchaseOrderDetails.Remove(newDetail);
            Assert.IsFalse(o1.PurchaseOrderDetails.Contains(newDetail));
            Assert.IsFalse(o2.PurchaseOrderDetails.Contains(newDetail));

            // verify that if the edit session is cancelled, that everything is
            // undone properly and events are raised properly
            notifications.Clear();
            newDetail = new PurchaseOrderDetail();
            ((IEditableObject)newDetail).BeginEdit();
            o1.PurchaseOrderDetails.Add(newDetail);
            newDetail.PurchaseOrderID = o2.PurchaseOrderID;
            o2.PurchaseOrderDetails.Add(newDetail);
            ((IEditableObject)newDetail).CancelEdit();
            Assert.AreEqual(4, notifications.Count);
            Assert.AreEqual("Order1_Add", notifications[0]);
            Assert.AreEqual("Order2_Add", notifications[1]);
            Assert.AreEqual("Order1_Remove", notifications[2]);
            Assert.AreEqual("Order2_Remove", notifications[3]);
            Assert.IsFalse(o1.PurchaseOrderDetails.Contains(newDetail));
            Assert.IsFalse(o2.PurchaseOrderDetails.Contains(newDetail));

            // verify that if the edit session is cancelled, that everything is
            // undone properly and events are raised properly
            notifications.Clear();
            PurchaseOrderDetail removeDetail = o1.PurchaseOrderDetails.First();
            ((IEditableObject)removeDetail).BeginEdit();
            o1.PurchaseOrderDetails.Remove(removeDetail);
            ((IEditableObject)removeDetail).CancelEdit();
            Assert.AreEqual(2, notifications.Count);
            Assert.AreEqual("Order1_Remove", notifications[0]);
            Assert.AreEqual("Order1_Add", notifications[1]);
            Assert.IsTrue(o1.PurchaseOrderDetails.Contains(removeDetail));
            Assert.AreEqual(o1.PurchaseOrderID, removeDetail.PurchaseOrderID);
        }

        /// <summary>
        /// In this repro the core issue was causing EntitySet.RegisterAssociationCallback
        /// to be called while EntitySet.UpdateRelatedAssociations was executing.
        /// </summary>
        [TestMethod]
        [WorkItem(890795)]
        public void EntityCollection_SelfAssociationUpdate()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<C> cs = ec.AddEntitySet<C>(EntitySetOperations.All);
            EntitySet<D> ds = ec.AddEntitySet<D>(EntitySetOperations.All);

            D d1 = new D { ID = 1 };
            D d2 = new D { ID = 2, DSelfRef_ID1 = 1 };
            D d3 = new D { ID = 3, DSelfRef_ID1 = 1 };

            ec.LoadEntities(new Entity[] { d1, d2, d3 });

            Assert.AreEqual(2, d1.Ds.Count);
            
            d1.Ds.EntityRemoved += (s,e) =>
            {
                // this is the key to the bug - it causes d2.Ds to cache
                // which causes a callback to be registered for that association
                Assert.AreEqual(0, d3.Ds.Count);
            };

            d1.Ds.Remove(d2);
        }

        /// <summary>
        /// Verify that when an entity is removed then readded to an EntityCollection,
        /// the EntityAdded collection changed event is only raised once.
        /// </summary>
        [TestMethod]
        [WorkItem(887459)]
        public void EntityCollection_EntityAddedEvent()
        {
            TestEntityContainer ec = new TestEntityContainer();
            PurchaseOrder order = new PurchaseOrder { PurchaseOrderID = 1 };
            PurchaseOrderDetail detail = new PurchaseOrderDetail();
            order.PurchaseOrderDetails.Add(detail);

            ec.LoadEntities(new Entity[] { order, detail });

            int eventCount = 0;
            order.PurchaseOrderDetails.EntityAdded += (s, e) => { eventCount++; };

            detail.PurchaseOrder = null;
            order.PurchaseOrderDetails.Add(detail);

            Assert.AreEqual(1, eventCount);
        }

        /// <summary>
        /// When EntityCollection is bound to a DataGrid and a governing FK is modified in a child of that
        /// collection, the sequence of events were previously:
        /// 
        /// 1) The Entity raised a property changed notification for the FK
        /// 2) EntitySet handled that event and notified the EntityCollection to remove the entity
        /// 3) EntityCollection raised a collection changed notification for the remove, and since the
        ///    grid row was in Edit mode, the edit session was cancelled on the entity
        /// 4) That caused the FK member to be reset back causing a similar chain of events
        ///    as above, causing the entity to be added BACK to the collection.
        /// 5) This caused ObservableCollection to throw an exception : "Cannot change ObservableCollection
        ///    during a CollectionChanged or PropertyChanged event"
        ///    
        /// The fix is to postpone the callback notifications when in an edit session. Another issue was the
        /// way DataGrid handled reentrancy. After EndEdit is called, it causes the row to be removed from the
        /// grid, but the grid didn't keep track of the fact that editing was finished, so that remove caused
        /// a CancelEdit to be called. Part of the fix was to ensure that final Cancel was a noop since EndEdit
        /// was already called.
        /// 
        /// This test runs both desktop and SL, but the bug only reproed in SL.
        /// </summary>
        [TestMethod]
        [WorkItem(888468)]
        public void EntityCollection_CollectionChangedNotificationsDeferredDuringEditSession()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Employee e1 = new Employee { EmployeeID = 1 };
            Employee e2 = new Employee { EmployeeID = 2 };
            PurchaseOrder o1 = new PurchaseOrder { PurchaseOrderID = 1, EmployeeID = 1 };
            bool isEditing = false;

            ec.LoadEntities(new Entity[] { e1, e2, o1 });

            System.Windows.Data.CollectionViewSource cvs1 = new System.Windows.Data.CollectionViewSource();
            cvs1.Source = e1.PurchaseOrders;
            cvs1.View.CollectionChanged += (object o, NotifyCollectionChangedEventArgs e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    // simulate grid behavior - when a row that is being edited is removed,
                    // the edit is cancelled
                    PurchaseOrder order = (PurchaseOrder)e.OldItems[0];
                    if (isEditing)
                    {
                        ((IEditableObject)order).CancelEdit();
                    }
                }
            };

            System.Windows.Data.CollectionViewSource cvs2 = new System.Windows.Data.CollectionViewSource();
            cvs2.Source = e2.PurchaseOrders;

            Assert.AreEqual(1, e1.PurchaseOrders.Count);
            Assert.AreEqual(0, e2.PurchaseOrders.Count);
            Assert.AreEqual(1, cvs1.View.SourceCollection.Cast<PurchaseOrder>().Count());
            Assert.AreEqual(0, cvs2.View.SourceCollection.Cast<PurchaseOrder>().Count());

            // update the order employee ID to cause the order to 
            // move to the other collection
            ((IEditableObject)o1).BeginEdit();
            isEditing = true;
            o1.EmployeeID = 2;
            isEditing = false;

            Assert.AreEqual(1, e1.PurchaseOrders.Count);
            Assert.AreEqual(0, e2.PurchaseOrders.Count);
            Assert.AreEqual(1, cvs1.View.SourceCollection.Cast<PurchaseOrder>().Count());
            Assert.AreEqual(0, cvs2.View.SourceCollection.Cast<PurchaseOrder>().Count());

            ((IEditableObject)o1).EndEdit();

            Assert.AreEqual(0, e1.PurchaseOrders.Count);
            Assert.AreEqual(1, e2.PurchaseOrders.Count);
            Assert.AreEqual(0, cvs1.View.SourceCollection.Cast<PurchaseOrder>().Count());
            Assert.AreEqual(1, cvs2.View.SourceCollection.Cast<PurchaseOrder>().Count());

            // move the entity back by modifying again
            ((IEditableObject)o1).BeginEdit();
            isEditing = true;
            o1.EmployeeID = 1;
            isEditing = false;
            ((IEditableObject)o1).EndEdit();

            Assert.AreEqual(1, e1.PurchaseOrders.Count);
            Assert.AreEqual(0, e2.PurchaseOrders.Count);
            Assert.AreEqual(1, cvs1.View.SourceCollection.Cast<PurchaseOrder>().Count());
            Assert.AreEqual(0, cvs2.View.SourceCollection.Cast<PurchaseOrder>().Count());
        }

#if !SILVERLIGHT
        /// <summary>
        /// Verifies that the EntitySet.CollectionChanged event doesn't have any subscribers after 
        /// adding/removing a graph of entities.
        /// </summary>
        [TestMethod]
        [WorkItem(868045)]
        public void EntitySet_CollectionChanged_Empty()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<PurchaseOrder> poSet = ec.AddEntitySet<PurchaseOrder>(EntitySetOperations.All);
            EntitySet<PurchaseOrderDetail> podSet = ec.AddEntitySet<PurchaseOrderDetail>(EntitySetOperations.All);

            PurchaseOrder po = new PurchaseOrder();
            po.PurchaseOrderID = 1;

            PurchaseOrderDetail pod = new PurchaseOrderDetail();
            pod.PurchaseOrder = po;

            poSet.Add(po);

            Assert.IsTrue(pod.IsInferred, "Expected an inferred add.");

            poSet.Remove(po);
            podSet.Remove(pod);

            Func<EntitySet, int> getCollectionChangedSubscriberCount = set =>
            {
                FieldInfo collectionChangedField = typeof(EntitySet).GetField("_collectionChangedEventHandler", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo invocationCountField = typeof(MulticastDelegate).GetField("_invocationCount", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(collectionChangedField, "Could not find private field EntitySet._collectionChangedEventHandler.");
                Assert.IsNotNull(invocationCountField, "Could not find private field Delegate._invocationCount.");

                Delegate collectionChangedDelegate = (Delegate)collectionChangedField.GetValue(set);
                if (collectionChangedDelegate == null)
                {
                    return 0;
                }

                int subscribers = ((IntPtr)invocationCountField.GetValue(collectionChangedDelegate)).ToInt32();
                if (subscribers == 0)
                {
                    // The field actually only has a non-zero value if there's a list of subscribers. If the value is 0, then the 
                    // only subscriber there is is the delegate itself.
                    return 1;
                }

                return subscribers;
            };

            Assert.AreEqual(0, getCollectionChangedSubscriberCount(poSet), "Did not expect any instances to have subscribed to poSet.CollectionChanged.");
            Assert.AreEqual(0, getCollectionChangedSubscriberCount(podSet), "Expected only a single EntityCollection instance to have subscribed to podSet.CollectionChanged.");
        }
#endif

        [TestMethod]
        [WorkItem(618711)]
        public void EntityRef_SingletonReturn()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<C> cs = ec.AddEntitySet<C>(EntitySetOperations.All);
            EntitySet<D> ds = ec.AddEntitySet<D>(EntitySetOperations.All);

            D d1 = new D { ID = 1 };
            D d2 = new D { ID = 2 };
            C c1 = new C { ID = 1, DID_Ref1 = 1 };
            C c2 = new C { ID = 2, DID_Ref1 = 2 };

            ec.LoadEntities(new Entity[] { d1, d2, c1, c2 });

            // access the FK association to get the "children"
            D d1Del = c1.D_Ref1;
            D d2Del = c2.D_Ref1;

            // remove the referenced entities
            ds.Remove(d1Del);
            ds.Remove(d2Del);

            // create two new uninitialized children
            D d1New = new D();
            D d2New = new D();

            // set the FK refs to the new entities
            // when the first is set, FK c1.DID_Ref1 becomes 0
            c1.D_Ref1 = d1New;

            // Previously this next line caused the bug. When the second is set,
            // c2.DID_Ref1 ALSO becomes 0. In the setter code for D_Ref1 when the
            // back reference is fixed up, since there are TWO entities matching the
            // D.C predicate an exception was thrown.
            c2.D_Ref1 = d2New;

            Assert.AreSame(d2New, c2.D_Ref1);

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.AreEqual(2, changeSet.AddedEntities.Count);
            Assert.AreEqual(2, changeSet.ModifiedEntities.Count);
            Assert.AreEqual(2, changeSet.RemovedEntities.Count);

            // here's a simpler repro
            ec.Clear();
            d1 = new D { ID = 1 };
            d2 = new D { ID = 2 };
            c1 = new C { ID = 1, DID_Ref1 = 1 };
            ec.LoadEntities(new Entity[] { d1, d2, c1 });
            // Here we're using ApplyState to allow us to set a PK member w/o validation failure
            // since PK members cannot be changed. This test should really be based on an association
            // not involving PK, but the test is still valid this way.
            d2.ApplyState(new Dictionary<string, object> {{"ID", 1}});
            Assert.IsNull(c1.D_Ref1);  // since there is more than one match
            d2.ApplyState(new Dictionary<string, object> { { "ID", 2 } });
            Assert.AreSame(d1, c1.D_Ref1);
        }

        /// <summary>
        /// When an entity is in an edit state, any behind the scenes refresh
        /// operation will be ignored for that entity. That ensures that user
        /// edits aren't wiped out while the entity is being edited. For example,
        /// in an interval based auto-refresh scenario, the worst that will happen
        /// is the entity will miss a refresh cycle. The conflict will be caught
        /// on submit as a concurrency failure, which can be addressed by refreshing
        /// that entity once more.
        /// </summary>
        [TestMethod]
        [WorkItem(860951)]
        public void EntityInEditState_RefreshIgnored()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Product p = new Product { ProductID = 1, Name = "Beef Flavored Ice Cream", Color = "Brown", ListPrice = 5.00M, ProductNumber = "1" };
            ec.LoadEntities(new Entity[] { p });

            // start an edit session
            ((IEditableObject)p).BeginEdit();
            p.ListPrice += 0.50M;
            Assert.AreEqual(EntityState.Modified, p.EntityState);

            // simulate a behind the scenes merge
            Product storeEntity = new Product { ProductID = 1, Name = "Beef Flavored Ice Cream", Color = "Tan", ListPrice = 6.00M, ProductNumber = "1" };
            ec.LoadEntities(new Entity[] { storeEntity }, LoadBehavior.RefreshCurrent);

            // verify server changes NOT merged in to current or original
            Assert.AreEqual("Brown", p.Color);
            Assert.AreEqual(5.50M, p.ListPrice);
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            Product original = (Product)p.GetOriginal();
            Assert.AreEqual("Beef Flavored Ice Cream", original.Name);
            Assert.AreEqual("Brown", original.Color);
            Assert.AreEqual(5.00M, original.ListPrice);

            ((IEditableObject)p).EndEdit();
            Assert.AreEqual("Brown", p.Color);
            Assert.AreEqual(5.50M, p.ListPrice);
        }

        [TestMethod]
        [WorkItem(800635)]
        public void EntityCollection_CachedDetachedEntities()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Product p = new Product { ProductID = 1 };
            ec.Products.Add(p);

            // this line causes the new product entity to be added to the
            // entity collection
            PurchaseOrderDetail d = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1, Product = p };

            ec.PurchaseOrderDetails.Add(d);
            ec.PurchaseOrderDetails.Remove(d);

            // the detached detail should no longer show up in the collection
            Assert.IsTrue(p.PurchaseOrderDetails.Count == 0);

            // verify same thing if collection is cached first - 
            // in the above test, the collection is not yet cached.
            ec = new TestEntityContainer();
            p = new Product { ProductID = 1 };
            ec.Products.Add(p);
            d = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1, Product = p };
            int count = p.PurchaseOrderDetails.Count; // force the collection to cache
            ec.PurchaseOrderDetails.Add(d);
            ec.PurchaseOrderDetails.Remove(d);
            Assert.IsTrue(p.PurchaseOrderDetails.Count == 0);
        }

        [TestMethod]
        [WorkItem(800635)]
        public void EntityCollection_CachedRemovedEntities()
        {
            TestEntityContainer ec = new TestEntityContainer();

            Product p = new Product { ProductID = 1 };
            ec.Products.Add(p);
            PurchaseOrderDetail d = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1, Product = p };
            ec.PurchaseOrderDetails.Add(d);
            ((IChangeTracking)ec).AcceptChanges();

            // Remove the detail and verify that it no longer shows up in the collection
            ec.PurchaseOrderDetails.Remove(d);
            Assert.IsTrue(p.PurchaseOrderDetails.Count == 0);

            // verify same thing if collection is cached first - 
            // in the above test, the collection is not yet cached.
            ec = new TestEntityContainer();
            p = new Product { ProductID = 1 };
            ec.Products.Add(p);
            d = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1, Product = p };
            ec.PurchaseOrderDetails.Add(d);
            ((IChangeTracking)ec).AcceptChanges();
            int count = p.PurchaseOrderDetails.Count; // force the collection to cache
            ec.PurchaseOrderDetails.Remove(d);
            Assert.IsTrue(p.PurchaseOrderDetails.Count == 0);
        }

        [TestMethod]
        [WorkItem(800635)]
        public void EntityRef_CachedDetachedEntities()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Product p = new Product { ProductID = 1 };
            ec.Products.Add(p);

            PurchaseOrderDetail d = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1, Product = p };
            ec.PurchaseOrderDetails.Add(d);

            ec.Products.Remove(p);

            // the detached product should no longer be cached
            Assert.IsNull(d.Product);
        }

        /// <summary>
        /// Verify that attempting to Attach/Load an entity with a null identity
        /// results in the expected exception
        /// </summary>
        [TestMethod]
        public void EntitySet_AddWithNullIdentity()
        {
            City city = new City();

            Cities.CityDomainContext ctxt = new CityDomainContext(TestURIs.Cities);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ctxt.Cities.Attach(city);
            }, string.Format(Resource.EntityKey_NullIdentity, city));

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ctxt.EntityContainer.LoadEntities(new Entity[] { city });
            }, string.Format(Resource.EntityKey_NullIdentity, city));
        }

        /// <summary>
        /// Verify that entities that are already attached to another EntityContainer
        /// cannot be set/added to association members.
        /// </summary>
        [TestMethod]
        public void EntityContainer_CrossContainerAttach_Associations()
        {
            TestEntityContainer ec1 = CreatePopulatedTestContainer();
            TestEntityContainer ec2 = CreatePopulatedTestContainer();

            // verify can't set entity ref to an entity from another container
            PurchaseOrderDetail d1 = ec1.PurchaseOrderDetails.First();
            Product p2 = ec2.Products.First();
            ExceptionHelper.ExpectInvalidOperationException(delegate {
                d1.Product = p2;
            }, 
            string.Format(Resource.EntityContainer_CrossContainerAttach, p2));

            // verify that if we detach first, it works
            ec2.Products.Detach(p2);
            p2.ProductID = 100; // need to update key to avoid cache collision
            d1.Product = p2;

            // verify can't add an entity from another container to an entity collection
            PurchaseOrder o1 = ec1.PurchaseOrders.First();
            PurchaseOrderDetail d2 = ec2.PurchaseOrderDetails.First();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                o1.PurchaseOrderDetails.Add(d2);
            },
            string.Format(Resource.EntityContainer_CrossContainerAttach, d2));

            // verify that if we detach first, it works
            ec2.PurchaseOrderDetails.Detach(d2);
            d2.PurchaseOrderDetailID = 100; // need to update key to avoid cache collision
            o1.PurchaseOrderDetails.Add(d2);
        }

        /// <summary>
        /// Verify that entities that are already attached to another EntityContainer
        /// cannot be added/attached to an entity set
        /// </summary>
        [TestMethod]
        public void EntityContainer_CrossContainerAttach_EntitySet()
        {
            TestEntityContainer ec1 = CreatePopulatedTestContainer();
            TestEntityContainer ec2 = new TestEntityContainer();
            ec2.LoadEntities(new Entity[] { 
                new Product { ProductID = 100, Name = "A" },
                new Product { ProductID = 200, Name = "B" },
                new PurchaseOrder { PurchaseOrderID = 100 },
                    new PurchaseOrderDetail { PurchaseOrderID = 100, PurchaseOrderDetailID = 1, ProductID = 100 },
                    new PurchaseOrderDetail { PurchaseOrderID = 100, PurchaseOrderDetailID = 2, ProductID = 200 }
            });

            // verify can't Add an entity from another container
            Product p2 = ec2.Products.First();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ec1.Products.Add(p2);
            },
            string.Format(Resource.EntityContainer_CrossContainerAttach, p2));
            
            // verify it works if detach is called first
            ec2.Products.Detach(p2);
            ec1.Products.Attach(p2);

            // verify can't Attach an entity from another container
            p2 = ec2.Products.First();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ec1.Products.Attach(p2);
            },
            string.Format(Resource.EntityContainer_CrossContainerAttach, p2));

            // verify it works if detach is called first
            ec2.Products.Detach(p2);
            ec1.Products.Attach(p2);
        }

        private TestEntityContainer CreatePopulatedTestContainer()
        {
            TestEntityContainer ec = new TestEntityContainer();
            ec.LoadEntities(new Entity[] { 
                new Product { ProductID = 1, Name = "A" },
                new Product { ProductID = 2, Name = "B" },
                new Product { ProductID = 3, Name = "C" },
                new Product { ProductID = 4, Name = "D" },
                new PurchaseOrder { PurchaseOrderID = 1 },
                    new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1, ProductID = 1 },
                    new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 2, ProductID = 2 },
                new PurchaseOrder { PurchaseOrderID = 2 },
                    new PurchaseOrderDetail { PurchaseOrderID = 2, PurchaseOrderDetailID = 1, ProductID = 3 },
                    new PurchaseOrderDetail { PurchaseOrderID = 2, PurchaseOrderDetailID = 2, ProductID = 4 }
            });

            return ec;
        }

        /// <summary>
        /// Tests the EntityContainer.EntitySets property
        /// </summary>
        [TestMethod]
        [TestDescription("Tests the EntityContainer.EntitySets property.")]
        public void EntityContainer_EntitySets()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Assert.AreEqual(5, ec.EntitySets.Count());

            // verify that external entity sets aren't included in
            // the collection
            Cities.CityDomainContext cities = new Cities.CityDomainContext(TestURIs.Cities);
            EntityContainer citiesContainer = cities.EntityContainer;
            ec.AddReference(citiesContainer.GetEntitySet(typeof(Cities.City)));
            Assert.AreEqual(5, ec.EntitySets.Count());
            Assert.IsFalse(ec.EntitySets.Any(p => p.EntityType == typeof(Cities.City)));
        }

        /// <summary>
        /// Verify that attempting to Add/Attach an entity with a duplicate identity
        /// results in an expected exception.
        /// </summary>
        [TestMethod]
        public void EntitySet_DuplicateKeyDetection()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<PurchaseOrderDetail> set = ec.GetEntitySet<PurchaseOrderDetail>();

            PurchaseOrderDetail d1 = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1 };
            set.Add(d1);
            ((IRevertibleChangeTracking)ec).AcceptChanges();

            // attempt to add another entity with the same identity - expect an exception
            PurchaseOrderDetail d2 = new PurchaseOrderDetail { PurchaseOrderID = 1, PurchaseOrderDetailID = 1 };
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                set.Add(d2);
            }, Resource.EntitySet_DuplicateIdentity);

            // attempt to attach another entity with the same identity - expect an exception
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                set.Attach(d2);
            }, Resource.EntitySet_DuplicateIdentity);
        }

        /// <summary>
        /// Tests the EntityContainer.GetEntitySet method.
        /// </summary>
        [TestDescription("Verifies that EntityContainer.GetEntitySet<T> throws an exception when T is a non-root entity type.")]
        [TestMethod]
        public void EntityContainer_GetEntitySet()
        {
            ConfigurableEntityContainer ec = new ConfigurableEntityContainer();
            ec.CreateSet<City>(EntitySetOperations.All);

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ec.GetEntitySet<CityWithEditHistory>();
            }, String.Format(Resource.EntityContainer_CannotRetrieveEntitySetForDerivedEntity, typeof(CityWithEditHistory).Name, typeof(City).Name));
        }

        /// <summary>
        /// Tests that any stale entities that remain in an unloaded
        /// cached EntityCollection are correctly purged the entity
        /// is removed or detached from the source entity set.
        /// 
        /// Note that in addition to this Remove scenario, the bug also exists in cases
        /// where the entity was detached, and in that case you can't use any "removed"
        /// history to filter it out.
        /// </summary>
        [TestMethod]
        public void EntityCollection_Bug722053()
        {
            TestEntityContainer ec = new TestEntityContainer();
            PurchaseOrderDetail ord = new PurchaseOrderDetail
            {
                PurchaseOrderID = 111, PurchaseOrderDetailID = 222, ProductID = 222
            };
            PurchaseOrder o = new PurchaseOrder
            {
                PurchaseOrderID = 111
            };
            Product p = new Product
            {
                ProductID = 222, Name = "Test"
            };
            ord.PurchaseOrder = o;
            ord.Product = p;

            ec.PurchaseOrderDetails.Add(ord);
            ((IChangeTracking)ec).AcceptChanges();
            Assert.AreEqual(1, ec.PurchaseOrders.Count);
            Assert.AreEqual(1, ec.Products.Count);
            Assert.AreEqual(1, ec.PurchaseOrderDetails.Count);

            // remove the detail
            ec.PurchaseOrderDetails.Remove(ord);
            ((IChangeTracking)ec).AcceptChanges();

            // Repro code
            Product ordProd = ord.Product;
            PurchaseOrder ordOrders = ord.PurchaseOrder;

            PurchaseOrderDetail newOrd = new PurchaseOrderDetail
            {
                PurchaseOrderID = o.PurchaseOrderID, PurchaseOrderDetailID = 222, ProductID = p.ProductID
            };
            Assert.AreEqual(0, p.PurchaseOrderDetails.Count);  // This was failing!
            newOrd.Product = ordProd;
            Assert.AreEqual(1, p.PurchaseOrderDetails.Count);
            newOrd.PurchaseOrder = ordOrders;
            Assert.AreEqual(1, p.PurchaseOrderDetails.Count);
            ec.PurchaseOrderDetails.Add(newOrd);
            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.AddedEntities.Count == 1 && changeSet.ModifiedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);

            // verify the same scenario above for detach
            ec = new TestEntityContainer();
            ord = new PurchaseOrderDetail
            {
                PurchaseOrderID = 111, PurchaseOrderDetailID = 222, ProductID = 222
            };
            o = new PurchaseOrder
            {
                PurchaseOrderID = 111
            };
            p = new Product
            {
                ProductID = 222, Name = "Test"
            };
            ord.Product = p;
            ord.PurchaseOrder = o;

            ec.PurchaseOrderDetails.Add(ord);
            ((IChangeTracking)ec).AcceptChanges();
            Assert.AreEqual(1, ec.PurchaseOrders.Count);
            Assert.AreEqual(1, ec.Products.Count);
            Assert.AreEqual(1, ec.PurchaseOrderDetails.Count);

            // detach the detail
            ec.PurchaseOrderDetails.Detach(ord);
            ((IChangeTracking)ec).AcceptChanges();

            ordProd = ord.Product;
            ordOrders = ord.PurchaseOrder;

            newOrd = new PurchaseOrderDetail
            {
                PurchaseOrderID = o.PurchaseOrderID, PurchaseOrderDetailID = 222, ProductID = p.ProductID
            };
            Assert.AreEqual(0, p.PurchaseOrderDetails.Count);  // This was failing!
            newOrd.Product = ordProd;
            Assert.AreEqual(1, p.PurchaseOrderDetails.Count);
            newOrd.PurchaseOrder = ordOrders;
            Assert.AreEqual(1, p.PurchaseOrderDetails.Count);
            ec.PurchaseOrderDetails.Add(newOrd);
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.AddedEntities.Count == 1 && changeSet.ModifiedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);
        }

        /// <summary>
        /// Tests that a stale cached entity in an unloaded EntityRef is correctly
        /// reset when the cached entity is removed or detached from the source
        /// entity set.
        /// </summary>
        [TestMethod]
        public void EntityRef_Bug722053()
        {
            TestEntityContainer ec = new TestEntityContainer();
            PurchaseOrderDetail ord = new PurchaseOrderDetail
            {
                PurchaseOrderID = 111, 
                PurchaseOrderDetailID = 111, 
                ProductID = 222
            };
            PurchaseOrder o = new PurchaseOrder
            {
                PurchaseOrderID = 111
            };
            ord.PurchaseOrder = o;

            // add the 2 entities and commit
            ec.PurchaseOrderDetails.Add(ord);
            ((IChangeTracking)ec).AcceptChanges();
            Assert.AreEqual(1, ec.PurchaseOrders.Count);
            Assert.AreEqual(1, ec.PurchaseOrderDetails.Count);

            // remove the order
            ec.PurchaseOrders.Remove(o);
            ((IChangeTracking)ec).AcceptChanges();

            // verify that entity ref reflects the delete
            Assert.IsNull(ord.PurchaseOrder);

            // do the same test again, this time verifying for Detach
            ec = new TestEntityContainer();
            ord = new PurchaseOrderDetail
            {
                PurchaseOrderID = 111,
                PurchaseOrderDetailID = 111,
                ProductID = 222
            };
            o = new PurchaseOrder
            {
                PurchaseOrderID = 111
            };
            ord.PurchaseOrder = o;

            // add the 2 entities and commit
            ec.PurchaseOrderDetails.Add(ord);
            ((IChangeTracking)ec).AcceptChanges();
            Assert.AreEqual(1, ec.PurchaseOrders.Count);
            Assert.AreEqual(1, ec.PurchaseOrderDetails.Count);

            // detach the order
            ec.PurchaseOrders.Detach(o);
            ((IChangeTracking)ec).AcceptChanges();

            // verify that entity ref reflects the detach
            Assert.IsNull(ord.PurchaseOrder);
        }

        /// <summary>
        /// Previously we had an invalid Debug.Assert that was failing in the below scenario.
        /// The Assert has been removed since it was invalid, and this test repros that scenario
        /// and verifies that the issue no longer occurs.
        /// </summary>
        [TestMethod]
        public void EntityCollection_Bug722843()
        {
            TestEntityContainer ec = new TestEntityContainer();

            Product prod = new Product
            {
                ProductID = 1
            };
            PurchaseOrderDetail detail1 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1, PurchaseOrderDetailID = 1, ProductID = 1
            };
            PurchaseOrderDetail detail2 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1, PurchaseOrderDetailID = 2, ProductID = 1
            };
            ec.LoadEntities(new Entity[] { prod, detail1, detail2 });

            prod = ec.Products.First();
            Assert.AreEqual(2, prod.PurchaseOrderDetails.Count);

            // force the collection to clear by changing the parent key
            // Here we're using ApplyState to allow us to set a PK member w/o validation failure
            // since PK members cannot be changed. This test should really be based on an association
            // not involving PK, but the test is still valid this way.
            prod.ApplyState(new Dictionary<string, object> { {"ProductID", 5 } });

            // load another unrelated entity
            PurchaseOrderDetail detail = new PurchaseOrderDetail
            {
                PurchaseOrderID = 2,
                PurchaseOrderDetailID = 1,
                ProductID = 6
            };
            // this load was causing a debug assert
            ec.LoadEntities(new Entity[] { detail });

            Assert.AreEqual(0, prod.PurchaseOrderDetails.Count);
        }

        [TestMethod]
        public void EntitySet_Attach_DontLoadUnloadedRefs()
        {
            NorthwindEntityContainer entities = new NorthwindEntityContainer();

            // first load a category
            DataTests.Northwind.LTS.Category category = new DataTests.Northwind.LTS.Category
            {
                CategoryID = 1, CategoryName = "Frozen Snacks"
            };
            entities.LoadEntities(new Entity[] { category });

            // Now attach a product. When attaching, we don't expect the category
            // reference to be loaded.
            DataTests.Northwind.LTS.Product prod = new DataTests.Northwind.LTS.Product
            {
                ProductID = 1, CategoryID = 1, ProductName = "Beefcicles"
            };

            IEntityRef er = prod.GetEntityRef("Category");
            Assert.IsNull(er);
            var prodSet = entities.GetEntitySet<DataTests.Northwind.LTS.Product>();
            prodSet.Attach(prod);
            Assert.IsNull(prod.GetEntityRef("Category"));

            // now do the same test for an initialized EntityRef
            prod = new DataTests.Northwind.LTS.Product
            {
                ProductID = 2, CategoryID = 1, ProductName = "Beefy Cheezy Pockets"
            };

            Assert.IsNull(prod.Category);  // force the ref to initialize itself
            er = prod.GetEntityRef("Category");
            Assert.IsFalse(er.HasValue);
            prodSet.Attach(prod);
            Assert.IsFalse(er.HasValue);
        }

        [TestMethod]
        public void EntitySet_Attach_DontLoadUnloadedCollections()
        {
            NorthwindEntityContainer entities = new NorthwindEntityContainer();

            // first load a product
            DataTests.Northwind.LTS.Product prod = new DataTests.Northwind.LTS.Product
            {
                ProductID = 1, CategoryID = 1, ProductName = "Beefcicles"
            };
            entities.LoadEntities(new Entity[] { prod });

            // Now attach a category. When attaching, we don't expect the category
            // reference to be loaded.
            DataTests.Northwind.LTS.Category category = new DataTests.Northwind.LTS.Category
            {
                CategoryID = 1, CategoryName = "Frozen Snacks"
            };

            IEntityCollection ec = category.Products;
            Assert.IsFalse(ec.HasValues);
            var categorySet = entities.GetEntitySet<DataTests.Northwind.LTS.Category>();
            categorySet.Attach(category);
            Assert.IsFalse(ec.HasValues);
        }

        /// <summary>
        /// Test verifying that our generated code for 1:1 associations doesn't
        /// recurse infinitely.
        /// </summary>
        [TestMethod]
        public void OneToOne_AssociationSetter()
        {
            TestDomainServices.D d1 = new TestDomainServices.D
            {
                ID = 1
            };
            TestDomainServices.D d2 = new TestDomainServices.D
            {
                ID = 2
            };
            TestDomainServices.C c = new TestDomainServices.C
            {
                ID = 1,
                D_Ref1 = d1
            };

            c.D_Ref1 = d2;
            Assert.AreSame(d2, c.D_Ref1);
        }

        [TestMethod]
        public void EntityCollectionConstructorValidation()
        {
            PurchaseOrder order = new PurchaseOrder();
            ArgumentException argException = null;
            try
            {
                EntityCollection<PurchaseOrder> ec = new EntityCollection<PurchaseOrder>(order, "DNE", p => true);
            }
            catch (ArgumentException e)
            {
                argException = e;
            }
            Assert.AreEqual(new ArgumentException(string.Format(Resource.Property_Does_Not_Exist, typeof(PurchaseOrder), "DNE"), "memberName").Message, argException.Message);

            argException = null;
            try
            {
                EntityCollection<PurchaseOrder> ec = new EntityCollection<PurchaseOrder>(order, "Status", p => true);
            }
            catch (ArgumentException e)
            {
                argException = e;
            }
            Assert.AreEqual(new ArgumentException(string.Format(Resource.MemberMustBeAssociation, "Status"), "memberName").Message, argException.Message);

            ArgumentNullException argNullException = null;
            try
            {
                EntityCollection<PurchaseOrder> ec = new EntityCollection<PurchaseOrder>(null, "PurchaseOrderDetails", p => true);
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("parent").Message, argNullException.Message);

            argNullException = null;
            try
            {
                EntityCollection<PurchaseOrder> ec = new EntityCollection<PurchaseOrder>(order, null, p => true);
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("memberName").Message, argNullException.Message);

            argNullException = null;
            try
            {
                EntityCollection<PurchaseOrder> ec = new EntityCollection<PurchaseOrder>(order, "PurchaseOrderDetails", null);
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("entityPredicate").Message, argNullException.Message);

            argNullException = null;
            try
            {
                EntityCollection<PurchaseOrder> ec = new EntityCollection<PurchaseOrder>(order, "PurchaseOrderDetails", p => true, null, p => p.GetType());
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("attachAction").Message, argNullException.Message);

            argNullException = null;
            try
            {
                EntityCollection<PurchaseOrder> ec = new EntityCollection<PurchaseOrder>(order, "PurchaseOrderDetails", p => true, p => p.GetType(), null);
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("detachAction").Message, argNullException.Message);
        }

        [TestMethod]
        public void EntityRefConstructorValidation()
        {
            PurchaseOrderDetail detail = new PurchaseOrderDetail();
            ArgumentException argException = null;
            try
            {
                EntityRef<Product> er = new EntityRef<Product>(detail, "DNE", p => true);
            }
            catch (ArgumentException e)
            {
                argException = e;
            }
            Assert.AreEqual(new ArgumentException(string.Format(Resource.Property_Does_Not_Exist, typeof(PurchaseOrderDetail), "DNE"), "memberName").Message, argException.Message);

            argException = null;
            try
            {
                EntityRef<Product> er = new EntityRef<Product>(detail, "DueDate", p => true);
            }
            catch (ArgumentException e)
            {
                argException = e;
            }
            Assert.AreEqual(new ArgumentException(string.Format(Resource.MemberMustBeAssociation, "DueDate"), "memberName").Message, argException.Message);

            ArgumentNullException argNullException = null;
            try
            {
                EntityRef<Product> er = new EntityRef<Product>(null, "Product", p => true);
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("parent").Message, argNullException.Message);

            argNullException = null;
            try
            {
                EntityRef<Product> er = new EntityRef<Product>(detail, null, p => true);
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("memberName").Message, argNullException.Message);

            argNullException = null;
            try
            {
                EntityRef<Product> er = new EntityRef<Product>(detail, "Product", null);
            }
            catch (ArgumentNullException e)
            {
                argNullException = e;
            }
            Assert.AreEqual(new ArgumentNullException("entityPredicate").Message, argNullException.Message);
        }

        /// <summary>
        /// Verify that when a member of the ThisKey changes on a parent entity, that
        /// its dependent EntityCollections are Reset
        /// </summary>
        [TestMethod]
        public void EntityCollection_ParentKeyModification()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            ec.Products.Clear();

            Product p1 = new Product
            {
                ProductID = 1
            };
            Product p2 = new Product
            {
                ProductID = 2
            };
            Product p3 = new Product
            {
                ProductID = 3
            };
            PurchaseOrderDetail d1 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1,
                PurchaseOrderDetailID = 1,
                ProductID = 1
            };
            PurchaseOrderDetail d2 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1,
                PurchaseOrderDetailID = 2,
                ProductID = 2
            };
            PurchaseOrderDetail d3 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1,
                PurchaseOrderDetailID = 3,
                ProductID = 2
            };
            PurchaseOrderDetail d4 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1,
                PurchaseOrderDetailID = 4,
                ProductID = 3
            };
            ec.LoadEntities(new Entity[] { p1, p2, p3, d1, d2, d3, d4 });

            NotifyCollectionChangedEventArgs args1 = null;
            ((INotifyCollectionChanged)p1.PurchaseOrderDetails).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                args1 = e;
            };

            Assert.AreEqual(1, p1.PurchaseOrderDetails.Count);
            Assert.AreEqual(2, p2.PurchaseOrderDetails.Count);

            // now change p1's ProductID to 0 - expect the cached
            // entities to be reset, and a collection reset notification
            args1 = null;
            // Here we're using ApplyState to allow us to set a PK member w/o validation failure
            // since PK members cannot be changed. This test should really be based on an association
            // not involving PK, but the test is still valid this way.
            p1.ApplyState(new Dictionary<string, object> { { "ProductID", 0 } });
            Assert.AreEqual(0, p1.PurchaseOrderDetails.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, args1.Action);

            // now change to 3 - expect to get one detail, and a collection reset notification
            args1 = null;
            p1.ApplyState(new Dictionary<string, object> { { "ProductID", 3 } });
            Assert.AreEqual(1, p1.PurchaseOrderDetails.Count);
            Assert.IsTrue(p1.PurchaseOrderDetails.Contains(d4));
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, args1.Action);
        }

        /// <summary>
        /// Ensure that if the fk member(s) governing an EntityCollection
        /// change, the EntityCollection raises a Reset notification
        /// Related bug: 679626
        /// </summary>
        [TestMethod]
        public void EntityCollection_FKUpdateEntityCollectionResetNotification()
        {
            Cities.CityData cities = new Cities.CityData();
            Cities.CityDomainContext ctxt = new Cities.CityDomainContext(TestURIs.Cities);

            ctxt.EntityContainer.LoadEntities(cities.States);
            ctxt.EntityContainer.LoadEntities(cities.Counties);

            Cities.State state = ctxt.States.Single(p => p.Name == "WA");
            Assert.AreEqual(3, state.Counties.Count);

            List<NotifyCollectionChangedEventArgs> collectionChangedArgs = new List<NotifyCollectionChangedEventArgs>();
            ((INotifyCollectionChanged)state.Counties).CollectionChanged += (s, e) =>
                {
                    collectionChangedArgs.Add(e);
                };

            // Here we're using ApplyState to allow us to set a PK member w/o validation failure
            // since PK members cannot be changed. This test should really be based on an association
            // not involving PK, but the test is still valid this way.
            state.ApplyState(new Dictionary<string, object> { {"Name", "HI"} });

            Assert.AreEqual(0, state.Counties.Count);

            Assert.AreEqual(1, collectionChangedArgs.Count);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, collectionChangedArgs.Single().Action);
        }

        [TestMethod]
        public void EntityCollection_FKMembershipUpdates()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<PurchaseOrder> orders = ec.GetEntitySet<PurchaseOrder>();
            EntitySet<Employee> employees = ec.GetEntitySet<Employee>();

            Employee e1 = new Employee
            {
                EmployeeID = 1
            };
            Employee e2 = new Employee
            {
                EmployeeID = 2
            };
            PurchaseOrder o1 = new PurchaseOrder
            {
                PurchaseOrderID = 1,
                EmployeeID = 1
            };
            PurchaseOrder o2 = new PurchaseOrder
            {
                PurchaseOrderID = 2,
                EmployeeID = 1
            };
            PurchaseOrder o3 = new PurchaseOrder
            {
                PurchaseOrderID = 3,
                EmployeeID = 1
            };
            PurchaseOrder o4 = new PurchaseOrder
            {
                PurchaseOrderID = 4,
                EmployeeID = 2
            };
            ec.LoadEntities(new Entity[] { e1, e2, o1, o2, o3, o4 });

            // TODO : test with both loaded and unloaded ECs

            List<NotifyCollectionChangedEventArgs> ordersChangedArgs = new List<NotifyCollectionChangedEventArgs>();
            ((INotifyCollectionChanged)e1.PurchaseOrders).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                Assert.AreSame(e1.PurchaseOrders, sender);
                ordersChangedArgs.Add(e);
            };

            List<NotifyCollectionChangedEventArgs> reportsChangedArgs = new List<NotifyCollectionChangedEventArgs>();
            ((INotifyCollectionChanged)e1.Reports).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                Assert.AreSame(e1.Reports, sender);
                reportsChangedArgs.Add(e);
            };

            // verify that if an entity in the set changes FKs such that
            // it is now a member of an EntityCollection, the EC is updated
            // automatically
            Assert.AreEqual(3, e1.PurchaseOrders.Count);
            o4.EmployeeID = e1.EmployeeID;
            Assert.AreEqual(4, e1.PurchaseOrders.Count);
            NotifyCollectionChangedEventArgs args = ordersChangedArgs.Single();
            Assert.AreEqual(NotifyCollectionChangedAction.Add, args.Action);
            Assert.AreSame(o4, args.NewItems.Cast<PurchaseOrder>().Single());

            // verify that if an entity in an EC changes FKs such that it
            // is no longer a member of the EC, it is removed from the EC
            // automatically
            ordersChangedArgs.Clear();
            o4.EmployeeID = e2.EmployeeID;  // remove 2 from the set by changing FKs
            PurchaseOrder o5 = e1.PurchaseOrders.First();
            o5.EmployeeID = e2.EmployeeID;
            Assert.AreEqual(2, e1.PurchaseOrders.Count);
            Assert.AreEqual(2, e2.PurchaseOrders.Count);
            // ensure we got both events
            args = ordersChangedArgs[0];
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
            Assert.AreSame(o4, args.OldItems.Cast<PurchaseOrder>().Single());
            args = ordersChangedArgs[1];
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
            Assert.AreSame(o5, args.OldItems.Cast<PurchaseOrder>().Single());

            // now detach the parent and ensure that we no longer get notifications
            ordersChangedArgs.Clear();
            ec.Employees.Detach(e1);
            ec.Employees.Detach(e2);
            o4.EmployeeID = e1.EmployeeID;
            Assert.AreEqual(0, ordersChangedArgs.Count);
            Assert.AreEqual(2, e1.PurchaseOrders.Count);
            Assert.AreEqual(2, e2.PurchaseOrders.Count);

            // now reattach and verify we get notifications again
            ec.Employees.Attach(e1);
            ec.Employees.Attach(e2);
            o5 = e1.PurchaseOrders.First();
            o5.Employee = e2;  // set via reference (which sets the FK)
            args = ordersChangedArgs.Single();
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
            Assert.AreSame(o5, args.OldItems.Cast<PurchaseOrder>().Single());

            #region Test multiple EntityCollection notifications
            // verify notifications for an entity with multiple EntityCollections
            // we've already verified we're getting notifications for Employee.PurchaseOrders,
            // now verify Employee.Reports
            Assert.AreEqual(0, e1.Reports.Count);
            Employee report1 = new Employee
            {
                EmployeeID = 3,
                ManagerID = e1.EmployeeID
            };
            Employee report2 = new Employee
            {
                EmployeeID = 4,
                ManagerID = e1.EmployeeID
            };
            Assert.AreEqual(0, reportsChangedArgs.Count);  // don't expect any until the reports are attached
            ec.LoadEntities(new Employee[] { report1, report2 });
            args = reportsChangedArgs[0];
            Assert.AreEqual(NotifyCollectionChangedAction.Add, args.Action);
            Assert.AreSame(report1, args.NewItems.Cast<Employee>().Single());
            args = reportsChangedArgs[1];
            Assert.AreEqual(NotifyCollectionChangedAction.Add, args.Action);
            Assert.AreSame(report2, args.NewItems.Cast<Employee>().Single());
            Assert.AreEqual(2, e1.Reports.Count);

            // now modify one of the FKs and verify notification
            reportsChangedArgs.Clear();
            report1.Manager = null;
            args = reportsChangedArgs.Single();
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, args.Action);
            Assert.AreSame(report1, args.OldItems.Cast<Employee>().Single());
            Assert.AreEqual(1, e1.Reports.Count);
            #endregion

        }

        /// <summary>
        /// Ensure that if the fk member(s) governing an EntityRef
        /// change, the EntityRef raises a property changed notification
        /// Related bug: 679626
        /// </summary>
        [TestMethod]
        public void EntityRef_FKUpdateEntityRefChangeNotification()
        {
            Cities.CityData cities = new Cities.CityData();
            Cities.CityDomainContext ctxt = new Cities.CityDomainContext(TestURIs.Cities);

            ctxt.EntityContainer.LoadEntities(cities.Zips);
            ctxt.EntityContainer.LoadEntities(cities.Cities);

            Cities.Zip zip = ctxt.Zips.First(p => p.Code == 98052);
            Assert.AreEqual("Redmond", zip.City.Name);

            List<PropertyChangedEventArgs> propChangedArgs = new List<PropertyChangedEventArgs>();
            ((INotifyPropertyChanged)zip).PropertyChanged += (s, e) =>
            {
                propChangedArgs.Add(e);

                // simulate data binding for the association member
                // by reloading whenever it changes
                if (e.PropertyName == "City")
                {
                    Cities.City tmpCity = zip.City;
                }

            };

            Cities.City city = zip.City; // cause the ref to load
            zip.CityName = "Issaquah";

            Assert.IsNull(zip.City);

            Assert.AreEqual(1, propChangedArgs.Count(p => p.PropertyName == "City"));

            // test the multipart key scenario where all key members
            // are set in sequence CityName/CountyName/StateName is the multipart key
            city = zip.City; // cause the ref to load
            city = cities.Cities.First(p => p.Name == "Toledo");
            propChangedArgs.Clear();
            zip.CityName = city.Name;
            zip.CountyName = city.CountyName;
            zip.StateName = city.StateName;
            Assert.AreSame(city, zip.City);

            // Ideally we'd have a way to limit the number of change events in this scenario.
            Assert.AreEqual(3, propChangedArgs.Count(p => p.PropertyName == "City"));
        }

        [TestMethod]
        public void EntityRef_FKModificationUpdateNotifications()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<PurchaseOrder> orders = ec.GetEntitySet<PurchaseOrder>();

            PurchaseOrder order = new PurchaseOrder
            {
                PurchaseOrderID = 1
            };
            PurchaseOrder order2 = new PurchaseOrder
            {
                PurchaseOrderID = 2
            };
            PurchaseOrderDetail detail = new PurchaseOrderDetail
            {
                PurchaseOrderDetailID = 1,
                PurchaseOrderID = 1
            };
            ec.LoadEntities(new Entity[] { order, order2, detail });

            int notifications = 0;
            detail.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "PurchaseOrder")
                {
                    notifications++;
                }
            };

            Assert.AreSame(order, detail.PurchaseOrder);

            // now modify the detail's OrderID - we expect a change notification,
            // and when we access the ref, we expect the correct entity
            Assert.AreEqual(0, notifications);
            // Here we're using ApplyState to allow us to set a PK member w/o validation failure
            // since PK members cannot be changed. This test should really be based on an association
            // not involving PK, but the test is still valid this way.
            detail.ApplyState(new Dictionary<string, object> { { "PurchaseOrderID", 2 } });
            Assert.AreEqual(1, notifications);
            Assert.AreSame(order2, detail.PurchaseOrder);

            // now change the OrderID on referenced entity - we expect a change notification,
            // and when we access the ref, we expect null (since Order(3) does not exist)
            notifications = 0;
            order2.ApplyState(new Dictionary<string, object> { { "PurchaseOrderID", 3 } });
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(null, detail.PurchaseOrder);
        }

        /// <summary>
        /// In the original repro, since after the report was moved, the ManagerID
        /// was still 0, it matches both EmployeeIDs still, so still shows up in
        /// the entity collections
        /// </summary>
        [TestMethod]
        public void Bug504822_OriginalRepro()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Employee> employees = ec.GetEntitySet<Employee>();

            Employee e2 = new Employee
            {
                LoginID = "e2"
            };
            Employee e3 = new Employee();
            Employee e4 = new Employee();
            Employee e5 = new Employee
            {
                LoginID = "e5"
            };

            employees.Add(e2);
            employees.Add(e3);
            employees.Add(e4);
            employees.Add(e5);

            // add two Reports to e2
            e2.Reports.Add(e3);
            e2.Reports.Add(e4);

            Assert.AreEqual(2, e2.Reports.Count);

            // now move one report to employee e5
            e5.Reports.Add(e4);

            Assert.AreEqual(1, e2.Reports.Count);
            Assert.AreEqual(1, e5.Reports.Count);
        }

        [TestMethod]
        public void Bug525514_EntityRef_PropertyChangedNotification()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Employee> employees = ec.GetEntitySet<Employee>();
            EntitySet<PurchaseOrder> orders = ec.GetEntitySet<PurchaseOrder>();

            PurchaseOrder order = new PurchaseOrder
            {
                PurchaseOrderID = 1,
                EmployeeID = 1
            };
            Employee emp = new Employee
            {
                EmployeeID = 1
            };

            int eventCount = 0;
            order.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Employee")
                {
                    eventCount++;
                }
            };

            ec.LoadEntities(new PurchaseOrder[] { order });
            Assert.IsNull(order.Employee);
            Assert.AreEqual(0, eventCount);

            // now load the employee into the set - we expect a change notification
            order.EmployeeID = 1;
            employees.LoadEntity(emp);   // simulate a query result coming in
            Assert.AreSame(emp, order.Employee);
            Assert.AreEqual(1, eventCount);

            // remove the entity from the set - we expect a change notification again
            employees.Remove(emp);
            Assert.IsNull(order.Employee);
            Assert.AreEqual(2, eventCount);

            // add the entity back to the source set - then clear the set,
            // verifying that we get the expected notifications
            ((IRevertibleChangeTracking)employees).AcceptChanges();
            eventCount = 0;
            employees.LoadEntity(emp);
            Assert.AreSame(emp, order.Employee);
            Assert.AreEqual(1, eventCount);
            employees.Clear();
            Assert.IsNull(order.Employee);
            Assert.AreEqual(2, eventCount);

            // verify that directly modifying the entity reference by
            // setting it directly raises change notifications
            order.EmployeeID = 0;
            eventCount = 0;
            order.Employee = emp;
            Assert.AreSame(emp, order.Employee);
            Assert.AreEqual(2, eventCount);
            order.Employee = null;
            Assert.AreEqual(4, eventCount);

            // load the entity again and make sure our cached null value is reset
            employees.Detach(emp);
            order.EmployeeID = 1;
            Assert.AreEqual(5, eventCount);
            employees.LoadEntity(emp);
            Assert.AreSame(emp, order.Employee);
            Assert.AreEqual(6, eventCount);

            // remove the referenced entity from the set - we expect a change notification
            employees.Detach(emp);
            Assert.IsNull(order.Employee);
            Assert.AreEqual(7, eventCount);

            // add the entity back to the source set - no update since
            // the entity is new
            eventCount = 0;
            employees.Add(emp);
            Assert.IsNull(order.Employee);
            Assert.AreEqual(0, eventCount);

            // remove the parent entity from its set and verify that
            // we no longer receive notifications
            eventCount = 0;
            employees.Detach(emp);
            orders.Remove(order);
            employees.LoadEntity(emp);
            Assert.IsNull(order.Employee);
            Assert.AreEqual(0, eventCount);
        }

        [TestMethod]
        public void Bug527565_EntityCollection_NotifyCollectionChanged()
        {
            PurchaseOrder order = new PurchaseOrder();

            List<NotifyCollectionChangedEventArgs> events = new List<NotifyCollectionChangedEventArgs>();
            ((INotifyCollectionChanged)order.PurchaseOrderDetails).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                Assert.AreSame(order.PurchaseOrderDetails, sender);
                events.Add(e);
            };

            PurchaseOrderDetail detail = new PurchaseOrderDetail();
            order.PurchaseOrderDetails.Add(detail);
            NotifyCollectionChangedEventArgs raisedEvent = events.Single();
            Assert.AreEqual(NotifyCollectionChangedAction.Add, raisedEvent.Action);
            Assert.AreSame(detail, raisedEvent.NewItems.Cast<PurchaseOrderDetail>().Single());

            events.Clear();
            order.PurchaseOrderDetails.Remove(detail);
            raisedEvent = events.Single();
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, raisedEvent.Action);
            Assert.AreSame(detail, raisedEvent.OldItems.Cast<PurchaseOrderDetail>().Single());
        }

        /// <summary>
        /// Ensure that property change notifications for HasChanges
        /// </summary>
        [TestMethod]
        public void Bug507257_Entity_HasChangesUpdateNotification()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product prod = products.First();

            int hasChangesNotifications = 0;
            int entityStateNotifications = 0;
            prod.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "HasChanges")
                {
                    hasChangesNotifications++;
                }
                else if (e.PropertyName == "EntityState")
                {
                    entityStateNotifications++;
                }
            };

            string origColor = prod.Color;
            prod.Color = "blue";
            Assert.AreEqual(1, hasChangesNotifications);
            Assert.AreEqual(1, entityStateNotifications);
            Assert.IsTrue(prod.HasChanges);
            Assert.AreEqual(EntityState.Modified, prod.EntityState);

            // Now revert to the original value and verify that
            // we do not get a notification for HasChanges, that HasChanges
            // is true, and the entity state remains Modified
            prod.Color = origColor;
            Assert.AreEqual(1, hasChangesNotifications);
            Assert.AreEqual(1, entityStateNotifications);
            Assert.IsTrue(prod.HasChanges);
            Assert.AreEqual(EntityState.Modified, prod.EntityState);
        }

        [TestMethod]
        public void EntitySet_CanOnlyAddExactEntityType()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            ExceptionHelper.ExpectArgumentException(delegate
            {
                products.Add(new Employee());
            }, new ArgumentException(string.Format(Resource.EntitySet_Wrong_Type, typeof(Product), typeof(Employee)), "entity").Message);
        }

        /// <summary>
        /// Here we're testing the scenario where an entity with the same key as an existing
        /// entity is added, after the first is removed.
        /// </summary>
        [TestMethod]
        public void EntitySet_Delete_Add_AcceptChanges()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            products.Clear();
            ((IRevertibleChangeTracking)products).AcceptChanges();

            var existingProduct = new Product() { ProductID = 1 };
            products.Attach(existingProduct);
            products.Remove(existingProduct);

            // we don't expect an exception here since cache entry with the
            // duplicate ID is in the Deleted state prior to this addition
            products.Add(new Product() { ProductID = existingProduct.ProductID });

            // This shouldn't throw an exception from the identity cache.
            ((IRevertibleChangeTracking)products).AcceptChanges();
        }

        /// <summary>
        /// Verifies the ordering of events across EntitySet and Entity.
        /// </summary>
        [TestMethod]
        [WorkItem(814122)]
        public void EntitySet_EventOrdering()
        {
            EventingTestDataContext ctxt = new EventingTestDataContext();
            TestEntityContainer ec = (TestEntityContainer)ctxt.EntityContainer;
            EntitySet<PurchaseOrder> poSet = ec.PurchaseOrders;
            EntitySet<Employee> employeeSet = ec.Employees;

            PurchaseOrder po = new PurchaseOrder() { PurchaseOrderID = 1, EmployeeID = 1 };
            ec.LoadEntities(new PurchaseOrder[] { po });

            string events = String.Empty;
            ((INotifyCollectionChanged)employeeSet).CollectionChanged += (s, e) => events += "EmployeeSet" + e.Action;
            po.PropertyChanged += (s, e) => events += e.PropertyName;

            // Access po.Employee to force internal event subscriptions.
            Assert.IsNull(po.Employee, "Employee should be null.");

            Employee employee = new Employee() { EmployeeID = 1 };
            employee.PropertyChanged += (s, e) => events += e.PropertyName;
            ec.LoadEntities(new Employee[] { employee });

            // Verify the order is: Employee.PropertyChanged, EntitySet<Employee>.CollectionChanged, PurchaseOrder.PropertyChanged.
            Assert.AreEqual("EntityStateEmployeeSetAddEmployee", events, "Events are ordered incorrectly.");

            events = String.Empty;

            Employee newEmployee = new Employee();
            newEmployee.PropertyChanged += (s, e) => events += e.PropertyName;
            employeeSet.Add(newEmployee);

            // Verify the order is: Employee.PropertyChanged, EntitySet<Employee>.CollectionChanged.
            Assert.AreEqual("EntityStateEmployeeSetAdd", events);
        }

        /// <summary>
        /// Verify for EntitySet Adds/Removes that the entity state is updated
        /// BEFORE the HasChanges event is fired.
        /// </summary>
        [TestMethod]
        public void EntitySet_HasChangesEventOrdering()
        {
            EventingTestDataContext ctxt = new EventingTestDataContext();
            TestEntityContainer ec = (TestEntityContainer)ctxt.EntityContainer;
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();
            Product entity = null;
            EntityOperationType operation = EntityOperationType.Insert;
            bool expectChanges = false;

            #region Event subscriptions
            prodSet.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                // When transitioning to HasChanges == true, verify that the modified
                // entity state is updated prior to the event being raised
                if (e.PropertyName == "HasChanges" && expectChanges)
                {
                    // verify that when the event is raised, recomputation of HasChanges
                    // returns true, verifying that the entity state has been updated
                    // prior to the event being raised
                    Assert.IsTrue(prodSet.HasChanges);

                    if (operation == EntityOperationType.Insert)
                    {
                        Assert.AreEqual(EntityState.New, entity.EntityState);
                    }
                    else if (operation == EntityOperationType.Delete)
                    {
                        Assert.AreEqual(EntityState.Deleted, entity.EntityState);
                    }
                    else if (operation == EntityOperationType.Update)
                    {
                        Assert.AreEqual(EntityState.Modified, entity.EntityState);
                    }
                }
            };

            ec.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "HasChanges" && expectChanges)
                {
                    // verify that when the EntityContainer event is raised, HasChanges on the
                    // modified set is updated already
                    Assert.IsTrue(prodSet.HasChanges);
                }
            };

            ctxt.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "HasChanges" && expectChanges)
                {
                    // verify that when the DomainContext event is raised, HasChanges on the
                    // modified set is updated already
                    Assert.IsTrue(prodSet.HasChanges);
                }
            };
            #endregion

            // test insert
            expectChanges = true;
            operation = EntityOperationType.Insert;
            entity = new Product
            {
                ProductID = 1234
            };
            prodSet.Add(entity);

            // test delete
            expectChanges = false;
            ((IRevertibleChangeTracking)prodSet).RejectChanges();
            expectChanges = true;
            operation = EntityOperationType.Delete;
            entity = prodSet.ToArray()[5];
            Assert.AreEqual(EntityState.Unmodified, entity.EntityState);
            prodSet.Remove(entity);

            // test edit
            expectChanges = false;
            ((IRevertibleChangeTracking)prodSet).RejectChanges();
            expectChanges = true;
            operation = EntityOperationType.Update;
            entity = prodSet.ToArray()[5];
            Assert.AreEqual(EntityState.Unmodified, entity.EntityState);
            entity.Color += "x";
        }

        /// <summary>
        /// Test INPC events for HasChanges and Count properties all the way
        /// throught the stack : DomainContext/EntityContainer/EntitySet to ensure
        /// events are propigated properly.
        /// </summary>
        [TestMethod]
        public void EntityContainer_INotifyPropertyChanging_ChangeTracking()
        {
            EventingTestDataContext ctxt = new EventingTestDataContext();
            TestEntityContainer ec = (TestEntityContainer)ctxt.EntityContainer;
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();
            EntitySet<PurchaseOrder> orderSet = ec.GetEntitySet<PurchaseOrder>();

            #region Event Subscriptions
            int setCountEventTotal = 0;
            int setHasChangesEventTotal = 0;
            PropertyChangedEventHandler setPropertyChangedHandler = delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Count")
                {
                    setCountEventTotal++;
                }
                else if (e.PropertyName == "HasChanges")
                {
                    setHasChangesEventTotal++;
                }
            };
            prodSet.PropertyChanged += setPropertyChangedHandler;
            orderSet.PropertyChanged += setPropertyChangedHandler;

            int containerHasChangedEventTotal = 0;
            ec.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "HasChanges")
                {
                    containerHasChangedEventTotal++;
                }
            };

            int contextHasChangedEventTotal = 0;
            ctxt.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "HasChanges")
                {
                    contextHasChangedEventTotal++;
                }
            };
            #endregion

            Func<int, int, int, int, bool> VerifyChangeEvents = delegate(int expectedSetCountTotal, int expectedSetHasChangedTotal, int expectedContainerHasChangedTotal, int expectedContextHasChangedTotal)
            {
                Assert.AreEqual(expectedSetCountTotal, setCountEventTotal);

                Assert.AreEqual(expectedSetHasChangedTotal, setHasChangesEventTotal);

                Assert.AreEqual(expectedContainerHasChangedTotal, containerHasChangedEventTotal);

                Assert.AreEqual(expectedContextHasChangedTotal, contextHasChangedEventTotal);

                // DomainContext.HasChanges should always equal EntityContainer.HasChanges
                Assert.AreEqual(ctxt.HasChanges, ec.HasChanges);

                // EntityContainer.HasChanges should only be true if any of it's EntitySets have changes
                Assert.AreEqual(ec.HasChanges, ec.EntitySets.Any(p => p.HasChanges));

                return true;
            };

            Action ResetEventTotals = delegate
            {
                setCountEventTotal = setHasChangesEventTotal = 0;
                containerHasChangedEventTotal = 0;
                contextHasChangedEventTotal = 0;
            };

            #region Verify EntitySet Add/Remove eventing
            // add to the set
            Product newProd1 = new Product { ProductID = 1001 };
            prodSet.Add(newProd1);
            Product newProd2 = new Product { ProductID = 1002 };
            prodSet.Add(newProd2);

            // verify events on Add
            VerifyChangeEvents(2, 1, 1, 1);
            ResetEventTotals();

            // verify events on Remove
            prodSet.Remove(newProd1);
            VerifyChangeEvents(1, 0, 0, 0);

            prodSet.Remove(newProd2);
            // only get the change event when the last add is reverted
            VerifyChangeEvents(2, 1, 1, 1);
            ResetEventTotals();

            // verify Attach/Detach eventing - we expect the count event to
            // be raised, but not HasChanges, since entities are attached 
            // as unmodified
            Product attachProd1 = new Product { ProductID = 1234 };
            Product attachProd2 = new Product { ProductID = 1235 };
            prodSet.Attach(attachProd1);
            prodSet.Attach(attachProd2);
            VerifyChangeEvents(2, 0, 0, 0);

            // if we modify one of the attached entities, we expect to
            // get the change event
            attachProd1.Color += "x";
            VerifyChangeEvents(2, 1, 1, 1);
            attachProd2.Color += "x";
            VerifyChangeEvents(2, 1, 1, 1);
            ((IRevertibleChangeTracking)attachProd1).RejectChanges();
            VerifyChangeEvents(2, 1, 1, 1);
            ((IRevertibleChangeTracking)attachProd2).RejectChanges();
            VerifyChangeEvents(2, 2, 2, 2);

            // the entities are no longer modified, so we expect
            // only to see count events
            prodSet.Detach(attachProd1);
            VerifyChangeEvents(3, 2, 2, 2);
            prodSet.Detach(attachProd2);
            VerifyChangeEvents(4, 2, 2, 2);
            #endregion

            #region Verify Entity edit eventing
            ResetEventTotals();

            // modify an entity in the set
            Product[] products = prodSet.ToArray();
            Product p2 = products[4];
            Product p3 = products[5];
            p2.Color += "x";
            p3.Color += "x";
            VerifyChangeEvents(0, 1, 1, 1);

            ((IRevertibleChangeTracking)p2).RejectChanges();
            VerifyChangeEvents(0, 1, 1, 1);

            ((IRevertibleChangeTracking)p3).RejectChanges();
            VerifyChangeEvents(0, 2, 2, 2);

            p2.Color += "x";
            p3.Color += "x";
            VerifyChangeEvents(0, 3, 3, 3);

            // when changes are accepted, we get a change event
            // as HasChanges transitions to false
            ((IRevertibleChangeTracking)prodSet).AcceptChanges();
            VerifyChangeEvents(0, 4, 4, 4);
            #endregion

            #region Verify multi-set eventing
            // Verify dirty notifications across multiple sets
            ResetEventTotals();

            PurchaseOrder order = new PurchaseOrder { PurchaseOrderID = 1 };
            orderSet.Add(order);
            VerifyChangeEvents(1, 1, 1, 1);

            // adding to another set shouldn't raise HasChanges events
            // for the EntityCollection or DomainContext, but should raise
            // HasChanged for the EntitySet
            prodSet.Add(newProd1);
            VerifyChangeEvents(2, 2, 1, 1);

            orderSet.Remove(order);
            VerifyChangeEvents(3, 3, 1, 1);

            prodSet.Remove(newProd1);
            VerifyChangeEvents(4, 4, 2, 2);
            #endregion

            #region Verify Clear eventing
            // when an entity set has changes, then is cleared, we expect to get a HasChanges
            // update notification
            ResetEventTotals();

            prodSet.Add(newProd1);
            VerifyChangeEvents(1, 1, 1, 1);

            prodSet.Clear();
            VerifyChangeEvents(2, 2, 2, 2);

            // if an empty set is cleared, we don't expect any notifications
            Assert.IsFalse(ctxt.HasChanges);
            ResetEventTotals();
            prodSet.Clear();
            VerifyChangeEvents(0, 0, 0, 0);
            #endregion
        }

        [TestMethod]
        public void EntityContainer_GraphSupport()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<PurchaseOrder> orders = ec.GetEntitySet<PurchaseOrder>();
            EntitySet<PurchaseOrderDetail> details = ec.GetEntitySet<PurchaseOrderDetail>();

            PurchaseOrder order = new PurchaseOrder();

            // add some details
            order.PurchaseOrderDetails.Add(new PurchaseOrderDetail
            {
                ProductID = 200
            });
            order.PurchaseOrderDetails.Add(new PurchaseOrderDetail
            {
                ProductID = 201
            });
            order.PurchaseOrderDetails.Add(new PurchaseOrderDetail
            {
                ProductID = 202
            });

            // verify the EntityCollection count
            Assert.AreEqual(3, order.PurchaseOrderDetails.Count());

            // verify the new Order is in the entity set
            orders.Add(order);
            Assert.IsTrue(orders.Contains(order));

            // add all the details explicitly (not necessary, but for testing)
            foreach (PurchaseOrderDetail detail in order.PurchaseOrderDetails)
            {
                details.Add(detail);
            }

            // verify the changeset is as expected
            EntityChangeSet changeSet = ec.GetChanges();
            Assert.AreEqual(4, changeSet.AddedEntities.Count);
            Assert.IsTrue(changeSet.AddedEntities.Contains(order));
            foreach (PurchaseOrderDetail detail in order.PurchaseOrderDetails)
            {
                Assert.IsTrue(changeSet.AddedEntities.Contains(detail));
            }

            ((IRevertibleChangeTracking)ec).RejectChanges();

            // verify the changeset is as expected
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.IsEmpty);
        }

        [TestMethod]
        public void EntityContainer_ComplexPropertyChanges()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<MixedType> mixedTypes = ec.GetEntitySet<MixedType>();

            var obj = new MixedType()
            {
                ID = "X",
                IntsProp = new int[0],
                StringsProp = new string[] { "hello" }
            };
            mixedTypes.Attach(obj);

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.IsEmpty);

            obj.StringsProp = new string[] { "hello", "world" };

            changeSet = ec.GetChanges();
            Assert.IsFalse(changeSet.IsEmpty);

            Assert.AreEqual(1, changeSet.ModifiedEntities.Count);
            Assert.AreEqual(obj, changeSet.ModifiedEntities[0]);
        }

        /// <summary>
        /// Verify that EntityContainer.AcceptChanges transitions all modified entity
        /// state properly
        /// </summary>
        [TestMethod]
        public void EntityContainer_AcceptChanges()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();

            // modify an entity
            Product[] products = prodSet.ToArray();
            Product modifiedProd = products[3];
            modifiedProd.Class += "x";

            // remove an entity
            Product removedProd = products[7];
            prodSet.Remove(removedProd);

            // add some entities
            Product newProd1 = new Product
            {
                ProductID = 333
            };
            prodSet.Add(newProd1);
            Product newProd2 = new Product
            {
                ProductID = 334
            };
            prodSet.Add(newProd2);

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.AreEqual(1, changeSet.ModifiedEntities.Count);
            Assert.AreEqual(2, changeSet.AddedEntities.Count);
            Assert.AreEqual(1, changeSet.RemovedEntities.Count);

            ((IRevertibleChangeTracking)ec).AcceptChanges();

            // verify that there are no more pending changes
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.IsEmpty);

            // verify that all the entities have state transitioned properly
            Assert.AreEqual(EntityState.Unmodified, modifiedProd.EntityState);
            Assert.AreEqual(EntityState.Unmodified, newProd1.EntityState);
            Assert.AreEqual(EntityState.Unmodified, newProd2.EntityState);
            Assert.AreEqual(EntityState.Detached, removedProd.EntityState);

            // verify that the new entities is now being tracked
            newProd1.Color += "x";
            Assert.AreEqual(EntityState.Modified, newProd1.EntityState);
            Assert.IsTrue(ec.GetChanges().ModifiedEntities.Contains(newProd1));

            // verify that the other entities are still being tracked
            modifiedProd.Color += "x";
            Assert.AreEqual(EntityState.Modified, modifiedProd.EntityState);
            Assert.IsTrue(ec.GetChanges().ModifiedEntities.Contains(modifiedProd));

            ((IRevertibleChangeTracking)ec).RejectChanges();
            Assert.IsTrue(ec.GetChanges().IsEmpty);
        }

        [TestMethod]
        public void EntityCollection_Caching()
        {
            TestEntityContainer ec = new TestEntityContainer();

            PurchaseOrder order = new PurchaseOrder()
            {
                PurchaseOrderID = 1
            };
            ec.PurchaseOrders.Add(order);

            // add some details
            for (int i = 1; i <= 20; i++)
            {
                ec.PurchaseOrderDetails.Add(
                    new PurchaseOrderDetail
                    {
                        PurchaseOrderID = 1,
                        PurchaseOrderDetailID = i
                    }
                );
            }

            ((IRevertibleChangeTracking)ec).AcceptChanges();

            // verify that the cached set is empty before it is forced to load
#if !SILVERLIGHT // can't do private reflection in SL
            IList cachedEntities = (IList)order.PurchaseOrderDetails.GetType().GetProperty("Entities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(order.PurchaseOrderDetails, null);
            Assert.AreEqual(0, cachedEntities.Count);
#endif

            // this first call to Count will force the collection to load
            Assert.AreEqual(20, order.PurchaseOrderDetails.Count);

#if !SILVERLIGHT
            // verify that the cached set is now loaded
            cachedEntities = (IList)order.PurchaseOrderDetails.GetType().GetProperty("Entities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(order.PurchaseOrderDetails, null);
            Assert.AreEqual(20, cachedEntities.Count);
#endif

            // verify that whenever new entities are added to the source EntitySet
            // (simulating client queries via LoadEntity below), they are also added to the cached set.
            PurchaseOrderDetail detail = new PurchaseOrderDetail { PurchaseOrderID = order.PurchaseOrderID, PurchaseOrderDetailID = 21 };
            ec.PurchaseOrderDetails.LoadEntity(detail);
            Assert.AreEqual(21, order.PurchaseOrderDetails.Count);
#if !SILVERLIGHT
            Assert.AreEqual(21, cachedEntities.Count);
#endif

            // verify that whenever entities are removed to the source EntitySet,
            // they are also removed to the cached set
            ec.PurchaseOrderDetails.Remove(detail);
            Assert.AreEqual(20, order.PurchaseOrderDetails.Count);
#if !SILVERLIGHT
            Assert.AreEqual(20, cachedEntities.Count);
#endif

            // verify that when the source EntitySet is cleared,
            // the cached set is also cleared
            ec.PurchaseOrderDetails.Clear();
#if !SILVERLIGHT
            cachedEntities = (IList)order.PurchaseOrderDetails.GetType().GetProperty("Entities", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(order.PurchaseOrderDetails, null);
            Assert.AreEqual(0, cachedEntities.Count);
#endif
            Assert.AreEqual(0, order.PurchaseOrderDetails.Count);
        }

        /// <summary>
        /// EntityCollection was firing CollectionChanged before entity was actually removed from collection
        /// </summary>
        [TestMethod]
        public void EntityCollection_Bug509561()
        {
            TestEntityContainer ec = new TestEntityContainer();
            PurchaseOrderDetail removedDetail = null;

            PurchaseOrder order = new PurchaseOrder()
            {
                PurchaseOrderID = 1
            };
            ec.PurchaseOrders.Add(order);

            ((IRevertibleChangeTracking)ec).AcceptChanges();

            // add some details
            for (int i = 1; i < 5; i++)
            {
                PurchaseOrderDetail detail = new PurchaseOrderDetail
                {
                    PurchaseOrderDetailID = i
                };
                order.PurchaseOrderDetails.Add(detail);
                ec.PurchaseOrderDetails.Add(detail);
            }

            Assert.AreEqual(4, order.PurchaseOrderDetails.Count);

            ((INotifyCollectionChanged)order.PurchaseOrderDetails).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    PurchaseOrderDetail[] currDetails = order.PurchaseOrderDetails.ToArray();
                    Assert.IsFalse(currDetails.Contains(removedDetail));
                }
            };

            // remove from entity set - we expect it to also be
            // removed from the entity collection
            removedDetail = order.PurchaseOrderDetails.First();
            ec.PurchaseOrderDetails.Remove(removedDetail);
            Assert.AreEqual(3, order.PurchaseOrderDetails.Count);

            removedDetail = order.PurchaseOrderDetails.First();
            removedDetail.PurchaseOrder = null;
            Assert.AreEqual(2, order.PurchaseOrderDetails.Count);
        }

        [TestMethod]
        public void EntityCollection_UpdateOnAccept()
        {
            TestEntityContainer ec = new TestEntityContainer();

            PurchaseOrder order = new PurchaseOrder
            {
                PurchaseOrderID = 1
            };
            PurchaseOrderDetail detail = new PurchaseOrderDetail
            {
                PurchaseOrder = order,
                PurchaseOrderDetailID = 1
            };
            ec.LoadEntities(new Entity[] { order, detail });

            // force collection to cache
            Assert.AreEqual(1, order.PurchaseOrderDetails.Count);

            // now simulate submission of a new detail for the order via the entity set - once
            // the changes are accepted, we expect the cached collection to be updated and show
            // the new detail
            EntitySet<PurchaseOrderDetail> detailsSet = ec.GetEntitySet<PurchaseOrderDetail>();
            PurchaseOrderDetail newDetail1 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1,
                PurchaseOrderDetailID = 2
            };
            detailsSet.Add(newDetail1);
            Assert.AreEqual(1, order.PurchaseOrderDetails.Count);  // expect count to remain same
            ((IRevertibleChangeTracking)detailsSet).AcceptChanges();  // cause New -> Unmodified state transition
            Assert.AreEqual(2, order.PurchaseOrderDetails.Count);  // expect to see the new detail
            Assert.IsTrue(order.PurchaseOrderDetails.Contains(newDetail1));

            // verify that the same behavior occurs if we are in an edit session
            PurchaseOrderDetail newDetail2 = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1,
                PurchaseOrderDetailID = 3
            };
            ((IEditableObject)newDetail2).BeginEdit();
            detailsSet.Add(newDetail2);
            Assert.AreEqual(2, order.PurchaseOrderDetails.Count);  // expect count to remain same
            ((IRevertibleChangeTracking)detailsSet).AcceptChanges();  // cause New -> Unmodified state transition
            Assert.AreEqual(3, order.PurchaseOrderDetails.Count);  // expect to see the new detail
            Assert.IsTrue(order.PurchaseOrderDetails.Contains(newDetail2));
        }

        [TestMethod]
        public void EntityRef_UpdateOnAccept()
        {
            TestEntityContainer ec = new TestEntityContainer();

            PurchaseOrderDetail detail = new PurchaseOrderDetail
            {
                PurchaseOrderID = 1,
                PurchaseOrderDetailID = 1
            };
            ec.LoadEntities(new Entity[] { detail });

            // force collection to cache
            Assert.IsNull(detail.PurchaseOrder);

            // now simulate submission of a new order for the order via the entity set - once
            // the changes are accepted, we expect the cached reference to be updated and show
            // the new order
            EntitySet<PurchaseOrder> ordersSet = ec.GetEntitySet<PurchaseOrder>();
            PurchaseOrder newOrder = new PurchaseOrder
            {
                PurchaseOrderID = 1
            };
            ordersSet.Add(newOrder);
            Assert.IsNull(detail.PurchaseOrder);  // expect ref to remain the same
            ((IRevertibleChangeTracking)ordersSet).AcceptChanges();  // cause New -> Unmodified state transition
            Assert.AreSame(newOrder, detail.PurchaseOrder);  // expect to see the new order
        }

        [TestMethod]
        public void EntityCollection_CollectionChangedEvents()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            EntitySet<PurchaseOrderDetail> pods = ec.GetEntitySet<PurchaseOrderDetail>();

            Product product = new Product
            {
                ProductID = 1
            };
            products.Add(product);

            // subscribe to CollectionChanged event
            int notifyCollectionChangedCount = 0;
            ((INotifyCollectionChanged)product.PurchaseOrderDetails).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs args)
            {
                notifyCollectionChangedCount++;
            };

            // subscribe to PropertyChanged event
            int countChangedEventCount = 0;
            ((INotifyPropertyChanged)product.PurchaseOrderDetails).PropertyChanged += delegate(object sender, PropertyChangedEventArgs args)
            {
                Assert.AreSame(sender, product.PurchaseOrderDetails);
                if (args.PropertyName == "Count")
                {
                    countChangedEventCount++;
                }
            };

            // subscribe to the Added/Removed events
            int addedCount = 0;
            int removedCount = 0;
            EntityCollectionChangedEventArgs<PurchaseOrderDetail> lastEntityCollectionChangedEventArgs = null;
            product.PurchaseOrderDetails.EntityAdded += delegate(object sender, EntityCollectionChangedEventArgs<PurchaseOrderDetail> args)
            {
                lastEntityCollectionChangedEventArgs = args;
                addedCount++;
            };
            product.PurchaseOrderDetails.EntityRemoved += delegate(object sender, EntityCollectionChangedEventArgs<PurchaseOrderDetail> args)
            {
                lastEntityCollectionChangedEventArgs = args;
                removedCount++;
            };

            // verify notification for Add
            PurchaseOrderDetail pod = null;
            for (int i = 0; i < 3; i++)
            {
                pod = new PurchaseOrderDetail();
                pod.Product = product;
            }

            Assert.AreEqual(3, notifyCollectionChangedCount);
            Assert.AreEqual(3, addedCount);
            Assert.AreEqual(3, countChangedEventCount);

            // now try the Remove scenario
            Assert.IsTrue(pods.Contains(pod));
            notifyCollectionChangedCount = 0;
            countChangedEventCount = 0;
            product.PurchaseOrderDetails.Remove(pod);
            pods.Remove(pod);

            Assert.AreEqual(1, notifyCollectionChangedCount);
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, countChangedEventCount);
            Assert.AreSame(pod, lastEntityCollectionChangedEventArgs.Entity);
        }

        [TestMethod]
        public void EntityCollection_CollectionChangedEvents_SourceSetReset()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            EntitySet<PurchaseOrderDetail> pods = ec.GetEntitySet<PurchaseOrderDetail>();

            Product product = new Product
            {
                ProductID = 1
            };
            products.Add(product);
            for (int i = 1; i <= 3; i++)
            {
                pods.Add(new PurchaseOrderDetail
                {
                    PurchaseOrderID = 1,
                    PurchaseOrderDetailID = i,
                    Product = product
                });
            }
            ((IChangeTracking)ec).AcceptChanges();

            // subscribe to CollectionChanged event
            int notifyCollectionChangedCount = 0;
            ((INotifyCollectionChanged)product.PurchaseOrderDetails).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs args)
            {
                notifyCollectionChangedCount++;
            };

            // subscribe to the Removed events
            int removedCount = 0;
            EntityCollectionChangedEventArgs<PurchaseOrderDetail> lastEntityCollectionChangedEventArgs = null;
            product.PurchaseOrderDetails.EntityRemoved += delegate(object sender, EntityCollectionChangedEventArgs<PurchaseOrderDetail> args)
            {
                lastEntityCollectionChangedEventArgs = args;
                removedCount++;
            };

            Assert.AreEqual(3, product.PurchaseOrderDetails.Count);

            // verify EntityRemoved event when the source EntitySet is cleared
            removedCount = 0;
            pods.Clear();
            Assert.AreEqual(0, product.PurchaseOrderDetails.Count);
            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(1, notifyCollectionChangedCount);
        }

        [TestMethod]
        public void EntityCollection_ToString()
        {
            Employee employee = new Employee();
            Assert.AreEqual<string>(typeof(PurchaseOrder).Name, employee.PurchaseOrders.ToString());
            Assert.AreEqual<string>(typeof(Employee).Name, employee.Reports.ToString());
        }

        [TestMethod]
        public void LoadBehavior_LostUpdateScenario()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Product product = new Product
            {
                ProductID = 1,
                Name = "A",
                Color = "B",
                Class = "C"
            };
            ec.LoadEntities(new Product[] { product });

            // make a modification to Color
            product.Color = "X";

            // merge in a new product with the SAME modification
            // to Color (and some others)
            Product newProduct = new Product
            {
                ProductID = 1,
                Name = "A",
                Color = "X",
                Class = "Y"
            };
            ec.LoadEntities(new Product[] { newProduct }, LoadBehavior.MergeIntoCurrent);
            Assert.AreEqual("A", product.Name);
            Assert.AreEqual("X", product.Color);
            Assert.AreEqual("Y", product.Class);

            // merge again with another modification to Color - we need to be sure 
            // that this modification isn't merged in, overwriting our change
            newProduct = new Product
            {
                ProductID = 1,
                Name = "A",
                Color = "Z",
                Class = "Z"
            };
            ec.LoadEntities(new Product[] { newProduct }, LoadBehavior.MergeIntoCurrent);
            Assert.AreEqual("A", product.Name);
            Assert.AreEqual("Z", product.Color);  // should be merged, since Color is no longer considered Modified
            Assert.AreEqual("Z", product.Class);  // should be merged, since we haven't modified
        }

        [TestMethod]
        public void LoadBehavior_MergeAndTracking()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Product product = new Product
            {
                ProductID = 1,
                Name = "A",
                Color = "B",
                Class = "C"
            };
            ec.LoadEntities(new Product[] { product });
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);

            // create a new product instance for the same ID to merge in
            Product dupProduct = new Product
            {
                ProductID = 1,
                Name = "X",
                Color = "B",
                Class = "Z"
            };
            ec.LoadEntities(new Product[] { dupProduct }, LoadBehavior.RefreshCurrent);

            // after the values are overwritten, we expect the product to
            // remain unmodified
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);
        }

        /// <summary>
        /// Verify that if an edit session is begun on an unmodified entity, if the
        /// edits are cancelled, the entity is again unmodified.
        /// </summary>
        [TestMethod]
        public void CancelEdit_UnmodifiedEntity()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product product = products.First();

            Assert.AreEqual(EntityState.Unmodified, product.EntityState);
            ((IEditableObject)product).BeginEdit();
            product.Name = "Cinnamon crunchies";
            product.ListPrice += 8.50M;
            Assert.AreEqual(EntityState.Modified, product.EntityState);
            ((IEditableObject)product).CancelEdit();

            Assert.AreEqual(EntityState.Unmodified, product.EntityState);

            // This time start with an unmodified entity, do a
            // Begin/Cancel with no intervening changes and verify still unmodified
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);
            ((IEditableObject)product).BeginEdit();
            ((IEditableObject)product).CancelEdit();
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);
        }

        /// <summary>
        /// Test IEditableObject support for an Attached entity
        /// </summary>
        [TestMethod]
        public void EntityEditableObject_AttachedEntity()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product product = products.First();

            // the first time we edit the entity, the root entity
            // snapshot is taken
            product.Class = "SN";
            product.Color = "Purple";
            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(product));

            // test CancelEdit
            IDictionary<string, object> snapshot = product.ExtractState();
            IEditableObject editableProduct = (IEditableObject)product;
            editableProduct.BeginEdit();
            product.Name = "Choco Chips";
            product.Color = "Brown";
            editableProduct.CancelEdit();
            Assert.IsTrue(TestHelperMethods.VerifyEntityState(snapshot, product.ExtractState()));

            // verify the entity is still in the changeset
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(product));

            // test EndEdit and verify changes are accepted
            editableProduct.BeginEdit();
            product.Name = "Choco Chips";
            product.Color = "Brown";
            editableProduct.EndEdit();
            Assert.AreEqual("Brown", product.Color);
            Assert.AreEqual("Choco Chips", product.Name);

            // test a second edit session and verify changes are accepted
            editableProduct.BeginEdit();
            product.Color = "Yellow";
            editableProduct.EndEdit();
            Assert.AreEqual("Yellow", product.Color);

            // verify the entity is still in the changeset
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(product));
        }

        /// <summary>
        /// Verify that even if an entity isn't attached, it still has
        /// IEditableObject support
        /// </summary>
        [TestMethod]
        public void EntityEditableObject_UnattachedEntity()
        {
            Product product = new Product
            {
                Name = "Fruity Snax",
                Color = "Red",
                ProductNumber = "abc123"
            };

            // test CancelEdit
            IDictionary<string, object> snapshot = product.ExtractState();
            IEditableObject editableProduct = (IEditableObject)product;
            editableProduct.BeginEdit();
            product.Name = "Choco Chips";
            product.Color = "Brown";
            editableProduct.CancelEdit();
            Assert.IsTrue(TestHelperMethods.VerifyEntityState(snapshot, product.ExtractState()));

            // test EndEdit and verify changes are accepted
            editableProduct.BeginEdit();
            product.Name = "Choco Chips";
            product.Color = "Brown";
            editableProduct.EndEdit();
            Assert.AreEqual("Brown", product.Color);
            Assert.AreEqual("Choco Chips", product.Name);

            // test a second edit session and verify changes are accepted
            editableProduct.BeginEdit();
            product.Color = "Yellow";
            editableProduct.EndEdit();
            Assert.AreEqual("Yellow", product.Color);
        }

        public void EntityEditableObject_OrderingSemantics()
        {
            Product product = new Product
            {
                Name = "Fruity Snax",
                Color = "Red"
            };

            // calling EndEdit when not editing is a noop
            IEditableObject editableProduct = (IEditableObject)product;
            editableProduct.EndEdit();

            // calling CancelEdit when not editing is a noop
            editableProduct.EndEdit();

            // calling BeginEdit multiple times results in all but the first
            // call being ignored, meaning a subsequent cancel rolls back to
            // the first snapshot
            IDictionary<string, object> snapshot = product.ExtractState();
            editableProduct.BeginEdit();
            product.Name = "Choco Chips";
            editableProduct.BeginEdit();
            editableProduct.BeginEdit();
            product.Color = "Brown";
            editableProduct.CancelEdit();
            Assert.IsTrue(TestHelperMethods.VerifyEntityState(snapshot, product.ExtractState()));
        }

        /// <summary>
        /// Verify expected exception when attempting to remove an entity
        /// from an entity set that doesn't support remove
        /// </summary>
        [TestMethod]
        public void EntitySet_RemoveNotSupported()
        {
            ConfigurableEntityContainer ec = new ConfigurableEntityContainer();
            ec.CreateSet<Product>(EntitySetOperations.Add);
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product existingProduct = new Product
            {
                ProductID = 1
            };
            products.LoadEntity(existingProduct);

            // verify that New entities can be removed
            // after they have been added
            Product newProduct = new Product();
            products.Add(newProduct);
            products.Remove(newProduct);
            Assert.IsFalse(products.Contains(newProduct));
            Assert.AreEqual(EntityState.Detached, newProduct.EntityState);

            // verify that attempting to remove a non-new entity
            // results in the expected exception
            NotSupportedException expectedException = null;
            try
            {
                products.Remove(existingProduct);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(Resource.EntitySet_UnsupportedOperation, typeof(Product), EntitySetOperations.Remove), expectedException.Message);
        }

        /// <summary>
        /// Verify expected exception when attempting to add an entity
        /// to an entity set that doesn't support add
        /// </summary>
        [TestMethod]
        public void EntitySet_AddNotSupported()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<Product> products = ec.AddEntitySet<Product>(EntitySetOperations.Edit | EntitySetOperations.Remove);

            Product existingProduct = new Product
            {
                ProductID = 1
            };
            products.LoadEntity(existingProduct);

            NotSupportedException expectedException = null;
            try
            {
                Product newProduct = new Product
                {
                    ProductID = 2
                };
                products.Add(newProduct);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(Resource.EntitySet_UnsupportedOperation, typeof(Product), EntitySetOperations.Add), expectedException.Message);

            // verify that we can add a Removed entity back even
            // though Adds are not supported
            Product removedProduct = products.First();
            EntityState prevState = removedProduct.EntityState;
            products.Remove(removedProduct);
            Assert.IsFalse(products.Contains(removedProduct));
            Assert.AreEqual(EntityState.Deleted, removedProduct.EntityState);
            products.Add(removedProduct);
            Assert.IsTrue(products.Contains(removedProduct));
            Assert.AreEqual(prevState, removedProduct.EntityState);

            // verify that when undoing a remove of an entity that has
            // also been modified, the modification state is maintained
            // after the re-add
            removedProduct.ListPrice += 1.0M;
            prevState = removedProduct.EntityState;
            Assert.AreEqual(EntityState.Modified, prevState);
            products.Remove(removedProduct);
            Assert.IsFalse(products.Contains(removedProduct));
            Assert.AreEqual(EntityState.Deleted, removedProduct.EntityState);
            products.Add(removedProduct);
            Assert.IsTrue(products.Contains(removedProduct));
            Assert.AreEqual(prevState, removedProduct.EntityState);
        }

        /// <summary>
        /// Verify expected exception when attempting to edit an entity
        /// from an entity set that doesn't support edit
        /// </summary>
        [TestMethod]
        public void EntitySet_EditNotSupported()
        {
            ConfigurableEntityContainer ec = new ConfigurableEntityContainer();
            ec.CreateSet<Product>(EntitySetOperations.Add | EntitySetOperations.Remove);
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product product = new Product
            {
                ProductID = 1
            };

            // until the entity is attached, edits are not tracked, so they are permitted
            product.Name += "x";

            products.LoadEntities(new Entity[] { product });

            // verify that attempting to edit the entity
            // results in the expected exception
            NotSupportedException expectedException = null;
            try
            {
                product.Name += "x";
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(Resource.EntitySet_UnsupportedOperation, typeof(Product), EntitySetOperations.Edit), expectedException.Message);

            // Now verify that even though Edit is not supported for existing entities,
            // we can still change properties of New entities
            Product newProduct = new Product { ProductID = 2 };
            products.Add(newProduct);
            newProduct.Name = "Cheezy Doodles";
        }

        /// <summary>
        /// Verify expected exception when attempting to add an entity thats
        /// already in the set
        /// </summary>
        [TestMethod]
        public void EntitySet_AddEntityAlreadyInSet()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Product product = new Product()
            {
                ProductID = 1
            };
            ec.GetEntitySet<Product>().LoadEntity(product);

            InvalidOperationException expectedException = null;
            try
            {
                ec.GetEntitySet<Product>().Add(product);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(Resource.EntitySet_EntityAlreadyInSet, expectedException.Message);
        }

        /// <summary>
        /// Verify expected exception when attempting to remove an entity thats
        /// not in the set
        [TestMethod]
        public void EntitySet_RemoveEntityNotInSet()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Product product = new Product();

            InvalidOperationException expectedException = null;
            try
            {
                ec.GetEntitySet<Product>().Remove(product);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(Resource.EntitySet_EntityNotInSet, expectedException.Message);
        }

        /// <summary>
        /// We rely on this helper throughout the tests, so verify it operates as expected
        /// </summary>
        [TestMethod]
        public void TestVerifyEntityStateHelper()
        {
            foreach (Product product in BaselineTestData.Products)
            {
                Assert.IsTrue(TestHelperMethods.VerifyEntityState(product.ExtractState(), product.ExtractState()));
            }
        }

        /// <summary>
        /// Test Reject at the entity level for a modified entity
        /// </summary>
        [TestMethod]
        public void EntityRejectChanges_ModifiedEntity()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product product = products.First();
            IDictionary<string, object> originalState = product.ExtractState();

            // make some changes to the entity
            product.Name = "Foo";
            product.ProductNumber = "Bar";
            Assert.AreEqual(EntityState.Modified, product.EntityState);
            Assert.IsFalse(ec.GetChanges().IsEmpty);

            // revert the changes
            ((IRevertibleChangeTracking)product).RejectChanges();
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);
            Assert.IsTrue(TestHelperMethods.VerifyEntityState(originalState, product.ExtractState()));
            Assert.IsTrue(ec.GetChanges().IsEmpty);
        }

        /// <summary>
        /// Test Reject at the entity level for a new entity
        /// </summary>
        [TestMethod]
        public void EntityRejectChanges_NewEntity()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product newProduct = new Product();

            // calling Reject on a new unattached entity is a noop
            ((IRevertibleChangeTracking)newProduct).RejectChanges();

            products.Add(newProduct);

            // calling Reject on a new attached entity is also a noop
            ((IRevertibleChangeTracking)newProduct).RejectChanges();

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 1 && changeSet.RemovedEntities.Count == 0);
            Assert.IsTrue(changeSet.AddedEntities.Contains(newProduct));

            // the Add must be reverted through the EntitySet
            ((IRevertibleChangeTracking)products).RejectChanges();
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);
            Assert.AreEqual(0, products.Count);
        }

        /// <summary>
        /// Test Reject at the entity level for a deleted entity
        /// </summary>
        [TestMethod]
        public void EntityRejectChanges_DeletedEntity()
        {
            TestEntityContainer ec = GetModifiableTestContainer();
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();
            int originalCount = prodSet.Count;

            // remove a couple entities
            Product[] products = prodSet.ToArray();
            Product delProd1 = products[0];
            Product delProd5 = products[5];

            // make modifications prior to deleting one of the entities
            // to ensure that both the changes and the delete are reverted
            IDictionary<string, object> originalState = delProd5.ExtractState();
            delProd5.Name += "x";
            delProd5.Class = "FB";

            prodSet.Remove(delProd1);
            prodSet.Remove(delProd5);

            // calling Reject on a deleted entity is a noop
            ((IRevertibleChangeTracking)delProd1).RejectChanges();

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 0 && changeSet.RemovedEntities.Count == 2);
            Assert.IsTrue(changeSet.RemovedEntities.SequenceEqual(new Entity[] { delProd5, delProd1 }));

            // the Remove must be reverted through the EntitySet
            ((IRevertibleChangeTracking)prodSet).RejectChanges();
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);
            Assert.AreEqual(originalCount, prodSet.Count);

            // verify that the property modifications were reverted as well
            Assert.IsTrue(TestHelperMethods.VerifyEntityState(originalState, delProd5.ExtractState()));
        }

        /// <summary>
        /// Test change tracking and rejection at the DomainContext level
        /// </summary>
        [TestMethod]
        public void DataContextChangeTracking()
        {
            ChangeTrackingTestContext ctxt = new ChangeTrackingTestContext();
            int originalProductCount = ctxt.Products.Count;

            // Add some new entities
            PurchaseOrder newOrder1 = new PurchaseOrder();
            PurchaseOrder newOrder2 = new PurchaseOrder();
            ctxt.PurchaseOrders.Add(newOrder1);
            ctxt.PurchaseOrders.Remove(newOrder1);  // remove then re-add the new order should work
            ctxt.PurchaseOrders.Add(newOrder1);
            ctxt.PurchaseOrders.Add(newOrder2);

            EntityChangeSet changeSet = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 2 && changeSet.RemovedEntities.Count == 0);
            Assert.IsTrue(changeSet.AddedEntities.Contains(newOrder1));
            Assert.IsTrue(changeSet.AddedEntities.Contains(newOrder2));

            // Modify some entities
            Product[] products = ctxt.Products.ToArray();
            Product modifiedProduct1 = products[22];
            modifiedProduct1.Name += "x";
            Product modifiedProduct2 = products[5];
            modifiedProduct2.Name += "x";
            Product modifiedProduct3 = products[1];
            modifiedProduct3.Color += "x";

            changeSet = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 3 && changeSet.AddedEntities.Count == 2 && changeSet.RemovedEntities.Count == 0);
            Assert.IsTrue(changeSet.ModifiedEntities.SequenceEqual(new Entity[] { modifiedProduct1, modifiedProduct2, modifiedProduct3 }));

            // Remove some entities
            Product removedProduct1 = products[3];
            ctxt.Products.Remove(removedProduct1);
            Product removedProduct2 = products[100];
            ctxt.Products.Remove(removedProduct2);
            ctxt.Products.Remove(modifiedProduct3);  // remove one of the modified entities

            changeSet = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 2 && changeSet.AddedEntities.Count == 2 && changeSet.RemovedEntities.Count == 3);
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(modifiedProduct1));
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(modifiedProduct2));
            Assert.IsTrue(changeSet.RemovedEntities.Contains(removedProduct1));
            Assert.IsTrue(changeSet.RemovedEntities.Contains(removedProduct2));
            Assert.IsTrue(changeSet.RemovedEntities.Contains(modifiedProduct3));

            ctxt.RejectChanges();

            changeSet = ctxt.EntityContainer.GetChanges();
            Assert.IsTrue(changeSet.IsEmpty);
            Assert.AreEqual(originalProductCount, ctxt.Products.Count);
            Assert.AreEqual(0, ctxt.PurchaseOrders.Count);
        }

        [TestMethod]
        public void EntitySet_Attach()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();

            // verify null handling
            Exception expectedException = null;
            try
            {
                products.Attach(null);
            }
            catch (ArgumentNullException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(new ArgumentNullException("entity").Message, expectedException.Message);
            expectedException = null;

            // attach a product and verify state
            Product product = new Product
            {
                ProductID = 1,
                Name = "Choco Crisp"
            };
            products.Attach(product);
            Assert.AreSame(products, product.EntitySet);
            Assert.IsTrue(products.Contains(product));
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);
            Assert.IsTrue(ec.GetChanges().IsEmpty);

            // verify that if we attempt to attach an entity with a duplicate
            // identity, we get an exception
            Product dupProduct = new Product
            {
                ProductID = 1,
                Name = "Duplicate"
            };
            try
            {
                products.Attach(dupProduct);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(Resource.EntitySet_DuplicateIdentity, expectedException.Message);
            expectedException = null;
        }

        [TestMethod]
        public void EntitySet_Detach()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<Product> products = ec.GetEntitySet<Product>();

            // verify null handling
            Exception expectedException = null;
            try
            {
                products.Detach(null);
            }
            catch (ArgumentNullException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(new ArgumentNullException("entity").Message, expectedException.Message);
            expectedException = null;

            // attempt to detach an entity not in the set
            Product dneProduct = new Product();
            try
            {
                products.Detach(dneProduct);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(Resource.EntitySet_EntityNotInSet, expectedException.Message);
            expectedException = null;

            // attach a product
            Product product = new Product
            {
                ProductID = 1,
                Name = "Choco Crisp"
            };
            products.Attach(product);
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);

            // modify then detach the product, verifying that when the entity
            // is detached all state is reset, and it transitions back to the
            // default state of 'New'
            product.Name = "Choco Bites";
            Assert.AreEqual(EntityState.Modified, product.EntityState);
            products.Detach(product);
            Assert.AreEqual(EntityState.Detached, product.EntityState);
            Assert.IsFalse(products.Contains(product));
            Assert.AreEqual(null, product.EntitySet);

            // verify that if the entity is reattached, it's state
            // is reset
            products.Attach(product);
            Assert.AreEqual(EntityState.Unmodified, product.EntityState);

            // verify that the reattached entity is being change tracked
            product.Name += "x";
            Assert.AreEqual(EntityState.Modified, product.EntityState);
            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(product));

            // detach once more
            products.Detach(product);
            Assert.IsTrue(ec.GetChanges().IsEmpty);
        }

        [TestMethod]
        public void EntitySet_CollectionChangedEvents()
        {
            TestEntityContainer ec = new TestEntityContainer();
            NotifyCollectionChangedEventArgs collectionChangedArgs = null;
            object collectionChangedSender = null;
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();
            int collectionChangedEventCount = 0;

            ((INotifyCollectionChanged)prodSet).CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs args)
            {
                collectionChangedArgs = args;
                collectionChangedSender = sender;
                collectionChangedEventCount++;
            };
            int addedCount = 0;
            int removedCount = 0;
            prodSet.EntityAdded += (s, e) =>
            {
                addedCount++;
            };
            prodSet.EntityRemoved += (s, e) =>
            {
                removedCount++;
            };

            // verify notifications for Add
            Product product = new Product
            {
                ProductID = 1
            };
            prodSet.Add(product);
            Assert.AreSame(prodSet, collectionChangedSender);
            Assert.AreSame(product, collectionChangedArgs.NewItems.Cast<Product>().Single());
            Assert.AreEqual(NotifyCollectionChangedAction.Add, collectionChangedArgs.Action);
            Assert.AreEqual(0, collectionChangedArgs.NewStartingIndex);
            Assert.AreEqual(1, collectionChangedEventCount);
            Assert.AreEqual(1, addedCount);

            // add a few more
            prodSet.Add(new Product
            {
                ProductID = 2
            });
            prodSet.Add(new Product
            {
                ProductID = 3
            });
            prodSet.Add(new Product
            {
                ProductID = 4
            });
            Assert.AreEqual(4, collectionChangedEventCount);
            Assert.AreEqual(4, addedCount);

            // verify notification for Remove
            collectionChangedSender = null;
            collectionChangedArgs = null;
            Product[] products = prodSet.ToArray();
            product = products[2];
            prodSet.Remove(product);
            Assert.AreSame(prodSet, collectionChangedSender);
            Assert.AreSame(product, collectionChangedArgs.OldItems.Cast<Product>().Single());
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, collectionChangedArgs.Action);
            Assert.AreEqual(2, collectionChangedArgs.OldStartingIndex);
            Assert.AreEqual(5, collectionChangedEventCount);
            Assert.AreEqual(1, removedCount);

            // reattach the product just removed and verify notification for Attach
            collectionChangedSender = null;
            collectionChangedArgs = null;
            prodSet.Attach(product);
            Assert.AreSame(prodSet, collectionChangedSender);
            Assert.AreSame(product, collectionChangedArgs.NewItems.Cast<Product>().Single());
            Assert.AreEqual(NotifyCollectionChangedAction.Add, collectionChangedArgs.Action);
            Assert.AreEqual(3, collectionChangedArgs.NewStartingIndex);
            Assert.AreEqual(6, collectionChangedEventCount);
            Assert.AreEqual(5, addedCount);

            // verify notification for Detach
            collectionChangedSender = null;
            collectionChangedArgs = null;
            products = prodSet.ToArray();
            product = products[1];
            prodSet.Detach(product);
            Assert.AreSame(prodSet, collectionChangedSender);
            Assert.AreSame(product, collectionChangedArgs.OldItems.Cast<Product>().Single());
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, collectionChangedArgs.Action);
            Assert.AreEqual(1, collectionChangedArgs.OldStartingIndex);
            Assert.AreEqual(7, collectionChangedEventCount);
            Assert.AreEqual(2, removedCount);

            // verify notification for Clear
            removedCount = 0;
            collectionChangedSender = null;
            collectionChangedArgs = null;
            int prodCount = prodSet.Count;
            prodSet.Clear();
            Assert.AreSame(prodSet, collectionChangedSender);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, collectionChangedArgs.Action);
            Assert.AreEqual(8, collectionChangedEventCount);
            Assert.AreEqual(prodCount, removedCount);
        }

        [TestMethod]
        public void EntitySet_SupportedOperations()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<Product> set = ec.AddEntitySet<Product>(EntitySetOperations.None);
            Assert.IsFalse(set.CanAdd);
            Assert.IsFalse(set.CanEdit);
            Assert.IsFalse(set.CanRemove);

            ec = new DynamicEntityContainer();
            set = ec.AddEntitySet<Product>(EntitySetOperations.Add);
            Assert.IsTrue(set.CanAdd);
            Assert.IsFalse(set.CanEdit);
            Assert.IsFalse(set.CanRemove);

            ec = new DynamicEntityContainer();
            set = ec.AddEntitySet<Product>(EntitySetOperations.Add | EntitySetOperations.Edit);
            Assert.IsTrue(set.CanAdd);
            Assert.IsTrue(set.CanEdit);
            Assert.IsFalse(set.CanRemove);

            ec = new DynamicEntityContainer();
            set = ec.AddEntitySet<Product>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            Assert.IsTrue(set.CanAdd);
            Assert.IsTrue(set.CanEdit);
            Assert.IsTrue(set.CanRemove);
        }

        /// <summary>
        /// Verify that changes can be tracked and Accepted/Rejected at the EntityContainer level.
        /// Basically we want to verify that an EntityContainer is useable for change tracking independent
        /// of a DomainContext, through the public interfaces exposed to users
        /// </summary>
        [TestMethod]
        public void EntityContainerChangeTracking()
        {
            TestEntityContainer ec = new TestEntityContainer();

            // load some entities into the container
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();
            foreach (Product prod in BaselineTestData.Products.Select(p => TestHelperMethods.CloneEntity(p)))
            {
                prodSet.Attach(prod);
            }
            int countBefore = prodSet.Count;

            Assert.IsTrue(prodSet.CanAdd);
            Assert.IsTrue(prodSet.CanRemove);
            Assert.IsTrue(prodSet.CanEdit);

            // make some edits
            Product[] products = prodSet.ToArray();
            Product modifiedProduct = products[5];
            modifiedProduct.Color += "x";

            Product newProduct = new Product();
            Assert.AreEqual(EntityState.Detached, newProduct.EntityState);
            Assert.IsFalse(prodSet.Contains(newProduct));

            // test IEditableObject.Begin/End/CancelEdit
            Product modifiedProduct2 = products[11];
            ((IEditableObject)modifiedProduct2).BeginEdit();
            modifiedProduct2.Name = "Foobar";
            modifiedProduct2.Color += "x";
            ((IEditableObject)modifiedProduct2).CancelEdit();
            ((IEditableObject)modifiedProduct2).BeginEdit();
            modifiedProduct2.Name = "Modified";
            ((IEditableObject)modifiedProduct2).EndEdit();
            Assert.AreEqual(EntityState.Modified, modifiedProduct2.EntityState);

            // make some additions
            newProduct = new Product
            {
                ProductID = 1001
            };
            prodSet.Add(newProduct);

            // make some removals
            Product removedProduct = products[12];
            prodSet.Remove(removedProduct);

            // get the changeset and verify
            EntityChangeSet changeSet = ec.GetChanges();
            Assert.AreEqual(2, changeSet.ModifiedEntities.Count);
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(modifiedProduct));
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(modifiedProduct2));
            Assert.AreSame(newProduct, changeSet.AddedEntities.Single());
            Assert.AreSame(removedProduct, changeSet.RemovedEntities.Single());

            // reject changes
            ((IRevertibleChangeTracking)ec).RejectChanges();
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.IsEmpty);
            Assert.AreEqual(countBefore, prodSet.Count);
        }

        /// <summary>
        /// Test entity update change tracking
        /// </summary>
        [TestMethod]
        public void EntityChangeTracking_Updates()
        {
            TestEntityContainer ec = GetModifiableTestContainer();

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.IsEmpty);
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);

            // Edit some entities
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();
            Product[] products = prodSet.ToArray();
            Assert.AreEqual(EntityState.Unmodified, products[0].EntityState);
            products[0].Color += "x";
            Assert.AreEqual(EntityState.Modified, products[0].EntityState);

            // modify same entity multiple times
            products[1].Color += "x";
            products[1].Class += "x";

            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.SequenceEqual(new Entity[] { products[0], products[1] }));
            Assert.IsTrue(changeSet.AddedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);

            // modify another and verify
            products[2].Color += "x";
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.SequenceEqual(new Entity[] { products[0], products[1], products[2] }));
            Assert.IsTrue(changeSet.AddedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);
        }

        /// <summary>
        /// Test entity insert change tracking
        /// </summary>
        [TestMethod]
        public void EntityChangeTracking_Inserts()
        {
            TestEntityContainer ec = GetModifiableTestContainer();

            // Add some entities
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Product newProd1 = new Product
            {
                ProductID = 1001,
                Color = "Beige"
            };
            Assert.AreEqual(EntityState.Detached, newProd1.EntityState);
            products.Add(newProd1);
            Assert.AreEqual(EntityState.New, newProd1.EntityState);

            Product newProd2 = new Product
            {
                ProductID = 1002,
                Color = "Orange"
            };
            products.Add(newProd2);

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.AddedEntities.SequenceEqual(new Entity[] { newProd1, newProd2 }));
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);

            // Add another and verify
            Product newProd3 = new Product
            {
                ProductID = 1003,
                Color = "Yellow"
            };
            products.Add(newProd3);
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.AddedEntities.SequenceEqual(new Entity[] { newProd1, newProd2, newProd3 }));
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);

            // Now remove one of the previously added new entities
            Assert.AreEqual(EntityState.New, newProd2.EntityState);
            products.Remove(newProd2);
            Assert.AreEqual(EntityState.Detached, newProd2.EntityState);
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.AddedEntities.SequenceEqual(new Entity[] { newProd1, newProd3 }));
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.RemovedEntities.Count == 0);
        }

        /// <summary>
        /// Test entity delete change tracking
        /// </summary>
        [TestMethod]
        public void EntityChangeTracking_Deletes()
        {
            TestEntityContainer ec = GetModifiableTestContainer();

            // Delete some entities
            EntitySet<Product> prodSet = ec.GetEntitySet<Product>();
            Product[] products = prodSet.ToArray();
            Product removedProduct1 = products[0];
            Assert.AreEqual(EntityState.Unmodified, removedProduct1.EntityState);
            prodSet.Remove(products[0]);
            Assert.AreEqual(EntityState.Deleted, removedProduct1.EntityState);

            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.RemovedEntities.SequenceEqual(new Entity[] { removedProduct1 }));
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 0);

            // Update then Delete an entity
            Product removedProduct2 = products[1];
            Assert.AreEqual(EntityState.Unmodified, removedProduct2.EntityState);
            products[1].Class += "x";
            Assert.AreEqual(EntityState.Modified, removedProduct2.EntityState);
            prodSet.Remove(products[1]);
            Assert.AreEqual(EntityState.Deleted, removedProduct2.EntityState);

            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.RemovedEntities.SequenceEqual(new Entity[] { removedProduct1, removedProduct2 }));
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 0);

            // Now Remove a previously added new entity
            Product newProd2 = new Product
            {
                ProductID = 1001,
                Color = "Black"
            };
            prodSet.Add(newProd2);
            Assert.AreEqual(EntityState.New, newProd2.EntityState);
            prodSet.Remove(newProd2);
            Assert.AreEqual(EntityState.Detached, newProd2.EntityState);

            // we don't expect the new entity to show up in the list of removed entities
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.RemovedEntities.SequenceEqual(new Entity[] { removedProduct1, removedProduct2 }));
            Assert.IsTrue(changeSet.ModifiedEntities.Count == 0 && changeSet.AddedEntities.Count == 0);
        }

        /// <summary>
        /// Return a test container with cloned entities that we can modify freely
        /// without polluting our test data
        /// </summary>
        public static TestEntityContainer GetModifiableTestContainer()
        {
            TestEntityContainer ec = new TestEntityContainer();
            ec.LoadEntities(BaselineTestData.Products.Select(p => TestHelperMethods.CloneEntity(p)));
            return ec;
        }

        [TestMethod]
        public void TestIdentityCaching()
        {
            TestEntityContainer ec = new TestEntityContainer();
            EntitySet<Product> productSet = ec.GetEntitySet<Product>();

            // load some products
            List<Product> products = BaselineTestData.Products.OrderBy(p => p.ProductID).Take(50).ToList();
            IEnumerable<Product> loadedProducts = ec.LoadEntities(products).Cast<Product>();
            Assert.IsTrue(loadedProducts.SequenceEqual(products));
            Assert.IsTrue(productSet.SequenceEqual(products));

            // attempt to load the same product again, and make sure
            // the cached entity is maintained
            Product prod = products[5];
            prod.Name = "Cache Test";
            Product dupProd = new Product
            {
                ProductID = prod.ProductID
            };
            Entity loadedProduct = productSet.LoadEntity(dupProd);
            Assert.AreSame(prod, loadedProduct);
            Assert.IsFalse(productSet.Contains(dupProd));
            Product cachedProd = productSet.Single(p => p.ProductID == dupProd.ProductID);
            Assert.AreEqual("Cache Test", cachedProd.Name);  // make sure change was preserved
            Assert.AreEqual(50, productSet.Count);

            // Set with one dupe and one new product - expect only the new product
            // to me added
            int maxProdID = BaselineTestData.Products.Max(p => p.ProductID);
            Product newProd = new Product
            {
                ProductID = maxProdID + 1
            };
            loadedProducts = ec.LoadEntities(new Product[] { dupProd, newProd }).Cast<Product>();
            Assert.IsTrue(loadedProducts.SequenceEqual(new Product[] { prod, newProd }));
            Assert.AreEqual(51, productSet.Count);
        }

        [TestMethod]
        public void LoadBehavior_KeepCurrent()
        {
            // load an entity
            TestEntityContainer ec = new TestEntityContainer();
            Product p1 = new Product
            {
                ProductID = 1,
                Color = "Red",
                Size = "Big"
            };
            ec.LoadEntities(new Product[] { p1 }, LoadBehavior.KeepCurrent);

            // load the same entity again, with changes
            Product p2 = new Product
            {
                ProductID = 1,
                Color = "Blue",
                Size = "Big"
            };
            ec.LoadEntities(new Product[] { p2 }, LoadBehavior.KeepCurrent);

            // verify the entity instance in cache is not updated
            Product cachedProd = ec.GetEntitySet<Product>().Single(p => p.ProductID == p1.ProductID);
            Assert.AreSame(p1, cachedProd);
            Assert.AreEqual("Red", cachedProd.Color);
            Assert.AreSame("Big", cachedProd.Size);
        }

        [TestMethod]
        public void LoadBehavior_MergeIntoCurrent()
        {
            // load an entity
            TestEntityContainer ec = new TestEntityContainer();
            Product p1 = new Product
            {
                ProductID = 1,
                Name = "Unchanged",
                Color = "Red",
                Size = "Big"
            };
            ec.LoadEntities(new Product[] { p1 }, LoadBehavior.KeepCurrent);

            // make a change to the entity
            p1.Color = "Purple";
            Assert.AreEqual(EntityState.Modified, p1.EntityState);

            // load the same entity again, with changes
            Product p2 = new Product
            {
                ProductID = 1,
                Name = "Unchanged",
                Color = "Blue",    // conflicting change
                Size = "Small"     // non-conflicting change
            };
            ec.LoadEntities(new Product[] { p2 }, LoadBehavior.MergeIntoCurrent);

            // verify the entity instance in cache has been merged properly
            Product cachedProd = ec.GetEntitySet<Product>().Single(p => p.ProductID == p1.ProductID);
            Assert.AreSame(p1, cachedProd);
            Assert.AreEqual("Purple", cachedProd.Color);  // expect our change to be maintained 
            Assert.AreSame("Small", cachedProd.Size);     // expect to get the new Size
            Assert.AreSame("Unchanged", cachedProd.Name);
            Assert.AreEqual(EntityState.Modified, p1.EntityState);
        }

        [TestMethod]
        public void LoadBehavior_RefreshCurrent()
        {
            // load an entity and make a change
            TestEntityContainer ec = new TestEntityContainer();
            Product p1 = new Product
            {
                ProductID = 1,
                Color = "Red",
                Size = "Big"
            };
            ec.LoadEntities(new Product[] { p1 });
            p1.Color = "Purple";

            // load the same entity again, with changes
            Product p2 = new Product
            {
                ProductID = 1,
                Color = "Blue",
                Size = "Huge"
            };
            ec.LoadEntities(new Product[] { p2 }, LoadBehavior.RefreshCurrent);

            // verify the entity instance in cache is overwritten
            // with the new values, and our changes are lost
            Product cachedProd = ec.GetEntitySet<Product>().Single(p => p.ProductID == p1.ProductID);
            Assert.AreSame(p1, cachedProd);
            Assert.AreEqual(p2.Color, cachedProd.Color);
            Assert.AreSame(p2.Size, cachedProd.Size);

            // Verify that original values are cleared, and the
            // entity transitions back to Unmodified.
            Assert.IsNull(p1.GetOriginal());
            Assert.AreEqual(EntityState.Unmodified, p1.EntityState);
            Assert.IsFalse(p1.HasChanges);

            // do the same test again for an unmodified entity
            // don't expect original values to be set
            ((IRevertibleChangeTracking)ec).RejectChanges();
            Assert.AreEqual(EntityState.Unmodified, p1.EntityState);
            Assert.IsNull(p1.OriginalValues);
            ec.LoadEntities(new Product[] { p2 }, LoadBehavior.RefreshCurrent);
            Assert.AreEqual(EntityState.Unmodified, p1.EntityState);
            Assert.IsNull(p1.OriginalValues);

            // do the same test again for a modified entity that also
            // has a custom method invocation. We expect the entity 
            // to remain in the modified state (CM invocation is retained)
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            City city = new City() {Name="Toledo", CountyName="Lucas", StateName="OH"};
            cities.EntityContainer.LoadEntities(new Entity[] { city });
            city.StateName = "XX";
            city.AssignCityZone("Zone42");
            Assert.IsNotNull(city.CustomMethodInvocation);
            Assert.AreEqual(EntityState.Modified, city.EntityState);
            cities.EntityContainer.LoadEntities(new Entity[] { new City() {Name="Toledo", CountyName="Lucas", StateName="OH"}}, LoadBehavior.RefreshCurrent);
            Assert.IsNotNull(city.CustomMethodInvocation);
            Assert.AreEqual(EntityState.Modified, city.EntityState);
        }

        [TestMethod]
        public void TestLoadContainer()
        {
            TestEntityContainer ec = new TestEntityContainer();
            Assert.AreEqual(5, ec.EntitySets.Count());

            ec.LoadEntities(BaselineTestData.Products);
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Assert.IsNotNull(products);
            Assert.IsTrue(products.SequenceEqual(BaselineTestData.Products));
        }

        [TestMethod]
        public void TestClearContainer()
        {
            TestEntityContainer ec = new TestEntityContainer();
            ec.LoadEntities(BaselineTestData.Products);

            EntitySet<Product> products = ec.GetEntitySet<Product>();
            Assert.IsTrue(products.SequenceEqual(BaselineTestData.Products));

            // clear the set and verify
            ec.Clear();
            products = ec.GetEntitySet<Product>();
            Assert.AreEqual(0, products.Count);

            // reload again
            var query = BaselineTestData.Products.OrderBy(p => p.ProductID).Take(30);
            ec.LoadEntities(query);
            products = ec.GetEntitySet<Product>();
            Assert.IsTrue(query.SequenceEqual(products));
        }

        [TestMethod]
        public void TestClearEntitySet()
        {
            TestEntityContainer ec = new TestEntityContainer();

            // clear empty set
            EntitySet<Product> products = ec.GetEntitySet<Product>();
            products.Clear();

            ec.LoadEntities(BaselineTestData.Products);

            // verify that after load, the entity references its set
            Product prod = BaselineTestData.Products.First();
            Assert.AreSame(products, prod.EntitySet);

            // clear the set and verify
            ec.Clear();
            products = ec.GetEntitySet<Product>();
            Assert.AreEqual(0, products.Count);

            // verify that after the set is cleared, all entities are
            // detached from the set
            Assert.IsTrue(BaselineTestData.Products.All(p => p.EntitySet == null));
            Assert.IsTrue(BaselineTestData.Products.All(p => p.EntityState == EntityState.Detached));
           
            // verify that Clear also removes any and all pending changes
            prod = TestHelperMethods.CloneEntity(prod);
            products.Attach(prod);
            prod.Name += "x";
            EntityChangeSet changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.ModifiedEntities.Contains(prod));
            ec.Clear();
            changeSet = ec.GetChanges();
            Assert.IsTrue(changeSet.IsEmpty);
        }

        /// <summary>
        /// Verifies that EntityContainer references are referenced during entity set lookups.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that EntityContainer references are referenced during entity set lookups.")]
        public void AddReference()
        {
            // Create 2 EntityContainers.
            EntityContainer emptyContainer = new DynamicEntityContainer();
            EntityContainer referencedContainer = new TestEntityContainer();

            // Add a reference between the two.
            emptyContainer.AddReference(referencedContainer.GetEntitySet<Product>());

            // 'emptyContainer' has no entity sets, let's attempt to access a referenced EntitySet.
            Assert.IsNotNull(emptyContainer.GetEntitySet<Product>());
        }

        /// <summary>
        /// Verifies that null EntityContainer references cannot be added.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that null EntityContainer references cannot be added.")]
        public void AddReferenceNullThrows()
        {
            // Create an EntityContainer
            EntityContainer emptyContainer = new DynamicEntityContainer();

            // Attempt to add a new EntityContainer reference.
            ExceptionHelper.ExpectArgumentNullException(
                () => emptyContainer.AddReference(null),
                "entitySet");
        }

        /// <summary>
        /// Verifies that null EntityContainer references cannot be added.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that null EntityContainer references cannot be added.")]
        public void AddReferenceWithIncorrectType()
        {
            // Create an EntityContainer
            EntityContainer emptyContainer = new DynamicEntityContainer();

            // Attempt to add a new EntityContainer reference.
            ExceptionHelper.ExpectInvalidOperationException(
                () => emptyContainer.AddReference(emptyContainer.GetEntitySet<Product>()),
                string.Format(CultureInfo.InvariantCulture, Resource.EntityContainerDoesntContainEntityType, typeof(Product)));
        }
    }

    public class ChangeTrackingTestContext : DomainContext
    {
        public ChangeTrackingTestContext()
            : base(new WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract>(TestURIs.LTS_Catalog))
        {

        }

        public EntitySet<Product> Products
        {
            get
            {
                return EntityContainer.GetEntitySet<Product>();
            }
        }

        public EntitySet<PurchaseOrder> PurchaseOrders
        {
            get
            {
                return EntityContainer.GetEntitySet<PurchaseOrder>();
            }
        }

        protected override EntityContainer CreateEntityContainer()
        {
            TestEntityContainer ec = new TestEntityContainer();
            ec.LoadEntities(BaselineTestData.Products.Select(p => TestHelperMethods.CloneEntity(p)));
            return ec;
        }
    }

    public class EventingTestDataContext : DomainContext
    {
        public EventingTestDataContext()
            : base(new WebDomainClient<object>(TestURIs.RootURI))
        {
        }

        protected override EntityContainer CreateEntityContainer()
        {
            return EntityContainerTests.GetModifiableTestContainer();
        }
    }

    public class TestEntityContainer : EntityContainer
    {
        public TestEntityContainer()
        {
            CreateEntitySet<Product>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<PurchaseOrder>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<PurchaseOrderDetail>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<Employee>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<MixedType>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
        }

        public EntitySet<MixedType> MixedTypes
        {
            get
            {
                return GetEntitySet<MixedType>();
            }
        }

        public EntitySet<Product> Products
        {
            get
            {
                return GetEntitySet<Product>();
            }
        }

        public EntitySet<PurchaseOrder> PurchaseOrders
        {
            get
            {
                return GetEntitySet<PurchaseOrder>();
            }
        }

        public EntitySet<PurchaseOrderDetail> PurchaseOrderDetails
        {
            get
            {
                return GetEntitySet<PurchaseOrderDetail>();
            }
        }

        public EntitySet<Employee> Employees
        {
            get
            {
                return GetEntitySet<Employee>();
            }
        }
    }

    /// <summary>
    /// An dynamic EntityContainer class that allows external configuration of
    /// EntitySets for testing purposes.
    /// </summary>
    public class DynamicEntityContainer : EntityContainer 
    {
        public EntitySet<T> AddEntitySet<T>(EntitySetOperations operations) where T : Entity
        {
            base.CreateEntitySet<T>(operations);
            return GetEntitySet<T>();
        }
    }
}
