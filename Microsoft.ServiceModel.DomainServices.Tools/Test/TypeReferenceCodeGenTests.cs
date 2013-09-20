using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    using TypeRef.Test1;
    using TypeRef.Test2;

    /// <summary>
    /// Tests code generator's use of CodeTypeReferences
    /// </summary>
    [TestClass]
    public class TypeReferenceCodeGenTests
    {
        [TestMethod]
        [Description("DomainServices in different namespaces both import common types for C#, short type names")]
        [DeploymentItem(@"Microsoft.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt", "CG_TR_CS")]
        public void TypeReference_Common_Types_CS()
        {
            this.TypeReference_Common_Types_Helper(/*isCSharp*/ true, /*useFullTypes*/ false, "CG_TR_CS");
        }

        [TestMethod]
        [Description("DomainServices in different namespaces both import common types for C#, full type names")]
        [DeploymentItem(@"Microsoft.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt", "CG_TR_CS_Full")]
        public void TypeReference_Common_Types_CS_FullTypeNames()
        {
            this.TypeReference_Common_Types_Helper(/*isCSharp*/ true, /*useFullTypes*/ true, "CG_TR_CS_Full");
        }

        [TestMethod]
        [Description("DomainServices in different namespaces both import common types for VB, short type names")]
        [DeploymentItem(@"Microsoft.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt", "CG_TR_VB")]
        public void TypeReference_Common_Types_VB()
        {
            this.TypeReference_Common_Types_Helper(/*isCSharp*/ false, /*useFullTypes*/ false, "CG_TR_VB");
        }

        [TestMethod]
        [Description("DomainServices in different namespaces both import common types for VB, full type names")]
        [DeploymentItem(@"Microsoft.ServiceModel.DomainServices.Tools\Test\ProjectPath.txt", "CG_TR_VB_Full")]
        public void TypeReference_Common_Types_VB_FullTypeNames()
        {
            this.TypeReference_Common_Types_Helper(/*isCSharp*/ false, /*useFullTypes*/ true, "CG_TR_VB_Full");
        }

        // Common helper that does body of test
        private void TypeReference_Common_Types_Helper(bool isCSharp, bool useFullTypeNames, string testFolderName)
        {
            using (AssemblyGenerator asmGen = new AssemblyGenerator(testFolderName, isCSharp, useFullTypeNames, new Type[] { typeof(TypeReferenceDomainService_Common_Types1), typeof(TypeReferenceDomainService_Common_Types2) }))
            {
                // Force this type to be shared to force failure
                asmGen.MockSharedCodeService.AddSharedType(typeof(System.Reflection.BindingFlags));

                string generatedCode = asmGen.GeneratedCode;
                Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Failed to generate code:\r\n" + asmGen.ConsoleLogger.Errors);

                Assembly assy = asmGen.GeneratedAssembly;
                Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                VerifyTypeRefEntity(asmGen, typeof(TypeReferenceCommonTypeEntity1));
                VerifyTypeRefEntity(asmGen, typeof(TypeReferenceCommonTypeEntity2));
            }
        }

        private void VerifyTypeRefEntity(AssemblyGenerator asmGen, Type serverEntityType)
        {
            VerifyTypeRefEntityProperty(asmGen, serverEntityType, "NullableBindingFlags", typeof(Nullable<BindingFlags>));
            VerifyTypeRefEntityProperty(asmGen, serverEntityType, "ListOfBindingFlags", typeof(List<BindingFlags>));
        }

        private void VerifyTypeRefEntityProperty(AssemblyGenerator asmGen, Type serverEntityType, string propertyName, Type propertyType)
        {
            Type clientEntityType = asmGen.GetGeneratedType(serverEntityType.FullName);
            Assert.IsNotNull(clientEntityType, "Expected entity of type " + serverEntityType);
            Assert.AreNotSame(serverEntityType, clientEntityType, "Server and client entity types should not be the same instance");

            PropertyInfo serverPropertyInfo = serverEntityType.GetProperty(propertyName);
            Assert.IsNotNull(serverPropertyInfo, "Expected server property " + propertyName + " on entity type " + serverEntityType);

            PropertyInfo clientPropertyInfo = clientEntityType.GetProperty(propertyName);
            Assert.IsNotNull(clientPropertyInfo, "Expected client property " + propertyName + " on entity type " + clientEntityType);

            Assert.AreEqual(serverPropertyInfo.PropertyType.Name, clientPropertyInfo.PropertyType.Name, "server and client properties have different return types");
        }
    }
}

namespace TypeRef.Test1
{
    // The test is this: we cache CodeTypeReferences in our code generator, but we add the
    // imports only when we get a cache miss.  This test forces that scenario where we
    // get a cache hit on Nullable<BindingFlags> but still need to add System.Reflection
    // to the second generated namespace
    public class TypeReferenceCommonTypeEntity1
    {
        [Key]
        public string ID { get; set; }

        // See note above -- this type was selected because it resides in
        // System.Reflection, not normally imported.  We use nullable and List
        // to exercise discovery of generic parameter types
        public System.Reflection.BindingFlags? NullableBindingFlags { get; set; }
        public IEnumerable<System.Reflection.BindingFlags> ListOfBindingFlags { get; set; }
    }


    [EnableClientAccess]
    public class TypeReferenceDomainService_Common_Types1 : DomainService
    {
        [Query]
        public IQueryable<TypeReferenceCommonTypeEntity1> GetEntities()
        {
            throw new NotImplementedException();
        }
    }
}

// Types in this namespace exactly mirror TypeRef.Test1 in terms of
// exposing types from System.Reflection.  It is the occurence in
// the 2nd namespace that becomes interesting.
namespace TypeRef.Test2
{
    public class TypeReferenceCommonTypeEntity2
    {
        [Key]
        public string ID { get; set; }

        public System.Reflection.BindingFlags? NullableBindingFlags { get; set; }
        public IEnumerable<System.Reflection.BindingFlags> ListOfBindingFlags { get; set; }
    }

    [EnableClientAccess]
    public class TypeReferenceDomainService_Common_Types2 : DomainService
    {
        [Query]
        public IQueryable<TypeReferenceCommonTypeEntity2> GetEntities()
        {
            throw new NotImplementedException();
        }
    }
}
