#if NET10_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using People;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class PeopleDomainServiceTests
    {
        [TestMethod]
        public async Task GetPersonsQueryTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            Assert.HasCount(0, domainContext.Persons);
            
            await domainContext.Load(domainContext.GetPersonsQuery(), false);
            Assert.HasCount(2, domainContext.Persons);

            Person person1 = domainContext.Persons.Single(p => p.Name == "Erik");
            Person person2 = domainContext.Persons.Single(p => p.Name == "Gustav");

            Assert.AreEqual(new DateOnly(1970, 1, 1), person1.FavouriteDay);
            Assert.AreEqual(new DateOnly(1523, 6, 6), person2.FavouriteDay);
        }

        [TestMethod]
        public async Task GetPersonsByFavouriteDayQueryTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            Assert.HasCount(0, domainContext.Persons);

            DateOnly favouriteDay = new DateOnly(1970, 1, 1);
            await domainContext.Load(domainContext.GetPersonsByFavouriteDayQuery(favouriteDay), false);

            Assert.HasCount(1, domainContext.Persons);
            Assert.AreEqual(favouriteDay, domainContext.Persons.Single().FavouriteDay);
        }

        [TestMethod]
        public async Task GetPersonsByNonNullWeddingDayQueryTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            Assert.HasCount(0, domainContext.Persons);

            DateOnly? weddingDay = new DateOnly(1531, 9, 24);
            await domainContext.Load(domainContext.GetPersonsByWeddingDayQuery(weddingDay), false);

            Assert.HasCount(1, domainContext.Persons);
            Assert.AreEqual(weddingDay, domainContext.Persons.Single().WeddingDay);
        }

        [TestMethod]
        public async Task GetPersonsByNullWeddingDayQueryTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            Assert.HasCount(0, domainContext.Persons);

            DateOnly? weddingDay = null;
            await domainContext.Load(domainContext.GetPersonsByWeddingDayQuery(weddingDay), false);

            Assert.HasCount(1, domainContext.Persons);
            Assert.AreEqual(weddingDay, domainContext.Persons.Single().WeddingDay);
            Assert.IsNull(domainContext.Persons.Single().WeddingDay);
        }

        [TestMethod]
        public async Task GetFavouriteDayByNameTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            InvokeResult<DateOnly> result1 = await domainContext.GetFavouriteDayByNameAsync("Erik", System.Threading.CancellationToken.None);
            InvokeResult<DateOnly> result2 = await domainContext.GetFavouriteDayByNameAsync("Gustav", System.Threading.CancellationToken.None);
            Assert.AreEqual(new DateOnly(1970, 1, 1), result1.Value);
            Assert.AreEqual(new DateOnly(1523, 6, 6), result2.Value);
        }

        [TestMethod]
        public async Task GetWeddingDayByNameTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            
            InvokeResult<DateOnly?> result1 = await domainContext.GetWeddingDayByNameAsync("Erik", System.Threading.CancellationToken.None);
            InvokeResult<DateOnly?> result2 = await domainContext.GetWeddingDayByNameAsync("Gustav", System.Threading.CancellationToken.None);
            
            Assert.IsNull(result1.Value);
            Assert.AreEqual(new DateOnly(1531, 9, 24), result2.Value);
        }
    }
}
#endif
