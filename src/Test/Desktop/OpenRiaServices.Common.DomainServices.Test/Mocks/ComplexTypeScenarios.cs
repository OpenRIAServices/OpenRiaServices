using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using OpenRiaServices.EntityFramework;
using OpenRiaServices.Server;
using System;

namespace TestDomainServices
{
    [EnableClientAccess]
    public class ComplexTypes_TestService : DomainService
    {
        public IQueryable<ComplexType_Parent> GetParents()
        {
            return CreateParents().AsQueryable();
        }

        public void UpdateComplexType_Parent(ComplexType_Parent parent)
        {
            ComplexType_Parent orig = this.ChangeSet.GetOriginal(parent);

            // verify that the original values were populated properly
            if ((orig.ContactInfo.PrimaryPhone.AreaCode != "419") ||
                (orig.ContactInfo.HomeAddress.State != "OH")
                )
            {
                throw new Exception("Incorrect original values!");
            }
        }

        [EntityAction]
        public void TestAutoSync(ComplexType_Parent parent, Phone phone)
        {
            // update a nested simple member
            parent.ContactInfo.HomeAddress.AddressLine2 = "Updated";

            // set a CT ref to the new phone instance
            parent.ContactInfo.PrimaryPhone = phone;
        }

        public Address RoundtripAddress(Address address)
        {
            return address;
        }

        public Address ReturnHomeAddress(ContactInfo contact)
        {
            return contact.HomeAddress;
        }

        internal List<ComplexType_Parent> CreateParents()
        {
            List<ComplexType_Parent> parents = new List<ComplexType_Parent>() {

                new ComplexType_Parent {
                    ID = 1,
                    ContactInfo =
                        new ContactInfo
                        {
                            Name = "Mathew",
                            HomeAddress = new Address { AddressLine1 = "47 South Wynn Rd.", City = "Oregon", State = "OH", Zip = "43616" },
                            PrimaryPhone = new Phone { AreaCode = "419", Number = "693-6096" }
                        },
                },
                new ComplexType_Parent {
                    ID = 2,
                    ContactInfo =
                    new ContactInfo
                    {
                        Name = "Fred",
                        HomeAddress = new Address { AddressLine1 = "21 Maple St.", City = "Issaquah", State = "WA", Zip = "98029" },
                        PrimaryPhone = new Phone { AreaCode = "425", Number = "111-2222" }
                    }
                },
                new ComplexType_Parent {
                    ID = 3,
                    ContactInfo =
                    new ContactInfo
                    {
                        Name = "Amy",
                        HomeAddress = new Address { AddressLine1 = "31 Springwood West", City = "Oregon", State = "OH", Zip = "43616" },
                        PrimaryPhone = new Phone { AreaCode = "419", Number = "333-4444" }
                    }
                }
            };

            return parents;
        }
    }

    /// <summary>
    /// This DomainService shows that a service can be authored that conatains no entities.
    /// </summary>
    [EnableClientAccess]
    public class ComplexTypes_InvokeOperationsOnly : DomainService
    {
        [Invoke]
        public void UpdateContact(ContactInfo contact)
        {
            // noop
        }

        public Address RoundtripAddress(Address address)
        {
            return address;
        }

        [Invoke]
        public Address InvokeGetInvalidAddress()
        {
            return new Address
            {
                AddressLine1 = "47 South Wynn Rd.",
                City = "Oregon",
                State = "Invalid"
            };
        }
    }

    [EnableClientAccess]
    [LinqToEntitiesDomainServiceDescriptionProvider(typeof(NorthwindModel.NorthwindEntities))]
    public class ComplexTypes_TestService_Scenarios : DomainService
    {
        public IEnumerable<ComplexType_Scenarios_Parent> GetComplexType_Scenarios_Parent()
        {
            return null;
        }

        public IEnumerable<NorthwindModel.Employee> GetEmployees()
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class ComplexTypes_TestService_ComplexMethodSignatures : DomainService
    {
        public IEnumerable<ComplexType_Parent> GetComplexType_Scenarios_Parent()
        {
            return null;
        }

        public void CustomUpdateHomeAddress(ComplexType_Parent parent, Address newAddress)
        {

        }

        [EntityAction]
        public void UpdateHomeAddress(ComplexType_Parent parent, Address newAddress)
        {

        }

        // Service method taking a contact and returning a collection of addresses
        public IEnumerable<Address> Foo(ContactInfo contact)
        {
            return null;
        }

        [Invoke]
        public IEnumerable<Phone> GetAllPhoneNumbers(ContactInfo contact)
        {
            return null;
        }

        // service method taking a collection of addresses
        public void Bar(IEnumerable<Address> addresses)
        {
        }

        [Invoke]
        public void AppendMoreAddresses(IEnumerable<Address> addresses)
        {
        }
    }

    public class ComplexType_Recursive
    {
        public string P1 { get; set; }

        [Range(0, 5)]
        public int P4 { get; set; }

        public ComplexType_Recursive P2 { get; set; }

        public IEnumerable<ComplexType_Recursive> P3 { get; set; }
    }

    /// <summary>
    /// Don't add any type or member level validation to this type.
    /// </summary>
    public class ComplexType_NoValidation
    {
        public ContactInfo ContactInfo { get; set; }
    }

    public class ComplexType_Invalid_AssociationMember
    {
        public string FK { get; set; }

        [Association("Foo", "FK", "Name", IsForeignKey = true)]
        public ContactInfo ContactInfo { get; set; }
    }

    public class ComplexType_Invalid_Parent
    {
    }

    public class ComplexType_Valid_Child : ComplexType_Invalid_Parent
    {
        public int Prop { get; set; }
    }

    [EnableClientAccess]
    public class ComplexType_InvalidEntity_ComplexProperty : DomainService
    {
        public IEnumerable<ComplexType_Invalid_Entity_ComplexProperty> GetEntities() { return null; }
    }

    public class ComplexType_Invalid_Entity_ComplexProperty
    {
        [Key]
        public int Id { get; set; }

        [Association("E_C", "Id", "Prop")]
        public ComplexType_Valid_Child Child { get; set; }
    }

    [EnableClientAccess]
    public class ComplexType_InvalidEntityInheritance : DomainService
    {
        public IEnumerable<ComplexType_Invalid_EntityInheritance_FromComplex> GetParents() { return null; }
        public void Invoke(ComplexType_Invalid_Parent parent) { }
    }

    public class ComplexType_Invalid_EntityInheritance_FromComplex : ComplexType_Invalid_Parent
    {
        [Key]
        public int Id { get; set; }
    }

    [EnableClientAccess]
    public class ComplexType_InvalidInheritance : DomainService
    {
        public IEnumerable<ComplexType_Parent> GetParents() { return null; }
        public ComplexType_Valid_Child Invoke(ComplexType_Invalid_Parent parent) { return null; }
    }

    [KnownType(typeof(ComplexType_KnownType_Child))]
    public class ComplexType_KnownType_Parent
    {
    }

    public class ComplexType_KnownType_Child : ComplexType_KnownType_Parent
    {
    }

    public class ComplexType_Invalid_CompositionMember
    {
        [Composition]
        public List<ComplexType_Valid_Child> Children { get; set; }
    }

    public class ComplexType_Invalid_IncludeMember
    {
        [Include]
        public ComplexType_Valid_Child Child { get; set; }
    }

    public class ComplexType_Valid_SimpleList
    {
        public List<int> Numbers { get; set; }
    }

    public class ComplexType_Valid_ComplexList
    {
        public List<ComplexType_Valid_SimpleList> NumberList { get; set; }
    }

    [RoundtripOriginal]
    public class ComplexType_Parent
    {
        [Key]
        public int ID { get; set; }

        [CustomValidation(typeof(DynamicTestValidator), "Validate")]
        public ContactInfo ContactInfo { get; set; }
    }

    [CustomValidation(typeof(DynamicTestValidator), "Validate")]
    [RoundtripOriginal]
    public class ContactInfo
    {
        public string Name { get; set; }

        public Address HomeAddress { get; set; }

        public Phone PrimaryPhone { get; set; }
    }

    [RoundtripOriginal]
    [MetadataType(typeof(AddressMetadata))]
    public class Address
    {
        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }
    }

    public static class AddressMetadata
    {
        [StringLength(2)]
        public static string State;

        [StringLength(5)]
        public static string Zip;
    }

    [CustomValidation(typeof(DynamicTestValidator), "Validate")]
    [RoundtripOriginal]
    [MetadataType(typeof(PhoneMetadata))]
    public class Phone
    {
        public string AreaCode { get; set; }

        public string Number { get; set; }
    }

    public static class PhoneMetadata
    {
        [StringLength(3)]
        [Required]
        public static string AreaCode;

        [Required]
        public static string Number;
    }

    public class ComplexType_Scenarios_Parent
    {
        [Key]
        public int ID { get; set; }

        public ComplexType_Recursive ComplexType_Recursive { get; set; }
    }


    public class ValidatableObject : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            throw new NotImplementedException();
        }
    }

    public class Indirect_ValidatableObject
    {
        public ValidatableObject Validatable { get; }
    }

    #region Complex Types Inheritance
    [EnableClientAccess]
    public class ComplexTypes_DomainService : DomainService
    {
        static ComplexInheritance_Child highestChild = null;
        static int highestSum = Int32.MinValue;

        public IEnumerable<ComplexTypeInheritance_EntityGrandparent> GetStub()
        {
            highestChild = null;
            highestSum = Int32.MinValue;

            ComplexTypeInheritance_EntityGrandparent[] stubs = new ComplexTypeInheritance_EntityGrandparent[]
            {
                new ComplexTypeInheritance_EntityGrandchild()
                {
                    ID = 1,
                    Child = new ComplexInheritance_Child()
                    {
                        A1 = 1,
                        A2 = 2,
                        Z1 = 10,
                        Z2 = 20,
                    },
                }
            };

            return stubs;
        }

        // Tests that we can roundtrip complex types in a custom method.
        public void ChooseHighestStubOrChild(ComplexTypeInheritance_EntityGrandchild grandchild, ComplexInheritance_Child child, ComplexInheritance_Child[] children)
        {
            var entityChildren = new List<ComplexInheritance_Child>();
            entityChildren.Add(grandchild.Child);
            ChooseHighestChild(entityChildren);
            ChooseHighestChild(new ComplexInheritance_Child[] { child });
            ChooseHighestChild(children);
        }

        // Tests that we can roundtrip complex types in an invoke method.
        public ComplexInheritance_Child GetHighestChild(ComplexInheritance_Child child, ComplexInheritance_Child[] children)
        {
            ChooseHighestChild(new ComplexInheritance_Child[] { child });
            ChooseHighestChild(children);
            return highestChild;
        }

        // Tests that we can round trip objects with inheritance heirarchies on the server but not on the client.
        public ComplexInheritance_Child GetInheritedMember(ComplexInheritance_Child child)
        {
            return new ComplexInheritance_Child()
            {
                A1 = child.A1 + 1,
                A2 = child.A2 + 1,
                Z1 = child.Z1 + 1,
                Z2 = child.Z2 + 1,
            };
        }

        private static void ChooseHighestChild(IEnumerable<ComplexInheritance_Child> children)
        {
            foreach (var child in children)
            {
                int sum = child.A1 + child.A2 + child.Z1 + child.Z2;

                if (sum >= highestSum)
                {
                    highestChild = child;
                    highestSum = sum;
                }
            }
        }
    }

    // note: we ensure children have As and Zs because DataContract serialization
    // serializes first by base type, then by alpha DataMembers
    // note: no Known Type means base does not exist on the client, and child and child_child are unrelated.
    // Whether or not they are related affects the serialization.
    public class ComplexInheritance_Base
    {
        public int A1 { get; set; }
        public int Z1 { get; set; }
    }

    public class ComplexInheritance_Child : ComplexInheritance_Base
    {
        public int A2 { get; set; }
        public int Z2 { get; set; }
    }

    [KnownType(typeof(ComplexTypeInheritance_EntityGrandchild))]
    public class ComplexTypeInheritance_EntityGrandparent
    {
        [Key]
        public int ID { get; set; }
    }

    public class ComplexTypeInheritance_EntityParent : ComplexTypeInheritance_EntityGrandparent
    {
        public ComplexInheritance_Child Child { get; set; }
    }

    public class ComplexTypeInheritance_EntityGrandchild : ComplexTypeInheritance_EntityParent
    {
    }
    #endregion

    #region Complex Types in Invoke and Custom
    // Test Invoke (In/Out) and Named Update (In)
    [EnableClientAccess]
    public class ComplexInvokeAndCustom : DomainService
    {
        public IEnumerable<StubEntity> GetStub()
        {
            return null;
        }

        public InvokeOut InvokeMethod(InvokeIn1 in1, InvokeIn2 in2)
        {
            return null;
        }

        public void CustomMethod(StubEntity entity, CustomIn1 in1, CustomIn2 in2)
        {
        }

        public static List<Type> GetExposedEntityTypes()
        {
            return new List<Type>(new Type[]
            {
                typeof(StubEntity),
            });
        }

        public static List<Type> GetExposedComplexTypes()
        {
            return new List<Type>(new Type[]
            {
                typeof(InvokeIn1),
                typeof(InvokeIn2),
                typeof(InvokeOut),
                typeof(CustomIn1),
                typeof(CustomIn2),
                typeof(RelatedType1),
                typeof(RelatedType2),
                typeof(RelatedType3),
            });
        }
    }

    public class InvokeIn1
    {
        public RelatedType1 Relative { get; set; }
    }

    public class InvokeIn2
    {
    }

    public class InvokeOut
    {
        public RelatedType2 Relative { get; set; }
    }

    public class CustomIn1
    {
    }

    public class CustomIn2
    {
        public RelatedType3 Relative { get; set; }
    }

    public class RelatedType1
    {
    }

    public class RelatedType2
    {
    }

    public class RelatedType3
    {
    }

    public class StubEntity
    {
        [Key]
        public int ID { get; set; }
    }
    #endregion
}
