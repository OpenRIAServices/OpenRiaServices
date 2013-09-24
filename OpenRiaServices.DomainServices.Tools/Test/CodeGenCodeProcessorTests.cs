using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Client.Test;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    #region Tests

    /// <summary>
    /// Unit Tests to verify <see cref="CodeProcessor"/> behaviors.
    /// </summary>
    [TestClass]
    public class CodeGenCodeProcessorTests
    {
        /// <summary>
        /// Verifies that a <see cref="DomainService"/> can be annotated with a valid <see cref="CodeProcessor"/> type.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that a DomainService can be annotated with a valid CodeProcessor type.")]
        public void DomainService_CodeProcessor_ValidType()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(TestProvider_ValidType));

            // Validate generated code
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                @"[DomainIdentifier(""a"")]",
                @"class MockEntity1 : Entity");
        }

        /// <summary>
        /// Verifies that a <see cref="DomainService"/>s base type can be annotated with a valid <see cref="CodeProcessor"/> type.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that a DomainService can be annotated with a valid CodeProcessor type.")]
        public void DomainService_CodeProcessor_ValidNestedType()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(TestProvider_ValidNestedType));

            // Validate generated code
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                @"[DomainIdentifier(""a"")]",
                @"class MockEntity1 : Entity");
        }

        /// <summary>
        /// Verifies that a <see cref="DomainService"/> can be annotated with a <see cref="CodeProcessor"/> that can inject inheritance into the generated client code.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that a DomainService can be annotated with a CodeProcessor that can inject inheritance into the generated client code.")]
        public void DomainService_CodeProcessor_InjectInheritance()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(TestProvider_InjectInheritance));

            // Validate generated code modifications
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                @"[DomainIdentifier(""a"")]",
                @"class MockEntity1 : InjectedBaseClass, IInjectedInterface");
        }

        /// <summary>
        /// Verifies that a <see cref="DomainService"/>s base type can be annotated with a <see cref="CodeProcessor"/> that can inject inheritance into the generated client code.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that a DomainService can be annotated with a CodeProcessor that can inject inheritance into the generated client code.")]
        public void DomainService_CodeProcessor_NestedType_InjectInheritance()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(TestProvider_NestedType_InjectInheritance));

            // Validate generated code modifications
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                @"[DomainIdentifier(""a"")]",
                @"class MockEntity1 : InjectedBaseClass, IInjectedInterface");
        }

        /// <summary>
        /// Verifies that a <see cref="CodeProcessor"/> with a private constructor is invoked.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that a CodeProcessor with a private constructor is invoked.")]
        public void DomainService_CodeProcessor_PrivateCtor()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(TestProvider_PrivateCtor));

            // Validate generated code modifications
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                @"[DomainIdentifier(""a"")]",
                @"class MockEntity1 : InjectedBaseClass, IInjectedInterface");
        }

        /// <summary>
        /// Verifies that errors are correctly logged when a <see cref="DomainService"/> is annotated with a <see cref="CodeProcessor"/> of an invalid type.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that errors are correctly logged when a DomainService is annotated with a CodeProcessor of an invalid type.")]
        public void DomainService_CodeProcessor_InvalidType()
        {
             string error = string.Format(
                    CultureInfo.InvariantCulture,
                    Resource.ClientCodeGen_DomainService_CodeProcessor_NotValidType,
                    typeof(string),
                    typeof(TestProvider_InvalidType),
                    typeof(CodeProcessor));

            TestHelper.GenerateCodeAssertFailure("C#", typeof(TestProvider_InvalidType), error);
        }

        /// <summary>
        /// Verifies that exceptions thrown by <see cref="CodeProcessor"/> types are logged and continue to propagate.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that exceptions thrown by CodeProcessors are logged and continue to propagate.")]
        public void DomainService_CodeProcessor_Throws()
        {
            ConsoleLogger logger = new ConsoleLogger();

            ExceptionHelper.ExpectException<NotImplementedException>(
                () => TestHelper.GenerateCode("C#", typeof(TestProvider_Throws), logger),
                false);

            TestHelper.AssertContainsErrors(logger,
                                            string.Format(
                                                CultureInfo.InvariantCulture,
                                                Resource.ClientCodeGen_DomainService_CodeProcessor_ExceptionCaught,
                                                typeof(CodeProcessor_Throws),
                                                typeof(TestProvider_Throws),
                                                "The method or operation is not implemented."));
        }

        /// <summary>
        /// Verifies that the <see cref="CodeProcessor"/> receives all types expected in the 'typeMapping' parameter.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that the CodeProcessor receives all types expected in the 'typeMapping' parameter.")]
        public void DomainService_CodeProcessor_TypeMapping()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] { typeof(TestProvider_ExamineTypeMapping1), typeof(TestProvider_ExamineTypeMapping2)}, null);
        }

        /// <summary>
        /// Verifies that the <see cref="CodeProcessor"/> receives all types expected in the 'typeMapping' parameter.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that CodeProcessors are invoked after the entire CodeDOM graph is complete.")]
        public void DomainService_CodeProcessor_InvokedAtEndOfCodeGen()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(TestProvider_ClearCodeDOMGraph));
            TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode, "class");
        }
    }

    #endregion Tests

    #region DomainServices using various CodeProcessor types

    /// <summary>
    /// Simple mock entity type.
    /// </summary>
    public class MockEntity1
    {
        /// <summary>
        /// Entity Key.
        /// </summary>
        [Key]
        public int Identifier { get; set; }
    }

    /// <summary>
    /// Simple mock entity type.
    /// </summary>
    public class MockEntity2
    {
        /// <summary>
        /// Entity Key.
        /// </summary>
        [Key]
        public int Identifier { get; set; }
    }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with an valid <see cref="CodeProcessor"/> type.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_NoOp))]
    public class TestProvider_ValidType : GenericDomainService<MockEntity1> { }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with an valid <see cref="CodeProcessor"/> type.
    /// </summary>
    public class TestProvider_ValidNestedType : TestProvider_ValidType { }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with an valid <see cref="CodeProcessor"/> type that injects inheritance types.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_InjectInheritance))]
    public class TestProvider_InjectInheritance : GenericDomainService<MockEntity1> { }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with an valid <see cref="CodeProcessor"/> type that injects inheritance types.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_InjectInheritance))]
    public class TestProvider_NestedType_InjectInheritance : GenericDomainService<MockEntity1> { }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with an invalid <see cref="CodeProcessor"/> type.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(string))]
    public class TestProvider_InvalidType : GenericDomainService<MockEntity1> { }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with a <see cref="CodeProcessor"/> that throws.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_Throws))]
    public class TestProvider_Throws : GenericDomainService<MockEntity1> { }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with a <see cref="CodeProcessor"/> that does not have a public constructor.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_PrivateCtor))]
    public class TestProvider_PrivateCtor : GenericDomainService<MockEntity1> { }

    /// <summary>
    /// Test <see cref="DomainService"/> used to validate the population of the 'typeMapping' passed to <see cref="CodeProcessor"/>s.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_ExamineTypeMapping<MockEntity1>))]
    public class TestProvider_ExamineTypeMapping1 : GenericDomainService<MockEntity1> { }

    /// <summary>
    /// Test <see cref="DomainService"/> used to validate the population of the 'typeMapping' passed to <see cref="CodeProcessor"/>s.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_ExamineTypeMapping<MockEntity2>))]
    public class TestProvider_ExamineTypeMapping2 : GenericDomainService<MockEntity2> { }

    /// <summary>
    /// Test <see cref="DomainService"/> decorated with a <see cref="CodeProcessor"/> that clears the CodeDOM graph.
    /// </summary>
    [DomainIdentifier("a", CodeProcessor = typeof(CodeProcessor_ClearGraph))]
    public class TestProvider_ClearCodeDOMGraph : GenericDomainService<MockEntity1> { }

    #endregion DomainServices using various CodeProcessor types

    #region CodeProcessors

    /// <summary>
    /// Test <see cref="CodeProcessor"/> that does no work.
    /// </summary>
    public class CodeProcessor_NoOp : CodeProcessor
    {
        public CodeProcessor_NoOp(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping) { }
    }

    /// <summary>
    /// Test <see cref="CodeProcessor"/> that injects base types.
    /// </summary>
    public class CodeProcessor_InjectInheritance : CodeProcessor
    {
        public CodeProcessor_InjectInheritance(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            // Get a reference to the entity class
            CodeTypeDeclaration codeGenEntity = typeMapping[typeof(MockEntity1)];

            codeGenEntity.BaseTypes.Clear();

            // Inject an artificial base class "InjectedBaseClass"
            codeGenEntity.BaseTypes.Add(new CodeTypeReference("InjectedBaseClass"));

            // Inject an artificial base class "IInjectedInterface"
            codeGenEntity.BaseTypes.Add(new CodeTypeReference("IInjectedInterface"));
        }
    }

    /// <summary>
    /// Test <see cref="CodeProcessor"/> that removes base types.
    /// </summary>
    public class CodeProcessor_InjectInheritance2 : CodeProcessor
    {
        public CodeProcessor_InjectInheritance2(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            // Get a reference to the entity class
            CodeTypeDeclaration codeGenEntity = typeMapping[typeof(MockEntity1)];

            // Inject an artificial base class "IInjectedInterface"
            codeGenEntity.BaseTypes.Add(new CodeTypeReference("IInjectedInterface2"));
        }
    }

    /// <summary>
    /// Test <see cref="CodeProcessor"/> that does not have a public constructor accepting a <see cref="CodeDomProvider"/>.
    /// </summary>
    public class CodeProcessor_PrivateCtor : CodeProcessor_InjectInheritance
    {
        public CodeProcessor_PrivateCtor() : base(null) { }
        public CodeProcessor_PrivateCtor(int a) : base(null) { }
        private CodeProcessor_PrivateCtor(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
    }

    /// <summary>
    /// Test <see cref="CodeProcessor"/> that throws an exception when invoked.
    /// </summary>
    public class CodeProcessor_Throws : CodeProcessor
    {
        public CodeProcessor_Throws(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Test <see cref="CodeProcessor"/> that verifies the expected values are found within the 'typeMapping' parameter.
    /// </summary>
    public class CodeProcessor_ExamineTypeMapping<TEntity> : CodeProcessor
    {
        public CodeProcessor_ExamineTypeMapping(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            Assert.AreEqual(2, typeMapping.Count);
            Assert.AreEqual(typeof(TEntity).Name, typeMapping[typeof(TEntity)].Name);
            if (domainServiceDescription.DomainServiceType == typeof(TestProvider_ExamineTypeMapping1))
            {
                Assert.AreEqual("TestProvider_ExamineTypeMapping1", typeMapping[typeof(TestProvider_ExamineTypeMapping1)].Name);
            }
            else if (domainServiceDescription.DomainServiceType == typeof(TestProvider_ExamineTypeMapping2))
            {
                Assert.AreEqual("TestProvider_ExamineTypeMapping2", typeMapping[typeof(TestProvider_ExamineTypeMapping2)].Name);
            }
        }
    }

    /// <summary>
    /// Test <see cref="CodeProcessor"/> that clears the CodeDOM graph.
    /// </summary>
    public class CodeProcessor_ClearGraph : CodeProcessor
    {
        public CodeProcessor_ClearGraph(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            codeCompileUnit.Namespaces.Clear();
        }
    }

    #endregion CodeProcessors
}
