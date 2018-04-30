using System;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Client.Data;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test.Data
{
    [TestClass]
    public class IndexedListTests : UnitTestBase
    {
        private IndexedList _indexedList;

        [TestInitialize]
        public void TestSetup()
        {
            _indexedList = new IndexedList();
        }

        [TestMethod]
        [Description("Removing from an empty list should do nothing, as per IList interface")]
        public void RemoveFromEmpty()
        {
            Assert.AreEqual(0, _indexedList.Count);
            _indexedList.Remove(new City());
            Assert.AreEqual(0, _indexedList.Count);
        }

        [TestMethod]
        [Description("Removing a non-existent item should do nothing, as per IList interface")]
        public void Remove_NotContained()
        {
            _indexedList.Add(new City("a city"));
            Assert.AreEqual(1, _indexedList.Count);
            _indexedList.Remove(new City("a different city"));
            Assert.AreEqual(1, _indexedList.Count);
        }

        [TestMethod]
        [Description("Should not be able to add the same item twice")]
        public void AddDuplicate()
        {
            var city = new City();
            _indexedList.Add(city);
            ExceptionHelper.ExpectException<InvalidOperationException>(() => _indexedList.Add(city));
            Assert.AreEqual(1, _indexedList.Count);
        }

        [TestMethod]
        public void Remove()
        {
            var city1 = new City("city 1");
            var city2 = new City("city 2");

            _indexedList.Add(city1);
            _indexedList.Add(city2);

            _indexedList.Remove(city1);

            Assert.IsFalse(_indexedList.Contains(city1), "list should not contain removed item");
            Assert.AreEqual(1, _indexedList.Count, "list of length 2 should be 1 after remove");
            Assert.AreEqual(0, _indexedList.IndexOf(city2));
        }

        [TestMethod]
        public void RemoveLast()
        {
            var city1 = new City("city 1");
            var city2 = new City("city 2");

            _indexedList.Add(city1);
            _indexedList.Add(city2);
            _indexedList.Remove(city2);

            Assert.IsFalse(_indexedList.Contains(city2), "list should not contain removed item");
            Assert.AreEqual(1, _indexedList.Count, "list of length 2 should be 1 after remove");
            Assert.AreEqual(0, _indexedList.IndexOf(city1));
        }

        [TestMethod]
        public void AddRemoveMainainsListIndexes()
        {
            var city1 = new City("city 1");
            var city2 = new City("city 2");
            var city3 = new City("city 3");

            _indexedList.Add(city1);
            _indexedList.Add(city2);
            _indexedList.Add(city3);

            _indexedList.Remove(city1);
            Assert.AreEqual(0, _indexedList.IndexOf(city2));
            Assert.AreEqual(1, _indexedList.IndexOf(city3));
        }
    }
}
