#if NET

using System;
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

#if NET10_0_OR_GREATER
            Assert.AreEqual(new System.DateOnly(1997, 1, 1), person1Birthday);
            Assert.AreEqual(new System.DateOnly(1496, 5, 12), person2Birthday);
#else
            Assert.AreEqual(new System.DateOnly(1, 1, 1), person1Birthday);
            Assert.AreEqual(new System.DateOnly(1, 1, 1), person2Birthday);
#endif
        }
    }
}
#endif
