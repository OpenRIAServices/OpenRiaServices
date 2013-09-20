using System.Linq;
using System.ServiceModel.DomainServices.Client;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.ServiceModel.DomainServices.Client.Common.Test
{
    [TestClass]
    public class EntityTests : UnitTestBase
    {
        [TestMethod]
        [WorkItem(183155)]
        public void CheckEntitySetProtectedProperty()
        {
            CityEntityContainer ec = new CityEntityContainer();
            ec.LoadEntities(new Entity[] { 
                new City { Name = "Redmond", CountyName = "King", StateName = "WA" },
                new City { Name = "Bellevue", CountyName = "King", StateName = "WA" }
            });

            EntitySet<City> es = ec.Cities;

            City c1 = ec.Cities.First();
            Assert.AreEqual(c1.GetEntitySet(), es);

            ec.Cities.Detach(c1);
            Assert.IsNull(c1.GetEntitySet());

            City c2 = new City() { Name = "NewCity", CountyName = "King", StateName = "WA" };
            ec.Cities.Add(c2);
            Assert.AreEqual(c2.GetEntitySet(), es);

            City c3 = new City() { Name = "AnotherNewCity", CountyName = "King", StateName = "WA" };
            ec.Cities.Attach(c3);
            Assert.AreEqual(c3.GetEntitySet(), es);

            City c4 = ec.Cities.Last();
            ec.Cities.Remove(c4);
            Assert.IsNull(c4.GetEntitySet());

        }
    }

    public class CityEntityContainer : EntityContainer
    {
        public CityEntityContainer()
        {
            CreateEntitySet<City>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
        }

        public EntitySet<City> Cities
        {
            get
            {
                return GetEntitySet<City>();
            }
        }
    }
}

namespace Cities
{
    public partial class City : Entity
    {
        internal EntitySet GetEntitySet()
        {
            return this.EntitySet;
        }
    }
}
