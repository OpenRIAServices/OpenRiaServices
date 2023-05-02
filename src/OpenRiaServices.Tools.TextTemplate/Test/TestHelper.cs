using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using OpenRiaServices.Tools.TextTemplate.Test;

namespace OpenRiaServices.Tools.Test
{
    public class TestHelper
    {
        // Returns the folder under which this test is currently running.
        public static string TestDir
        {
            get
            {
                return Path.GetDirectoryName(typeof(TestHelper).Assembly.Location);
            }
        }

        // Returns the path of the currently executing project as well as the output path
        // This unusual logic is necessary because MSTest has deployed this test to somewhere
        // from which we cannot locate our test project.  We use this technique to leave
        // breadcrumbs we can use to get back to it/
        public static void GetProjectPaths(string relativeTestDir, out string projectPath, out string outputPath)
        {
            string projectPathRelativeFileName = Path.Combine(relativeTestDir, "ProjectPath.txt");
            string projectPathFile = GetTestFileName(projectPathRelativeFileName);
            projectPath = string.Empty;
            outputPath = string.Empty;
            string inputString = string.Empty;
            using (StreamReader t1 = new StreamReader(projectPathFile))
            {
                inputString = t1.ReadToEnd();
            }

            string[] split = inputString.Split(',');
            projectPath = split[0];
            outputPath = split[1];
        }

        // Returns the full path to a deployment item file
        public static string GetTestFileName(string baseName)
        {
            string testFileName = Path.Combine(TestDir, baseName);

            Assert.IsTrue(File.Exists(testFileName), "Cannot locate " + testFileName +
                " which is a necessary input file to this test.\r\n" +
                " Be sure to add a [DeploymentItem] attribute for every new test file.");

            return testFileName;
        }

        // Creates a mock code generation host that uses the given logger and shared type service
        internal static MockCodeGenerationHost CreateMockCodeGenerationHost(ILoggingService logger, ISharedCodeService sharedCodeService)
        {
            MockCodeGenerationHost host = new MockCodeGenerationHost(logger, sharedCodeService);
            return host;
        }

        internal static void GenerateAndVerifyCodeGenerators(Type[] domainServiceTypes, Type[] sharedTypes, string[] refAssemblies)
        {
            Type[] codeDomCodeGenAssemblyTypes = null;
            Type[] t4CodeGenAssemblyTypes = null;
            using (AssemblyGenerator asmGen = new AssemblyGenerator(/* isCSharp */ true, /* useFullTypeNames */ false, domainServiceTypes))
            {
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
                codeDomCodeGenAssemblyTypes = codeDomCodeGenAssembly.GetTypes();


                using (T4AssemblyGenerator asmGenT4 = new T4AssemblyGenerator(/* isCSharp */ true, /* useFullTypeNames */ false, domainServiceTypes))
                {
                    foreach (Type t in sharedTypes)
                    {
                        asmGenT4.MockSharedCodeService.AddSharedType(t);
                    }

                    foreach (string refAssembly in refAssemblies)
                    {
                        asmGenT4.ReferenceAssemblies.Add(refAssembly);
                    }

                    string generatedCodeT4 = asmGenT4.GeneratedCode;
                    Assert.IsFalse(string.IsNullOrEmpty(generatedCodeT4), "Failed to generate code:\r\n" + asmGenT4.ConsoleLogger.Errors);

                    var t4CodeGenAssembly = asmGenT4.GeneratedAssembly;
                    Assert.IsNotNull(t4CodeGenAssembly, "Assembly failed to build: " + asmGenT4.ConsoleLogger.Errors);
                    t4CodeGenAssemblyTypes = t4CodeGenAssembly.GetTypes();

                    TestHelper.VerifyTypeEquality(codeDomCodeGenAssemblyTypes, t4CodeGenAssemblyTypes);
                }
            }

        }

        private static void VerifyTypeEquality(Type[] t1, Type[] t2)
        {
            if (TestHelper.AreNotNull(t1, t2))
            {
                Assert.AreEqual(t1.Count(), t2.Count());
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
                Assert.AreEqual(fieldInfo1.Count(), fieldInfo2.Count());
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
                Assert.AreEqual(attributes1.Count(), attributes2.Count());
                // We cannot evaluate the attributes if for metadataloadcontext
                foreach (CustomAttributeData attr1 in attributes1)
                {
                    CustomAttributeData attr2 = attributes2.First(a => a.ToString() == attr1.ToString());
                    Assert.IsNotNull(attr2, $"Could not find an attribute matching '{attr1}' in t4 codegen generated assembly");
                    Assert.AreEqual(attr1.ConstructorArguments.Count(), attr2.ConstructorArguments.Count());
                    Assert.AreEqual(attr1.NamedArguments.Count, attr2.NamedArguments.Count);
                }
            }
        }

        private static void VerifyPropertiesEquality(PropertyInfo[] properties1, PropertyInfo[] properties2)
        {
            if (TestHelper.AreNotNull(properties1, properties2))
            {
                Assert.AreEqual(properties1.Count(), properties2.Count());
                foreach (PropertyInfo prop1 in properties1)
                {
                    PropertyInfo prop2 = properties2.First(p => p.Name == prop1.Name);
                    Assert.IsNotNull(prop2, $"Could not find a property matching '{prop1.Name}' in t4 codegen generated assembly");
                    Assert.IsTrue(AreTypesEqual(prop1.PropertyType, prop2.PropertyType));
                    TestHelper.VerifyAttributesEquality(prop1.GetCustomAttributesData(), prop2.GetCustomAttributesData());
                }
            }
        }

        private static void VerifyMethodsEquality(MethodInfo[] methods1, MethodInfo[] methods2)
        {
            if (TestHelper.AreNotNull(methods1, methods2))
            {
                Assert.AreEqual(methods1.Count(), methods2.Count());
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
                if (mParams.Count() == method1Params.Count())
                {
                    bool found = true;
                    for (int i = 0; i < mParams.Count(); i++)
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
            string t1Name = OpenRiaServices.Tools.TextTemplate.CodeGenUtilities.GetTypeName(t1);
            string t2Name = OpenRiaServices.Tools.TextTemplate.CodeGenUtilities.GetTypeName(t2);
            if (t1Name == t2Name)
            {
                return true;
            }
            return false;
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
