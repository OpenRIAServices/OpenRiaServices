using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EFCoreModels.OwnedTypes;

namespace OpenRiaServices.Server.EntityFrameworkCore.Test
{
    [TestClass]
    public class DomainServiceDescriptionTests
    {
        /// <summary>
        /// Verify that the EF metadata provider is registered for mapped CTs, and that attribute are inferred properly
        /// </summary>
        [TestMethod]
        public void ComplexType_EFCoreProvidderTest()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(EFCoreComplexTypesService));

            CollectionAssert.AreEquivalent(dsd.EntityTypes.ToList(), new[] { typeof(Employee) });
            CollectionAssert.AreEquivalent(dsd.ComplexTypes.ToList(), new[] { typeof(Address), typeof(ContactInfo) });


            var employee = TypeDescriptor.GetProperties(typeof(Employee));
            Assert.IsNotNull(employee[nameof(Employee.EmployeeId)]!.Attributes[typeof(KeyAttribute)]);

            // Contact Info should not have association attribute 
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsNull(employee[nameof(Employee.ContactInfo)]!.Attributes[typeof(AssociationAttribute)], "Navigation property to Owned entity MUST NOT be marked with AssociationAttribute, or ComplexObject will not be generated on client");
#pragma warning restore CS0618 // Type or member is obsolete


            // the HomePhone member is mapped as non-nullable, with a max length of 24. Verify attributes
            // were inferred
            var contactInfo = TypeDescriptor.GetProperties(typeof(ContactInfo));
            var homePhone = contactInfo[nameof(ContactInfo.HomePhone)]!;
            Assert.IsNotNull(homePhone.Attributes[typeof(RequiredAttribute)]);
            StringLengthAttribute sl = (StringLengthAttribute)homePhone.Attributes[typeof(StringLengthAttribute)]!;
            Assert.AreEqual(24, sl.MaximumLength);

            //Assert.IsNotNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            //Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);

            // the AddressLine1 member is mapped as non-nullable, with a max length of 100. Verify attributes
            // were inferred
            var address = TypeDescriptor.GetProperties(typeof(Address));
            var addressLine = address[nameof(Address.AddressLine)]!;
            Assert.IsNotNull(addressLine.Attributes[typeof(RequiredAttribute)]);
            sl = (StringLengthAttribute)addressLine.Attributes[typeof(StringLengthAttribute)]!;
            Assert.AreEqual(100, sl.MaximumLength);

            //Assert.IsNotNull(pd.Attributes[typeof(ConcurrencyCheckAttribute)]);
            //Assert.IsNotNull(pd.Attributes[typeof(RoundtripOriginalAttribute)]);
        }
    }

    [EnableClientAccess]
    public class EFCoreComplexTypesService : DbDomainService<OwnedTypesDbContext>
    {
        public IQueryable<Employee> GetCustomers()
        {
            return null;
        }
    }
}
