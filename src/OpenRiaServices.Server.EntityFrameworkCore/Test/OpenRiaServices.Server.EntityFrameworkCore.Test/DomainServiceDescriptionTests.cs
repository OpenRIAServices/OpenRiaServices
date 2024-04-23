using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OwnedTypes = EFCoreModels.Scenarios.OwnedTypes;
using ComplexTypes = EFCoreModels.Scenarios.ComplexTypes;
using MSTest = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Server.EntityFrameworkCore.Test
{
    [TestClass]
    public class DomainServiceDescriptionTests
    {
        /// <summary>
        /// Verify that the EF metadata provider is registered for mapped CTs, and that attribute are inferred properly
        /// </summary>
        [TestMethod]
        public void ComplexType_EFCore_OwnedTypes()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(EFCoreOwnedTypesService));

            CollectionAssert.AreEquivalent(dsd.EntityTypes.ToList(), new[] { typeof(OwnedTypes.Employee) });
            CollectionAssert.AreEquivalent(dsd.ComplexTypes.ToList(), new[] { typeof(OwnedTypes.Address), typeof(OwnedTypes.ContactInfo) });

            var employee = TypeDescriptor.GetProperties(typeof(OwnedTypes.Employee));
            Assert.IsNotNull(employee[nameof(OwnedTypes.Employee.EmployeeId)]!.Attributes[typeof(KeyAttribute)]);

            // the HomePhone member is mapped as non-nullable, with a max length of 24. Verify attributes
            // were inferred
            var contactInfo = TypeDescriptor.GetProperties(typeof(OwnedTypes.ContactInfo));
            var homePhone = contactInfo[nameof(OwnedTypes.ContactInfo.HomePhone)]!;
            Assert.IsNotNull(homePhone.Attributes[typeof(RequiredAttribute)]);
            StringLengthAttribute sl = (StringLengthAttribute)homePhone.Attributes[typeof(StringLengthAttribute)]!;
            Assert.AreEqual(24, sl.MaximumLength);


            // the AddressLine1 member is mapped as non-nullable, with a max length of 100. Verify attributes
            // were inferred
            var address = TypeDescriptor.GetProperties(typeof(OwnedTypes.Address));
            var addressLine = address[nameof(OwnedTypes.Address.AddressLine)]!;
            Assert.IsNotNull(addressLine.Attributes[typeof(RequiredAttribute)]);
            sl = (StringLengthAttribute)addressLine.Attributes[typeof(StringLengthAttribute)]!;
            Assert.AreEqual(100, sl.MaximumLength);
        }
        /// <summary>
        /// Verify that the EF metadata provider is registered for mapped CTs, and that attribute are inferred properly
        /// </summary>
        [TestMethod]
        [MSTest.Ignore("Does not work yet, attributes are not discovered on ComplexObjects")]
        public void ComplexType_EFCore_ComplexTypes()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(EFCoreComplexTypesService));

            CollectionAssert.AreEquivalent(dsd.EntityTypes.ToList(), new[] { typeof(ComplexTypes.Employee) });
            CollectionAssert.AreEquivalent(dsd.ComplexTypes.ToList(), new[] { typeof(ComplexTypes.Address), typeof(ComplexTypes.ContactInfo) });

            var employee = TypeDescriptor.GetProperties(typeof(ComplexTypes.Employee));
            Assert.IsNotNull(employee[nameof(ComplexTypes.Employee.EmployeeId)]!.Attributes[typeof(KeyAttribute)]);

            // the HomePhone member is mapped as non-nullable, with a max length of 24. Verify attributes
            // were inferred
            var contactInfo = TypeDescriptor.GetProperties(typeof(ComplexTypes.ContactInfo));
            var homePhone = contactInfo[nameof(ComplexTypes.ContactInfo.HomePhone)]!;

            Assert.IsNotNull(homePhone.Attributes[typeof(RequiredAttribute)]);
            StringLengthAttribute sl = (StringLengthAttribute)homePhone.Attributes[typeof(StringLengthAttribute)]!;
            Assert.AreEqual(24, sl.MaximumLength);


            // the AddressLine1 member is mapped as non-nullable, with a max length of 100. Verify attributes
            // were inferred
            var address = TypeDescriptor.GetProperties(typeof(ComplexTypes.Address));
            var addressLine = address[nameof(ComplexTypes.Address.AddressLine)]!;
            Assert.IsNotNull(addressLine.Attributes[typeof(RequiredAttribute)]);
            sl = (StringLengthAttribute)addressLine.Attributes[typeof(StringLengthAttribute)]!;
            Assert.AreEqual(100, sl.MaximumLength);
        }
    }

    [EnableClientAccess]
    public class EFCoreOwnedTypesService : DbDomainService<OwnedTypes.OwnedTypesDbContext>
    {
        public IQueryable<OwnedTypes.Employee> GetCustomers()
        {
            return null!;
        }
    }

    [EnableClientAccess]
    public class EFCoreComplexTypesService : DbDomainService<ComplexTypes.ComplexTypesDbContext>
    {
        public IQueryable<ComplexTypes.Employee> GetCustomers()
        {
            return null!;
        }
    }
}
