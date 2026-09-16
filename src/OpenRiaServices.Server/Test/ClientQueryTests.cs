using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Server.Test
{
    [TestClass]
    public class ClientQueryTests
    {
        [TestMethod]
        public async Task QueryMethod_AppliesClientQueryBeforeReturningList()
        {
            ClientQueryDomainService service = CreateService();
            DomainOperationEntry operation = DomainServiceDescription.GetDescription(typeof(ClientQueryDomainService))
                .GetQueryMethod(nameof(ClientQueryDomainService.GetEntities));
            IQueryable<ClientQueryEntity> query = Array.Empty<ClientQueryEntity>().AsQueryable()
                .Where(entity => entity.Id > 1)
                .OrderByDescending(entity => entity.Id)
                .Skip(1)
                .Take(2);
            QueryDescription description = new QueryDescription(operation, new object[] { 2 }, true, query);

            ServiceQueryResult<ClientQueryEntity> result =
                await service.QueryAsync<ClientQueryEntity>(description, CancellationToken.None);

            CollectionAssert.AreEqual(new[] { 4, 3 }, result.Result.Select(entity => entity.Id).ToArray());
            Assert.AreEqual(4, result.TotalCount);
            Assert.AreEqual(1, description.ClientQueryApplyCount);
            Assert.IsTrue(operation.HasClientQueryParameter);
            Assert.HasCount(1, operation.Parameters);
            Assert.AreEqual(typeof(int), operation.Parameters[0].ParameterType);
            Assert.IsTrue(service.ReceivedCancellationToken);
        }

        [TestMethod]
        public async Task QueryMethod_DoesNotFailWhenClientQueryIsNotApplied()
        {
            ClientQueryDomainService service = CreateService();
            DomainOperationEntry operation = DomainServiceDescription.GetDescription(typeof(ClientQueryDomainService))
                .GetQueryMethod(nameof(ClientQueryDomainService.GetEntitiesWithoutApplying));
            IQueryable<ClientQueryEntity> query = Array.Empty<ClientQueryEntity>().AsQueryable()
                .Where(entity => entity.Id == 1);
            QueryDescription description = new QueryDescription(operation, Array.Empty<object>(), true, query);

            ServiceQueryResult<ClientQueryEntity> result =
                await service.QueryAsync<ClientQueryEntity>(description, CancellationToken.None);

            Assert.HasCount(2, result.Result);
            Assert.AreEqual(5, result.TotalCount);
            Assert.AreEqual(0, description.ClientQueryApplyCount);
        }

        [TestMethod]
        public async Task SetTotalCount_AllowsMultipleApplyCalls()
        {
            ClientQueryDomainService service = CreateService();
            DomainOperationEntry operation = DomainServiceDescription.GetDescription(typeof(ClientQueryDomainService))
                .GetQueryMethod(nameof(ClientQueryDomainService.GetEntitiesWithExplicitCount));
            IQueryable<ClientQueryEntity> query = Array.Empty<ClientQueryEntity>().AsQueryable().Take(1);
            QueryDescription description = new QueryDescription(operation, Array.Empty<object>(), true, query);

            ServiceQueryResult<ClientQueryEntity> result =
                await service.QueryAsync<ClientQueryEntity>(description, CancellationToken.None);

            Assert.HasCount(1, result.Result);
            Assert.AreEqual(12, result.TotalCount);
            Assert.AreEqual(2, description.ClientQueryApplyCount);
        }

        [TestMethod]
        public void ApplyTo_AllowsMultipleCallsWhenTotalCountIsNotRequested()
        {
            ClientQuery<ClientQueryEntity> query = new ClientQuery<ClientQueryEntity>(null, false, 0);
            IQueryable<ClientQueryEntity> source = Enumerable.Range(1, 2)
                .Select(id => new ClientQueryEntity { Id = id })
                .AsQueryable();

            query.ApplyTo(source);
            query.ApplyTo(source);

            Assert.AreEqual(2, query.ApplyCount);
        }

        [TestMethod]
        public void ApplyTo_RejectsMultipleCallsWhenAutomaticTotalCountIsRequested()
        {
            ClientQuery<ClientQueryEntity> query = new ClientQuery<ClientQueryEntity>(null, true, 0);
            IQueryable<ClientQueryEntity> source = Array.Empty<ClientQueryEntity>().AsQueryable();
            query.ApplyTo(source);

            Assert.ThrowsExactly<InvalidOperationException>(() => query.ApplyTo(source));
        }

        [TestMethod]
        public void Description_RejectsClientQueryWithLegacyOutCount()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                DomainServiceDescription.GetDescription(typeof(ClientQueryWithOutCountDomainService)));
        }

        [TestMethod]
        public void Description_RejectsMismatchedClientQueryEntityType()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                DomainServiceDescription.GetDescription(typeof(MismatchedClientQueryDomainService)));
        }

        [TestMethod]
        public void Description_RejectsMultipleClientQueryParameters()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                DomainServiceDescription.GetDescription(typeof(MultipleClientQueryDomainService)));
        }

        private static ClientQueryDomainService CreateService()
        {
            ClientQueryDomainService service = new ClientQueryDomainService();
            service.Initialize(new DomainServiceContext(
                new MockDataService(),
                new MockUser("client-query"),
                DomainOperationType.Query));
            return service;
        }
    }

    [EnableClientAccess]
    public class ClientQueryDomainService : DomainService
    {
        private readonly IQueryable<ClientQueryEntity> _entities = Enumerable.Range(1, 5)
            .Select(id => new ClientQueryEntity { Id = id })
            .AsQueryable();

        public bool ReceivedCancellationToken { get; private set; }

        [Query]
        public Task<List<ClientQueryEntity>> GetEntities(
            int minimumId,
            ClientQuery<ClientQueryEntity> query,
            CancellationToken cancellationToken)
        {
            ReceivedCancellationToken = cancellationToken == ServiceContext.CancellationToken;
            return Task.FromResult(query.ApplyTo(_entities.Where(entity => entity.Id >= minimumId)).ToList());
        }

        [Query(ResultLimit = 2)]
        public List<ClientQueryEntity> GetEntitiesWithoutApplying(ClientQuery<ClientQueryEntity> query)
        {
            return _entities.ToList();
        }

        [Query]
        public List<ClientQueryEntity> GetEntitiesWithExplicitCount(ClientQuery<ClientQueryEntity> query)
        {
            query.SetTotalCount(12);
            query.ApplyTo(_entities);
            return query.ApplyTo(_entities).ToList();
        }

        protected override ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            return new ValueTask<int>(query.Count());
        }
    }

    [EnableClientAccess]
    public class ClientQueryWithOutCountDomainService : DomainService
    {
        [Query]
        public IEnumerable<ClientQueryEntity> GetEntities(ClientQuery<ClientQueryEntity> query, out int totalCount)
        {
            totalCount = 0;
            return Array.Empty<ClientQueryEntity>();
        }
    }

    [EnableClientAccess]
    public class MismatchedClientQueryDomainService : DomainService
    {
        [Query]
        public IEnumerable<ClientQueryEntity> GetEntities(ClientQuery<OtherClientQueryEntity> query)
        {
            return Array.Empty<ClientQueryEntity>();
        }
    }

    [EnableClientAccess]
    public class MultipleClientQueryDomainService : DomainService
    {
        [Query]
        public IEnumerable<ClientQueryEntity> GetEntities(
            ClientQuery<ClientQueryEntity> first,
            ClientQuery<ClientQueryEntity> second)
        {
            return Array.Empty<ClientQueryEntity>();
        }
    }

    public class ClientQueryEntity
    {
        [Key]
        public int Id { get; set; }
    }

    public class OtherClientQueryEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
