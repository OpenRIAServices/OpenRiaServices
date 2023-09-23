using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.Internal;

namespace OpenRiaServices.Client.Test.Reflection
{
    [TestClass]
    public class MetaTypeTests
    {
        [TestMethod]
        public void ShouldIgnorePropertiesWithIgnoreDataMember()
        {
            var metaType = MetaType.GetMetaType(typeof(EntityWithIgnoreProperty));
            var entity = new EntityWithIgnoreProperty()
            {
                Id = 1,
                IgnoredProperty = "ignored",
            };

            Assert.AreEqual(1, metaType.DataMembers.Count(), "DataMembers should not include ignored members");
            Assert.AreEqual(nameof(EntityWithIgnoreProperty.Id), metaType.DataMembers.First().Name);

            var state = ObjectStateUtility.ExtractState(entity);
            Assert.AreEqual(1, state.Count, "Extract state should only include non ignored properties");

            ObjectStateUtility.ApplyState(entity, new Dictionary<string, object>
            {
                { nameof(EntityWithIgnoreProperty.Id), (object)2},
                { nameof(EntityWithIgnoreProperty.IgnoredProperty), null},
            });
            Assert.AreEqual(2, entity.Id, "ApplyState should not change ignored properties");
            Assert.IsNotNull(entity.IgnoredProperty, "ApplyState should not change ignored properties");
        }

        public class EntityWithIgnoreProperty : Entity
        {
            public int Id { get; set; }

            [IgnoreDataMember]
            public string IgnoredProperty { get; set; }

            [IgnoreDataMember]
            public string ThrowingProperty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }
    }
}
