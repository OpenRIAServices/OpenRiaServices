using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Server.Test
{
    [TestClass]
    public class QueryAttributeTests
    {
        #region QueryAttribute ctor and properties
        [TestMethod]
        [Description("Parameterless ctor for QueryAttribute initializes properties")]
        public void QueryAttribute_Default_Ctor()
        {
            QueryAttribute qa = new QueryAttribute();
            Assert.IsFalse(qa.IsDefault, "IsDefault should be false using default ctor");
            Assert.IsFalse(qa.HasSideEffects, "HasSideEffects should be false using default ctor");
            Assert.AreEqual(0, qa.ResultLimit, "Result limit should be zero using default ctor");
            Assert.IsTrue(qa.IsComposable, "IsComposable should be true using default ctor");
        }

        [TestMethod]
        [Description("QueryAttribute properties can be set")]
        public void QueryAttribute_Properties()
        {
            QueryAttribute qa = new QueryAttribute();

            // IsDefault can be set and reset
            qa.IsDefault = true;
            Assert.IsTrue(qa.IsDefault, "IsDefault should have been true after set");

            qa.IsDefault = false;
            Assert.IsFalse(qa.IsDefault, "IsDefault should have been false after reset");

            // IsComposable can be set and reset
            qa.IsComposable = true;
            Assert.IsTrue(qa.IsComposable, "IsComposable should have been true after set");

            qa.IsComposable = false;
            Assert.IsFalse(qa.IsComposable, "IsComposable should have been false after reset");

            // HasSideEffects can be set and reset
            qa.HasSideEffects = true;
            Assert.IsTrue(qa.HasSideEffects, "HasSideEffects should have been true after set");

            qa.HasSideEffects = false;
            Assert.IsFalse(qa.HasSideEffects, "HasSideEffects should have been false after reset");

            qa.ResultLimit = 5000;
            Assert.AreEqual(5000, qa.ResultLimit, "ResultLimit was not settable");

            qa.ResultLimit = 0;
            Assert.AreEqual(0, qa.ResultLimit, "ResultLimit was not resettable");
        }

        [TestMethod]
        [Description("Validate QueryAttribute has no properties unknown to these unit tests")]
        public void QueryAttribute_No_New_Properties()
        {
            PropertyInfo[] properties = typeof(QueryAttribute).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.AreEqual(4, properties.Length, "QueryAttribute has a new property not covered by unit tests.");
        }
        #endregion // QueryAttribute ctor and properties

        #region QueryAttributes in Domain Services

        [TestMethod]
        [Description("DomainService can have multiple query defaults differing by entity type")]
        public void QueryAttribute_DomainService_Valid_Multiple()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(QueryAttribute_Valid_DomainService));
            DomainOperationEntry[] does = dsd.DomainOperationEntries.ToArray();

            // This DSD defines 2 defaults plus 2 non-defaults
            Assert.AreEqual(4, does.Length, "Expected this many valid query methods");

            // validate the Linq query we recommend OData use to extract the default queries
            IEnumerable<DomainOperationEntry> defaultQueries = dsd.DomainOperationEntries
                                                .Where(doe =>
                                                        (doe.Operation == DomainOperation.Query) &&
                                                        (doe.Attributes.OfType<QueryAttribute>().Any(qa => qa.IsDefault)));
            Assert.AreEqual(2, defaultQueries.Count(), "Expected 2 default queries");
            Assert.IsTrue(defaultQueries.Any(doe => doe.Name.Equals("GetEntity1")), "Expected to find GetEntity1 in list of default queries");
            Assert.IsTrue(defaultQueries.Any(doe => doe.Name.Equals("GetEntity2")), "Expected to find GetEntity2 in list of default queries");

        }

        [TestMethod]
        [Description("Default queries cannot take parameters")]
        public void QueryAttribute_DomainService_Illegal_Default_With_Params()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(QueryAttribute_DomainService_Default_Query_Has_Params));
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Params, "GetEntity1"));
        }

        [TestMethod]
        [Description("Default queries cannot return singletons")]
        public void QueryAttribute_DomainService_Illegal_Default_Singleton()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(QueryAttribute_DomainService_Default_Query_Singleton));
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Be_Singleton, "GetEntity1"));
        }

        [TestMethod]
        [Description("Multiple default queries cannot return same entity type")]
        public void QueryAttribute_DomainService_Illegal_Multiple_Defaults_Same_Entity()
        {
            // We cannot predict which query will be accepted first and which will cause the error, so we format
            // both and verify we get at least one of them.
            string err1 = string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Multiple, "GetEntity1", "GetEntity1Again", typeof(QueryEntity1));
            string err2 = string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Multiple, "GetEntity1Again", "GetEntity1", typeof(QueryEntity1));
            string actualErr = null;

            try
            {
                DomainServiceDescription.GetDescription(typeof(QueryAttribute_DomainService_Default_Query_Same_Entity));
            }
            catch (InvalidOperationException ioe)
            {
                actualErr = ioe.Message;
            }

            Assert.IsNotNull(actualErr, "Expected InvalidOperationException for attempt to set IsDefault on multiple queries using same entity type.");

            Assert.IsTrue(actualErr == err1 || actualErr == err2, "Expected error message: " + err1 + Environment.NewLine + "but saw " + actualErr);
        }

        [TestMethod]
        [Description("Multiple default queries cannot return multiple entity types within a single hierarchy")]
        public void QueryAttribute_DomainService_Illegal_Multiple_Defaults_Same_Entity_Hierarchy()
        {
            // We cannot predict which query will be accepted first and which will cause the error, so we format
            // both and verify we get at least one of them.
            string err1 = string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Multiple_Inheritance, "GetEntity1", "GetEntity1Again", typeof(QueryEntity1Derived), typeof(QueryEntity1));
            string err2 = string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DefaultQuery_Cannot_Have_Multiple_Inheritance, "GetEntity1Again", "GetEntity1", typeof(QueryEntity1Derived), typeof(QueryEntity1));
            string actualErr = null;

            try
            {
                DomainServiceDescription.GetDescription(typeof(QueryAttribute_DomainService_Default_Query_Same_Entity_Hierarchy));
            }
            catch (InvalidOperationException ioe)
            {
                actualErr = ioe.Message;
            }

            Assert.IsNotNull(actualErr, "Expected InvalidOperationException for attempt to set IsDefault on multiple queries using same entity type.");

            Assert.IsTrue(actualErr == err1 || actualErr == err2, "Expected error message: " + err1 + Environment.NewLine + "but saw " + actualErr);
        }
        #endregion // QueryAttributes in Domain Services


        #region Domain Services

        [KnownType(typeof(QueryEntity1Derived))]
        public class QueryEntity1
        {
            [Key]
            public string TheKey { get; set; }
        }
        public class QueryEntity2
        {
            [Key]
            public string TheKey { get; set; }
        }
        public class QueryEntity1Derived : QueryEntity1
        {
        }

        /// <summary>
        /// This test domain service demonstrates it is legal to:
        /// <para>Have IsDefault on multiple queries that differ in type</para>
        /// <para>Have IsDefault on multiple queries whose types are derived</para>
        /// </summary>
        [EnableClientAccess]
        public class QueryAttribute_Valid_DomainService : DomainService
        {
            [Query(IsDefault = true)]
            public IQueryable<QueryEntity1> GetEntity1() { return null; }

            [Query(IsDefault = true)]
            public IQueryable<QueryEntity2> GetEntity2() { return null; }

            // Derived type is legal only because it is not marked as IsDefault=true.
            // We include this to ensure its presence does not trigger failure and that
            // we don't see this query in our Linq expression to extract defaults.
            [Query(IsDefault = false)]
            public IQueryable<QueryEntity1Derived> GetEntity1Derived() { return null; }

            // Legal because it is not marked IsDefault=true.
            // Here to validate presence does not generate error in presence of defaults above
            [Query(IsDefault = false, IsComposable=false)]
            public QueryEntity2 GetEntity2Singleton() { return null; }
        }

        /// <summary>
        /// This test domain service demonstrates it is illegal to:
        /// <para>Have IsDefault on a query with parameters</para>
        /// </summary>
        [EnableClientAccess]
        public class QueryAttribute_DomainService_Default_Query_Has_Params : DomainService
        {
            [Query(IsDefault = true)]
            public IQueryable<QueryEntity1> GetEntity1(string param1) { return null; }
        }

        /// <summary>
        /// This test domain service demonstrates it is illegal to:
        /// <para>Have IsDefault on a query that returns a singleton</para>
        /// </summary>
        [EnableClientAccess]
        public class QueryAttribute_DomainService_Default_Query_Singleton : DomainService
        {
            [Query(IsDefault = true, IsComposable = false)]
            public QueryEntity1 GetEntity1() { return null; }
        }

        /// <summary>
        /// This test domain service demonstrates it is illegal to:
        /// <para>Have 2 default queries for the same entity type</para>
        /// </summary>
        [EnableClientAccess]
        public class QueryAttribute_DomainService_Default_Query_Same_Entity : DomainService
        {
            [Query(IsDefault = true)]
            public IQueryable<QueryEntity1> GetEntity1() { return null; }

            [Query(IsDefault = true)]
            public IEnumerable<QueryEntity1> GetEntity1Again() { return null; }
        }

        /// <summary>
        /// This test domain service demonstrates it is illegal to:
        /// <para>Have 2 default queries for different entity types
        /// within a type hierarchy</para>
        /// </summary>
        [EnableClientAccess]
        public class QueryAttribute_DomainService_Default_Query_Same_Entity_Hierarchy : DomainService
        {
            [Query(IsDefault = true)]
            public IQueryable<QueryEntity1> GetEntity1() { return null; }

            [Query(IsDefault = true)]
            public IEnumerable<QueryEntity1Derived> GetEntity1Again() { return null; }
        }
        #endregion // DomainServices
    }
}
