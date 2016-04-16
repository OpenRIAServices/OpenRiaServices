extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using System.Reflection;
    using TestDomainServices;
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;
    using TypeUtility = SSmDsClient::OpenRiaServices.DomainServices.TypeUtility;

    [TestClass]
    public partial class ComplexTypeTests : UnitTestBase
    {
        [TestCleanup]
        public void TestCleanup()
        {
            DynamicTestValidator.Reset();
        }

        /// <summary>
        /// The key to this bug is a self referential type hierarchy
        /// A->B->A. The bug manifested itself because the RequiresValidation
        /// calculation for B was using partial results from A (A hadn't finished
        /// calculating yet).
        /// </summary>
        [TestMethod]
        [WorkItem(191931)]
        public void ComplexType_RecursiveRequiresValidationCalculation()
        {
            MetaType mtA = MetaType.GetMetaType(typeof(IndirectSelfReference_A));
            Assert.IsTrue(mtA.RequiresValidation);

            MetaType mtB = MetaType.GetMetaType(typeof(IndirectSelfReference_B));
            Assert.IsTrue(mtB.RequiresValidation);

            MetaType mt = MetaType.GetMetaType(typeof(ComplexType_RequiresValidation));
            Assert.IsTrue(mt.RequiresValidation);
        }

        [TestMethod]
        [Asynchronous]
        public void ComplexType_ServiceWithInvokeOperationsOnly()
        {
            ComplexTypes_InvokeOperationsOnly ctxt = new ComplexTypes_InvokeOperationsOnly(new Uri(TestURIs.RootURI, "TestDomainServices-ComplexTypes_InvokeOperationsOnly.svc"));

            Address address = new Address { AddressLine1 = "47 South Wynn Rd.", City = "Oregon", State = "OH" };
            InvokeOperation<Address> io = ctxt.RoundtripAddress(address);

            EnqueueConditional(delegate
            {
                return io.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(io);

                Address returnAddress = io.Value;
                Assert.IsTrue(DeepCompare(address, returnAddress));
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify the root context flows through the hierarchy during validation.
        /// </summary>
        [TestMethod]
        public void ComplexType_ValidationContext()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p1 = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p1.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p1 });

            ec.ValidationContext = ValidationUtilities.CreateValidationContext(ec, null);
            ec.ValidationContext.Items.Add("A", "B");

            ValidationContext validationContext = null;
            Action<ValidationContext, object> validationCallback = (ctxt, o) =>
                {
                    validationContext = ctxt;
                };

            p1.ContactInfo.HomeAddress.TestValidatePropertyCallback = validationCallback;

            p1.ContactInfo.HomeAddress.State = "HI";

            // verify that our callback was called and that the validation
            // context we set up flowed through
            Assert.IsNotNull(validationContext);
            Assert.IsTrue(validationContext.Items.ContainsKey("A"));
        }

        /// <summary>
        /// Verify that overrides of the protected virtuals defined on ComplexType are called
        /// as expected
        /// </summary>
        [TestMethod]
        public void ComplexType_VerifyProtectedVirtuals()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p1 = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p1.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p1 });

            string validatedValue = null;
            Action<ValidationContext, object> validationCallback = (ctxt, o) =>
            {
                validatedValue = (string)o;
            };
            p1.ContactInfo.HomeAddress.TestValidatePropertyCallback = validationCallback;

            PropertyChangedEventArgs propertyChangedArgs = null;
            Action<PropertyChangedEventArgs> propertyChangedCallback = (e) =>
            {
                propertyChangedArgs = e;
            };
            p1.ContactInfo.HomeAddress.TestPropertyChangedCallback = propertyChangedCallback;

            p1.ContactInfo.HomeAddress.State = "HI";

            Assert.AreEqual("HI", validatedValue);
            Assert.AreEqual("State", propertyChangedArgs.PropertyName);
        }

        /// <summary>
        /// Ensure that when the validation errors are cleared for an instance,
        /// the clear recurses through all children.
        /// </summary>
        [TestMethod]
        public void ComplexType_ValidationCollection_Clear()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p1 = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p1.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p1 });

            // Clear from root entity - expect errors for all children to be cleared
            TestHelperMethods.EnableValidation(p1, false);
            p1.ContactInfo.HomeAddress.State = "Invalid";
            p1.ContactInfo.PrimaryPhone.AreaCode = "Invalid";
            TestHelperMethods.EnableValidation(p1, true);
            Assert.IsFalse(ec.GetChanges().Validate(null));
            Assert.AreEqual(2, p1.ValidationErrors.Count);
            Assert.AreEqual(2, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);
            p1.ValidationErrors.Clear();
            Assert.AreEqual(0, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);

            // Clear from a CT instance in the hierarchy - expect errors for that child
            // to be cleared and all affected errors should also be cleared from the parent(s)
            // up the hierarchy
            Assert.IsFalse(ec.GetChanges().Validate(null));
            Assert.AreEqual(2, p1.ValidationErrors.Count);
            Assert.AreEqual(2, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);
            p1.ContactInfo.HomeAddress.ValidationErrors.Clear();
            Assert.AreEqual(1, p1.ValidationErrors.Count);
            Assert.AreEqual("ContactInfo.PrimaryPhone.AreaCode", p1.ValidationErrors.Single().MemberNames.Single());
            Assert.AreEqual(1, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual("PrimaryPhone.AreaCode", p1.ContactInfo.ValidationErrors.Single().MemberNames.Single());
            Assert.AreEqual(0, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);
            Assert.AreEqual("AreaCode", p1.ContactInfo.PrimaryPhone.ValidationErrors.Single().MemberNames.Single());

            // Next we'll test that when validation errors are cleared on a root instance, results are cleared
            // recursively for elements in CT collections as well
            ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent_Scenarios> parentSet = ec.AddEntitySet<ComplexType_Parent_Scenarios>(EntitySetOperations.All);
            ComplexType_Parent_Scenarios p = new ComplexType_Parent_Scenarios
            {
                ID = 1,
                P1 = CreateRecursiveHierarchy("1"),
                P2 = new List<ComplexType_Recursive> {
                    CreateRecursiveHierarchy("2"),
                    CreateRecursiveHierarchy("3"),
                    CreateRecursiveHierarchy("4")
                }
            };

            // add some validation errors to some CT collection members
            ComplexType_Recursive child = p.P2.First();
            child.ValidationErrors.Add(new ValidationResult("Invalid"));
            Assert.IsTrue(child.HasValidationErrors);
            ComplexType_Recursive grandChild = child.P3.First();
            grandChild.ValidationErrors.Add(new ValidationResult("Invalid"));
            Assert.IsTrue(grandChild.HasValidationErrors);

            p.ValidationErrors.Clear();
            Assert.IsFalse(p.HasValidationErrors);
            Assert.IsFalse(child.HasValidationErrors);
            Assert.IsFalse(grandChild.HasValidationErrors);
        }

        /// <summary>
        /// Ensure that when the validation errors are added manually to an instance,
        /// the change is reflected throughout the hierarchy
        /// </summary>
        [TestMethod]
        public void ComplexType_ValidationCollection_Add()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p1 = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p1.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p1 });

            Assert.AreEqual(0, p1.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);

            ValidationResult stateError = new ValidationResult("Invalid State!", new string[] { "State" });
            p1.ContactInfo.HomeAddress.ValidationErrors.Add(stateError);

            Assert.AreEqual(1, p1.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);

            Assert.AreEqual(1, p1.ValidationErrors.Count);
            Assert.AreEqual("ContactInfo.HomeAddress.State", p1.ValidationErrors.Single().MemberNames.Single());
            Assert.AreEqual(1, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual("HomeAddress.State", p1.ContactInfo.ValidationErrors.Single().MemberNames.Single());
            Assert.AreEqual(1, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual("State", p1.ContactInfo.HomeAddress.ValidationErrors.Single().MemberNames.Single());
            Assert.AreEqual(0, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);
        }

        /// <summary>
        /// Ensure that when the validation errors are removed manually from an instance,
        /// the change is reflected throughout the hierarchy
        /// </summary>
        [TestMethod]
        public void ComplexType_ValidationCollection_Remove()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p1 = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p1.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p1 });

            // Clear from root entity - expect errors for all children to be cleared
            TestHelperMethods.EnableValidation(p1, false);
            p1.ContactInfo.HomeAddress.State = "Invalid";
            p1.ContactInfo.PrimaryPhone.AreaCode = "Invalid";
            TestHelperMethods.EnableValidation(p1, true);
            Assert.IsFalse(ec.GetChanges().Validate(null));
            Assert.AreEqual(2, p1.ValidationErrors.Count);
            Assert.AreEqual(2, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);

            // attempt to remove an item not in the collection - expect no change
            ValidationResult result = new ValidationResult("DNE", new string[] { "DNE" });
            p1.ContactInfo.PrimaryPhone.ValidationErrors.Remove(result);
            Assert.AreEqual(2, p1.ValidationErrors.Count);
            Assert.AreEqual(2, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);

            // remove an error
            result = p1.ContactInfo.PrimaryPhone.ValidationErrors.Single();
            p1.ContactInfo.PrimaryPhone.ValidationErrors.Remove(result);

            Assert.AreEqual(1, p1.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(1, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);

            // remove the last error
            result = p1.ContactInfo.HomeAddress.ValidationErrors.Single();
            p1.ContactInfo.HomeAddress.ValidationErrors.Remove(result);

            Assert.AreEqual(0, p1.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.HomeAddress.ValidationErrors.Count);
            Assert.AreEqual(0, p1.ContactInfo.PrimaryPhone.ValidationErrors.Count);
        }

        /// <summary>
        /// Test verifying that complex type instances cannot be shared between entities.
        /// </summary>
        [TestMethod]
        public void ComplexType_SharedInstances()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p1 = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p1.ContactInfo = contact;
            ComplexType_Parent p2 = new ComplexType_Parent { ID = 2 };
            ec.LoadEntities(new Entity[] { p1, p2 });

            // attempt to share the instance
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                p2.ContactInfo = contact;
            }, Resource.ComplexType_InstancesCannotBeShared);

            // attempt to share a nested instance
            ContactInfo contact2 = CreateContact();
            p2.ContactInfo = contact2;
            ((IChangeTracking)p2).AcceptChanges();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                p2.ContactInfo.PrimaryPhone = p1.ContactInfo.PrimaryPhone;
            }, Resource.ComplexType_InstancesCannotBeShared);

            // verify the value was NOT assigned
            Assert.AreEqual(EntityState.Unmodified, p2.EntityState);
            Assert.AreSame(contact2, p2.ContactInfo);

            // set up sharing BEFORE attach and verify error is still caught immediately
            // on assignment
            ec.Clear();
            p1 = new ComplexType_Parent { ID = 1 };
            p1.ContactInfo = CreateContact();
            p2 = new ComplexType_Parent { ID = 2 };
            p2.ContactInfo = CreateContact();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                p1.ContactInfo.HomeAddress = p2.ContactInfo.HomeAddress;
            }, Resource.ComplexType_InstancesCannotBeShared);

            // test entity with two different CT members pointing to the same CT.
            // should catch sharing in that case as well
            ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent_Scenarios> es2 = ec.AddEntitySet<ComplexType_Parent_Scenarios>(EntitySetOperations.All);
            ComplexType_Parent_Scenarios ps1 = new ComplexType_Parent_Scenarios();
            ComplexType_Parent_Scenarios ps2 = new ComplexType_Parent_Scenarios();
            ec.LoadEntities(new Entity[] { ps1, ps2 });
            ComplexType_Recursive shared = new ComplexType_Recursive();
            ps1.P1 = shared;
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ps1.P4 = shared;
            }, Resource.ComplexType_InstancesCannotBeShared);

            // assigning the same instance to the same entity property multiple times should work fine
            // Here we have to set specially, since the default property setter won't allow the
            // same instance to be reassigned (it'll noop)
            ps1.SetP1WithoutValueCheck(ps1.P1);
            ps1.SetP1WithoutValueCheck(ps1.P1);
            ps1.SetP1WithoutValueCheck(ps1.P1);
            ps1.P1 = ps1.P1;
            ps1.P1 = ps1.P1;
            ps1.P1 = ps1.P1;
        }

        /// <summary>
        /// Verify LoadBehavior for ComplexTypes
        /// </summary>
        [TestMethod]
        public void ComplexType_MergeBehavior()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);

            Func<ComplexType_Parent> createParent = 
                delegate {  
                    return new ComplexType_Parent
                    {
                        ID = 1,
                        ContactInfo =
                            new ContactInfo
                            {
                                Name = "Mathew",
                                HomeAddress = new Address { AddressLine1 = "47 South Wynn Rd.", City = "Oregon", State = "OH", Zip = "43616" },
                                PrimaryPhone = new Phone { AreaCode = "419", Number = "693-6096" }
                            },
                    };
            };
                                                      
            ComplexType_Parent p1 = createParent();

            ComplexType_Parent p2 = new ComplexType_Parent
            {
                ID = 1,
                ContactInfo =
                    new ContactInfo
                    {
                        Name = "XXX",
                        HomeAddress = new Address { AddressLine1 = "XXX", City = "XXX", State = "XX", Zip = "XXXXX" },
                        PrimaryPhone = null // verify reference is also merged
                    },
            };

            // test LoadBehavior.RefreshCurrent
            ec.LoadEntities(new Entity[] { p1 });
            ec.LoadEntities(new Entity[] { p2 }, LoadBehavior.RefreshCurrent);
            Assert.IsTrue(DeepCompare(p1, p2));

            // test LoadBehavior.RefreshCurrent
            ec.Clear();
            p1 = createParent();
            ec.LoadEntities(new Entity[] { p1 });
            ec.LoadEntities(new Entity[] { p2 }, LoadBehavior.KeepCurrent);
            Assert.IsTrue(DeepCompare(p1, createParent()));

            // test LoadBehavior.MergeIntoCurrent
            ec.Clear();
            p1 = createParent();
            ec.LoadEntities(new Entity[] { p1 });

            // modify a few values (both a simple member as well as a reference)
            p1.ContactInfo.HomeAddress.Zip = "98029";
            p1.ContactInfo.PrimaryPhone = new Phone { AreaCode = "YYY", Number = "YYY-YYYY" };
            Assert.AreEqual(EntityState.Modified, p1.EntityState);
            
            ec.LoadEntities(new Entity[] { p2 }, LoadBehavior.MergeIntoCurrent);
            Assert.AreEqual("98029", p1.ContactInfo.HomeAddress.Zip);  // modified value NOT merged
            Assert.AreEqual("XX", p1.ContactInfo.HomeAddress.State);   // modified value merged
            Assert.AreEqual("YYY", p1.ContactInfo.PrimaryPhone.AreaCode); // modified reference NOT merged
        }

        /// <summary>
        /// Verify that RTO specifications are respected for CTs.
        /// </summary>
        [TestMethod]
        public void ComplexType_RoundtripOriginal()
        {
            ComplexRTOA a = new ComplexRTOA
            {
                NonRoundtrippedSimple = "X",
                RoundtrippedSimple = "X",
                NonRoundtrippedCTSingleton = new ComplexRTOB { NonRoundtrippedSimple = "X", RoundtrippedSimple = "X" },
                RoundtrippedCTSingleton = new ComplexRTOB { NonRoundtrippedSimple = "X", RoundtrippedSimple = "X" },
                NonRoundtrippedCTCollection = new List<ComplexRTOB> { new ComplexRTOB { NonRoundtrippedSimple = "X", RoundtrippedSimple = "X" } },
                RoundtrippedCTCollection = new List<ComplexRTOB> { new ComplexRTOB { NonRoundtrippedSimple = "X", RoundtrippedSimple = "X" } }
            };
            ComplexRTOParent entity = new ComplexRTOParent { ID = 1, CTMember = a };

            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexRTOParent> es = ec.AddEntitySet<ComplexRTOParent>(EntitySetOperations.All);
            ec.LoadEntities(new Entity[] { entity });

            entity.Prop += 1;  // make the entity dirty

            ChangeSetEntry cse = ChangeSetBuilder.Build(ec.GetChanges()).Single();

            Assert.IsTrue(entity.MetaType.ShouldRoundtripOriginal);
            ComplexRTOParent original = (ComplexRTOParent)cse.OriginalEntity;

            // verify direct simple properties
            Assert.AreEqual("X", original.CTMember.RoundtrippedSimple);
            Assert.IsNull(original.CTMember.NonRoundtrippedSimple);

            // verify nested CT properties through singletons
            Assert.AreEqual("X", original.CTMember.RoundtrippedCTSingleton.RoundtrippedSimple);
            Assert.IsNull(original.CTMember.RoundtrippedCTSingleton.NonRoundtrippedSimple);

            // verify nested CT properties through collections
            Assert.IsTrue(original.CTMember.RoundtrippedCTCollection.All(p => p.RoundtrippedSimple == "X"));
            Assert.IsTrue(original.CTMember.RoundtrippedCTCollection.All(p => p.NonRoundtrippedSimple == null));

            // verify non roundtripped CT members
            Assert.IsNull(original.CTMember.NonRoundtrippedCTSingleton);
            Assert.IsNull(original.CTMember.NonRoundtrippedCTCollection);
        }

        /// <summary>
        /// E2E query test returning an entity with CT members
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ComplexType_QueryTest()
        {
            ComplexTypes_TestContext ctxt = new ComplexTypes_TestContext(TestURIs.ComplexTypes_TestService);

            var query = ctxt.GetParentsQuery();

            LoadOperation lo = ctxt.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(lo.HasError);
                Assert.AreEqual(3, lo.Entities.Count());

                ComplexType_Parent p = ctxt.ComplexType_Parents.First();
                Assert.IsNotNull(p.ContactInfo);
                Assert.IsNotNull(p.ContactInfo.HomeAddress);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// E2E invoke test for a method taking and returning CTs
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ComplexType_InvokeTest()
        {
            ComplexTypes_TestContext ctxt = new ComplexTypes_TestContext(TestURIs.ComplexTypes_TestService);

            ContactInfo contact = CreateContact();

            InvokeOperation<Address> io = ctxt.ReturnHomeAddress(contact);

            EnqueueConditional(delegate
            {
                return io.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(io.HasError);
                Address returnAddress = io.Value;
                Assert.IsTrue(DeepCompare(contact.HomeAddress, returnAddress));
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify an invalid CT can be sent as a parameter and returned, verifying validation isn't performed
        /// during deserialization.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ComplexType_RoundtripInvalidCT()
        {
            ComplexTypes_TestContext ctxt = new ComplexTypes_TestContext(TestURIs.ComplexTypes_TestService);

            Address address = new Address { AddressLine1 = "47 South Wynn Rd.", City = "Oregon", State = "OH" };
            TestHelperMethods.EnableValidation(address, false);
            address.State = "Invalid";
            TestHelperMethods.EnableValidation(address, true);

            InvokeOperation<Address> io = ctxt.RoundtripAddress(address);

            EnqueueConditional(delegate
            {
                return io.IsComplete;
            });
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(io);

                Address returnAddress = io.Value;
                Assert.IsFalse(returnAddress.HasValidationErrors);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// E2E test verifying that an entity with CT members can be updated
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ComplexType_UpdateTest()
        {
            ComplexTypes_TestContext ctxt = new ComplexTypes_TestContext(TestURIs.ComplexTypes_TestService);

            var query = ctxt.GetParentsQuery().Where(p => p.ContactInfo.Name == "Mathew");

            ComplexType_Parent parent = null;
            SubmitOperation so = null;
            LoadOperation lo = ctxt.Load(query, false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(lo.HasError);

                parent = ctxt.ComplexType_Parents.First();
                parent.ContactInfo.PrimaryPhone.AreaCode = "425";
                Assert.AreEqual(EntityState.Modified, parent.EntityState);

                so = ctxt.SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// E2E test verifying that an auto-sync works for CT members updated on the server
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ComplexType_TestMemberAutosync()
        {
            ComplexTypes_TestContext ctxt = new ComplexTypes_TestContext(TestURIs.ComplexTypes_TestService);

            Phone newPhone = new Phone { AreaCode = "111", Number = "999-9999" };
            SubmitOperation so = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(lo.HasError);

                ComplexType_Parent parent = ctxt.ComplexType_Parents.Single(p => p.ContactInfo.Name == "Mathew");
                parent.TestAutoSync(null);
                Assert.AreEqual(EntityState.Modified, parent.EntityState);

                parent = ctxt.ComplexType_Parents.Single(p => p.ContactInfo.Name == "Amy");
                parent.TestAutoSync(newPhone);
                Assert.AreEqual(EntityState.Modified, parent.EntityState);

                so = ctxt.SubmitChanges();
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(so.HasError);

                ComplexType_Parent parent = ctxt.ComplexType_Parents.Single(p => p.ContactInfo.Name == "Mathew");
                Assert.AreEqual("Updated", parent.ContactInfo.HomeAddress.AddressLine2);
                Assert.AreEqual(null, parent.ContactInfo.PrimaryPhone);

                parent = ctxt.ComplexType_Parents.Single(p => p.ContactInfo.Name == "Amy");
                Assert.AreEqual("Updated", parent.ContactInfo.HomeAddress.AddressLine2);
                Assert.IsTrue(DeepCompare(parent.ContactInfo.PrimaryPhone, newPhone));
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test complex type determination and MetaType handling
        /// </summary>
        [TestMethod]
        public void ComplexType_MetaType()
        {
            MetaType ct = MetaType.GetMetaType(typeof(Address));
            Assert.IsTrue(ct.IsComplex);

            ct = MetaType.GetMetaType(typeof(ContactInfo));
            Assert.IsTrue(ct.IsComplex);
            Assert.IsTrue(ct.RequiresValidation);

            // verify the ComplexType meta members
            MetaType mt = MetaType.GetMetaType(typeof(ComplexType_Parent));
            MetaMember mm = mt["ContactInfo"];
            Assert.IsTrue(mm.IsComplex);

            // verify CT collections also show up as CT data members
            mt = MetaType.GetMetaType(typeof(ComplexType_Recursive));
            mm = mt["P3"];
            Assert.AreSame(typeof(IEnumerable<ComplexType_Recursive>), mm.Member.PropertyType);
            Assert.IsTrue(mm.IsComplex);
            Assert.IsTrue(mm.IsCollection);

            // verify CT type validation does not influence property validation
            mt = MetaType.GetMetaType(typeof(ComplexType_PropertyValidationOnly));
            string result = mt.DataMembers.Where(m => m.RequiresValidation).Aggregate<MetaMember,string>(string.Empty, (s, m) => s + " " + m.Member.Name);
            Assert.AreEqual(" ValidationComplexObject ValidationInt", result);

            // negative tests for complex type determination
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(Cities.City)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(object)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(string)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(int)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(DateTime)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(DateTimeOffset)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(MethodInfo)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(NC2)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(NC3<int>)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(NC4)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(NC5)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(NC6)));
            Assert.IsFalse(TypeUtility.IsComplexType(typeof(C1)));

            // positive tests for complex type determination
            Assert.IsTrue(TypeUtility.IsComplexType(typeof(Address)));
            Assert.IsTrue(TypeUtility.IsComplexType(typeof(ContactInfo)));

            Assert.IsFalse(TypeUtility.IsComplexTypeCollection(typeof(Dictionary<string, Phone>)));
            Assert.IsFalse(TypeUtility.IsSupportedComplexType(typeof(Dictionary<string, Phone>)));
        }

        /// <summary>
        /// Verify entity doesn't raise property change notifications for nested CT changes, but does if the
        /// top level CT property is changed
        /// </summary>
        [TestMethod]
        public void ComplexType_Notifications()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ec.LoadEntities(new Entity[] { p });

            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;

            int propertyChangedCount = 0;
            ((INotifyPropertyChanged)p).PropertyChanged += (s, e) =>
                {
                    propertyChangedCount++;
                };

            p.ContactInfo.HomeAddress.AddressLine1 += "x";
            Assert.AreEqual(0, propertyChangedCount);

            p.ContactInfo = new ContactInfo { Name = "Mathew" };
            Assert.AreEqual(1, propertyChangedCount);
        }

        /// <summary>
        /// Verify that Entity.Extract state recursively extracts state for CT members
        /// as well.
        /// </summary>
        [TestMethod]
        public void ComplexType_ExtractState()
        {
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;

            IDictionary<string, object> state = p.ExtractState();
            Assert.AreEqual(1, state["ID"]);

            IDictionary<string, object> contactState = (IDictionary<string, object>)state["ContactInfo"];
            Assert.AreEqual("Mathew", contactState["Name"]);

            IDictionary<string, object> addressState = (IDictionary<string, object>)contactState["HomeAddress"];
            Assert.AreEqual("47 South Wynn Rd.", addressState["AddressLine1"]);
            Assert.AreEqual("Oregon", addressState["City"]);
        }

        /// <summary>
        /// Verify that the GetRoundtripState method used during changeset construction works properly
        /// </summary>
        [TestMethod]
        [WorkItem(187169)]
        public void ComplexType_GetRoundtripState()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ec.AddEntitySet<ComplexType_Parent_Scenarios>(EntitySetOperations.All);
            ComplexType_Parent e1 = new ComplexType_Parent() { ID = 1 };
            e1.ContactInfo = CreateContact();

            e1.ContactInfo.PrimaryPhone = null;   // this induces the bug

            ComplexType_Parent_Scenarios e2 = new ComplexType_Parent_Scenarios
            {
                ID = 1,
                P1 = CreateRecursiveHierarchy("1"),
                P2 = null  // create a null original collection
            };

            ComplexType_Parent_Scenarios e3 = new ComplexType_Parent_Scenarios
            {
                ID = 2,
                P1 = CreateRecursiveHierarchy("2"),
                P2 = new List<ComplexType_Recursive> {
                    CreateRecursiveHierarchy("2"),
                    CreateRecursiveHierarchy("3"),
                    CreateRecursiveHierarchy("4")
                }
            };

            // create a null CT original member in a CT collection element
            e3.P2.ElementAt(2).P2 = null;

            ec.LoadEntities(new Entity[] { e1, e2, e3 });

            // dirty all the entities
            e1.ContactInfo.HomeAddress.AddressLine1 += "x";
            e2.P1.P1 += "x";
            e3.P1.P1 += "x";

            var rtoState = ObjectStateUtility.ExtractRoundtripState(typeof(ComplexType_Parent), e1.GetOriginal().ExtractState());
            Assert.IsNotNull(rtoState);

            rtoState = ObjectStateUtility.ExtractRoundtripState(typeof(ComplexType_Parent_Scenarios), e2.GetOriginal().ExtractState());
            Assert.IsNotNull(rtoState);

            rtoState = ObjectStateUtility.ExtractRoundtripState(typeof(ComplexType_Parent_Scenarios), e3.GetOriginal().ExtractState());
            Assert.IsNotNull(rtoState);

            var rtoEntity = ChangeSetBuilder.GetRoundtripEntity(e1);
            Assert.IsNotNull(rtoEntity);

            rtoEntity = ChangeSetBuilder.GetRoundtripEntity(e3);
            Assert.IsNotNull(rtoEntity);
        }

        /// <summary>
        /// Verify that cycles are detected during extract state as well as when attempting to create
        /// cycles by setting CT members.
        /// </summary>
        [TestMethod]
        public void ComplexType_ExtractState_CycleDetection()
        {
            ComplexType_Recursive ct = new ComplexType_Recursive { P1 = "1" };
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ct.P2 = ct;  // attempt to point to self
            }, Resource.CyclicReferenceError);

            // attempt to create a cyclic chain
            ct = new ComplexType_Recursive { P1 = "1" };
            ct.P2 = new ComplexType_Recursive();
            ct.P2.P2 = new ComplexType_Recursive();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ct.P2.P2.P2 = ct;
            }, Resource.CyclicReferenceError);

            // bypass the setter to force the cycle and test extract state
            ct.P2.P2.P2.SetP2Direct(ct);
            ComplexType_Parent_Scenarios parent = new ComplexType_Parent_Scenarios { ID = 1 };
            parent.P1 = ct;

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                parent.ExtractState();
            }, Resource.CyclicReferenceError);
        }

        /// <summary>
        /// Verify state extraction for a recursive CT, which also includes collections of CTs
        /// </summary>
        [TestMethod]
        public void ComplexType_ExtractState_Recursive()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent_Scenarios> es = ec.AddEntitySet<ComplexType_Parent_Scenarios>(EntitySetOperations.All);
            ComplexType_Parent_Scenarios p = new ComplexType_Parent_Scenarios
            {
                ID = 1,
                P1 = CreateRecursiveHierarchy("1"),
                P2 = new List<ComplexType_Recursive> {
                    CreateRecursiveHierarchy("2"),
                    CreateRecursiveHierarchy("3"),
                    CreateRecursiveHierarchy("4")
                }
            };
            ec.LoadEntities(new Entity[] { p });
            IDictionary<string, object> state = p.ExtractState();
            Assert.AreEqual(1, state["ID"]);

            // verify the nested CT collection was extracted as collection instance (not a state dictionary)
            List<ComplexType_Recursive> ctCollection = (List<ComplexType_Recursive>)state["P2"];
            Assert.AreEqual(3, ctCollection.Count);
            Assert.AreEqual("2", ctCollection[0].P1);
            Assert.AreEqual("3", ctCollection[1].P1);
            Assert.AreEqual("4", ctCollection[2].P1);

            // verify the nested CT was extracted properly
            IDictionary<string, object> ctState = (IDictionary<string, object>)state["P1"];
            Assert.AreEqual("1", ctState["P1"]);
            Assert.AreEqual("1_P2", ((IDictionary<string, object>)ctState["P2"])["P1"]);
            ctCollection = (List<ComplexType_Recursive>)ctState["P3"];
            Assert.AreEqual(3, ctCollection.Count);
            Assert.AreEqual("1_P3_1", ctCollection[0].P1);
            Assert.AreEqual("1_P3_2", ctCollection[1].P1);
            Assert.AreEqual("1_P3_3", ctCollection[2].P1);
        }

        /// <summary>
        /// Verify that Entity.ApplyState recursively applies CT state
        /// </summary>
        [TestMethod]
        public void ComplexType_ApplyState()
        {
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;

            IDictionary<string, object> newState = p.ExtractState();

            ComplexType_Parent p2 = new ComplexType_Parent();
            p2.ApplyState(newState);

            Assert.AreEqual(1, p2.ID);
            Assert.AreEqual("Mathew", p2.ContactInfo.Name);
            Assert.AreEqual("47 South Wynn Rd.", p2.ContactInfo.HomeAddress.AddressLine1);
            Assert.AreEqual("Oregon", p2.ContactInfo.HomeAddress.City);
        }

        /// <summary>
        /// Verify Entity.ApplyState for a recursive CT, which includes CT collection members.
        /// </summary>
        [TestMethod]
        public void ComplexType_ApplyState_Recursive()
        {
            // Do a test with a recursive CT hierarchy including collections of CTs
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent_Scenarios> es = ec.AddEntitySet<ComplexType_Parent_Scenarios>(EntitySetOperations.All);
            ComplexType_Parent_Scenarios p = new ComplexType_Parent_Scenarios
            {
                ID = 1,
                P1 = CreateRecursiveHierarchy("1"),
                P2 = new List<ComplexType_Recursive> {
                    CreateRecursiveHierarchy("2"),
                    CreateRecursiveHierarchy("3"),
                    CreateRecursiveHierarchy("4")
                }
            };
            ec.LoadEntities(new Entity[] { p });
            IDictionary<string, object> state = p.ExtractState();

            // verify the test function works as expected
            Assert.IsTrue(DeepCompare(p, p));
            ComplexType_Parent_Scenarios p2 = new ComplexType_Parent_Scenarios
            {
                ID = 1,
                P1 = CreateRecursiveHierarchy("1"),
                P2 = new List<ComplexType_Recursive> {
                    CreateRecursiveHierarchy("2"),
                    CreateRecursiveHierarchy("3"),
                    CreateRecursiveHierarchy("4")
                }
            };
            Assert.IsTrue(DeepCompare(p, p2));
            p2 = new ComplexType_Parent_Scenarios
            {
                ID = 1,
                P1 = CreateRecursiveHierarchy("1"),
                P2 = new List<ComplexType_Recursive> {
                    CreateRecursiveHierarchy("2"),
                    CreateRecursiveHierarchy("3"),
                    CreateRecursiveHierarchy("5")   // shouldn't match now
                }
            };
            Assert.IsFalse(DeepCompare(p, p2));

            p2 = new ComplexType_Parent_Scenarios();
            p2.ApplyState(state);
            Assert.IsTrue(DeepCompare(p, p2));

            // a few manual verifications that the nested CT was applied properly
            Assert.AreEqual(p.ID, p2.ID);
            Assert.AreEqual(p.P1.P1, p2.P1.P1);
            Assert.AreEqual(p.P1.P2.P1, p2.P1.P2.P1);
        }

        /// <summary>
        /// Verify an end to end change tracking scenario for an entity with CT members
        /// </summary>
        [TestMethod]
        public void ComplexType_ChangeTracking()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            ContactInfo originalContact = CreateContact();
            p.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p });

            // change a nested CT member
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            p.ContactInfo.HomeAddress.AddressLine1 += "x";
            Assert.AreEqual(EntityState.Modified, p.EntityState);

            // reject and verify
            ((IRevertibleChangeTracking)p).RejectChanges();
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            Assert.AreEqual("47 South Wynn Rd.", p.ContactInfo.HomeAddress.AddressLine1);
            Assert.IsTrue(DeepCompare(p.ContactInfo, originalContact));

            // set the CT member to a new instance
            p.ContactInfo = new ContactInfo { Name = "Marty", HomeAddress = new Address { AddressLine1 = "123 Maple St." } };
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            ((IRevertibleChangeTracking)p).RejectChanges();
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            Assert.AreEqual("47 South Wynn Rd.", p.ContactInfo.HomeAddress.AddressLine1);
            Assert.IsTrue(DeepCompare(p.ContactInfo, originalContact));

            // verify that when a nested instance is set to null and a new one is attached
            // tracking works as expected
            // (Previously there was a bug here when the tracker detached from the nulled out instance)
            p.ContactInfo = CreateContact();
            ((IRevertibleChangeTracking)p).AcceptChanges();
            p.ContactInfo.HomeAddress.AddressLine1 += "x";
            p.ContactInfo.PrimaryPhone = null;
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            ((IRevertibleChangeTracking)p).RejectChanges();
            p.ContactInfo.HomeAddress.AddressLine1 += "x";
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            ((IRevertibleChangeTracking)p).RejectChanges();

            // set non-null CT members to null and verify the entity is modified
            p.ContactInfo = null;
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            ((IRevertibleChangeTracking)p).RejectChanges();
            Assert.IsTrue(DeepCompare(p.ContactInfo, originalContact));

            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            p.ContactInfo.HomeAddress = null;
            Assert.AreEqual(EntityState.Modified, p.EntityState);
        }

        /// <summary>
        /// Verify that CT members behave the same way as top level entity members
        /// when the entity cannot be edited.
        /// </summary>
        [TestMethod]
        [WorkItem(186637)]
        public void ComplexType_ReadOnlyVerification()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.None);
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p });

            // modifying a top level property should result in an exception
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                p.ContactInfo = null;
            });

            // modifying nested CT members should result in the same behavior
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                p.ContactInfo.PrimaryPhone.Number += "x";
            });

            // Actual bug repro : To truely simulate a load operation, we need to detach
            // then reload
            es.Detach(p);
            ec.LoadEntities(new Entity[] { p });
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                p.ContactInfo = null;
            });
            ExceptionHelper.ExpectException<NotSupportedException>(delegate
            {
                p.ContactInfo.PrimaryPhone.Number += "x";
            });
        }

        /// <summary>
        /// Verify CT members participate in edit sessions
        /// </summary>
        [TestMethod]
        public void ComplexType_EditSession_RootEntity()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p });

            // cancel edit
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            ((IEditableObject)p).BeginEdit();
            p.ContactInfo.HomeAddress.AddressLine1 += "x";  // simple property change
            p.ContactInfo.PrimaryPhone = null;  // reference change
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            ((IEditableObject)p).CancelEdit();
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            Assert.AreEqual("47 South Wynn Rd.", p.ContactInfo.HomeAddress.AddressLine1);
            Assert.AreEqual("693-6096", p.ContactInfo.PrimaryPhone.Number);

            // end edit
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            ((IEditableObject)p).BeginEdit();
            p.ContactInfo.HomeAddress.AddressLine1 += "x";
            p.ContactInfo.PrimaryPhone = null;  // reference change
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            ((IEditableObject)p).EndEdit();
            Assert.AreEqual(EntityState.Modified, p.EntityState);
            Assert.AreEqual("47 South Wynn Rd.x", p.ContactInfo.HomeAddress.AddressLine1);
            Assert.IsNull(p.ContactInfo.PrimaryPhone);
        }

        /// <summary>
        /// Verify CT edit session behavior
        /// </summary>
        [TestMethod]
        public void ComplexType_EditSession()
        {
            ContactInfo original = CreateContact();
            ContactInfo contact = CreateContact();

            // verify cancel edit
            ((IEditableObject)contact).BeginEdit();
            Assert.IsTrue(contact.IsEditing);
            contact.PrimaryPhone.Number += "1";
            contact.Name += "x";
            contact.HomeAddress.City = "Madrid";
            ((IEditableObject)contact).CancelEdit();
            Assert.IsFalse(contact.IsEditing);
            Assert.IsTrue(DeepCompare(contact, original));

            // verify end edit
            ((IEditableObject)contact).BeginEdit();
            Assert.IsTrue(contact.IsEditing);
            contact.PrimaryPhone.Number = "999-9999";
            contact.Name = "Nine";
            contact.HomeAddress.City = "Niner";
            ((IEditableObject)contact).EndEdit();
            Assert.IsFalse(contact.IsEditing);
            Assert.AreEqual("999-9999", contact.PrimaryPhone.Number);
            Assert.AreEqual("Nine", contact.Name);
            Assert.AreEqual("Niner", contact.HomeAddress.City);

            // verify EndEdit when not editing is a noop
            Assert.IsFalse(contact.IsEditing);
            ((IEditableObject)contact).EndEdit();
            ((IEditableObject)contact).EndEdit();

            // verify multiple BeginEdits noop
            ((IEditableObject)contact).BeginEdit();
            ((IEditableObject)contact).BeginEdit();
            ((IEditableObject)contact).CancelEdit();
        }

        /// <summary>
        /// Verify that when validation errors are pushed into an entity,
        /// nested validation errors are pushed into their corresponding CTs
        /// </summary>
        [TestMethod]
        public void ComplexType_ServerValidationPush()
        {
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;

            List<ValidationResult> errors = new List<ValidationResult>
            {
                new ValidationResult("ContactInfo", new string[] {"ContactInfo"}),
                new ValidationResult("ContactInfo.Name", new string[] {"ContactInfo.Name"}),
                new ValidationResult("ContactInfo.HomeAddress.City", new string[] {"ContactInfo.HomeAddress.City"}),
                new ValidationResult("ContactInfo.HomeAddress.State", new string[] {"ContactInfo.HomeAddress.State"})
            };

            ValidationUtilities.ApplyValidationErrors(p, errors);

            Assert.IsTrue(p.HasValidationErrors);
            Assert.AreEqual(4, p.ValidationErrors.Count);
            ValidationResult result = p.ValidationErrors.Single(q => q.MemberNames.Single() == "ContactInfo");
            Assert.AreEqual("ContactInfo", result.ErrorMessage);
            result = p.ValidationErrors.Single(q => q.MemberNames.Single() == "ContactInfo.Name");
            Assert.AreEqual("ContactInfo.Name", result.ErrorMessage);
            result = p.ValidationErrors.Single(q => q.MemberNames.Single() == "ContactInfo.HomeAddress.City");
            Assert.AreEqual("ContactInfo.HomeAddress.City", result.ErrorMessage);
            result = p.ValidationErrors.Single(q => q.MemberNames.Single() == "ContactInfo.HomeAddress.State");
            Assert.AreEqual("ContactInfo.HomeAddress.State", result.ErrorMessage);

            Assert.IsTrue(p.ContactInfo.HasValidationErrors);
            Assert.AreEqual(3, p.ContactInfo.ValidationErrors.Count);
            Assert.IsNull(p.ContactInfo.ValidationErrors.SingleOrDefault(q => q.MemberNames.Contains("ContactInfo")), "The ContactInfo error should not be pushed down to the Contact instance.");
            result = p.ContactInfo.ValidationErrors.Single(q => q.MemberNames.Single() == "Name");
            Assert.AreEqual("ContactInfo.Name", result.ErrorMessage);
            result = p.ContactInfo.ValidationErrors.Single(q => q.MemberNames.Single() == "HomeAddress.City");
            Assert.AreEqual("ContactInfo.HomeAddress.City", result.ErrorMessage);
            result = p.ContactInfo.ValidationErrors.Single(q => q.MemberNames.Single() == "HomeAddress.State");
            Assert.AreEqual("ContactInfo.HomeAddress.State", result.ErrorMessage);

            Assert.IsTrue(p.ContactInfo.HomeAddress.HasValidationErrors);
            Assert.AreEqual(2, p.ContactInfo.HomeAddress.ValidationErrors.Count);
            result = p.ContactInfo.HomeAddress.ValidationErrors.Single(q => q.MemberNames.Single() == "City");
            Assert.AreEqual("ContactInfo.HomeAddress.City", result.ErrorMessage);
            result = p.ContactInfo.HomeAddress.ValidationErrors.Single(q => q.MemberNames.Single() == "State");
            Assert.AreEqual("ContactInfo.HomeAddress.State", result.ErrorMessage);

            Assert.IsFalse(p.ContactInfo.PrimaryPhone.HasValidationErrors);
            Assert.AreEqual(0, p.ContactInfo.PrimaryPhone.ValidationErrors.Count);
        }

        /// <summary>
        /// This bug was due to the fact that the fx had no way to differentiate between
        /// Type level errors and property level errors when the member name involved a
        /// path.
        /// </summary>
        [TestMethod]
        [WorkItem(191351)]
        public void ComplexType_TypeLevelErrorApplication()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);

            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            p.ContactInfo = CreateContact();

            // configure Type level validation errors for Parent.ContactInfo
            DynamicTestValidator.ForcedValidationResults.Clear();
            DynamicTestValidator.ForcedValidationResults[typeof(ContactInfo)] = new ValidationResult("Invalid");

            es.Add(p);
            EntityChangeSet cs = ec.GetChanges();
            bool isValid = cs.Validate(null);
            Assert.IsFalse(isValid);

            // We expect the member name for the type level error coming from Parent.ContactInfo to be
            // formatted "ContactInfo."
            Assert.IsTrue(p.HasValidationErrors);
            Assert.AreEqual(1, p.ValidationErrors.Count);
            ValidationResult result = p.ValidationErrors.Single(q => q.ErrorMessage == "Invalid-TypeLevel");
            Assert.AreEqual("ContactInfo.", result.MemberNames.Single());

            // On the actual source object for the type level error, the member names collection should be
            // empty, as it was originally
            Assert.IsTrue(p.ContactInfo.HasValidationErrors);
            Assert.AreEqual(1, p.ValidationErrors.Count);
            result = p.ContactInfo.ValidationErrors.Single(q => q.ErrorMessage == "Invalid-TypeLevel");
            Assert.AreEqual(0, result.MemberNames.Count());
        }

        /// <summary>
        /// Verify that if any nested CTs are currently in an edit session, a submit
        /// cannot take place
        /// </summary>
        [TestMethod]
        public void ComplexType_SubmitWithOpenEditSession()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p });

            ((IEditableObject)p.ContactInfo.HomeAddress).BeginEdit();
            p.ContactInfo.HomeAddress.AddressLine1 += "x";

            EntityChangeSet cs = ec.GetChanges();

            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                cs.Validate(null);
            }, string.Format(Resource.Entity_UncommittedChanges, p));

            ((IEditableObject)p.ContactInfo.HomeAddress).EndEdit();
            cs = ec.GetChanges();
        }

        /// <summary>
        /// Verify Entity.GetOriginal works for CT members as well
        /// </summary>
        [TestMethod]
        public void ComplexType_GetOriginalEntity()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p });

            string origAreaCode = p.ContactInfo.PrimaryPhone.AreaCode;
            p.ContactInfo.PrimaryPhone.AreaCode = "425";

            ComplexType_Parent original = (ComplexType_Parent)p.GetOriginal();
            Assert.AreEqual(1, original.ID);
            Assert.AreEqual("Mathew", original.ContactInfo.Name);
            Assert.AreEqual("47 South Wynn Rd.", original.ContactInfo.HomeAddress.AddressLine1);
            Assert.AreEqual(origAreaCode, original.ContactInfo.PrimaryPhone.AreaCode);
        }

        /// <summary>
        /// Verifies that when a CT instance with existing validation errors is assigned to a CT property,
        /// the validation errors are 'imported' into the parent with proper member path handling. Whenever
        /// a new CT instance is set, first top level property validation on the parent is run which causes
        /// all errors to be cleared for the property and property level errors to be added. Then we import any
        /// errors already existing on the CT. Deep validation is NOT rerun on assignment since CTs already validate
        /// themselves as members are set.
        /// </summary>
        [TestMethod]
        [WorkItem(190323)]
        public void ComplexType_AssignCTWithPreexistingErrors()
        {
            // Verify with Entity parent
            ComplexType_Parent parent = new ComplexType_Parent();
            ContactInfo contact = CreateContact();
            contact.ValidationErrors.Add(new ValidationResult("Invalid State", new string[] { "HomeAddress.State" }));
            contact.ValidationErrors.Add(new ValidationResult("Invalid AreaCode", new string[] { "PrimaryPhone.AreaCode" }));
            Assert.IsTrue(contact.HasValidationErrors);
            Assert.AreEqual(2, contact.ValidationErrors.Count);

            // Add an event handler that does validation - this simulates user authored
            // property setter validation
            parent.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "ContactInfo")
                {
                    // Place an existing error for the property to ensure that errors are merged
                    parent.ValidationErrors.Add(new ValidationResult("Invalid Contact", new string[] { "ContactInfo" }));
                }
            };

            // assign the instance - expect that the validation errors
            // are synced in
            parent.ContactInfo = contact;
            Assert.IsTrue(parent.HasValidationErrors);
            Assert.AreEqual(3, parent.ValidationErrors.Count);
            Assert.IsNotNull(parent.ValidationErrors.Single(p => p.MemberNames.Contains("ContactInfo")));
            Assert.IsNotNull(parent.ValidationErrors.Single(p => p.MemberNames.Contains("ContactInfo.HomeAddress.State")));
            Assert.IsNotNull(parent.ValidationErrors.Single(p => p.MemberNames.Contains("ContactInfo.PrimaryPhone.AreaCode")));

            // assign to null - expect that the nested validation errors are cleared, but the
            // property level error added above should remain.
            parent.ContactInfo = null;
            Assert.AreEqual(1, parent.ValidationErrors.Count);
            Assert.IsNotNull(parent.ValidationErrors.SingleOrDefault(p => p.MemberNames.Contains("ContactInfo")));

            // Verify with ComplexObject parent
            contact = CreateContact();
            Assert.IsFalse(contact.HasValidationErrors);
            Phone phone = new Phone { AreaCode = "419", Number = "693-6096" };
            phone.ValidationErrors.Add(new ValidationResult("Invalid AreaCode", new string[] { "AreaCode" }));

            // Add an event handler that does validation - this simulates user authored
            // property setter validation
            ((INotifyPropertyChanged)contact).PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "PrimaryPhone")
                {
                    // Place an existing error for the property to ensure that errors are merged
                    contact.ValidationErrors.Add(new ValidationResult("Invalid PrimaryPhone", new string[] { "PrimaryPhone" }));
                }
            };

            // assign the instance - expect that the validation errors
            // are synced in
            contact.PrimaryPhone = phone;
            Assert.IsTrue(contact.HasValidationErrors);
            Assert.AreEqual(2, contact.ValidationErrors.Count);
            Assert.IsNotNull(contact.ValidationErrors.Single(p => p.MemberNames.Contains("PrimaryPhone")));
            Assert.IsNotNull(contact.ValidationErrors.Single(p => p.MemberNames.Contains("PrimaryPhone.AreaCode")));

            // assign to null - expect that the nested validation errors are cleared, but the
            // property level error added above should remain.
            contact.PrimaryPhone = null;
            Assert.AreEqual(1, contact.ValidationErrors.Count);
            Assert.IsNotNull(contact.ValidationErrors.SingleOrDefault(p => p.MemberNames.Contains("PrimaryPhone")));
        }

        /// <summary>
        /// Verify that CT validation errors are reported on the top level entity for both
        /// attached AND detached entities.
        /// </summary>
        [TestMethod]
        public void ComplexType_Validation()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);

            // first validate for an attached entity
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            p.ContactInfo = CreateContact();
            ec.LoadEntities(new Entity[] { p });
            Verify_ComplexType_Validation(p);

            // validate for a removed entity validation is still performed. Even for deleted
            // entities, top level entity properties are still validated, so CT members need
            // to be as well
            p.ValidationErrors.Clear();
            es.Remove(p);
            p.ContactInfo.HomeAddress.State = "OH";
            ((IRevertibleChangeTracking)ec).AcceptChanges();
            Assert.AreEqual(0, p.ValidationErrors.Count);
            Verify_ComplexType_Validation(p);

            // now validate for a new Detached entity (validation should still be performed)
            p = new ComplexType_Parent { ID = 1 };
            p.ContactInfo = CreateContact();
            Verify_ComplexType_Validation(p);
        }

        private void Verify_ComplexType_Validation(ComplexType_Parent p)
        { 
            List<string> entityErrors = new List<string>();
            ((INotifyDataErrorInfo)p).ErrorsChanged += (s, e) =>
            {
                entityErrors.Add(e.PropertyName);
            };

            List<string> contactErrors = new List<string>();
            ((INotifyDataErrorInfo)p.ContactInfo).ErrorsChanged += (s, e) =>
            {
                contactErrors.Add(e.PropertyName);
            };

            List<string> addressErrors = new List<string>();
            ((INotifyDataErrorInfo)p.ContactInfo.HomeAddress).ErrorsChanged += (s, e) =>
            {
                addressErrors.Add(e.PropertyName);
            };

            p.ContactInfo.HomeAddress.State = "Invalid";

            // verify the errors are reported at the entity level
            Assert.AreEqual("Invalid", p.ContactInfo.HomeAddress.State);
            Assert.IsTrue(entityErrors.SequenceEqual(new string[] { "ContactInfo.HomeAddress.State" }));
            IEnumerable<ValidationResult> results = p.ValidationErrors.Where(e => e.MemberNames.Contains("ContactInfo.HomeAddress.State"));
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("The field State must be a string with a maximum length of 2.", results.Single().ErrorMessage);

            // verify the errors are reported at the Contact level
            Assert.IsTrue(p.ContactInfo.HasValidationErrors);
            Assert.IsTrue(contactErrors.SequenceEqual(new string[] { "HomeAddress.State" }));
            results = p.ContactInfo.ValidationErrors.Where(e => e.MemberNames.Contains("HomeAddress.State"));
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("The field State must be a string with a maximum length of 2.", results.Single().ErrorMessage);

            // verify the errors are reported at the Address level
            Assert.IsTrue(p.ContactInfo.HomeAddress.HasValidationErrors);
            Assert.IsTrue(addressErrors.SequenceEqual(new string[] { "State" }));
            results = p.ContactInfo.HomeAddress.ValidationErrors.Where(e => e.MemberNames.Contains("State"));
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("The field State must be a string with a maximum length of 2.", results.Single().ErrorMessage);

            // clear the error
            p.ContactInfo.HomeAddress.State = "WA";
            Assert.IsFalse(p.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HomeAddress.HasValidationErrors);
            Assert.AreEqual(0, p.ValidationErrors.Count);
            Assert.AreEqual(0, p.ContactInfo.ValidationErrors.Count);
            Assert.AreEqual(0, p.ContactInfo.HomeAddress.ValidationErrors.Count);
        }

        [TestMethod]
        public void ComplexType_Validation_ClearDependentErrors2()
        {
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;

            List<ValidationResult> errors = new List<ValidationResult>
            {
                new ValidationResult("ContactInfo", new string[] {"ContactInfo"}),
                new ValidationResult("ContactInfo.Name", new string[] {"ContactInfo.Name"}),
                new ValidationResult("ContactInfo.HomeAddress.City", new string[] {"ContactInfo.HomeAddress.City"}),
                new ValidationResult("ContactInfo.HomeAddress.State", new string[] {"ContactInfo.HomeAddress.State"}),
                new ValidationResult("ContactInfo.PrimaryPhone.AreaCode", new string[] {"ContactInfo.PrimaryPhone.AreaCode"}),
                new ValidationResult("ContactInfo.PrimaryPhone.Number", new string[] {"ContactInfo.PrimaryPhone.Number"})
            };

            ValidationUtilities.ApplyValidationErrors(p, errors);

            Assert.IsTrue(p.HasValidationErrors);
            Assert.AreEqual(6, p.ValidationErrors.Count);

            Assert.IsTrue(p.ContactInfo.HasValidationErrors);
            Assert.AreEqual(5, p.ContactInfo.ValidationErrors.Count);
            Assert.IsNull(p.ContactInfo.ValidationErrors.SingleOrDefault(q => q.MemberNames.Contains("ContactInfo")), "The ContactInfo error should not be pushed down to the Contact instance.");

            Assert.IsTrue(p.ContactInfo.HomeAddress.HasValidationErrors);
            Assert.AreEqual(2, p.ContactInfo.HomeAddress.ValidationErrors.Count);

            Assert.IsTrue(p.ContactInfo.PrimaryPhone.HasValidationErrors);
            Assert.AreEqual(2, p.ContactInfo.PrimaryPhone.ValidationErrors.Count);

            // set the phone CT member to null. We expect all dependent errors up the
            // hierarchy leading to that instance to be removed.
            p.ContactInfo.PrimaryPhone = null;

            Assert.IsTrue(p.HasValidationErrors);
            Assert.AreEqual(4, p.ValidationErrors.Count);
            Assert.IsFalse(p.ValidationErrors.SelectMany(q => q.MemberNames).Any(q => q.Contains("Phone")));

            Assert.IsTrue(p.ContactInfo.HasValidationErrors);
            Assert.AreEqual(3, p.ContactInfo.ValidationErrors.Count);
            Assert.IsFalse(p.ContactInfo.ValidationErrors.SelectMany(q => q.MemberNames).Any(q => q.Contains("Phone")));

            Assert.IsTrue(p.ContactInfo.HomeAddress.HasValidationErrors);
            Assert.AreEqual(2, p.ContactInfo.HomeAddress.ValidationErrors.Count);
        }

        /// <summary>
        /// The issue here was the framework was using Validator.ValidateObject specifying validateAllProperties = false
        /// thinking that would skip property validation entirely. However even with that specified, RequireAttributes are
        /// validated.
        /// </summary>
        [TestMethod]
        [WorkItem(186561)]
        public void ComplexType_Validation_Bug186561()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);

            // first validate for an attached entity
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            p.ContactInfo = CreateContact();
            ec.LoadEntities(new Entity[] { p });

            TestHelperMethods.EnableValidation(p, false);
            p.ContactInfo.PrimaryPhone.Number = null;
            TestHelperMethods.EnableValidation(p, true);

            EntityChangeSet cs = ec.GetChanges();
            Assert.AreEqual(0, p.ValidationErrors.Count);
            cs.Validate(ec.ValidationContext);
            ValidationResult[] results = p.ValidationErrors.ToArray();
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual("ContactInfo.PrimaryPhone.Number", results[0].MemberNames.Single());

            p.ValidationErrors.Clear();
            TestHelperMethods.EnableValidation(p, false);
            p.ContactInfo.PrimaryPhone = new Phone();
            TestHelperMethods.EnableValidation(p, true);
            Assert.AreEqual(0, p.ValidationErrors.Count);
            cs.Validate(ec.ValidationContext);
            results = p.ValidationErrors.ToArray();
            Assert.AreEqual(2, results.Length);
            IEnumerable<string> invalidMembers = results.SelectMany(q => q.MemberNames);
            Assert.IsTrue(invalidMembers.Contains("ContactInfo.PrimaryPhone.AreaCode"));
            Assert.IsTrue(invalidMembers.Contains("ContactInfo.PrimaryPhone.Number"));
        }

        /// <summary>
        /// Verify that full recursive validation is done for CTs at submit time
        /// </summary>
        [TestMethod]
        public void ComplexType_SubmitValidation_Direct()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            EntitySet<ComplexType_Parent_Scenarios> es2 = ec.AddEntitySet<ComplexType_Parent_Scenarios>(EntitySetOperations.All);

            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            TestHelperMethods.EnableValidation(p, false); // turn off validation so we can create an invalid entity
            p.ContactInfo = CreateContact();
            p.ContactInfo.HomeAddress.State = "Invalid";
            TestHelperMethods.EnableValidation(p, true);
            es.Add(p);

            DynamicTestValidator.Monitor(true);

            EntityChangeSet cs = ec.GetChanges();
            bool isValid = cs.Validate(null);
            Assert.IsFalse(isValid);

            // verify custom validation was called at CT type and property levels. Note in this case the type level
            // validation for the ContactInfo instance will not be run, since type level validation only
            // runs if there are no property level errors.
            Assert.AreEqual(2, DynamicTestValidator.ValidationCalls.Count);
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == "ContactInfo" && v.ObjectType == typeof(ComplexType_Parent)));
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == null && v.ObjectType == typeof(Phone)));

            Assert.AreEqual("Invalid", p.ContactInfo.HomeAddress.State);
            ValidationResult result = p.ValidationErrors.Single(e => e.MemberNames.Contains("ContactInfo.HomeAddress.State"));
            Assert.AreEqual("The field State must be a string with a maximum length of 2.", result.ErrorMessage);

            // Try the same test again, this time removing the property level validation error
            // so type level validation is run
            p.ContactInfo.HomeAddress.State = "OH";
            DynamicTestValidator.ValidationCalls.Clear();

            cs = ec.GetChanges();
            isValid = cs.Validate(null);
            Assert.IsTrue(isValid);

            // verify custom validation was called at CT type and property levels
            Assert.AreEqual(3, DynamicTestValidator.ValidationCalls.Count);
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == null && v.ObjectType == typeof(ContactInfo)));
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == "ContactInfo" && v.ObjectType == typeof(ComplexType_Parent)));
            Assert.IsNotNull(DynamicTestValidator.ValidationCalls.Single(v => v.MemberName == null && v.ObjectType == typeof(Phone)));

            DynamicTestValidator.Monitor(false);
            es.Remove(p);
            Assert.IsFalse(es.HasChanges);

            // verify that deep validation is done at submit time for CT collections as well
            ComplexType_Parent_Scenarios p2 = new ComplexType_Parent_Scenarios { ID = 1 };
            List<ComplexType_Recursive> children = new List<ComplexType_Recursive> {
                new ComplexType_Recursive { P1 = "1", P4 = -1 },  // invalid element
                new ComplexType_Recursive { P1 = "2", P3 = 
                    new List<ComplexType_Recursive> { 
                        new ComplexType_Recursive { P1 = "3", P4 = -5 }  // invalid element in nested collection
                    }
                }
            };
            p2.P2 = children;

            // set a singleton CT on the entity with a nested CT collection containing an
            // invalid element
            p2.P1 = new ComplexType_Recursive
            {
                P1 = "5",
                P3 =
                    new List<ComplexType_Recursive> { 
                        new ComplexType_Recursive { P1 = "5", P4 = -5 }  // invalid element in nested collection
                    }
            };

            es2.Add(p2);
            cs = ec.GetChanges();
            isValid = cs.Validate(null);
            Assert.IsFalse(isValid);
            Assert.AreEqual(3, p2.ValidationErrors.Count);
            result = p2.ValidationErrors.Single(e => e.MemberNames.Single() == "P1.P3().P4");
            Assert.AreEqual("The field P4 must be between 0 and 5.", result.ErrorMessage);
            result = p2.ValidationErrors.Single(e => e.MemberNames.Single() == "P2().P4");
            Assert.AreEqual("The field P4 must be between 0 and 5.", result.ErrorMessage);
            result = p2.ValidationErrors.Single(e => e.MemberNames.Single() == "P2().P3().P4");
            Assert.AreEqual("The field P4 must be between 0 and 5.", result.ErrorMessage);
        }

        /// <summary>
        /// Verify that member names are handled properly in validation for validation errors that
        /// specify multiple member names.
        /// </summary>
        [TestMethod]
        public void ComplexType_CustomValidator_MultipleMemberNames()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            EntitySet<ComplexType_Parent_Scenarios> es2 = ec.AddEntitySet<ComplexType_Parent_Scenarios>(EntitySetOperations.All);

            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            p.ContactInfo = CreateContact();

            // configure multi member validation errors for both Type level
            // and member level validation
            DynamicTestValidator.ForcedValidationResults.Clear();
            ValidationResult contactResult = new ValidationResult("ContactInfo", new string[] { "Name", "PrimaryPhone" });
            ValidationResult phoneResult = new ValidationResult("Phone", new string[] { "AreaCode", "Number" });
            DynamicTestValidator.ForcedValidationResults[p.ContactInfo] = contactResult;
            DynamicTestValidator.ForcedValidationResults[typeof(Phone)] = phoneResult;

            // test for submit time validation
            es.Add(p);
            EntityChangeSet cs = ec.GetChanges();
            bool isValid = cs.Validate(null);
            Assert.IsFalse(isValid);

            // First test the type level validation done. We don't expect the ContactInfo type level validation to be run,
            // since the contact instance has property validation errors. We expect path information to be appended
            // to the member names.
            Assert.AreEqual(2, p.ValidationErrors.Count);
            
            // Property level CT validation : Ensure that paths are correctly appended to the
            // member names specified.
            ValidationResult result = p.ValidationErrors.Single(q => q.ErrorMessage == "ContactInfo-ContactInfo");
            string[] memberNames = result.MemberNames.ToArray();
            Assert.AreEqual(2, memberNames.Length);
            Assert.IsTrue(memberNames.Contains("ContactInfo.Name"));
            Assert.IsTrue(memberNames.Contains("ContactInfo.PrimaryPhone"));

            // Tpye level CT validation : again, we expect member names to be transformed into full paths
            result = p.ValidationErrors.Single(q => q.ErrorMessage == "Phone-TypeLevel");
            memberNames = result.MemberNames.ToArray();
            Assert.AreEqual(2, memberNames.Length);
            Assert.IsTrue(memberNames.Contains("ContactInfo.PrimaryPhone.AreaCode"));
            Assert.IsTrue(memberNames.Contains("ContactInfo.PrimaryPhone.Number"));

            DynamicTestValidator.ForcedValidationResults.Clear();
            DynamicTestValidator.ForcedValidationResults[typeof(ContactInfo)] = contactResult;
            cs = ec.GetChanges();
            isValid = cs.Validate(null);
            Assert.IsFalse(isValid);

            Assert.AreEqual(1, p.ValidationErrors.Count);
            result = p.ValidationErrors.Single(q => q.ErrorMessage == "ContactInfo-TypeLevel");
            memberNames = result.MemberNames.ToArray();
            Assert.AreEqual(2, memberNames.Length);
            Assert.IsTrue(memberNames.Contains("ContactInfo.Name"));
            Assert.IsTrue(memberNames.Contains("ContactInfo.PrimaryPhone"));
        }

        /// <summary>
        /// E2E test verifying that client and server submit time validation function equivalently.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ComplexType_SubmitValidation_CrossTier()
        { 
            ComplexTypes_TestContext ctxt = new ComplexTypes_TestContext(TestURIs.ComplexTypes_TestService);
            Dictionary<ComplexType_Parent, ValidationResult[]> validationResults = null;
            bool submitComplete = false;
            SubmitOperation so = null;
            LoadOperation lo = ctxt.Load(ctxt.GetParentsQuery(), false);

            // This is the validation method that will be reused below for both calls
            Action<Dictionary<ComplexType_Parent, ValidationResult[]>> checkResults = (r) =>
                {
                    Assert.AreEqual(1, r.Count);
                    var item = r.Single(p => p.Key.ID == 1);
                    ComplexType_Parent entity = item.Key;
                    ValidationResult result = item.Value.Single();
                    Assert.AreEqual("The field AreaCode must be a string with a maximum length of 3.", result.ErrorMessage);
                    Assert.AreEqual("ContactInfo.PrimaryPhone.AreaCode", result.MemberNames.Single());
                };

            EnqueueConditional(delegate
            {
                return lo.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsFalse(lo.HasError);

                ComplexType_Parent[] entities = ctxt.ComplexType_Parents.ToArray();

                // create some validation errors
                ComplexType_Parent entity = entities.Single(p => p.ID == 1);
                entity.ContactInfo.HomeAddress.AddressLine1 += "x";
                TestHelperMethods.EnableValidation(entity, false);
                entity.ContactInfo.PrimaryPhone.AreaCode = "Invalid";
                TestHelperMethods.EnableValidation(entity, true);

                so = ctxt.SubmitChanges(TestHelperMethods.DefaultOperationAction, null);
            });
            EnqueueConditional(delegate
            {
                return so.IsComplete;
            });
            EnqueueCallback(delegate
            {
                Assert.IsTrue(so.HasError);
                validationResults = so.EntitiesInError.ToDictionary(p => (ComplexType_Parent)p, p => p.ValidationErrors.ToArray());
                checkResults(validationResults);

                // now submit again bypassing client validation
                TestHelperMethods.SubmitDirect(ctxt, (submitResults) =>
                {
                    validationResults = submitResults.Results.Where(p => p.ValidationErrors != null)
                        .ToDictionary(p => (ComplexType_Parent)p.Entity, p => p.ValidationErrors
                            .Select(q => new ValidationResult(q.Message, q.SourceMemberNames)).ToArray());
                    submitComplete = true;
                });
            });
            EnqueueConditional(delegate
            {
                return submitComplete;
            });
            EnqueueCallback(delegate
            {
                checkResults(validationResults);
            });
            
            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that the tracker detaches properly when entities are detached
        /// from the container.
        /// </summary>
        [TestMethod]
        public void ComplexType_TrackerDetach()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p });

            p.ContactInfo.PrimaryPhone.AreaCode = "425";
            Assert.AreEqual(EntityState.Modified, p.EntityState);

            // detach the entity and set the reference to null
            es.Detach(p);
            p.ContactInfo = null;

            // reattach and change a member on the CT instance which has been detached.
            // The entity should no longer be tracking it
            es.Attach(p);
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
            contact.PrimaryPhone.AreaCode = "419";
            Assert.AreEqual(EntityState.Unmodified, p.EntityState);
        }

        // Roundtrip values that have inheritance hierarchies on the server but not on the client.
        [TestMethod]
        [Asynchronous]
        public void ComplexType_InheritanceSerialization()
        {
            LoadOperation<ComplexTypeInheritance_EntityGrandparent> load = null;
            InvokeOperation<ComplexInheritance_Child> invoke = null;
            // Whatever inheritance the server has, the client should not see it in generated code.
            EnsureNoInheritance(typeof(ComplexInheritance_Child));

            // EntityParent should be skipped in code gen, Child should appear on the grand child.
            EnsureProperty(typeof(ComplexTypeInheritance_EntityGrandparent), "Child", false);
            EnsureProperty(typeof(ComplexTypeInheritance_EntityGrandchild), "Child", true);

            // Whatever inheritance the server has, the serialization should not be affected.
            ComplexTypes_DomainContext context = new ComplexTypes_DomainContext(TestURIs.ComplexTypes_DomainService);
            load = context.Load(context.GetStubQuery());

            this.EnqueueConditional(delegate
            {
                return load.IsComplete;
            });

            this.EnqueueCallback(delegate
            {
                EnsureOperationNoError(load);
                ComplexTypeInheritance_EntityGrandparent grandparent = load.Entities.First();
                ComplexTypeInheritance_EntityGrandchild grandchild = grandparent as ComplexTypeInheritance_EntityGrandchild;
                Assert.IsNotNull(grandchild, string.Format("Service should return known type {0}", typeof(ComplexTypeInheritance_EntityGrandchild)));
                
                // ensure complex type hanging off entity serialized correctly at the right level in the hierarchy
                Assert.AreEqual(1, grandchild.Child.A1);
                Assert.AreEqual(2, grandchild.Child.A2);
                Assert.AreEqual(10, grandchild.Child.Z1);
                Assert.AreEqual(20, grandchild.Child.Z2);

                // test round trip of complex types
                invoke = context.GetInheritedMember(new ComplexInheritance_Child()
                {
                    A1 = 100,
                    A2 = 101,
                    Z1 = 200,
                    Z2 = 201,
                });
            });

            this.EnqueueConditional(delegate
            {
                return invoke.IsComplete;
            });

            this.EnqueueCallback(delegate
            {
                EnsureOperationNoError(invoke);
                ComplexInheritance_Child returnedChild = invoke.Value;
                Assert.AreEqual(101, returnedChild.A1);
                Assert.AreEqual(102, returnedChild.A2);
                Assert.AreEqual(201, returnedChild.Z1);
                Assert.AreEqual(202, returnedChild.Z2);
            });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Test serialization in invoke and named update methods.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ComplexType_RoundtripComplexTypeParams()
        {
            ComplexTypes_DomainContext context = new ComplexTypes_DomainContext(TestURIs.ComplexTypes_DomainService);
            LoadOperation<ComplexTypeInheritance_EntityGrandparent> load = context.Load(context.GetStubQuery());
            SubmitOperation submit = null;
            InvokeOperation<ComplexInheritance_Child> invoke = null;

            this.EnqueueConditional(delegate
            {
                return load.IsComplete;
            });

            this.EnqueueCallback(delegate
            {
                EnsureOperationNoError(load);
                ComplexTypeInheritance_EntityGrandchild stub = load.Entities.First() as ComplexTypeInheritance_EntityGrandchild;
                Assert.IsNotNull(stub);

                ComplexInheritance_Child child = GetComplexChildren(1000)[0];
                ComplexInheritance_Child[] children = GetComplexChildren(2000);
                // Named update method with complex types.
                stub.ChooseHighestStubOrChild(child, children);
                submit = context.SubmitChanges();
            });

            this.EnqueueConditional(delegate
            {
                return submit.IsComplete;
            });

            this.EnqueueCallback(delegate
            {
                EnsureOperationNoError(submit);

                // Invoke methods should succeed.
                ComplexInheritance_Child child = GetComplexChildren(-1)[0];
                ComplexInheritance_Child[] children = GetComplexChildren(-2);
                invoke = context.GetHighestChild(child, children);
            });

            this.EnqueueConditional(delegate
            {
                return invoke.IsComplete;
            });

            this.EnqueueCallback(delegate
            {
                EnsureOperationNoError(invoke);

                ComplexInheritance_Child returnedChild = invoke.Value;
                Assert.AreEqual(2000, returnedChild.A1);
                Assert.AreEqual(2000, returnedChild.A2);
                Assert.AreEqual(2000, returnedChild.Z1);
                Assert.AreEqual(2000, returnedChild.Z2);
            });

            this.EnqueueTestComplete();
        }

        private static ComplexInheritance_Child[] GetComplexChildren(int value)
        {
            return new ComplexInheritance_Child[]
            {
                new ComplexInheritance_Child()
                {
                    A1 = value,
                    A2 = value,
                    Z1 = value,
                    Z2 = value,
                },
            };
        }

        private static int[] GetInts(int value)
        {
            return new int[]
            {
                value,
            };
        }

        public Phone SetAreaCode(Phone phone, string code) { return null;  }

        private ContactInfo CreateContact()
        {
            ContactInfo contact = new ContactInfo
            {
                Name = "Mathew",
                HomeAddress = new Address { AddressLine1 = "47 South Wynn Rd.", City = "Oregon", State = "OH" },
                PrimaryPhone = new Phone { AreaCode = "419", Number = "693-6096" }
            };

            return contact;
        }

        private ComplexType_Recursive CreateRecursiveHierarchy(string id)
        {
             ComplexType_Recursive ct =
                new ComplexType_Recursive
                {
                    P1 = id,
                    P2 = new ComplexType_Recursive { P1 = id + "_P2" },
                    P3 = new List<ComplexType_Recursive> {
                         new ComplexType_Recursive { P1 = id + "_P3_1" },
                         new ComplexType_Recursive { P1 = id + "_P3_2" },
                         new ComplexType_Recursive { P1 = id + "_P3_3" },
                    }
                };

            return ct;
        }

        private bool DeepCompare(object o1, object o2)
        {
            if ((o1 == null && o2 != null) ||
                (o2 == null && o1 != null))
            {
                return false;
            }

            if (o1 == null && o2 == null)
            {
                return true;
            }

            foreach (MetaMember mm in MetaType.GetMetaType(o1.GetType()).DataMembers)
            {
                object value1 = mm.GetValue(o1);
                object value2 = mm.GetValue(o2);

                if (mm.IsComplex)
                {
                    if (mm.IsCollection)
                    {
                        if ((value1 == null && value2 != null) ||
                            (value2 == null && value1 != null))
                        {
                            return false;
                        }

                        if (value1 == null && value2 == null)
                        {
                            return true;
                        }

                        object[] values1 = Enumerable.Cast<object>((IEnumerable)value1).ToArray();
                        object[] values2 = Enumerable.Cast<object>((IEnumerable)value2).ToArray();

                        if (values1.Length != values2.Length)
                        {
                            return false;
                        }
                        for (int i = 0; i < values1.Length; i++)
                        {
                            if (!DeepCompare(values1[i], values2[i]))
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        if (!DeepCompare(value1, value2))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (!object.Equals(value1, value2))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void EnsureProperty(Type type, string propertyName, bool exists)
        {
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            bool found = false;

            foreach (var property in properties)
            {
                if (property.Name == propertyName)
                {
                    found = true;
                    break;
                }
            }

            Assert.AreEqual(exists, found, string.Format("Expected property {0} {1} to exist", propertyName, exists ? "" : "not"));
        }

        private static void EnsureNoInheritance(Type complexType)
        {
            Type baseType = complexType.BaseType;
            Assert.IsTrue(baseType == typeof(ComplexObject), string.Format("Complex type '{0}' should inherit from ComplexObject, but inherits from '{1}'.", complexType, baseType));
        }

        private static void EnsureOperationNoError(OperationBase operation)
        {
            Assert.IsFalse(operation.HasError, operation.HasError ? operation.Error.ToString() : null);
        }
    }

    /// <summary>
    /// This class contains Silverlight only ComplexType tests.
    /// </summary>
    public partial class ComplexTypeTests
    {
        /// <summary>
        /// Verify INDEI events are raised as expected up the hierarchy.
        /// </summary>
        [TestMethod]
        public void ComplexType_INDEIEvents()
        {
            DynamicEntityContainer ec = new DynamicEntityContainer();
            EntitySet<ComplexType_Parent> es = ec.AddEntitySet<ComplexType_Parent>(EntitySetOperations.All);
            ComplexType_Parent p1 = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p1.ContactInfo = contact;
            ec.LoadEntities(new Entity[] { p1 });

            List<string> changedProperties = new List<string>();
            EventHandler<DataErrorsChangedEventArgs> handler = (o, e) =>
                {
                    changedProperties.Add(e.PropertyName);
                };

            ((INotifyDataErrorInfo)p1).ErrorsChanged += handler;
            ((INotifyDataErrorInfo)p1.ContactInfo).ErrorsChanged += handler;
            ((INotifyDataErrorInfo)p1.ContactInfo.HomeAddress).ErrorsChanged += handler;

            p1.ContactInfo.HomeAddress.State = "Invalid";

            Assert.IsTrue(changedProperties.SequenceEqual(new string[] { "State", "HomeAddress.State", "ContactInfo.HomeAddress.State" }));

            // Verify that unsubscribe works
            changedProperties.Clear();
            ((INotifyDataErrorInfo)p1).ErrorsChanged -= handler;
            ((INotifyDataErrorInfo)p1.ContactInfo).ErrorsChanged -= handler;
            ((INotifyDataErrorInfo)p1.ContactInfo.HomeAddress).ErrorsChanged -= handler;
            p1.ContactInfo.HomeAddress.State = "Invalid2";
            Assert.AreEqual(0, changedProperties.Count);
        }

        /// <summary>
        /// Verify that when an edit session is ended, a complex type revalidates itself and notifies its parents
        /// </summary>
        [TestMethod]
        public void ComplexType_EditSession_EndEditValidation()
        {
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            ContactInfo contact = CreateContact();
            p.ContactInfo = contact;

            ((IEditableObject)p.ContactInfo.HomeAddress).BeginEdit();

            // push an invalid entry into the nested CT
            TestHelperMethods.EnableValidation(p, false);
            p.ContactInfo.HomeAddress.State = "Invalid";
            TestHelperMethods.EnableValidation(p, true);

            // verify that when the edit session is ended, the validation
            // error shows up the hierarchy
            Assert.IsFalse(p.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HomeAddress.HasValidationErrors);
            ((IEditableObject)p.ContactInfo.HomeAddress).EndEdit();

            Action<ComplexType_Parent> verifyErrors = delegate(ComplexType_Parent entity)
            {
                Assert.IsTrue(entity.HasValidationErrors && entity.ValidationErrors.Count == 1);
                ValidationResult result = entity.ValidationErrors.Single();
                Assert.AreEqual("ContactInfo.HomeAddress.State", result.MemberNames.Single());
                Assert.IsTrue(entity.ContactInfo.HasValidationErrors && entity.ContactInfo.ValidationErrors.Count == 1);
                result = entity.ContactInfo.ValidationErrors.Single();
                Assert.AreEqual("HomeAddress.State", result.MemberNames.Single());
                Assert.IsTrue(entity.ContactInfo.HomeAddress.HasValidationErrors && entity.ContactInfo.HomeAddress.ValidationErrors.Count == 1);
                result = entity.ContactInfo.HomeAddress.ValidationErrors.Single();
                Assert.AreEqual("State", result.MemberNames.Single());
            };
            verifyErrors(p);

            // start another session then cancel it. We expect the errors to be
            // restored
            ((IEditableObject)p.ContactInfo.HomeAddress).BeginEdit();
            p.ContactInfo.HomeAddress.State = "WA";
            Assert.IsFalse(p.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HomeAddress.HasValidationErrors);
            ((IEditableObject)p.ContactInfo.HomeAddress).CancelEdit();
            Assert.AreEqual("Invalid", p.ContactInfo.HomeAddress.State);
            verifyErrors(p);

            // finally clear the error before starting an edit session, make some invalid
            // edits in the session then verify all errors are cleared when the session is
            // cancelled
            p.ContactInfo.HomeAddress.State = "WA";
            Assert.IsFalse(p.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HomeAddress.HasValidationErrors);
            ((IEditableObject)p.ContactInfo.HomeAddress).BeginEdit();
            p.ContactInfo.HomeAddress.State = "Invalid";
            verifyErrors(p);
            ((IEditableObject)p.ContactInfo.HomeAddress).CancelEdit();
            Assert.IsFalse(p.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HasValidationErrors);
            Assert.IsFalse(p.ContactInfo.HomeAddress.HasValidationErrors);
        }

        /// <summary>
        /// Verify that when a nested CT reference is set to null, all dependent validations up the
        /// containment hierarchy are cleared (meaning any errors with a path that leads to the CT
        /// member that was nulled out)
        /// </summary>
        [TestMethod]
        public void ComplexType_Validation_ClearDependentErrors()
        {
            ComplexType_Parent p = new ComplexType_Parent { ID = 1 };
            p.ContactInfo = CreateContact();

            p.ContactInfo.HomeAddress.State = "Invalid";
            Assert.IsNotNull(p.ValidationErrors.Single(q => q.MemberNames.Single() == "ContactInfo.HomeAddress.State"));
            Assert.IsNotNull(p.ContactInfo.ValidationErrors.Single(q => q.MemberNames.Single() == "HomeAddress.State"));
            Assert.IsNotNull(p.ContactInfo.HomeAddress.ValidationErrors.Single(q => q.MemberNames.Single() == "State"));

            // when the instance is cleared out, the validation errors should be removed
            p.ContactInfo.HomeAddress = null;
            Assert.IsFalse(p.HasValidationErrors);
            Assert.AreEqual(0, p.ValidationErrors.Count);
            Assert.IsFalse(p.ContactInfo.HasValidationErrors);
            Assert.AreEqual(0, p.ContactInfo.ValidationErrors.Count);
        }
    }

    public class C1 : INotifyPropertyChanged
    {
        public string P1 { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [RoundtripOriginal]
    public class ComplexRTOParent : Entity
    {
        private int _prop;

        [Key]
        public int ID { get; set; }

        public int Prop
        {
            get
            {
                return this._prop;
            }
            set
            {
                if ((this._prop != value))
                {
                    this.RaiseDataMemberChanging("Prop");
                    this.ValidateProperty("Prop", value);
                    this._prop = value;
                    this.RaiseDataMemberChanged("Prop");
                }
            }
        }

        [RoundtripOriginal]
        public ComplexRTOA CTMember { get; set; }
    }

    public class ComplexRTOA : ComplexObject
    {
        public string NonRoundtrippedSimple { get; set; }

        [RoundtripOriginal]
        public string RoundtrippedSimple { get; set; }

        public ComplexRTOB NonRoundtrippedCTSingleton { get; set; }

        [RoundtripOriginal]
        public ComplexRTOB RoundtrippedCTSingleton { get; set; }

        public IEnumerable<ComplexRTOB> NonRoundtrippedCTCollection { get; set; }

        [RoundtripOriginal]
        public IEnumerable<ComplexRTOB> RoundtrippedCTCollection { get; set; }
    }

    public class ComplexRTOB : ComplexObject
    {
        public string NonRoundtrippedSimple { get; set; }

        [RoundtripOriginal]
        public string RoundtrippedSimple { get; set; }
    }

    [RoundtripOriginal]
    public class ComplexType_Recursive : ComplexObject
    {
        private string _p1;
        private int _p4;
        private ComplexType_Recursive _p2;
        private IEnumerable<ComplexType_Recursive> _p3;

        public string P1 
        { 
            get
            {
                return this._p1;
            }
            set
            {
                if (this._p1 != value)
                {
                    this.RaiseDataMemberChanging("P1");
                    this._p1 = value;
                    this.RaiseDataMemberChanged("P1");
                }
            }
        }

        [Range(0,5)]
        public int P4
        {
            get
            {
                return this._p4;
            }
            set
            {
                if (this._p4 != value)
                {
                    this.RaiseDataMemberChanging("P4");
                    this._p4 = value;
                    this.RaiseDataMemberChanged("P4");
                }
            }
        }

        public ComplexType_Recursive P2
        {
            get
            {
                return this._p2;
            }
            set
            {
                if (this._p2 != value)
                {
                    this.RaiseDataMemberChanging("P2");
                    this._p2 = value;
                    this.RaiseDataMemberChanged("P2");
                }
            }
        }

        public void SetP2Direct(ComplexType_Recursive o)
        {
            this._p2 = o;
        }

        public IEnumerable<ComplexType_Recursive> P3
        {
            get
            {
                return this._p3;
            }
            set
            {
                if (this._p3 != value)
                {
                    this.RaiseDataMemberChanging("P3");
                    this._p3 = value;
                    this.RaiseDataMemberChanged("P3");
                }
            }
        } 

    }

    [RoundtripOriginal]
    public class ComplexType_Parent_Scenarios : Entity
    {
        private ComplexType_Recursive _p1;
        private IEnumerable<ComplexType_Recursive> _p2;
        private ComplexType_Recursive _p4;

        [Key]
        public int ID { get; set; }

        public ComplexType_Recursive P1
        {
            get
            {
                return this._p1;
            }
            set
            {
                if ((this._p1 != value))
                {
                    this.RaiseDataMemberChanging("P1");
                    this.ValidateProperty("P1", value);
                    this._p1 = value;
                    this.RaiseDataMemberChanged("P1");
                }
            }
        }

        public void SetP1Direct(ComplexType_Recursive o)
        {
            this._p1 = o;
        }

        public void SetP1WithoutValueCheck(ComplexType_Recursive o)
        {
            this.RaiseDataMemberChanging("P1");
            this._p1 = o;
            this.RaiseDataMemberChanged("P1");
        }


        public IEnumerable<ComplexType_Recursive> P2
        {
            get
            {
                return this._p2;
            }
            set
            {
                if ((this._p2 != value))
                {
                    this.RaiseDataMemberChanging("P2");
                    this.ValidateProperty("P2", value);
                    this._p2 = value;
                    this.RaiseDataMemberChanged("P2");
                }
            }
        }

        public ComplexType_Recursive P4
        {
            get
            {
                return this._p4;
            }
            set
            {
                if ((this._p4 != value))
                {
                    this.RaiseDataMemberChanging("P4");
                    this.ValidateProperty("P4", value);
                    this._p4 = value;
                    this.RaiseDataMemberChanged("P4");
                }
            }
        }
    }

    // CTs can't be private
    class NC2 : C1
    {

    }

    // CTs can't be generic
    public class NC3<T> : C1
    {
        public T P2 { get; set; }
    }

    // CTs can't be abstract
    public abstract class NC4 : C1
    {

    }

    // CTs can't be enumerable
    public class NC5 : C1, IEnumerable
    {

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    // CTs must have a public parameterless ctor
    public class NC6 : C1
    {
        public NC6(int x) { }
    }

    public class IndirectSelfReference_A : ComplexObject
    {
        public IndirectSelfReference_B B
        {
            get
            {
                return null;
            }
            set
            {

            }
        }

        [StringLength(5)]
        public string Z { get; set; }
    }

    public class IndirectSelfReference_B : ComplexObject
    {
        public IndirectSelfReference_A A
        {
            get
            {
                return null;
            }
        }
    }

    [CustomValidation(typeof(DynamicTestValidator), "Validate")]
    public class ComplexType_RequiresValidation : ComplexObject
    {
        public ComplexType_NoValidationRequired A { get; set; }
    }

    public class ComplexType_NoValidationRequired : ComplexObject
    {
    }

    public class ComplexType_PropertyValidationOnly : ComplexObject
    {
        public int NoValidationInt { get; set; }
        public ComplexType_RequiresValidation NoValidationComplexObject { get; set; }

        [CustomValidation(typeof(DynamicTestValidator), "Validate")]
        public int ValidationInt { get; set; }
        [CustomValidation(typeof(DynamicTestValidator), "Validate")]
        public ComplexType_RequiresValidation ValidationComplexObject { get; set; }
    }
}

namespace TestDomainServices
{
    public partial class Address
    {
        public Action<PropertyChangedEventArgs> TestPropertyChangedCallback;
        public Action<ValidationContext, object> TestValidatePropertyCallback;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (this.TestPropertyChangedCallback != null)
            {
                this.TestPropertyChangedCallback(e);
            }
        }

        protected override void ValidateProperty(ValidationContext validationContext, object value)
        {
            // We access the protected IsDeserializing member here to verify it is
            // accessible to derived types.
            if (this.IsDeserializing)
            {
                throw new Exception("Validation should never be called during deserialization!");
            }

            base.ValidateProperty(validationContext, value);

            if (this.TestValidatePropertyCallback != null)
            {
                this.TestValidatePropertyCallback(validationContext, value);
            }
        }
    }

}
