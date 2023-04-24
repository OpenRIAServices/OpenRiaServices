#if NET472
extern alias SystemWebDomainServices;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Linq;
using System.Linq;
using Cities;
using OpenRiaServices.EntityFramework;
using OpenRiaServices.LinqToSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using System.Globalization;

namespace OpenRiaServices.Server.Test
{
   
    [TestClass]
    public class DomainServiceDescriptionLTSTest : UnitTestBase
    {

        /// <summary>
        /// Verify that our DAL providers correctly infer and apply Editable(false)
        /// </summary>
        [TestMethod]
        public void EditableFalse_DALInference()
        {
            // Verify that key members get Editable(false, AllowInitialValue = true)
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Product))["ProductID"];
            EditableAttribute editable = pd.Attributes.OfType<EditableAttribute>().SingleOrDefault();
            Assert.IsNotNull(editable);
            Assert.IsFalse(editable.AllowEdit);
            Assert.IsTrue(editable.AllowInitialValue);

            // Verify that key members that are also FK members do NOT get EditableAttribute
            pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Order_Detail))["OrderID"];
            AssociationAttribute assoc = TypeDescriptor.GetProperties(typeof(NorthwindModel.Order_Detail))["Order"].Attributes.OfType<AssociationAttribute>().Single();
            Assert.IsTrue(assoc.ThisKeyMembers.Contains("OrderID"));
            editable = pd.Attributes.OfType<EditableAttribute>().SingleOrDefault();
            Assert.IsNull(editable);

            // Do the same tests for LTS
            // Verify that key members get Editable(false, AllowInitialValue = true)
            dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Northwind));
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Product))["ProductID"];
            editable = pd.Attributes.OfType<EditableAttribute>().SingleOrDefault();
            Assert.IsNotNull(editable);
            Assert.IsFalse(editable.AllowEdit);
            Assert.IsTrue(editable.AllowInitialValue);

            // Verify that key members that are also FK members do NOT get EditableAttribute
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Order_Detail))["OrderID"];
            assoc = TypeDescriptor.GetProperties(typeof(NorthwindModel.Order_Detail))["Order"].Attributes.OfType<AssociationAttribute>().Single();
            Assert.IsTrue(assoc.ThisKeyMembers.Contains("OrderID"));
            editable = pd.Attributes.OfType<EditableAttribute>().SingleOrDefault();
            Assert.IsNull(editable);

            // if an entity has a Timestamp member, it should be marked Editable(false)
            LinqToSqlTypeDescriptionContext ltsContext = new LinqToSqlTypeDescriptionContext(typeof(DataTests.Scenarios.LTS.Northwind.NorthwindScenarios));
            System.Data.Linq.Mapping.MetaType metaType = ltsContext.MetaModel.GetMetaType(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity));
            TypeDescriptionProvider tdp = TypeDescriptor.GetProvider(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity));
            LinqToSqlTypeDescriptor ltsTypeDescriptor = new LinqToSqlTypeDescriptor(ltsContext, metaType, tdp.GetTypeDescriptor(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity)));
            pd = ltsTypeDescriptor.GetProperties().Cast<PropertyDescriptor>().Single(p => p.Attributes[typeof(RoundtripOriginalAttribute)] != null);
            Assert.AreEqual("Timestamp", pd.Name);
            editable = pd.Attributes.OfType<EditableAttribute>().SingleOrDefault();
            Assert.IsFalse(editable.AllowEdit);
            Assert.IsFalse(editable.AllowInitialValue);

            LinqToEntitiesTypeDescriptionContext efContext = new LinqToEntitiesTypeDescriptionContext(typeof(DataTests.Scenarios.EF.Northwind.NorthwindEntities_Scenarios));
            tdp = TypeDescriptor.GetProvider(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity));
            LinqToEntitiesTypeDescriptor efTypeDescriptor = new LinqToEntitiesTypeDescriptor(efContext, efContext.GetEdmType(typeof(DataTests.Scenarios.EF.Northwind.TimestampEntity)), tdp.GetTypeDescriptor(typeof(DataTests.Scenarios.EF.Northwind.TimestampEntity)));
            pd = efTypeDescriptor.GetProperties().Cast<PropertyDescriptor>().Single(p => p.Attributes[typeof(RoundtripOriginalAttribute)] != null);
            Assert.AreEqual("Timestamp", pd.Name);
            editable = pd.Attributes.OfType<EditableAttribute>().SingleOrDefault();
            Assert.IsFalse(editable.AllowEdit);
            Assert.IsFalse(editable.AllowInitialValue);
        }

        [TestMethod]
        [DeploymentItem(@"DatModels\ScenarioModels\Northwind.map")]
        public void LTS_ExternalMapping()
        {
            // verify that our external mapping file exists
            // Test pass if this is commented out
            //Assert.IsTrue(File.Exists("Northwind.map"));
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(LTSExternalMappingService));

            // verify that our TDP has inferred DAL metadata
            Type entityType = typeof(DataTests.Scenarios.LTS.Northwind_ExternalMapping.Customer);
            Assert.IsTrue(dsd.EntityTypes.Contains(entityType));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(entityType)["Orders"];
            Assert.IsNotNull(pd.Attributes[typeof(AssociationAttribute)]);
        }

        [TestMethod]
        public void RequiredAttribute_DALInference()
        {
            // Verify that members of EF model marked non Nullable have RequiredAttribute
            var dsd = DomainServiceDescription.GetDescription(typeof(DataTests.Scenarios.EF.Northwind.EF_NorthwindScenarios_RequiredAttribute));
            var properties = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.EF.Northwind.RequiredAttributeTestEntity));
            
            var property = properties["RequiredString"];
            Assert.IsNotNull(property);
            var requiredAttribute = (RequiredAttribute) property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, false);

            property = properties["RequiredStringOverride"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, true);

            property = properties["RequiredInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalString"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            // Verify that members of LTS model marked non Nullable have RequiredAttribute
            dsd = DomainServiceDescription.GetDescription(typeof(DataTests.Scenarios.LTS.Northwind.LTS_NorthwindScenarios));
            properties = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.LTS.Northwind.RequiredAttributeTestEntity));
            
            property = properties["RequiredString"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, false);

            property = properties["RequiredStringOverride"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, true);

            property = properties["RequiredInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["RequiredNullableInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNotNull(requiredAttribute);
            Assert.AreEqual<bool>(requiredAttribute.AllowEmptyStrings, false);

            property = properties["OptionalString"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);

            property = properties["OptionalNullableInt32"];
            Assert.IsNotNull(property);
            requiredAttribute = (RequiredAttribute)property.Attributes[typeof(RequiredAttribute)];
            Assert.IsNull(requiredAttribute);
        }

        /// <summary>
        /// Verify that our DAL providers correctly infer and apply RoundtripOriginalAttribute
        /// </summary>
        [TestMethod]
        public void RoundtripOriginalAttribute_DALInference()
        {
            // Verify that members that participate in concurrency checks
            // have the attribute applied, and those that don't do not
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Product))["ProductName"];
            Assert.IsNotNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);
            pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Category))["CategoryName"];
            Assert.IsNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            Assert.IsNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);

            // verify that FK members have the attribute applied
            pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Product))["CategoryID"];
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);

            // For LTS, Verify that all writeable members have RTO applied
            dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Northwind));
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Product))["ProductName"];
            Assert.IsNotNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Category))["CategoryName"];
            Assert.IsNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);

            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Product))["CategoryID"];
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);

            // if an entity has a Timestamp member, no other members should be marked RTO
            LinqToSqlTypeDescriptionContext ltsContext = new LinqToSqlTypeDescriptionContext(typeof(DataTests.Scenarios.LTS.Northwind.NorthwindScenarios));
            System.Data.Linq.Mapping.MetaType metaType = ltsContext.MetaModel.GetMetaType(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity));
            TypeDescriptionProvider tdp = TypeDescriptor.GetProvider(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity));
            LinqToSqlTypeDescriptor ltsTypeDescriptor = new LinqToSqlTypeDescriptor(ltsContext, metaType, tdp.GetTypeDescriptor(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity)));
            pd = ltsTypeDescriptor.GetProperties().Cast<PropertyDescriptor>().Single(p => p.Attributes[typeof(RoundtripOriginalAttribute)] != null);
            Assert.AreEqual("Timestamp", pd.Name);

            LinqToEntitiesTypeDescriptionContext efContext = new LinqToEntitiesTypeDescriptionContext(typeof(DataTests.Scenarios.EF.Northwind.NorthwindEntities_Scenarios));
            tdp = TypeDescriptor.GetProvider(typeof(DataTests.Scenarios.LTS.Northwind.TimestampEntity));
            LinqToEntitiesTypeDescriptor efTypeDescriptor = new LinqToEntitiesTypeDescriptor(efContext, efContext.GetEdmType(typeof(DataTests.Scenarios.EF.Northwind.TimestampEntity)), tdp.GetTypeDescriptor(typeof(DataTests.Scenarios.EF.Northwind.TimestampEntity)));
            pd = efTypeDescriptor.GetProperties().Cast<PropertyDescriptor>().Single(p => p.Attributes[typeof(RoundtripOriginalAttribute)] != null);
            Assert.AreEqual("Timestamp", pd.Name);
        }

        
        /// <summary>
        /// Verify that a stack overflow does not occur for the below service
        /// containing a DAL inheritance model
        /// </summary>
        [TestMethod]
        [WorkItem(843965)]
        public void DomainServiceDescription_DALInheritanceTDPIssue()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(Bug843965_Service));
            Assert.IsNotNull(dsd);
        }

        
        [TestMethod]
        public void TestExplicitDALMetadataProviders()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(LTSRepositoryTest));

            // verify that the correct DAL providers have been registered for the exposed types
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Customer))["CustomerID"];
            Assert.IsNotNull(pd.Attributes[typeof(KeyAttribute)]);
            pd = TypeDescriptor.GetProperties(typeof(AdventureWorksModel.Department))["DepartmentID"];
            Assert.IsNotNull(pd.Attributes[typeof(KeyAttribute)]);

            // verify that we get the expected exception if the implicit provider attributes are
            // used on a non DAL service
            string expectedMsg = string.Format(CultureInfo.CurrentCulture,
                    "'{0}' cannot be applied to DomainService Type '{1}' because '{1}' does not derive from '{2}'.",
                    typeof(LinqToSqlDomainServiceDescriptionProviderAttribute).Name, "City", typeof(LinqToSqlDomainService<>).Name);
            DomainServiceDescriptionProviderAttribute dsdpa = new LinqToSqlDomainServiceDescriptionProviderAttribute();
            OpenRiaServices.Client.Test.ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                dsdpa.CreateProvider(typeof(City), null);
            }, expectedMsg);

            expectedMsg = string.Format(CultureInfo.CurrentCulture,
                    "'{0}' cannot be applied to DomainService Type '{1}' because '{1}' does not derive from '{2}'.",
                    typeof(LinqToEntitiesDomainServiceDescriptionProviderAttribute).Name, "City", typeof(LinqToEntitiesDomainService<>).Name);
            dsdpa = new LinqToEntitiesDomainServiceDescriptionProviderAttribute();
            OpenRiaServices.Client.Test.ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                dsdpa.CreateProvider(typeof(City), null);
            }, expectedMsg);
        }

        
        /// <summary>
        /// Verify that both DAL providers support accessing their respective
        /// contexts in the constructor.
        /// </summary>
        [TestMethod]
        [WorkItem(827125)]
        public void DomainServiceConstructor_ContextAccess()
        {
            LTSService_ConstructorInit lts = new LTSService_ConstructorInit();
            Assert.IsNotNull(lts.DataContext.LoadOptions);

            EFService_ConstructorInit ef = new EFService_ConstructorInit();
        }
    }   
    
        
    public class LTSExternalMappingService : LinqToSqlDomainService<DataTests.Scenarios.LTS.Northwind_ExternalMapping.Northwind>
    {
        public IQueryable<DataTests.Scenarios.LTS.Northwind_ExternalMapping.Customer> GetCustomers()
        {
            return null;
        }
    }
    
    public class Bug843965_Service : LinqToSqlDomainService<DataTests.Scenarios.LTS.Northwind.NorthwindScenarios>
    {
        public IQueryable<DataTests.Scenarios.LTS.Northwind.Bug843965_A> GetBug843965_As()
        {
            return null;
        }
    }

    
    // Test service demonstrating that the DAL providers can be used externally by
    // explicitly specifying a context Type.
    // Here we verify a repository scenario where multiple TDPs are registered for
    // mulitiple DALs
    [LinqToSqlDomainServiceDescriptionProvider(typeof(DataTests.Northwind.LTS.NorthwindDataContext))]
    [LinqToEntitiesDomainServiceDescriptionProvider(typeof(AdventureWorksModel.AdventureWorksEntities))]
    public class LTSRepositoryTest : DomainService
    {
        private DataTests.Northwind.LTS.NorthwindDataContext nw = new DataTests.Northwind.LTS.NorthwindDataContext();
        private AdventureWorksModel.AdventureWorksEntities aw = new AdventureWorksModel.AdventureWorksEntities();

        public System.Collections.Generic.IEnumerable<DataTests.Northwind.LTS.Customer> GetCustomers()
        {
            return nw.Customers;
        }

        public System.Collections.Generic.IEnumerable<AdventureWorksModel.Department> GetDepartments()
        {
            return aw.Departments;
        }
    }

    
    [EnableClientAccess]
    public class LTSService_ConstructorInit : LinqToSqlDomainService<DataTests.Northwind.LTS.NorthwindDataContext>
    {
        public LTSService_ConstructorInit()
        {
            DataLoadOptions loadOpts = new DataLoadOptions();
            loadOpts.LoadWith<DataTests.Northwind.LTS.Order>(p => p.Order_Details);

            this.DataContext.LoadOptions = loadOpts;
        }
    }
}
#endif 
