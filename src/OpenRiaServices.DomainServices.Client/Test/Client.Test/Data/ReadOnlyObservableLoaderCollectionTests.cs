using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Client.Data;

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass]
    public class ReadOnlyObservableLoaderCollectionTests
    {
        [TestMethod]
        public void ResetShouldRaisePropertyChanges()
        {
            List<PropertyChangedEventArgs> propertyChanges = new List<PropertyChangedEventArgs>();
            List<NotifyCollectionChangedEventArgs> collectionChanges = new List<NotifyCollectionChangedEventArgs>();

            var collection = new ReadOnlyObservableLoaderCollection<string>();
            collection.CollectionChanged += (_, arg) => collectionChanges.Add(arg);
            collection.PropertyChanged += (_, arg) => propertyChanges.Add(arg);

            collection.Reset(new[] { "A", "B", "C" });

            Assert.AreEqual(1, collectionChanges.Count, "Only 1 collection change should be raised on reset");
            Assert.AreEqual(1, propertyChanges.Count, "Only 1 property change should be raised on reset");

            Assert.AreEqual(NotifyCollectionChangedAction.Reset, collectionChanges[0].Action);
            Assert.AreEqual(nameof(collection.Count), propertyChanges[0].PropertyName);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, collection);

            collection.Reset(new[] { "D", "E" });

            Assert.AreEqual(2, collectionChanges.Count, "1 addition collection change should be raised on reset");
            Assert.AreEqual(2, propertyChanges.Count, "1 extra property change should be raised on reset");

            Assert.AreEqual(NotifyCollectionChangedAction.Reset, collectionChanges[0].Action);
            Assert.AreEqual(nameof(collection.Count), propertyChanges[0].PropertyName);
            CollectionAssert.AreEqual(new[] { "D", "E" }, collection);

        }
    }
}
