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
using OpenRiaServices.Common.Test;
using OpenRiaServices.DomainServices.Client.Test;
using OpenRiaServices.DomainServices.EntityFramework;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.Threading;
using Cities;
using OpenRiaServices.DomainServices.LinqToSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TheTypeDescriptorExtensions = SystemWebDomainServices::OpenRiaServices.DomainServices.Server.TypeDescriptorExtensions;

namespace OpenRiaServices.DomainServices.Server.Test
{
    using System.Threading.Tasks;
    using MetaType = SystemWebDomainServices::OpenRiaServices.DomainServices.Server.MetaType;


    /// <summary>
    /// DomainServiceDescription tests
    /// </summary>
    [TestClass]
    public class DomainServiceDescriptionTest : UnitTestBase
    {
        [TestMethod]
        [WorkItem(193755)]
        public void DomainServiceDescription_MultipleVersionMembers()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate {
                DomainServiceDescription.GetDescription(typeof(DP_Entity_MultipleVersionMembers_DomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_MultipleVersionMembers, typeof(DP_Entity_MultipleVersionMembers)));
        }

        [TestMethod]
        [WorkItem(192108)]
        public void Validation_NonNullableProjectionIncludeMembers()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(Projections_ValueType_DomainService));

            Projections_ValueType p = new Projections_ValueType { ID = 1, P1 = "Mathew" };
            ValidationContext vc = ValidationUtilities.CreateValidationContext(p, null);
            
            List<ValidationResult> results = new List<ValidationResult>();
            ValidationUtilities.TryValidateObject(p, vc, results);
            Assert.AreEqual(0, results.Count);

            // The underlying cause was our projection member descriptor was returning null for
            // non-nullable value types.
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(Projections_ValueType))["ProductID"];
            object value = pd.GetValue(p);
            Assert.IsNotNull(value);
            Assert.AreEqual(typeof(int), value.GetType());
        }

        /// <summary>
        /// The issue with this bug was that DataMember attributes weren't being honored in
        /// surrogate generation when applied via a buddy class. This caused the surrogate to
        /// be out of sync with the generated client code causing data loss.
        /// </summary>
        [TestMethod]
        [WorkItem(190430)]
        public void SurrogateGeneration_BuddyClassDataMemberAttribute()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestProvider_Scenarios_CodeGen));

            HashSet<Type> knownEntityTypes = new HashSet<Type>() { typeof(TestEntity_DataMemberBuddy) };
            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, typeof(TestEntity_DataMemberBuddy));

            PropertyInfo pi = surrogateType.GetProperty("Prop1");
            DataMemberAttribute actualDma = pi.GetCustomAttributes(typeof(DataMemberAttribute), false).OfType<DataMemberAttribute>().SingleOrDefault();
            DataMemberAttribute expectedDma = TypeDescriptor.GetProperties(typeof(TestEntity_DataMemberBuddy))["Prop1"].Attributes.OfType<DataMemberAttribute>().SingleOrDefault();
            Assert.IsNotNull(actualDma);
            Assert.AreEqual(expectedDma.Name, actualDma.Name);
            Assert.AreEqual(expectedDma.IsRequired, actualDma.IsRequired);
        }

        /// <summary>
        /// Verify that a service can contain only CTs (no entities)
        /// </summary>
        [TestMethod]
        public void ComplexType_Description_InvokeOperationsOnly()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexTypes_InvokeOperationsOnly));
            Assert.AreEqual(0, dsd.EntityTypes.Count());
            Assert.AreEqual(3, dsd.ComplexTypes.Count());
        }

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
        public void ComplexType_MetaTypeTests()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexTypes_TestService));

            // verify that RequiresValidation is recursive through CTs
            MetaType mt = MetaType.GetMetaType(typeof(ComplexType_NoValidation));
            Assert.IsTrue(mt.RequiresValidation);
            Assert.IsTrue(MetaType.GetMetaType(typeof(ContactInfo)).RequiresValidation);
            Assert.IsTrue(MetaType.GetMetaType(typeof(Address)).RequiresValidation);
            Assert.IsTrue(MetaType.GetMetaType(typeof(Phone)).RequiresValidation);

            // add positive/negative CT identification tests
        }

        /// <summary>
        /// Verify all complex type validations
        /// </summary>
        [TestMethod]
        public void ComplexType_Validation()
        {
            // association from entity to complex types
            {
                DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexType_InvalidEntity_ComplexProperty));
                dsd.Initialize();
                Assert.AreEqual(0, dsd.ComplexTypes.Count(), "entity associates to complex type, dsd should not recognize this complex type");
            }

            // entity inheritance from complex type
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexType_InvalidEntityInheritance));
                dsd.Initialize();
            }, string.Format(Resource.InvalidComplexType_EntityInheritance, "TestDomainServices.ComplexType_InvalidEntityInheritance", "TestDomainServices.ComplexType_Invalid_EntityInheritance_FromComplex", "TestDomainServices.ComplexType_Invalid_Parent"));

            // complex type inheritance
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexType_InvalidInheritance));
                dsd.Initialize();
            }, string.Format(Resource.InvalidComplexType_Inheritance, "TestDomainServices.ComplexType_InvalidInheritance", "TestDomainServices.ComplexType_Valid_Child", "TestDomainServices.ComplexType_Invalid_Parent"));

            // known types
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.ValidateComplexType(typeof(ComplexType_KnownType_Parent));
            }, string.Format(Resource.InvalidComplexType_KnownTypes, "ComplexType_KnownType_Parent"));

            // association
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.ValidateComplexType(typeof(ComplexType_Invalid_AssociationMember));
            }, string.Format(Resource.InvalidComplexType_PropertyAttribute, "ContactInfo", "ComplexType_Invalid_AssociationMember", Resource.InvalidComplexType_AssociationMember));

            // composition
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.ValidateComplexType(typeof(ComplexType_Invalid_CompositionMember));
            }, string.Format(Resource.InvalidComplexType_PropertyAttribute, "Children", "ComplexType_Invalid_CompositionMember", Resource.InvalidComplexType_CompositionMember));

            // include
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.ValidateComplexType(typeof(ComplexType_Invalid_IncludeMember));
            }, string.Format(Resource.InvalidComplexType_PropertyAttribute, "Child", "ComplexType_Invalid_IncludeMember", Resource.InvalidComplexType_IncludeMember));

            // lists are allowed
            DomainServiceDescription.ValidateComplexType(typeof(ComplexType_Valid_SimpleList));
            DomainServiceDescription.ValidateComplexType(typeof(ComplexType_Valid_ComplexList));

        }

        /// <summary>
        /// Verify that we register buddy and domain metadata providers for CTs
        /// </summary>
        [TestMethod]
        public void ComplexTypes_MetadataProvider()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexTypes_TestService));

            // verify the attribute doesn't exist on the actual property
            Assert.IsTrue(typeof(Phone).GetProperty("AreaCode").GetCustomAttributes(typeof(StringLengthAttribute), false).Length == 0);

            // verify via property descriptor
            StringLengthAttribute sla = (StringLengthAttribute)TypeDescriptor.GetProperties(typeof(Phone))["AreaCode"].Attributes[typeof(StringLengthAttribute)];
            Assert.AreEqual(3, sla.MaximumLength);
        }

        [TestMethod]
        public void ComplexTypes_ServiceDescription_Nested_ComplexTypes()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexTypes_TestService));
            Assert.AreEqual(1, dsd.EntityTypes.Count());
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(ComplexType_Parent)));
            Assert.AreEqual(3, dsd.ComplexTypes.Count());
            Assert.IsTrue(dsd.ComplexTypes.Contains(typeof(ContactInfo)));
            Assert.IsTrue(dsd.ComplexTypes.Contains(typeof(Address)));
            Assert.IsTrue(dsd.ComplexTypes.Contains(typeof(Phone)));
        }

        [TestMethod]
        public void ComplexTypes_ServiceDescription_RecursiveComplexType()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexTypes_TestService_Scenarios));
            Assert.AreEqual(2, dsd.EntityTypes.Count());
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(ComplexType_Scenarios_Parent)));
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(NorthwindModel.Employee)));
            Assert.AreEqual(1, dsd.ComplexTypes.Count());
            Assert.IsTrue(dsd.ComplexTypes.Contains(typeof(ComplexType_Recursive)));
        }

        [TestMethod]
        public void ComplexTypes_ServiceDescription_ComplexMethodSignatures()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ComplexTypes_TestService_ComplexMethodSignatures));
            Assert.AreEqual(1, dsd.EntityTypes.Count());
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(ComplexType_Parent)));
            Assert.AreEqual(3, dsd.ComplexTypes.Count());
            Assert.IsTrue(dsd.ComplexTypes.Contains(typeof(ContactInfo)));
            Assert.IsTrue(dsd.ComplexTypes.Contains(typeof(Address)));
            Assert.IsTrue(dsd.ComplexTypes.Contains(typeof(Phone)));

            int numOps = 7;
            Assert.AreEqual(numOps, dsd.DomainOperationEntries.Count(), string.Format("DSD should retrieve {0} operations from ComplexTypes_TestService_ComplexMethodSignatures.", numOps));
            DomainOperationEntry doe = null;

            doe = dsd.DomainOperationEntries.Single(p => p.Name == "CustomUpdateHomeAddress");
            Assert.AreEqual(DomainOperation.Custom, doe.Operation);

            doe = dsd.DomainOperationEntries.Single(p => p.Name == "UpdateHomeAddress");
            Assert.AreEqual(DomainOperation.Custom, doe.Operation);

            doe = dsd.DomainOperationEntries.Single(p => p.Name == "Foo");
            Assert.AreEqual(DomainOperation.Invoke, doe.Operation);

            doe = dsd.DomainOperationEntries.Single(p => p.Name == "GetAllPhoneNumbers");
            Assert.AreEqual(DomainOperation.Invoke, doe.Operation);

            doe = dsd.DomainOperationEntries.Single(p => p.Name == "Bar");
            Assert.AreEqual(DomainOperation.Invoke, doe.Operation);

            doe = dsd.DomainOperationEntries.Single(p => p.Name == "AppendMoreAddresses");
            Assert.AreEqual(DomainOperation.Invoke, doe.Operation);
        }

        [TestMethod]
        public void ComplexTypes_ServiceDescription_Invoke_Custom_ComplexTypes()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.ComplexInvokeAndCustom));

            List<Type> entityTypes = TestDomainServices.ComplexInvokeAndCustom.GetExposedEntityTypes();
            List<Type> complexTypes = TestDomainServices.ComplexInvokeAndCustom.GetExposedComplexTypes();

            Assert.AreEqual(entityTypes.Count, dsd.EntityTypes.Count());
            Assert.AreEqual(complexTypes.Count, dsd.ComplexTypes.Count());

            foreach (Type entityType in dsd.EntityTypes)
            {
                Assert.IsTrue(entityTypes.Contains(entityType), string.Format("", "DSD exposes EntityType {0}, but the DS does not expose it", entityType.Name));;
            }

            foreach (Type complexType in dsd.ComplexTypes)
            {
                Assert.IsTrue(complexTypes.Contains(complexType), string.Format("", "DSD exposes ComplexType {0}, but the DS does not expose it", complexType.Name)); ;
            }
        }

        /// <summary>
        /// Verify that projected members do not take the DataMemberAttribute from their source
        /// member - they get their own new attribute instead.
        /// </summary>
        [TestMethod]
        [WorkItem(877984)]
        public void ProjectionInclude_DataMemberAttribute()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(DomainService_Bug877984));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(Bug877984_TestEntity))["BestFriendName"];
            DataMemberAttribute dma = (DataMemberAttribute)pd.Attributes[typeof(DataMemberAttribute)];
            Assert.IsNull(dma.Name); 
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
        [WorkItem(858226)]
        public void EFInvokeOperationConvention()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(Bug858226_Service));
            DomainOperationEntry doe = dsd.DomainOperationEntries.Single(p => p.Name == "RegisterUser");
            Assert.AreEqual(DomainOperation.Invoke, doe.Operation);
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

        /// <summary>
        /// Verify that our EF custom type descriptors work for POCO models
        /// </summary>
        [TestMethod]
        public void DomainServiceDescription_EFPOCO()
        {
            // First create a context manually and verify that POCO metadata is configured correctly
            NorthwindPOCOModel.NorthwindEntities ctxt = new NorthwindPOCOModel.NorthwindEntities();
            EntityType entityType = ctxt.Products.EntitySet.ElementType;
            Assert.IsNotNull(entityType);

            // direct test verifying that our helper methods work for POCO metadata
            entityType = (EntityType)ObjectContextUtilities.GetEdmType(ctxt.MetadataWorkspace, typeof(NorthwindPOCOModel.Product));
            Assert.IsNotNull(entityType);

            // E2E DomainServiceDescription test, verifying that our custom TDs are registered
            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.NorthwindPOCO));

            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(NorthwindPOCOModel.Product))["ProductID"];
            Assert.IsNotNull(pd.Attributes[typeof(KeyAttribute)]);

            pd = TypeDescriptor.GetProperties(typeof(NorthwindPOCOModel.Product))["Category"];
            AssociationAttribute assocAttrib = (AssociationAttribute)pd.Attributes[typeof(AssociationAttribute)];
            Assert.IsNotNull(assocAttrib);
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
        /// Verify that the copy constructor copies all required members.
        /// </summary>
        [TestMethod]
        public void DomainServiceDescription_CopyConstructor()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));

            // now create a copy
            DomainServiceDescription dsdCopy = new DomainServiceDescription(dsd);
            dsdCopy.Initialize();

            // ensure all required info was copied correctly
            Assert.IsTrue(dsd.Attributes.Cast<Attribute>().SequenceEqual(dsdCopy.Attributes.Cast<Attribute>()));
            Assert.IsTrue(dsd.DomainOperationEntries.SequenceEqual(dsdCopy.DomainOperationEntries));
        }

        [TestMethod]
        [Description("A new override of an entity property raises an exception")]
        public void Polymorphic_Property_New_Illegal()
        {
            // Scenario 1 -- a hidden intermediate entity uses new.  The visible derived
            // entity is rejected because of that.
            string errorMessage = string.Format(CultureInfo.CurrentCulture, 
                                                Resource.Entity_Property_Redefined, 
                                                typeof(Inherit_Polymorphic_New1_Derived_Visible), 
                                                "BaseProperty",
                                                typeof(Inherit_Polymorphic_New1_Entity));
            ExceptionHelper.ExpectInvalidOperationException(() => DomainServiceDescription.GetDescription(typeof(Inherit_Polymorphic_New1_DomainService)), errorMessage);

            // Scenario 2 -- an entity cannot use new to override a property on a non-exposed base
            errorMessage = string.Format(CultureInfo.CurrentCulture,
                                                Resource.Entity_Property_Redefined,
                                                typeof(Inherit_Polymorphic_New2_Entity),
                                                "BaseProperty",
                                                typeof(Inherit_Polymorphic_New2_Below_Entity));
            ExceptionHelper.ExpectInvalidOperationException(() => DomainServiceDescription.GetDescription(typeof(Inherit_Polymorphic_New2_DomainService)), errorMessage);
        }

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

        [TestMethod]
        [Description("Verify that our DAL providers correctly infer and apply DatabaseGeneratedAttribute")]
        public void DatabaseGeneratedAttributeAttribute_DALInference_EFIdentity()
        {
            // Verify that members that are db generated have the attribute applied, and those that don't do not
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.EF.Northwind));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Product))["ProductID"];
            Attribute dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNotNull(dbAttr);
            Assert.IsTrue(CompareDatabaseGeneratedOption(dbAttr, "Identity"));
            pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Product))["ProductName"];
            dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNull(dbAttr);

            pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Category))["CategoryID"];
            dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNotNull(dbAttr);
            Assert.IsTrue(CompareDatabaseGeneratedOption(dbAttr, "Identity"));
            pd = TypeDescriptor.GetProperties(typeof(NorthwindModel.Category))["CategoryName"];
            dbAttr = GetDatabaseGeneratedAttribute(pd.Attributes);
            Assert.IsNull(dbAttr);

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
        [Description("Asserts that underdefined EF properties with null facet values are successfully handled during timestamp comparison.")]
        [WorkItem(857020)]
        public void EF_TimestampComparisonHandlesNullFacetValues()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(DataTests.Scenarios.EF.Northwind.EF_NorthwindScenarios_TimestampComparison));
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(DataTests.Scenarios.EF.Northwind.EntityWithNullFacetValuesForTimestampComparison));

            PropertyDescriptor property = properties["StringWithoutConcurrencyMode"];
            Assert.IsNotNull(property,
                "StringWithoutConcurrencyMode property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutConcurrencyMode property is not a timestamp.");

            property = properties["StringWithoutFixedLength"];
            Assert.IsNotNull(property,
                "StringWithoutFixedLength property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutFixedLength property is not a timestamp.");

            property = properties["StringWithoutMaxLength"];
            Assert.IsNotNull(property,
                "StringWithoutMaxLength property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutMaxLength property is not a timestamp.");

            property = properties["StringWithoutComputed"];
            Assert.IsNotNull(property,
                "StringWithoutComputed property should exist.");
            Assert.IsNull(property.Attributes[typeof(TimestampAttribute)],
                "StringWithoutComputed property is not a timestamp.");

            property = properties["ConcurrencyTimestamp"];
            Assert.IsNotNull(property,
                "ConcurrencyTimestamp property should exist.");
            Assert.IsNotNull(property.Attributes[typeof(TimestampAttribute)],
                "ConcurrencyTimestamp property is a timestamp.");
        }

        /// <summary>
        /// Can't have a child Update w/o a parent Update
        /// Can't have a child Insert w/o a parent Update or Insert
        /// Can't have a child Delete w/o a parent Update or Delete
        /// Can't have a child custom operation w/o a parent Update or Insert
        /// </summary>
        [TestMethod]
        [WorkItem(791884)]
        public void Composition_ExplicitChildOperationValidation()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(CompositionOperationValidation_InvalidScenario1));
            }, string.Format(Resource.Composition_ParentsMustSupportUpdate, typeof(Child)));

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(CompositionOperationValidation_InvalidScenario2));
            }, string.Format(Resource.Composition_ParentsMustSupportInsert, typeof(Child)));

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(CompositionOperationValidation_InvalidScenario3));
            }, string.Format(Resource.Composition_ParentsMustSupportDelete, typeof(Child)));

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(CompositionOperationValidation_InvalidScenario4));
            }, string.Format(Resource.Composition_ParentsMustSupportUpdate, typeof(Child)));

            // expect all of these valid scenarios to succeed
            DomainServiceDescription.GetDescription(typeof(CompositionOperationValidation_ValidScenario1));
            DomainServiceDescription.GetDescription(typeof(CompositionOperationValidation_ValidScenario2));
        }

        [TestMethod]
        public void TestCompositionalDescription()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.CompositionScenarios_Explicit));

            // expect all Types (even child Types) to be listed
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(Parent)));
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(Child)));
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(GrandChild)));
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(GreatGrandChild)));
        }

        [TestMethod]
        [Description("DomainServiceDescription.IsComposedEntityType handles derived types")]
        public void Composition_Inheritance_IsComposedEntityType()
        {
            // Verify the mock scenarios domain service
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.CompositionInheritanceScenarios));

            // expect all Types (even child Types) to be listed
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(CI_Parent)));
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(CI_SpecialParent)));
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(CI_Child)));
            Assert.IsTrue(dsd.EntityTypes.Contains(typeof(CI_AdoptedChild)));

            // Test lazily computed composition hashset
            Assert.IsTrue(dsd.IsComposedEntityType(typeof(CI_Child)), "Child should have been IsComposed");
            Assert.IsTrue(dsd.IsComposedEntityType(typeof(CI_AdoptedChild)), "AdoptedChild should have been IsComposed");

            Assert.IsFalse(dsd.IsComposedEntityType(typeof(CI_Parent)), "Parent should not have been IsComposed");
            Assert.IsFalse(dsd.IsComposedEntityType(typeof(CI_SpecialParent)), "Parent should not have been IsComposed");

            // Verify the test-local hierarchy with a split in the hierarchy
            dsd = DomainServiceDescription.GetDescription(typeof(Derived_Composition_Types.Derived_Composition_DomainService));
            Assert.IsTrue(dsd.IsComposedEntityType(typeof(Derived_Composition_Types.C1)), "C1 should have been composed");
            Assert.IsTrue(dsd.IsComposedEntityType(typeof(Derived_Composition_Types.C2)), "C2 should have been composed");
            Assert.IsTrue(dsd.IsComposedEntityType(typeof(Derived_Composition_Types.C3)), "C3 should have been composed");

            IEnumerable<PropertyDescriptor> parentAssociations = dsd.GetParentAssociations(typeof(Derived_Composition_Types.C1));
            Assert.IsNotNull(parentAssociations, "C1 did not have parent associations");
            PropertyDescriptor[] pds = parentAssociations.ToArray();
            Assert.IsTrue(parentAssociations.Any(p => p.Name == "C1s"), "C1 did not show P1's associations");

            parentAssociations = dsd.GetParentAssociations(typeof(Derived_Composition_Types.C2));
            Assert.IsNotNull(parentAssociations, "C2 did not have parent associations");
            Assert.IsTrue(parentAssociations.Any(p => p.Name == "C1s"), "C2 did not show P1's associations");

            parentAssociations = dsd.GetParentAssociations(typeof(Derived_Composition_Types.C3));
            Assert.IsNotNull(parentAssociations, "C3 did not have parent associations");
            Assert.IsTrue(parentAssociations.Any(p => p.Name == "C1s"), "C3 did not show P1's associations");
        }

        /// <summary>
        /// Verify that self referencing composition relationships don't cause
        /// infinite recursion
        /// </summary>
        [TestMethod]
        public void Composition_SelfReferencing()
        {
            // It was these calls that were recursing infinitely during codegen
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.CompositionScenarios_SelfReferencingComposition_Update));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(SelfReferencingComposition), DomainOperation.Insert));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(SelfReferencingComposition), DomainOperation.Update));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(SelfReferencingComposition), DomainOperation.Delete));

            // Test the same scenario for a named update
            dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.CompositionScenarios_SelfReferencingComposition_NamedUpdate));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(SelfReferencingComposition), DomainOperation.Insert));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(SelfReferencingComposition), DomainOperation.Update));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(SelfReferencingComposition), DomainOperation.Delete));
        }

        /// <summary>
        /// Verify that composition cycles don't cause
        /// infinite recursion
        /// </summary>
        [TestMethod]
        public void Composition_Cycle()
        {
            // It was these calls that were recursing infinitely during codegen
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.Composition_TestCyclicComposition));

            Assert.IsTrue(dsd.IsOperationSupported(typeof(CompositionCycle_A), DomainOperation.Insert));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_A), DomainOperation.Update));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_A), DomainOperation.Delete));

            // checks on B or its children bring in a composition cycle
            Assert.IsTrue(dsd.IsOperationSupported(typeof(CompositionCycle_B), DomainOperation.Insert));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_B), DomainOperation.Update));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_B), DomainOperation.Delete));

            Assert.IsTrue(dsd.IsOperationSupported(typeof(CompositionCycle_C), DomainOperation.Insert));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_C), DomainOperation.Update));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_C), DomainOperation.Delete));

            Assert.IsTrue(dsd.IsOperationSupported(typeof(CompositionCycle_D), DomainOperation.Insert));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_D), DomainOperation.Update));
            Assert.IsFalse(dsd.IsOperationSupported(typeof(CompositionCycle_D), DomainOperation.Delete));
        }

        /// <summary>
        /// Verify that IsOperationSupported works correctly when the parent update operation is a
        /// "named update" rather than a normal update.
        /// </summary>
        [TestMethod]
        public void TestComposition_ParentNamedUpdate()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.CompositionScenarios_NamedUpdate));

            Assert.IsTrue(dsd.IsOperationSupported(typeof(Parent), DomainOperation.Update));

            // expect the rest of the tests to succeed since
            // there is a named update for the parent
            Assert.IsTrue(dsd.IsOperationSupported(typeof(Child), DomainOperation.Insert));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(Child), DomainOperation.Update));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(Child), DomainOperation.Delete));

            Assert.IsTrue(dsd.IsOperationSupported(typeof(GrandChild), DomainOperation.Insert));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(GrandChild), DomainOperation.Update));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(GrandChild), DomainOperation.Delete));

            Assert.IsTrue(dsd.IsOperationSupported(typeof(GreatGrandChild), DomainOperation.Insert));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(GreatGrandChild), DomainOperation.Update));
            Assert.IsTrue(dsd.IsOperationSupported(typeof(GreatGrandChild), DomainOperation.Delete));
        }

        [TestMethod]
        public void TestTryGetParent()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(TestDomainServices.CompositionScenarios_Explicit));

            IEnumerable<PropertyDescriptor> parentAssociations = dsd.GetParentAssociations(typeof(TestDomainServices.Child));
            Assert.IsNotNull(parentAssociations.SingleOrDefault(p => p.Name == "Children" && p.ComponentType == typeof(TestDomainServices.Parent)));

            parentAssociations = dsd.GetParentAssociations(typeof(TestDomainServices.GrandChild));
            Assert.IsNotNull(parentAssociations.SingleOrDefault(p => p.Name == "Children" && p.ComponentType == typeof(TestDomainServices.Child)));

            parentAssociations = dsd.GetParentAssociations(typeof(TestDomainServices.GreatGrandChild));
            Assert.IsNotNull(parentAssociations.SingleOrDefault(p => p.Name == "Child" && p.ComponentType == typeof(TestDomainServices.GrandChild)));

            parentAssociations = dsd.GetParentAssociations(typeof(TestDomainServices.Parent));
            Assert.AreEqual(0, parentAssociations.Count());
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

        [TestMethod]
        [Description("Ensure that overrides of base DomainService methods aren't inferred as operations")]
        public void DomainService_BaseMethodOverrides()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(DomainService_BaseMethodOverrides));
            Assert.AreEqual(0, dsd.DomainOperationEntries.Count());
        }

        [TestMethod]
        [Description("Verify that associations can't be marked with RequiredAttribute")]
        public void EntityWithRequiredAssociation()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainService_EntityWithRequiredAssociation));
            }, string.Format(CultureInfo.CurrentCulture, Resource.Entity_RequiredAssociationNotAllowed, typeof(EntityWithRequiredAssociation), "RequiredAssociation"));
        }

        [TestMethod]
        public void DomainServiceDescriptionProviderAttribute_InvalidTypes()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                var att = new DomainServiceDescriptionProviderAttribute(typeof(string));
                att.CreateProvider(typeof(DomainService_Basic), null);
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidType, typeof(string).FullName, typeof(DomainServiceDescriptionProvider).FullName));

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                var att = new DomainServiceDescriptionProviderAttribute(typeof(DescriptionProviderWithInvalidConstructor));
                att.CreateProvider(typeof(DomainService_Basic), null);
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescriptionProviderAttribute_MissingConstructor, typeof(DescriptionProviderWithInvalidConstructor).FullName));

            ExceptionHelper.ExpectArgumentException(delegate
            {
                var att = new DomainServiceDescriptionProviderAttribute(typeof(DescriptionProviderA));
                att.CreateProvider(typeof(string), null);
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidType, typeof(string).FullName, typeof(DomainService).FullName), "domainServiceType");
        }

        [TestMethod]
        public void IncompatibleOperationParentType()
        {
            ReflectionDomainServiceDescriptionProvider provider = new ReflectionDomainServiceDescriptionProvider(typeof(SingletonQueryMethod_ValidScenarios));
            DomainServiceDescription description = provider.GetDescription();
            TestDomainOperationEntry entry = new TestDomainOperationEntry(typeof(CityDomainService), "GetCities", DomainOperation.Query, typeof(City), new DomainOperationParameter[0], AttributeCollection.Empty);

            ExceptionHelper.ExpectArgumentException(delegate
            {
                description.AddOperation(entry);
            }, new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_IncompatibleOperationParentType, "GetCities", typeof(SingletonQueryMethod_ValidScenarios), typeof(CityDomainService)), "operation").Message);
        }

        [TestMethod]
        public void SingletonQueryMethods()
        {
            // verify that a singleton query method can be inferred
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(SingletonQueryMethod_ValidScenarios));
            DomainOperationEntry entry = description.GetQueryMethod("GetCityByName");
            Assert.IsNotNull(entry);
            Assert.AreEqual(false, ((QueryAttribute)entry.OperationAttribute).IsComposable);

            // verify explicitly attributed singleton query method
            entry = description.GetQueryMethod("GetStateByName");
            Assert.IsNotNull(entry);
            Assert.AreEqual(false, ((QueryAttribute)entry.OperationAttribute).IsComposable);

            // verify that attempting to mark a singleton returning query method
            // as composable results in an exception
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                description = DomainServiceDescription.GetDescription(typeof(SingletonQueryMethod_NonComposable));
            }, string.Format(Resource.DomainServiceDescription_SingletonQueryMethodCannotCompose, "GetCityByName", typeof(City)));

            // This second test is for an issue captured by bug 711141
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                description = DomainServiceDescription.GetDescription(typeof(SingletonQueryMethod_NonComposable2));
            }, string.Format(Resource.DomainServiceDescription_SingletonQueryMethodCannotCompose, "GetCityByName", typeof(City)));

            // test false positives
            description = DomainServiceDescription.GetDescription(typeof(SingletonQueryMethod_FalsePositives));
            DomainOperationEntry[] entries = description.DomainOperationEntries.ToArray();
            Assert.AreEqual(2, entries.Length);
            Assert.AreEqual(DomainOperation.Invoke, entries[0].Operation);
            Assert.AreEqual(DomainOperation.Invoke, entries[1].Operation);
        }

        [TestMethod]
        public void InvokeOperationArrayReturnType()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(SimpleDomainService_InvokeOperations));
            DomainOperationEntry[] operations = description.DomainOperationEntries.ToArray();

            Assert.AreEqual(8, operations.Length);

            Assert.IsNotNull(description.GetInvokeOperation("GetString"));
            Assert.IsNotNull(description.GetInvokeOperation("GetStringArray"));
            Assert.IsNotNull(description.GetInvokeOperation("GetStringEnumerable"));
            Assert.IsNotNull(description.GetInvokeOperation("GetStringEnumerableWithParam1"));
            Assert.IsNull(description.GetInvokeOperation("GetStringEnumerableWithParam2"));
            Assert.IsNotNull(description.GetQueryMethod("GetCity"));
            Assert.IsNotNull(description.GetQueryMethod("GetCityArray"));
            Assert.IsNotNull(description.GetQueryMethod("GetCityEnumerable"));
            Assert.IsNotNull(description.GetQueryMethod("GetCityIQueryable"));
        }

        /// <summary>
        /// Verifies that when a custom TDP is registered for a base type, we still return the right properties for its derived types.
        /// </summary>
        [WorkItem(843965)]
        [TestMethod]
        public void DomainDescriptionProvider_EntityTypeInheritance()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DomainService_Inheritance));

            // Verify type at level 2 in the hierarchy.
            Type rootType = description.EntityTypes.Single(t => t == typeof(Mock_CG_BaseEntity));
            Assert.IsNotNull(rootType, "Root type not found.");

            PropertyDescriptor dataProperty = TypeDescriptor.GetProperties(rootType)["Data"];
            PropertyDescriptor data2Property = TypeDescriptor.GetProperties(rootType)["Data2"];
            PropertyDescriptor data3Property = TypeDescriptor.GetProperties(rootType)["Data3"];
            Assert.IsNotNull(dataProperty, "Inherited property not found.");
            Assert.IsNull(data2Property, "Inherited derived property was found.");
            Assert.IsNull(data3Property, "Derived property was found.");

            // Verify type at level 2 in the hierarchy.
            Type derivedType = description.EntityTypes.Single(t => t == typeof(Mock_CG_DerivedEntity));
            Assert.IsNotNull(derivedType, "Derived type not found.");

            dataProperty = TypeDescriptor.GetProperties(derivedType)["Data"];
            data2Property = TypeDescriptor.GetProperties(derivedType)["Data2"];
            data3Property = TypeDescriptor.GetProperties(derivedType)["Data3"];
            Assert.IsNotNull(dataProperty, "Inherited property not found.");
            Assert.IsNotNull(data2Property, "Inherited derived property not found.");
            Assert.IsNull(data3Property, "Derived property was found.");

            // Verify type at level 3 in the hierarchy.
            Type derivedType2 = description.EntityTypes.Single(t => t == typeof(Mock_CG_DerivedDerivedEntity));
            Assert.IsNotNull(derivedType2, "Derived type #2 not found.");

            dataProperty = TypeDescriptor.GetProperties(derivedType)["Data"];
            data2Property = TypeDescriptor.GetProperties(derivedType2)["Data2"];
            data3Property = TypeDescriptor.GetProperties(derivedType2)["Data3"];
            Assert.IsNotNull(dataProperty, "Inherited property not found.");
            Assert.IsNotNull(data2Property, "Inherited derived property not found.");
            Assert.IsNotNull(data3Property, "Derived property not found.");

            // Verify the helper methods for derived entities

            // Valid question about root should return 2 derived types
            Type[] derivedTypes = description.GetEntityDerivedTypes(typeof(Mock_CG_BaseEntity)).ToArray();
            Assert.AreEqual(2, derivedTypes.Length, "Should be 2 derived types");
            Assert.IsTrue(derivedTypes.Contains(typeof(Mock_CG_DerivedEntity)), "derived types did not include Mock_CG_DerivedEntity");
            Assert.IsTrue(derivedTypes.Contains(typeof(Mock_CG_DerivedDerivedEntity)), "derived types did not include Mock_CG_DerivedEntity");

            // Valid question about middle entity should return 1 derived
            derivedTypes = description.GetEntityDerivedTypes(typeof(Mock_CG_DerivedEntity)).ToArray();
            Assert.AreEqual(1, derivedTypes.Length, "Should be 1 derived type");
            Assert.IsTrue(derivedTypes.Contains(typeof(Mock_CG_DerivedDerivedEntity)), "derived types did not include Mock_CG_DerivedEntity");

            derivedTypes = description.GetEntityDerivedTypes(typeof(Mock_CG_DerivedDerivedEntity)).ToArray();
            Assert.AreEqual(0, derivedTypes.Length, "Should be no derived type");
        }

        /// <summary>
        /// Verify that the descriptors defined in a DomainService inheritance hierarchy are
        /// "chained" such that each can contribute to the description.
        /// </summary>
        [TestMethod]
        public void DomainDescriptionProvider_Inheritance()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DSDTestServiceB));
            DomainOperationEntry[] operations = description.DomainOperationEntries.ToArray();

            Assert.AreEqual(6, operations.Length);
            Assert.AreEqual(6, description.EntityTypes.Count());

            // verify that the operations added at each level are present,
            // including the base reflection operations
            Assert.IsNotNull(description.GetQueryMethod("A1"));
            Assert.IsNotNull(description.GetQueryMethod("A2"));
            Assert.IsNotNull(description.GetQueryMethod("B1"));
            Assert.IsNotNull(description.GetQueryMethod("B1"));
            Assert.IsNotNull(description.GetQueryMethod("GetCities"));
            Assert.IsNotNull(description.GetQueryMethod("GetZips"));
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
        public void DomainDescriptionProvider_AutoCudScenario()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DSDTestService_CUDFields));
            DomainOperationEntry[] operations = description.DomainOperationEntries.ToArray();

            Assert.AreEqual(8, operations.Length);
            Assert.AreEqual(4, description.EntityTypes.Count());

            // verify State virtual CRUD methods
            Assert.IsNotNull(description.GetQueryMethod("GetStates"));
            Assert.IsNotNull(description.GetSubmitMethod(typeof(State), DomainOperation.Insert));
            Assert.IsNotNull(description.GetSubmitMethod(typeof(State), DomainOperation.Update));
            Assert.IsNotNull(description.GetSubmitMethod(typeof(State), DomainOperation.Delete));

            // verify City virtual CRUD methods
            var getCities = description.GetQueryMethod("GetCities");
            Assert.IsNotNull(getCities);
            Assert.IsNotNull(getCities.OperationAttribute);
            Assert.IsInstanceOfType(getCities.OperationAttribute, typeof(QueryAttribute));

            var insertCity = description.GetSubmitMethod(typeof(City), DomainOperation.Insert);
            Assert.IsNotNull(insertCity);
            Assert.IsNotNull(insertCity.OperationAttribute);
            Assert.IsInstanceOfType(insertCity.OperationAttribute, typeof(InsertAttribute));

            var updateCity = description.GetSubmitMethod(typeof(City), DomainOperation.Update);
            Assert.IsNotNull(updateCity);
            Assert.IsNotNull(updateCity.OperationAttribute);
            Assert.IsInstanceOfType(updateCity.OperationAttribute, typeof(UpdateAttribute));

            var deleteCity = description.GetSubmitMethod(typeof(City), DomainOperation.Delete);
            Assert.IsNotNull(deleteCity);
            Assert.IsNotNull(deleteCity.OperationAttribute);
            Assert.IsInstanceOfType(deleteCity.OperationAttribute, typeof(DeleteAttribute));
        }


        [TestMethod]
        [WorkItem(835656)]
        public void DomainDescriptionProvider_AutoCud_ExtensionScenario()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(DSDTestService_AutoCrud_ReflExtensions));
            DomainOperationEntry[] operations = description.DomainOperationEntries.ToArray();

            Assert.AreEqual(10, operations.Length);
            Assert.AreEqual(4, description.EntityTypes.Count());

            // verify custom method
            Assert.IsNotNull(description.GetCustomMethod(typeof(City), "IncreaseTaxes"));

            // verify query method
            Assert.IsNotNull(description.GetQueryMethod("GetStatesByZone"));

            // verify that a domain service description provider can add operation
            // attributes
            foreach (DomainOperationEntry operation in operations)
            {
                int attribCount = operation.Attributes.OfType<TestAttributeA>().Count();
                if (operation.Name.Length % 2 == 0)
                {
                    Assert.AreEqual(1, attribCount);
                }
                else
                {
                    Assert.AreEqual(0, attribCount);
                }
            }
        }

        [TestMethod]
        public void DomainServiceDescription_Initialization()
        {
            DomainServiceDescription description = new DomainServiceDescription(typeof(DSDTestServiceA));

            description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, "B1", DomainOperation.Query, typeof(IEnumerable<County>), new DomainOperationParameter[0], AttributeCollection.Empty));
            description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, "B2", DomainOperation.Query, typeof(IEnumerable<County>), new DomainOperationParameter[0], AttributeCollection.Empty));

            // expect exceptions when using an uninitialized description
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                description.GetQueryMethod("B1");
            },
            Resource.DomainServiceDescription_Uninitialized);

            // DONT expecte an exception when accessing the operation entries or
            // Attributes
            int count = description.Attributes.Count;
            description.DomainOperationEntries.Count();

            description.Initialize();

            DomainOperationEntry operation = description.GetQueryMethod("B1");
            Assert.IsNotNull(operation);
            Assert.AreEqual(2, description.DomainOperationEntries.Count());

            // verify that once a description has been initialized, it can no longer be modified
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, "B3", DomainOperation.Query, typeof(IEnumerable<County>), new DomainOperationParameter[0], AttributeCollection.Empty));
            },
            Resource.DomainServiceDescription_InvalidUpdate);
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

        /// <summary>
        /// Direct tests for the parse method used by the LTS TDP
        /// </summary>
        [TestMethod]
        public void TestLTSDataTypeParsing()
        {
            VerifyStringLengthAttribute("NChar(50)", new StringLengthAttribute(50));
            VerifyStringLengthAttribute("vArchar(50)", new StringLengthAttribute(50));
            VerifyStringLengthAttribute("char(50)", new StringLengthAttribute(50));
            VerifyStringLengthAttribute("nVarchar(50)", new StringLengthAttribute(50));
            VerifyStringLengthAttribute("sdf NChar(50) sdf", null);
            VerifyStringLengthAttribute("TEXT NOT NULL", null);
            VerifyStringLengthAttribute("char(50", null);
            VerifyStringLengthAttribute("nVarchar50)", null);
            VerifyStringLengthAttribute("", null);
            VerifyStringLengthAttribute("int", null);
            VerifyStringLengthAttribute("()(()", null);
            VerifyStringLengthAttribute("char(ksdf)", null);
            VerifyStringLengthAttribute("char(max)", null);
            VerifyStringLengthAttribute("(foo) char(50)", null);
            VerifyStringLengthAttribute("nvarchar(", null);
            VerifyStringLengthAttribute(" char(50)", new StringLengthAttribute(50));
            VerifyStringLengthAttribute("   char(50)", new StringLengthAttribute(50));
            VerifyStringLengthAttribute(" char(50", null);
            VerifyStringLengthAttribute("  char(50)  ", new StringLengthAttribute(50));
            VerifyStringLengthAttribute(" c    ", null);
            VerifyStringLengthAttribute("nvarchar()", null);
        }

        private void VerifyStringLengthAttribute(string dbType, StringLengthAttribute expectedAttribute)
        {
            List<Attribute> attributes = new List<Attribute>();
            LinqToSqlTypeDescriptor.InferStringLengthAttribute(dbType, attributes);

            if (expectedAttribute != null)
            {
                StringLengthAttribute sla = attributes.Cast<StringLengthAttribute>().Single();
                Assert.AreEqual(expectedAttribute.MaximumLength, sla.MaximumLength);
            }
            else
            {
                Assert.IsTrue(attributes.Count == 0);
            }
        }

        /// <summary>
        /// Test verifying that our ExplicitAttributes filter logic works as expected
        /// </summary>
        [TestMethod]
        public void TestExplicitAttributesFiltering()
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(AttributeTest))["A"];
            Attribute[] attribs = attribs = TheTypeDescriptorExtensions.ExplicitAttributes(pd).OfType<Attribute>().ToArray();
            Assert.AreEqual(0, attribs.OfType<MetadataTypeAttribute>().Count());
            Assert.AreEqual(0, attribs.OfType<ReadOnlyAttribute>().Count());

            // expect TD to return cached instances
            Attribute a1 = TypeDescriptor.GetAttributes(typeof(AttributeTest))[0];
            Attribute a2 = TypeDescriptor.GetAttributes(typeof(AttributeTest))[0];
            Assert.AreSame(a1, a2);

            // now register the buddy metadata provider
            AssociatedMetadataTypeTypeDescriptionProvider provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(AttributeTest));
            TypeDescriptor.AddProvider(provider, typeof(AttributeTest));

            // expect TD to return cached instances with custom TDPs in the chain
            a1 = TypeDescriptor.GetAttributes(typeof(AttributeTest))[0];
            a2 = TypeDescriptor.GetAttributes(typeof(AttributeTest))[0];
            Assert.AreSame(a1, a2);

            // Verify that we can successfully strip out inherited Type level attributes from the
            // property
            pd = TypeDescriptor.GetProperties(typeof(AttributeTest))["A"];
            attribs = TheTypeDescriptorExtensions.ExplicitAttributes(pd).OfType<Attribute>().ToArray();
            Assert.AreEqual(0, attribs.OfType<MetadataTypeAttribute>().Count());
            Assert.AreEqual(0, attribs.OfType<DisplayColumnAttribute>().Count());
            Assert.AreEqual(1, attribs.OfType<EditableAttribute>().Count());

            // Verify that the Type level attributes applied to both the Type
            // itself as well as the buddy class are still returned
            attribs = TypeDescriptor.GetAttributes(typeof(AttributeTest)).OfType<Attribute>().ToArray();
            Assert.AreEqual(2, attribs.Length);
            Assert.AreEqual(1, attribs.OfType<MetadataTypeAttribute>().Count());
            Assert.AreEqual(1, attribs.OfType<DisplayColumnAttribute>().Count());

            // Verify that we don't inherit attributes with Inherit=false as their attribute usage.
            attribs = TheTypeDescriptorExtensions.Attributes(typeof(AttributeTestTypeDerived)).OfType<Attribute>().ToArray();
            Assert.AreEqual(1, attribs.Length);
            Assert.AreEqual(1, attribs.OfType<AttributeTestInheritAttribute>().Count());
            Assert.AreEqual(0, attribs.OfType<AttributeTestNonInheritAttribute>().Count());

            // Verify that we get back attributes with Inherit=false when the attribute was 
            // put on the type we're reflecting on.
            attribs = TheTypeDescriptorExtensions.Attributes(typeof(AttributeTestTypeBase)).OfType<Attribute>().ToArray();
            Assert.AreEqual(2, attribs.Length);
            Assert.AreEqual(1, attribs.OfType<AttributeTestInheritAttribute>().Count());
            Assert.AreEqual(1, attribs.OfType<AttributeTestNonInheritAttribute>().Count());
        }

        [TestMethod]
        public void DomainService_GenericType()
        {
            Type t = typeof(GenericDomainService<Mock_CG_SimpleEntity>);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(t);
            }, string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainService_InvalidType, t.FullName));
        }

        [TestMethod]
        public void DomainService_InterfaceType()
        {
            Type t = typeof(IAppDomainSetup);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(t);
            }, string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainService_InvalidType, t.FullName));
        }

        [TestMethod]
        public void DomainService_GenericTypeDefinition()
        {
            Type t = typeof(GenericDomainService<>);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(t);
            }, string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainService_InvalidType, t.FullName));
        }

        [TestMethod]
        public void DomainService_Abstract()
        {
            Type t = typeof(AbstractDomainService);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(t);
            }, string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainService_InvalidType, t.FullName));
        }

        [TestMethod]
        public void TestNonFKEntityFrameworkModels()
        {
            // first try a model where FKs are part of CSDL
            Type contextType = typeof(NorthwindModel.NorthwindEntities);
            LinqToEntitiesTypeDescriptionContext ctxt = new LinqToEntitiesTypeDescriptionContext(contextType);
            EntityType edmType = (EntityType)ctxt.GetEdmType(typeof(NorthwindModel.Product));
            NavigationProperty navProp = edmType.NavigationProperties.Single(p => p.Name == "Category");
            AssociationInfo assocInfo = ctxt.GetAssociationInfo(navProp);
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

        [EnableClientAccess]
        public class TestAWDomainService : LinqToEntitiesDomainService<AdventureWorksModel.AdventureWorksEntities>
        {
            // Expose an entity with a multipart key reference
            public IQueryable<AdventureWorksModel.SalesOrderDetail> GetSalesOrderDetails()
            {
                return null;
            }
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

        [TestMethod]
        public void DomainServiceDescription_VirtualMethods()
        {
            DomainServiceDescription.GetDescription(typeof(Provider_VirtualMethods));
            DomainServiceDescription.GetDescription(typeof(Provider_OverridenMethods));
        }

        [TestMethod]
        public void DomainServiceDescription_Convention()
        {
            var d = DomainServiceDescription.GetDescription(typeof(Provider_Convention_1));
            var result = d.DomainOperationEntries.OrderBy(op => op.Name).Select(op => op.Name).Aggregate((l, r) => l + "," + r);
            Assert.AreEqual("Add,DeleteCity,DoSomething,GetCities,InsertCity,UpdateCity", result);

            d = DomainServiceDescription.GetDescription(typeof(Provider_Convention_2));
            result = d.DomainOperationEntries.OrderBy(op => op.Name).Select(op => op.Name).Aggregate((l, r) => l + "," + r);
            Assert.AreEqual("AddCity,GetCities,ModifyCity,RemoveCity", result);

            d = DomainServiceDescription.GetDescription(typeof(Provider_Convention_3));
            result = d.DomainOperationEntries.OrderBy(op => op.Name).Select(op => op.Name).Aggregate((l, r) => l + "," + r);
            Assert.AreEqual("ChangeCity,CreateCity,GetCities", result);

            d = DomainServiceDescription.GetDescription(typeof(Provider_Convention_Complex));
            result = d.DomainOperationEntries.OrderBy(op => op.Name).Select(op => op.Name).Aggregate((l, r) => l + "," + r);
            Assert.AreEqual("FindAnotherWithPhone,FindOthersWithAddressAndPhone,GetParents,MoveParent", result);
            result = d.DomainOperationEntries.OrderBy(op => op.Name).Select(op => op.Operation.ToString()).Aggregate((l, r) => l + "," + r);
            Assert.AreEqual("Invoke,Invoke,Query,Custom", result);
        }

        [TestMethod]
        public void DomainServiceDescription_InterfaceAttributes()
        {
            var d = DomainServiceDescription.GetDescription(typeof(InterfaceInheritanceDomainService));
            Assert.IsTrue(d.DomainOperationEntries.Count() > 0, "Expected to find DomainOperationEntries.");

            // Step 1: Verify attributes at type level.
            //

            // We should have 6 'MockAttributeAllowMultiple' types with 3 originating from the interface, 3 from the class
            var typeLevelAttributes_AllowMultiple = d.Attributes.OfType<MockAttributeAllowMultiple>();
            Assert.AreEqual(6, typeLevelAttributes_AllowMultiple.Count());
            Assert.AreEqual(3, typeLevelAttributes_AllowMultiple.Count(a => a.Value == "Class"));
            Assert.AreEqual(3, typeLevelAttributes_AllowMultiple.Count(a => a.Value == "Interface"));

            // We should have 1 'MockAttributeAllowOnce' originating from the class (overriding the interface).
            var typeLevelAttributes_AllowOnce = d.Attributes.OfType<MockAttributeAllowOnce>();
            Assert.AreEqual(1, typeLevelAttributes_AllowOnce.Count());
            Assert.AreEqual(1, typeLevelAttributes_AllowOnce.Count(a => a.Value == "Class"));
            Assert.AreEqual(0, typeLevelAttributes_AllowOnce.Count(a => a.Value == "Interface"));

            // We should have 1 'MockAttributeAllowOnce_AppliedToInterfaceOnly' originating from the interface.
            var typeLevelAttributes_AppliedToInterfaceOnly = d.Attributes.OfType<MockAttributeAllowOnce_AppliedToInterfaceOnly>();
            Assert.AreEqual(1, typeLevelAttributes_AppliedToInterfaceOnly.Count());
            Assert.AreEqual(0, typeLevelAttributes_AppliedToInterfaceOnly.Count(a => a.Value == "Class"));
            Assert.AreEqual(1, typeLevelAttributes_AppliedToInterfaceOnly.Count(a => a.Value == "Interface"));

            // We should not have any interface-only attributes on our DomainService!
            var typeLevelAttributes_AllowOnceInterfaceOnly = d.Attributes.OfType<MockAttributeAllowOnce_InterfaceOnly>();
            Assert.AreEqual(0, typeLevelAttributes_AllowOnceInterfaceOnly.Count());

            // We should not have any interface-only attributes on our DomainService!
            var typeLevelAttributes_AllowMultipleInterfaceOnly = d.Attributes.OfType<MockAttributeAllowMultiple_InterfaceOnly>();
            Assert.AreEqual(0, typeLevelAttributes_AllowMultipleInterfaceOnly.Count());

            // Step 2: Verify attributes on methods
            //

            // We should have the following operations as defined by the interface
            var result = d.DomainOperationEntries.OrderBy(op => op.Name).Select(op => op.Name).Aggregate((l, r) => l + "," + r);
            Assert.AreEqual("EntityWithXElement_Custom_AttributeAggregation,EntityWithXElement_Custom_AttributeOverrides,EntityWithXElement_Delete,EntityWithXElement_Get,EntityWithXElement_Insert,EntityWithXElement_Update", result);

            // Verify each method
            foreach (var op in d.DomainOperationEntries)
            {
                switch (op.Name)
                {
                    case "EntityWithXElement_Custom_AttributeAggregation":
                        {
                            Assert.AreEqual(DomainOperation.Custom, op.Operation);

                            var attributes = op.Attributes.OfType<MockAttributeAllowMultiple>();
                            Assert.AreEqual(6, attributes.Count());
                            Assert.AreEqual(3, attributes.Count(a => a.Value == "Class"));
                            Assert.AreEqual(3, attributes.Count(a => a.Value == "Interface"));
                        }
                        break;
                    case "EntityWithXElement_Custom_AttributeOverrides":
                        {
                            Assert.AreEqual(DomainOperation.Custom, op.Operation);

                            var attributes = op.Attributes.OfType<MockAttributeAllowOnce>();
                            Assert.AreEqual(1, attributes.Count());
                            Assert.AreEqual(1, attributes.Count(a => a.Value == "Class"));
                            Assert.AreEqual(0, attributes.Count(a => a.Value == "Interface"));

                            var attributes_interfaceOnly = op.Attributes.OfType<MockAttributeAllowOnce_AppliedToInterfaceOnly>();
                            Assert.AreEqual(1, attributes_interfaceOnly.Count());
                            Assert.AreEqual(0, attributes_interfaceOnly.Count(a => a.Value == "Class"));
                            Assert.AreEqual(1, attributes_interfaceOnly.Count(a => a.Value == "Interface"));
                        }
                        break;
                    case "EntityWithXElement_Delete":
                        Assert.AreEqual(DomainOperation.Delete, op.Operation);
                        break;
                    case "EntityWithXElement_Get":
                        Assert.AreEqual(DomainOperation.Query, op.Operation);
                        break;
                    case "EntityWithXElement_Insert":
                        Assert.AreEqual(DomainOperation.Insert, op.Operation);
                        break;
                    case "EntityWithXElement_Update":
                        Assert.AreEqual(DomainOperation.Update, op.Operation);
                        break;
                    default:
                        Assert.Fail("Unexpected DomainOperationEntry");
                        break;
                }
            }
        }

#if MEDIUM_TRUST

        [TestMethod]
        [Description("Verifies the medium trust sandbox calls back as SecurityTransparent")]
        public void DomainServiceDescription_MediumTrust_Verify_Sandbox_IsTransparent()
        {
            Assert.IsFalse(typeof(DomainServiceDescriptionTest).IsSecurityTransparent, "Expected our unit test to be not transparent");
            SandBoxer.ExecuteInMediumTrust(Callback_Verify_Sandbox_Transparent);
        }

        public static void Callback_Verify_Sandbox_Transparent()
        {
            Assert.IsTrue(typeof(DomainServiceDescriptionTest).IsSecurityTransparent, "Expected our unit test to be transparent");
        }

        [TestMethod]
        [Description("Verifies the medium trust sandbox assert and parameter mechanisms are handled properly")]
        public void DomainServiceDescription_MediumTrust_Verify_SandboxAssertMechanism()
        {
            AssertFailedException expectedException = null;
            AssertFailedException actualException = null;

            // Capture a real Assert.Fail exception for comparison
            try
            {
                Assert.Fail("oops");
            }
            catch (AssertFailedException afe)
            {
                expectedException = afe;
            }

            // Now go do that in partial trust
            try
            {
                SandBoxer.ExecuteInMediumTrust(Callback_Verify_SandboxAssertException, "oops");
                Assert.Fail("SandBox mechanism failed to throw");
            }
            catch (AssertFailedException afe)
            {
                actualException = afe;
            }

            Assert.AreEqual(expectedException.Message, actualException.Message, "SandBoxAssertException did not work properly.");
        }

        public static void Callback_Verify_SandboxAssertException(string message)
        {
            Assert.Fail(message);
        }

        // TODO: [roncain] Investigating why this succeeds
        //[TestMethod]
        //[Description("Private reflection should not work in partial trust")]
        //public void DomainServiceDescription_MediumTrust_PrivateReflection_Throws()
        //{
        //    MethodInfo mInfo = typeof(DomainServiceDescriptionTest).GetMethod("AFineAndPrivatePlace", BindingFlags.Static | BindingFlags.NonPublic);
        //    Assert.IsNotNull(mInfo, "Expected to find AFineAndPrivatePlace method");
        //    SandBoxer.ExecuteInMediumTrust(Callback_Verify_Sandbox_PrivateReflection);
        //}

        //public static void Callback_Verify_Sandbox_PrivateReflection()
        //{
        //    try
        //    {
        //        MethodInfo mInfo = typeof(DomainServiceDescriptionTest).GetMethod("AFineAndPrivatePlace", BindingFlags.Static | BindingFlags.NonPublic);
        //        SandBoxer.Assert(mInfo == null, "Did not expect private reflection to succeed");
        //    }
        //    catch (SecurityException)
        //    {
        //    }
        // }
        //private static void AFineAndPrivatePlace() { }

        [TestMethod]
        [Description("TypeDescriptor.AddProvider should fail")]
        public void DomainServiceDescription_MediumTrust_TdpRegistrationFails()
        {
            SandBoxer.ExecuteInMediumTrust(Callback_TdpRegistrationFail);
        }

        public static void Callback_TdpRegistrationFail()
        {
            try
            {
                Mock_CG_SimpleEntity entity = new Mock_CG_SimpleEntity();
                RegisterProvider(entity);
                Assert.Fail("Expected TDP registration to throw SecurityException");
            }
            catch (SecurityException)
            {
            }
        }

        private static void RegisterProvider(object instance)
        {
            TypeDescriptor.AddProvider(TypeDescriptor.GetProvider(instance), instance);
        }

        [TestMethod]
        [Description("Verifies DomainServiceDescription.GetDescription works in medium trust")]
        public void DomainServiceDescription_MediumTrust_GetDescription()
        {
            SandBoxer.ExecuteInMediumTrust(Callback_BasicDescriptionSucceeds);
        }

        public static void Callback_BasicDescriptionSucceeds()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(DomainService_Basic));
            Assert.IsNotNull(dsd, "Expected nominal DomainServiceDescription to return non-null instance");
        }
#endif

        [TestMethod]
        [Description("DomainServiceDescription's internal cache should be thread-safe")]
        public void DomainServiceDescription_MultipleThreads()
        {
            const int numberOfThreads = 10;
            Semaphore s = new Semaphore(0, numberOfThreads);
            Exception lastError = null;

            Type t = typeof(DP_Entity_DummyForMultipleThreadTest_DomainService);
            for (int i = 0; i < numberOfThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        DomainServiceDescription.GetDescription(t);
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                    }
                    finally
                    {
                        s.Release();
                    }
                });
            }

            for (int i = 0; i < numberOfThreads; i++)
            {
                s.WaitOne(TimeSpan.FromSeconds(5));
            }

            if (lastError != null)
            {
                Assert.Fail(lastError.ToString());
            }
        }

        public class DP_Entity_DummyForMultipleThreadTest_DomainService : GenericDomainService<DP_Entity_DummyForMultipleThreadTest> { }

        public class DP_Entity_DummyForMultipleThreadTest
        {
            [Key]
            public string KeyProperty { get; set; }
        }

        [TestMethod]
        [Description("Entity types must be public")]
        public void DomainServiceDescription_Entity_Fail_Not_Public()
        {
            Type t = typeof(DP_Entity_Not_Public_DomainService);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc = DomainServiceDescription.GetDescription(t);
            }, "Type 'DP_Entity_Not_Public' is not a valid entity type.  Entity types must be public.");
        }

        internal class DP_Entity_MultipleVersionMembers_DomainService : GenericDomainService<DP_Entity_MultipleVersionMembers> { }

        public class DP_Entity_MultipleVersionMembers
        {
            [Key]
            public int ID { get; set; }

            [Timestamp]
            [ConcurrencyCheck]
            public byte[] Version1 { get; set; }

            [Timestamp]
            [ConcurrencyCheck]
            public byte[] Version2 { get; set; }
        }

        internal class DP_Entity_Not_Public_DomainService : GenericDomainService<DP_Entity_Not_Public> { }

        internal class DP_Entity_Not_Public
        {
            [Key]
            public string KeyProperty { get; set; }
        }

        [TestMethod]
        [Description("Entity types cannot be abstract unless they specify a concrete base")]
        public void DomainServiceDescription_Entity_Fail_Abstract()
        {
            Type t = typeof(DP_Entity_Abstract_DomainService);
            string errorMessage = string.Format(CultureInfo.CurrentCulture, Resource.KnownTypeAttributeRequired_Abstract, typeof(DP_Entity_Abstract).Name, typeof(DP_Entity_Abstract_DomainService).Name);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc = DomainServiceDescription.GetDescription(t);
            }, errorMessage);
        }

        public class DP_Entity_Abstract_DomainService : GenericDomainService<DP_Entity_Abstract> { }

        public abstract class DP_Entity_Abstract
        {
            [Key]
            public string KeyProperty { get; set; }
        }

        [TestMethod]
        [Description("Entity types cannot be generic")]
        public void DomainServiceDescription_Entity_Fail_Generic()
        {
            Type t = typeof(DP_Entity_Generic_String_DomainService);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc = DomainServiceDescription.GetDescription(t);
            }, "Type 'DP_Entity_Generic`1' is not a valid entity type.  Entity types cannot be generic.");
        }

        public class DP_Entity_Generic_String_DomainService : GenericDomainService<DP_Entity_Generic<string>> { }

        public class DP_Entity_Generic<T>
        {
            [Key]
            public string KeyProperty { get; set; }
        }

        [TestMethod]
        [Description("Entity types cannot be nullable")]
        public void DomainServiceDescription_Entity_Fail_Nullable()
        {
            Type t = typeof(NullableDomainService);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc = DomainServiceDescription.GetDescription(t);
            }, "Type 'Nullable`1' is not a valid entity type.  Entity types cannot be nullable.");
        }

        [TestMethod]
        [Description("Entity types cannot be primitive")]
        public void DomainServiceDescription_Entity_Fail_Primitive()
        {
            Type t = typeof(String_DomainService);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc = DomainServiceDescription.GetDescription(t);
            }, "Type 'String' is not a valid entity type.  Entity types cannot be a primitive type or a simple type like string or Guid.");
        }

        [TestMethod]
        [Description("Entity types cannot be collections")]
        public void DomainServiceDescription_Entity_Fail_Collection()
        {
            Type t = typeof(DP_Entity_Collection_List_DomainService);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc = DomainServiceDescription.GetDescription(t);
            }, "Type 'List`1' is not a valid entity type.  Entity types cannot be generic.");
        }

        [TestMethod]
        [Description("Entity types have updatable/deletable member that participate in concurrency")]
        public void DomainServiceDescription_Entity_Fail_Concurrency()
        {
            Type t1 = typeof(Mock_CG_Entity_Excluded_Concurrency_Property_DomainService);
            DomainServiceDescription desc1 = DomainServiceDescription.GetDescription(t1);

            Type t2 = typeof(Mock_CG_Updatable_Entity_Excluded_Concurrency_Property_DomainService);
            string errorMessage = String.Format(CultureInfo.CurrentCulture, Resource.Invalid_Exclude_Property_Concurency_Conflict, typeof(Mock_CG_Entity_Excluded_Concurrency_Property), "ConcurrencyProperty");
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc2 = DomainServiceDescription.GetDescription(t2);
            }, errorMessage);

            Type t3 = typeof(Mock_CG_Custom_Entity_Excluded_Concurrency_Property_DomainService);
            errorMessage = String.Format(CultureInfo.CurrentCulture, Resource.Invalid_Exclude_Property_Concurency_Conflict, typeof(Mock_CG_Entity_Excluded_Concurrency_Property), "ConcurrencyProperty");
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc3 = DomainServiceDescription.GetDescription(t3);
            }, errorMessage);
        }

        public class NullableDomainService : GenericDomainService<int?> { }

        public class DP_Entity_Collection_List_DomainService : GenericDomainService<List<DP_Entity_Collection>> { }

        public struct DP_Entity_Collection
        {
            [Key]
            public string KeyProperty { get; set; }
        }

        [TestMethod]
        public void Select_ComplexArgs()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(SelectMethod_InvalidProvider_ComnplexParams));
            }, String.Format(Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "GetCounties", "prototype"));
        }

        [TestMethod]
        public void DomainMethod_ComplexArgs()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DomainMethod_InvalidProvider_ComnplexParams));
            }, String.Format(Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "LinkTo", "prototype"));
        }

        [TestMethod]
        public void InvalidUpdateMethodSignatures()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(UpdateMethod_InvalidProvider_VoidInput));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "UpdateCounty1"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(UpdateMethod_InvalidProvider_TooManyParams));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "UpdateCounty2"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(UpdateMethod_InvalidProvider_ByRefParam));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeByVal, "UpdateCounty3", "county1"));
        }

        [TestMethod]
        public void InvalidDeleteMethodSignatures()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DeleteMethod_InvalidProvider_VoidInput));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "DeleteCounty1"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DeleteMethod_InvalidProvider_TooManyParams));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "DeleteCounty2"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(DeleteMethod_InvalidProvider_ByRefParam));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeByVal, "DeleteCounty3", "county1"));
        }

        [TestMethod]
        public void InvalidInsertMethodSignatures()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InsertMethod_InvalidProvider_VoidInput));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "InsertCounty1"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InsertMethod_InvalidProvider_TooManyParams));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "InsertCounty2"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InsertMethod_InvalidProvider_ByRefParam));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntry_ParamMustBeByVal, "InsertCounty3", "county1"));
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
        public void ProjectionInclude_InvalidProjections()
        {
            DomainServiceDescription desc = null;

            Exception expectedException = null;
            try
            {
                desc = DomainServiceDescription.GetDescription(typeof(InvalidProjections_EmptyPath_DomainService));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.IsTrue(expectedException.Message.Contains(Resource.InvalidMemberProjection_EmptyPath));

            expectedException = null;
            try
            {
                desc = DomainServiceDescription.GetDescription(typeof(InvalidProjections_EmptyMemberName_DomainService));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.IsTrue(expectedException.Message.Contains(Resource.InvalidMemberProjection_EmptyMemberName));

            expectedException = null;
            try
            {
                desc = DomainServiceDescription.GetDescription(typeof(InvalidProjections_InvalidPath1_DomainService));
            }
            catch (ArgumentException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.InvalidMemberProjection_Path, "B.C.M.DNE", typeof(IncludesA), "Ref"), expectedException.Message);

            expectedException = null;
            try
            {
                desc = DomainServiceDescription.GetDescription(typeof(InvalidProjections_InvalidPath2_DomainService));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.InvalidMemberProjection_InvalidProjectedType, typeof(IncludesD), "P3", typeof(IncludesA), "Ref"), expectedException.Message);

            // try to project a non-public type
            expectedException = null;
            try
            {
                desc = DomainServiceDescription.GetDescription(typeof(InvalidProjections_InvalidPath3_DomainService));
            }
            catch (ArgumentException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(Resource.InvalidMemberProjection_Path, "B.C.P2", typeof(IncludesA), "Ref"), expectedException.Message);
        }

        [TestMethod]
        public void TestDomainOperationEntry_OverloadsNotSupported()
        {
            InvalidOperationException expectedException = null;
            try
            {
                DomainServiceDescription.GetDescription(typeof(InvalidProvider_MethodOverloads));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(Resource.DomainOperationEntryOverload_NotSupported, "GetCities"), expectedException.Message);
        }

        [TestMethod]
        public void TestDomainOperationEntry_InvalidReturnTypes()
        {
            InvalidOperationException expectedException = null;
            try
            {
                DomainServiceDescription.GetDescription(typeof(InvalidProvider_InvalidDomainOperationEntryReturnType_IEnumerable));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(Resource.InvalidDomainOperationEntry_InvalidQueryOperationReturnType, typeof(IEnumerable), "GetCities"), expectedException.Message);

            expectedException = null;
            try
            {
                DomainServiceDescription.GetDescription(typeof(InvalidProvider_InvalidDomainOperationEntryReturnType_Int));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.Invalid_Entity_Type, typeof(int).Name, Resource.EntityTypes_Cannot_Be_Primitives), expectedException.Message);
        }

        [TestMethod]
        public void TestCitiesDomainServiceDescription()
        {
            ValidateMetadata(typeof(CityDomainService), CityEntities);

        }

        [TestMethod]
        public void TestLinqToSqlDomainServiceDescription()
        {
            ValidateMetadata(typeof(TestDomainServices.LTS.Catalog), CatalogEntities);
        }

        [TestMethod]
        public void TestLinqToEntitiesDomainServiceDescription()
        {
            ValidateMetadata(typeof(TestDomainServices.EF.Catalog), CatalogEntities);
        }

        [TestMethod]
        [Description("This test makes sure we can obtain an error message for invalid LinqToSqlDomainServiceDescriptionProviderAttribute")]
        public void TestLinqToSqlInvalidDescriptionProvider()
        {
            ExceptionHelper.ExpectInvalidOperationException(() =>
                {
                    ValidateMetadata(typeof(InvalidLinqToSqlDomainServiceDescriptionProviderDS), CatalogEntities);
                },
                string.Format(OpenRiaServices.DomainServices.LinqToSql.Resource.InvalidLinqToSqlDomainServiceDescriptionProviderSpecification,
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
                                                           string.Format(OpenRiaServices.DomainServices.EntityFramework.Resource.InvalidLinqToEntitiesDomainServiceDescriptionProviderSpecification,
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
        [Description("This test makes sure we can obtain a description without instantiating L2E DomainService or its ObjectContext")]
        public void TestLinqToEntitiesContextNotInstantiated()
        {
            ValidateMetadata(typeof(ThrowingDomainServiceL2E), CatalogEntities);
        }

        [TestMethod]
        public void TestAssociationExtensionAttributes_LTS()
        {
            VerifyAdventureWorksAssociations(typeof(TestDomainServices.LTS.Catalog));
        }

        [TestMethod]
        public void TestAssociationExtensionAttributes_EF()
        {
            VerifyAdventureWorksAssociations(typeof(TestDomainServices.EF.Catalog));
        }

        private void VerifyAdventureWorksAssociations(Type providerType)
        {
            DomainServiceDescription d = DomainServiceDescription.GetDescription(providerType);

            #region Verify bidirectional PurchaseOrder/PurchaseOrderDetail association
            // verify PurchaseOrder.PurchaseOrderDetails association
            Type entityType = d.EntityTypes.Single(p => p.Name == "PurchaseOrder");
            AssociationAttribute expected = new AssociationAttribute(
                "PurchaseOrder_PurchaseOrderDetail",
                "PurchaseOrderID",
                "PurchaseOrderID"
            );
            expected.IsForeignKey = false;
            ValidateAssociationAttribute(entityType, "PurchaseOrderDetails", expected);

            // verify PurchaseOrderDetail.PurchaseOrder association
            entityType = d.EntityTypes.Single(p => p.Name == "PurchaseOrderDetail");
            expected = new AssociationAttribute(
                "PurchaseOrder_PurchaseOrderDetail",
                "PurchaseOrderID",
                "PurchaseOrderID"
            );
            expected.IsForeignKey = true;
            ValidateAssociationAttribute(entityType, "PurchaseOrder", expected);
            #endregion

            #region Verify bidirectional Product/PurchaseOrderDetail association
            // verify Product.PurchaseOrderDetails association
            entityType = d.EntityTypes.Single(p => p.Name == "Product");
            expected = new AssociationAttribute(
                "Product_PurchaseOrderDetail",
                "ProductID",
                "ProductID"
            );
            expected.IsForeignKey = false;
            ValidateAssociationAttribute(entityType, "PurchaseOrderDetails", expected);

            // verify PurchaseOrderDetail.Product association
            entityType = d.EntityTypes.Single(p => p.Name == "PurchaseOrderDetail");
            expected = new AssociationAttribute(
                "Product_PurchaseOrderDetail",
                "ProductID",
                "ProductID"
            );
            expected.IsForeignKey = true;
            ValidateAssociationAttribute(entityType, "Product", expected);
            #endregion
        }

        private void ValidateAssociationAttribute(Type entityType, string associationMember, AssociationAttribute expected)
        {
            IEnumerable<PropertyDescriptor> properties = TypeDescriptor.GetProperties(entityType).Cast<PropertyDescriptor>();
            PropertyDescriptor pd = properties.SingleOrDefault(p => p.Name == associationMember);
            Assert.IsNotNull(pd, "Entity type " + entityType.Name + " did not have a property descriptor '" + associationMember + "'");
            AttributeCollection attributes = pd.Attributes;
            AssociationAttribute assoc = attributes.OfType<AssociationAttribute>().SingleOrDefault();
            Assert.IsNotNull(assoc, "Entity type " + entityType.Name + " did not have an AssociationAttribute");
            Assert.AreEqual(expected.Name, assoc.Name);
            Assert.AreEqual(expected.ThisKey, assoc.ThisKey);
            Assert.AreEqual(expected.OtherKey, assoc.OtherKey);
            Assert.AreEqual(expected.IsForeignKey, assoc.IsForeignKey);
        }

        [TestMethod]
        public void TestEntityTypeDiscovery()
        {
            DomainServiceDescription d = DomainServiceDescription.GetDescription(typeof(TestGraphProvider));
            Assert.AreEqual(5, d.EntityTypes.Count());
            Type[] expectedTypes = new Type[] { typeof(A), typeof(B), typeof(C), typeof(D), typeof(E) };
            Assert.IsTrue(d.EntityTypes.OrderBy(p => p.Name).SequenceEqual(expectedTypes));
        }

        [TestMethod]
        [Description("Tests the UpdateAttribute.UsingCustomMethod getter and setter.")]
        public void UpdateAttributeUsingCustomMethod()
        {
            DomainServiceDescription d = DomainServiceDescription.GetDescription(typeof(UpdateDomainService));
            Assert.IsNotNull(d.DomainOperationEntries.Single(doe => (doe.Operation == DomainOperation.Update) && (doe.Name == "UpdateA")),
                "There should be 1 update operation named UpdateA.");
            Assert.IsNotNull(d.DomainOperationEntries.Single(doe => (doe.Operation == DomainOperation.Custom) && (doe.Name == "CustomUpdateA")),
                "There should be 1 custom update operation named CustomUpdateA.");
        }

        [TestMethod]
        [WorkItem(208055)]
        [Description("Verifies that MetadataTypeAttribute with cyclic or self reference throws.")]
        public void MetadataTypeAttributeCycleTest()
        {
            DomainServiceDescription desc = null;
            Exception expectedException = null;
            string exceptionMsg = null;
            try
            {
                desc = DomainServiceDescription.GetDescription(typeof(TestDomainServices.MetadataTypeAttributeCycleTestDomainService1));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            exceptionMsg = string.Format(Resource.CyclicMetadataTypeAttributesFound, typeof(TestDomainServices.EntityWithCyclicMetadataTypeAttributeA).FullName); 
            Assert.AreEqual(exceptionMsg, expectedException.Message);

            expectedException = null;
            try
            {
                desc = DomainServiceDescription.GetDescription(typeof(TestDomainServices.MetadataTypeAttributeCycleTestDomainService2));
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            exceptionMsg= string.Format(Resource.CyclicMetadataTypeAttributesFound, typeof(TestDomainServices.EntityWithSelfReferencingcMetadataTypeAttribute).FullName);
            Assert.AreEqual(exceptionMsg, expectedException.Message);
        }

        [TestMethod]
        [WorkItem(791391)]
        [Description("Entity types that derive from types marked with [DataContract] must be marked with [DataContract]")]
        public void DomainServiceDescription_Entity_Fail_MissingDataContractAttribute()
        {
            Type t = typeof(DP_Entity_MissingDataContractAttribute_DomainService);
            string errorMessage = string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceDescription_DataContractAttributeRequired, typeof(DP_Entity_MissingDataContractAttribute).Name, typeof(DP_Entity_WithDataContractAttribute).Name);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription desc = DomainServiceDescription.GetDescription(t);
            }, errorMessage);
        }

        public class DP_Entity_MissingDataContractAttribute_DomainService : GenericDomainService<DP_Entity_MissingDataContractAttribute> { }

        public class DP_Entity_MissingDataContractAttribute : DP_Entity_WithDataContractAttribute
        {
        }

        [DataContract]
        public class DP_Entity_WithDataContractAttribute
        {
            [Key]
            [DataMember]
            public string KeyProperty { get; set; }
        }

        private static void ValidateMetadata(Type domainServiceType, EntityMetadata[] metadata)
        {
            DomainServiceDescription desc = DomainServiceDescription.GetDescription(domainServiceType);
            Assert.IsNotNull(desc, "Could not get DomainServiceDescription for " + domainServiceType.Name);
            IEnumerable<Type> entityTypes = desc.EntityTypes;
            Assert.IsNotNull(entityTypes);
            foreach (EntityMetadata em in metadata)
            {
                System.Diagnostics.Debug.WriteLine("Validating entity " + domainServiceType.Name + "." + em.Name);

                Type foundEntityType = null;
                foreach (Type entityType in entityTypes)
                {
                    if (entityType.Name.Equals(em.Name))
                    {
                        foundEntityType = entityType;
                        break;
                    }
                }
                Assert.IsNotNull(foundEntityType, "Could not find entity type " + em.Name + " in " + domainServiceType.Name);
                ValidateEntity(domainServiceType, em, foundEntityType);
            }

        }
        private static void ValidateEntity(Type domainServiceType, EntityMetadata metadata, Type entityType)
        {
            PropertyDescriptorCollection pds = TypeDescriptor.GetProperties(entityType);
            foreach (PropertyMetadata pm in metadata.Properties)
            {
                System.Diagnostics.Debug.WriteLine("  validating member " + pm.Name);

                PropertyDescriptor foundDescriptor = null;
                foreach (PropertyDescriptor pd in pds)
                {
                    if (pd.Name.Equals(pm.Name))
                    {
                        foundDescriptor = pd;
                        break;
                    }
                }
                Assert.IsNotNull(foundDescriptor, "Could not find property " + pm.Name + " in " + domainServiceType.Name);
                ValidateEntityMember(domainServiceType, entityType, pm, foundDescriptor);
            }
        }

        private static void ValidateEntityMember(Type domainServiceType, Type entityType, PropertyMetadata propertyMetadata, PropertyDescriptor pd)
        {
            foreach (AttributeMetadata am in propertyMetadata.Attributes)
            {
                Attribute foundAttribute = null;
                System.Diagnostics.Debug.WriteLine("    validating [" + am.Name + "]");

                foreach (Attribute attr in pd.Attributes)
                {
                    if (am.Name.Equals(attr.GetType().Name))
                    {
                        foundAttribute = attr;
                        break;
                    }
                }
                Assert.IsNotNull(foundAttribute, "Could not find attribute " + am.Name + " in property " + pd.Name + " in " + domainServiceType.Name);
            }
        }

        #region Negative association validation tests
        [TestMethod]
        public void InvalidAssociationTest_NullAssociationName()
        {
            ExceptionHelper.ExpectArgumentException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_NullName.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_NameCannotBeNullOrEmpty, "B", "InvalidAssociation_NullName.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_EmptyThisKey()
        {
            ExceptionHelper.ExpectArgumentException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_EmptyThisKey.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_StringCannotBeNullOrEmpty, "A_B", "InvalidAssociation_EmptyThisKey.A", "ThisKey"));
        }

        [TestMethod]
        public void InvalidAssociationTest_NullOtherKey()
        {
            ExceptionHelper.ExpectArgumentException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_NullOtherKey.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_StringCannotBeNullOrEmpty, "A_B", "InvalidAssociation_NullOtherKey.A", "OtherKey"));
        }

        [TestMethod]
        public void InvalidAssociationTest_NonExistentThisKey()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_ThisKeyNotFound.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_ThisKeyNotFound, "A_B", "InvalidAssociation_ThisKeyNotFound.A", "NonExistentKey"));
        }


        [TestMethod]
        [Description("The number of keys named in an association must match between 'this' and 'other'")]
        public void InvalidAssociationTest_KeyCountMismatch()
        {
            // Too few on "other"
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_KeyCountMismatch.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_Key_Count_Mismatch, "A_B", "InvalidAssociation_KeyCountMismatch.A", "ID1,ID2", "B_ID"));

            // Too few on "this"
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_KeyCountMismatch_Reverse.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_Key_Count_Mismatch, "A_B", "InvalidAssociation_KeyCountMismatch_Reverse.A", "A_ID", "B_ID1,B_ID2"));
        }


        [TestMethod]
        public void InvalidAssociationTest_DupNameSelfRef()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_DupName_SelfRef.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_NonUniqueAssociationName, "A_A", "InvalidAssociation_DupName_SelfRef.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_DupNameNonSelfRef()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_DupName_NonSelfRef.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_NonUniqueAssociationName, "A_B", "InvalidAssociation_DupName_NonSelfRef.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_OtherKeyNotFoundSelfRef()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_OtherKeyNotFound_SelfRef.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_OtherKeyNotFound, "A_A", "InvalidAssociation_OtherKeyNotFound_SelfRef.A", "NonExistentKey", "InvalidAssociation_OtherKeyNotFound_SelfRef.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_OtherKeyNotFoundNonSelfRef()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_OtherKeyNotFound_NonSelfRef.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_OtherKeyNotFound, "A_B", "InvalidAssociation_OtherKeyNotFound_NonSelfRef.A", "NonExistentKey", "InvalidAssociation_OtherKeyNotFound_NonSelfRef.B"));
        }

        [TestMethod]
        public void InvalidAssociationTest_InvalidIsFKSelfRef()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_InvalidIsFK_SelfRef.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_IsFKInvalid, "A_A", "InvalidAssociation_InvalidIsFK_SelfRef.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_InvalidIsFKNonSelfRef()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_InvalidIsFK_NonSelfRef.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_IsFKInvalid, "A_B", "InvalidAssociation_InvalidIsFK_NonSelfRef.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_RoundTripOriginal()
        {
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_RoundTripOriginal.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_RoundTripOriginal, "B", "InvalidAssociation_RoundTripOriginal.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_FKNotSingleton()
        {
            ExceptionHelper.ExpectInvalidOperationException(() =>
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_FKNotSingleton.InvalidAssociationDomainService)),
                string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_FKNotSingleton, "A_B", "InvalidAssociation_FKNotSingleton.A"));
        }

        [TestMethod]
        public void InvalidAssociationTest_OtherFKNotSingleton()
        {
            ExceptionHelper.ExpectInvalidOperationException(() =>
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_OtherFKNotSingleton.InvalidAssociationDomainService)),
                string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_FKNotSingleton, "A_B", "InvalidAssociation_OtherFKNotSingleton.B"));
        }

        [TestMethod]
        public void InvalidAssociationTest_TypesDoNotAlign_OneToOne()
        {
            ExceptionHelper.ExpectInvalidOperationException(() =>
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_TypesDoNotAlign_OneToOne.InvalidAssociationDomainService)),
                string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_TypesDoNotAlign, "A_B", "InvalidAssociation_TypesDoNotAlign_OneToOne.A", "InvalidAssociation_TypesDoNotAlign_OneToOne.B"));
        }

        [TestMethod]
        public void InvalidAssociationTest_TypesDoNotAlign_OneToMany()
        {
            ExceptionHelper.ExpectInvalidOperationException(() =>
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_TypesDoNotAlign_OneToMany.InvalidAssociationDomainService)),
                string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_TypesDoNotAlign, "A_B", "InvalidAssociation_TypesDoNotAlign_OneToMany.A", "InvalidAssociation_TypesDoNotAlign_OneToMany.B"));
        }

        #endregion

        #region Association validations when other entity type is not exposed to the client
        [TestMethod]
        public void InvalidAssociationTest_DupNameNonSelfRef_OtherTypeNotExposed()
        {
            // this test is similar to the DupNameNonSelfRef test, but type B is not exposed to the client. This is to ensure that
            // we still validate no duplicate association names on this entity
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                DomainServiceDescription.GetDescription(typeof(InvalidAssociation_DupName_NonSelfRefNonIncluded.InvalidAssociationDomainService));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidAssociation_NonUniqueAssociationName, "A_B", "InvalidAssociation_DupName_NonSelfRefNonIncluded.A"));
        }

        [TestMethod]
        public void ValidAssociationTest_InvalidIsFK_OtherTypeNotExposed()
        {
            DomainServiceDescription d = DomainServiceDescription.GetDescription(typeof(ValidAssociation_InvalidIsFK_OtherTypeNotExposed.ValidAssociationDomainService));
            Assert.IsTrue(d.EntityTypes.Contains(typeof(ValidAssociation_InvalidIsFK_OtherTypeNotExposed.A)));
            Assert.IsFalse(d.EntityTypes.Contains(typeof(ValidAssociation_InvalidIsFK_OtherTypeNotExposed.B)));

            Type A = d.EntityTypes.Single(t => t.Name == "A");
            Assert.IsTrue(TypeDescriptor.GetProperties(A)["B"].Attributes.OfType<AssociationAttribute>().Any());
        }

        [TestMethod]
        public void ValidAssociationTest_OtherKeyNotFound_OtherTypeNotExposed()
        {
            DomainServiceDescription d = DomainServiceDescription.GetDescription(typeof(ValidAssociation_OtherKeyNotFound_OtherTypeNotExposed.ValidAssociationDomainService));
            Assert.IsTrue(d.EntityTypes.Contains(typeof(ValidAssociation_OtherKeyNotFound_OtherTypeNotExposed.A)));
            Assert.IsFalse(d.EntityTypes.Contains(typeof(ValidAssociation_OtherKeyNotFound_OtherTypeNotExposed.B)));

            Type A = d.EntityTypes.Single(t => t.Name == "A");
            Assert.IsTrue(TypeDescriptor.GetProperties(A)["B"].Attributes.OfType<AssociationAttribute>().Any());
        }
        #endregion

        [TestMethod]
        public void ValidAssociationTest_InheritanceSelfRef()
        {
            DomainServiceDescription d = DomainServiceDescription.GetDescription(typeof(ValidAssociation_InheritanceSelfRef.ValidAssociationDomainService));
            Assert.IsTrue(d.EntityTypes.Contains(typeof(ValidAssociation_InheritanceSelfRef.A)));
            Assert.IsTrue(d.EntityTypes.Contains(typeof(ValidAssociation_InheritanceSelfRef.B)));

            PropertyDescriptorCollection a_properties = TypeDescriptor.GetProperties(d.EntityTypes.Single(t => t.Name == "A"));
            Assert.IsNotNull(a_properties["A1"].Attributes.OfType<AssociationAttribute>().Single());
            Assert.IsNotNull(a_properties["A2"].Attributes.OfType<AssociationAttribute>().Single());
            Assert.IsNotNull(a_properties["B"].Attributes.OfType<AssociationAttribute>().Single());

            PropertyDescriptorCollection b_properties = TypeDescriptor.GetProperties(d.EntityTypes.Single(t => t.Name == "B"));
            Assert.IsNotNull(b_properties["A1"].Attributes.OfType<AssociationAttribute>().Single());
            Assert.IsNotNull(b_properties["A2"].Attributes.OfType<AssociationAttribute>().Single());
            Assert.IsNotNull(b_properties["A"].Attributes.OfType<AssociationAttribute>().Single());
            Assert.IsNotNull(b_properties["B"].Attributes.OfType<AssociationAttribute>().Single());
            Assert.IsNotNull(b_properties["B1"].Attributes.OfType<AssociationAttribute>().Single());
            Assert.IsNotNull(b_properties["B2"].Attributes.OfType<AssociationAttribute>().Single());
        }

        [AttributeTestInheritAttribute]
        [AttributeTestNonInheritAttribute]
        public class AttributeTestTypeBase
        {
        }

        public class AttributeTestTypeDerived : AttributeTestTypeBase
        {
        }

        [AttributeUsage(AttributeTargets.Class, Inherited = true)]
        public class AttributeTestInheritAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public class AttributeTestNonInheritAttribute : Attribute
        {
        }

        [MetadataType(typeof(AttributeTestMetadata))]
        public class AttributeTest
        {
            public AttributeTest A
            {
                get;
                set;
            }
        }

        [DisplayColumn("A")]
        public class AttributeTestMetadata
        {
            [Editable(false)]
            public static object A;
        }

        #region Test data

        private static EntityMetadata[] CityEntities = {

            new EntityMetadata() {
                Name="City", 
                Properties=new PropertyMetadata[] {
                  new PropertyMetadata() {Name="Name", Attributes= new AttributeMetadata[] {
                      new AttributeMetadata {Name="KeyAttribute"}}
                  },
                  new PropertyMetadata() {Name="CountyName", Attributes=new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute" }
                    }
                  }, 
                  new PropertyMetadata() {Name="StateName", Attributes=new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute" }
                    }
                  }
                }
             },

            new EntityMetadata() {
                Name="County", 
                Properties=new PropertyMetadata[] {
                  new PropertyMetadata() {Name="Name", Attributes= new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute"}
                    }
                  },
                  new PropertyMetadata() {Name="StateName", Attributes=new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute" }
                    }
                  }
                }
            },

             new EntityMetadata() {
                Name="State", 
                Properties=new PropertyMetadata[] {
                  new PropertyMetadata() {Name="Name", Attributes= new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute"}
                    }
                  },
                  new PropertyMetadata() {Name="FullName", Attributes=new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute" }
                    }
                  }
                },
             },
              
             new EntityMetadata() {
                Name="Zip", 
                Properties=new PropertyMetadata[] 
                { new PropertyMetadata() {Name="Code", Attributes= new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute"}
                    }
                  },
                  new PropertyMetadata() {Name="FourDigit", Attributes=new AttributeMetadata[] {
                          new AttributeMetadata {Name="KeyAttribute" }
                    }
                  },
                    
                  new PropertyMetadata() {Name="CityName", Attributes=new AttributeMetadata[0] },
                  new PropertyMetadata() {Name="CountyName", Attributes=new AttributeMetadata[0] },
                  new PropertyMetadata() {Name="StateName", Attributes=new AttributeMetadata[0] }
                }
              }
          };


        private static EntityMetadata[] CatalogEntities = 
        {
            new EntityMetadata() {
                Name="Product", 
                Properties=new PropertyMetadata[] 
                { new PropertyMetadata() {Name="ProductID", Attributes= new AttributeMetadata[]
                        { new AttributeMetadata {Name="KeyAttribute"} } },
                  new PropertyMetadata() {Name="Weight", Attributes= new AttributeMetadata[]          // Nullable field
                        {  } },
                  new PropertyMetadata() {Name="Size", Attributes= new AttributeMetadata[]            // string field
                        {  } },
                  new PropertyMetadata() {Name="ModifiedDate", Attributes= new AttributeMetadata[]    // DateTime field
                        {  } },
                  new PropertyMetadata() {Name="ListPrice", Attributes= new AttributeMetadata[]       // Decimal field
                        {  } },


                }
            },
            new EntityMetadata() {
                Name="PurchaseOrder", 
                Properties=new PropertyMetadata[] 
                { new PropertyMetadata() {Name="PurchaseOrderID", Attributes= new AttributeMetadata[]
                        { new AttributeMetadata {Name="KeyAttribute"} } },
                }
            },
            new EntityMetadata() {
                Name="PurchaseOrderDetail", 
                Properties=new PropertyMetadata[] 
                { new PropertyMetadata() {Name="PurchaseOrderID", Attributes= new AttributeMetadata[]
                        { new AttributeMetadata {Name="KeyAttribute"},
                           } },
                  new PropertyMetadata() {Name="PurchaseOrderDetailID", Attributes= new AttributeMetadata[]
                        { new AttributeMetadata {Name="KeyAttribute"},
                           } },
                  new PropertyMetadata() {Name="ProductID", Attributes= new AttributeMetadata[]        // generated field
                        {  } },
                }
            },

        };

        [EnableClientAccess]
        public class GenericDomainService<T> : DomainService
        {
            [Query]
            public IEnumerable<T> Get()
            {
                throw new NotImplementedException();
            }
        }

        public class Projections_ValueType_DomainService : GenericDomainService<Projections_ValueType>
        {
        }

        public class Projections_ValueType
        {
            [Key]
            public int ID { get; set; }

            [StringLength(10)]
            public string P1 { get; set; }

            [ExternalReference]
            [Association("Product", "ProductID", "ProductID", IsForeignKey = true)]
            [Include("ProductID", "ProductID")]
            public NorthwindModel.Product Product { get; set; }

        }

        public class InvalidProjections_EmptyPath_DomainService : GenericDomainService<InvalidProjections_EmptyPath>
        {
        }

        public class InvalidProjections_EmptyPath
        {
            [Include("", "Invalid")]
            public IncludesA Ref
            {
                get;
                set;
            }
        }

        public class InvalidProjections_EmptyMemberName_DomainService : GenericDomainService<InvalidProjections_EmptyMemberName>
        {
        }

        public class InvalidProjections_EmptyMemberName
        {
            [Include("A.B.P1", "")]
            public IncludesA Ref
            {
                get;
                set;
            }
        }

        public class InvalidProjections_InvalidPath1_DomainService : GenericDomainService<InvalidProjections_InvalidPath1>
        {
        }

        public class InvalidProjections_InvalidPath1
        {
            [Include("B.C.M.DNE", "DNE")]
            public IncludesA Ref
            {
                get;
                set;
            }
        }

        public class InvalidProjections_InvalidPath2_DomainService : GenericDomainService<InvalidProjections_InvalidPath2>
        {
        }

        // try to project an type not supported by codegen/serialization
        public class InvalidProjections_InvalidPath2
        {
            [Include("B.C.D.P3", "P3")]
            public IncludesA Ref
            {
                get;
                set;
            }
        }

        public class InvalidProjections_InvalidPath3_DomainService : GenericDomainService<InvalidProjections_InvalidPath3>
        {
        }

        // try to project a non-public type
        public class InvalidProjections_InvalidPath3
        {
            [Include("B.C.P2", "P2")]
            public IncludesA Ref
            {
                get;
                set;
            }
        }

        // test a path cycle
        public class InvalidProjections_CyclicPath
        {
            [Include("B.C.P2", "P2")]
            public IncludesA Ref
            {
                get;
                set;
            }
        }
        #endregion

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

        #region DatabaseGeneratedAttribute Tests Helper Methods
        /// <summary>
        /// Finds if the attributeCollection has a DatabaseGeneratedAttribute and returns it.
        /// </summary>
        /// <param name="attributes">The attribute collection.</param>
        /// <returns>The DatabaseGeneratedAttribute if found, else null.</returns>
        private static Attribute GetDatabaseGeneratedAttribute(AttributeCollection attributes)
        {
            Attribute dbAttr = null;
            IEnumerator enumerator = attributes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (string.Equals(enumerator.Current.GetType().FullName, "System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute"))
                {
                    dbAttr = enumerator.Current as Attribute;
                    break;
                }
            }
            return dbAttr;
        }

        /// <summary>
        /// Compares the value of the DatabaseGeneratedOption in the DatabaseGeneratedAttribute with the expected value.
        /// </summary>
        /// <param name="dbAttr">The DatabaseGeneratedAttribute object.</param>
        /// <param name="expectedDatabaseGeneratedOption">The expected value of DatabaseGeneratedOption as a string.</param>
        /// <returns>True if the DatabaseGeneratedOption value in the attribute matches the expected value, else false.</returns>
        private static bool CompareDatabaseGeneratedOption(Attribute dbAttr, string expectedDatabaseGeneratedOption)
        {
            if (!string.Equals(dbAttr.GetType().FullName, "System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute"))
            {
                return false;
            }

            Type databaseGeneratedAttributeType = dbAttr.GetType();
            PropertyInfo databaseGeneratedOptionProperty = databaseGeneratedAttributeType.GetProperty("DatabaseGeneratedOption");

            // Get the DatabaseGeneratedOption value on the attribute.
            object attribValue = databaseGeneratedOptionProperty.GetValue(dbAttr, null);

            // Get the value of the field from the type.
            var enumValue = databaseGeneratedOptionProperty.PropertyType.GetField(expectedDatabaseGeneratedOption).GetValue(null);

            return (int)enumValue == (int)attribValue;
        }
        #endregion

        #region Singleton query method test DomainServices
        public class EnumerableSingleton : IEnumerable
        {
            #region IEnumerable Members

            public IEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [EnableClientAccess]
        public class SingletonQueryMethod_ValidScenarios : DomainService
        {
            public City GetCityByName(string name)
            {
                return null;
            }

            [Query(IsComposable = false)]
            public State GetStateByName(string name)
            {
                return null;
            }

            public EnumerableSingleton GetEnumerableSingleton()
            {
                return null;
            }
        }

        [EnableClientAccess]
        public class SingletonQueryMethod_NonComposable : DomainService
        {
            [Query]
            public IEnumerable<City> GetCities()
            {
                return null;
            }

            [Query]
            public City GetCityByName(string name)
            {
                return null;
            }
        }

        /// <summary>
        /// This service differs from the first in that it intentionally
        /// only has a single query method returning the entity Type.
        /// </summary>
        [EnableClientAccess]
        public class SingletonQueryMethod_NonComposable2 : DomainService
        {
            [Query]
            public City GetCityByName(string name)
            {
                return null;
            }
        }

        /// <summary>
        /// Service containing singleton returning methods that shouldn't be inferred
        /// as singleton query methods
        /// </summary>
        [EnableClientAccess]
        public class SingletonQueryMethod_FalsePositives : DomainService
        {
            public int GetFoos(string bar)
            {
                return 0;
            }

            public DateTime GetDateTime(int a, int b)
            {
                return DateTime.Now;
            }

            public Type GetAType()
            {
                return null;
            }
        }
        #endregion
    }

    [EnableClientAccess]
    public class EFComplexTypesService : LinqToEntitiesDomainService<DataTests.Scenarios.EF.Northwind.NorthwindEntities_Scenarios>
    {
        public IQueryable<DataTests.Scenarios.EF.Northwind.EmployeeWithCT> GetCustomers()
        {
            return null;
        }
    }

    public class LTSExternalMappingService : LinqToSqlDomainService<DataTests.Scenarios.LTS.Northwind_ExternalMapping.Northwind>
    {
        public IQueryable<DataTests.Scenarios.LTS.Northwind_ExternalMapping.Customer> GetCustomers()
        {
            return null;
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

    public class Bug843965_Service : LinqToSqlDomainService<DataTests.Scenarios.LTS.Northwind.NorthwindScenarios>
    {
        public IQueryable<DataTests.Scenarios.LTS.Northwind.Bug843965_A> GetBug843965_As()
        {
            return null;
        }
    }

    public class Bug846250_Service : LinqToEntitiesDomainService<NorthwindModel.NorthwindEntities>
    {
        public IQueryable<SpecialOrder> GetSpecialOrders()
        {
            return null;
        }
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

    public class EFPocoEntity_IEntityChangeTracker : System.Data.Entity.Core.Objects.DataClasses.IEntityChangeTracker
    {
        public void EntityComplexMemberChanged(string entityMemberName, object complexObject, string complexObjectMemberName)
        {
            throw new NotImplementedException();
        }

        public void EntityComplexMemberChanging(string entityMemberName, object complexObject, string complexObjectMemberName)
        {
            throw new NotImplementedException();
        }

        public void EntityMemberChanged(string entityMemberName)
        {
            throw new NotImplementedException();
        }

        public void EntityMemberChanging(string entityMemberName)
        {
            throw new NotImplementedException();
        }

        public EntityState EntityState
        {
            get { throw new NotImplementedException(); }
        }
    }

    #region DomainServiceDescriptionProvider samples
    /// <summary>
    /// Example provider showing how a new description can be constructed which adds
    /// additional operations to the base description.
    /// </summary>
    internal class DescriptionProviderA : DomainServiceDescriptionProvider
    {
        private DomainServiceDescriptionProvider parentProvider;

        public DescriptionProviderA(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            this.parentProvider = parent;
        }

        public override DomainServiceDescription GetDescription()
        {
            DomainServiceDescription description = base.GetDescription();

            description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, "A1", DomainOperation.Query, typeof(IEnumerable<County>), new DomainOperationParameter[0], AttributeCollection.Empty));
            description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, "A2", DomainOperation.Query, typeof(IEnumerable<County>), new DomainOperationParameter[0], AttributeCollection.Empty));

            return description;
        }
    }

    /// <summary>
    /// Example provider showing how a new description can be constructed which adds
    /// additional operations to the base description.
    /// </summary>
    internal sealed class DescriptionProviderB : DomainServiceDescriptionProvider
    {
        private DomainServiceDescriptionProvider parentProvider;

        public DescriptionProviderB(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            this.parentProvider = parent;
        }

        public override DomainServiceDescription GetDescription()
        {
            DomainServiceDescription description = base.GetDescription();

            description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, "B1", DomainOperation.Query, typeof(IEnumerable<County>), new DomainOperationParameter[0], AttributeCollection.Empty));
            description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, "B2", DomainOperation.Query, typeof(IEnumerable<County>), new DomainOperationParameter[0], AttributeCollection.Empty));

            return description;
        }
    }

    /// <summary>
    /// Example provider showing how to do the "virtual" CRUD methods scenario for fields
    /// </summary>
    internal class AutoCrudDomainServiceDescriptionProvider : DomainServiceDescriptionProvider
    {
        private DomainServiceDescriptionProvider parentProvider;

        public AutoCrudDomainServiceDescriptionProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            this.parentProvider = parent;
        }

        public override DomainServiceDescription GetDescription()
        {
            DomainServiceDescription description = base.GetDescription();

            IEnumerable<FieldInfo> cudFields = description.DomainServiceType.GetFields().Where(p => p.IsPublic && p.GetCustomAttributes(typeof(AutoCrud), true).Any());

            List<DomainOperationEntry> operations = new List<DomainOperationEntry>();
            foreach (FieldInfo cudField in cudFields)
            {
                // add a virtual insert method
                string singleParamName = cudField.FieldType.Name.Substring(0, 1).ToLower();
                DomainOperationParameter[] parameters = new DomainOperationParameter[] { new DomainOperationParameter(singleParamName, cudField.FieldType, AttributeCollection.Empty) };
                description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, string.Format("Insert{0}", cudField.FieldType.Name), DomainOperation.Insert, typeof(void), parameters, AttributeCollection.Empty));

                // add a virtual delete method
                parameters = new DomainOperationParameter[] { new DomainOperationParameter(singleParamName, cudField.FieldType, AttributeCollection.Empty) };
                description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, string.Format("Delete{0}", cudField.FieldType.Name), DomainOperation.Delete, typeof(void), parameters, AttributeCollection.Empty));

                // add a virtual update method
                parameters = new DomainOperationParameter[] { new DomainOperationParameter("curr" + cudField.FieldType.Name, cudField.FieldType, AttributeCollection.Empty) };
                description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, string.Format("Update{0}", cudField.FieldType.Name), DomainOperation.Update, typeof(void), parameters, AttributeCollection.Empty));

                // add virtual query method
                description.AddOperation(new TestDomainOperationEntry(description.DomainServiceType, string.Format("Get{0}", cudField.Name), DomainOperation.Query, typeof(IEnumerable<>).MakeGenericType(cudField.FieldType), new DomainOperationParameter[0], AttributeCollection.Empty));
            }

            return description;
        }
    }

    internal class OperationAnnotationDSDProvider : DomainServiceDescriptionProvider
    {
        private DomainServiceDescriptionProvider parentProvider;

        public OperationAnnotationDSDProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            this.parentProvider = parent;
        }

        public override AttributeCollection GetOperationAttributes(DomainOperationEntry operation)
        {
            AttributeCollection attribs = base.GetOperationAttributes(operation);

            if (operation.Name.Length % 2 == 0)
            {
                attribs = AttributeCollection.FromExisting(attribs, new TestAttributeA("Even"));
            }

            return attribs;
        }
    }

    public class TestDomainOperationEntry : DomainOperationEntry
    {
        public TestDomainOperationEntry(Type domainServiceType, string methodName, DomainOperation operationType, Type methodReturnType, IEnumerable<DomainOperationParameter> parameters, AttributeCollection methodAttributes)
            : base(domainServiceType, methodName, operationType, methodReturnType, parameters, methodAttributes)
        {
        }

        public override object Invoke(DomainService domainService, object[] parameters)
        {
            throw new NotImplementedException();
        }
    }

    internal class DescriptionProviderWithInvalidConstructor : DomainServiceDescriptionProvider
    {
        public DescriptionProviderWithInvalidConstructor(Type domainServiceType, bool x)
            : base(domainServiceType, null)
        {
        }
    }

    /// <summary>
    /// Test attribute applied to a DomainService field to mark it
    /// as a a CRUD field. All CRUD operations will then be inferred
    /// for it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoCrud : Attribute
    {
    }

    [DomainServiceDescriptionProvider(typeof(DescriptionProviderA))]
    public class DSDTestServiceA : DomainService
    {
        [AutoCrud]
        public State States;

        public IEnumerable<City> GetCities()
        {
            throw new NotImplementedException();
        }
    }

    [DomainServiceDescriptionProvider(typeof(DescriptionProviderB))]
    public class DSDTestServiceB : DSDTestServiceA
    {
        public IEnumerable<Zip> GetZips()
        {
            throw new NotImplementedException();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AutoCrudDomainServiceDescriptionProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        public AutoCrudDomainServiceDescriptionProviderAttribute() : base(typeof(AutoCrudDomainServiceDescriptionProvider)) { }
    }

    [AutoCrudDomainServiceDescriptionProviderAttribute]
    public class DSDTestService_CUDFields : DomainService
    {
        [AutoCrud]
        public City Cities;

        [AutoCrud]
        public State States;
    }

    [AutoCrudDomainServiceDescriptionProviderAttribute]
    [DomainServiceDescriptionProvider(typeof(OperationAnnotationDSDProvider))]
    public class DSDTestService_AutoCrud_ReflExtensions : DomainService
    {
        [AutoCrud]
        public City Cities;

        [AutoCrud]
        public State States;

        // verify that the reflection provider can correctly infer
        // and add a custom method for the implicit City entity.
        // For the test to be valid - do not add an explicit query method
        // for City
        public void IncreaseTaxes(City city) { }

        // verify an additional query method can be added by the refl
        // provider
        public IEnumerable<State> GetStatesByZone(int zone) { return null; }
    }

    #endregion

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

    class EntityMetadata
    {
        public string Name;
        public PropertyMetadata[] Properties;
    }
    class PropertyMetadata
    {
        public string Name;
        public AttributeMetadata[] Attributes;
    }
    class AttributeMetadata
    {
        public string Name;
    }

    #region Test Entity Graph
    /// <summary>
    /// Class hierarchy used for testing include graph cycles,
    /// entity discovery, multiple includes of same type, and 
    /// other corner cases.
    /// </summary>
    public class A
    {
        [Key]
        public int A_ID
        {
            get;
            set;
        }

        [Include]
        [Association("A_B", "A_ID", "B_ID")]
        public IEnumerable<B> Bs
        {
            get;
            set;
        }

        [Include]
        [Association("A_C", "A_ID", "C_ID")]
        public IEnumerable<C> Cs
        {
            get;
            set;
        }
    }
    public class B
    {
        [Key]
        public int B_ID
        {
            get;
            set;
        }

        public int B_ID2
        {
            get;
            set;
        }
    }
    public class C
    {
        [Key]
        public int C_ID
        {
            get;
            set;
        }

        [Include]
        [Association("C_D", "C_ID", "D_ID")]
        public IEnumerable<D> Ds
        {
            get;
            set;
        }

        [Include]
        [Association("C_E", "C_ID", "E_ID")]
        public IEnumerable<E> Es
        {
            get;
            set;
        }

        // Another association to E
        [Include]
        [Association("C_E2", "C_ID", "E_ID2")]
        public IEnumerable<E> Es2
        {
            get;
            set;
        }

        // Association not marked include
        [Association("C_F", "C_ID", "F_ID")]
        public F F
        {
            get;
            set;
        }
    }
    public class D
    {
        [Key]
        public int D_ID
        {
            get;
            set;
        }

        // Now both D and A include B
        [Include]
        [Association("D_B", "D_ID", "B_ID")]
        public B B
        {
            get;
            set;
        }

        // Another association to B
        [Include]
        [Association("D_B2", "D_ID", "B_ID2")]
        public B B2
        {
            get;
            set;
        }
    }
    public class E
    {
        [Key]
        public int E_ID
        {
            get;
            set;
        }

        public int E_ID2
        {
            get;
            set;
        }

        // this link creates the cycle
        // A->C->E->A
        [Include]
        [Association("E_A", "E_ID", "A_ID")]
        public A A
        {
            get;
            set;
        }
    }
    public class F
    {
        public int F_ID
        {
            get;
            set;
        }
    }
    [EnableClientAccess]
    public class TestGraphProvider : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
        // Another Domain operation entry returning As
        [Query]
        public IEnumerable<A> GetAs2()
        {
            return null;
        }
    }
    #endregion

    #region Test DomainServices
    [EnableClientAccess]
    public class CompositionOperationValidation_InvalidScenario1 : DomainService
    {
        public IQueryable<Parent> GetParents()
        {
            return null;
        }

        // Invalid : can't have an update operation for a child
        // unless one exists for parent as well
        public void Update(Child child)
        {
        }
    }

    public class CompositionOperationValidation_InvalidScenario2 : DomainService
    {
        public IQueryable<Parent> GetParents()
        {
            return null;
        }

        // Invalid : can't have an insert operation for a child
        // unless one exists for parent as well
        public void Insert(Child child)
        {
        }
    }

    public class CompositionOperationValidation_InvalidScenario3 : DomainService
    {
        public IQueryable<Parent> GetParents()
        {
            return null;
        }

        // Invalid : can't have a delete operation for a child
        // unless one exists for parent as well
        public void Delete(Child child)
        {
        }
    }

    public class CompositionOperationValidation_InvalidScenario4 : DomainService
    {
        public IQueryable<Parent> GetParents()
        {
            return null;
        }

        // Invalid : can't have a custom update operation for a child
        // unless one exists for parent as well
        [EntityAction]
        public void CustomUpdate(Child child)
        {
        }
    }

    [EnableClientAccess]
    public class CompositionOperationValidation_ValidScenario1 : DomainService
    {
        public IQueryable<Parent> GetParents()
        {
            return null;
        }

        public void Update(Parent parent)
        {
        }

        // valid, since its parent GrandChild supports update implicitly via Parent
        public void Update(GreatGrandChild greatGrandChild)
        {
        }
    }

    [EnableClientAccess]
    public class CompositionOperationValidation_ValidScenario2 : DomainService
    {
        public IQueryable<Parent> GetParents()
        {
            return null;
        }

        public void Update(Parent parent)
        {
        }

        // valid, since its parent GrandChild supports Update implicitly via Parent
        public void Delete(GreatGrandChild greatGrandChild)
        {
        }
    }

    [EnableClientAccess]
    public class DomainService_EntityWithRequiredAssociation : DomainService
    {
        public IQueryable<EntityWithRequiredAssociation> GetEntities()
        {
            return null;
        }
    }

    public class EntityWithRequiredAssociation
    {
        [Key]
        public int ID { get; set; }

        public int FK { get; set; }

        [Association("RequiredAssociation", "FK", "ID")]
        [Required]
        public EntityWithRequiredAssociation RequiredAssociation { get; set; }
    }

    [EnableClientAccess]
    public class DomainService_Bug877984 : DomainService
    {
        public IQueryable<Bug877984_TestEntity> GetEntities()
        {
            return null;
        }
    }

    public class Bug877984_TestEntity
    {
        [Key]
        public int ID { get;set; }

        public int BesetFriendID { get;set; }

        [DataMember(Name = "Name")]
        public string PersonName { get; set; }

        [DataMember]
        [Include("PersonName", "BestFriendName")]
        [Association("BestFriend", "BesetFriendID", "ID", IsForeignKey = true)]
        public Bug877984_TestEntity BestFriend { get; set; }
    }

    [EnableClientAccess]
    public class DomainService_BaseMethodOverrides : DomainService
    {
        public override ValueTask<ServiceQueryResult> QueryAsync(QueryDescription queryDescription)
        {
            return base.QueryAsync(queryDescription);
        }

        public override Task<bool> SubmitAsync(ChangeSet changeSet)
        {
            return base.SubmitAsync(changeSet);
        }

        public override ValueTask<ServiceInvokeResult> InvokeAsync(InvokeDescription invokeDescription)
        {
            return base.InvokeAsync(invokeDescription);
        }

        public override void Initialize(DomainServiceContext context)
        {
            base.Initialize(context);
        }
    }

    public class String_DomainService : DomainServiceDescriptionTest.GenericDomainService<string> { }

    // DomainOperationEntry overloads not supported
    [EnableClientAccess]
    public class InvalidProvider_MethodOverloads : DomainService
    {
        [Query]
        public IEnumerable<City> GetCities(int p)
        {
            return null;
        }

        [Query]
        public IEnumerable<City> GetCities(string p)
        {
            return null;
        }
    }

    // IEnumerable return type not supported
    [EnableClientAccess]
    public class InvalidProvider_InvalidDomainOperationEntryReturnType_IEnumerable : DomainService
    {
        [Query]
        public IEnumerable GetCities()
        {
            return null;
        }
    }

    // int Select methods not supported
    [EnableClientAccess]
    public class InvalidProvider_InvalidDomainOperationEntryReturnType_Int : DomainService
    {
        [Query]
        public int GetCities()
        {
            return 5;
        }
    }

    // Used to verify things work properly in medium trust. Do not add 
    // [MetadataProvider]s to this type.
    [EnableClientAccess]
    public class DomainService_Basic : DomainService
    {
        [Query]
        public virtual IEnumerable<Mock_CG_SimpleEntity> GetEntities()
        {
            return null;
        }
    }

    // Used to verify things work properly in medium trust. Do not add 
    // a buddy class for this type.
    public class Mock_CG_SimpleEntity
    {
        [Key]
        public int KeyField { get; set; }

        public string StringField { get; set; }
    }

    // Just add some provider to make sure we end up adding a new provider to the chain.
    [DomainServiceDescriptionProvider(typeof(DescriptionProviderA))]
    public class DomainService_Inheritance : DomainService
    {
        public IEnumerable<Mock_CG_BaseEntity> GetEntities()
        {
            return null;
        }
    }

    [KnownType(typeof(Mock_CG_DerivedEntity))]
    [KnownType(typeof(Mock_CG_DerivedDerivedEntity))]
    public class Mock_CG_BaseEntity
    {
        [Key]
        public int KeyField { get; set; }

        [ConcurrencyCheck]
        public string Data { get; set; }
    }

    [KnownType(typeof(Mock_CG_DerivedDerivedEntity))]
    public class Mock_CG_DerivedEntity : Mock_CG_BaseEntity
    {
        public string Data2 { get; set; }
    }

    public class Mock_CG_DerivedDerivedEntity : Mock_CG_DerivedEntity
    {
        public string Data3 { get; set; }
    }

    [EnableClientAccess]
    public class Provider_VirtualMethods : DomainService
    {
        [Query]
        public virtual IEnumerable<City> GetCities()
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class Provider_OverridenMethods : Provider_VirtualMethods
    {
        [Query]
        public override IEnumerable<City> GetCities()
        {
            return base.GetCities();
        }
    }

    [EnableClientAccess]
    public class Provider_Convention_1 : DomainService
    {
        public IEnumerable<City> GetCities()
        {
            return null;
        }
        public void UpdateCity(City city)
        {
        }
        public void DeleteCity(City city)
        {
        }
        public bool ResolveCity(City curr, City original, City store, bool isDeleted)
        {
            return false;
        }
        public void InsertCity(City city)
        {
        }
        public int Add(int a, int b)
        {
            return 5;
        }
        public void DoSomething()
        {
        }
        // this method should not be inferred as an operation
        public void Transformer(object from, object to)
        {
        }
    }

    [EnableClientAccess]
    public class Provider_Convention_2 : DomainService
    {
        public IEnumerable<City> GetCities()
        {
            return null;
        }
        public void ModifyCity(City city)
        {
        }
        public void RemoveCity(City city)
        {
        }
        public void AddCity(City city)
        {
        }
    }

    [EnableClientAccess]
    public class Provider_Convention_3 : DomainService
    {
        public IEnumerable<City> GetCities()
        {
            return null;
        }
        public void ChangeCity(City city)
        {
        }
        public void CreateCity(City city)
        {
        }
    }

    [EnableClientAccess]
    public class Provider_Convention_Complex : DomainService
    {
        public IEnumerable<ComplexType_Parent> GetParents() { return null; }

        // Invoke
        public int FindAnotherWithPhone(Phone phone) { return 1; }
        public IEnumerable<int> FindOthersWithAddressAndPhone(Address address, Phone phone) { return null; }

        // Update
        public void MoveParent(ComplexType_Parent parent, Address address) { }

        // Should remain invalid

        // looks like an update but returns a complex type
        public ContactInfo ExtractContact(ComplexType_Parent parent) { return null; }

        // Invokes with illegal complex types
        public void InvokeAbstract(InvalidComplexType_Abstract ict) { }
        public void InvokeStruct(InvalidComplexType_Struct ict) { }
        public void InvokeNoConstructor(InvalidComplexType_NoConstructor ict) { }
        public void InvokeMicrosoft(System.Data.SqlClient.SqlDataReader ict) { }
    }

    public class InvalidComplexType_Generic<T> { }
    public abstract class InvalidComplexType_Abstract { }
    public struct InvalidComplexType_Struct { }
    public class InvalidComplexType_NoConstructor { private InvalidComplexType_NoConstructor(int i) { } }

    public abstract class AbstractDomainService : DomainServiceDescriptionTest.GenericDomainService<Mock_CG_SimpleEntity> { }

    [EnableClientAccess]
    public class UpdateDomainService : DomainService
    {
        public IEnumerable<A> GetAs() { return null; }

        [Update]
        public void UpdateA(A a) { }

        [EntityAction]
        public void CustomUpdateA(A a) { }
    }

    public class Mock_CG_Entity_Excluded_Concurrency_Property_DomainService : DomainServiceDescriptionTest.GenericDomainService<Mock_CG_Entity_Excluded_Concurrency_Property>
    {
    }

    public class Mock_CG_Updatable_Entity_Excluded_Concurrency_Property_DomainService : DomainServiceDescriptionTest.GenericDomainService<Mock_CG_Entity_Excluded_Concurrency_Property>
    {
        [Update]
        public void Update(Mock_CG_Entity_Excluded_Concurrency_Property entity)
        {
        }
    }

    public class Mock_CG_Custom_Entity_Excluded_Concurrency_Property_DomainService : DomainServiceDescriptionTest.GenericDomainService<Mock_CG_Entity_Excluded_Concurrency_Property>
    {
        [EntityAction]
        public void Update(Mock_CG_Entity_Excluded_Concurrency_Property entity)
        {
        }
    }

    public class Mock_CG_Entity_Excluded_Concurrency_Property
    {
        [Key]
        [DataMember]
        public int K { get; set; }

        [Exclude]
        [ConcurrencyCheck]
        public int ConcurrencyProperty { get; set; }
    }

    // Invalid entity property polymorphism
    // Scenario 1 is illegal because a hidden type in the
    // hierarchy override a property on the base.
    [EnableClientAccess]
    public class Inherit_Polymorphic_New1_DomainService : DomainService
    {
        public IEnumerable<Inherit_Polymorphic_New1_Entity> GetEntities() { return null; }
    }
    [KnownType(typeof(Inherit_Polymorphic_New1_Derived_Visible))]
    public class Inherit_Polymorphic_New1_Entity
    {
        [Key]
        public string TheKey { get; set; }
        public string BaseProperty { get; set; }
    }
    public class Inherit_Polymorphic_New1_Derived_Hidden : Inherit_Polymorphic_New1_Entity
    {
        public new string BaseProperty { get; set; }
    }
    public class Inherit_Polymorphic_New1_Derived_Visible : Inherit_Polymorphic_New1_Derived_Hidden
    {
    }

    // Scenario 2 is illegal because an entity type in the
    // hierarchy override a property under the base.
    [EnableClientAccess]
    public class Inherit_Polymorphic_New2_DomainService : DomainService
    {
        public IEnumerable<Inherit_Polymorphic_New2_Entity> GetEntities() { return null; }
    }

    public class Inherit_Polymorphic_New2_Below_Entity
    {
        public string BaseProperty { get; set; }
    }
    public class Inherit_Polymorphic_New2_Entity : Inherit_Polymorphic_New2_Below_Entity
    {
        [Key]
        public string TheKey { get; set; }
        public new string BaseProperty { get; set; }
    }

    public class SimpleDomainService_InvokeOperations : DomainService
    {
        public string GetString()
        {
            return "Foo";
        }

        public string[] GetStringArray()
        {
            return new string[] { "Foo", "Bar" };
        }

        public IEnumerable<string> GetStringEnumerable()
        {
            return null;
        }

        public IEnumerable<string> GetStringEnumerableWithParam1(string param1)
        {
            return null;
        }

        public IEnumerable<string> GetStringEnumerableWithParam2(City param1)
        {
            return null;
        }

        public City GetCity()
        {
            return null;
        }

        public City[] GetCityArray()
        {
            return null;
        }

        public IEnumerable<City> GetCityEnumerable()
        {
            return null;
        }

        public IQueryable<City> GetCityIQueryable()
        {
            return null;
        }
    }
    #endregion
}

public class Proxy
{
    public static string Foo()
    {
        return "hello";
    }
}

#region Composition plus Inheritance scenarios
namespace Derived_Composition_Types
{
    [EnableClientAccess]
    public class Derived_Composition_DomainService : DomainService
    {
        public IEnumerable<P1> GetP1s() { return null; }
    }

    // Comp association is P1 <-->> C1
    // Comp inheritance has a split like this:
    //      C1 <-- C2,
    //      C1 <-- C3
    //   to demonstrate we handle splits in the hierarchy
    public class P1
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association("P1_C1", "ID", "ID")]
        [Composition]
        public List<C1> C1s { get { return null; } set { } }
    }
    [KnownType(typeof(C2))]
    [KnownType(typeof(C3))]
    public class C1
    {
        [Key]
        public int ID { get; set; }

        [Association("P1_C1", "ID", "ID", IsForeignKey=true)]
        public P1 P1 { get { return null; } set { } }
    }

    public class C2 : C1 { }

    public class C3 : C1 { }

}

#endregion // Composition plus Inheritance scenarios

#region Invalid Associations scenarios
namespace InvalidAssociation_RoundTripOriginal
{
    public class A
    {
        [Key]
        public int A_ID1 { get; set; }

        [Key]
        public int A_ID2 { get; set; }

        [Key]
        public int A_ID3 { get; set; }

        [Include]
        [Association("A_B", "A_ID1, A_ID2, A_ID3", "B_ID, NonExistentKey, B_ID2")]
        [RoundtripOriginal]
        public B B { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_NullName
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association(null, "ID", "B_ID")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_EmptyThisKey
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association("A_B", "", "B_ID")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_NullOtherKey
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association("A_B", "ID", null)]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_ThisKeyNotFound
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        public int ID2 { get; set; }

        [Include]
        [Association("A_B", "ID, ID2, NonExistentKey", "B_ID1, B_ID2, B_ID3")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID1 { get; set; }
        [Key]
        public int B_ID2 { get; set; }
        [Key]
        public int B_ID3 { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_InvalidIsFK_SelfRef
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        public int ID2 { get; set; }

        [Association("A_A", "ID", "ID2")]
        public A A1 { get; set; }

        [Association("A_A", "ID2", "ID1")]
        public A A2 { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_InvalidIsFK_NonSelfRef
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Association("A_B", "ID", "B_ID", IsForeignKey = true)]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }

        [Association("A_B", "B_ID", "ID", IsForeignKey = true)]
        public A A { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }

        [Query]
        public IEnumerable<B> GetBs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_DupName_SelfRef
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        public int ID2 { get; set; }
        public int ID3 { get; set; }

        [Association("A_A", "ID", "ID2")]
        public A A1 { get; set; }

        [Association("A_A", "ID2", "ID", IsForeignKey = true)]
        public A A2 { get; set; }

        [Association("A_A", "ID3", "ID")]
        public A A3 { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_DupName_NonSelfRef
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Association("A_B", "ID", "B_ID")]
        public B B { get; set; }

        [Association("A_B", "ID", "B_ID")]
        public B B2 { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }

        [Association("A_B", "B_ID", "ID", IsForeignKey = true)]
        public A A { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }

        [Query]
        public IEnumerable<B> GetBs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_DupName_NonSelfRefNonIncluded
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Association("A_B", "ID", "B_ID")]
        public B B { get; set; }

        [Association("A_B", "ID", "B_ID")]
        public B B2 { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }

        [Association("A_B", "B_ID", "ID", IsForeignKey = true)]
        public A A { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_OtherKeyNotFound_SelfRef
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        public int ID2 { get; set; }
        public int ID3 { get; set; }

        [Association("A_A", "ID, ID2, ID3", "ID2, ID3,NonExistentKey")]
        public A A1 { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_OtherKeyNotFound_NonSelfRef
{
    public class A
    {
        [Key]
        public int A_ID1 { get; set; }
        [Key]
        public int A_ID2 { get; set; }
        [Key]
        public int A_ID3 { get; set; }

        [Include]
        [Association("A_B", "A_ID1, A_ID2, A_ID3", "B_ID, NonExistentKey, B_ID2")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }

        public int B_ID2 { get; set; }

        [Association("A_B", "B_ID, B_ID2", "ID", IsForeignKey = true)]
        public A A { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_KeyCountMismatch
{
    public class A
    {
        [Key]
        public int ID1 { get; set; }

        public int ID2 { get; set; }

        [Include]
        [Association("A_B", "ID1,ID2", "B_ID")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_KeyCountMismatch_Reverse
{
    public class A
    {
        [Key]
        public int A_ID { get; set; }

        [Include]
        [Association("A_B", "A_ID", "B_ID1,B_ID2")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID1 { get; set; }

        [Key]
        public int B_ID2 { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}
namespace ValidAssociation_InheritanceSelfRef
{
    // These classes test 3 scenarios.
    //  1) Self-referencing associations in a base class are inherited (A1, A2)
    //  2) Self-referencing associations in a derived class are supported (B1, B2)
    //  3) Self-referencing associations split between base and derived classes are supported (A, B)

    [KnownType(typeof(B))]
    public class A
    {
        [Key]
        public int ID { get; set; }

        public int ID_A { get; set; }

        [Association("A_A", "ID", "ID_A")]
        public A A1 { get; set; }

        [Association("A_A", "ID_A", "ID", IsForeignKey = true)]
        public A A2 { get; set; }

        [Include]
        [Association("A_B", "ID", "ID")]
        public B B { get; set; }
    }

    public class B : A
    {
        public int ID_B { get; set; }

        [Association("A_B", "ID", "ID", IsForeignKey = true)]
        public A A { get; set; }

        [Association("B_B", "ID", "ID_B")]
        public B B1 { get; set; }

        [Association("B_B", "ID_B", "ID", IsForeignKey = true)]
        public B B2 { get; set; }
    }

    [EnableClientAccess]
    public class ValidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }

        [Query]
        public IEnumerable<B> GetBs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_FKNotSingleton
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association("A_B", "ID", "ID", IsForeignKey = true)]
        public List<B> ListOfBs { get; set; }
    }

    public class B
    {
        [Key]
        public int ID { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_OtherFKNotSingleton
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association("A_B", "ID", "ID")]
        public List<B> ListOfBs { get; set; }
    }

    public class B
    {
        [Key]
        public int ID { get; set; }

        [Association("A_B", "ID", "ID", IsForeignKey = true)]
        public List<A> ListOfAs { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_TypesDoNotAlign_OneToOne
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association("A_B", "ID", "ID")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int ID { get; set; }

        [Association("A_B", "ID", "ID", IsForeignKey = true)]
        public C C { get; set; }
    }

    public class C
    {
        [Key]
        public int ID { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }

        [Query]
        public IEnumerable<C> GetCs()
        {
            return null;
        }
    }
}

namespace InvalidAssociation_TypesDoNotAlign_OneToMany
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Include]
        [Association("A_B", "ID", "ID")]
        public List<B> B { get; set; }
    }

    public class B
    {
        [Key]
        public int ID { get; set; }

        [Association("A_B", "ID", "ID", IsForeignKey = true)]
        public C C { get; set; }
    }

    public class C
    {
        [Key]
        public int ID { get; set; }
    }

    [EnableClientAccess]
    public class InvalidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }

        [Query]
        public IEnumerable<C> GetCs()
        {
            return null;
        }
    }
}

#endregion

#region Obscure valid Associations scenarios
// in this case, since B is not included and there is no Query method returning B, the otherKey (even non-existent) is not validated
namespace ValidAssociation_OtherKeyNotFound_OtherTypeNotExposed
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Key]
        public int ID1 { get; set; }

        [Association("A_B", "ID, ID1", "B_ID, NonExistentKey")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }
    }

    [EnableClientAccess]
    public class ValidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}

// in this case, since B is not included and there is no Query method returning B, the IsForeignKey value is not validated
namespace ValidAssociation_InvalidIsFK_OtherTypeNotExposed
{
    public class A
    {
        [Key]
        public int ID { get; set; }

        [Association("A_B", "ID", "B_ID")]
        public B B { get; set; }
    }

    public class B
    {
        [Key]
        public int B_ID { get; set; }

        [Association("A_B", "ID", "B_ID")]
        public A A { get; set; }
    }

    [EnableClientAccess]
    public class ValidAssociationDomainService : DomainService
    {
        [Query]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
    }
}
#endregion
