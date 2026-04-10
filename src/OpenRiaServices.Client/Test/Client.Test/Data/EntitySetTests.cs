extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using Cities;
using DataTests.Northwind.LTS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using Description = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Client.Test
{

    [TestClass]
    public partial class EntitySetTests : UnitTestBase
    {
        [TestMethod]
        public void InferredAddThenAttach_EntityCollection()
        {
            NorthwindEntityContainer ec = new NorthwindEntityContainer();

            // track changes on products
            int productAdded = 0;
            int productRemoved = 0;
            int productPropertyChanged = 0;
            int productsCollectionChanged = 0;
            EntitySet<Product> productsSet = ec.GetEntitySet<Product>();
            productsSet.EntityAdded += (obj, args) => productAdded += 1;
            productsSet.EntityRemoved += (obj, args) => productRemoved += 1;
            productsSet.PropertyChanged += (obj, args) => productPropertyChanged += 1;
            ((INotifyCollectionChanged)productsSet).CollectionChanged += (obj, args) =>
                productsCollectionChanged += 1;

            // create and add order to container
            Order order = new Order
            {
                OrderID = 1
            };
            ec.LoadEntities(new Entity[] { order });
            ((IChangeTracking)ec).AcceptChanges();

            // build a detached graph of a detail and product
            Order_Detail detail = new Order_Detail
            {
                OrderID = 1,
                ProductID = 2
            };
            Product product = new Product
            {
                ProductID = 3
            };
            detail.Product = product;

            // Add detached detail to attached order.
            // Product is now tracked since it is an 'inferred' entity
            order.Order_Details.Add(detail);
            Assert.IsTrue(product.IsInferred);
            Assert.AreEqual(1, productAdded);
            Assert.AreEqual(0, productRemoved);
            Assert.AreEqual(2, productPropertyChanged);
            Assert.AreEqual(1, productsCollectionChanged);

            // now explicitly attach product
            productsSet.Attach(product);

            // should not have duplicate refernce to product
            Assert.AreEqual(1, productsSet.Count);

            // should not raise events since product was already added
            Assert.AreEqual(1, productAdded);
            Assert.AreEqual(0, productRemoved);
            Assert.AreEqual(2, productPropertyChanged);
            Assert.AreEqual(1, productsCollectionChanged);
        }

        private static EntitySet<T> CreateEntitySet<T>() where T : Entity
        {
            return CreateEntitySet<T>(EntitySetOperations.All);
        }

        private static EntitySet<T> CreateEntitySet<T>(EntitySetOperations operations) where T : Entity
        {
            DynamicEntityContainer container = new DynamicEntityContainer();
            return container.AddEntitySet<T>(operations);
        }

        private static City CreateLocalCity(string name)
        {
            return new City { Name = name, CountyName = "King", StateName = "WA" };
        }

        /// <summary>
        /// An dynamic EntityContainer class that allows external configuration of
        /// EntitySets for testing purposes.
        /// </summary>
        private class DynamicEntityContainer : EntityContainer
        {
            public EntitySet<T> AddEntitySet<T>(EntitySetOperations operations) where T : Entity
            {
                base.CreateEntitySet<T>(operations);
                return GetEntitySet<T>();
            }
        }
    }
}
