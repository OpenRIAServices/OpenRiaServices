extern alias SSmDsClient;
using System;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;

    [TestClass]
    public class EntityKeyTests : UnitTestBase
    {
        /// <summary>
        /// Test the EntityKey APIs directly
        /// </summary>
        [TestMethod]
        public void EntityKey_Creation()
        {
            // Test one of the generic Create overloads (that doesn't box
            // the key values)
            EntityKey key = EntityKey.Create(5, 2.34M, "test", "hello world");
            string formattedKey = key.ToString();

            object[] keyValues = [5, 2.34M, "test", "hello world"];
            string expectedKey = "{" + string.Join(",", keyValues) + "}";
            Assert.AreEqual(expectedKey, formattedKey);

            // pass the same set into the params version and verify the keys are equal
            EntityKey key2 = EntityKey.Create(keyValues);
            Assert.AreSame(key.GetType(), key2.GetType());
            Assert.AreEqual(key, key2);
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode());

            // Test boxing version for N key values
            key = EntityKey.Create(5, "hello", 3, 4, 5, "test", 2132, '?');
            formattedKey = key.ToString();
            Assert.AreEqual("{5,hello,3,4,5,test,2132,?}", formattedKey);

            // pass the same set of values into the params version and verify the keys are equal
            key2 = EntityKey.Create([5, "hello", 3, 4, 5, "test", 2132, '?']);
            Assert.AreEqual(key, key2);
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode());
        }

        [TestMethod]
        public void EntityKey_CopyKeyValuesTo()
        {
            VerifyKeyValues(EntityKey.Create(1, "two"), [1, "two"]);
            VerifyKeyValues(EntityKey.Create(1, "two", 3M), [1, "two", 3M]);
            VerifyKeyValues(EntityKey.Create(1, "two", 3M, '4', true), [1, "two", 3M, '4', true]);

            // Wrong lenght destinatio should throw ArgumentException
            Assert.Throws<ArgumentException>(() => EntityKey.Create(1, "two").CopyKeyValuesTo(new object[1]));
            Assert.Throws<ArgumentException>(() => EntityKey.Create(1, "two").CopyKeyValuesTo(new object[3]));

            static void VerifyKeyValues(EntityKey key, object[] expectedValues)
            {
                object[] actualValues = new object[expectedValues.Length];
                key.CopyKeyValuesTo(actualValues);
                Assert.AreSequenceEqual(expectedValues, actualValues);
            }
        }

        [TestMethod]
        public void EntityKey_NullValues()
        {
            string expectedMsg = new ArgumentNullException("value", Resource.EntityKey_CannotBeNull).Message;

            Assert.Throws<ArgumentNullException>(() => EntityKey.Create([5, null, "test"]), expectedMsg);
            Assert.Throws<ArgumentNullException>(() => EntityKey.Create([5, null]), expectedMsg);
        }

        /// <summary>
        /// Verify that for a multipart key, the key created returns
        /// the same hash code that would result from ORing the values
        /// together
        /// </summary>
        [TestMethod]
        public void EntityKey_TestHashCodeValues()
        {
            Guid g = Guid.NewGuid();
            object[] keyValues = [123, 34.5M, "hello", new DateTime(234234), g, false, '?'];
            EntityKey key = EntityKey.Create(keyValues);
            int hashCode = key.GetHashCode();
            int expectedHashCode = 0;
            foreach (object keyValue in keyValues)
            {
                expectedHashCode ^= keyValue.GetHashCode();
            }
            Assert.AreEqual(expectedHashCode, hashCode);

            // test the non-boxed version and verify we get the same hashcode
            key = EntityKey.Create(123, 34.5M, "hello", new DateTime(234234), g, false, '?');
            hashCode = key.GetHashCode();
            Assert.AreEqual(expectedHashCode, hashCode);

            // compute directly without boxing and verify equal
            int directlyComputed = (123).GetHashCode() ^ (34.5M).GetHashCode() ^ "hello".GetHashCode() ^
                new DateTime(234234).GetHashCode() ^ g.GetHashCode() ^ false.GetHashCode() ^ '?'.GetHashCode();
            Assert.AreEqual(directlyComputed, key.GetHashCode());
        }
    }
}
