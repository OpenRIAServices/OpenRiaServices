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
        private IndexedList<City> _indexedList;

        [TestInitialize]
        public void TestSetup()
        {
            _indexedList = new IndexedList<City>();
        }

        [TestMethod]
        [Description("Removing from an empty list should do nothing, as per IList interface")]
        public void RemoveFromEmpty()
        {
            _indexedList.Remove(new City());
        }

        [TestMethod]
        [Description("Removing a non-existent item should do nothing, as per IList interface")]
        public void Remove_NotContained()
        {
            _indexedList.Add(new City("a city"));
            _indexedList.Remove(new City("a different city"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "Should not be able to add duplicate value")]
        public void AddDuplicate()
        {
            var city = new City();
            _indexedList.Add(city);
            _indexedList.Add(city);
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
        }
    }
}
