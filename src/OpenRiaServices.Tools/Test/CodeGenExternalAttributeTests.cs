using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.Server.Test.Utilities;
using OpenRiaServices;
using DataTests.Scenarios.EF.Northwind;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DomainObjectResource = OpenRiaServices.Server.Resource;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Unit tests to verify the code generation behavior of properties marked with ExternalReferenceAttribute.
    /// </summary>
    [TestClass]
    public class CodeGenExternalReferenceAttributeTests
    {
        /// <summary>
        /// Verifies that entities with external references are generated properly.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that entities with external references are generated properly.")]
        public void CodeGen_External_Entity_Succeeds()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(MockOrder_DomainService));
        }

        /// <summary>
        /// Verifies that entities marked with ExternalReferenceAttribute attributes and missing AssociationAttribute generate an error.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that entities marked with ExternalReferenceAttribute attributes and missing AssociationAttribute generate an error.")]
        public void CodeGen_External_Entity_WithoutAssociation_Error()
        {
            string error = string.Format(DomainObjectResource.InvalidExternal_NonAssociationMember, typeof(MockOrderWithoutAssociations).Name, "Customer");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(MockOrderWithoutAssociations_DomainService), error);
        }

        /// <summary>
        /// Verifies that Entity Framework entities that properly reference POCO entities generate code as expected.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that Entity Framework entities that properly reference POCO entities generate code as expected.")]
        public void CodeGen_External_Entity_EFtoPOCO()
        {
            ConsoleLogger logger = new ConsoleLogger();
            Type[] domainServices = 
                {
                     typeof(PersonalDetails_DomainService),
                     typeof(EF_NorthwindScenarios_EmployeeWithExternalProperty)
                };
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", domainServices, logger, null);

            TestHelper.AssertGeneratedCodeContains(
                generatedCode,
                "private EntityRef<global::DataTests.Scenarios.EF.Northwind.PersonalDetails> _personalDetails_MarkedAsExternal;",
                "[Association(\"Employee_PersonalDetails\", \"EmployeeID\", \"UniqueID\", IsForeignKey=true)] [ExternalReference()]",
                "public global::DataTests.Scenarios.EF.Northwind.PersonalDetails PersonalDetails_MarkedAsExternal",
                "private bool FilterPersonalDetails_MarkedAsExternal(global::DataTests.Scenarios.EF.Northwind.PersonalDetails entity)");
        }
    }

    #region Mock Entities

    public class MockCustomer
    {
        [Key]
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class MockProduct
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public abstract class MockOrderBase
    {
        [Key]
        public int OrderId { get; set; }

        // Foreign keys
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
    }

    public class MockOrder_DomainService : GenericDomainService<MockOrder> { }

    public partial class MockOrder : MockOrderBase
    {
        [ExternalReference, Association("Order_Customer", "CustomerId", "CustomerId", IsForeignKey = true)]
        public MockCustomer Customer { get; private set; }

        [ExternalReference, Association("Order_Product", "ProductId", "ProductId")]
        public List<MockProduct> Products { get; private set; }
    }

    public class MockOrderWithoutAssociations_DomainService : GenericDomainService<MockOrderWithoutAssociations> { }

    public partial class MockOrderWithoutAssociations : MockOrderBase
    {
        [ExternalReference]
        public MockCustomer Customer { get; private set; }

        [ExternalReference]
        public List<MockProduct> Products { get; private set; }
    }

    public class PersonalDetails_DomainService : GenericDomainService<PersonalDetails> { }
    
    #endregion Mock Entities
}