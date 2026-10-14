using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.Serialization;
using OpenRiaServices.Server;
using OpenRiaServices.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Linq.Dynamic.UnitTests
{
    /// <summary>
    /// Tests <see cref="QueryDeserializer"/> members.
    /// </summary>
    [TestClass]
    public class QueryDeserializerTest
    {
        [TestMethod]
        [Description("Verify that exponents in doubles are supported.")]
        public void TestExponents()
        {
            IQueryable<EntityWithExcludedMember> queryable = new EntityWithExcludedMember[] {
                new EntityWithExcludedMember() { Key = 1, Double = 2E+2 },
                new EntityWithExcludedMember() { Key = 2, Double = 2E+3 },
            }.AsQueryable();

            List<ServiceQueryPart> queryParts = new List<ServiceQueryPart>()
            {
                new ServiceQueryPart("where", "it.Double < 2.5E+2")
            };

            queryable = (IQueryable<EntityWithExcludedMember>)QueryDeserializer.Deserialize(DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService)), queryable, queryParts);
            Assert.AreEqual(1, queryable.Count());
            Assert.AreEqual(1, queryable.Single().Key);
        }

        [TestMethod]
        [Description("Verify that we check constructor access, for instance disallowing a string constructor that takes a char and a length")]
        public void TestConstructorAccessChecked()
        {
            IQueryable<EntityWithExcludedMember> queryable = Array.Empty<EntityWithExcludedMember>().AsQueryable();

            List<ServiceQueryPart> queryParts = new List<ServiceQueryPart>()
            {
                new ServiceQueryPart("where", "it.Double.ToString() == String('a', 1000)")
            };

            Assert.ThrowsExactly<ParseException>(() =>
            {
                QueryDeserializer.Deserialize(DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService)), queryable, queryParts);
            }, string.Format(CultureInfo.CurrentCulture, System.Linq.Dynamic.Resource.MethodsAreInaccessible + " (at index 24)", typeof(String).Name));
        }

        [TestMethod]
        [Description("Tests that excluded members can't be accessed by a query.")]
        [WorkItem(783040)]
        public void PostProcessor_CannotAccessExcludedMembers()
        {
            AccessMember(typeof(EntityWithExcludedMember), "ExcludedMember");
            AccessMember(typeof(EntityWithExcludedMember), "NonDataMember");
        }

        [TestMethod]
        [DataRow("where", "it.ExcludedField == \"Whatever\"", "ExcludedField", typeof(EntityWithExcludedMember))]
        [DataRow("where", "it.NonDataMemberField == \"Whatever\"", "NonDataMemberField", typeof(EntityWithExcludedMember))]
        [DataRow("where", "it.DataMemberField == \"Whatever\"", "DataMemberField", typeof(EntityWithExcludedMember))]
        [DataRow("where", "it.IgnoredField == \"Whatever\"", "IgnoredField", typeof(EntityWithExcludedMember))]
        [DataRow("orderby", "it.ExcludedField", "ExcludedField", typeof(EntityWithExcludedMember))]
        [DataRow("where", "it.Complex.HiddenField == \"Whatever\"", "HiddenField", typeof(QueryComplexValue))]
        public void PostProcessor_CannotAccessFields(string queryOperator, string expression, string memberName, Type receiverType)
        {
            // Fields are never generated on the client, so we should always throw an exception when they are accessed in a query.
            var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
                Deserialize(new EntityWithExcludedMember(), queryOperator, expression));
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.UnknownPropertyOrField, memberName, receiverType.Name), exception.Message);
        }

        [TestMethod]
        [DataRow("where")]
        [DataRow("orderby")]
        public void PostProcessor_CannotAccessInheritedExcludedMember(string queryOperator)
        {
            string expression = queryOperator == "where" ? "it.InheritedExcludedMember == \"Whatever\"" : "it.InheritedExcludedMember";
            var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
                Deserialize(new EntityWithExcludedMember(), queryOperator, expression));
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.UnknownPropertyOrField, "InheritedExcludedMember", nameof(EntityWithExcludedMember)), exception.Message);
        }

        [TestMethod]
        [DataRow("it[0] == \"Whatever\"")]
        [DataRow("it[\"key\"] == \"Whatever\"")]
        public void PostProcessor_CannotAccessCustomIndexers(string expression)
        {
            var entity = new EntityWithExcludedMember();
            var exception = Assert.ThrowsExactly<InvalidOperationException>(() => Deserialize(entity, "where", expression));
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.UnknownPropertyOrField, "Item", nameof(EntityWithExcludedMember)), exception.Message);
            Assert.AreEqual(0, entity.IndexerCalls);
        }

        [TestMethod]
        public void Parser_CannotAccessIndexerWithoutArguments()
        {
            Assert.ThrowsExactly<ParseException>(() => Deserialize(new EntityWithExcludedMember(), "where", "it.Item == \"Whatever\""));
        }

        [TestMethod]
        public void PostProcessor_CollectionSubclassCannotExposeCustomIndexer()
        {
            var values = new IndexedList { "Whatever" };
            Assert.AreEqual(1, Deserialize(values, "where", "it[0] == \"Whatever\"").Cast<IndexedList>().Count());
            Assert.ThrowsExactly<InvalidOperationException>(() => Deserialize(values, "where", "it[\"key\"] == \"Whatever\""));
            Assert.AreEqual(0, values.IndexerCalls);
        }

        [TestMethod]
        public void PostProcessor_AllowsStandardIndexing()
        {
            AssertMatch("Whatever", "it[0] == 'W'");
            AssertMatch(new[] { "Whatever" }, "it[0] == \"Whatever\"");
            AssertMatch(new List<string> { "Whatever" }, "it[0] == \"Whatever\"");
            AssertMatch<IList<string>>(new List<string> { "Whatever" }, "it[0] == \"Whatever\"");
            AssertMatch<IReadOnlyList<string>>(new List<string> { "Whatever" }, "it[0] == \"Whatever\"");
            AssertMatch(new ReadOnlyCollection<string>(new[] { "Whatever" }), "it[0] == \"Whatever\"");
            AssertMatch(new Dictionary<string, string> { { "key", "Whatever" } }, "it[\"key\"] == \"Whatever\"");
            AssertMatch<IReadOnlyDictionary<string, string>>(new Dictionary<string, string> { { "key", "Whatever" } }, "it[\"key\"] == \"Whatever\"");
        }

        [TestMethod]
        [DataRow("String.Empty == \"\"")]
        [DataRow("Int32.MaxValue > 0")]
        [DataRow("Math.PI > 3")]
        [DataRow("Guid.Empty == Guid(\"00000000-0000-0000-0000-000000000000\")")]
        public void PostProcessor_AllowsStaticConstants(string expression)
        {
            AssertMatch(new EntityWithExcludedMember(), expression);
        }

        private static IQueryable Deserialize<T>(T value, string queryOperator, string expression)
        {
            return QueryDeserializer.Deserialize(DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService)),
                new[] { value }.AsQueryable(), new[] { new ServiceQueryPart(queryOperator, expression) });
        }

        [TestMethod]
        [DataRow("where", "it.HiddenRelated != null", "HiddenRelated")]
        [DataRow("where", "it.HiddenRelated.Secret == \"Whatever\"", "HiddenRelated")]
        [DataRow("where", "it.HiddenRelated.Secret.StartsWith(\"W\")", "HiddenRelated")]
        [DataRow("orderby", "it.HiddenRelated.Secret", "HiddenRelated")]
        [DataRow("where", "it.HiddenRelatedCollection.Any(Secret == \"Whatever\")", "HiddenRelatedCollection")]
        [DataRow("where", "it.HiddenRelatedCollection.Count > 0", "HiddenRelatedCollection")]
        [DataRow("orderby", "it.HiddenRelatedCollection.Count", "HiddenRelatedCollection")]
        public void PostProcessor_CannotAccessExcludedNavigations(string queryOperator, string expression, string memberName)
        {
            var related = new QueryRelatedEntity { Key = 1, Secret = "Whatever" };
            var entity = new EntityWithExcludedMember { Key = 1, HiddenRelated = related, HiddenRelatedCollection = new[] { related } };
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService));
            Assert.Contains(typeof(QueryRelatedEntity), description.EntityTypes);
            var exception = Assert.ThrowsExactly<InvalidOperationException>(() => Deserialize(entity, queryOperator, expression));
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.UnknownPropertyOrField, memberName, nameof(EntityWithExcludedMember)), exception.Message);
        }

        [TestMethod]
        public void PostProcessor_AllowsVisibleAssociation()
        {
            var entity = new EntityWithExcludedMember { Key = 1, VisibleRelated = new QueryRelatedEntity { Key = 1, Secret = "Whatever" } };
            AssertMatch(entity, "it.VisibleRelated.Secret == \"Whatever\"");
        }

        private static void AssertMatch<T>(T value, string expression)
        {
            Assert.AreEqual(1, Deserialize(value, "where", expression).Cast<T>().Count());
        }

        private sealed class IndexedList : List<string>
        {
            public int IndexerCalls { get; private set; }

            public string this[string key]
            {
                get
                {
                    this.IndexerCalls++;
                    return "Whatever";
                }
            }
        }

        private static void AccessMember(Type entityType, string memberToAccess)
        {
            IQueryable<EntityWithExcludedMember> queryable = Array.Empty<EntityWithExcludedMember>().AsQueryable();

            List<ServiceQueryPart> queryParts = new List<ServiceQueryPart>()
            {
                new ServiceQueryPart("where", String.Format("it.{0} == \"Whatever\"", memberToAccess))
            };

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                QueryDeserializer.Deserialize(DomainServiceDescription.GetDescription(typeof(QueryDeserializerDomainService)), queryable, queryParts);
            }, string.Format(CultureInfo.CurrentCulture, System.Linq.Dynamic.Resource.UnknownPropertyOrField, memberToAccess, entityType.Name));
        }
    }

    public class QueryDeserializerDomainService : DomainService
    {
        public IQueryable<EntityWithExcludedMember> GetEntities()
        {
            return null;
        }

        /// <summary>Exposes the navigation target as an entity type.</summary>
        public IQueryable<QueryRelatedEntity> GetRelatedEntities()
        {
            return null;
        }
    }

    [DataContract]
    [MetadataType(typeof(QueryEntityMetadata))]
    public class EntityWithExcludedMember : QueryEntityBase
    {
        [Key]
        [DataMember]
        public int Key
        {
            get;
            set;
        }

        [DataMember]
        public double Double
        {
            get;
            set;
        }

        [Exclude]
        [DataMember]
        public string ExcludedMember
        {
            get;
            set;
        }

        public string NonDataMember
        {
            get;
            set;
        }

        /// <summary>Gets or sets a serialized complex value.</summary>
        [DataMember]
        public QueryComplexValue Complex { get; set; }

        /// <summary>A field excluded from client metadata.</summary>
        [Exclude]
        [DataMember]
        public string ExcludedField;

        /// <summary>A field without serialization attributes.</summary>
        public string NonDataMemberField;

        /// <summary>A field that is not part of property-based client metadata.</summary>
        [DataMember]
        public string DataMemberField;

        /// <summary>A field ignored by serialization.</summary>
        [IgnoreDataMember]
        public string IgnoredField;

        /// <summary>Gets or sets an excluded reference to an exposed entity type.</summary>
        [Exclude]
        public QueryRelatedEntity HiddenRelated { get; set; }

        /// <summary>Gets or sets an excluded collection of exposed entities.</summary>
        [Exclude]
        public ICollection<QueryRelatedEntity> HiddenRelatedCollection { get; set; }

        /// <summary>Gets or sets a query-visible association.</summary>
        [EntityAssociation("Entity_Related", "Key", "Key")]
        public QueryRelatedEntity VisibleRelated { get; set; }

        /// <summary>Gets the number of custom indexer invocations.</summary>
        [Exclude]
        public int IndexerCalls { get; private set; }

        /// <summary>Gets a server-only value by numeric index.</summary>
        public string this[int index]
        {
            get
            {
                this.IndexerCalls++;
                return "Whatever";
            }
        }

        /// <summary>Gets a server-only value by key.</summary>
        public string this[string key]
        {
            get
            {
                this.IndexerCalls++;
                return "Whatever";
            }
        }
    }

    /// <summary>Supplies an inherited property whose metadata is registered on the entity.</summary>
    [DataContract]
    public class QueryEntityBase
    {
        /// <summary>Gets or sets a value excluded by derived entity metadata.</summary>
        [DataMember]
        public string InheritedExcludedMember { get; set; }
    }

    /// <summary>Defines client visibility for inherited entity properties.</summary>
    public class QueryEntityMetadata
    {
        /// <summary>Gets or sets the excluded inherited member metadata.</summary>
        [Exclude]
        public string InheritedExcludedMember { get; set; }
    }

    /// <summary>An exposed navigation target for query visibility regressions.</summary>
    public class QueryRelatedEntity
    {
        /// <summary>Gets or sets the entity key.</summary>
        [Key]
        public int Key { get; set; }

        /// <summary>Gets or sets a queryable value reachable through a navigation.</summary>
        public string Secret { get; set; }
    }

    /// <summary>A complex value with a server-only field.</summary>
    public class QueryComplexValue
    {
        /// <summary>Gets or sets a client-visible value.</summary>
        public string Visible { get; set; }

        /// <summary>A field outside client metadata.</summary>
        public string HiddenField;
    }
}
