using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    [TestClass]
    public class CodeGenComplexTypesTests
    {
        [TestMethod]
        [Description("Complex types can be shared by domain services.")]
        public void CodeGenComplexTypes_ComplexTypes_Are_Shared_By_DomainServices()
        {
            Type[] domainServices = new Type[]
            {
                typeof(ComplexCodeGen_SharedComplexTypes1),
                typeof(ComplexCodeGen_SharedComplexTypes2),
            };

            string[] codeContains = new string[]
            {
                "public InvokeOperation Invoke1(ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT complexType, Action<InvokeOperation> callback, object userState)",
                "public InvokeOperation Invoke1(ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT complexType)",
                "public InvokeOperation Invoke2(ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT complexType, Action<InvokeOperation> callback, object userState)",
                "public InvokeOperation Invoke2(ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT complexType)",
            };

            // compiling is enough validation that the type does not appear twice
            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, null);
        }

        [TestMethod]
        [Description("Tests RoundtripOriginalAttribute code gen behavior on complex types.")]
        public void CodeGenComplexTypes_ComplexTypes_Roundtrip_Propagation()
        {
            Type[] domainServices = new Type[]
            {
                typeof(ComplexCodeGen_RTO),
            };

            string[] codeContains = new string[]
            {
                // Entity itself has RTO
  @"[RoundtripOriginal()]
    public sealed partial class ComplexCodeGen_RTO_Entity : Entity",

                // Full members have RTO
      @"[RoundtripOriginal()]
        public int FullMember",
      @"[RoundtripOriginal()]
        public ComplexCodeGen_RTO_On_Type_And_Members FullTMMember",

                // TM has RTO on the type itself
  @"[RoundtripOriginal()]
    public sealed partial class ComplexCodeGen_RTO_On_Type_And_Members",
            };

            string[] codeNotContains = new string[]
            {
                // Entity CT member skips RTO.
      @"[RoundtripOriginal()]
        public ComplexCodeGen_RTO_CT_Full_RTO EntityFullMember",

                // Full does not have type property
  @"[RoundtripOriginal()]
    public sealed partial class ComplexCodeGen_RTO_CT_Full_RTO",

                // TM members do not have RTO
      @"[RoundtripOriginal()]
        public int TMMember",
      @"[RoundtripOriginal()]
        public ComplexCodeGen_RTO_NoRTO TMNoMember",

                // None does not have type property
  @"[RoundtripOriginal()]
    public sealed partial class ComplexCodeGen_RTO_NoRTO",

                // None members do not have RTO
      @"[RoundtripOriginal()]
        public int NoMember",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, codeNotContains);
        }

        [TestMethod]
        [Description("Complex Types can be excluded on CT properties and Entity properties.")]
        public void CodeGenComplexTypes_ComplexTypes_Exclusion_Works()
        {
            Type[] domainServices = new Type[]
            {
                typeof(ComplexCodeGen_Exclude_ComplexType),
            };

            string[] codeContains = new string[]
            {
                "public sealed partial class ComplexCodeGen_Exclude_CT : ComplexObject",
                "private ComplexCodeGen_Exclude_CT _excludeCT;",
                "public ComplexCodeGen_Exclude_CT ExcludeCT",
            };

            string[] codeNotContains = new string[]
            {
                "ComplexCodeGen_CT_Excluded_Via_Property",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, codeNotContains);
        }

        [TestMethod]
        [Description("Complex members have DataMember and Display attributes.")]
        public void CodeGenComplexTypes_ComplexTypes_Have_DataMember_And_Display()
        {
            // Case 1: Only entity exposes complex type property
            Type[] domainServices = new Type[]
            {
                typeof(CodeGenComplexTypes_Attributes_OnEntity),
            };

            string[] codeContains = new string[]
            {
      @"[DataMember()]
        [Display(AutoGenerateField=false)]
        public CodeGenComplexTypes_Attributes_ComplexType ComplexType",

      @"[DataMember()]
        [Display(AutoGenerateField=false)]
        public IEnumerable<CodeGenComplexTypes_Attributes_ComplexType> ComplexTypes",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, null);

            // Case 2: Only complex type exposes complex type property
            domainServices = new Type[]
            {
                typeof(CodeGenComplexTypes_Attributes_OnComplexType),
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, null);
        }

        [TestMethod]
        [Description("Code gen Invoke methods with complex types in their signatures.")]
        public void CodeGenComplexTypes_ValidInvokeMethodSignature_Succeeds()
        {
            Type[] domainServices = new Type[]
            {
                typeof(ComplexTypes_TestService_ComplexMethodSignatures),
            };

            string[] codeContains = new string[]
            {
                // named updates on the entity
                "public void CustomUpdateHomeAddress(Address newAddress)",
                "public void UpdateHomeAddress(Address newAddress)",

                // named updates on the domain context
                "public void CustomUpdateHomeAddress(ComplexType_Parent parent, Address newAddress)",
                "public void UpdateHomeAddress(ComplexType_Parent parent, Address newAddress)",

                // invokes
                "public InvokeOperation AppendMoreAddresses(IEnumerable<Address> addresses, Action<InvokeOperation> callback, object userState)",
                "public InvokeOperation AppendMoreAddresses(IEnumerable<Address> addresses)",
                "public InvokeOperation Bar(IEnumerable<Address> addresses, Action<InvokeOperation> callback, object userState)",
                "public InvokeOperation Bar(IEnumerable<Address> addresses)",
                "public InvokeOperation<IEnumerable<Address>> Foo(ContactInfo contact, Action<InvokeOperation<IEnumerable<Address>>> callback, object userState)",
                "public InvokeOperation<IEnumerable<Address>> Foo(ContactInfo contact)",
                "public InvokeOperation<IEnumerable<Phone>> GetAllPhoneNumbers(ContactInfo contact, Action<InvokeOperation<IEnumerable<Phone>>> callback, object userState)",
                "public InvokeOperation<IEnumerable<Phone>> GetAllPhoneNumbers(ContactInfo contact)",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, null);
        }
    }

    public class CodeGenComplexTypes_DomainService<T> : DomainService
    {
        public IEnumerable<T> GetEntity() { return null; }
    }

    public class ComplexCodeGen_Entity
    {
        [Key]
        public int Key { get; set; }
    }

    #region Test Automatic Attributes
    [EnableClientAccess]
    public class CodeGenComplexTypes_Attributes_OnEntity : CodeGenComplexTypes_DomainService<CodeGenComplexTypes_Attributes_Entity_WithComplexType> { }

    [EnableClientAccess]
    public class CodeGenComplexTypes_Attributes_OnComplexType : CodeGenComplexTypes_DomainService<CodeGenComplexTypes_Attributes_Entity>
    {
        [Invoke]
        public void Invoke(CodeGenComplexType_Attributes_ComplexType_WithComplexType complexType) { }
    }

    public class CodeGenComplexTypes_Attributes_Entity : ComplexCodeGen_Entity
    {
        public CodeGenComplexType_Attributes_ComplexType_WithComplexType ComplexType { get; set; }
    }
    
    public class CodeGenComplexTypes_Attributes_Entity_WithComplexType : ComplexCodeGen_Entity
    {
        public CodeGenComplexTypes_Attributes_ComplexType ComplexType { get; set; }
        public IEnumerable<CodeGenComplexTypes_Attributes_ComplexType> ComplexTypes { get; set; }
    }

    public class CodeGenComplexTypes_Attributes_ComplexType
    {
    }

    public class CodeGenComplexType_Attributes_ComplexType_WithComplexType
    {
        public CodeGenComplexTypes_Attributes_ComplexType ComplexType { get; set; }
        public IEnumerable<CodeGenComplexTypes_Attributes_ComplexType> ComplexTypes { get; set; }
    }
    #endregion

    #region Test Exclude
    [EnableClientAccess]
    public class ComplexCodeGen_Exclude_ComplexType : CodeGenComplexTypes_DomainService<ComplexCodeGen_Exclude_Entity> { }

    public class ComplexCodeGen_Exclude_Entity : ComplexCodeGen_Entity
    {
        [Exclude]
        public ComplexCodeGen_CT_Excluded_Via_Property PropertyExclude { get; set; }

        public ComplexCodeGen_Exclude_CT ExcludeCT { get; set; }
    }

    public class ComplexCodeGen_Exclude_CT
    {
        [Exclude]
        public ComplexCodeGen_CT_Excluded_Via_Property PropertyExclude { get; set; }
    }

    public class ComplexCodeGen_CT_Excluded_Via_Property { }
    #endregion

    #region RoundtripOriginal
    [EnableClientAccess]
    public class ComplexCodeGen_RTO : CodeGenComplexTypes_DomainService<ComplexCodeGen_RTO_Entity> { }

    [RoundtripOriginal]
    public class ComplexCodeGen_RTO_Entity : ComplexCodeGen_Entity
    {
        [RoundtripOriginal]
        public ComplexCodeGen_RTO_CT_Full_RTO EntityFullMember { get; set; }
    }

    public class ComplexCodeGen_RTO_CT_Full_RTO
    {
        [RoundtripOriginal]
        public int FullMember { get; set; }

        [RoundtripOriginal]
        public ComplexCodeGen_RTO_On_Type_And_Members FullTMMember { get; set; }
    }

    [RoundtripOriginal]
    public class ComplexCodeGen_RTO_On_Type_And_Members
    {
        [RoundtripOriginal]
        public int TMMember { get; set; }

        [RoundtripOriginal]
        public ComplexCodeGen_RTO_NoRTO TMNoMember { get; set; }
    }

    public class ComplexCodeGen_RTO_NoRTO
    {
        public int NoMember { get; set; }
    }
    #endregion

    #region Sharing complex types
    [EnableClientAccess]
    public class ComplexCodeGen_SharedComplexTypes1 : CodeGenComplexTypes_DomainService<ComplexCodeGen_Entity>
    {
        public void Invoke1(ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT complexType) { }
    }

    [EnableClientAccess]
    public class ComplexCodeGen_SharedComplexTypes2 : CodeGenComplexTypes_DomainService<ComplexCodeGen_Entity>
    {
        public void Invoke2(ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT complexType) { }
    }
    
    public class ComplexCodeGen_SharedComplexType_SharedComplexTypes_CT
    {
    }
    #endregion
}
