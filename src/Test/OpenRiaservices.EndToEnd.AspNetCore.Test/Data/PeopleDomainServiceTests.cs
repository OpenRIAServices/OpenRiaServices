#if NET10_0_OR_GREATER

extern alias httpDomainClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using httpDomainClient::OpenRiaServices.Client.DomainClients;
using People;
using static People.PeopleDomainContext;

namespace OpenRiaServices.Client.Test
{
    /// <summary>
    /// End to end test for <see cref="DateOnly" /> and <see cref="TimeOnly" /> support,
    /// which is supported by DataContractSerializer on .NET 10 and later.
    /// </summary>
    [TestClass]
    public class PeopleDomainServiceTests
    {
        [TestMethod]
        public async Task TestDateOnlyProperty()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            LoadResult<Person> result = await domainContext.LoadAsync(domainContext.GetPersonsQuery());
            Person person1 = result.Single(p => p.Name == "Erik");
            Person person2 = result.Single(p => p.Name == "Gustav");

            Assert.HasCount(2, result);
            Assert.AreEqual(new(1970, 1, 1), person1.FavouriteDay);
            Assert.AreEqual(new(1523, 6, 6), person2.FavouriteDay);
        }

        [TestMethod]
        public async Task TestDateOnlyParameter()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            DateOnly favouriteDay = new(1970, 1, 1);
            LoadResult<Person> result = await domainContext.LoadAsync(domainContext.GetPersonsByFavouriteDayQuery(favouriteDay));

            Assert.HasCount(1, result);
            Assert.AreEqual(favouriteDay, result.Single().FavouriteDay);
        }

        [TestMethod]
        public async Task TestNullableDateOnlyParameter()
        {
            using var httpHandler = new RecordingHttpHandler(new HttpClientHandler());
            var dc = new BinaryHttpDomainClientFactory(TestURIs.RootURI, httpHandler)
                .CreateDomainClient(typeof(IPeopleDomainServiceContract), new Uri("People-PeopleDomainService", UriKind.Relative), false);

            PeopleDomainContext domainContext = new PeopleDomainContext(dc);

            DateOnly? weddingDay = new(1531, 9, 24);
            LoadResult<Person> result = await domainContext.LoadAsync(domainContext.GetPersonsByWeddingDayQuery(weddingDay));

            Assert.HasCount(1, result);
            Assert.AreEqual(weddingDay, result.Single().WeddingDay);
            Assert.AreEqual("?weddingDay=1531-09-24", httpHandler.Requests.Single().RequestUri.Query);
        }

        [TestMethod]
        public async Task TestNullableDateOnlyParameterWithNullValue()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            DateOnly? weddingDay = null;
            LoadResult<Person> result = await domainContext.LoadAsync(domainContext.GetPersonsByWeddingDayQuery(weddingDay));

            Assert.HasCount(1, result);
            Assert.IsNull(result.Single().WeddingDay);
        }

        [TestMethod]
        public async Task TestDateOnlyReturnValue()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            InvokeResult<DateOnly> result1 = await domainContext.GetFavouriteDayByNameAsync("Erik", CancellationToken.None);
            InvokeResult<DateOnly> result2 = await domainContext.GetFavouriteDayByNameAsync("Gustav", CancellationToken.None);

            Assert.AreEqual(new(1970, 1, 1), result1.Value);
            Assert.AreEqual(new(1523, 6, 6), result2.Value);
        }

        [TestMethod]
        public async Task TestNullableDateOnlyReturnValue()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            InvokeResult<DateOnly?> result1 = await domainContext.GetWeddingDayByNameAsync("Erik", CancellationToken.None);
            InvokeResult<DateOnly?> result2 = await domainContext.GetWeddingDayByNameAsync("Gustav", CancellationToken.None);

            Assert.IsNull(result1.Value);
            Assert.AreEqual(new(1531, 9, 24), result2.Value);
        }

        [TestMethod]
        public async Task TestComplexTypesWithDateOnlyProperty()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            InvokeResult<Lifespan> result1 = await domainContext.GetPersonLifespanByNameAsync("Erik", CancellationToken.None);
            InvokeResult<Lifespan> result2 = await domainContext.GetPersonLifespanByNameAsync("Gustav", CancellationToken.None);

            Assert.AreEqual(new(1997, 1, 1), result1.Value.Born);
            Assert.IsNull(result1.Value.Dead);
            Assert.AreEqual(new(1496, 5, 12), result2.Value.Born);
            Assert.AreEqual(new(1560, 9, 29), result2.Value.Dead);
        }

        [TestMethod]
        public async Task TestQueryWithTimeOnlyProperty()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            LoadResult<WorkdaySchedule> result = await domainContext.LoadAsync(domainContext.GetWorkdaySchedulesQuery());
            WorkdaySchedule schedule1 = result.Single(p => p.Id == 1);
            WorkdaySchedule schedule2 = result.Single(p => p.Id == 2);
            WorkdaySchedule schedule3 = result.Single(p => p.Id == 3);

            Assert.HasCount(3, result);
            Assert.AreEqual(new(8, 0), schedule1.StartTime);
            Assert.AreEqual(new(7, 45, 23, 555), schedule2.StartTime);
            Assert.AreEqual(new(7, 10, 0), schedule3.StartTime);
            Assert.AreEqual(new(17, 0), schedule1.EndTime);
            Assert.AreEqual(new(16, 45, 23, 555), schedule2.EndTime);
            Assert.IsNull(schedule3.EndTime);
            Assert.AreEqual(new(12, 0), schedule1.LunchBreak.StartTime);
            Assert.AreEqual(new(11, 30, 42), schedule2.LunchBreak.StartTime);
            Assert.AreEqual(new(11, 0), schedule3.LunchBreak.StartTime);
            Assert.AreEqual(new(13, 0), schedule1.LunchBreak.EndTime);
            Assert.AreEqual(new(12, 0), schedule2.LunchBreak.EndTime);
            Assert.IsNull(schedule3.LunchBreak.EndTime);
        }

        [TestMethod]
        public async Task TestTimeOnlyParameter()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            TimeOnly startTime = new(8, 0);
            LoadResult<WorkdaySchedule> result = await domainContext.LoadAsync(domainContext.GetWorkdayScheduleByStartTimeQuery(startTime));

            Assert.HasCount(1, result);
            Assert.AreEqual(startTime, result.Single().StartTime);
        }

        [TestMethod]
        public async Task TestNullableTimeOnlyParameter()
        {
            using var httpHandler = new RecordingHttpHandler(new HttpClientHandler());
            var dc = new BinaryHttpDomainClientFactory(TestURIs.RootURI, httpHandler)
                .CreateDomainClient(typeof(IPeopleDomainServiceContract), new Uri("People-PeopleDomainService", UriKind.Relative), false);

            PeopleDomainContext domainContext = new PeopleDomainContext(dc);

            TimeOnly? endTime = new(17, 0);
            LoadResult<WorkdaySchedule> result = await domainContext.LoadAsync(domainContext.GetWorkdayScheduleByEndTimeQuery(endTime));

            Assert.HasCount(1, result);
            Assert.AreEqual(endTime, result.Single().EndTime);
            Assert.AreEqual("?endTime=17%3A00%3A00.0000000", httpHandler.Requests.Single().RequestUri.Query);
        }

        [TestMethod]
        public async Task TestNullableTimeOnlyParameterWithNullValue()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            TimeOnly? endTime = null;
            LoadResult<WorkdaySchedule> result = await domainContext.LoadAsync(domainContext.GetWorkdayScheduleByEndTimeQuery(endTime));

            Assert.HasCount(1, result);
            Assert.IsNull(result.Single().EndTime);
        }

        [TestMethod]
        public async Task TestTimeOnlyReturnValue()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            InvokeResult<TimeOnly> result1 = await domainContext.GetStartTimeByIdAsync(1, CancellationToken.None);
            InvokeResult<TimeOnly> result2 = await domainContext.GetStartTimeByIdAsync(2, CancellationToken.None);
            InvokeResult<TimeOnly> result3 = await domainContext.GetStartTimeByIdAsync(3, CancellationToken.None);

            Assert.AreEqual(new(8, 0), result1.Value);
            Assert.AreEqual(new(7, 45, 23, 555), result2.Value);
            Assert.AreEqual(new(7, 10, 0), result3.Value);
        }

        [TestMethod]
        public async Task TestNullableTimeOnlyReturnValue()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            InvokeResult<TimeOnly?> result1 = await domainContext.GetEndTimeByIdAsync(1, CancellationToken.None);
            InvokeResult<TimeOnly?> result2 = await domainContext.GetEndTimeByIdAsync(2, CancellationToken.None);
            InvokeResult<TimeOnly?> result3 = await domainContext.GetEndTimeByIdAsync(3, CancellationToken.None);

            Assert.AreEqual(new(17, 0), result1.Value);
            Assert.AreEqual(new(16, 45, 23, 555), result2.Value);
            Assert.IsNull(result3.Value);
        }

        [TestMethod]
        public async Task TestComplexTypeWithTimeOnlyProperty()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();

            InvokeResult<LunchBreak> result1 = await domainContext.GetLunchBreakByIdAsync(1, CancellationToken.None);
            InvokeResult<LunchBreak> result2 = await domainContext.GetLunchBreakByIdAsync(2, CancellationToken.None);
            InvokeResult<LunchBreak> result3 = await domainContext.GetLunchBreakByIdAsync(3, CancellationToken.None);

            Assert.AreEqual(new(12, 0), result1.Value.StartTime);
            Assert.AreEqual(new(13, 0), result1.Value.EndTime);
            Assert.AreEqual(new(11, 30, 42), result2.Value.StartTime);
            Assert.AreEqual(new(12, 0), result2.Value.EndTime);
            Assert.AreEqual(new(11, 0), result3.Value.StartTime);
            Assert.IsNull(result3.Value.EndTime);
        }

        [TestMethod]
        public async Task InvokeOperationTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext();
            InvokeOperation invoke = domainContext.GetStartTimeById(1);

            await invoke;

            Assert.IsNull(invoke.Error);
            Assert.AreEqual("GetStartTimeById", invoke.OperationName);
            Assert.HasCount(1, invoke.Parameters);
            Assert.AreEqual(1, invoke.Parameters["id"]);
            Assert.AreEqual(new TimeOnly(8, 0), invoke.Value);
            //Assert.AreEqual("my user state", invoke.UserState);
        }
    }

    class RecordingHttpHandler : DelegatingHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public RecordingHttpHandler(HttpMessageHandler inner)
            : base(inner)
        { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
#endif
