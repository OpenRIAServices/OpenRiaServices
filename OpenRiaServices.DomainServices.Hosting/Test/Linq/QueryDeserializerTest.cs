using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Client.Test;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Hosting;
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
            IQueryable<EntityWithExcludedMember> queryable = new EntityWithExcludedMember[0].AsQueryable();

            List<ServiceQueryPart> queryParts = new List<ServiceQueryPart>()
            {
                new ServiceQueryPart("where", "it.Double.ToString() == String('a', 1000)")
            };

            ExceptionHelper.ExpectException<ParseException>(delegate
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

        private static void AccessMember(Type entityType, string memberToAccess)
        {
            IQueryable<EntityWithExcludedMember> queryable = new EntityWithExcludedMember[0].AsQueryable();

            List<ServiceQueryPart> queryParts = new List<ServiceQueryPart>()
            {
                new ServiceQueryPart("where", String.Format("it.{0} == \"Whatever\"", memberToAccess))
            };

            ExceptionHelper.ExpectInvalidOperationException(delegate
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
    }

    [DataContract]
    public class EntityWithExcludedMember
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
    }
}
