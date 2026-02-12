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

            DateOnly person1Birthday = domainContext.Persons.Single(p => p.Name == "Erik").Birthday;
            DateOnly person2Birthday = domainContext.Persons.Single(p => p.Name == "Gustav").Birthday;

            Assert.AreEqual(new System.DateOnly(1997, 1, 1), person1Birthday);
            Assert.AreEqual(new System.DateOnly(1496, 5, 12), person2Birthday);
        }

        [TestMethod]
        public async Task GetPersonsByDateQueryTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            Assert.HasCount(0, domainContext.Persons);

            DateOnly birthday = new DateOnly(1997, 1, 1);
            await domainContext.Load(domainContext.GetPersonsByDateQuery(birthday), false);

            Assert.HasCount(1, domainContext.Persons);
            Assert.AreEqual(birthday, domainContext.Persons.Single().Birthday);
        }

        [TestMethod]
        public async Task GetBirthdaysTest()
        {
            PeopleDomainContext domainContext = new PeopleDomainContext(TestURIs.People);
            InvokeResult<IEnumerable<DateOnly>> result = await domainContext.GetBirthdaysAsync(System.Threading.CancellationToken.None);
            Assert.HasCount(2, result.Value);
        }
    }
}
#endif
