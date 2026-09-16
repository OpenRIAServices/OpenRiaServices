using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleStructs;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class SimpleStructDomainServiceTests
    {
        [TestMethod]
        public async Task RoundtripsSimpleStructs()
        {
            SimpleStructDomainContext domainContext = new SimpleStructDomainContext();
            SimpleStruct simple = new(42);
            CompositeSimpleStruct composite = new(7, Guid.NewGuid());

            InvokeResult<SimpleStruct> simpleResult = await domainContext.RoundtripSimpleStructAsync(simple, CancellationToken.None);
            InvokeResult<CompositeSimpleStruct> compositeResult = await domainContext.RoundtripCompositeSimpleStructAsync(composite, CancellationToken.None);

            Assert.AreEqual(simple, simpleResult.Value);
            Assert.AreEqual(composite, compositeResult.Value);
        }

        [TestMethod]
        public async Task RoundtripsSimpleStructCollections()
        {
            SimpleStructDomainContext domainContext = new SimpleStructDomainContext();
            SimpleStruct[] simple = [new(1), new(2)];
            CompositeSimpleStruct[] composite = [new(3, Guid.NewGuid()), new(4, Guid.NewGuid())];

            InvokeResult<System.Collections.Generic.IEnumerable<SimpleStruct>> simpleResult =
                await domainContext.RoundtripSimpleStructCollectionAsync(simple, CancellationToken.None);
            InvokeResult<System.Collections.Generic.IEnumerable<CompositeSimpleStruct>> compositeResult =
                await domainContext.RoundtripCompositeSimpleStructCollectionAsync(composite, CancellationToken.None);

            CollectionAssert.AreEqual(simple, simpleResult.Value.ToArray());
            CollectionAssert.AreEqual(composite, compositeResult.Value.ToArray());
        }

        [TestMethod]
        public async Task LoadsEntityWithSimpleStructKeyAndProperty()
        {
            SimpleStructDomainContext domainContext = new SimpleStructDomainContext();

            LoadResult<SimpleStructEntity> result =
                await domainContext.LoadAsync(domainContext.GetSimpleStructEntitiesQuery());

            SimpleStructEntity entity = result.Single();
            Assert.AreEqual(new SimpleStruct(1), entity.Id);
            Assert.AreEqual(
                new CompositeSimpleStruct(2, new Guid("207b1d8f-1f78-4f35-b262-b2787e1289f1")),
                entity.CompositeValue);
            Assert.AreSame(entity, domainContext.SimpleStructEntities.Single());
        }
    }
}
