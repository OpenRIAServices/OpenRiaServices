using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRiaServices.DomainServices.Client.Test;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Server.Test
{
    /// <summary>
    /// Summary description for DomainMethodServerTest
    /// </summary>
    [TestClass]
    public class DomainMethodServerTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        [Description("Verify getting description for domain service with valid domain method signatures does not throw")]
        public void DomainServiceWithMultipleDomainMethods()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_MultipleMethods));
            Assert.IsNotNull(description);

            // verify GetDomainMethod returns the correct DomainOperationEntry
            DomainOperationEntry processCityMethod = description.GetCustomMethod(typeof(City), "ProcessCity");
            Assert.IsNotNull(processCityMethod);
            Assert.AreEqual(DomainOperation.Custom, processCityMethod.Operation);
            Assert.AreEqual<string>("ProcessCity", processCityMethod.Name);

            DomainOperationEntry processCountyMethod = description.GetCustomMethod(typeof(County), "ProcessCounty");
            Assert.IsNotNull(processCountyMethod);
            Assert.AreEqual(DomainOperation.Custom, processCityMethod.Operation);
            Assert.AreEqual<string>("ProcessCounty", processCountyMethod.Name);

            // retry with different casing. Verify null is returned since exact matching is needed.
            processCountyMethod = description.GetCustomMethod(typeof(County), "processcounty");
            Assert.IsNull(processCountyMethod);
        }

        [TestMethod]
        [Description("Verify that calling submit with a domain method invocation in the changeset will invoke the method methods accordingly")]
        public void DomainService_CallSubmitDirectly()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_MultipleMethods));
            List<ChangeSetEntry> changeSetEntries = new List<ChangeSetEntry>();

            ChangeSetEntry processCityOperation = new ChangeSetEntry();
            processCityOperation.Entity = new City { Name = "Redmond", CountyName = "King", StateName = "WA" };
            processCityOperation.DomainOperationEntry = description.GetCustomMethod(typeof(City), "ProcessCity");
            processCityOperation.Operation = DomainOperation.Update;
            processCityOperation.EntityActions = new EntityActionCollection { { "ProcessCity", new object[] { new byte[] { byte.MaxValue, byte.MinValue, 123 } } } };
            changeSetEntries.Add(processCityOperation);

            ChangeSet changeset = new ChangeSet(changeSetEntries);
            DomainMethod_ValidProvider_MultipleMethods myTestProvider = ServerTestHelper.CreateInitializedDomainService<DomainMethod_ValidProvider_MultipleMethods>(DomainOperationType.Submit);
            myTestProvider.Submit(changeset);

            // check that the domain services have invoked the domain method correctly by checking the internal variables set
            Assert.AreEqual<string>("ProcessCity_", myTestProvider.Invoked);
            Assert.AreEqual<int>(3, myTestProvider.InputData.Length);
            Assert.AreEqual<byte>(123, myTestProvider.InputData[2]);
        }

        [TestMethod]
        [Description("Verify that calling submit with 2 domain method invocations in the changeset will invoke the method methods accordingly")]
        public void DomainService_CallSubmitWithMultipleInvocations()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_MultipleMethods));
            List<ChangeSetEntry> changeSetEntries = new List<ChangeSetEntry>();

            ChangeSetEntry processCountyOperation = new ChangeSetEntry();
            processCountyOperation.Id = 1;
            processCountyOperation.Entity = new County { Name = "King", StateName="WA" };
            processCountyOperation.DomainOperationEntry = description.GetCustomMethod(typeof(County), "ProcessCounty");
            processCountyOperation.Operation = DomainOperation.Update;
            processCountyOperation.EntityActions = new EntityActionCollection { { "ProcessCounty", null } };
            changeSetEntries.Add(processCountyOperation);

            ChangeSetEntry processCityOperation = new ChangeSetEntry();
            processCityOperation.Id = 2;
            processCityOperation.Entity = new City { Name = "Redmond", CountyName = "King", StateName = "WA" };
            processCityOperation.DomainOperationEntry = description.GetCustomMethod(typeof(City), "ProcessCity");
            processCityOperation.Operation = DomainOperation.Update;
            processCityOperation.EntityActions = new EntityActionCollection { { "ProcessCity", new object[] { new byte[] { 123, 1 } } } };
            changeSetEntries.Add(processCityOperation);

            ChangeSet changeset = new ChangeSet(changeSetEntries);
            DomainMethod_ValidProvider_MultipleMethods myTestProvider = ServerTestHelper.CreateInitializedDomainService<DomainMethod_ValidProvider_MultipleMethods>(DomainOperationType.Submit);
            myTestProvider.Submit(changeset);

            // check that the domain services have invoked the domain method correctly by checking the internal variables set
            Assert.AreEqual<string>("ProcessCounty_ProcessCity_", myTestProvider.Invoked);
            Assert.AreEqual<int>(2, myTestProvider.InputData.Length);
        }

        [TestMethod]
        [Description("Verify GetDomainMethods return all domain methods specified on a domain service")]
        public void GetDomainMethods_Sanity()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_MultipleMethods));

            // verify that GetDomainMethods with City type only returns methods that are associated with City
            IEnumerable<DomainOperationEntry> domainMethods = description.GetCustomMethods(typeof(City));
            Assert.IsNotNull(domainMethods);
            Assert.AreEqual(4, domainMethods.Count());
            Assert.IsNotNull(domainMethods.Single(m => m.Name == "ProcessCity"));
            Assert.IsNotNull(domainMethods.Single(m => m.Name == "AssignCityZone"));
            Assert.IsNotNull(domainMethods.Single(m => m.Name == "AssignCityZoneIfAuthorized"));
            Assert.IsNotNull(domainMethods.Single(m => m.Name == "AutoAssignCityZone"));
            Assert.IsNull(domainMethods.FirstOrDefault(m => m.Name == "ProcessCounty"));

            // verify that GetDomainMethods with Zip type returns one method
            domainMethods = description.GetCustomMethods(typeof(Zip));
            Assert.AreEqual(2, domainMethods.Count());
            Assert.IsNotNull(domainMethods.Single(m => m.Name == "ReassignZipCode"));

            // verify that GetDomainMethods with County type returns one method
            domainMethods = description.GetCustomMethods(typeof(County));
            Assert.AreEqual(1, domainMethods.Count());
            Assert.IsNotNull(domainMethods.Single(m => m.Name == "ProcessCounty"));

            // verify that GetDomainMethods return empty collection when passing in type that is not associated with any methods on the provider
            domainMethods = description.GetCustomMethods(typeof(State));
            Assert.IsNotNull(domainMethods);
            Assert.AreEqual(0, domainMethods.Count());
        }

        [TestMethod]
        [Description("Verify GetDomainMethods return empty collection")]
        public void GetDomainMethods_NoDomainMethods()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_NoDomainMethods));
            IEnumerable<DomainOperationEntry> domainMethods = description.GetCustomMethods(typeof(City));
            Assert.IsNotNull(domainMethods);
            Assert.AreEqual(0, domainMethods.Count());
        }

        #region Negative domain method signature tests
        [TestMethod]
        [Description("Verify GetDomainMethods with null entityType")]
        public void GetDomainmethods_NullEntityType()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_NoDomainMethods));
            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                description.GetCustomMethods(null);
            }, "entityType");
        }

        [TestMethod]
        [Description("Verify GetDomainMethod with negative inputs")]
        public void GetDomainMethod_ExceptionCases()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_MultipleMethods));
            // call with null entityType, verify ArgumentNullEx
            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                description.GetCustomMethod(null, "ProcessCity");
            }, "entityType");

            // call with empty domain method name. Verify ArgumentEx.
            ExceptionHelper.ExpectArgumentException(delegate
            {
                description.GetCustomMethod(typeof(City), "");
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntry_ArgumentCannotBeNullOrEmpty, "methodName"));

            // call with null domain method name. Verify ArgumentEx.
            ExceptionHelper.ExpectArgumentException(delegate
            {
                description.GetCustomMethod(typeof(City), null);
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntry_ArgumentCannotBeNullOrEmpty, "methodName"));
        }

        [TestMethod]
        [Description("Verify null is returned when GetDomainMethod is called with method that does not exist on domain service")]
        public void GetDomainMethod_NotFound()
        {
            DomainOperationEntry returned;
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainMethod_ValidProvider_MultipleMethods));

            // call with a domain method name that is not associated with the specified entityType. Verify null is returned
            returned = description.GetCustomMethod(typeof(City), "ProcessCounty");
            Assert.IsNull(returned);

            // call with non-entity type as entityType argument. Verify null is returned.
            returned = description.GetCustomMethod(typeof(string), "ProcessCity");
            Assert.IsNull(returned);
        }

        [TestMethod]
        [Description("Verify overloads of domain method name is not supported")]
        public void OverloadsNotSupported()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_MethodOverloads));
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntryOverload_NotSupported, "ProcessCity"));
        }

        [TestMethod]
        [Description("Verify domain method signature must be of void return type")]
        public void InvalidReturnType()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_InvalidReturnType));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_NonQueryMustReturnVoid, "ProcessCity"));
        }

        [TestMethod]
        [Description("Verify first argument of a domain method signature must be an entity type")]
        public void FirstArgMustBeEntity()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_FirstArgNonEntity));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainMethod_ParamMustBeEntity, "name", "ProcessCity"));
        }

        [TestMethod]
        [Description("Verify that non-first arguments of a domain method signature must be of one of the predefined types")]
        public void OtherArgsMustBeOfPredefinedTypes()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_MultipleEntities));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "ProcessCity", "county"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_ArgOfTypeObject));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "ProcessCity", "objArg"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_ArgOfTypeIntPtr));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "ProcessCity", "intPtrArg"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_ArgOfTypeUIntPtr));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "ProcessCity", "uintPtrArg"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_ArgOfComplexTypeIEnumerable));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "ProcessCity", "ienumerableArg"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_ArgOfComplexTypeList));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "ProcessCity", "listArg"));

        }

        [TestMethod]
        [Description("Verify that parameterless domain method signature is not supported")]
        public void ParameterlessDomainMethodNotSupported()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_Parameterless));
            }, Resource.InvalidCustomMethod_MethodCannotBeParameterless);
        }
        #endregion
    }
}
