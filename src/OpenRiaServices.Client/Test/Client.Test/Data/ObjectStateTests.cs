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
        public void StateAndOriginalCopies()
        {
            var entity = CreateVersionedEntity("original");
            var container = new ConfigurableEntityContainer();
            container.CreateSet<VersionedEntity>(EntitySetOperations.Edit);
            container.GetEntitySet<VersionedEntity>().Attach(entity);

            var copy = new VersionedEntity();
            copy.ApplyState(new Dictionary<string, object>(entity.ExtractState()));
            AssertVersionedData(copy, "original");
            Assert.AreNotSame(entity.Detail, copy.Detail);

            ((IEditableObject)entity).BeginEdit();
            entity.Name = "edit";
            ((IEditableObject)entity.Detail).BeginEdit();
            entity.Detail.Name = "edit";
            ((IEditableObject)entity.Detail).CancelEdit();
            ((IEditableObject)entity).CancelEdit();
            AssertVersionedData(entity, "original");

            entity.Name = "modified";
            AssertVersionedData((VersionedEntity)entity.GetOriginal(), "original");

            var original = new VersionedEntity();
            original.ApplyState(ObjectStateUtility.ExtractRoundtripState(typeof(VersionedEntity), entity.OriginalValues));
            AssertVersionedData(original, "original");
            Assert.AreNotSame(entity.Details[0], original.Details[0]);

            ((IRevertibleChangeTracking)entity).RejectChanges();
            AssertVersionedData(entity, "original");

            entity.Name = "modified";
            var empty = new VersionedEntity { Detail = new VersionedComplexObject() };
            entity.UpdateOriginalValues(empty.ExtractState());
            ((IRevertibleChangeTracking)entity).RejectChanges();
            Assert.IsNull(entity.UnusedData);
            Assert.IsNull(entity.Detail.UnusedData);
        }

        [TestMethod]
        [DataRow(LoadBehavior.KeepCurrent)]
        [DataRow(LoadBehavior.MergeIntoCurrent)]
        [DataRow(LoadBehavior.RefreshCurrent)]
        public void Merge(LoadBehavior loadBehavior)
        {
            var entity = CreateVersionedEntity("original");
            var container = new ConfigurableEntityContainer();
            container.CreateSet<VersionedEntity>(EntitySetOperations.Edit);
            container.GetEntitySet<VersionedEntity>().Attach(entity);
            entity.Name = "local";
            entity.Detail.Name = "local";
            entity.Merge(CreateVersionedEntity("server"), loadBehavior);

            var result = ConvertMessagePack<VersionedEntity, NewVersionEntity>(entity);
            bool keepCurrent = loadBehavior == LoadBehavior.KeepCurrent;
            Assert.AreEqual(keepCurrent ? "original" : "server", result.Extra);
            Assert.AreEqual(keepCurrent ? "original" : "server", result.Detail.Extra);
            Assert.AreEqual(loadBehavior == LoadBehavior.RefreshCurrent ? "server" : "local", result.Name);
            Assert.AreEqual(loadBehavior == LoadBehavior.RefreshCurrent ? "server" : "local", result.Detail.Name);

            if (loadBehavior == LoadBehavior.MergeIntoCurrent)
                AssertVersionedData((VersionedEntity)entity.GetOriginal(), "server");
        }

        [TestMethod]
        public void PartialStateAndEmptySnapshot()
        {
            var entity = CreateVersionedEntity("original");
            entity.ApplyState(new Dictionary<string, object> { { nameof(VersionedEntity.Name), "partial" } });
            Assert.AreEqual("original", ConvertMessagePack<VersionedEntity, NewVersionEntity>(entity).Extra);

            var empty = new VersionedEntity { Detail = new VersionedComplexObject() };
            entity.ApplyState(empty.ExtractState());
            Assert.IsNull(entity.UnusedData);
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
