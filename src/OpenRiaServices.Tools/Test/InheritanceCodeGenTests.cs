using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using DomainService_Resource = OpenRiaServices.Server.Resource;

namespace OpenRiaServices.Tools.Test
{
    using Inheritance.Tests;
    using IgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

    /// <summary>
    /// Tests CustomAttributeGenerator
    /// </summary>
    [TestClass]
    public class InheritanceCodeGenTests
    {
        [TestMethod]
        [Description("Inheritance: the domain service catalog identifies the right entities")]
        public void Inherit_Gen_DomainServiceCatalog()
        {
            using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                true /*isCSharp*/,
                new Type[] { typeof(Inherit_Basic_DomainService) }))
            {
                // ---------------------------------------------------
                // Get the DomainServiceCatalog from our helper
                // ---------------------------------------------------
                IEnumerable<DomainServiceDescription> dsds = asmGen.DomainServiceCatalog.DomainServiceDescriptions;
                Assert.IsNotNull(dsds, "DomainServiceDescriptions expected");
                Assert.AreEqual(1, dsds.Count(), "Should have seen just 1 domain service description");
                DomainServiceDescription dsd = dsds.First();

                // ----------------------
                // VisibleRootEntityTypes
                // ----------------------
                IEnumerable<Type> rootEntities = dsd.RootEntityTypes;
                Assert.IsNotNull(rootEntities, "Expected root entities");
                Assert.AreEqual(1, rootEntities.Count(), "Expected 1 root entity");
                Assert.IsTrue(rootEntities.Contains(typeof(Inherit_Basic_Root)), "Expected our visible root to be in roots");

                // ----------------------
                // EntityTypes
                // ----------------------
                IEnumerable<Type> allEntities = dsd.EntityTypes;
                Assert.IsNotNull(allEntities, "Expected all entities");
                Assert.AreEqual(2, allEntities.Count(), "Expected 2 entities");
                Assert.IsTrue(allEntities.Contains(typeof(Inherit_Basic_Root)), "Expected our visible root to be in all entities");
                Assert.IsTrue(allEntities.Contains(typeof(Inherit_Basic_Derived)), "Expected our derived entity");

                // ----------------------
                // GetVisibleEntityRootType/GetVisibleEntityBaseType
                // ----------------------

                Type visibleRoot = dsd.GetRootEntityType((typeof(Inherit_Basic_Derived)));
                Assert.AreEqual(typeof(Inherit_Basic_Root), visibleRoot, "Expected visible root to be reported as root of derived");

                visibleRoot = dsd.GetRootEntityType((typeof(Inherit_Basic_Root)));
                Assert.AreEqual(typeof(Inherit_Basic_Root), visibleRoot, "Expected visible root to be reported as root of self");

                visibleRoot = dsd.GetEntityBaseType((typeof(Inherit_Basic_Derived)));
                Assert.AreEqual(typeof(Inherit_Basic_Root), visibleRoot, "Expected visible root to be reported as base of derived");

            }
        }

        [TestMethod]
        [Description("Inheritance: basic validation of code gen for domain service with single derived type")]
        public void Inherit_Gen_Basic()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Basic_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // ---------------------------------
                    // Check visible root type
                    // ---------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from Entity, and be public, and not be sealed
                    Assert.AreEqual(TypeConstants.EntityTypeFullName, rootType.BaseType.FullName);
                    Assert.IsFalse(rootType.IsSealed);
                    Assert.IsTrue(rootType.IsPublic);

                    // Base type must declare TheKey and GetIdentity as well as its property,
                    AssertDeclaresMembers(rootType, "TheKey", "GetIdentity", "RootProperty");

                    // Validate that root type has generated [KnownType] for each visible attribute
                    AssertKnownTypeAttributes(asmGen, rootType, true /* expected */,
                                                    typeof(Inherit_Basic_Derived));

                    // check for default ctor
                    ConstructorInfo ctor = rootType.GetConstructor(Array.Empty<Type>());
                    Assert.IsNotNull(ctor);
                    Assert.AreEqual(rootType, ctor.DeclaringType, "Root ctor should have been declared in root");

                    // ---------------------------------
                    // Check that derived type was generated
                    // ---------------------------------
                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from our visible base type, public, not sealed (because it has subtypes)
                    Assert.AreEqual(rootType, derivedType.BaseType);
                    Assert.IsTrue(derivedType.IsSealed);
                    Assert.IsTrue(derivedType.IsPublic);

                    // Should have generated its properties
                    AssertDeclaresMembers(derivedType, "DerivedProperty");

                    // Should not have declared the base type members, but should see them
                    AssertHasButDoesNotDeclareMembers(derivedType, "TheKey", "GetIdentity", "RootProperty");

                    // check for default ctor
                    ctor = derivedType.GetConstructor(Array.Empty<Type>());
                    Assert.IsNotNull(ctor);
                    Assert.AreEqual(derivedType, ctor.DeclaringType, "Derived ctor should have been declared in derived type");

                    // -----------------------------------------
                    // Check DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Basic_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Basic_DomainContext");

                    // validate EntitySet property
                    PropertyInfo pInfo = domainContextType.GetProperty("Inherit_Basic_Roots");  // note plural
                    Assert.IsNotNull(pInfo, "Did not find EntitySet");

                    Type entitySetType = pInfo.PropertyType;
                    Assert.IsTrue(entitySetType.IsGenericType, "EntitySet should have been generic type");
                    Assert.IsTrue(entitySetType.Name.StartsWith("EntitySet"), "EntitySet should have been of type EntitySet<>");

                    Type[] genericArgs = entitySetType.GetGenericArguments();
                    Assert.AreEqual(1, genericArgs.Length, "Expected EntitySet to have 1 generic arg");
                    Assert.AreEqual(rootType, genericArgs[0], "Expected EntitySet generic arg to be root type");

                    // validate query root for root entity
                    MethodInfo mInfo = domainContextType.GetMethod("GetQuery");
                    Assert.IsNotNull(mInfo, "Did not find root entity GetQuery method");

                    Type rootQueryType = mInfo.ReturnType;
                    Assert.IsTrue(rootQueryType.IsGenericType, "Root query should have been generic type");
                    Assert.IsTrue(rootQueryType.Name.StartsWith("EntityQuery"), "Root query should have been of type EntityQuery<>");

                    genericArgs = rootQueryType.GetGenericArguments();
                    Assert.AreEqual(1, genericArgs.Length, "Expected root query to have 1 generic arg");
                    Assert.AreEqual(rootType, genericArgs[0], "Expected root query generic arg to be root type");

                    // validate query root for derived entity
                    mInfo = domainContextType.GetMethod("GetDerivedQuery");
                    Assert.IsNotNull(mInfo, "Did not find root entity GetDerivedQuery method");

                    Type derivedQueryType = mInfo.ReturnType;
                    Assert.IsTrue(derivedQueryType.IsGenericType, "Derived query should have been generic type");
                    Assert.IsTrue(derivedQueryType.Name.StartsWith("EntityQuery"), "Derived query should have been of type EntityQuery<>");

                    genericArgs = derivedQueryType.GetGenericArguments();
                    Assert.AreEqual(1, genericArgs.Length, "Expected root query to have 1 generic arg");
                    Assert.AreEqual(derivedType, genericArgs[0], "Expected root query generic arg to be derived type");

                    // -------------------------------------
                    // EntityContainer creates list for root
                    // -------------------------------------
                    // TODO: would like better test
                    string expectedCodeGen = isCSharp
                            ? "this.CreateEntitySet<Inherit_Basic_Root>(EntitySetOperations.None);"
                            : "Me.CreateEntitySet(Of Inherit_Basic_Root)(EntitySetOperations.None)";

                    if (!asmGen.GeneratedCode.Contains(expectedCodeGen))
                    {
                        Assert.Fail("Expected to see this in generatedCode:\r\n" + expectedCodeGen + "\r\n but instead saw:\r\n" + asmGen.GeneratedCode);
                    }
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: CUD methods on derived types is allowed")]
        public void Inherit_Gen_Basic_With_CUD()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Basic_CUD_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Basic_CUD_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Basic_CUD_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root entity has custom method defined on root
                    // -----------------------------------------
                    this.AssertValidCustomMethod(asmGen, rootType, domainContextType, "CustomIt", new Type[] { typeof(bool) });

                    // -----------------------------------------
                    // Derived entity has custom method
                    // -----------------------------------------
                    this.AssertValidCustomMethod(asmGen, derivedType, domainContextType, "CustomItDerived", new Type[] { typeof(int) });

                    // -------------------------------------
                    // EntityContainer creates list for root
                    // -------------------------------------
                    // TODO: would like better test
                    string expectedCodeGen = isCSharp
                                                ? "this.CreateEntitySet<Inherit_Basic_Root>(EntitySetOperations.All);"
                                                : "Me.CreateEntitySet(Of Inherit_Basic_Root)(EntitySetOperations.All)";

                    if (!asmGen.GeneratedCode.Contains(expectedCodeGen))
                    {
                        Assert.Fail("Expected to see this in generatedCode:\r\n" + expectedCodeGen + "\r\n but instead saw:\r\n" + asmGen.GeneratedCode);
                    }
                }
            }
        }

        #region Association
        [TestMethod]
        [Description("Inheritance: unidirectional 1:1 association is legal from derived entity to other root entity identified only through [Include]")]
        public void Inherit_Gen_Assoc_Uni_Derived_To_Included_Root()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Assoc_UD2IR_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Assoc_UD2IR_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Assoc_UD2IR_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_UD2IR_Source_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_UD2IR_Source_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Other entity generated (declared only via [Include] on derived's assoc prop)
                    // -----------------------------------------
                    Type otherType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_UD2IR_Target_Root).FullName);
                    Assert.IsNotNull(otherType, "Expected to see other root base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ----------------------------------------------
                    // DerivedType.Other property
                    //  - should have been generated
                    //  - should be of the type of the Other entity (generated)
                    //  - should have carried the [Association] attribute along
                    PropertyInfo pInfo = derivedType.GetProperty("Target");
                    Assert.IsNotNull(pInfo, "Expected 'Target' property on derived type");
                    Assert.AreEqual(otherType, pInfo.PropertyType, "'Target' property should have been of type " + otherType);

                    CustomAttributeData assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Target' property");

                    string value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("TargetId", value, "Expected 'thisKey' to be TargetId, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("Id", value, "Expected 'otherKey' to be Id, not " + value);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: bidirectional 1:1 association is legal from derived entity to other root entity identified only through [Include]")]
        public void Inherit_Gen_Assoc_Bi_Derived_To_Included_Root()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Assoc_BD2IR_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Assoc_BD2IR_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Assoc_BD2IR_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2IR_Source_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2IR_Source_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Other entity generated (declared only via [Include] on derived's assoc prop)
                    // -----------------------------------------
                    Type targetType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2IR_Target_Root).FullName);
                    Assert.IsNotNull(targetType, "Expected to see other root base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ----------------------------------------------
                    // DerivedType.Target property
                    //  - should have been generated
                    //  - should be of the type of the Other entity (generated)
                    //  - should have carried the [Association] attribute along
                    PropertyInfo pInfo = derivedType.GetProperty("Target");
                    Assert.IsNotNull(pInfo, "Expected 'Target' property on derived type");
                    Assert.AreEqual(targetType, pInfo.PropertyType, "'Target' property should have been of type " + targetType);

                    CustomAttributeData assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Target' property");

                    string value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("TargetId", value, "Expected 'thisKey' to be TargetId, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("Id", value, "Expected 'otherKey' to be Id, not " + value);

                    // ----------------------------------------------
                    // Target.Source property
                    //  - should have been generated
                    //  - should be of the type of the source entity (generated)
                    //  - should have carried the [Association] attribute along
                    pInfo = targetType.GetProperty("Source");
                    Assert.IsNotNull(pInfo, "Expected 'Source' property on target type");
                    Assert.AreEqual(derivedType, pInfo.PropertyType, "'Source' property should have been of type " + derivedType);

                    assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Source' property");

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("SourceKey", value, "Expected 'thisKey' to be SourceKey, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("TheKey", value, "Expected 'otherKey' to be TheKey, not " + value);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: bidirectional 1:1 association is legal from derived entity to other derived entity identified only through [Include]")]
        public void Inherit_Gen_Assoc_Bi_Derived_To_Included_Derived()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Assoc_BD2ID_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Assoc_BD2ID_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Assoc_BD2ID_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Source_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Source_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Other entity generated (declared only via [Include] on derived's assoc prop)
                    // -----------------------------------------
                    Type targetType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Target_Derived).FullName);
                    Assert.IsNotNull(targetType, "Expected to see other derived type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ---------------------------------------------------------------------------------------
                    // The root of the target derived entity type should not be visible -- nothing exposed it
                    // ---------------------------------------------------------------------------------------
                    Type targetRootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Target_Root).FullName);
                    Assert.IsNull(targetRootType, "Not expected to see other root type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ----------------------------------------------
                    // DerivedType.Target property
                    //  - should have been generated
                    //  - should be of the type of the Other entity (generated)
                    //  - should have carried the [Association] attribute along
                    PropertyInfo pInfo = derivedType.GetProperty("Target");
                    Assert.IsNotNull(pInfo, "Expected 'Target' property on derived type");
                    Assert.AreEqual(targetType, pInfo.PropertyType, "'Target' property should have been of type " + targetType);

                    CustomAttributeData assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Target' property");

                    string value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("OtherId", value, "Expected 'thisKey' to be OtherId, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("Id", value, "Expected 'otherKey' to be Id, not " + value);

                    // ----------------------------------------------
                    // Target.Source property
                    //  - should have been generated
                    //  - should be of the type of the source entity (generated)
                    //  - should have carried the [Association] attribute along
                    pInfo = targetType.GetProperty("Source");
                    Assert.IsNotNull(pInfo, "Expected 'Source' property on target type");
                    Assert.AreEqual(derivedType, pInfo.PropertyType, "'Source' property should have been of type " + derivedType);

                    assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Source' property");

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("SourceKey", value, "Expected 'thisKey' to be SourceKey, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("TheKey", value, "Expected 'otherKey' to be TheKey, not " + value);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: bidirectional 1:M association is legal from derived entity to other derived entity identified only through [Include]")]
        public void Inherit_Gen_Assoc_Bi_Derived_To_Included_Derived_OneToMany()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Assoc_BD2ID_OneToMany_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Assoc_BD2ID_OneToMany_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Assoc_BD2ID_OneToMany_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Source_Root_OneToMany).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Source_Derived_OneToMany).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Other entity generated (declared only via [Include] on derived's assoc prop)
                    // -----------------------------------------
                    Type targetType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Target_Derived_OneToMany).FullName);
                    Assert.IsNotNull(targetType, "Expected to see other derived type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ---------------------------------------------------------------------------------------
                    // The root of the target derived entity type should not be visible -- nothing exposed it
                    // ---------------------------------------------------------------------------------------
                    Type targetRootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Target_Root_OneToMany).FullName);
                    Assert.IsNull(targetRootType, "Not expected to see other root type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ----------------------------------------------
                    // DerivedType.Target property
                    //  - should have been generated
                    //  - should be of the type of the Other entity (generated)
                    //  - should have carried the [Association] attribute along
                    PropertyInfo pInfo = derivedType.GetProperty("Targets");
                    Assert.IsNotNull(pInfo, "Expected 'Targets' property on derived type");
                    Type propType = pInfo.PropertyType;
                    Assert.IsTrue(propType.Name.StartsWith("EntityCollection"), "Expected 'Targets' property to be EntityCollection, not " + propType.Name);
                    Assert.IsTrue(propType.IsGenericType, "Expected 'Targets' property to be generic");
                    Type genericType = propType.GetGenericArguments()[0];
                    Assert.AreEqual(targetType, genericType, "'Targets' property should have been of type " + targetType + ", not " + genericType);

                    CustomAttributeData assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Targets' property");

                    string value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("OtherId", value, "Expected 'thisKey' to be OtherId, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("Id", value, "Expected 'otherKey' to be Id, not " + value);

                    // ----------------------------------------------
                    // Target.Source property
                    //  - should have been generated
                    //  - should be of the type of the source entity (generated)
                    //  - should have carried the [Association] attribute along
                    pInfo = targetType.GetProperty("Source");
                    Assert.IsNotNull(pInfo, "Expected 'Source' property on target type");
                    propType = pInfo.PropertyType;
                    Assert.AreEqual(derivedType, propType, "'Sources' property should have been of type " + derivedType + ", not " + propType);

                    assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Source' property");

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("SourceKey", value, "Expected 'thisKey' to be SourceKey, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("TheKey", value, "Expected 'otherKey' to be TheKey, not " + value);
                }
            }
        }


        [TestMethod]
        [Description("Inheritance: bidirectional M:1 association is legal from derived entity to other derived entity identified only through [Include]")]
        public void Inherit_Gen_Assoc_Bi_Derived_To_Included_Derived_ManyToOne()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Assoc_BD2ID_ManyToOne_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + string.Join("\r\n", asmGen.ConsoleLogger.ErrorMessages.ToArray()));

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Assoc_BD2ID_ManyToOne_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Assoc_BD2ID_ManyToOne_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Source_Root_ManyToOne).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Source_Derived_ManyToOne).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Other entity generated (declared only via [Include] on derived's assoc prop)
                    // -----------------------------------------
                    Type targetType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Target_Derived_ManyToOne).FullName);
                    Assert.IsNotNull(targetType, "Expected to see other derived type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ---------------------------------------------------------------------------------------
                    // The root of the target derived entity type should not be visible -- nothing exposed it
                    // ---------------------------------------------------------------------------------------
                    Type targetRootType = asmGen.GetGeneratedType(typeof(Inherit_Assoc_BD2ID_Target_Root_ManyToOne).FullName);
                    Assert.IsNull(targetRootType, "Not expected to see other root type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ----------------------------------------------
                    // DerivedType.Target property
                    //  - should have been generated
                    //  - should be of the type of the Other entity (generated)
                    //  - should have carried the [Association] attribute along
                    PropertyInfo pInfo = derivedType.GetProperty("Target");
                    Assert.IsNotNull(pInfo, "Expected 'Target' property on derived type");
                    Type propType = pInfo.PropertyType;
                    Assert.AreEqual(targetType, propType, "'Target' property should have been of type " + targetType + ", not " + propType);

                    CustomAttributeData assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Targets' property");

                    string value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("OtherId", value, "Expected 'thisKey' to be OtherId, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("Id", value, "Expected 'otherKey' to be Id, not " + value);

                    // ----------------------------------------------
                    // Target.Sources property
                    //  - should have been generated
                    //  - should be of the type of the source entity (generated)
                    //  - should have carried the [Association] attribute along
                    pInfo = targetType.GetProperty("Sources");
                    Assert.IsNotNull(pInfo, "Expected 'Sources' property on target type");
                    propType = pInfo.PropertyType;
                    Assert.IsTrue(propType.Name.StartsWith("EntityCollection"), "Expected 'Sources' property to be EntityCollection, not " + propType.Name);
                    Assert.IsTrue(propType.IsGenericType, "Expected 'Sources' property to be generic");
                    Type genericType = propType.GetGenericArguments()[0];
                    Assert.AreEqual(derivedType, genericType, "'Sources' property should have been of type " + derivedType + ", not " + genericType);

                    assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Source' property");

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("SourceKey", value, "Expected 'thisKey' to be SourceKey, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("TheKey", value, "Expected 'otherKey' to be TheKey, not " + value);
                }
            }
        }

        #endregion // Association


        #region Projection

        [TestMethod]
        [Description("Inheritance: bidirectional 1:1 association with projection is legal from derived entity to other derived entity identified only through [Include]")]
        public void Inherit_Gen_Projection_Bi_Derived_To_Included_Derived()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Projection_BD2ID_DomainService) }))
                {
                    DomainServiceCatalog cat = asmGen.DomainServiceCatalog;

                    string generatedCode = asmGen.GeneratedCode;

                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Projection_BD2ID_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Projection_BD2ID_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Source_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Source_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Other entity generated (declared only via [Include] on derived's assoc prop)
                    // -----------------------------------------
                    Type targetType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Target_Derived).FullName);
                    Assert.IsNotNull(targetType, "Expected to see other derived type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ---------------------------------------------------------------------------------------
                    // The root of the target derived entity type should not be visible -- nothing exposed it
                    // ---------------------------------------------------------------------------------------
                    Type targetRootType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Target_Root).FullName);
                    Assert.IsNull(targetRootType, "Not expected to see other root type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ----------------------------------------------
                    // DerivedType.Target property
                    //  - should have been generated
                    //  - should be of the type of the Other entity (generated)
                    //  - should have carried the [Association] attribute along
                    PropertyInfo pInfo = derivedType.GetProperty("Target");
                    Assert.IsNotNull(pInfo, "Expected 'Target' property on derived type");
                    Assert.AreEqual(targetType, pInfo.PropertyType, "'Target' property should have been of type " + targetType);

                    CustomAttributeData assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Target' property");

                    string value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("OtherId", value, "Expected 'thisKey' to be OtherId, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("Id", value, "Expected 'otherKey' to be Id, not " + value);

                    // ----------------------------------------------
                    // Target.Source property
                    //  - should have been generated
                    //  - should be of the type of the source entity (generated)
                    //  - should have carried the [Association] attribute along
                    pInfo = targetType.GetProperty("Source");
                    Assert.IsNotNull(pInfo, "Expected 'Source' property on target type");
                    Assert.AreEqual(derivedType, pInfo.PropertyType, "'Source' property should have been of type " + derivedType);

                    assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Source' property");

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("SourceKey", value, "Expected 'thisKey' to be SourceKey, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("TheKey", value, "Expected 'otherKey' to be TheKey, not " + value);


                    // -------------------------------------------------
                    // Projections should have generated new properties
                    // -------------------------------------------------

                    // Our "source" entity projects one prop from root and one from derived
                    pInfo = derivedType.GetProperty("ProjectedTargetRootProp");
                    Assert.IsNotNull(pInfo, "Expected to find projected property on " + derivedType.Name + ": ProjectedTargetRootProp");

                    pInfo = derivedType.GetProperty("ProjectedTargetDerivedProp");
                    Assert.IsNotNull(pInfo, "Expected to find projected property on " + derivedType.Name + ": ProjectedTargetDerivedProp");
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: bidirectional 1:1 association with projection is legal from target derived entity to source derived entity identified only through [Include]")]
        public void Inherit_Gen_Projection_Bi_Derived_To_Included_Derived_Reversed()
        {
            // This test differs from the one above only in that project elements occur
            // on the target of the association rather than the source
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Projection_BD2ID_Reversed_DomainService) }))
                {
                    DomainServiceCatalog cat = asmGen.DomainServiceCatalog;

                    string generatedCode = asmGen.GeneratedCode;

                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Projection_BD2ID_Reversed_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Projection_BD2ID_Reversed_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Reversed_Source_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Reversed_Source_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Other entity generated (declared only via [Include] on derived's assoc prop)
                    // -----------------------------------------
                    Type targetType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Reversed_Target_Derived).FullName);
                    Assert.IsNotNull(targetType, "Expected to see other derived type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ---------------------------------------------------------------------------------------
                    // The root of the target derived entity type should not be visible -- nothing exposed it
                    // ---------------------------------------------------------------------------------------
                    Type targetRootType = asmGen.GetGeneratedType(typeof(Inherit_Projection_BD2ID_Reversed_Target_Root).FullName);
                    Assert.IsNull(targetRootType, "Not expected to see other root type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // ----------------------------------------------
                    // DerivedType.Target property
                    //  - should have been generated
                    //  - should be of the type of the Other entity (generated)
                    //  - should have carried the [Association] attribute along
                    PropertyInfo pInfo = derivedType.GetProperty("Target");
                    Assert.IsNotNull(pInfo, "Expected 'Target' property on derived type");
                    Assert.AreEqual(targetType, pInfo.PropertyType, "'Target' property should have been of type " + targetType);

                    CustomAttributeData assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Target' property");

                    string value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("OtherId", value, "Expected 'thisKey' to be OtherId, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("Id", value, "Expected 'otherKey' to be Id, not " + value);

                    // ----------------------------------------------
                    // Target.Source property
                    //  - should have been generated
                    //  - should be of the type of the source entity (generated)
                    //  - should have carried the [Association] attribute along
                    pInfo = targetType.GetProperty("Source");
                    Assert.IsNotNull(pInfo, "Expected 'Source' property on target type");
                    Assert.AreEqual(derivedType, pInfo.PropertyType, "'Source' property should have been of type " + derivedType);

                    assocCad = AssemblyGenerator.GetCustomAttributeData(pInfo, typeof(AssociationAttribute)).SingleOrDefault();
                    Assert.IsNotNull(assocCad, "Could not find Association custom attribute data on 'Source' property");

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "thisKey");
                    Assert.AreEqual("SourceKey", value, "Expected 'thisKey' to be SourceKey, not " + value);

                    value = AssemblyGenerator.GetCustomAttributeValue<string>(assocCad, "otherKey");
                    Assert.AreEqual("TheKey", value, "Expected 'otherKey' to be TheKey, not " + value);


                    // -------------------------------------------------
                    // Projections should have generated new properties
                    // -------------------------------------------------

                    // Our "target" entity projects one prop from root and one from derived
                    pInfo = targetType.GetProperty("ProjectedSourceRootProp");
                    Assert.IsNotNull(pInfo, "Expected to find projected property on " + targetType.Name + ": ProjectedSourceRootProp");

                    pInfo = targetType.GetProperty("ProjectedSourceDerivedProp");
                    Assert.IsNotNull(pInfo, "Expected to find projected property on " + targetType.Name + ": ProjectedSourceDerivedProp");
                }
            }
        }

        #endregion // Projection

        [TestMethod]
        [Description("Inheritance: validates buddy class on derived types works")]
        public void Inherit_Gen_Buddy()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Buddy_DomainService) }))
                {


                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // ---------------------------------
                    // Check visible root type
                    // ---------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Buddy_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from Entity, and be public, and not be sealed
                    Assert.AreEqual(TypeConstants.EntityTypeFullName, rootType.BaseType.FullName);
                    Assert.IsFalse(rootType.IsSealed);
                    Assert.IsTrue(rootType.IsPublic);

                    // Base type must declare TheKey and GetIdentity as well as its property,
                    AssertDeclaresMembers(rootType, "TheKey", "GetIdentity", "RootProperty");

                    // Validate that root type has generated [KnownType] for each visible attribute
                    // Note that one came from non-exposed base type and the other came from the
                    // visible root type itself
                    AssertKnownTypeAttributes(asmGen, rootType, true /* expected */,
                                                    typeof(Inherit_Buddy_Derived1),
                                                    typeof(Inherit_Buddy_Derived2));

                    // check for default ctor
                    ConstructorInfo ctor = rootType.GetConstructor(Array.Empty<Type>());
                    Assert.IsNotNull(ctor);
                    Assert.AreEqual(rootType, ctor.DeclaringType, "Root ctor should have been declared in root");

                    // ---------------------------------
                    // Check derived1 type
                    // ---------------------------------
                    Type derived1Type = asmGen.GetGeneratedType(typeof(Inherit_Buddy_Derived1).FullName);
                    Assert.IsNotNull(derived1Type, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from our visible base type, public, not sealed (because it has subtypes)
                    Assert.AreEqual(rootType, derived1Type.BaseType);
                    Assert.IsFalse(derived1Type.IsSealed);
                    Assert.IsTrue(derived1Type.IsPublic);

                    // Should have generated its properties
                    AssertDeclaresMembers(derived1Type, "Derived1Property");

                    // Should not have declared the base type members, but should see them
                    AssertHasButDoesNotDeclareMembers(derived1Type, "TheKey", "GetIdentity", "RootProperty");

                    // check for default ctor
                    ctor = derived1Type.GetConstructor(Array.Empty<Type>());
                    Assert.IsNotNull(ctor);
                    Assert.AreEqual(derived1Type, ctor.DeclaringType, "Derived ctor should have been declared in derived type");

                    // ---------------------------------
                    // Check derived2 type
                    // ---------------------------------
                    Type derived2Type = asmGen.GetGeneratedType(typeof(Inherit_Buddy_Derived2).FullName);
                    Assert.IsNotNull(derived2Type, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from other derived type, public, sealed (because it has no subtypes)
                    Assert.AreEqual(derived1Type, derived2Type.BaseType);
                    Assert.IsTrue(derived2Type.IsSealed);
                    Assert.IsTrue(derived2Type.IsPublic);

                    // Should have generated its properties
                    AssertDeclaresMembers(derived2Type, "Derived2Property");

                    // Should not have declared the base type members, but should see them
                    AssertHasButDoesNotDeclareMembers(derived2Type, "TheKey", "GetIdentity", "RootProperty", "Derived1Property");

                    // check for default ctor
                    ctor = derived2Type.GetConstructor(Array.Empty<Type>());
                    Assert.IsNotNull(ctor);
                    Assert.AreEqual(derived2Type, ctor.DeclaringType, "Derived ctor should have been declared in derived type");

                    // -----------------------------------------
                    // Check DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_Buddy_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Buddy_DomainContext");

                    // validate EntitySet property
                    PropertyInfo pInfo = domainContextType.GetProperty("Inherit_Buddy_Roots");  // note plural
                    Assert.IsNotNull(pInfo, "Did not find EntitySet");

                    Type entityListType = pInfo.PropertyType;
                    Assert.IsTrue(entityListType.IsGenericType, "EntitySet should have been generic type");
                    Assert.IsTrue(entityListType.Name.StartsWith("EntitySet"), "EntitySet should have been of type EntitySet<>");

                    Type[] genericArgs = entityListType.GetGenericArguments();
                    Assert.AreEqual(1, genericArgs.Length, "Expected EntitySet to have 1 generic arg");
                    Assert.AreEqual(rootType, genericArgs[0], "Expected EntitySet generic arg to be root type");

                    // validate query root for root entity
                    MethodInfo mInfo = domainContextType.GetMethod("GetQuery");
                    Assert.IsNotNull(mInfo, "Did not find root entity GetQuery method");

                    Type rootQueryType = mInfo.ReturnType;
                    Assert.IsTrue(rootQueryType.IsGenericType, "Root query should have been generic type");
                    Assert.IsTrue(rootQueryType.Name.StartsWith("EntityQuery"), "Root query should have been of type EntityQuery<>");

                    genericArgs = rootQueryType.GetGenericArguments();
                    Assert.AreEqual(1, genericArgs.Length, "Expected root query to have 1 generic arg");
                    Assert.AreEqual(rootType, genericArgs[0], "Expected root query generic arg to be root type");

                    // validate query root for derived 1 entity
                    mInfo = domainContextType.GetMethod("GetDerived1Query");
                    Assert.IsNotNull(mInfo, "Did not find root entity GetDerivedQuery method");

                    Type derived1QueryType = mInfo.ReturnType;
                    Assert.IsTrue(derived1QueryType.IsGenericType, "Derived query should have been generic type");
                    Assert.IsTrue(derived1QueryType.Name.StartsWith("EntityQuery"), "Derived query should have been of type EntityQuery<>");

                    genericArgs = derived1QueryType.GetGenericArguments();
                    Assert.AreEqual(1, genericArgs.Length, "Expected root query to have 1 generic arg");
                    Assert.AreEqual(derived1Type, genericArgs[0], "Expected root query generic arg to be derived type");

                    // validate query root for derived 1 entity
                    mInfo = domainContextType.GetMethod("GetDerived2Query");
                    Assert.IsNotNull(mInfo, "Did not find root entity GetDerivedQuery method");

                    Type derived2QueryType = mInfo.ReturnType;
                    Assert.IsTrue(derived2QueryType.IsGenericType, "Derived query should have been generic type");
                    Assert.IsTrue(derived2QueryType.Name.StartsWith("EntityQuery"), "Derived query should have been of type EntityQuery<>");

                    genericArgs = derived2QueryType.GetGenericArguments();
                    Assert.AreEqual(1, genericArgs.Length, "Expected root query to have 1 generic arg");
                    Assert.AreEqual(derived2Type, genericArgs[0], "Expected root query generic arg to be derived type");

                    // -------------------------------
                    // Check validation attributes
                    // -------------------------------

                    // Root.RootProperty should be [StringLength(10)]
                    MemberInfo memberInfo = rootType.GetProperty("RootProperty");
                    CustomAttributeData rootPropCad = AssemblyGenerator.GetCustomAttributeData(memberInfo, typeof(StringLengthAttribute)).SingleOrDefault();
                    Assert.IsNotNull(rootPropCad, "Could not find RootProperty custom attribute data");
                    int maxLen = AssemblyGenerator.GetCustomAttributeValue<int>(rootPropCad, "maximumLength");
                    Assert.AreEqual(10, maxLen, "RootProperty [StringLength] was incorrect");

                    // Derived1.Derived1Property should be [StringLength(11)]
                    memberInfo = derived1Type.GetProperty("Derived1Property");
                    CustomAttributeData derived1PropCad = AssemblyGenerator.GetCustomAttributeData(memberInfo, typeof(StringLengthAttribute)).SingleOrDefault();
                    Assert.IsNotNull(derived1PropCad, "Could not find Derived1Property custom attribute data");
                    maxLen = AssemblyGenerator.GetCustomAttributeValue<int>(derived1PropCad, "maximumLength");
                    Assert.AreEqual(11, maxLen, "Derived1Property [StringLength] was incorrect");

                    // Derived2.Derived2Property should be [StringLength(12)]
                    memberInfo = derived2Type.GetProperty("Derived2Property");
                    CustomAttributeData derived2PropCad = AssemblyGenerator.GetCustomAttributeData(memberInfo, typeof(StringLengthAttribute)).SingleOrDefault();
                    Assert.IsNotNull(derived2PropCad, "Could not find Derived2Property custom attribute data");
                    maxLen = AssemblyGenerator.GetCustomAttributeValue<int>(derived2PropCad, "maximumLength");
                    Assert.AreEqual(12, maxLen, "Derived2Property [StringLength] was incorrect");

                    // Derived2.RootProperty should be [StringLength(10)]
                    memberInfo = derived2Type.GetProperty("RootProperty");
                    rootPropCad = AssemblyGenerator.GetCustomAttributeData(memberInfo, typeof(StringLengthAttribute)).SingleOrDefault();
                    Assert.IsNotNull(rootPropCad, "Could not find RootProperty custom attribute data");
                    maxLen = AssemblyGenerator.GetCustomAttributeValue<int>(rootPropCad, "maximumLength");
                    Assert.AreEqual(10, maxLen, "Derived2.RootProperty [StringLength] was incorrect");
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: checks generation of partial methods")]
        public void Inherit_Gen_Partial_Methods()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Basic_DomainService) }))
                {
                    // We add this extra code into the compile.
                    // C# will generate compile errors if you attempt to implement a partial
                    // method that has not been declared.
                    // VB will not, so the VB part of this test is somewhat incomplete -- the
                    // methods will always exist whether we generated partial declarations or not.
                    string userCode = isCSharp
                        ? @"namespace Inheritance.Tests
                            {
                                using System;
                                using OpenRiaServices.Client;
                                public sealed partial class Inherit_Basic_Derived : Inherit_Basic_Root
                                {
                                    partial void OnCreated() {}
                                    partial void OnDerivedPropertyChanging(string value) {}
                                    partial void OnDerivedPropertyChanged() {}
                                }
                                public partial class Inherit_Basic_Root : Entity
                                {
                                    partial void OnCreated() {}
                                    partial void OnRootPropertyChanging(string value) {}
                                    partial void OnRootPropertyChanged() {}
                                    partial void OnTheKeyChanging(string value) {}
                                    partial void OnTheKeyChanged() {}
                                }
                            }
                            "
                        : @"
                            Option Compare Binary
                            Option Infer On
                            Option Strict On
                            Option Explicit On

                            Imports System
                            Imports OpenRiaServices.Client

                            Namespace Inheritance.Tests
                                
                                Partial Public NotInheritable Class Inherit_Basic_Derived
                                    Inherits Inherit_Basic_Root
                                    
                                    Private Sub OnCreated()
                                    End Sub
                                    Private Sub OnDerivedPropertyChanging(ByVal value As String)
                                    End Sub
                                    Private Sub OnDerivedPropertyChanged()
                                    End Sub
                                End Class
                                
                                 Partial Public Class Inherit_Basic_Root
                                    Inherits Entity
                                    
                                    Private Sub OnCreated()
                                    End Sub
                                    Private Sub OnRootPropertyChanging(ByVal value As String)
                                    End Sub
                                    Private Sub OnRootPropertyChanged()
                                    End Sub
                                    Private Sub OnTheKeyChanging(ByVal value As String)
                                    End Sub
                                    Private Sub OnTheKeyChanged()
                                    End Sub

                                End Class
                            End Namespace
                            ";

                    asmGen.AddUserCode(userCode);

                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // ---------------------------------
                    // Check visible root type
                    // ---------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    AssertPartialMethodDeclared(rootType, "OnCreated");
                    AssertPartialMethodDeclared(rootType, "OnRootPropertyChanging");
                    AssertPartialMethodDeclared(rootType, "OnRootPropertyChanged");
                    AssertPartialMethodDeclared(rootType, "OnTheKeyChanging");
                    AssertPartialMethodDeclared(rootType, "OnTheKeyChanged");

                    // ---------------------------------
                    // Check visible derived type
                    // ---------------------------------
                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    AssertPartialMethodDeclared(derivedType, "OnCreated");
                    AssertPartialMethodDeclared(derivedType, "OnDerivedPropertyChanging");
                    AssertPartialMethodDeclared(derivedType, "OnDerivedPropertyChanged");

                }
            }
        }

        [TestMethod]
        [Description("Inheritance: validates the DomainServiceDescription builds correct known type hierarchy for abstracts")]
        public void Inherit_DomainServiceDescription_Type_Hierarchies()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Abstract_Root_DomainService) }))
                {
                    DomainServiceDescription dsd = asmGen.DomainServiceCatalog.DomainServiceDescriptions.FirstOrDefault();
                    Assert.IsNotNull(dsd, "Expected DomainServiceDescription");

                    // There is only one root entity -- validate this
                    Type[] rootTypes = dsd.RootEntityTypes.ToArray();
                    Assert.AreEqual(1, rootTypes.Length, "Expected only one root type");

                    // Verify we can determine all the derived types via the hierarchy alone
                    Type[] expectedDeriveds = new Type[]
                    {
                        typeof(Inherit_Abstract_Derived),
                        typeof(Inherit_Abstract_Derived_A),
                        typeof(Inherit_Abstract_Derived_B),
                        typeof(Inherit_Abstract_Derived_C)
                    };

                    Type[] expectedBases = new Type[]
                    {
                        typeof(Inherit_Abstract_Root),
                        typeof(Inherit_Abstract_Derived),
                        typeof(Inherit_Abstract_Derived),
                        typeof(Inherit_Abstract_Derived_A)
                    };

                    // Get all the derived types from the root
                    List<Type> derivedTypes = dsd.GetEntityDerivedTypes(typeof(Inherit_Abstract_Root)).ToList();
                    Assert.AreEqual(expectedDeriveds.Length, derivedTypes.Count, "Expected this many derived types from root");

                    for (int i = 0; i < expectedDeriveds.Length; ++i)
                    {
                        Type derivedType = expectedDeriveds[i];
                        Type baseType = expectedBases[i];
                        Assert.IsTrue(derivedTypes.Contains(derivedType), "Did not find " + derivedType + " in list of derived types");
                        Assert.AreEqual(baseType, dsd.GetEntityBaseType(derivedType), "Expected " + baseType + " to be base of " + derivedType);
                        Assert.AreEqual(typeof(Inherit_Abstract_Root), dsd.GetRootEntityType(derivedType), "Wrong root type");
                    }

                    // Now verify the known type hierarchy -- should have one entry per entity
                    Dictionary<Type, HashSet<Type>> knownTypes = dsd.EntityKnownTypes;
                    Assert.AreEqual(5, knownTypes.Count, "Expected this many entities in known type hash");

                    // Validate all entity types have an entry
                    Assert.IsTrue(knownTypes.ContainsKey(typeof(Inherit_Abstract_Root)), "Did not find root in hash");
                    foreach (Type derivedType in expectedDeriveds)
                    {
                        Assert.IsTrue(knownTypes.ContainsKey(derivedType), "Did not find " + derivedType + " in hash");
                    }

                    // Verify the root rolled up all the known types from all deriveds
                    List<Type> allKnownTypes = knownTypes[typeof(Inherit_Abstract_Root)].ToList();
                    Assert.AreEqual(expectedDeriveds.Length, allKnownTypes.Count, "Expected root's list of known types to include all deriveds");

                    foreach (Type expected in expectedDeriveds)
                    {
                        Assert.IsTrue(allKnownTypes.Contains(expected), "Did not find " + expected + " in list of known types");
                    }
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: root entity may be abstract if it specifies a concrete derived type")]
        public void Inherit_Gen_Abstract_Root_With_Concrete_Derived()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Abstract_Root_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // ---------------------------------
                    // Check visible root type
                    // ---------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Abstract_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from Entity, and be public, and not be sealed
                    Assert.AreEqual(TypeConstants.EntityTypeFullName, rootType.BaseType.FullName);
                    Assert.IsFalse(rootType.IsSealed);
                    Assert.IsTrue(rootType.IsPublic);
                    Assert.IsTrue(rootType.IsAbstract, "Expected generated type to preserve Abstract");

                    // Base type must declare TheKey and GetIdentity
                    AssertDeclaresMembers(rootType, "TheKey", "GetIdentity");

                    // check for default ctor
                    ConstructorInfo ctor = rootType.GetConstructor(Array.Empty<Type>());
                    Assert.IsNull(ctor, "Did not expect to find public ctor on abstract root");

                    string[] expectedAbstracts = new string[] {
                        typeof(Inherit_Abstract_Derived).FullName,
                        typeof(Inherit_Abstract_Derived_A).FullName
                    };

                    string[] expectedConcretes = new string[] {
                        typeof(Inherit_Abstract_Derived_B).FullName,
                        typeof(Inherit_Abstract_Derived_C).FullName
                    };

                    foreach (string s in expectedAbstracts)
                    {
                        Type derivedType = asmGen.GetGeneratedType(s);
                        Assert.IsNotNull(derivedType, "Expected to see entity derived type " + s + " but saw:\r\n" + asmGen.GeneratedTypeNames);
                        Assert.IsTrue(derivedType.IsAbstract, "Expected " + s + " to be abstract");
                    }

                    foreach (string s in expectedConcretes)
                    {
                        Type derivedType = asmGen.GetGeneratedType(s);
                        Assert.IsNotNull(derivedType, "Expected to see entity derived type " + s + " but saw:\r\n" + asmGen.GeneratedTypeNames);
                        Assert.IsFalse(derivedType.IsAbstract, "Expected " + s + " to be concrete");
                    }
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: root entity type without [Key] causes error")]
        public void Inherit_Gen_No_Key_Error()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_No_Key_DomainService) }))
                {
                    // Trigger code generation but don't attempt to build
                    string generatedCode = asmGen.GeneratedCode;

                    Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "Missing [Key] property should not have generated code");

                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.Entity_Has_No_Key_Properties, typeof(Inherit_No_Key_Entity).Name, typeof(Inherit_No_Key_DomainService).Name);
                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: derived entity type with [Key] causes error")]
        public void Inherit_Gen_Derived_Key_Error()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Derived_Key_DomainService) }))
                {
                    // Trigger code generation but don't attempt to build
                    string generatedCode = asmGen.GeneratedCode;

                    Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "[Key] property on derived type should not have generated code");

                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.DerivedEntityCannotHaveKey, "TheDerivedKey", typeof(Inherit_Derived_Key_Derived).Name);
                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: derived entity type not specified in [KnownType] causes error")]
        public void Inherit_Gen_Missing_KnownType_Error()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Missing_KnownType_DomainService) }))
                {
                    // Trigger code generation but don't attempt to build
                    string generatedCode = asmGen.GeneratedCode;

                    Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "Missing [KnownType] for derived type should not have generated code");

                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.KnownTypeAttributeRequired, typeof(Inherit_Missing_KnownType_Derived).Name, typeof(Inherit_Missing_KnownType_Root).Name);
                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: entity properties may be polymorphic")]
        public void Inherit_Gen_Polymorphic_Property()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Polymorphic_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // ---------------------------------
                    // Check visible root type
                    // ---------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Polymorphic_Entity).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from Entity, and be public, and not be sealed
                    Assert.AreEqual(TypeConstants.EntityTypeFullName, rootType.BaseType.FullName);
                    Assert.IsFalse(rootType.IsSealed);
                    Assert.IsTrue(rootType.IsPublic);

                    // Base type must declare TheKey and GetIdentity
                    AssertDeclaresMembers(rootType, "TheKey", "GetIdentity", "VirtualProperty", "BaseProperty");

                    // VirtualProperty should appear on root and be concrete
                    PropertyInfo pInfo = rootType.GetProperty("VirtualProperty");
                    Assert.IsNotNull(pInfo, "Expected to find VirtualProperty");
                    Assert.IsNotNull(pInfo.GetGetMethod(), "Expected VirtualProperty getter");
                    Assert.IsNotNull(pInfo.GetSetMethod(), "Expected VirtualProperty setter");
                    Assert.IsFalse(pInfo.GetGetMethod().IsAbstract, "Did not expact Abstract on VirtualProperty");
                    Assert.IsFalse(pInfo.GetGetMethod().IsVirtual, "Did not expact Virtual on VirtualProperty");

                    // We have an abstract "hole" that should not appear
                    Type abstractHoleType = asmGen.GetGeneratedType(typeof(Inherit_Polymorphic_Abstract_Derived).FullName);
                    Assert.IsNull(abstractHoleType, "Did not expect " + typeof(Inherit_Polymorphic_Abstract_Derived) + " to be generated.");

                    // Derived from the abstract hole is this type, which should lift some properties
                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Polymorphic_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see entity derived type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Should have lifted AbstractProperty from missing abstract intermediate
                    AssertDeclaresMembers(derivedType, "AbstractProperty");

                    // Should inherit some properties from the base only
                    AssertHasButDoesNotDeclareMembers(derivedType, "VirtualProperty", "BaseProperty");

                    // AbstractProperty should appear on root and be concrete
                    pInfo = derivedType.GetProperty("AbstractProperty");
                    Assert.IsNotNull(pInfo, "Expected to find AbstractProperty");
                    Assert.IsNotNull(pInfo.GetGetMethod(), "Expected AbstractProperty getter");
                    Assert.IsNotNull(pInfo.GetSetMethod(), "Expected AbstractProperty setter");
                    Assert.IsFalse(pInfo.GetGetMethod().IsAbstract, "Did not expact Abstract on AbstractProperty");
                    Assert.IsFalse(pInfo.GetGetMethod().IsVirtual, "Did not expact Virtual on AbstractProperty");

                }
            }
        }

        [TestMethod]
        [Description("Inheritance: using 'new' to override a property generates an error")]
        public void Inherit_Gen_New_Property_Illegal()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Polymorphic_New_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNull(assy, "Assembly should have failed to build: " + asmGen.ConsoleLogger.Errors);

                    // Code gen should fail and log an error for the attempt to override BaseProperty on the non-exposed entity
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.Entity_Property_Redefined, typeof(Inherit_Polymorphic_New_Derived), "BaseProperty", typeof(Inherit_Polymorphic_New_Entity));
                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);

                    //// Code gen should fail and log an error for the attempt to override BaseProperty on the non-exposed entity
                    //errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.Entity_Property_Redefined, typeof(Inherit_Polymorphic_New_Derived), "VirtualProperty", typeof(Inherit_Polymorphic_New_Entity));
                    //TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Code generator flattens the hierarchy when it has non-visible types")]
        public void Inherit_Gen_Flatten()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Flatten_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // ---------------------------------
                    // Check hidden base type -- should not have been generated
                    // ---------------------------------
                    Type hiddenBaseType = asmGen.GetGeneratedType(typeof(Inherit_Flatten_HiddenBase).FullName);
                    Assert.IsNull(hiddenBaseType, "Should not have generated HiddenBase");


                    // ---------------------------------
                    // Check visible root type
                    // ---------------------------------
                    Type visibleRootType = asmGen.GetGeneratedType(typeof(Inherit_Flatten_VisibleRoot).FullName);
                    Assert.IsNotNull(visibleRootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from Entity, and be public, and not be sealed
                    Assert.AreEqual(TypeConstants.EntityTypeFullName, visibleRootType.BaseType.FullName);
                    Assert.IsFalse(visibleRootType.IsSealed);
                    Assert.IsTrue(visibleRootType.IsPublic);

                    // Base type must declare TheKey and GetIdentity as well as its property,
                    // even though the [Key] property came from an invisible base
                    AssertDeclaresMembers(visibleRootType, "TheKey", "GetIdentity", "VisibleRootProperty", "HiddenBaseProperty");

                    // Validate that root type has generated [KnownType] for each visible attribute
                    AssertKnownTypeAttributes(asmGen, visibleRootType, true /* expected */,
                                                    typeof(Inherit_Flatten_Visible1),
                                                    typeof(Inherit_Flatten_Visible2));

                    // Validate we did not gen known types for invisible types
                    AssertKnownTypeAttributes(asmGen, visibleRootType, false /* expected */,
                                typeof(Inherit_Flatten_HiddenBase),
                                typeof(Inherit_Flatten_Hidden1),
                                typeof(Inherit_Flatten_Hidden2));

                    // ---------------------------------
                    // Check that the skipped types did not get generated
                    // ---------------------------------
                    Type skippedType = asmGen.GetGeneratedType(typeof(Inherit_Flatten_Hidden1).FullName);
                    Assert.IsNull(skippedType, "Expected to *not* see explicitly first skipped derived type");

                    skippedType = asmGen.GetGeneratedType(typeof(Inherit_Flatten_Hidden2).FullName);
                    Assert.IsNull(skippedType, "Expected to *not* see explicitly second skipped derived type");

                    // ---------------------------------
                    // Check that first visible derived type was generated
                    // ---------------------------------
                    Type visible1Type = asmGen.GetGeneratedType(typeof(Inherit_Flatten_Visible1).FullName);
                    Assert.IsNotNull(visible1Type, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from our visible base type, public, not sealed (because it has subtypes)
                    Assert.AreEqual(visibleRootType, visible1Type.BaseType);
                    Assert.IsFalse(visible1Type.IsSealed);
                    Assert.IsTrue(visible1Type.IsPublic);

                    // Should have lifted skipped1's property and generated its own
                    AssertDeclaresMembers(visible1Type, "Hidden1Property", "Visible1Property");

                    // Should not have declared the base type members, but should see them
                    AssertHasButDoesNotDeclareMembers(visible1Type, "TheKey", "GetIdentity", "VisibleRootProperty", "HiddenBaseProperty");

                    // ---------------------------------
                    // Check that second visible derived type was generated
                    // ---------------------------------
                    Type visible2Type = asmGen.GetGeneratedType(typeof(Inherit_Flatten_Visible2).FullName);
                    Assert.IsNotNull(visible2Type, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // Must derive from our visible derived type, public, sealed
                    Assert.AreEqual(visible1Type, visible2Type.BaseType);
                    Assert.IsTrue(visible2Type.IsSealed);
                    Assert.IsTrue(visible2Type.IsPublic);

                    // Should have lifted skipped2's property and generated its own
                    AssertDeclaresMembers(visible2Type, "Hidden2Property", "Visible2Property");

                    // Should not have declared the base type members, but should see them
                    AssertHasButDoesNotDeclareMembers(visible2Type, "TheKey", "GetIdentity", "VisibleRootProperty");

                    // Should not have re-lifted properties from skipped1 (visible1 did that for us)
                    AssertHasButDoesNotDeclareMembers(visible2Type, "Hidden1Property", "Visible1Property");


                }
            }
        }

        [TestMethod]
        [Description("Inheritance: insert method on derived entity only is an error")]
        public void Inherit_Gen_No_Root_Insert_Is_Error()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_No_Root_Insert_DomainService) }))
                {
                    // Trigger code generation but don't attempt to build
                    string generatedCode = asmGen.GeneratedCode;

                    Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "Missing root insert should not have generated code");

                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.DomainOperation_Required_On_Root, "InsertIt", typeof(Inherit_Basic_Derived).Name, typeof(Inherit_Basic_Root).Name);
                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: update method on derived entity only is an error")]
        public void Inherit_Gen_No_Root_Update_Is_Error()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_No_Root_Update_DomainService) }))
                {
                    // Trigger code generation but don't attempt to build
                    string generatedCode = asmGen.GeneratedCode;

                    Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "Missing root update should not have generated code");

                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.DomainOperation_Required_On_Root, "UpdateIt", typeof(Inherit_Basic_Derived).Name, typeof(Inherit_Basic_Root).Name);
                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: delete method on derived entity only is an error")]
        public void Inherit_Gen_No_Root_Delete_Is_Error()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_No_Root_Delete_DomainService) }))
                {
                    // Trigger code generation but don't attempt to build
                    string generatedCode = asmGen.GeneratedCode;

                    Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "Missing root delete should not have generated code");

                    string errorMessage = string.Format(CultureInfo.CurrentCulture, DomainService_Resource.DomainOperation_Required_On_Root, "DeleteIt", typeof(Inherit_Basic_Derived).Name, typeof(Inherit_Basic_Root).Name);
                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: custom method on derived entity only is legal")]
        public void Inherit_Gen_No_Root_Custom()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_No_Root_Custom_DomainService) }))
                {
                    Assembly assy = asmGen.GeneratedAssembly;
                    Assert.IsNotNull(assy, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

                    TestHelper.AssertNoErrorsOrWarnings(asmGen.ConsoleLogger);

                    // -----------------------------------------
                    // Root and Derived entities were generated
                    // -----------------------------------------
                    Type rootType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Root).FullName);
                    Assert.IsNotNull(rootType, "Expected to see entity base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    Type derivedType = asmGen.GetGeneratedType(typeof(Inherit_Basic_Derived).FullName);
                    Assert.IsNotNull(derivedType, "Expected to see derived base type but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // DomainContext was generated
                    // -----------------------------------------
                    Type domainContextType = asmGen.GetGeneratedType("Inheritance.Tests.Inherit_No_Root_Custom_DomainContext");
                    Assert.IsNotNull(domainContextType, "Expected to find domain context: Inheritance.Tests.Inherit_Basic_CUD_DomainContext but saw:\r\n" + asmGen.GeneratedTypeNames);

                    // -----------------------------------------
                    // Root entity does *NOT* have custom method
                    // -----------------------------------------
                    MethodInfo methodInfo = rootType.GetMethod("CustomIt");
                    Assert.IsNull(methodInfo, "Expected no CustomIt method on root entity");

                    // -----------------------------------------
                    // Derived entity has custom method
                    // -----------------------------------------
                    this.AssertValidCustomMethod(asmGen, derivedType, domainContextType, "CustomIt", new Type[] { typeof(string) });

                    // -------------------------------------
                    // EntityContainer creates list for root
                    // -------------------------------------
                    // TODO: would like better test
                    string expectedCodeGen = isCSharp
                                                ? "this.CreateEntitySet<Inherit_Basic_Root>(EntitySetOperations.None);"
                                                : "Me.CreateEntitySet(Of Inherit_Basic_Root)(EntitySetOperations.None)";

                    if (!asmGen.GeneratedCode.Contains(expectedCodeGen))
                    {
                        Assert.Fail("Expected to see this in generatedCode:\r\n" + expectedCodeGen + "\r\n but instead saw:\r\n" + asmGen.GeneratedCode);
                    }
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: overloaded custom method on derived type is error")]
        [Ignore] // TODO: [Dev10] This generates no errors, but it should
        public void Inherit_Gen_Custom_Overload_Is_Error()
        {
            foreach (bool isCSharp in new bool[] { true, false })
            {
                using (AssemblyGenerator asmGen = new AssemblyGenerator(
                                                    isCSharp,
                    new Type[] { typeof(Inherit_Custom_Overload_DomainService) }))
                {
                    // Trigger code generation but don't attempt to build
                    string generatedCode = asmGen.GeneratedCode;

                    Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "Overloaded custom method should not have generated code");

                    string errorMessage = string.Format(CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_NamingCollision_MemberAlreadyExists,
                        "Inherit_Custom_Overload_DomainContext",
                        "CustomIt");

                    TestHelper.AssertContainsErrors(asmGen.ConsoleLogger, errorMessage);
                }
            }
        }

        [TestMethod]
        [Description("Inheritance: domain service inheritance with EnableClientAccess produces an error")]
        public void Inherit_Gen_DomainService_With_EnableClientAccess_Is_Error()
        {
            Type[] domainServices = new Type[]
            {
                typeof(Inherit_DomainService_Parent),
                typeof(Inherit_DomainService_Child1),
                typeof(Inherit_DomainService_Child2),
            };

            string[] errors = new string[]
            {
                string.Format(Resource.ClientCodeGen_DomainService_Inheritance_Not_Allowed, typeof(Inherit_DomainService_Child1), typeof(Inherit_DomainService_Parent)),
                string.Format(Resource.ClientCodeGen_DomainService_Inheritance_Not_Allowed, typeof(Inherit_DomainService_Child2), typeof(Inherit_DomainService_Parent)),
            };

            TestHelper.GenerateCodeAssertFailure("C#", domainServices, errors);
        }

        internal static void AssertPartialMethodDeclared(Type type, string methodName)
        {
            MethodInfo mInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(mInfo, "Expected to find " + methodName + " partial method in generated type " + type.Name);
        }

        internal static void AssertKnownTypeAttributes(AssemblyGenerator asmGen, MemberInfo memberInfo, bool expected, params Type[] knownTypes)
        {
            // Get all the [KnownType] attributes for the given member
            IList<CustomAttributeData> attributes = AssemblyGenerator.GetCustomAttributeData(memberInfo, typeof(KnownTypeAttribute));
            List<Type> foundKnownTypes = new List<Type>();
            foreach (CustomAttributeData cad in attributes)
            {
                Type foundType = AssemblyGenerator.GetCustomAttributeValue<Type>(cad, "Type");
                if (foundType != null)
                {
                    foundKnownTypes.Add(foundType);
                }
            }

            for (int i = 0; i < knownTypes.Length; ++i)
            {
                bool foundIt = false;
                foreach (CustomAttributeData cad in attributes)
                {
                    Type foundType = AssemblyGenerator.GetCustomAttributeValue<Type>(cad, "Type");

                    // The generated type name will prepend the VS namespace -- be aware of difference
                    string generatedTypeName = asmGen.GetGeneratedTypeName(knownTypes[i].FullName);

                    if (foundType != null && String.Equals(generatedTypeName, foundType.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (!foundIt && expected)
                {
                    string errorMessage = "Expected to find [KnownType] for " + knownTypes[i].FullName + " but instead found:";
                    foreach (Type foundKnownType in foundKnownTypes)
                    {
                        errorMessage += (Environment.NewLine + "  " + foundKnownType.FullName);
                    }
                    Assert.Fail(errorMessage);
                }
                if (foundIt && !expected)
                {
                    Assert.Fail("Did not expect to find " + knownTypes[i].FullName + " in generated known types.");
                }
            }
        }


        /// <summary>
        /// Asserts that the given type declares the specified members
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberNames"></param>
        private void AssertDeclaresMembers(Type type, params string[] memberNames)
        {
            foreach (string memberName in memberNames)
            {
                MemberInfo[] mInfos = type.GetMember(memberName);
                if (mInfos.Length != 1)
                {
                    Assert.Fail("Expected generated type " + type.Name + " to declare " + memberName);
                }
                Assert.AreEqual(type, mInfos[0].DeclaringType, "Member " + memberName + " is declared on " + mInfos[0].DeclaringType + " but we expected it to be declared on " + type);
            }
        }

        /// <summary>
        /// Asserts the given type exposes but does not declare the specified members
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberNames"></param>
        private void AssertHasButDoesNotDeclareMembers(Type type, params string[] memberNames)
        {
            foreach (string memberName in memberNames)
            {
                MemberInfo[] mInfos = type.GetMember(memberName);
                if (mInfos.Length != 1)
                {
                    Assert.Fail("Expected generated type " + type.Name + " to expose " + memberName);
                }
                Assert.AreNotEqual(type, mInfos[0].DeclaringType, "Member " + memberName + " was not expected to be declared by " + type);
            }
        }

        private void AssertDoesNotHaveMembers(Type type, params string[] memberNames)
        {
            foreach (string memberName in memberNames)
            {
                MemberInfo[] mInfos = type.GetMember(memberName);
                if (mInfos.Length != 0)
                {
                    Assert.Fail("Expected generated type " + type.Name + " *NOT* to expose " + memberName);
                }
            }
        }


        private void AssertValidCustomMethod(AssemblyGenerator asmGen, Type entityType, Type domainContextType, string methodName, Type[] parameters)
        {
            // ----------------------------------------
            // Validate the method on the entity
            // ----------------------------------------
            MethodInfo mInfo = entityType.GetMethod(methodName);
            Assert.IsNotNull(mInfo, "Expected to find method " + methodName + " on entity " + entityType.Name);
            Assert.IsTrue(mInfo.IsPublic, "Expected method " + methodName + " to be public");
            Assert.AreEqual(typeof(void).FullName, mInfo.ReturnType.FullName, "Expected " + methodName + " to return void");
            ParameterInfo[] pInfos = mInfo.GetParameters();
            Assert.AreEqual(parameters.Length, pInfos.Length, "Expected " + parameters.Length + " parameters for " + methodName);
            for (int i = 0; i < parameters.Length; ++i)
            {
                Assert.AreEqual(parameters[i], pInfos[i].ParameterType, "Expected parameter " + i + " for " + methodName + " to be of type " + parameters[i].Name);
            }

            // ------------------------------------------
            // Validate the IsXXXInvoked and CanXXXInvoke
            // ------------------------------------------
            string thePropertyName = "Is" + methodName + "Invoked";
            PropertyInfo pInfo = entityType.GetProperty(thePropertyName);
            Assert.IsNotNull(pInfo, "Expected to find " + thePropertyName + " on entity type " + entityType.Name);
            Assert.IsNotNull(pInfo.GetGetMethod(), "Expected getter on " + thePropertyName + " on entity type " + entityType.Name);
            Assert.IsTrue(pInfo.GetGetMethod().IsPublic);
            Assert.AreEqual(typeof(bool), pInfo.PropertyType);

            thePropertyName = "Can" + methodName;
            pInfo = entityType.GetProperty(thePropertyName);
            Assert.IsNotNull(pInfo, "Expected to find " + thePropertyName + " on entity type " + entityType.Name);
            Assert.IsNotNull(pInfo.GetGetMethod(), "Expected getter on " + thePropertyName + " on entity type " + entityType.Name);
            Assert.IsTrue(pInfo.GetGetMethod().IsPublic);
            Assert.AreEqual(typeof(bool), pInfo.PropertyType);

            // ---------------------------------------
            // Validate partial methods on the entity
            // ---------------------------------------

            // Validate partial OnXXXInvoking and OnXXXInvoked
            // TODO: [roncain] would like better test but requires more work to compose
            string onInvokedDecl = asmGen.IsCSharp
                                        ? "partial void On" + methodName + "Invoked("
                                        : "Private Partial Sub On" + methodName + "Invoked(";

            CodeGenHelper.AssertGenerated(asmGen.GeneratedCode, onInvokedDecl);

            string onInvokingDecl = asmGen.IsCSharp
                            ? "partial void On" + methodName + "Invoking("
                            : "Private Partial Sub On" + methodName + "Invoking(";

            CodeGenHelper.AssertGenerated(asmGen.GeneratedCode, onInvokingDecl);

            // ----------------------------------------
            // Validate the method on the DomainContext
            // ----------------------------------------
            mInfo = domainContextType.GetMethod(methodName);
            Assert.IsNotNull(mInfo, "Expected to find method " + methodName + " on context " + domainContextType.Name);
            Assert.IsTrue(mInfo.IsPublic, "Expected method " + methodName + " to be public");
            Assert.AreEqual(typeof(void), mInfo.ReturnType, "Expected " + methodName + " to return void");
            pInfos = mInfo.GetParameters();
            Assert.AreEqual(parameters.Length + 1, pInfos.Length, "Expected " + (parameters.Length + 1) + " parameters for " + methodName);
            Assert.AreEqual(entityType, pInfos[0].ParameterType, "Expected " + methodName + " on " + domainContextType.Name + " to have entity " + entityType + " as the first param");
            for (int i = 0; i < parameters.Length; ++i)
            {
                Assert.AreEqual(parameters[i], pInfos[i + 1].ParameterType, "Expected parameter " + i + " for " + methodName + " to be of type " + parameters[i].Name);
            }
        }
    }

}

// Avoid the System namespace or VB treats System as its root namespace
namespace Inheritance.Tests
{
    using OpenRiaServices.Tools.Test;

    #region Inherit_Basic

    // ----------------------------------------------------------------
    // Inherit_Basic
    //    Simple test of inheritance that exposes root and one derived entity
    //    This domain service is entirely legal, and the test validates it generates correct code
    public class Inherit_Basic_DomainService : GenericDomainService<Inherit_Basic_Root>
    {
        // public IQueryable<Inherit_Basic_Root> GetInherit_Basic_Root() is generated in our generic type
        public IEnumerable<Inherit_Basic_Derived> GetDerived() { return null; }
    }
    [KnownType(typeof(Inherit_Basic_Derived))]
    public partial class Inherit_Basic_Root
    {
        [Key]
        public string TheKey { get; set; }
        public string RootProperty { get; set; }
    }
    public partial class Inherit_Basic_Derived : Inherit_Basic_Root
    {
        public string DerivedProperty { get; set; }
    }
    #endregion // Inherit_Basic



    #region Inherit_Basic_CUD
    // ----------------------------------------------------------------
    // Inherit_Basic_CUD
    //    Same entity hierarchy as Inherit_Basic, but the domain service
    //    exposes CUD methods on both derived and root entities
    public class Inherit_Basic_CUD_DomainService : GenericDomainService<Inherit_Basic_Root>
    {
        public void InsertIt(Inherit_Basic_Root entity) { }
        public void UpdateIt(Inherit_Basic_Root entity) { }
        public void DeleteIt(Inherit_Basic_Root entity) { }
        [EntityAction]
        public void CustomIt(Inherit_Basic_Root entity, bool flag) { }

        public void InsertIt(Inherit_Basic_Derived entity) { }
        public void UpdateIt(Inherit_Basic_Derived entity) { }
        public void DeleteIt(Inherit_Basic_Derived entity) { }
        [EntityAction]
        public void CustomItDerived(Inherit_Basic_Derived entity, int value) { }
    }
    #endregion // Inherit_Basic_CUD



    #region Inherit_Buddy
    // ----------------------------------------------------------------
    // Inherit_Buddy
    //    Simple test of inheritance that exposes root and one derived entity
    //    using the buddy class mechanism to expose metadata, including [KnownType]
    //    This domain service is entirely legal, and the test validates it generates correct code
    //    Hierarchy looks like this:
    //      Invisible_Root -- exposes [Key] and [KnownType] for Derived1
    //      Root -- exposes [KnownType] for Derived2
    //      Derived1 : Root -- first subclass
    //      Derived2 : Derived1 -- second subclass
    //    All types use buddy classes to declare validation attributes
    public class Inherit_Buddy_DomainService : GenericDomainService<Inherit_Buddy_Root>
    {
        // public IQueryable<Inherit_Buddy_Root> GetInherit_Buddy_Root() is generated in our generic type
        public IEnumerable<Inherit_Buddy_Derived1> GetDerived1() { return null; }
        public IEnumerable<Inherit_Buddy_Derived2> GetDerived2() { return null; }
    }

    [KnownType(typeof(Inherit_Buddy_Root))]             // not needed, but may encounter visible root exposed
    [KnownType(typeof(Inherit_Buddy_Derived1))]         // exposes 1 of 2 derived types
    public partial class Inherit_Buddy_Invisible_Root
    {
        [Key]
        public string TheKey { get; set; }
    }

    [MetadataType(typeof(Inherit_Buddy_Root_Metadata))]
    public partial class Inherit_Buddy_Root : Inherit_Buddy_Invisible_Root
    {
        public string RootProperty { get; set; }
    }

    // KnownType appears on a partial type -- cannot be used in buddy due to
    // failure of KnownTypeAttribute to override Attribute.TypeId
    [KnownType(typeof(Inherit_Buddy_Derived2))]     // 2nd derived type comes from visible root
    public partial class Inherit_Buddy_Root
    {
    }

    [MetadataType(typeof(Inherit_Buddy_Derived1_Metadata))]
    public partial class Inherit_Buddy_Derived1 : Inherit_Buddy_Root
    {
        public string Derived1Property { get; set; }
    }

    [MetadataType(typeof(Inherit_Buddy_Derived2_Metadata))]
    public partial class Inherit_Buddy_Derived2 : Inherit_Buddy_Derived1
    {
        public string Derived2Property { get; set; }
    }

    public class Inherit_Buddy_Root_Metadata
    {
        [StringLength(10)]
        public string RootProperty;
    }

    public class Inherit_Buddy_Derived1_Metadata
    {
        [StringLength(11)]
        public string Derived1Property;
    }

    public class Inherit_Buddy_Derived2_Metadata
    {
        [StringLength(12)]
        public string Derived2Property;
    }

    #endregion // Inherit_Buddy


    #region Inherit_No_Key

    // ----------------------------------------------------------------
    // Inherit_No_Key
    //  The [Key] field is absent on the root entity type, causing code gen error
    //
    public class Inherit_No_Key_DomainService : GenericDomainService<Inherit_No_Key_Entity> { }

    public partial class Inherit_No_Key_Entity
    {
        public Inherit_No_Key_Entity() { }

        // [Key] explicitly absent to trigger failure
        public string TheKey { get; set; }
    }
    #endregion // Inherit_No_Key


    #region Inherit_No_Key

    // ----------------------------------------------------------------
    // Inherit_Derived_Key
    //  The [Key] field is present on a derived entity type, causing code gen error
    //
    public class Inherit_Derived_Key_DomainService : GenericDomainService<Inherit_Derived_Key_Root> { }

    [KnownType(typeof(Inherit_Derived_Key_Derived))]
    public partial class Inherit_Derived_Key_Root
    {
        public Inherit_Derived_Key_Root() { }

        [Key]
        public string TheKey { get; set; }
    }

    public partial class Inherit_Derived_Key_Derived : Inherit_Derived_Key_Root
    {
        public Inherit_Derived_Key_Derived() { }

        [Key]   // this is a no-no on derived types
        public string TheDerivedKey { get; set; }
    }
    #endregion // Inherit_Derived_Key

    #region Inherit_Missing_KnownType

    // ----------------------------------------------------------------
    // Inherit_Missing_KnownType
    //  A root type missing a [KnownType] for an exposed entity
    //
    public class Inherit_Missing_KnownType_DomainService : GenericDomainService<Inherit_Missing_KnownType_Root>
    {
        // This query exposes the derived type, but it lacks a [KnownType] on the root
        public IEnumerable<Inherit_Missing_KnownType_Derived> GetDerivedEntities() { return null; }
    }

    // [KnownType(typeof(Inherit_Missing_KnownType_Derived))] -- absence of this attribute causes error
    public partial class Inherit_Missing_KnownType_Root
    {
        public Inherit_Missing_KnownType_Root() { }

        [Key]
        public string TheKey { get; set; }
    }

    public partial class Inherit_Missing_KnownType_Derived : Inherit_Missing_KnownType_Root
    {
        public Inherit_Missing_KnownType_Derived() { }
        public string TheDerivedKey { get; set; }
    }
    #endregion // Inherit_Missing_KnownType

    #region Inherit_Polymorphic

    // ----------------------------------------------------------------
    // Inherit_Polymorphic
    //    Contains entities with polymorphic properties
    //
    public class Inherit_Polymorphic_DomainService : GenericDomainService<Inherit_Polymorphic_Entity> { }

    [KnownType(typeof(Inherit_Polymorphic_Derived))]    // here to cause derived entity to be generated
    public class Inherit_Polymorphic_Entity
    {
        public Inherit_Polymorphic_Entity() { }
        [Key]
        public string TheKey { get; set; }
        public virtual string VirtualProperty { get; set; }     // illegal
        public string BaseProperty { get; set; }                // legal here, but illegal below
    }
    // Injected this abstract class here because root cannot be abstract
    // Note this type is not exposed via [KnownType]
    public abstract class Inherit_Polymorphic_Abstract_Derived : Inherit_Polymorphic_Entity   // abstract to test abstract properties
    {
        public Inherit_Polymorphic_Abstract_Derived() { }
        public abstract string AbstractProperty { get; set; }
    }
    public class Inherit_Polymorphic_Derived : Inherit_Polymorphic_Abstract_Derived
    {
        public override string AbstractProperty { get; set; }
        public override string VirtualProperty { get; set; }
    }

    #endregion // Inherit_Polymorphic

    #region Inherit_Polymorphic_New

    // ----------------------------------------------------------------
    // Inherit_Polymorphic_New
    //    Contains entities with 'new' polymorphic properties to trigger error
    //
    public class Inherit_Polymorphic_New_DomainService : GenericDomainService<Inherit_Polymorphic_New_Entity> { }

    [KnownType(typeof(Inherit_Polymorphic_New_Derived))]    // here to cause derived entity to be generated
    public class Inherit_Polymorphic_New_Entity
    {
        public Inherit_Polymorphic_New_Entity() { }
        [Key]
        public string TheKey { get; set; }
        public string BaseProperty { get; set; }                // legal here, but illegal below
    }
    // We put one error condition on a non-exposed entity to see if we find it
    public class Inherit_Polymorphic_New_Derived_Not_Exposed : Inherit_Polymorphic_New_Entity
    {
        // Attempting to override with 'new' is an error
        public new string BaseProperty { get; set; }
    }

    // And another error on the exposed entity
    public class Inherit_Polymorphic_New_Derived : Inherit_Polymorphic_New_Derived_Not_Exposed
    {
    }

    #endregion // Inherit_Polymorphic_New


    #region Inherit_Flatten

    // ----------------------------------------------------------------
    // Inherit_Flatten types:
    //  Entity hierarchy is:
    //      HiddenBase -- base type that is not exposed and whose properties should not be exposed
    //      VisibleRoot -- derived from HiddenBase, least derived entity
    //      Hidden1 -- derived from VisibleRoot, omitted from known types
    //      Visible1 -- derived from Hidden1 and identified in known type
    //      Hidden2 -- derived from Visible1, omitted from known types
    //      Visible2 -- derived from Hidden2 and identified in known types
    //  The purpose of this inheritance is to verify:
    //      - Types and properties below the visible root do not get exposed
    //      - Gaps in hierarchy are allowed and do not generate the gaps
    //      - Properties from hidden entities are lifted to the next available visible entity
    //      - Properties are not lifted multiple times if there are multiple gaps
    //      - Even [Key] properties may be lifted from types below the root entity
    //
    public class Inherit_Flatten_DomainService : GenericDomainService<Inherit_Flatten_VisibleRoot> { }

    // HiddenBase: base type that is not exposed
    public partial class Inherit_Flatten_HiddenBase
    {
        public Inherit_Flatten_HiddenBase() { }

        // Notice te [Key] is on a type not exposed -- this is legal
        [Key]
        public string TheKey { get; set; }

        public string HiddenBaseProperty { get; set; }
    }

    [KnownType(typeof(Inherit_Flatten_Visible1))]
    [KnownType(typeof(Inherit_Flatten_Visible2))]
    public partial class Inherit_Flatten_VisibleRoot : Inherit_Flatten_HiddenBase
    {
        public Inherit_Flatten_VisibleRoot() { }
        public string VisibleRootProperty { get; set; }
    }

    public partial class Inherit_Flatten_Hidden1 : Inherit_Flatten_VisibleRoot
    {
        public string Hidden1Property { get; set; }
    }
    public partial class Inherit_Flatten_Visible1 : Inherit_Flatten_Hidden1
    {
        public string Visible1Property { get; set; }
    }
    public partial class Inherit_Flatten_Hidden2 : Inherit_Flatten_Visible1
    {
        public string Hidden2Property { get; set; }
    }
    public partial class Inherit_Flatten_Visible2 : Inherit_Flatten_Hidden2
    {
        public string Visible2Property { get; set; }
    }

    #endregion // Inherit_Flatten

    #region Inherit_No_Root_Insert
    // ----------------------------------------------------------------
    // Inherit_No_Root_Insert
    //    DomainService exposes insert operation on derived entity only -- invalid
    //
    public class Inherit_No_Root_Insert_DomainService : GenericDomainService<Inherit_Basic_Root>
    {
        public void InsertIt(Inherit_Basic_Derived entity) { }
    }
    #endregion // Inherit_No_Root_Insert

    #region Inherit_No_Root_Update
    // ----------------------------------------------------------------
    // Inherit_No_Root_Update
    //    DomainService exposes Update operation on derived entity only -- invalid
    //
    public class Inherit_No_Root_Update_DomainService : GenericDomainService<Inherit_Basic_Root>
    {
        public void UpdateIt(Inherit_Basic_Derived entity) { }
    }
    #endregion // Inherit_No_Root_Update

    #region Inherit_No_Root_Delete
    // ----------------------------------------------------------------
    // Inherit_No_Root_Delete
    //    DomainService exposes Delete operation on derived entity only -- invalid
    //
    public class Inherit_No_Root_Delete_DomainService : GenericDomainService<Inherit_Basic_Root>
    {
        public void DeleteIt(Inherit_Basic_Derived entity) { }
    }
    #endregion // Inherit_No_Root_Delete


    #region Inherit_No_Root_Custom
    // ----------------------------------------------------------------
    // Inherit_No_Root_Custom
    //    DomainService exposes a custom operation on derived entity only
    //
    public class Inherit_No_Root_Custom_DomainService : GenericDomainService<Inherit_Basic_Root>
    {
        [EntityAction]
        public void CustomIt(Inherit_Basic_Derived entity, string stringValue) { }
    }
    #endregion // Inherit_No_Root_Custom


    #region Inherit_Custom_Overload
    // ----------------------------------------------------------------
    // Inherit_Custom_Overload
    //    DomainService exposes a custom operation on derived entity only -- invalid
    //
    public class Inherit_Custom_Overload_DomainService : GenericDomainService<Inherit_Basic_Root>
    {
        [EntityAction]
        public void CustomIt(Inherit_Basic_Root entity, bool flag) { }
        [EntityAction]
        public void CustomIt(Inherit_Basic_Derived entity, bool flag) { }
    }
    #endregion // Inherit_Custom_Overload

    #region Inherit_Abstract_Root
    // ----------------------------------------------------------------
    // Inherit_Abstract_Root
    //    DomainService exposes a root entity that is abstract
    //    but is legal because it identifes a concrete subclass
    [KnownType(typeof(Inherit_Abstract_Derived))]
    public abstract class Inherit_Abstract_Root
    {
        public Inherit_Abstract_Root() { }
        [Key]
        public string TheKey { get; set; }
    }
    // Declares only its immediate deriveds to test inheritable knowntype attributes
    [KnownType(typeof(Inherit_Abstract_Derived_A))]
    [KnownType(typeof(Inherit_Abstract_Derived_B))]
    public abstract class Inherit_Abstract_Derived : Inherit_Abstract_Root
    {
    }
    [KnownType(typeof(Inherit_Abstract_Derived_C))]
    public abstract class Inherit_Abstract_Derived_A : Inherit_Abstract_Derived
    {
    }
    // B and C are the only concretes, and neither are visible from the root.
    // This tests our ability to compute the closure of known types
    public class Inherit_Abstract_Derived_B : Inherit_Abstract_Derived
    {
    }
    public class Inherit_Abstract_Derived_C : Inherit_Abstract_Derived_A
    {
    }
    public class Inherit_Abstract_Root_DomainService : GenericDomainService<Inherit_Abstract_Root>
    {
    }
    #endregion // Inherit_Abstract_Root

    #region Inherit_Assoc_Uni_Derived_To_Included_Root
    // ----------------------------------------------------------------
    // Inherit_Assoc_Uni_Derived_To_Included_Root
    //    Derived entity has uni-directional 1:1 to another
    //    root entity identified only via [Include]
    //
    [KnownType(typeof(Inherit_Assoc_UD2IR_Source_Derived))]
    public class Inherit_Assoc_UD2IR_Source_Root
    {
        public Inherit_Assoc_UD2IR_Source_Root() { }
        [Key]
        public string TheKey { get; set; }
    }

    public class Inherit_Assoc_UD2IR_Source_Derived : Inherit_Assoc_UD2IR_Source_Root
    {
        public Inherit_Assoc_UD2IR_Source_Derived() { }

        public string TargetId { get; set; }    // FK

        // --- Test aspect --- this derived type forms a unidirectional 1:1 assoc to another root
        [Association("Source_Target", "TargetId", "Id", IsForeignKey = true)]
        [Include]
        public Inherit_Assoc_UD2IR_Target_Root Target { get; set; }
    }

    public class Inherit_Assoc_UD2IR_Target_Root
    {
        public Inherit_Assoc_UD2IR_Target_Root() { }
        [Key]
        public string Id { get; set; }
    }

    //public class Inherit_Assoc_UD2IR_Target_Derived : Inherit_Assoc_Other_Root
    //{
    //    public Inherit_Assoc_UD2IR_Target_Derived() { }
    //}

    public class Inherit_Assoc_UD2IR_DomainService : GenericDomainService<Inherit_Assoc_UD2IR_Source_Root>
    {
    }
    #endregion // Inherit_Assoc_Uni_Derived_To_Included_Root


    #region Inherit_Assoc_Bi_Derived_To_Included_Root
    // ----------------------------------------------------------------
    // Inherit_Assoc_Bi_Derived_To_Included_Root
    //    Derived entity has bi-directional 1:1 to another
    //    root entity identified only via [Include].
    //  This test validates not only that a derived type can
    //  form an association, but that the inverse association
    //  can use a property defined on the entity's base.
    [KnownType(typeof(Inherit_Assoc_BD2IR_Source_Derived))]
    public class Inherit_Assoc_BD2IR_Source_Root
    {
        public Inherit_Assoc_BD2IR_Source_Root() { }
        [Key]
        public string TheKey { get; set; }
    }

    public class Inherit_Assoc_BD2IR_Source_Derived : Inherit_Assoc_BD2IR_Source_Root
    {
        public Inherit_Assoc_BD2IR_Source_Derived() { }

        public string TargetId { get; set; } // FK to Other

        // --- Test aspect --- this derived type forms a unidirectional 1:1 assoc to another root
        [Association("Source_Target", "TargetId", "Id", IsForeignKey = true)]
        [Include]
        public Inherit_Assoc_BD2IR_Target_Root Target { get; set; }
    }

    public class Inherit_Assoc_BD2IR_Target_Root
    {
        public Inherit_Assoc_BD2IR_Target_Root() { }
        [Key]
        public string Id { get; set; }    // PK

        public string SourceKey { get; set; }     // FK to Bi-Derived, using prop on derived's base entity (root)
        [Association("Source_Target", "SourceKey", "TheKey")]
        public Inherit_Assoc_BD2IR_Source_Derived Source { get; set; }
    }

    public class Inherit_Assoc_BD2IR_DomainService : GenericDomainService<Inherit_Assoc_BD2IR_Source_Root>
    {
    }
    #endregion // Inherit_Assoc_Bi_Derived_To_Included_Derived

    #region Inherit_Assoc_Bi_Derived_To_Included_Derived
    // ----------------------------------------------------------------
    // Inherit_Assoc_Bi_Derived_To_Included_Derived
    //    Derived entity has bi-directional 1:1 to another
    //    root entity identified only via [Include].
    //  This test validates not only that a derived type can
    //  form an association, but that the inverse association
    //  can use a property defined on the entity's base.

    // Make this even harder by not exposing the root containing the key
    public class Inherit_Assoc_BD2ID_Source_Hidden_Root
    {
        public Inherit_Assoc_BD2ID_Source_Hidden_Root() { }
        [Key]
        public string TheKey { get; set; }
    }

    [KnownType(typeof(Inherit_Assoc_BD2ID_Source_Derived))]
    public class Inherit_Assoc_BD2ID_Source_Root : Inherit_Assoc_BD2ID_Source_Hidden_Root
    {
        public Inherit_Assoc_BD2ID_Source_Root() { }
    }

    public class Inherit_Assoc_BD2ID_Source_Derived : Inherit_Assoc_BD2ID_Source_Root
    {
        public Inherit_Assoc_BD2ID_Source_Derived() { }

        public string OtherId { get; set; } // FK to Other

        // --- Test aspect --- this derived type forms a bidirectional 1:1 assoc to another derived type
        [Association("Derived_Other", "OtherId", "Id", IsForeignKey = true)]
        [Include]
        public Inherit_Assoc_BD2ID_Target_Derived Target { get; set; }
    }

    // This other root does not appear in an [Include] or [KnownType]
    public class Inherit_Assoc_BD2ID_Target_Root
    {
        public Inherit_Assoc_BD2ID_Target_Root() { }
        [Key]
        public string Id { get; set; }    // PK
    }

    public class Inherit_Assoc_BD2ID_Target_Derived : Inherit_Assoc_BD2ID_Target_Root
    {
        public Inherit_Assoc_BD2ID_Target_Derived() { }

        public string SourceKey { get; set; }     // FK to Bi-Source_Derived, using prop on derived's base entity (root)
        [Association("Other_Derived", "SourceKey", "TheKey", IsForeignKey = true)]
        public Inherit_Assoc_BD2ID_Source_Derived Source { get; set; }
    }

    public class Inherit_Assoc_BD2ID_DomainService : GenericDomainService<Inherit_Assoc_BD2ID_Source_Root>
    {
    }
    #endregion // Inherit_Assoc_Bi_Derived_To_Included_Derived


    #region Inherit_Assoc_Bi_Derived_To_Included_Derived_OneToMany
    // ----------------------------------------------------------------
    // Inherit_Assoc_Bi_Derived_To_Included_Derived_OneToMany
    //    Derived entity has bi-directional 1:M to another
    //    derived entity identified only via [Include].
    //  This test validates not only that a derived type can
    //  form an association, but that the inverse association
    //  can use a property defined on the entity's base.
    [KnownType(typeof(Inherit_Assoc_BD2ID_Source_Derived_OneToMany))]
    public class Inherit_Assoc_BD2ID_Source_Root_OneToMany
    {
        public Inherit_Assoc_BD2ID_Source_Root_OneToMany() { }
        [Key]
        public string TheKey { get; set; }
    }

    public class Inherit_Assoc_BD2ID_Source_Derived_OneToMany : Inherit_Assoc_BD2ID_Source_Root_OneToMany
    {
        public Inherit_Assoc_BD2ID_Source_Derived_OneToMany() { }

        public string OtherId { get; set; }

        // --- Test aspect --- this derived type forms a bidirectional 1:M assoc to another derived type
        [Association("Derived_Other", "OtherId", "Id")]
        [Include]
        public List<Inherit_Assoc_BD2ID_Target_Derived_OneToMany> Targets { get; set; }
    }

    // This other root does not appear in an [Include] or [KnownType]
    public class Inherit_Assoc_BD2ID_Target_Root_OneToMany
    {
        public Inherit_Assoc_BD2ID_Target_Root_OneToMany() { }
        [Key]
        public string Id { get; set; }
        [Key]
        public string Id2 { get; set; }   // 2 part PK to allow 1:M to incomplete key
    }

    public class Inherit_Assoc_BD2ID_Target_Derived_OneToMany : Inherit_Assoc_BD2ID_Target_Root_OneToMany
    {
        public Inherit_Assoc_BD2ID_Target_Derived_OneToMany() { }

        public string SourceKey { get; set; }     // FK to Bi-Source_Derived, using prop on derived's base entity (root)
        [Association("Derived_Other", "SourceKey", "TheKey", IsForeignKey = true)]
        public Inherit_Assoc_BD2ID_Source_Derived_OneToMany Source { get; set; }
    }

    public class Inherit_Assoc_BD2ID_OneToMany_DomainService : GenericDomainService<Inherit_Assoc_BD2ID_Source_Root_OneToMany>
    {
    }
    #endregion // Inherit_Assoc_Bi_Derived_To_Included_Derived_OneToMany


    #region Inherit_Assoc_Bi_Derived_To_Included_Derived_ManyToOne
    // ----------------------------------------------------------------
    // Inherit_Assoc_Bi_Derived_To_Included_Derived_ManyToOne
    //    Derived entity has bi-directional M:1 to another
    //    derived entity identified only via [Include].
    //  This test validates not only that a derived type can
    //  form an association, but that the inverse association
    //  can use a property defined on the entity's base.
    [KnownType(typeof(Inherit_Assoc_BD2ID_Source_Derived_ManyToOne))]
    public class Inherit_Assoc_BD2ID_Source_Root_ManyToOne
    {
        public Inherit_Assoc_BD2ID_Source_Root_ManyToOne() { }
        [Key]
        public string TheKey { get; set; }        // 2 part PK to be real M:1
        [Key]
        public string TheKey2 { get; set; }

    }

    public class Inherit_Assoc_BD2ID_Source_Derived_ManyToOne : Inherit_Assoc_BD2ID_Source_Root_ManyToOne
    {
        public Inherit_Assoc_BD2ID_Source_Derived_ManyToOne() { }

        public string OtherId { get; set; } // FK to Other

        // --- Test aspect --- this derived type forms a bidirectional 1:M assoc to another derived type
        [Association("Derived_Other", "OtherId", "Id", IsForeignKey = true)]
        [Include]
        public Inherit_Assoc_BD2ID_Target_Derived_ManyToOne Target { get; set; }
    }

    // This other root does not appear in an [Include] or [KnownType]
    public class Inherit_Assoc_BD2ID_Target_Root_ManyToOne
    {
        public Inherit_Assoc_BD2ID_Target_Root_ManyToOne() { }
        [Key]
        public string Id { get; set; }
    }

    public class Inherit_Assoc_BD2ID_Target_Derived_ManyToOne : Inherit_Assoc_BD2ID_Target_Root_ManyToOne
    {
        public Inherit_Assoc_BD2ID_Target_Derived_ManyToOne() { }

        public string SourceKey { get; set; }     // FK to Bi-Source_Derived, using prop on derived's base entity (root)
        [Association("Derived_Other", "SourceKey", "TheKey")]
        public List<Inherit_Assoc_BD2ID_Source_Derived_ManyToOne> Sources { get; set; }
    }

    public class Inherit_Assoc_BD2ID_ManyToOne_DomainService : GenericDomainService<Inherit_Assoc_BD2ID_Source_Root_ManyToOne>
    {
    }
    #endregion // Inherit_Assoc_Bi_Derived_To_Included_Derived_ManyToOne


    #region Inherit_Projection_Bi_Derived_To_Included_Derived
    // ----------------------------------------------------------------
    // Inherit_Projection_Bi_Derived_To_Included_Derived
    //    Derived entity has bi-directional 1:1 to another
    //    root entity identified only via [Include].  It then
    //    adds a "projection" [Include] to expose another field.
    //  This test validates not only that a derived type can
    //  form an association and denormalize another derived type's fields.

    // Make this even harder by not exposing the root containing the key
    public class Inherit_Projection_BD2ID_Source_Hidden_Root
    {
        public Inherit_Projection_BD2ID_Source_Hidden_Root() { }
        [Key]
        public string TheKey { get; set; }
    }

    [KnownType(typeof(Inherit_Projection_BD2ID_Source_Derived))]
    public class Inherit_Projection_BD2ID_Source_Root : Inherit_Projection_BD2ID_Source_Hidden_Root
    {
        public Inherit_Projection_BD2ID_Source_Root() { }
    }

    public class Inherit_Projection_BD2ID_Source_Derived : Inherit_Projection_BD2ID_Source_Root
    {
        public Inherit_Projection_BD2ID_Source_Derived() { }
        public string OtherId { get; set; }             // FK to Other

        // --- Test aspect --- this derived type forms a bidirectional 1:1 assoc to another derived type
        [Association("Derived_Other", "OtherId", "Id", IsForeignKey = true)]
        [Include]
        [Include("TargetRootProperty", "ProjectedTargetRootProp")]          // project a field from target's root
        [Include("TargetDerivedProperty", "ProjectedTargetDerivedProp")]    // and one from target's derived type
        public Inherit_Projection_BD2ID_Target_Derived Target { get; set; }
    }

    // This other root does not appear in an [Include] or [KnownType]
    public class Inherit_Projection_BD2ID_Target_Root
    {
        public Inherit_Projection_BD2ID_Target_Root() { }
        [Key]
        public string Id { get; set; }    // PK

        public string TargetRootProperty { get; set; }
    }

    public class Inherit_Projection_BD2ID_Target_Derived : Inherit_Projection_BD2ID_Target_Root
    {
        public Inherit_Projection_BD2ID_Target_Derived() { }
        public string TargetDerivedProperty { get; set; }

        public string SourceKey { get; set; }     // FK to Bi-Source_Derived, using prop on derived's base entity (root)
        [Association("Derived_Other", "SourceKey", "TheKey")]
        public Inherit_Projection_BD2ID_Source_Derived Source { get; set; }
    }

    public class Inherit_Projection_BD2ID_DomainService : GenericDomainService<Inherit_Projection_BD2ID_Source_Root>
    {
    }
    #endregion // Inherit_Projection_Bi_Derived_To_Included_Derived


    #region Inherit_Projection_Bi_Derived_To_Included_Derived_Reversed
    // ----------------------------------------------------------------
    // Inherit_Projection_Bi_Derived_To_Included_Derived_Reversed
    //    Derived entity has bi-directional 1:1 to another
    //    root entity identified only via [Include].  It then
    //    adds a "projection" [Include] to expose another field.
    //  This test validates not only that a derived type can
    //  form an association and denormalize another derived type's fields.

    // Make this even harder by not exposing the root containing the key
    public class Inherit_Projection_BD2ID_Reversed_Source_Hidden_Root
    {
        public Inherit_Projection_BD2ID_Reversed_Source_Hidden_Root() { }
        [Key]
        public string TheKey { get; set; }
    }

    [KnownType(typeof(Inherit_Projection_BD2ID_Reversed_Source_Derived))]
    public class Inherit_Projection_BD2ID_Reversed_Source_Root : Inherit_Projection_BD2ID_Reversed_Source_Hidden_Root
    {
        public Inherit_Projection_BD2ID_Reversed_Source_Root() { }
        public string SourceRootProperty { get; set; }   // we'll project this
    }

    public class Inherit_Projection_BD2ID_Reversed_Source_Derived : Inherit_Projection_BD2ID_Reversed_Source_Root
    {
        public Inherit_Projection_BD2ID_Reversed_Source_Derived() { }
        public string SourceDerivedProperty { get; set; }     // we'll project this
        public string OtherId { get; set; }             // FK to Other

        // --- Test aspect --- this derived type forms a bidirectional 1:1 assoc to another derived type
        [Association("Derived_Other", "OtherId", "Id", IsForeignKey = true)]
        [Include]
        public Inherit_Projection_BD2ID_Reversed_Target_Derived Target { get; set; }
    }

    // This other root does not appear in an [Include] or [KnownType]
    public class Inherit_Projection_BD2ID_Reversed_Target_Root
    {
        public Inherit_Projection_BD2ID_Reversed_Target_Root() { }
        [Key]
        public string Id { get; set; }    // PK

        public string TargetRootProperty { get; set; }
    }

    public class Inherit_Projection_BD2ID_Reversed_Target_Derived : Inherit_Projection_BD2ID_Reversed_Target_Root
    {
        public Inherit_Projection_BD2ID_Reversed_Target_Derived() { }
        public string TargetDerivedProperty { get; set; }

        public string SourceKey { get; set; }     // FK to Bi-Source_Derived, using prop on derived's base entity (root)
        [Include("SourceRootProperty", "ProjectedSourceRootProp")]          // project a field from source's root
        [Include("SourceDerivedProperty", "ProjectedSourceDerivedProp")]    // and one from sources's derived type
        [Association("Derived_Other", "SourceKey", "TheKey")]
        public Inherit_Projection_BD2ID_Reversed_Source_Derived Source { get; set; }
    }

    public class Inherit_Projection_BD2ID_Reversed_DomainService : GenericDomainService<Inherit_Projection_BD2ID_Reversed_Source_Root>
    {
    }
    #endregion // Inherit_Projection_Bi_Derived_To_Included_Derived_Reversed

    #region Inherit_DomainService_With_EnableClientAccess
    public class Inherit_DomainService_Entity
    {
        [Key]
        public int Key { get; set; }
    }

    [EnableClientAccess]
    public class Inherit_DomainService_Parent : DomainService
    {
        [Query]
        public IQueryable<Inherit_DomainService_Entity> GetEntity()
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class Inherit_DomainService_Child1 : Inherit_DomainService_Parent
    {
    }

    [EnableClientAccess]
    public class Inherit_DomainService_Child2 : Inherit_DomainService_Parent
    {
    }
    #endregion
}
