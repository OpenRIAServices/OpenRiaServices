extern alias SystemWebDomainServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using OpenRiaServices.Client.Test;
using OpenRiaServices.EntityFramework;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System.Threading;
using Cities;
using OpenRiaServices.LinqToSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TheTypeDescriptorExtensions = SystemWebDomainServices::OpenRiaServices.Server.TypeDescriptorExtensions;
using System.Data.Linq;
using OpenRiaServices.Server.Test;

namespace OpenRiaServices.Server.Test
{
    using System.Threading.Tasks;
    using MetaType = SystemWebDomainServices::OpenRiaServices.Server.MetaType;

    public partial class DomainServiceDescriptionTest
    {

        /// <summary>
        /// Verify that the EF metadata provider is registered for mapped CTs, and that attribute are inferred properly
        /// </summary>
        [TestMethod]
        public void ComplexType_EFProviderTest()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(EFComplexTypesService));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.EF.Northwind.EmployeeWithCT))["EmployeeID"];
            Assert.IsNotNull(pd.Attributes[typeof(KeyAttribute)]);

            // the HomePhone member is mapped as non-nullable, with a max length of 24. Verify attributes
            // were inferred
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.EF.Northwind.ContactInfoCT))["HomePhone"];
            Assert.IsNotNull(pd.Attributes[typeof(RequiredAttribute)]);
            StringLengthAttribute sl = (StringLengthAttribute)pd.Attributes[typeof(StringLengthAttribute)];
            Assert.AreEqual(24, sl.MaximumLength);

            Assert.IsNotNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);

            // the AddressLine1 member is mapped as non-nullable, with a max length of 100. Verify attributes
            // were inferred
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.EF.Northwind.AddressCT))["AddressLine"];
            Assert.IsNotNull(pd.Attributes[typeof(RequiredAttribute)]);
            sl = (StringLengthAttribute)pd.Attributes[typeof(StringLengthAttribute)];
            Assert.AreEqual(100, sl.MaximumLength);

            Assert.IsNotNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);
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
        public void EFTypeDescriptor_ExcludedEntityMembers()
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(EFPocoEntity_IEntityChangeTracker))["EntityState"];
            Assert.IsTrue(LinqToEntitiesTypeDescriptor.ShouldExcludeEntityMember(pd));

            pd = TypeDescriptor.GetProperties(typeof(AdventureWorksModel.Customer))["EntityState"];
            Assert.IsTrue(LinqToEntitiesTypeDescriptor.ShouldExcludeEntityMember(pd));

            pd = TypeDescriptor.GetProperties(typeof(AdventureWorksModel.Customer))["SalesTerritoryReference"];
            Assert.IsTrue(LinqToEntitiesTypeDescriptor.ShouldExcludeEntityMember(pd));
        }

        [TestMethod]
        [WorkItem(858226)]
        public void EFInvokeOperationConvention()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(Bug858226_Service));
            DomainOperationEntry doe = dsd.DomainOperationEntries.Single(p => p.Name == "RegisterUser");
            Assert.AreEqual(DomainOperation.Invoke, doe.Operation);
        }

        /// <summary>
        /// Verify that after deriving from an EF type TypeDescriptor continues to work as expected.
        /// </summary>
        [TestMethod]
        [WorkItem(846250)]
        public void DomainServiceDescription_DeriveFromEFEntityType()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(Bug846250_Service));
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(SpecialOrder));
            Assert.IsNotNull(properties["SpecialData"], "Missing property");

            PropertyDescriptor orderDetailsProperty = properties["Order_Details"];
            Assert.IsNotNull(orderDetailsProperty, "Missing inherited property");
            Assert.IsNotNull(orderDetailsProperty.Attributes[typeof(IncludeAttribute)], "Missing [Include] on inherited property");
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
        /// <summary>
        /// Verify that our DAL providers correctly infer and apply Editable(false)
        /// </summary>
        [TestMethod]
        public void EditableFalse_DALInference_Lts()
        {

            DomainServiceDescription dsd;
            PropertyDescriptor pd;
            EditableAttribute editable;
            AssociationAttribute assoc;

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

        /// <summary>
        /// Verify that our DAL providers correctly infer and apply RoundtripOriginalAttribute
        /// </summary>
        [TestMethod]
        public void RoundtripOriginalAttribute_DALInference_Lts()
        {
            // For LTS, Verify that all writeable members have RTO applied
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Northwind));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Product))["ProductName"];
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


        [TestMethod]
        [Description("Verify that our DAL providers correctly infer and apply DatabaseGeneratedAttribute")]
        public void DatabaseGeneratedAttributeAttribute_DALInference_LTSIdentity()
        {
            // Verify that members that are db generated have the attribute applied, and those that don't do not
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Northwind));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Product))["ProductID"];
            Attribute dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNotNull(dbAttr);
            Assert.IsTrue(CompareDatabaseGeneratedOption(dbAttr, "Identity"));
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Product))["ProductName"];
            dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNull(dbAttr);

            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Category))["CategoryID"];
            dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNotNull(dbAttr);
            Assert.IsTrue(CompareDatabaseGeneratedOption(dbAttr, "Identity"));
            pd = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Category))["CategoryName"];
            dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNull(dbAttr);
        }

        [TestMethod]
        [Description("Verify that our DAL providers correctly infer and apply DatabaseGeneratedAttribute")]
        public void DatabaseGeneratedAttributeAttribute_DALInference_LTSComputed()
        {
            // Verify that members that are db generated have the attribute applied, and those that don't do not
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Catalog));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(DataTests.AdventureWorks.LTS.PurchaseOrderDetail))["LineTotal"];
            Attribute dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNotNull(dbAttr);
            Assert.IsTrue(CompareDatabaseGeneratedOption(dbAttr, "Computed"));

            dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Catalog));
            pd = TypeDescriptor.GetProperties(typeof(AdventureWorksModel.PurchaseOrderDetail))["LineTotal"];
            dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNotNull(dbAttr);
            Assert.IsTrue(CompareDatabaseGeneratedOption(dbAttr, "Computed"));
        }

        [TestMethod]
        [Description("Ensure that our TDPs cache properties")]
        public void DomainService_TdpCachesProperties()
        {
            // First make sure the TDPs are registered for all the entities.
            DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));

            Type categoryType = typeof(NorthwindModel.Category);

            PropertyDescriptorCollection p1 = TypeDescriptor.GetProperties(categoryType);
            PropertyDescriptorCollection p2 = TypeDescriptor.GetProperties(categoryType);

            Assert.AreSame(p1, p2, "TDP didn't cache the properties.");

            // First make sure the TDPs are registered for all the entities.
            DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Northwind));

            categoryType = typeof(DataTests.Northwind.LTS.Category);

            p1 = TypeDescriptor.GetProperties(categoryType);
            p2 = TypeDescriptor.GetProperties(categoryType);

            Assert.AreSame(p1, p2, "TDP didn't cache the properties.");
        }

        /// <summary>
        /// Verify that DSDPs are chained from those applied to the most derived DomainService
        /// base class, up the hierarchy to base. This means that the innermost parent is the
        /// DSDP applied to the most derived class (TDPs chain calling their parents first).
        /// </summary>
        [TestMethod]
        public void DomainDescriptionProvider_ChainOrdering()
        {
            DomainServiceDescriptionProvider dsdp = DomainServiceDescription.CreateDescriptionProvider(typeof(DSDTestServiceB));
            Assert.AreEqual(typeof(DescriptionProviderA), dsdp.GetType());
            Assert.AreEqual(typeof(DescriptionProviderB), dsdp.ParentProvider.GetType());
            Assert.AreEqual(typeof(ReflectionDomainServiceDescriptionProvider), dsdp.ParentProvider.ParentProvider.GetType());

            dsdp = DomainServiceDescription.CreateDescriptionProvider(typeof(TestDomainServices.LTS.Northwind));
            Assert.AreEqual(typeof(LinqToSqlDomainServiceDescriptionProvider), dsdp.GetType());
            Assert.AreEqual(typeof(ReflectionDomainServiceDescriptionProvider), dsdp.ParentProvider.GetType());
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
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                dsdpa.CreateProvider(typeof(City), null);
            }, expectedMsg);

            expectedMsg = string.Format(CultureInfo.CurrentCulture,
                    "'{0}' cannot be applied to DomainService Type '{1}' because '{1}' does not derive from '{2}'.",
                    typeof(LinqToEntitiesDomainServiceDescriptionProviderAttribute).Name, "City", typeof(LinqToEntitiesDomainService<>).Name);
            dsdpa = new LinqToEntitiesDomainServiceDescriptionProviderAttribute();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                dsdpa.CreateProvider(typeof(City), null);
            }, expectedMsg);
        }

        [TestMethod]
        public void TestNonFKEntityFrameworkModels()
        {
            // first try a model where FKs are part of CSDL
            Type contextType = typeof(NorthwindModel.NorthwindEntities);
            LinqToEntitiesTypeDescriptionContext ctxt = new LinqToEntitiesTypeDescriptionContext(contextType);
            EntityType edmType = (EntityType)ctxt.GetEdmType(typeof(NorthwindModel.Product));
            NavigationProperty navProp = edmType.NavigationProperties.Single(p => p.Name == "Category");
            OpenRiaServices.EntityFramework.AssociationInfo assocInfo = ctxt.GetAssociationInfo(navProp);
            Assert.IsNotNull(assocInfo);

            contextType = typeof(NorthwindNoFks.Northwind_NoFks_Entities);
            ctxt = new LinqToEntitiesTypeDescriptionContext(contextType);
            edmType = (EntityType)ctxt.GetEdmType(typeof(NorthwindNoFks.Product));
            navProp = edmType.NavigationProperties.Single(p => p.Name == "Category");
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                assocInfo = ctxt.GetAssociationInfo(navProp);
            }, string.Format("Unable to retrieve association information for association '{0}'. Only models that include foreign key information are supported. See Entity Framework documentation for details on creating models that include foreign key information.", navProp.RelationshipType.FullName));
        }

        [TestMethod]
        public void DomainServiceDescription_ConcurrencyCheckAttribute()
        {
            // verify that the LTS provider flows OCC metadata
            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Northwind));
            PropertyDescriptor prop = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Product))["ProductName"];
            Assert.IsNotNull(prop.Attributes[typeof(ConcurrencyCheckAttribute)]);
            prop = TypeDescriptor.GetProperties(typeof(DataTests.Northwind.LTS.Category))["CategoryName"];
            Assert.IsNull(prop.Attributes[typeof(ConcurrencyCheckAttribute)]);

            // verify that the EF provider flows OCC metadata
            desc = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));
            prop = TypeDescriptor.GetProperties(typeof(NorthwindModel.Product))["ProductName"];
            Assert.IsNotNull(prop.Attributes[typeof(ConcurrencyCheckAttribute)]);
            prop = TypeDescriptor.GetProperties(typeof(NorthwindModel.Category))["CategoryName"];
            Assert.IsNull(prop.Attributes[typeof(ConcurrencyCheckAttribute)]);
        }

        /// <summary>
        /// Direct tests against our custom TD for member projections
        /// </summary>
        [TestMethod]
        public void ProjectionInclude_TestPropertyDescriptor()
        {
            DataTests.Northwind.LTS.Product product = new DataTests.Northwind.LTS.Product
            {
                ProductID = 1
            };
            DataTests.Northwind.LTS.Category category = new DataTests.Northwind.LTS.Category
            {
                CategoryID = 1,
                CategoryName = "Frozen Treats"
            };
            DataTests.Northwind.LTS.Supplier supplier = new DataTests.Northwind.LTS.Supplier
            {
                SupplierID = 1,
                CompanyName = "The Treat Factory"
            };
            product.Category = category;
            product.Supplier = supplier;

            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Northwind));
            desc.Initialize();

            PropertyDescriptor supplierName = TypeDescriptor.GetProperties(product)["SupplierName"];
            Assert.IsNotNull(supplierName);
            Assert.AreEqual(supplier.CompanyName, supplierName.GetValue(product));
            Assert.AreEqual(typeof(string), supplierName.PropertyType);
            Assert.AreEqual(typeof(DataTests.Northwind.LTS.Product), supplierName.ComponentType);
            Assert.AreEqual(false, supplierName.CanResetValue(product));
            Assert.IsTrue(supplierName.IsReadOnly);
            Assert.IsTrue(supplierName.ShouldSerializeValue(product));

            PropertyDescriptor categoryName = TypeDescriptor.GetProperties(product)["CategoryName"];
            Assert.IsNotNull(categoryName);
            Assert.AreEqual(category.CategoryName, categoryName.GetValue(product));
        }

        [TestMethod]
        public void TestLinqToSqlDomainServiceDescription()
        {
            ValidateMetadata(typeof(TestDomainServices.LTS.Catalog), CatalogEntities);
        }

        [TestMethod]
        [Description("This test makes sure we can obtain an error message for invalid LinqToSqlDomainServiceDescriptionProviderAttribute")]
        public void TestLinqToSqlInvalidDescriptionProvider()
        {
            ExceptionHelper.ExpectInvalidOperationException(() =>
            {
                ValidateMetadata(typeof(InvalidLinqToSqlDomainServiceDescriptionProviderDS), CatalogEntities);
            },
                string.Format(OpenRiaServices.LinqToSql.Resource.InvalidLinqToSqlDomainServiceDescriptionProviderSpecification,
                    typeof(AdventureWorksModel.AdventureWorksEntities))
                );

            ExceptionHelper.ExpectInvalidOperationException(() =>
            {
                ValidateMetadata(typeof(LinqToSqlThrowingDataContextDS), CatalogEntities);
            }, "error");
        }

        [TestMethod]
        [Description("This test makes sure we can obtain an error message for invalid LinqToEntitiesDomainServiceDescriptionProviderAttribute")]
        public void TestLinqToEntitiesInvalidDescriptionProvider()
        {
            ExceptionHelper.ExpectInvalidOperationException(() =>
            {
                ValidateMetadata(typeof(InvalidLinqToEntitiesDomainServiceDescriptionProviderDS), CatalogEntities);
            },
                                                           string.Format(OpenRiaServices.EntityFramework.Resource.InvalidLinqToEntitiesDomainServiceDescriptionProviderSpecification,
                                                               typeof(DataTests.AdventureWorks.LTS.AdventureWorks))
                );
        }

        [TestMethod]
        [Description("This test makes sure we can obtain a description without instantiating L2S DomainService")]
        public void TestLinqToSqlDomainServiceNotInstantiated()
        {
            ValidateMetadata(typeof(ThrowingDomainServiceL2S), CatalogEntities);
        }

        [TestMethod]
        public void TestAssociationExtensionAttributes_LTS()
        {
            VerifyAdventureWorksAssociations(typeof(TestDomainServices.LTS.Catalog));
        }

        [EnableClientAccess]
        public class TestAWDomainService : LinqToEntitiesDomainService<AdventureWorksModel.AdventureWorksEntities>
        {
            // Expose an entity with a multipart key reference
            public IQueryable<AdventureWorksModel.SalesOrderDetail> GetSalesOrderDetails()
            {
                return null;
            }
        }
    }

    public class LTSExternalMappingService : LinqToSqlDomainService<DataTests.Scenarios.LTS.Northwind_ExternalMapping.Northwind>
    {
        public IQueryable<DataTests.Scenarios.LTS.Northwind_ExternalMapping.Customer> GetCustomers()
        {
            return null;
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

    [EnableClientAccess]
    public class EFComplexTypesService : LinqToEntitiesDomainService<DataTests.Scenarios.EF.Northwind.NorthwindEntities_Scenarios>
    {
        public IQueryable<DataTests.Scenarios.EF.Northwind.EmployeeWithCT> GetCustomers()
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

        public IEnumerable<DataTests.Northwind.LTS.Customer> GetCustomers()
        {
            return nw.Customers;
        }

        public IEnumerable<AdventureWorksModel.Department> GetDepartments()
        {
            return aw.Departments;
        }
    }

    public class Bug858226_Service : LinqToEntitiesDomainService<NorthwindModel.NorthwindEntities>
    {
        public IQueryable<NorthwindModel.Customer> GetCustomers()
        {
            return null;
        }

        // this should match the service operation convention
        public void RegisterUser(int id) { }
    }

    // Needs to derive from an EF entity type that has [Include] properties.
    [DataContract]
    public class SpecialOrder : NorthwindModel.Order
    {
        [DataMember]
        public string SpecialData
        {
            get;
            set;
        }
    }

    public class Bug846250_Service : LinqToEntitiesDomainService<NorthwindModel.NorthwindEntities>
    {
        public IQueryable<SpecialOrder> GetSpecialOrders()
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class EFService_ConstructorInit : LinqToEntitiesDomainService<NorthwindModel.NorthwindEntities>
    {
        public EFService_ConstructorInit()
        {
            string conn = this.ObjectContext.Connection.ConnectionString;
        }
    }

}
