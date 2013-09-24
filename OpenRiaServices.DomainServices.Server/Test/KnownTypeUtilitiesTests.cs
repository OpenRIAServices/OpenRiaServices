using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using KnownTypeUtil = OpenRiaServices.DomainServices.Server.KnownTypeUtilities;

namespace OpenRiaServices.DomainServices.Server.Test
{
    /// <summary>
    /// KnownTypeUtilities tests
    /// </summary>
    [TestClass]
    public class KnownTypeUtilitiesTests
    {
        [TestMethod]
        [Description("KnownTypeUtilities can inherit or not inherit base type's known types")]
        public void KnownType_Utilities_Import_Inherit()
        {
            // ----------------
            // Inherit = false
            // ----------------
            IEnumerable<Type> knownTypes = KnownTypeUtil.ImportKnownTypes(typeof(KTU_Simple), false /* inherit */);
            Assert.IsNotNull(knownTypes);
            Assert.AreEqual(2, knownTypes.Count());
            Assert.IsFalse(knownTypes.Contains(typeof(KTU_00)));    // declared on base-most type -- should not be seen
            Assert.IsFalse(knownTypes.Contains(typeof(KTU_0)));     // declared on base type -- should not be seen
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_1)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_2)));

            // ----------------
            // Inherit = true
            // ----------------
            knownTypes = KnownTypeUtil.ImportKnownTypes(typeof(KTU_Simple), true /* inherit */);
            Assert.IsNotNull(knownTypes);
            Assert.AreEqual(4, knownTypes.Count());
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_00)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_0)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_1)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_2)));
        }

        [TestMethod]
        [Description("KnownTypeUtilities can read simple form of [KnownType] that use the Type argument")]
        public void KnownType_Utilities_Import_Type_Arg()
        {
            // Absence of [KnownTypes] returns empty list
            IEnumerable<Type> knownTypes = KnownTypeUtil.ImportKnownTypes(typeof(KTU_1), false /* inherit */);
            Assert.IsNotNull(knownTypes);
            Assert.AreEqual(0, knownTypes.Count());

            // Presence of [KnownType] using Type member works
            knownTypes = KnownTypeUtil.ImportKnownTypes(typeof(KTU_Simple), false);
            Assert.IsNotNull(knownTypes);
            Assert.AreEqual(2, knownTypes.Count());
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_1)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_2)));
        }

        [TestMethod]
        [Description("KnownTypeUtilities can read [KnownType] that specify MethodName")]
        public void KnownType_Utilities_Import_MethodName_Arg()
        {
            // Presence of [KnownType] using Type[] returning method works
            IEnumerable<Type> knownTypes = KnownTypeUtil.ImportKnownTypes(typeof(KTU_Type_With_Array_Method), false /* inherit */);
            Assert.IsNotNull(knownTypes);
            Assert.AreEqual(2, knownTypes.Count());
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_2)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_3)));

            // Presence of [KnownType] using IEnumerable<Type> returning method works
            knownTypes = KnownTypeUtil.ImportKnownTypes(typeof(KTU_Type_With_Enumerable_Method), false /* inherit */);
            Assert.IsNotNull(knownTypes);
            Assert.AreEqual(2, knownTypes.Count());
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_3)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_4)));

            // Presence of [KnownType] using all known forms works
            knownTypes = KnownTypeUtil.ImportKnownTypes(typeof(KTU_Type_With_Enumerable_All_Forms), false /* inherit */);
            Assert.IsNotNull(knownTypes);
            Assert.AreEqual(4, knownTypes.Count());
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_1)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_2)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_3)));
            Assert.IsTrue(knownTypes.Contains(typeof(KTU_4)));
        }
    }

    public class KTU_00 { }
    public class KTU_0 { }
    public class KTU_1 { }
    public class KTU_2 { }
    public class KTU_3 { }
    public class KTU_4 { }

    [KnownType(typeof(KTU_00))]     // exposed only on least derived type
    public class KTU_Base00 { }

    [KnownType(typeof(KTU_0))]      // exposed on derived type but not least derived
    public class KTU_Base0 : KTU_Base00 { }

    [KnownType(typeof(KTU_1))]      // exposed on our visible base type
    [KnownType(typeof(KTU_2))]
    [KnownType(typeof(KTU_2))]  // known redundant but should be ignored
    public class KTU_Simple : KTU_Base0 { }

    // This type has a static method that returns them via a Type[]
    [KnownType("ArrayMethod")]
    public class KTU_Type_With_Array_Method {
        public static Type[] ArrayMethod()
        {
            return new Type[] { typeof(KTU_2), typeof(KTU_2), typeof(KTU_3) };  // redundancy again
        }
    }

    // This type has a static method that returns them as an IEnumerable<T>
    [KnownType("EnumerableMethod")]
    public class KTU_Type_With_Enumerable_Method
    {
        public static IEnumerable<Type> EnumerableMethod()
        {
            Type[] array = new Type[] { typeof(KTU_3), typeof(KTU_3), typeof(KTU_4) };  // redundancy again
            return new List<Type>(array);
        }
    }

    // This type uses every supported form of [KnownType] and allowable method signatures
    [KnownType("ArrayMethod")]
    [KnownType("EnumerableMethod")]
    [KnownType("NullReturning")]            // will return a null set of known types when called
    [KnownType("ThrowingVoidReturn")]       // should not be called
    [KnownType("ThrowingInstanceMethod")]   // should not be called
    [KnownType(typeof(KTU_1))]
    [KnownType(typeof(KTU_2))]
    public class KTU_Type_With_Enumerable_All_Forms
    {
        public static Type[] ArrayMethod()
        {
            return new Type[] { typeof(KTU_2), typeof(KTU_2), typeof(KTU_3) };
        }
        
        public static IEnumerable<Type> EnumerableMethod()
        {
            Type[] array = new Type[] { typeof(KTU_3), typeof(KTU_3), typeof(KTU_4) };
            return new List<Type>(array);
        }

        public static Type[] NullReturning()
        {
            return null;
        }

        public static void ThrowingVoidReturn()
        {
            Assert.Fail("Should not have called void returning method");
        }

        public Type[] ThrowingInstanceMethod()
        {
            Assert.Fail("Should not have called instance method");
            return null;
        }
    }
}