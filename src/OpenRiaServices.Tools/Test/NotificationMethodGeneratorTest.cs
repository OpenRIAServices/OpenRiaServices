using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools.Test
{
    [TestClass()]
    [DeploymentItem("NotificationMethodGeneratorTestCodeSnippets.xml")]
    [DeploymentItem("NotificationMethodGeneratorTests.xml")]
    public class NotificationMethodGeneratorTest
    {
        private static readonly string[] s_expectedSnippets = LoadSnippets("NotificationMethodGeneratorTestCodeSnippets.xml");

        private static string[] LoadSnippets(string path)
        {
            List<string> list = [];
            using XmlReader xmlReader = XmlReader.Create(path);

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.CDATA)
                {
                    list.Add(xmlReader.Value.Replace("\n", ""));
                }
            }

            return [.. list];
        }

        static IEnumerable<object[]> GetTestCasesFromXml(string filename, string nodeName, string[] attributes)
        {
            using var reader = XmlReader.Create(filename);
            if (!reader.ReadToDescendant(nodeName))
                throw new ArgumentException(message: "No node with specified name exist", paramName: nameof(nodeName));

            do
            {
                object[] values = new object[attributes.Length];
                for (int i = 0; i < attributes.Length; i++)
                {
                    values[i] = reader.GetAttribute(attributes[i]);
                }
                yield return values;
            }
            while (reader.ReadToFollowing(nodeName));
        }

        public static IEnumerable<object> PartialMethodsSnippetBlockTestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "PartialMethodsSnippetBlockArgs", new[] { "comments", "baseMethodNames", "parameters", "index" });

        [
        TestMethod,
        DynamicData(nameof(PartialMethodsSnippetBlockTestCases))]
        public void PartialMethodsSnippetBlockTest(string comments, string baseMethodNames, string parameters, string index)
        {
            string[] baseMethodNamesArray = baseMethodNames.Split(',');

            PartialMethodsSnippetBlockTest(true, comments, baseMethodNamesArray, parameters, int.Parse(index));
            PartialMethodsSnippetBlockTest(false, comments, baseMethodNamesArray, parameters, int.Parse(index) + 1);
        }

        public void PartialMethodsSnippetBlockTest(bool isCSharp, string comments, string[] baseMethodNames, string paramDeclsArgs, int index)
        {
            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));
            CodeParameterDeclarationExpressionCollection expressions = GetCodeParameterDeclaraionExpressions(paramDeclsArgs);

            foreach (string baseMethodName in baseMethodNames)
            {
                target.AddMethodFor(baseMethodName, expressions, comments);
            }

            StringBuilder actualSnippet = new StringBuilder();

            foreach (CodeSnippetTypeMember snippet in target.PartialMethodsSnippetBlock)
            {
                foreach (CodeCommentStatement comment in snippet.Comments)
                {
                    if (!isCSharp)
                    {
                        Assert.IsTrue(comment.Comment.Text.StartsWith(" ", StringComparison.Ordinal), "All VB XML Doc comments must be prefixed with a space");
                    }
                    actualSnippet.Append(comment.Comment.Text.TrimStart());
                }
                actualSnippet.Append(snippet.Text);
            }
            string expectedSnippet = s_expectedSnippets[index];
            Assert.AreEqual(expectedSnippet, actualSnippet.Replace("\r\n", "").ToString().TrimEnd());
        }

        [TestMethod]
        public void OnCreatedMethodInvokeExpressionTest()
        {
            OnCreatedMethodInvokeExpressionTest(true);
            OnCreatedMethodInvokeExpressionTest(false);
        }

        public void OnCreatedMethodInvokeExpressionTest(bool isCSharp)
        {
            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));

            Assert.AreEqual("OnCreated", target.OnCreatedMethodInvokeExpression.Method.MethodName);
        }

        public static IEnumerable<object> OnCreatedMethodInvokeExpressionTestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "GetMethodInvokeExpressionStatementFor1Args", new[] { "comments", "baseMethodNames", "parameters" });

        [
        TestMethod(),
        DynamicData(nameof(OnCreatedMethodInvokeExpressionTestCases)),
        ]
        public void GetMethodInvokeExpressionStatementForTest(string comments, string baseMethodNames, string parameters)
        {
            string[] baseMethodNamesArray = baseMethodNames.Split(',');

            GetMethodInvokeExpressionStatementForTest(true, comments, baseMethodNamesArray, parameters);
            GetMethodInvokeExpressionStatementForTest(false, comments, baseMethodNamesArray, parameters);
        }

        public void GetMethodInvokeExpressionStatementForTest(bool isCSharp, string comments, string[] baseMethodNames, string paramDeclsArgs)
        {
            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));

            CodeParameterDeclarationExpressionCollection expressions = GetCodeParameterDeclaraionExpressions(paramDeclsArgs);
            CodeParameterDeclarationExpression[] parameters = new CodeParameterDeclarationExpression[expressions.Count];

            expressions.CopyTo(parameters, 0);

            foreach (string baseMethodName in baseMethodNames)
            {
                target.AddMethodFor(baseMethodName, expressions, comments);

                CodeExpressionStatement actual = target.GetMethodInvokeExpressionStatementFor(baseMethodName);
                CodeMethodInvokeExpression actualExpression = (CodeMethodInvokeExpression)actual.Expression;

                Assert.AreEqual("On" + baseMethodName, actualExpression.Method.MethodName);

                for (int idx = 0; idx < parameters.Length; idx++)
                {
                    string paramName = ((CodeArgumentReferenceExpression)actualExpression.Parameters[idx]).ParameterName;
                    Assert.AreEqual(parameters[idx].Name, paramName);
                }
            }
        }

        public static IEnumerable<object> AddMethodFor1TestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "AddMethodFor1Args", new[] { "comments" });

        [
        TestMethod(),
        DynamicData(nameof(AddMethodFor1TestCases))]
        public void AddMethodFor1Test(string comments)
        {
            AddMethodFor1Test(true, comments);
            AddMethodFor1Test(false, comments);
        }

        public void AddMethodFor1Test(bool isCSharp, string comments)
        {
            string baseMethodName = "MyMethod"; // required param

            if (comments == "null") comments = null;

            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));

            target.AddMethodFor(baseMethodName, comments);

            Assert.IsNotNull(GetSnippet(target, baseMethodName), "Could not find generated method, Language: " + (isCSharp ? "C#" : "VB"));
        }

        public static IEnumerable<object> AddMethodFor2TestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "AddMethodFor2Args", new[] { "comments", "parameterDeclaration" });

        [
        TestMethod(),
        DynamicData(nameof(AddMethodFor2TestCases))]
        public void AddMethodFor2Test(string comments, string parameterDeclaration)
        {

            AddMethodFor2Test(true, comments, parameterDeclaration);
            AddMethodFor2Test(false, comments, parameterDeclaration);
        }
        public void AddMethodFor2Test(bool isCSharp, string comments, string paramDeclArgs)
        {
            string baseMethodName = "MyMethod"; // required param
            CodeParameterDeclarationExpression parameterDeclaration = null;

            if (string.IsNullOrEmpty(paramDeclArgs))
            {
                parameterDeclaration = new CodeParameterDeclarationExpression();
            }
            else if (paramDeclArgs != "null")
            {
                string[] args = paramDeclArgs.Split(',');
                parameterDeclaration = new CodeParameterDeclarationExpression(args[0], args[1]);
            }

            if (comments == "null") comments = null;

            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));

            target.AddMethodFor(baseMethodName, parameterDeclaration, comments);

            Assert.IsNotNull(GetSnippet(target, baseMethodName), "Could not find generated method, Language: " + (isCSharp ? "C#" : "VB"));
        }



        public static IEnumerable<object> AddMethodFor3TestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "AddMethodFor3Args", new[] { "comments", "parameters" });

        [TestMethod()]
        [DynamicData(nameof(AddMethodFor3TestCases))]
        public void AddMethodFor3Test(string comments, string paramDeclsArgs)
        {
            AddMethodFor3Test(true, comments, paramDeclsArgs);
            AddMethodFor3Test(false, comments, paramDeclsArgs);
        }

        public void AddMethodFor3Test(bool isCSharp, string comments, string paramDeclsArgs)
        {
            string baseMethodName = "MyMethod"; // required param

            CodeParameterDeclarationExpressionCollection parameters = GetCodeParameterDeclaraionExpressions(paramDeclsArgs);

            if (comments == "null") comments = null;

            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));

            target.AddMethodFor(baseMethodName, parameters, comments);

            Assert.IsNotNull(GetSnippet(target, baseMethodName), "Could not find generated method, Language: " + (isCSharp ? "C#" : "VB"));
        }

        [TestMethod()]
        public void NotificationMethodGeneratorConstructorTest()
        {
            bool isCSharp = false;
            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));
            Assert.IsNotNull(target.PartialMethodsSnippetBlock);
        }

        /// <summary>
        /// Generates a CodeParameterDeclarationExpresseionCollection from a semicolon separated string of 
        /// comma delimited type-name value pair argument strings.
        /// </summary>
        /// <param name="paramDeclArgs"></param>
        /// <returns></returns>
        private static CodeParameterDeclarationExpressionCollection GetCodeParameterDeclaraionExpressions(string paramDeclsArgs)
        {
            CodeParameterDeclarationExpressionCollection parameters = null;

            if (paramDeclsArgs != "null")
            {
                parameters = new CodeParameterDeclarationExpressionCollection();

                string[] paramDecls = paramDeclsArgs.Split(';');
                foreach (string paramDecl in paramDecls)
                {
                    if (paramDecl != "")
                    {
                        string[] args = paramDecl.Split(',');
                        Assert.AreEqual(2, args.Length, "Params definition file not in the correct format!");
                        CodeParameterDeclarationExpression codeParam = new CodeParameterDeclarationExpression(args[0], args[1]);
                        parameters.Add(codeParam);
                    }
                    // else  Note: setting an empty CodeParamDeclExp creates a 'void' param (we don't do this for code gen)
                    // codeParam = new CodeParameterDeclarationExpression();
                }
            }

            return parameters;
        }

        /// <summary>
        /// Checks if a code snippet was generated for the specified method name.
        /// This code does not check whether the code snippet is valir or not.
        /// </summary>
        private static CodeSnippetTypeMember GetSnippet(NotificationMethodGenerator target, string baseMethodName)
        {
            CodeSnippetTypeMember genMethod = null;

            foreach (CodeTypeMember member in target.PartialMethodsSnippetBlock)
            {
                genMethod = member as CodeSnippetTypeMember;
                if (genMethod != null && genMethod.Text.Contains(baseMethodName))
                {
                    return genMethod;
                }
                genMethod = null;
            }

            return genMethod;
        }

        private static CodeDomClientCodeGenerator CreateProxyGenerator(bool isCSharp)
        {
            MockCodeGenerationHost host = new MockCodeGenerationHost();
            CodeDomClientCodeGenerator generator = isCSharp
                                                        ? (CodeDomClientCodeGenerator)new CSharpCodeDomClientCodeGenerator()
                                                        : (CodeDomClientCodeGenerator)new VisualBasicCodeDomClientCodeGenerator();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = isCSharp ? "C#" : "VB",
            };
            generator.Initialize(host, new DomainServiceDescription[] { DomainServiceDescription.GetDescription(typeof(MockOrder_DomainService)) }, options);
            return generator;
        }
    }
}
