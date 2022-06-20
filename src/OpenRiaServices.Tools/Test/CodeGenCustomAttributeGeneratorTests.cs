using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests CustomAttributeGenerator
    /// </summary>
    [TestClass]
    public class CodeGenCustomAttributeGeneratorTests
    {
        [TestMethod]
        [Description("CustomAttributeGenerator emits valid code when attribute is shared")]
        public void CodeGen_CustomAttrGen_AttributeType_Shared()
        {
            // Create a shared type service that says the entity's attribute is "shared" when asked whether it is shared
            MockSharedCodeService mockSts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Gen_Type), typeof(Mock_CG_Attr_Gen_TestAttribute) },
                    Array.Empty<MethodBase>(),
                    Array.Empty<string>());

            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] { typeof(Mock_CG_Attr_Gen_DomainService) }, mockSts);
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Mock_CG_Attr_Gen_Test(typeof(Mock_CG_Attr_Gen_Type))]");
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits valid code for BindableAttribute using short and full type names")]
        public void CodeGen_CustomAttrGen_BindableAttribute()
        {
            // Create a shared type service that says the entity's attribute is "shared" when asked whether it is shared
            MockSharedCodeService mockSts = new MockSharedCodeService(
                    new Type[] { typeof(System.ComponentModel.BindableAttribute) },
                    Array.Empty<MethodBase>(),
                    Array.Empty<string>());

            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] { typeof(Mock_CG_Attr_Entity_Bindable_DomainService) }, new ConsoleLogger(), mockSts, /*useFullNames*/ false);
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Bindable(true, BindingDirection.TwoWay)]");

            generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] { typeof(Mock_CG_Attr_Entity_Bindable_DomainService) }, new ConsoleLogger(), mockSts, true);
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[global::System.ComponentModel.BindableAttribute(true, global::System.ComponentModel.BindingDirection.TwoWay)]");
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when attribute type is not shared")]
        public void CodeGen_CustomAttrGen_AttributeType_NotShared()
        {
            ConsoleLogger logger = new ConsoleLogger();

            // Create a shared type service that says the entity's attribute is "unshared" when asked whether it is shared
            MockSharedCodeService mockSts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Gen_Type) },
                    Array.Empty<MethodBase>(),
                    Array.Empty<string>());
            mockSts.AddUnsharedType(typeof(Mock_CG_Attr_Gen_TestAttribute));

            string generatedCode = TestHelper.GenerateCode("C#", new Type[] { typeof(Mock_CG_Attr_Gen_DomainService) }, logger, mockSts);

            string expectedWarning = string.Format(
                                    CultureInfo.CurrentCulture,
                                    Resource.ClientCodeGen_Attribute_RequiresDataAnnotations,
                                    typeof(Mock_CG_Attr_Gen_TestAttribute),
                                    "MockProject");
            TestHelper.AssertContainsWarnings(logger, expectedWarning);

            string warningComment = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_RequiresShared,
                    typeof(Mock_CG_Attr_Gen_TestAttribute),
                    "MockProject");

            TestHelper.AssertGeneratedCodeContains(generatedCode, warningComment);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when cannot determine attribute is shared")]
        public void CodeGen_CustomAttrGen_AttributeType_Shared_Unknowable()
        {
            ConsoleLogger logger = new ConsoleLogger();

            // Create a shared type service that says the entity's attribute is "unknowable" when asked whether it is shared
            MockSharedCodeService mockSts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Gen_Type) },
                    Array.Empty<MethodBase>(),
                    Array.Empty<string>());
            mockSts.AddUnknowableType(typeof(Mock_CG_Attr_Gen_TestAttribute));

            string generatedCode = TestHelper.GenerateCode("C#", new Type[] { typeof(Mock_CG_Attr_Gen_DomainService) }, logger, mockSts);
            TestHelper.AssertNoErrorsOrWarnings(logger);

            string warningComment = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_RequiresShared_NoPDB,
                    typeof(Mock_CG_Attr_Gen_TestAttribute),
                    typeof(Mock_CG_Attr_Gen_TestAttribute).Assembly.GetName().Name,
                    "MockProject");

            // CodeDom injects comments after line breaks
            warningComment = System.Text.RegularExpressions.Regex.Replace(warningComment, @"\r\n?|\n|\r", "\r\n        // ");

            TestHelper.AssertGeneratedCodeContains(generatedCode, warningComment);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits warning when attribute contains typeof of an unshared type")]
        public void CodeGen_CustomAttrGen_Attribute_References_Type_NotShared()
        {
            ConsoleLogger logger = new ConsoleLogger();


            // Create a shared type service that says the entity's attribute is "shared" when asked whether it is shared
            MockSharedCodeService mockSts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Gen_TestAttribute) },
                    Array.Empty<MethodBase>(),
                    Array.Empty<string>());
            // Explicitly make the typeof() ref in the attribute say it is unshared
            mockSts.AddUnsharedType(typeof(Mock_CG_Attr_Gen_Type));

            string generatedCode = TestHelper.GenerateCode("C#", new Type[] { typeof(Mock_CG_Attr_Gen_DomainService) }, logger, mockSts);
            TestHelper.AssertNoErrorsOrWarnings(logger);

            string warningComment = string.Format(
                                        CultureInfo.CurrentCulture,
                                        Resource.ClientCodeGen_Attribute_RequiresShared,
                                        typeof(Mock_CG_Attr_Gen_TestAttribute),
                                        "MockProject");

            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Mock_CG_Attr_Gen_Test(typeof(global::OpenRiaServices.Tools.Test.Mock_CG_Attr_Gen_Type))]");
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits warning when attribute contains typeof with type we cannot determine is shared")]
        public void CodeGen_CustomAttrGen_Attribute_References_Type_NotKnowable()
        {
            ConsoleLogger logger = new ConsoleLogger();

            // Create a shared type service that says the entity's attribute is "shared" when asked whether it is shared
            MockSharedCodeService mockSts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Gen_TestAttribute) },
                    Array.Empty<MethodBase>(),
                    Array.Empty<string>());
            // Explicitly make the typeof() ref in the attribute say it is unshared
            mockSts.AddUnknowableType(typeof(Mock_CG_Attr_Gen_Type));

            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] { typeof(Mock_CG_Attr_Gen_DomainService) }, logger, mockSts);

            string warningComment = string.Format(
                                        CultureInfo.CurrentCulture,
                                        Resource.ClientCodeGen_Attribute_RequiresShared,
                                        typeof(Mock_CG_Attr_Gen_TestAttribute),
                                        "MockProject");

            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Mock_CG_Attr_Gen_Test(typeof(global::OpenRiaServices.Tools.Test.Mock_CG_Attr_Gen_Type))]");
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when DomainService attribute throws an exception")]
        public void CodeGen_CustomAttrGen_DomainServiceAttributeThrows()
        {
            MockSharedCodeService sts = TestHelper.CreateCommonMockSharedCodeService();
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", new Type[] { typeof(AttributeThrowingDomainService) }, logger, sts);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Should have generated code despite warnings");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                new ThrowingServiceAttributeException(ThrowingServiceAttribute.ExceptionMessage),
                typeof(ThrowingServiceAttribute),
                ThrowingServiceAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeType,
                    expectedException.Message,
                    AttributeThrowingDomainService.DomainContextTypeName,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when an entity attribute throws an exception")]
        public void CodeGen_CustomAttrGen_EntityAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);

            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                    new ThrowingEntityAttributeException(ThrowingEntityAttribute.ExceptionMessage),
                    typeof(ThrowingEntityAttribute),
                    ThrowingEntityAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeType,
                    expectedException.Message,
                    typeof(AttributeThrowingEntity).Name,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when an entity property attribute throws an exception")]
        public void CodeGen_CustomAttrGen_EntityPropertyAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);

            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                    new ThrowingEntityPropertyAttributeException(ThrowingEntityPropertyAttribute.ExceptionMessage),
                    typeof(ThrowingEntityPropertyAttribute),
                    ThrowingEntityPropertyAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember,
                    expectedException.Message,
                    AttributeThrowingEntity.ThrowingPropertyName,
                    typeof(AttributeThrowingEntity).Name,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when an entity association attribute throws an exception")]
        public void CodeGen_CustomAttrGen_EntityAssociationAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                    new ThrowingEntityAssociationAttributeException(ThrowingEntityAssociationAttribute.ExceptionMessage),
                    typeof(ThrowingEntityAssociationAttribute),
                    ThrowingEntityAssociationAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember,
                    expectedException.Message,
                    AttributeThrowingEntity.ThrowingAssociationProperty,
                    typeof(AttributeThrowingEntity).Name,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when an entity association collection attribute throws an exception")]
        public void CodeGen_CustomAttrGen_EntityAssociationCollectionAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                    new ThrowingEntityAssociationCollectionAttributeException(ThrowingEntityAssociationCollectionAttribute.ExceptionMessage),
                    typeof(ThrowingEntityAssociationCollectionAttribute),
                    ThrowingEntityAssociationCollectionAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember,
                    expectedException.Message,
                    AttributeThrowingEntity.ThrowingAssociationCollectionProperty,
                    typeof(AttributeThrowingEntity).Name,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when a Query method attribute throws an exception")]
        public void CodeGen_CustomAttrGen_QueryMethodAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                    new ThrowingQueryMethodAttributeException(ThrowingQueryMethodAttribute.ExceptionMessage),
                    typeof(ThrowingQueryMethodAttribute),
                    ThrowingQueryMethodAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeMethod,
                    expectedException.Message,
                    AttributeThrowingDomainService.ThrowingQueryMethod,
                    AttributeThrowingDomainService.DomainContextTypeName,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when a Query method parameter attribute throws an exception")]
        public void CodeGen_CustomAttrGen_QueryMethodParameterAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                new ThrowingQueryMethodParameterAttributeException(ThrowingQueryMethodParameterAttribute.ExceptionMessage),
                typeof(ThrowingQueryMethodParameterAttribute),
                ThrowingQueryMethodParameterAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeMethodParameter,
                    expectedException.Message,
                    AttributeThrowingDomainService.ThrowingQueryMethodParameter,
                    AttributeThrowingDomainService.ThrowingQueryMethod,
                    AttributeThrowingDomainService.DomainContextTypeName,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when a Update method attribute throws an exception")]
        public void CodeGen_CustomAttrGen_UpdateMethodAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                new ThrowingUpdateMethodAttributeException(ThrowingUpdateMethodAttribute.ExceptionMessage),
                typeof(ThrowingUpdateMethodAttribute),
                ThrowingUpdateMethodAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeMethod,
                    expectedException.Message,
                    AttributeThrowingDomainService.ThrowingUpdateMethod,
                    typeof(AttributeThrowingEntity).Name,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when a Update method parameter attribute throws an exception")]
        public void CodeGen_CustomAttrGen_UpdateMethodParameterAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                new ThrowingUpdateMethodParameterAttributeException(ThrowingUpdateMethodParameterAttribute.ExceptionMessage),
                typeof(ThrowingUpdateMethodParameterAttribute),
                ThrowingUpdateMethodParameterAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeMethodParameter,
                    expectedException.Message,
                    AttributeThrowingDomainService.ThrowingUpdateMethodParameter,
                    AttributeThrowingDomainService.ThrowingUpdateMethod,
                    typeof(AttributeThrowingEntity).Name,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when a Invoke method attribute throws an exception")]
        public void CodeGen_CustomAttrGen_InvokeMethodAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                new ThrowingInvokeMethodAttributeException(ThrowingInvokeMethodAttribute.ExceptionMessage),
                typeof(ThrowingInvokeMethodAttribute),
                ThrowingInvokeMethodAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeMethod,
                    expectedException.Message,
                    AttributeThrowingDomainService.ThrowingInvokeMethod,
                    AttributeThrowingDomainService.DomainContextTypeName,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("CustomAttributeGenerator emits error in generated code when a Invoke method parameter attribute throws an exception")]
        public void CodeGen_CustomAttrGen_InvokeMethodParameterAttributeThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(AttributeThrowingDomainService), logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Code should have been generated");

            AttributeBuilderException expectedException = new AttributeBuilderException(
                new ThrowingInvokeMethodParameterAttributeException(ThrowingInvokeMethodParameterAttribute.ExceptionMessage),
                typeof(ThrowingInvokeMethodParameterAttribute),
                ThrowingInvokeMethodParameterAttribute.ThrowingPropertyName);

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeMethodParameter,
                    expectedException.Message,
                    AttributeThrowingDomainService.ThrowingInvokeMethodParameter,
                    AttributeThrowingDomainService.ThrowingInvokeMethod,
                    AttributeThrowingDomainService.DomainContextTypeName,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }

        [TestMethod]
        [Description("Checks which constructor gets selected for an attribute when the actual default value is passed as parameter")]
        public void CodeGen_CustomAttrGen_CtrSelectorTest()
        {
            using (AssemblyGenerator asmGen = new AssemblyGenerator(true, /*isCSharp*/ false, /*useFullTypeNames*/ new Type[] { typeof(DummyDomainService) }))
            {
                // Force the Attribute types to be shared
                asmGen.MockSharedCodeService.AddSharedType(typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib1));
                asmGen.MockSharedCodeService.AddSharedType(typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib2));
                asmGen.MockSharedCodeService.AddSharedType(typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib3));
                asmGen.MockSharedCodeService.AddSharedType(typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib4));

                asmGen.ReferenceAssemblies.Add(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);

                string generatedCode = asmGen.GeneratedCode;
                Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Failed to generate code:\r\n" + asmGen.ConsoleLogger.Errors);

                Assembly assy = asmGen.GeneratedAssembly;
                Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                Type clientEntityType = asmGen.GetGeneratedType(typeof(DummyEntityForAttribTest).FullName);

                MemberInfo[] prop1 = clientEntityType.GetMember("Prop1");
                IList<CustomAttributeData> cads1 = AssemblyGenerator.GetCustomAttributeData(prop1[0], typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib1));
                Assert.AreEqual(1, cads1.Count, "Expected Mock_CG_Attr_Gen_TestAttrib1 on " + clientEntityType + ".Prop1");
                //Check if the default constructor was used
                CustomAttributeData cad = cads1[0];
                IList<CustomAttributeTypedArgument> ctr1args = cad.ConstructorArguments;
                Assert.AreEqual(ctr1args.Count, 0);

                MemberInfo[] prop2 = clientEntityType.GetMember("Prop2");
                IList<CustomAttributeData> cads2 = AssemblyGenerator.GetCustomAttributeData(prop2[0], typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib2));
                Assert.AreEqual(1, cads2.Count, "Expected Mock_CG_Attr_Gen_TestAttrib2 on " + clientEntityType + ".Prop2");
                cad = cads2[0];
                //Check if the constructor with one int param was used
                IList<CustomAttributeTypedArgument> ctr2args = cad.ConstructorArguments;
                Assert.AreEqual(ctr2args.Count, 1);
                Assert.AreEqual(ctr2args[0].ArgumentType, typeof(int));
                Assert.AreEqual(ctr2args[0].Value, 0);

                MemberInfo[] prop3 = clientEntityType.GetMember("Prop3");
                IList<CustomAttributeData> cads3 = AssemblyGenerator.GetCustomAttributeData(prop3[0], typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib3));
                Assert.AreEqual(1, cads3.Count, "Expected Mock_CG_Attr_Gen_TestAttrib3 on " + clientEntityType + ".Prop3");
                cad = cads3[0];
                // Check if the ctor with one string param was used
                IList<CustomAttributeTypedArgument> ctr3args = cad.ConstructorArguments;
                Assert.AreEqual(ctr3args.Count, 1);
                Assert.AreEqual(ctr3args[0].ArgumentType, typeof(string));
                Assert.AreEqual(ctr3args[0].Value, null);

                MemberInfo[] prop4 = clientEntityType.GetMember("Prop4");
                IList<CustomAttributeData> cads4 = AssemblyGenerator.GetCustomAttributeData(prop4[0], typeof(System.ComponentModel.DataAnnotations.Mock_CG_Attr_Gen_TestAttrib4));
                Assert.AreEqual(1, cads4.Count, "Expected Mock_CG_Attr_Gen_TestAttrib4 on " + clientEntityType + ".Prop4");
                cad = cads4[0];
                // Check if the first ctor was used
                IList<CustomAttributeTypedArgument> ctr4args = cad.ConstructorArguments;
                Assert.AreEqual(ctr4args.Count, 1);
                Assert.AreEqual(ctr4args[0].ArgumentType, typeof(int));
                Assert.AreEqual(ctr4args[0].Value, 0);
            }
        }

        [TestMethod]
        [Description("CustomAttributeGenerator generates full names correctly for attributes in VB.")]
        public void CodeGen_ServiceKnownTypeAttrGen_VB_FullNames()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("VB", new Type[] { typeof(DomainServiceWithCustomMethod) }, null, null, /* generateFullNames */ true);
            TestHelper.AssertGeneratedCodeContains(generatedCode, "Global.System.ServiceModel.ServiceKnownTypeAttribute(GetType(Global.TestDomainServices.Address))");
        }

        [TestMethod]
        [Description("CustomAttributeGenerator generates full names correctly for types in attributes not shared on the client in VB.")]
        public void CodeGen_ServiceKnownTypeAttrGen_VB_NoFullNames()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("VB", new Type[] { typeof(DomainServiceWithCustomMethod) }, null, null, /* generateFullNames */ false);
            TestHelper.AssertGeneratedCodeContains(generatedCode, "ServiceKnownType(GetType(Global.TestDomainServices.Address))");
        }
    }

    [EnableClientAccess()]
    public class DomainServiceWithCustomMethod : DomainService
    {
        public IEnumerable<DummyEntityForCustomMethodTest> GetEntities()
        {
            return null;
        }

        [EntityAction]
        public void UpdateMethod(DummyEntityForCustomMethodTest t, Address t1)
        {
        }
    }

    public class DummyEntityForCustomMethodTest
    {
        [Key]
        public int Key { get; set; }
    }

    //Dummy entity type
    public class DummyEntityForAttribTest
    {
        [Key]
        public int ID { get; set; }

        [Mock_CG_Attr_Gen_TestAttrib1(0)]
        public int Prop1 { get; set; }

        [Mock_CG_Attr_Gen_TestAttrib2(0)]
        public int Prop2 { get; set; }

        [Mock_CG_Attr_Gen_TestAttrib3(null)]
        public int Prop3 { get; set; }

        [Mock_CG_Attr_Gen_TestAttrib4(null)]
        public int Prop4 { get; set; }
    }

    [EnableClientAccess]
    public class DummyDomainService : DomainService
    {
        [Query]
        public IQueryable<DummyEntityForAttribTest> GetEntities()
        {
            return null;
        }
    }

    // Arbitrary type we mention in the attribute
    public class Mock_CG_Attr_Gen_Type
    {

    }

    public class Mock_CG_Attr_Gen_DomainService : GenericDomainService<Mock_CG_Attr_Gen_Entity> { }

    public class Mock_CG_Attr_Gen_Entity
    {
        public Mock_CG_Attr_Gen_Entity() { }

        [Key]
        [Mock_CG_Attr_Gen_Test(typeof(Mock_CG_Attr_Gen_Type))]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_Bindable_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Bindable> { }

    public class Mock_CG_Attr_Entity_Bindable
    {
        [Key]
        [System.ComponentModel.Bindable(true, System.ComponentModel.BindingDirection.TwoWay)]
        public int K { get; set; }
    }
}

// The code generator only emits errors for attributes in the System.ComponentModel.DataAnnotations namespace to
// prevent attributes that are server-only by design from generating build warnings or comments in the code gen.
// For instance: [EnableClientAccess], [Query], [Invoke], and DAL-specific attributes.
namespace System.ComponentModel.DataAnnotations
{
    // Attribute we will declare as "unknowable" or "unshared" in tests above
    public class Mock_CG_Attr_Gen_TestAttribute : Attribute
    {
        private readonly Type _type;
        public Mock_CG_Attr_Gen_TestAttribute(Type type)
        {
            this._type = type;
        }
        public Type Type { get { return this._type; } }
    }

    public class Mock_CG_Attr_Gen_TestAttrib1 : Attribute
    {
        public Mock_CG_Attr_Gen_TestAttrib1()
        {
            IntProp = 0;
            StrProp = null;
        }

        public Mock_CG_Attr_Gen_TestAttrib1(int intProp)
        {
            IntProp = intProp;
            StrProp = null;
        }

        public int IntProp { get; set; }
        public string StrProp { get; set; }
    }

    public class Mock_CG_Attr_Gen_TestAttrib2 : Attribute
    {
        public Mock_CG_Attr_Gen_TestAttrib2(string str)
        {
        }

        public Mock_CG_Attr_Gen_TestAttrib2(int intProp, string str)
        {
            IntProp = intProp;
        }

        public Mock_CG_Attr_Gen_TestAttrib2(int intProp)
        {
            IntProp = intProp;
        }

        public int IntProp { get; set; }
    }

    public class Mock_CG_Attr_Gen_TestAttrib3 : Attribute
    {
        public Mock_CG_Attr_Gen_TestAttrib3(int intProp)
        {
            StrProp = null;
        }

        public Mock_CG_Attr_Gen_TestAttrib3(string str)
        {
            StrProp = str;
        }

        public Mock_CG_Attr_Gen_TestAttrib3(int intProp, string str)
        {
            StrProp = str;
        }

        public string StrProp { get; set; }
    }

    public class Mock_CG_Attr_Gen_TestAttrib4 : Attribute
    {
        public Mock_CG_Attr_Gen_TestAttrib4(int intProp)
        {
            StrProp = null;
            IntProp = intProp;
        }

        public Mock_CG_Attr_Gen_TestAttrib4(string str)
        {
            StrProp = str;
            IntProp = 0;
        }

        public Mock_CG_Attr_Gen_TestAttrib4(int intProp, string str)
        {
            StrProp = str;
            IntProp = intProp;
        }

        public int IntProp { get; set; }
        public string StrProp { get; set; }
    }
}
