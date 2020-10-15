using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel.Security;
using System.Threading;
using System.Threading.Tasks;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices.TypeNameConflictResolution;

namespace OpenRiaServices.Server.UnitTesting.Test
{
	[TestClass]
	public class DomainServiceTestHostTests
    {
		[TestMethod]
		public async Task AssertInvokeAsyncReturnsCorrectType()
		{
            var cityDomainServiceTestHost = new UnitTesting.DomainServiceTestHost<CityDomainService>();

            var expectedResult = "Echo: Hello";

            var result = await cityDomainServiceTestHost.InvokeAsync(s => s.EchoWithDelay("Hello", TimeSpan.FromMilliseconds(1)), CancellationToken.None);
            
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void AssertInvokeWithTaskReturnsTResult()
        {
            var cityDomainServiceTestHost = new UnitTesting.DomainServiceTestHost<CityDomainService>();

            var expectedResult = "Echo: Hello";

            var result = cityDomainServiceTestHost.Invoke(s => s.EchoWithDelay("Hello", TimeSpan.FromMilliseconds(1)));

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void AssertInvokeWithTaskVoidReturnDoesNotThrowError()
        {
            var cityDomainServiceTestHost = new UnitTesting.DomainServiceTestHost<CityDomainService>();

            cityDomainServiceTestHost.Invoke(s => s.Delay(TimeSpan.FromMilliseconds(1)));
        }

        [TestMethod]
        public async Task AssertInvokeAsyncWithoutTaskReturnsTResult()
        {
            var cityDomainServiceTestHost = new UnitTesting.DomainServiceTestHost<CityDomainService>();

            var expectedResult = "Echo: Hello";

            var result = await cityDomainServiceTestHost.InvokeAsync(s => s.Echo("Hello"), CancellationToken.None);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task AssertQueryAsync()
        {
            var cityDomainServiceTestHost = new UnitTesting.DomainServiceTestHost<CityDomainService>();

            var result = await cityDomainServiceTestHost.QueryAsync(s => s.GetZips(), CancellationToken.None);

            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public async Task AssertQueryAsyncTask()
        {
            var cityDomainServiceTestHost = new UnitTesting.DomainServiceTestHost<CityDomainService>();

            var result = await cityDomainServiceTestHost.QueryAsync(s => s.GetZipsWithDelay(TimeSpan.FromMilliseconds(1)), CancellationToken.None);

            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public async Task AssertSumbitAsyncDoesNotThrow()
        {
            var cityDomainServiceTestHost = new UnitTesting.DomainServiceTestHost<CityDomainService>();

            var changeSetEntries = new HashSet<ChangeSetEntry> 
            {
                new ChangeSetEntry { Operation = DomainOperation.Insert, Entity = new Zip() }
            };
            var changeSet = new ChangeSet(changeSetEntries);

            await cityDomainServiceTestHost.SubmitAsync(changeSet, CancellationToken.None);
        }

    }
}
