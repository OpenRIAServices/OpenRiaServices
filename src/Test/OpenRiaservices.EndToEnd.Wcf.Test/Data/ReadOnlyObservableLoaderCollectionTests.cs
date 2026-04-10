using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.Data;

namespace OpenRiaServices.Client.Test
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

            Assert.HasCount(1, collectionChanges, "Only 1 collection change should be raised on reset");
            Assert.HasCount(1, propertyChanges, "Only 1 property change should be raised on reset");

            Assert.AreEqual(NotifyCollectionChangedAction.Reset, collectionChanges[0].Action);
            Assert.AreEqual(nameof(collection.Count), propertyChanges[0].PropertyName);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, collection);

            collection.Reset(new[] { "D", "E" });

            Assert.HasCount(2, collectionChanges, "1 addition collection change should be raised on reset");
            Assert.HasCount(2, propertyChanges, "1 extra property change should be raised on reset");

            Assert.AreEqual(NotifyCollectionChangedAction.Reset, collectionChanges[0].Action);
            Assert.AreEqual(nameof(collection.Count), propertyChanges[0].PropertyName);
            CollectionAssert.AreEqual(new[] { "D", "E" }, collection);

        }
    }
}
