using System;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Client.Data;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test.Data
{
    [TestClass]
    public class DictListTests : UnitTestBase
    {
        private DictList<City> _dictlist;

        [TestInitialize]
        public void TestSetup()
        {
            _dictlist = new DictList<City>();
        }

        [TestMethod]
        [Description("Removing from an empty list should do nothing, as per IList interface")]
        public void RemoveFromEmpty()
        {
            _dictlist.Remove(new City());
        }

        [TestMethod]
        [Description("Removing a non-existent item should do nothing, as per IList interface")]
        public void Remove_NotContained()
        {
            _dictlist.Add(new City("a city"));
            _dictlist.Remove(new City("a different city"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "Should not be able to add duplicate value")]
        public void AddDuplicate()
        {
            var city = new City();
            _dictlist.Add(city);
            _dictlist.Add(city);
        }

        [TestMethod]
        public void Remove()
        {
            var city1 = new City("city 1");
            var city2 = new City("city 2");

            _dictlist.Add(city1);
            _dictlist.Add(city2);

            _dictlist.Remove(city1);

            Assert.IsFalse(_dictlist.Contains(city1), "list should not contain removed item");
            Assert.AreEqual(1, _dictlist.Count, "list of length 2 should be 1 after remove");
        }

        [TestMethod]
        public void RemoveLast()
        {
            var city1 = new City("city 1");
            var city2 = new City("city 2");

            _dictlist.Add(city1);
            _dictlist.Add(city2);
            _dictlist.Remove(city2);

            Assert.IsFalse(_dictlist.Contains(city2), "list should not contain removed item");
            Assert.AreEqual(1, _dictlist.Count, "list of length 2 should be 1 after remove");
        }
    }
}
