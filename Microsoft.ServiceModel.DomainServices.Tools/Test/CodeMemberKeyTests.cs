using System;
using System.Collections.Generic;
using System.ServiceModel.DomainServices.Server;
using System.ServiceModel.DomainServices.Server.Test.Utilities;
using System.ServiceModel.DomainServices.Client.Test;
using System.Reflection;
using Microsoft.ServiceModel.DomainServices.Tools.SharedTypes;
using Microsoft.ServiceModel.DomainServices.Tools.SourceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests for CodeMemberKey
    /// </summary>
    [TestClass]
    public class CodeMemberKeyTests
    {
        public CodeMemberKeyTests()
        {
        }

        [Description("CodeMemberKey.CreateTypeKey creates a valid key")]
        [TestMethod]
        public void CodeMemberKey_Type_Key()
        {
            // Form the name based key
            CodeMemberKey key = CodeMemberKey.CreateTypeKey(typeof(TestEntity).AssemblyQualifiedName);
            Assert.IsNotNull(key, "CreateTypeKey using name failed");
            Assert.AreEqual(key.TypeName, typeof(TestEntity).AssemblyQualifiedName, "TypeName property get different than ctor");
            Type t = key.Type;
            Assert.AreEqual(typeof(TestEntity), t, "CodeMemberKey.Type failed to load type from name");

            // Form the type-based key
            CodeMemberKey key2 = CodeMemberKey.CreateTypeKey(typeof(TestEntity));
            Assert.IsNotNull(key2, "CreateTypeKey using type failed");
            Assert.AreEqual(key2.TypeName, typeof(TestEntity).AssemblyQualifiedName, "TypeName property get different than ctor");
            t = key2.Type;
            Assert.AreEqual(typeof(TestEntity), t, "CodeMemberKey.Type failed to load type from name");

            // These keys should be the same
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode(), "CodeMemberKeys from type should have yielded same hash");
            Assert.AreEqual(key, key2);
        }

        [Description("CodeMemberKey.CreatePropertyKey creates a valid key")]
        [TestMethod]
        public void CodeMemberKey_Property_Key()
        {
            // Form the name based key
            CodeMemberKey key = CodeMemberKey.CreatePropertyKey(typeof(TestEntity).AssemblyQualifiedName, "TheValue");
            Assert.IsNotNull(key, "CreateTypeKey using name failed");
            Assert.AreEqual(key.TypeName, typeof(TestEntity).AssemblyQualifiedName, "TypeName property get different than ctor");
            Type t = key.Type;
            Assert.AreEqual(typeof(TestEntity), t, "CodeMemberKey.Type failed to load type from name");
            PropertyInfo propInfo = key.PropertyInfo;
            Assert.IsNotNull(propInfo, "CodeMemberKey.PropertyInfo failed to get real property info.");
            Assert.AreEqual("TheValue", propInfo.Name, "incorrect property info name");

            // Form the type-based key
            CodeMemberKey key2 = CodeMemberKey.CreatePropertyKey(typeof(TestEntity).GetProperty("TheValue"));
            Assert.IsNotNull(key2, "CreateTypeKey using property failed");
            Assert.AreEqual(key2.TypeName, typeof(TestEntity).AssemblyQualifiedName, "TypeName property get different than ctor");
            t = key2.Type;
            Assert.AreEqual(typeof(TestEntity), t, "CodeMemberKey.Type failed to load type from name");
            propInfo = key2.PropertyInfo;
            Assert.IsNotNull(propInfo, "CodeMemberKey.PropertyInfo failed to get real property info.");
            Assert.AreEqual("TheValue", propInfo.Name, "incorrect property info name");

            // These keys should be the same
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode(), "CodeMemberKeys from property should have yielded same hash");
            Assert.AreEqual(key, key2);
        }

        [Description("CodeMemberKey.CreateMethodKey creates a valid key")]
        [TestMethod]
        public void CodeMemberKey_Method_Key()
        {
            // Form the name based key
            CodeMemberKey key = CodeMemberKey.CreateMethodKey(typeof(object).AssemblyQualifiedName, "Equals", new [] { typeof(object).AssemblyQualifiedName });
            Assert.IsNotNull(key, "CreateTypeKey using name failed");
            Assert.AreEqual(key.TypeName, typeof(object).AssemblyQualifiedName, "TypeName property get different than ctor");
            Type t = key.Type;
            Assert.AreEqual(typeof(object), t, "CodeMemberKey.Type failed to load type from name");
            MethodBase methodBase = key.MethodBase;
            Assert.IsNotNull(methodBase, "CodeMemberKey.MethodBase failed to get real method info.");
            Assert.AreEqual("Equals", methodBase.Name, "incorrect method info name");

            // Form the type-based key
            CodeMemberKey key2 = CodeMemberKey.CreateMethodKey(typeof(object).GetMethod("Equals", new[] { typeof(object) }));
            Assert.IsNotNull(key2, "CreateTypeKey using property failed");
            Assert.AreEqual(key2.TypeName, typeof(object).AssemblyQualifiedName, "TypeName property get different than ctor");
            t = key2.Type;
            Assert.AreEqual(typeof(object), t, "CodeMemberKey.Type failed to load type from name");
            methodBase = key2.MethodBase;
            Assert.IsNotNull(methodBase, "CodeMemberKey.MethodBase failed to get real method info.");
            Assert.AreEqual("Equals", methodBase.Name, "incorrect method info name");

            // These keys should be the same
            Assert.AreEqual(key.GetHashCode(), key2.GetHashCode(), "CodeMemberKeys from property should have yielded same hash");
            Assert.AreEqual(key, key2);

            // Ensure null is acceptable for parameter types
            CodeMemberKey key3 = CodeMemberKey.CreateMethodKey(typeof(object).AssemblyQualifiedName, "GetHashCode", null);
            Assert.IsNotNull(key3, "CreateTypeKey using name failed");
            Assert.AreEqual(key3.TypeName, typeof(object).AssemblyQualifiedName, "TypeName property get different than ctor");
            t = key3.Type;
            Assert.AreEqual(typeof(object), t, "CodeMemberKey.Type failed to load type from name");
            methodBase = key3.MethodBase;
            Assert.IsNotNull(methodBase, "CodeMemberKey.MethodBase failed to get real method info.");
            Assert.AreEqual("GetHashCode", methodBase.Name, "incorrect method info name");
        }

        [Description("CodeMemberKey.CreateMethodKey with invalid parameter types fails")]
        [TestMethod]
        public void CodeMemberKey_Method_Key_Bad_ParameterTypes()
        {
            // Right number of parameters, wrong type
            CodeMemberKey key = CodeMemberKey.CreateMethodKey(typeof(string).AssemblyQualifiedName, "Contains", new[] { typeof(int).AssemblyQualifiedName });
            Assert.IsNotNull(key, "CreateTypeKey using name failed");
            Assert.AreEqual(key.TypeName, typeof(string).AssemblyQualifiedName, "TypeName property get different than ctor");
            Type t = key.Type;
            Assert.AreEqual(typeof(string), t, "CodeMemberKey.Type failed to load type from name");
            MethodBase methodBase = key.MethodBase;
            Assert.IsNull(methodBase, "CodeMemberKey.MethodBase should have failed to resolve.");

            // Wrong number of parameters
            key = CodeMemberKey.CreateMethodKey(typeof(string).AssemblyQualifiedName, "Contains", new[] { typeof(string).AssemblyQualifiedName, typeof(string).AssemblyQualifiedName });
            Assert.IsNotNull(key, "CreateTypeKey using name failed");
            Assert.AreEqual(key.TypeName, typeof(string).AssemblyQualifiedName, "TypeName property get different than ctor");
            t = key.Type;
            Assert.AreEqual(typeof(string), t, "CodeMemberKey.Type failed to load type from name");
            methodBase = key.MethodBase;
            Assert.IsNull(methodBase, "CodeMemberKey.MethodBase should have failed to resolve.");

            // Bogus parameter type name
            key = CodeMemberKey.CreateMethodKey(typeof(string).AssemblyQualifiedName, "Contains", new[] { "NotAType" });
            Assert.IsNotNull(key, "CreateTypeKey using name failed");
            Assert.AreEqual(key.TypeName, typeof(string).AssemblyQualifiedName, "TypeName property get different than ctor");
            t = key.Type;
            Assert.AreEqual(typeof(string), t, "CodeMemberKey.Type failed to load type from name");
            methodBase = key.MethodBase;
            Assert.IsNull(methodBase, "CodeMemberKey.MethodBase should have failed to resolve.");
        }

    }
}
