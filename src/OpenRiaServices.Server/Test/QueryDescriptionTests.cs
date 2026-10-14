using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

namespace OpenRiaServices.Server.Test
{
    [TestClass]
    public class QueryDescriptionTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DeferredQueryCachesSuccessfulResultIncludingNull(bool returnNull)
        {
            int calls = 0;
            IQueryable query = returnNull ? null : Array.Empty<DeferredEntity>().AsQueryable();
            var description = new QueryDescription(GetOperation(), new object[] { 1 }, () =>
            {
                calls++;
                return query;
            }, true);

            Assert.AreEqual(0, calls);
            Assert.IsTrue(description.IncludeTotalCount);
            Assert.AreSame(query, description.Query);
            Assert.AreSame(query, description.Query);
            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void DeferredQueryDoesNotHideFactoryFailures()
        {
            int calls = 0;
            var failure = new InvalidOperationException("Invalid client expression");
            var description = new QueryDescription(GetOperation(), new object[] { 1 }, () =>
            {
                calls++;
                throw failure;
            }, false);

            Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => _ = description.Query));
            Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => _ = description.Query));
            Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => description.ParseQuery()));
            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void ParseQueryRunsFactoryOnce()
        {
            int calls = 0;
            IQueryable query = Array.Empty<DeferredEntity>().AsQueryable();
            var description = new QueryDescription(GetOperation(), new object[] { 1 }, () =>
            {
                calls++;
                return query;
            }, false);

            description.ParseQuery();
            description.ParseQuery();
            Assert.AreSame(query, description.Query);
            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public async Task UnauthorizedQueryDoesNotBuildClientExpression()
        {
            using var service = CreateService(authenticated: false);
            bool parsed = false;
            var description = new QueryDescription(GetOperation(), new object[] { 1 }, () =>
            {
                parsed = true;
                throw new InvalidOperationException("Parser must not run");
            }, false);

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(async () =>
                await service.QueryAsync<DeferredEntity>(description, CancellationToken.None));
            Assert.IsFalse(parsed);
            Assert.IsFalse(service.MethodInvoked);
        }

        [TestMethod]
        public async Task InvalidParametersDoNotBuildClientExpression()
        {
            using var service = CreateService(authenticated: true);
            bool parsed = false;
            var description = new QueryDescription(GetOperation(), new object[] { 0 }, () =>
            {
                parsed = true;
                throw new InvalidOperationException("Parser must not run");
            }, false);

            var result = await service.QueryAsync<DeferredEntity>(description, CancellationToken.None);
            Assert.IsTrue(result.HasValidationErrors);
            Assert.IsFalse(parsed);
            Assert.IsFalse(service.MethodInvoked);
        }

        [TestMethod]
        public async Task AuthorizedQueryParsesOnceBeforeMethodAndPreservesComposition()
        {
            using var service = CreateService(authenticated: true);
            int calls = 0;
            var description = new QueryDescription(GetOperation(), new object[] { 1 }, () =>
            {
                calls++;
                Assert.IsFalse(service.MethodInvoked);
                return Array.Empty<DeferredEntity>().AsQueryable().Where(e => e.Id == 1);
            }, true);

            var result = await service.QueryAsync<DeferredEntity>(description, CancellationToken.None);
            Assert.AreEqual(1, calls);
            Assert.IsTrue(service.MethodInvoked);
            Assert.AreEqual(1, result.Result.Single().Id);
            Assert.AreEqual(1, result.TotalCount);
        }

        [TestMethod]
        public async Task ParserFailureReachesOnErrorWithoutInvokingMethod()
        {
            using var service = CreateService(authenticated: true);
            var failure = new InvalidOperationException("Invalid client expression");
            var description = new QueryDescription(GetOperation(), new object[] { 1 }, () => throw failure, false);

            var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
                await service.QueryAsync<DeferredEntity>(description, CancellationToken.None));
            Assert.AreSame(failure, thrown);
            Assert.AreSame(failure, service.LastError);
            Assert.IsFalse(service.MethodInvoked);
        }

        private static DomainOperationEntry GetOperation() =>
            DomainServiceDescription.GetDescription(typeof(DeferredQueryDomainService))
                .GetQueryMethod(nameof(DeferredQueryDomainService.GetEntities));

        private static DeferredQueryDomainService CreateService(bool authenticated)
        {
            var service = new DeferredQueryDomainService();
            var user = new MockUser("test") { IsAuthenticated = authenticated };
            service.Initialize(new DomainServiceContext(new MockDataService(), user, DomainOperationType.Query));
            return service;
        }

        [EnableClientAccess]
        public sealed class DeferredQueryDomainService : DomainService
        {
            public bool MethodInvoked { get; private set; }
            public Exception LastError { get; private set; }

            [RequiresAuthentication]
            public IQueryable<DeferredEntity> GetEntities([Range(1, 10)] int count)
            {
                MethodInvoked = true;
                return new[] { new DeferredEntity { Id = 1 }, new DeferredEntity { Id = 2 } }.AsQueryable();
            }

            protected override void OnError(DomainServiceErrorInfo errorInfo)
            {
                LastError = errorInfo.Error;
                base.OnError(errorInfo);
            }
        }

        public sealed class DeferredEntity
        {
            [Key]
            public int Id { get; set; }
        }
    }
}
