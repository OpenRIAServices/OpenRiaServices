using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

namespace OpenRiaServices.Server.UnitTesting.Test
{
	[TestClass]
	public class DomainServiceTestHostTests
    {
		[TestMethod]
		public async Task AssertInvokeAsyncReturnsCorrectType()
		{
            var testHost = new DomainServiceTestHost<CityDomainService>();

            var expectedResult = "Echo: Hello";

            var result = await testHost.InvokeAsync(s => s.EchoWithDelay("Hello", TimeSpan.FromMilliseconds(1), CancellationToken.None), CancellationToken.None);
            
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void AssertInvokeWithTaskReturnsTResult()
        {
            var testHost = new DomainServiceTestHost<CityDomainService>();

            var expectedResult = "Echo: Hello";

            var result = testHost.Invoke(s => s.EchoWithDelay("Hello", TimeSpan.FromMilliseconds(1), CancellationToken.None));

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void AssertInvokeWithTaskVoidReturnDoesNotThrowError()
        {
            var domainService = new DummyDomainService();
            var testHost = new DomainServiceTestHost<DummyDomainService>(() => domainService);

            testHost.Invoke(s => s.DummyInvoke());

            Assert.IsTrue(domainService.IsInvoked, "The method should be invoked");
        }

        [TestMethod]
        public async Task AssertInvokeAsyncWithTaskVoidReturnDoesNotThrowError()
        {
            var domainService = new DummyDomainService();
            var testHost = new DomainServiceTestHost<DummyDomainService>(() => domainService);

            await testHost.InvokeAsync(s => s.DummyInvoke());

            Assert.IsTrue(domainService.IsInvoked, "The method should be invoked");
        }

        [TestMethod]
        public async Task AssertInvokeAsyncWithoutTaskReturnsTResult()
        {
            var testHost = new DomainServiceTestHost<CityDomainService>();

            var expectedResult = "Echo: Hello";

            var result = await testHost.InvokeAsync(s => s.Echo("Hello"), CancellationToken.None);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task AssertQueryAsync()
        {
            var testHost = new DomainServiceTestHost<CityDomainService>();

            var result = await testHost.QueryAsync(s => s.GetZips(), CancellationToken.None);

            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public async Task QueryAsync()
        {
            var testHost = new DomainServiceTestHost<CityDomainService>();

            var result = await testHost.QueryAsync(s => s.GetZipsWithDelay(TimeSpan.FromMilliseconds(1)), CancellationToken.None);

            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public async Task SumbitAsync()
        {
            var testHost = new DomainServiceTestHost<ServerSideAsyncDomainService>();

            var changeSetEntries = new HashSet<ChangeSetEntry> 
            {
                new ChangeSetEntry { Operation = DomainOperation.Insert, Entity = new RangeItem() }
            };
            var changeSet = new ChangeSet(changeSetEntries);

            await testHost.SubmitAsync(changeSet, CancellationToken.None);
        }

        [TestMethod]
        public async Task InsertAsync()
        {
            var testHost = new DomainServiceTestHost<ServerSideAsyncDomainService>();

            var rangeItem = new RangeItem();

            await testHost.InsertAsync(rangeItem, CancellationToken.None);

            Assert.AreEqual(42, rangeItem.Id);
        }

        [TestMethod]
        public async Task UpdateAsync()
        {
            var testHost = new DomainServiceTestHost<ServerSideAsyncDomainService>();

            var rangeItem = new RangeItem();

            await testHost.UpdateAsync(rangeItem);

            Assert.AreEqual("updated", rangeItem.Text);
        }

        [TestMethod]
        public async Task DeleteAsync()
        {
            var testHost = new DomainServiceTestHost<ServerSideAsyncDomainService>();

            var rangeItem = new RangeItem();

            await testHost.DeleteAsync(rangeItem);

            Assert.AreEqual("deleted", rangeItem.Text);
        }

        public class DummyDomainService : DomainService
        {
            public bool IsInvoked { get; set; }

            [Invoke]
            public async Task DummyInvoke()
            {
                await Task.Delay(10);
                IsInvoked = true;
            }
        }

    }
}
