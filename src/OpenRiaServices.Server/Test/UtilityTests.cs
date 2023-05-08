extern alias SystemWebDomainServices;

using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
//using DbContextModels.AdventureWorks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TestDomainServices;
using Address = TestDomainServices.Address;

namespace OpenRiaServices.Server.Test
{
    using BinaryTypeUtility = SystemWebDomainServices::OpenRiaServices.BinaryTypeUtility;
    using SerializationUtility = SystemWebDomainServices::OpenRiaServices.SerializationUtility;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Tests utility helper methods.
    /// </summary>
    [TestClass]
    public class UtilityTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            DynamicTestValidator.Reset();
        }

        [TestMethod]
        public void ComplexType_CustomValidator_MultipleMemberNames()
        {
            // ensure TDPs are registered
            DomainServiceDescription.GetDescription(typeof(ComplexTypes_TestService));

            ComplexType_Parent entity = new ComplexType_Parent
            {
                ID = 1,
                ContactInfo =
                    new ContactInfo
                    {
                        Name = "Mathew",
                        HomeAddress = new Address { AddressLine1 = "47 South Wynn Rd.", City = "Oregon", State = "OH" },
                        PrimaryPhone = new Phone { AreaCode = "419", Number = "693-6096" }
                    },
            };

            // configure multi member validation errors
            DynamicTestValidator.ForcedValidationResults.Clear();
            ValidationResult contactResult = new ValidationResult("ContactInfo", new string[] { "Name", "PrimaryPhone" });
            ValidationResult phoneResult = new ValidationResult("Phone", new string[] { "AreaCode", "Number" });
            DynamicTestValidator.ForcedValidationResults[entity.ContactInfo] = contactResult;
            DynamicTestValidator.ForcedValidationResults[typeof(Phone)] = phoneResult;

            ValidationContext validationContext = ValidationUtilities.CreateValidationContext(entity, null);
            List<ValidationResult> results = new List<ValidationResult>();
            bool isValid = ValidationUtilities.TryValidateObject(entity, validationContext, results);
            Assert.IsFalse(isValid);

            // Verify that the member names have been transformed into full paths
            ValidationResult result = results.Single(q => q.ErrorMessage == "ContactInfo-ContactInfo");
            string[] memberNames = result.MemberNames.ToArray();
            Assert.AreEqual(2, memberNames.Length);
            Assert.IsTrue(memberNames.Contains("ContactInfo.Name"));
            Assert.IsTrue(memberNames.Contains("ContactInfo.PrimaryPhone"));

            // here we expect member names to be transformed into full paths
            result = results.Single(q => q.ErrorMessage == "Phone-TypeLevel");
            memberNames = result.MemberNames.ToArray();
            Assert.AreEqual(2, memberNames.Length);
            Assert.IsTrue(memberNames.Contains("ContactInfo.PrimaryPhone.AreaCode"));
            Assert.IsTrue(memberNames.Contains("ContactInfo.PrimaryPhone.Number"));
        }

        /// <summary>
        /// Verifty that our ValidateObject utility method handles complex type members
        /// </summary>
        [TestMethod]
        public void TestEntityValidation_ComplexTypes()
        {
            // ensure TDPs are registered
            DomainServiceDescription.GetDescription(typeof(ComplexTypes_TestService));

            ComplexType_Parent entity = new ComplexType_Parent
            {
                ID = 1,
                ContactInfo =
                    new ContactInfo
                    {
                        Name = "Mathew",
                        HomeAddress = new Address { AddressLine1 = "47 South Wynn Rd.", City = "Oregon", State = "OH" },
                        PrimaryPhone = new Phone { AreaCode = "419", Number = "693-6096" }
                    },
            };

            ValidationContext validationContext = ValidationUtilities.CreateValidationContext(entity, null);
            List<ValidationResult> results = new List<ValidationResult>();
            bool isValid = ValidationUtilities.TryValidateObject(entity, validationContext, results);
            Assert.IsTrue(isValid && results.Count == 0);

            // set an invalid property and revalidate
            entity.ContactInfo.PrimaryPhone.AreaCode = "Invalid";
            results = new List<ValidationResult>();
            isValid = ValidationUtilities.TryValidateObject(entity, validationContext, results);
            Assert.IsTrue(!isValid && results.Count == 1);
            ValidationResult result = results.Single();
            Assert.AreEqual("The field AreaCode must be a string with a maximum length of 3.", result.ErrorMessage);
            Assert.AreEqual("ContactInfo.PrimaryPhone.AreaCode", result.MemberNames.Single());

            // create TWO validation errors
            entity.ContactInfo.PrimaryPhone.AreaCode = "Invalid";
            entity.ContactInfo.HomeAddress.State = "Invalid";
            results = new List<ValidationResult>();
            isValid = ValidationUtilities.TryValidateObject(entity, validationContext, results);
            Assert.IsTrue(!isValid && results.Count == 2);
            Assert.AreEqual("The field State must be a string with a maximum length of 2.", results[0].ErrorMessage);
            Assert.AreEqual("ContactInfo.HomeAddress.State", results[0].MemberNames.Single());
            Assert.AreEqual("The field AreaCode must be a string with a maximum length of 3.", results[1].ErrorMessage);
            Assert.AreEqual("ContactInfo.PrimaryPhone.AreaCode", results[1].MemberNames.Single());

            // verify custom validation was called at CT type and property levels
            entity.ContactInfo.PrimaryPhone.AreaCode = "419";
            entity.ContactInfo.HomeAddress.State = "OH";
            DynamicTestValidator.Monitor(true);
            results = new List<ValidationResult>();
            isValid = ValidationUtilities.TryValidateObject(entity, validationContext, results);
            Assert.AreEqual(3, DynamicTestValidator.ValidationCalls.Count);
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == null && v.ObjectType == typeof(ContactInfo)));
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == "ContactInfo" && v.ObjectType == typeof(ComplexType_Parent)));
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == null && v.ObjectType == typeof(Phone)));
            DynamicTestValidator.Monitor(false);

            // verify deep CT collection validation
            List<ComplexType_Recursive> children = new List<ComplexType_Recursive> {
                new ComplexType_Recursive { P1 = "1", P4 = -1 },  // invalid element
                new ComplexType_Recursive { P1 = "2", P3 = 
                    new List<ComplexType_Recursive> { 
                        new ComplexType_Recursive { P1 = "3", P4 = -5 }  // invalid element in nested collection
                    }
                }
            };
            ComplexType_Scenarios_Parent parent = new ComplexType_Scenarios_Parent 
            { 
                ID = 1, 
                ComplexType_Recursive = new ComplexType_Recursive { P1 = "1", P3 = children } 
            };
            validationContext = ValidationUtilities.CreateValidationContext(parent, null);
            results = new List<ValidationResult>();
            isValid = ValidationUtilities.TryValidateObject(parent, validationContext, results);
            Assert.AreEqual(2, results.Count);
            result = results.Single(p => p.MemberNames.Single() == "ComplexType_Recursive.P3().P4");
            Assert.AreEqual("The field P4 must be between 0 and 5.", result.ErrorMessage);
            result = results.Single(p => p.MemberNames.Single() == "ComplexType_Recursive.P3().P3().P4");
            Assert.AreEqual("The field P4 must be between 0 and 5.", result.ErrorMessage);
        }
#if !NET6_0_OR_GREATER

        [TestMethod]
        [Description("Tests the reflection-based Binary support in the TypeUtility.")]
        public void BinarySupport()
        {
            Assert.IsFalse(BinaryTypeUtility.IsTypeBinary(typeof(int)),
                "Int32 isn't a Binary.");
            Assert.IsFalse(BinaryTypeUtility.IsTypeBinary(typeof(string)),
                "String isn't a Binary.");
            Assert.IsTrue(BinaryTypeUtility.IsTypeBinary(typeof(Binary)),
                "Binary is a Binary!");

            Assert.AreEqual(typeof(byte[]), SerializationUtility.GetClientType(typeof(Binary)),
                "The client type for Binary is byte[].");

            byte[] bytes = new byte[] { 10, 20, 30 };
            Binary binary = new Binary(bytes);

            Assert.IsTrue(bytes.SequenceEqual((IEnumerable<byte>)SerializationUtility.GetClientValue(typeof(byte[]), binary)),
                "Client Binary values should be equal.");
            Assert.IsTrue(bytes.SequenceEqual((IEnumerable<byte>)SerializationUtility.GetClientValue(typeof(byte[]), bytes)),
                "Client byte[] values should be equal.");
            Assert.AreEqual(binary, SerializationUtility.GetServerValue(typeof(Binary), binary),
                "Server Binary values should be equal.");
            Assert.AreEqual(binary, SerializationUtility.GetServerValue(typeof(Binary), bytes),
                "Server byte[] values should be equal.");
        }

#endif
    }
}
