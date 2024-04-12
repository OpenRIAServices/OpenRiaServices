using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using OpenRiaServices.Tools.TextTemplate.Test;
using System.Runtime.CompilerServices;

namespace OpenRiaServices.Tools.Test
{
    public class TestHelper
    {
         // Creates a mock code generation host that uses the given logger and shared type service
        internal static MockCodeGenerationHost CreateMockCodeGenerationHost(ILoggingService logger, ISharedCodeService sharedCodeService)
        {
            MockCodeGenerationHost host = new MockCodeGenerationHost(logger, sharedCodeService);
            return host;
        }

        internal static void GenerateAndVerifyCodeGenerators(Type[] domainServiceTypes, Type[] sharedTypes, string[] refAssemblies)
        {
            using AssemblyGenerator asmGen = new AssemblyGenerator(/* isCSharp */ true, /* useFullTypeNames */ false, domainServiceTypes);
            foreach (Type t in sharedTypes)
            {
                asmGen.MockSharedCodeService.AddSharedType(t);
            }

            foreach (string refAssembly in refAssemblies)
            {
                asmGen.ReferenceAssemblies.Add(refAssembly);
            }

            string generatedCode = asmGen.GeneratedCode;
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Failed to generate code:\r\n" + asmGen.ConsoleLogger.Errors);

            Assembly codeDomCodeGenAssembly = asmGen.GeneratedAssembly;
            Assert.IsNotNull(codeDomCodeGenAssembly, "Assembly failed to build: " + asmGen.ConsoleLogger.Errors);

            using T4AssemblyGenerator t4AsmGen = new T4AssemblyGenerator(/* isCSharp */ true, /* useFullTypeNames */ false, domainServiceTypes);
            // Use same MetadataLoadContext for both assemblies so that types can be compared via normal equality
            t4AsmGen.MetadataLoadContext = asmGen.MetadataLoadContext;
            foreach (Type t in sharedTypes)
            {
                t4AsmGen.MockSharedCodeService.AddSharedType(t);
            }
            foreach (string refAssembly in refAssemblies)
            {
                t4AsmGen.ReferenceAssemblies.Add(refAssembly);
            }

            string t4GeneratedCode = t4AsmGen.GeneratedCode;
            Assert.IsFalse(string.IsNullOrEmpty(t4GeneratedCode), "Failed to generate code:\r\n" + t4AsmGen.ConsoleLogger.Errors);

            Assembly t4CodeGenAssembly = t4AsmGen.GeneratedAssembly;
            Assert.IsNotNull(t4CodeGenAssembly, "Assembly failed to build: " + t4AsmGen.ConsoleLogger.Errors);

            TestHelper.VerifyAssembliesEquality(codeDomCodeGenAssembly, t4CodeGenAssembly);
        }

        internal static void VerifyAssembliesEquality(Assembly a, Assembly b)
        {
            TestHelper.VerifyTypeEquality(a.GetTypes(), b.GetTypes());
        }

        private static void VerifyTypeEquality(Type[] t1, Type[] t2)
        {
            if (TestHelper.AreNotNull(t1, t2))
            {
                Assert.AreEqual(t1.Length, t2.Length);
                foreach (Type type1 in t1)
                {
                    Type type2 = t2.First(e => e.FullName == type1.FullName);

                    TestHelper.VerifyAttributesEquality(type1.GetCustomAttributesData(), type2.GetCustomAttributesData());
                    TestHelper.VerifyPropertiesEquality(type1.GetProperties(), type2.GetProperties());
                    TestHelper.VerifyMethodsEquality(type1.GetMethods(), type2.GetMethods());
                    TestHelper.VerifyTypeEquality(type1.GetNestedTypes(), type2.GetNestedTypes());
                    TestHelper.VerifyFieldsEquality(type1.GetFields(), type2.GetFields());
                }
            }
        }

        private static void VerifyFieldsEquality(FieldInfo[] fieldInfo1, FieldInfo[] fieldInfo2)
        {
            if (TestHelper.AreNotNull(fieldInfo1, fieldInfo2))
            {
                Assert.AreEqual(fieldInfo1.Length, fieldInfo2.Length);
                foreach (FieldInfo field1 in fieldInfo1)
                {
                    FieldInfo field2 = fieldInfo2.First(f => f.Name == field1.Name);
                    Assert.IsTrue(AreTypesEqual(field2.FieldType, field1.FieldType));
                }
            }
        }

        private static void VerifyAttributesEquality(IList<CustomAttributeData> attributes1, IList<CustomAttributeData> attributes2)
        {
            if (TestHelper.AreNotNull(attributes1, attributes2))
            {
                Assert.AreEqual(attributes1.Count, attributes2.Count);

                foreach (CustomAttributeData attr1 in attributes1)
                {
                    CustomAttributeData attr2 = attributes2.First(a => TestHelper.AreTypesEqual(a.AttributeType, attr1.AttributeType));
                    Assert.IsNotNull(attr2, $"Could not find an attribute matching '{attr1}' in t4 codegen generated assembly");

                    Assert.AreEqual(attr1.ConstructorArguments.Count, attr2.ConstructorArguments.Count);
                    Assert.AreEqual(attr1.NamedArguments.Count, attr2.NamedArguments.Count);
                }
            }
        }

        private static void VerifyPropertiesEquality(PropertyInfo[] properties1, PropertyInfo[] properties2)
        {
            if (TestHelper.AreNotNull(properties1, properties2))
            {
                Assert.AreEqual(properties1.Length, properties2.Length);
                foreach (PropertyInfo prop1 in properties1)
                {
                    PropertyInfo prop2 = properties2.First(p => p.Name == prop1.Name);
                    Assert.IsNotNull(prop2, $"Could not find a property matching '{prop1.Name}' in t4 codegen generated assembly");
                    Assert.IsTrue(AreTypesEqual(prop1.PropertyType, prop2.PropertyType), "Property was of a different type");
                    TestHelper.VerifyAttributesEquality(prop1.GetCustomAttributesData(), prop2.GetCustomAttributesData());
                }
            }
        }

        private static void VerifyMethodsEquality(MethodInfo[] methods1, MethodInfo[] methods2)
        {
            if (TestHelper.AreNotNull(methods1, methods2))
            {
                Assert.AreEqual(methods1.Length, methods2.Length);
                foreach (MethodInfo method1 in methods1)
                {
                    MethodInfo method2 = GetMatchingMethod(methods2, method1);
                    Assert.IsNotNull(method2, $"Could not find a method matching '{method1}' in t4 codegen generated assembly");
                    TestHelper.VerifyAttributesEquality(method1.GetCustomAttributesData(), method2.GetCustomAttributesData());
                }
            }
        }

        private static MethodInfo GetMatchingMethod(MethodInfo[] methods2, MethodInfo method1)
        {
            ParameterInfo[] method1Params = method1.GetParameters();
            foreach (MethodInfo m in methods2.Where(m => m.Name == method1.Name))
            {
                if (!AreTypesEqual(m.ReturnType, method1.ReturnType))
                {
                    continue;
                }
                ParameterInfo[] mParams = m.GetParameters();
                if (mParams.Length == method1Params.Length)
                {
                    bool found = true;
                    for (int i = 0; i < mParams.Length; i++)
                    {
                        if (!AreTypesEqual(method1Params[i].ParameterType, mParams[i].ParameterType))
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found)
                    {
                        return m;
                    }
                }
            }
            return null;
        }

        private static bool AreTypesEqual(Type t1, Type t2)
        {
            // Now check if generic types (or Array)
            if ((t1.IsGenericType && t2.IsGenericType)
                || (t1.IsArray && t2.IsArray))
            {
                // Compare name by name and namespace since "FullName" can contain assemblyqualified type name 
                if (t1.Name != t2.Name || t1.Namespace != t2.Namespace)
                    return false;

                Type[] t1Arguments = t1.GetGenericArguments();
                Type[] t2Arguments = t2.GetGenericArguments();

                if (t1Arguments.Length != t2Arguments.Length)
                    return false;

                for (int i = 0; i < t1Arguments.Length; i++)
                {
                    if (!AreTypesEqual(t1Arguments[i], t2Arguments[i]))
                        return false;
                }

                // All arguments are equal
                return true;
            }
            else // Name is same, and not a generic type or array
            {
                return t1.FullName == t2.FullName;
            }
        }

        private static bool AreNotNull(object[] o1, object[] o2)
        {
            if (o1 == null || o2 == null)
            {
                Assert.IsNull(o1);
                Assert.IsNull(o2);
                return false;
            }
            return true;
        }

        private static bool AreNotNull(object o1, object o2)
        {
            if (o1 == null || o2 == null)
            {
                Assert.IsNull(o1);
                Assert.IsNull(o2);
                return false;
            }
            return true;
        }
    }
}
