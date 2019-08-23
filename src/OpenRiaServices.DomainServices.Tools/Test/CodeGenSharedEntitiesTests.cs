using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenSharedEntitiesTests
    {
        // Tests the following
        // 1. An entity shared across 2 domain services just works.
        // 2. Entity type, property and enumerable exposed once from each domain service are code gen'ed.
        // 3. Entity type, property and enumerable exposed by both domain services are code gen'ed.
        // 4. Entity type, property and enumerable exposed by no domain services are not code gen'ed.
        [TestMethod]
        [Description("Two DomainServices with shared entities succeeds")]
        public void CodeGen_SharedEntity_EntityShapingByTwoDomainServices_Succeeds()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_EntityShaping_ExposeAandBDomainService),
                typeof(SharedScenarios_EntityShaping_ExposeBAndCDomainService),
            };

            // Generated property getter, classes, and enumerables for A, B and C  exists
            string[] codeContains = new string[]
            {
                "public sealed partial class SharedScenarios_LeafEntityA : Entity",
                "public sealed partial class SharedScenarios_LeafEntityB : Entity",
                "public sealed partial class SharedScenarios_LeafEntityC : Entity",
                "public SharedScenarios_LeafEntityA A",
                "public SharedScenarios_LeafEntityB B",
                "public SharedScenarios_LeafEntityC C",
                "public EntitySet<SharedScenarios_LeafEntityA> SharedScenarios_LeafEntityAs",
                "public EntitySet<SharedScenarios_LeafEntityB> SharedScenarios_LeafEntityBs",
                "public EntitySet<SharedScenarios_LeafEntityC> SharedScenarios_LeafEntityCs",
            };

            // D does not exist
            string[] codeNotContains = new string[]
            {
                "SharedScenarios_LeafEntityD",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, codeNotContains);
        }

        // Tests the following
        // 1. An entity shared across 3 domain services just works.
        // 2. Entity type, property and enumerable exposed once from each domain service are code gen'ed.
        // 3. Entity type, property and enumerable exposed by no domain services are not code gen'ed.
        [TestMethod]
        [Description("Three DomainServices with shared entities exposed succeeds")]
        public void CodeGen_SharedEntity_EntityShapingByThreeDomainServices_Succeeds()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_EntityShaping_ExposeADomainService),
                typeof(SharedScenarios_EntityShaping_ExposeBDomainService),
                typeof(SharedScenarios_EntityShaping_ExposeCDomainService),
            };

            // Generated property getter, classes, and enumerables for A, B and C  exists
            string[] codeContains = new string[]
            {
                "public sealed partial class SharedScenarios_LeafEntityA : Entity",
                "public sealed partial class SharedScenarios_LeafEntityB : Entity",
                "public sealed partial class SharedScenarios_LeafEntityC : Entity",
                "public SharedScenarios_LeafEntityA A",
                "public SharedScenarios_LeafEntityB B",
                "public SharedScenarios_LeafEntityC C",
                "public EntitySet<SharedScenarios_LeafEntityA> SharedScenarios_LeafEntityAs",
                "public EntitySet<SharedScenarios_LeafEntityB> SharedScenarios_LeafEntityBs",
                "public EntitySet<SharedScenarios_LeafEntityC> SharedScenarios_LeafEntityCs",
            };

            // D does not exist
            string[] codeNotContains = new string[]
            {
                "SharedScenarios_LeafEntityD",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, codeNotContains);
        }

        // Tests the following
        // 1. All include/exclude attributed properties are generated correctly on a shared entity.
        // 2. Regular shaping takes over when include/exclude does not specify.
        [TestMethod]
        [Description("Two DomainServices with shared entities shaped by [Include] and [Exclude] succeeds")]
        public void CodeGen_SharedEntity_EntityShapingWithIncludeAndExclude_Succeeds()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_IncludeExcludeShaping_ExposeBDomainService),
                typeof(SharedScenarios_IncludeExcludeShaping_ExposeBAndCDomainService),
            };

            // Generated classes for A, B, and C exist
            // Generated property getter for A and C exists
            // Generated enumerables for B and C exist
            string[] codeContains = new string[]
            {
                "public sealed partial class SharedScenarios_LeafEntityA : Entity",
                "public sealed partial class SharedScenarios_LeafEntityB : Entity",
                "public sealed partial class SharedScenarios_LeafEntityC : Entity",
                "public SharedScenarios_LeafEntityA A",
                "public SharedScenarios_LeafEntityC C",
                "public EntitySet<SharedScenarios_LeafEntityB> SharedScenarios_LeafEntityBs",
                "public EntitySet<SharedScenarios_LeafEntityC> SharedScenarios_LeafEntityCs",
            };

            // Generated property getter for B does not exist
            // Generated enumerable fo A does not exist
            // D does not exist
            string[] codeNotContains = new string[]
            {
                "public SharedScenarios_LeafEntityB B",
                "public EntitySet<SharedScenarios_LeafEntityA> SharedScenarios_LeafEntityAs",
                "SharedScenarios_LeafEntityD",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, codeNotContains);
        }

        // Tests the following
        // 1. An entity with composition shared across two domain services just works.
        // 2. Composed entity is generated correctly.
        [TestMethod]
        [Description("Two DomainServices exposing a shared entities and its [Composition] member succeeds")]
        public void CodeGen_SharedEntity_EntityShapingWithComposition_Succeeds()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_CompositionShaping_ExposeBDomainService1),
                typeof(SharedScenarios_CompositionShaping_ExposeBDomainService2),
            };

            // Generated class and property getter for B exists
            string[] codeContains = new string[]
            {
                "public sealed partial class SharedScenarios_LeafEntityB : Entity",
                "public SharedScenarios_LeafEntityB B",
            };

            // Generated enumerable for B does not exist
            // A does not exist
            string[] codeNotContains = new string[]
            {
                "public EntitySet<SharedScenarios_LeafEntityB> SharedScenarios_LeafEntityBs",
                "SharedScenarios_LeafEntityA",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, codeNotContains);
        }

        // Tests the following
        // 1. An entity shared via inheritance across two domain services just works.
        // 2. Code generation does not generate unspecified inheritance heirarchies.
        [TestMethod]
        [Description("Two DomainServices with shared entities via inheritance succeeds")]
        public void CodeGen_SharedEntity_EntityShapingWithEvenInheritance_Succeeds()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_InheritanceShaping_ExposeXDomainService),
                typeof(SharedScenarios_InheritanceShaping_ExposeX2DomainService),
            };
 
            // Generated classes and enumerables for X and Z exist
            // Z inherits from X directly
            string[] codeContains = new string[]
            {
                "public partial class SharedScenarios_InheritanceShaping_X : Entity",
                "public sealed partial class SharedScenarios_InheritanceShaping_Z : SharedScenarios_InheritanceShaping_X",
                "public EntitySet<SharedScenarios_InheritanceShaping_X> SharedScenarios_InheritanceShaping_Xes",
            };

            // Z's entity set does not exist
            // Y does not exist
            string[] codeNotContains = new string[]
            {
                "EntitySet<SharedScenarios_InheritanceShaping_Z>",
                "SharedScenarios_InheritanceShaping_Y",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, codeNotContains);
        }

        // Tests than an entity shared via uneven inheritance across two domain services will fail.
        [TestMethod]
        [Description("Two DomainServices with shared entities via inheritance succeeds")]
        public void CodeGen_SharedEntity_EntityShapingWithUnevenInheritance_Fails()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_InheritanceShaping_ExposeXDomainService),
                typeof(SharedScenarios_InheritanceShaping_ExposeZDomainService),
            };

            string[] errors = new string[]
            { 
                string.Format(Resource.EntityCodeGen_SharedEntityMustBeLeastDerived,
                    typeof(SharedScenarios_InheritanceShaping_X), typeof(SharedScenarios_InheritanceShaping_ExposeXDomainService),
                    typeof(SharedScenarios_InheritanceShaping_Z), typeof(SharedScenarios_InheritanceShaping_ExposeZDomainService),
                    typeof(SharedScenarios_InheritanceShaping_Z)),
            };

            TestHelper.GenerateCodeAssertFailure("C#", domainServices, errors);
        }

        // Tests the following
        // 1. 2 DomainServices expose 2 different named update methods on the same entity just works.
        // 2. Verify the entity contains both named update methods.
        [TestMethod]
        [Description("Two DomainServices share an entity and each expose a named update method succeeds")]
        public void CodeGen_SharedEntity_TwoNamedUpdateMethods_Succeeds()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_NamedUpdate_ExposeNamedUpdate1DomainService),
                typeof(SharedScenarios_NamedUpdate_ExposeNamedUpdate2DomainService),
            };

            // Named update methods, other methods and guard properties exist for both methods
            string[] codeContains = new string[]
            {
                "base.InvokeAction(\"ChangeA1\", intProp);",
                "base.InvokeAction(\"ChangeA2\", boolProp);",
                "OnChangeA1Invoking",
                "OnChangeA2Invoking",
                "OnChangeA1Invoked",
                "OnChangeA2Invoked",
                "IsChangeA1Invoked",
                "IsChangeA2Invoked",
                "CanChangeA1",
                "CanChangeA2",
            };

            CodeGenHelper.BaseSuccessTest(domainServices, codeContains, null);
        }

        // Tests the following
        // 1. 2 DomainServices expose 2 overloaded named update methods with different parameters.
        // 2. Verify this generates a build error.
        [TestMethod]
        [Description("Two DomainServices expose overloaded named update methods on the same entity fails")]
        public void CodeGen_SharedEntity_TwoNamedUpdateMethodOverloads_Fails()
        {
            Type[] domainServices = new Type[]
            {
                typeof(SharedScenarios_NamedUpdate_ExposeNamedUpdate1DomainService),
                typeof(SharedScenarios_NamedUpdate_ExposeNamedUpdate1OverloadDomainService),
            };

            string[] errors = new string[]
            { 
                string.Format(Resource.EntityCodeGen_DuplicateCustomMethodName,
                    "ChangeA1",
                    typeof(SharedScenarios_LeafEntityA),
                    typeof(SharedScenarios_NamedUpdate_ExposeNamedUpdate1DomainService),
                    typeof(SharedScenarios_NamedUpdate_ExposeNamedUpdate1OverloadDomainService)),
            };

            TestHelper.GenerateCodeAssertFailure("C#", domainServices, errors);
        }
    }

    #region Leaf entities
    public class SharedScenarios_LeafEntityA
    {
        [Key]
        public int IdA { get; set; }
    }

    public class SharedScenarios_LeafEntityB
    {
        [Key]
        public int IdB { get; set; }
    }

    public class SharedScenarios_LeafEntityC
    {
        [Key]
        public int IdC { get; set; }
    }

    public class SharedScenarios_LeafEntityD
    {
        [Key]
        public int IdD { get; set; }
    }
    #endregion

    #region Entity shaping
    public class SharedScenarios_EntityShaping_FourPropertyEntity
    {
        [Key]
        public int Id { get; set; }
        public int IdA { get; set; }
        public int IdB { get; set; }
        public int IdC { get; set; }
        public int IdD { get; set; }

        [Association("Top_A", "IdA", "IdA")]
        public SharedScenarios_LeafEntityA A { get; set; }
        [Association("Top_B", "IdB", "IdB")]
        public SharedScenarios_LeafEntityB B { get; set; }
        [Association("Top_C", "IdC", "IdC")]
        public SharedScenarios_LeafEntityC C { get; set; }
        [Association("Top_D", "IdD", "IdD")]
        public SharedScenarios_LeafEntityD D { get; set; }
    }

    // --- For basic shared entity test.
    // Top level entity exposes A, B, C, and D
    // DS1 exposes top level entity, A and B
    // DS2 exposes top level entity, B and C
    // Verify that A, B and C entities are code gen'ed correctly,
    // and top level entity has only 3 properties.

    [EnableClientAccess]
    public class SharedScenarios_EntityShaping_ExposeAandBDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_EntityShaping_FourPropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityA> GetA() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityB> GetB() { return null; }
    }

    [EnableClientAccess]
    public class SharedScenarios_EntityShaping_ExposeBAndCDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_EntityShaping_FourPropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityB> GetB() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityC> GetC() { return null; }
    }

    // --- For a slightly more complex shared entity test.
    // Top level entity exposes A, B, C, and D
    // DS1 exposes top level entity and A
    // DS2 exposes top level entity and B
    // DS3 exposes top level entity and C
    // Verify that A, B and C entities are code gen'ed correctly,
    // and top level entity has only 3 properties.

    [EnableClientAccess]
    public class SharedScenarios_EntityShaping_ExposeADomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_EntityShaping_FourPropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityA> GetA() { return null; }
    }

    [EnableClientAccess]
    public class SharedScenarios_EntityShaping_ExposeBDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_EntityShaping_FourPropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityB> GetB() { return null; }
    }

    [EnableClientAccess]
    public class SharedScenarios_EntityShaping_ExposeCDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_EntityShaping_FourPropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityC> GetC() { return null; }
    }

    // --- Ensure shared entities continue to work with [Include] and [Exclude] attributes
    // Top level entity exposes [Inclue]A, [Exclue]B, C, D
    // DS1 exposes top level entity, B and C
    // DS2 exposes top level entity and C
    // Verify that A, B and C entities are code gen'ed correctly,
    // and top level entity has only 2 properties.

    public class SharedScenarios_IncludeExcludeShaping_ThreePropertyEntity
    {
        [Key]
        public int Id { get; set; }
        public int IdA { get; set; }
        public int IdB { get; set; }
        public int IdC { get; set; }
        public int IdD { get; set; }

        [Association("Top_A", "IdA", "IdA")]
        [Include]
        public SharedScenarios_LeafEntityA A { get; set; }

        [Association("Top_B", "IdB", "IdB")]
        [Exclude]
        public SharedScenarios_LeafEntityB B { get; set; }

        [Association("Top_C", "IdC", "IdC")]
        public SharedScenarios_LeafEntityC C { get; set; }

        [Association("Top_D", "IdD", "IdD")]
        public SharedScenarios_LeafEntityD D { get; set; }
    }

    [EnableClientAccess]
    public class SharedScenarios_IncludeExcludeShaping_ExposeBAndCDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_IncludeExcludeShaping_ThreePropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityB> GetB() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityC> GetC() { return null; }
    }

    [EnableClientAccess]
    public class SharedScenarios_IncludeExcludeShaping_ExposeBDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_IncludeExcludeShaping_ThreePropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityB> GetB() { return null; }
    }

    // --- Ensure shared entities continue to work with [Composition]
    // Top level entity exposes A, [Composition]B
    // DS1 exposes top level entity and B
    // DS2 exposes top level entity and B
    // Verify that B entity is code gen'ed correctly,
    // and top level entity has only 1 property.

    public class SharedScenarios_CompositionShaping_TwoPropertyEntity
    {
        [Key]
        public int Id { get; set; }
        public int IdA { get; set; }
        public int IdB { get; set; }

        [Association("Top_A", "IdA", "IdA")]
        public SharedScenarios_LeafEntityA A { get; set; }

        [Association("Top_B", "IdB", "IdB")]
        [Composition]
        public SharedScenarios_LeafEntityB B { get; set; }
    }

    [EnableClientAccess]
    public class SharedScenarios_CompositionShaping_ExposeBDomainService1 : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_CompositionShaping_TwoPropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityB> GetB() { return null; }
    }

    [EnableClientAccess]
    public class SharedScenarios_CompositionShaping_ExposeBDomainService2 : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_CompositionShaping_TwoPropertyEntity> GetProperty() { return null; }

        [Query]
        public IEnumerable<SharedScenarios_LeafEntityB> GetB() { return null; }
    }

    // --- Ensure shared entities continue to work with inheritance
    // Z derives from Y derives from X
    // DS1 exposes X
    // DS2 exposes Z
    // Declare only Z a known type and verify that X derives from Z

    [KnownType(typeof(SharedScenarios_InheritanceShaping_Z))]
    public class SharedScenarios_InheritanceShaping_X
    {
        [Key]
        public int Id { get; set; }
    }

    public class SharedScenarios_InheritanceShaping_Y : SharedScenarios_InheritanceShaping_X
    {
    }

    public class SharedScenarios_InheritanceShaping_Z : SharedScenarios_InheritanceShaping_Y
    {
    }

    [EnableClientAccess]
    public class SharedScenarios_InheritanceShaping_ExposeXDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_InheritanceShaping_X> GetProperty() { return null; }
    }

    [EnableClientAccess]
    public class SharedScenarios_InheritanceShaping_ExposeX2DomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_InheritanceShaping_X> GetProperty() { return null; }
    }

    // --- Ensure shared entities continue to work with inheritance
    // Z derives from Y derives from X
    // DS1 exposes X
    // DS2 exposes Z
    // Declare only Z a known type and verify that X derives from Z

    [EnableClientAccess]
    public class SharedScenarios_InheritanceShaping_ExposeZDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_InheritanceShaping_Z> GetProperty() { return null; }
    }
    #endregion

    #region Named updates

    // --- Ensure shared entities expose named updates from multiple sources
    // DS1 exposes Named Update 1
    // DS2 exposes Named Update 2
    // Verify both named updates appear on the Entity

    [EnableClientAccess]
    public class SharedScenarios_NamedUpdate_ExposeNamedUpdate1DomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_LeafEntityA> GetProperty() { return null; }

        [Update(UsingCustomMethod = true)]
        public void ChangeA1(SharedScenarios_LeafEntityA a, int intProp)
        {
        }
    }

    [EnableClientAccess]
    public class SharedScenarios_NamedUpdate_ExposeNamedUpdate2DomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_LeafEntityA> GetProperty() { return null; }

        [Update(UsingCustomMethod = true)]
        public void ChangeA2(SharedScenarios_LeafEntityA a, bool boolProp)
        {
        }
    }

    // --- Ensure named updates with conflicting names cause a build error
    // DS1 exposes Named Update (int)
    // DS2 exposes Named Update (int, bool)
    // Verify error occurs

    [EnableClientAccess]
    public class SharedScenarios_NamedUpdate_ExposeNamedUpdate1OverloadDomainService : DomainService
    {
        [Query]
        public IEnumerable<SharedScenarios_LeafEntityA> GetProperty() { return null; }

        [Update(UsingCustomMethod = true)]
        public void ChangeA1(SharedScenarios_LeafEntityA a, int intProp, bool boolProp)
        {
        }
    }
    #endregion
}
