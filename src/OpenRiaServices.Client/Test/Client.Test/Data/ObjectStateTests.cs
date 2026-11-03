using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolyType;
using PolyType.ReflectionProvider;

namespace OpenRiaServices.Client.Test;


public static class ObjectStateTests
{
    [TestClass]
    public class MessagePackUnusedData
    {
        [TestMethod]
        public void StateAndOriginalValueOperations()
        {
            var entity = CreateVersionedEntity("original");
            var container = new ConfigurableEntityContainer();
            container.CreateSet<VersionedEntity>(EntitySetOperations.Edit);
            container.GetEntitySet<VersionedEntity>().Attach(entity);

            //  "Extra" (MessagePackUnusedData) should be copied using ApplyState(ExtractState())
            var copy = new VersionedEntity();
            copy.ApplyState(new Dictionary<string, object>(entity.ExtractState()));
            AssertVersionedData(copy, "original");
            Assert.AreNotSame(entity.Detail, copy.Detail);

            // "Extra" (MessagePackUnusedData) are part of entity returned by GetOriginal()
            entity.Name = "modified";
            AssertVersionedData((VersionedEntity)entity.GetOriginal(), "original");

            // "Extra" (MessagePackUnusedData) are part of entity.OriginalValues
            var original = new VersionedEntity();
            original.ApplyState(ObjectStateUtility.ExtractRoundtripState(typeof(VersionedEntity), entity.OriginalValues));
            AssertVersionedData(original, "original");
            Assert.AreNotSame(entity.Details[0], original.Details[0]);

            // RejectChanges does not change "Extra" (MessagePackUnusedData) values
            ((IRevertibleChangeTracking)entity).RejectChanges();
            AssertVersionedData(entity, "original");

            entity.Name = "modified";
            var empty = new VersionedEntity { Detail = new VersionedComplexObject() };
            entity.UpdateOriginalValues(empty.ExtractState());
            ((IRevertibleChangeTracking)entity).RejectChanges();
            Assert.IsNull(entity.UnusedData);
            Assert.IsNull(entity.Detail.UnusedData);
        }

        /// <summary>
        /// Verify what happens when "Extra" (MessagePackUnusedData) property has value "original", and on Load the server sends a new "server" value
        /// </summary>
        /// <param name="loadBehavior"></param>
        [TestMethod]
        [DataRow(LoadBehavior.KeepCurrent)]
        [DataRow(LoadBehavior.MergeIntoCurrent)]
        [DataRow(LoadBehavior.RefreshCurrent)]
        public void MergeBehavior(LoadBehavior loadBehavior)
        {
            var entity = CreateVersionedEntity("original");
            var container = new ConfigurableEntityContainer();
            container.CreateSet<VersionedEntity>(EntitySetOperations.Edit);
            container.GetEntitySet<VersionedEntity>().Attach(entity);
            entity.Name = "local";
            entity.Detail.Name = "local";
            entity.Merge(CreateVersionedEntity("server"), loadBehavior);
            // Entity should have been updated with "Extra" value of ("server") from the server

            var result = ConvertMessagePack<VersionedEntity, NewVersionEntity>(entity);
            bool keepCurrent = loadBehavior == LoadBehavior.KeepCurrent;
            Assert.AreEqual(keepCurrent ? "original" : "server", result.Extra);
            Assert.AreEqual(keepCurrent ? "original" : "server", result.Detail.Extra);
            Assert.AreEqual(loadBehavior == LoadBehavior.RefreshCurrent ? "server" : "local", result.Name);
            Assert.AreEqual(loadBehavior == LoadBehavior.RefreshCurrent ? "server" : "local", result.Detail.Name);

            if (loadBehavior == LoadBehavior.MergeIntoCurrent)
                AssertVersionedData((VersionedEntity)entity.GetOriginal(), "server");
        }

        /// <summary>
        /// ApplyState should not overwrite the "Extra" (MessagePackUnusedData) property if it is not part of the state
        /// </summary>
        [TestMethod]
        public void ApplyStateTests()
        {
            var entity = CreateVersionedEntity("original");
            entity.ApplyState(new Dictionary<string, object> { { nameof(VersionedEntity.Name), "partial" } });

            Assert.AreEqual("original", ConvertMessagePack<VersionedEntity, NewVersionEntity>(entity).Extra, "MessagePackUnusedData is preserved if not part of state");

            var empty = new VersionedEntity { Detail = new VersionedComplexObject() };
            entity.ApplyState(empty.ExtractState());
            Assert.IsNull(entity.UnusedData, "null MessagePackUnusedData is copied by ExtractState + ApplyState");
            Assert.IsNull(entity.Detail.UnusedData);
        }

        private static VersionedEntity CreateVersionedEntity(string value)
        {
            return ConvertMessagePack<NewVersionEntity, VersionedEntity>(new NewVersionEntity
            {
                Id = 1,
                Name = value,
                Extra = value,
                Detail = new NewVersionComplexObject { Name = value, Extra = value },
                Details = new List<NewVersionComplexObject> { new NewVersionComplexObject { Name = value, Extra = value } }
            });
        }

        private static TTo ConvertMessagePack<TFrom, TTo>(TFrom value)
        {
            var serializer = new Nerdbank.MessagePack.MessagePackSerializer();
            var provider = ReflectionTypeShapeProvider.Default;
            using var stream = new MemoryStream();
            serializer.SerializeObject(stream, value, provider.GetTypeShapeOrThrow<TFrom>());
            return serializer.Deserialize(stream.ToArray(), provider.GetTypeShapeOrThrow<TTo>());
        }

        private static void AssertVersionedData(VersionedEntity entity, string value)
        {
            var result = ConvertMessagePack<VersionedEntity, NewVersionEntity>(entity);
            Assert.AreEqual(value, result.Name);
            Assert.AreEqual(value, result.Extra);
            Assert.AreEqual(value, result.Detail.Name);
            Assert.AreEqual(value, result.Detail.Extra);
            Assert.AreEqual(value, result.Details[0].Extra);
        }

        [DataContract]
        [RoundtripOriginal]
        public class VersionedEntity : Entity
        {
            private string _name;
            private VersionedComplexObject _detail;

            [Key]
            [DataMember]
            public int Id { get; set; }

            [DataMember]
            public string Name
            {
                get => _name;
                set
                {
                    RaiseDataMemberChanging(nameof(Name));
                    _name = value;
                    RaiseDataMemberChanged(nameof(Name));
                }
            }

            [DataMember]
            public VersionedComplexObject Detail
            {
                get => _detail;
                set
                {
                    RaiseDataMemberChanging(nameof(Detail));
                    _detail = value;
                    RaiseDataMemberChanged(nameof(Detail));
                }
            }

            [DataMember]
            public List<VersionedComplexObject> Details { get; set; }
        }

        [DataContract]
        [RoundtripOriginal]
        public class VersionedComplexObject : ComplexObject
        {
            private string _name;

            [DataMember]
            public string Name
            {
                get => _name;
                set
                {
                    RaiseDataMemberChanging(nameof(Name));
                    _name = value;
                    RaiseDataMemberChanged(nameof(Name));
                }
            }
        }

        /// <summary>
        /// Like VersionedEntity but with new properties <see cref="NewVersionEntity.Extra"/>,
        /// And <see cref="NewVersionEntity.Detail"/> and <see cref="NewVersionEntity.Details"/> using updated versions
        /// of the complex type with new properties <see cref="NewVersionComplexObject.Extra"/> that are not present in the original version.
        /// </summary>
        [DataContract]
        public class NewVersionEntity
        {
            [DataMember]
            public int Id { get; set; }
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Extra { get; set; }
            [DataMember]
            public NewVersionComplexObject Detail { get; set; }
            [DataMember]
            public List<NewVersionComplexObject> Details { get; set; }
        }

        /// <summary>
        /// Like <see cref="VersionedComplexObject"/> but with new property <see cref="NewVersionComplexObject.Extra"/> that is not present in the original version.
        /// </summary>
        [DataContract]
        public class NewVersionComplexObject
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Extra { get; set; }
        }
    }
}
