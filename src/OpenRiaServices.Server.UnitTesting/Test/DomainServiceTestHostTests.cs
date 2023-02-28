using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
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
        public void ContstructorWithUser()
        {
            var userA = new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("A")));
            var userB = new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("B")));
            //var userC = new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("C")));
            var serviceProvider = new ServiceProviderStub(new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("D"))));

            var testHost1 = new DomainServiceTestHost<UserTestDomainService>(userA);
            Assert.AreSame(userA, testHost1.User);
            Assert.AreEqual("A", testHost1.Invoke(x => x.GetUsername()));

            testHost1.ServiceProvider = serviceProvider;
            Assert.AreSame(userA, testHost1.User, "User should not be changed when serviceprovider is changed");
            Assert.AreEqual("A", testHost1.Invoke(x => x.GetUsername()));

            testHost1.User = userB;
            Assert.AreSame(userB, testHost1.User, "Property should update user");
            Assert.AreEqual("B", testHost1.Invoke(x => x.GetUsername()));


            var testHostFunc = new DomainServiceTestHost<UserTestDomainService>(() => new UserTestDomainService(), userA);
            Assert.AreSame(userA, testHostFunc.User);
            Assert.AreEqual("A", testHostFunc.Invoke(x => x.GetUsername()));
        }

        [TestMethod]
        public void DefaultUserIsAuthenticated()
        {
            var defaultUser = (IPrincipal)(new ServiceProviderStub()).GetService(typeof(IPrincipal));
            bool expectedAuthenticated = defaultUser.Identity.IsAuthenticated;

            CheckIsAuthenticated("Default ctor()", new DomainServiceTestHost<UserTestDomainService>());
            CheckIsAuthenticated("Func ctor(Func<TDomainService>)", new DomainServiceTestHost<UserTestDomainService>(() => new UserTestDomainService()));

            void CheckIsAuthenticated(string scenario, DomainServiceTestHost<UserTestDomainService> testHost)
            {
                Assert.AreEqual(defaultUser.Identity.Name, testHost.User.Identity.Name);
                Assert.AreEqual(expectedAuthenticated, testHost.User.Identity.IsAuthenticated, "testHost.User.Identity.IsAuthenticated was false for scenario: {0}", scenario);
                Assert.AreEqual(expectedAuthenticated, testHost.Invoke(x => x.IsAuthenticated()), "ServiceContext.User.Identity.IsAuthenticated was false for scenario: {0}", scenario);
            };
        }

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
        public async Task IEnumerableQueryAsync()
        {
            var testHost = new DomainServiceTestHost<CityDomainService>();

            var result = await testHost.QueryAsync(s => s.GetZipsAsEnumerable(), CancellationToken.None);

            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public async Task SubmitAsync()
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

        public class UserTestDomainService : DomainService
        {
            [Invoke]
            public string GetUsername() => ServiceContext.User.Identity.Name;

            [Invoke]
            public bool IsAuthenticated() => ServiceContext.User.Identity.IsAuthenticated;
        }
    }
}
