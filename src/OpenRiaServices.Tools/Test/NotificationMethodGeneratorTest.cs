﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    [TestClass()]
    public class NotificationMethodGeneratorTest
    {
        private TestContext context;
        public TestContext TestContext
        {
            get { return this.context; }
            set { this.context = value; }
        }

        private static XmlReader XmlReader;

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
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "PartialMethodsSnippetBlockArgs", new[] { "comments", "baseMethodNames", "parameters" });

        [
        DeploymentItem("OpenRiaServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        DeploymentItem("OpenRiaServices.Tools\\Test\\NotificationMethodGeneratorTestCodeSnippets.xml"),
        TestMethod,
#if NETFRAMEWORK
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "PartialMethodsSnippetBlockArgs", DataAccessMethod.Sequential)]
        public void PartialMethodsSnippetBlockTest()
        {
            string comments = this.TestContext.DataRow["comments"].ToString();
            string baseMethodNames = this.TestContext.DataRow["baseMethodNames"].ToString();
            string parameters = this.TestContext.DataRow["parameters"].ToString();

            PartialMethodsSnippetBlockTest(comments, baseMethodNames, parameters);
        }
#else
        DynamicData(nameof(PartialMethodsSnippetBlockTestCases))]
#endif
        public void PartialMethodsSnippetBlockTest(string comments, string baseMethodNames, string parameters)
        {
            string[] baseMethodNamesArray = baseMethodNames.Split(new char[] { ',' });

            PartialMethodsSnippetBlockTest(true, comments, baseMethodNamesArray, parameters);
            PartialMethodsSnippetBlockTest(false, comments, baseMethodNamesArray, parameters);
        }

        public void PartialMethodsSnippetBlockTest(bool isCSharp, string comments, string[] baseMethodNames, string paramDeclsArgs)
        {
            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));
            CodeParameterDeclarationExpressionCollection expressions = GetCodeParameterDeclaraionExpressions(paramDeclsArgs);

            foreach (string baseMethodName in baseMethodNames)
            {
                target.AddMethodFor(baseMethodName, expressions, comments);
            }

            // do verification ...
            if (XmlReader == null)
            {
                XmlReader = XmlReader.Create(this.TestContext.TestDeploymentDir + "\\NotificationMethodGeneratorTestCodeSnippets.xml");
            }

            do
            {
                XmlReader.Read();
            }
            while (!XmlReader.EOF && XmlReader.NodeType != XmlNodeType.CDATA);
            string snippetstr = "";

            foreach (CodeSnippetTypeMember snippet in target.PartialMethodsSnippetBlock)
            {
                foreach (CodeCommentStatement comment in snippet.Comments)
                {
                    if (!isCSharp)
                    {
                        Assert.IsTrue(comment.Comment.Text.StartsWith(" "), "All VB XML Doc comments must be prefixed with a space");
                    }
                    snippetstr += comment.Comment.Text.TrimStart();
                }
                snippetstr += snippet.Text;
            }
            Assert.AreEqual(snippetstr.Replace("\r\n", "").TrimEnd(), XmlReader.Value.Replace("\n", ""));
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

            Assert.AreEqual(target.OnCreatedMethodInvokeExpression.Method.MethodName, "OnCreated");
        }

        public static IEnumerable<object> OnCreatedMethodInvokeExpressionTestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "GetMethodInvokeExpressionStatementFor1Args", new[] { "comments", "baseMethodNames", "parameters" });

        [
        DeploymentItem("OpenRiaServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        TestMethod(),
#if NETFRAMEWORK
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "GetMethodInvokeExpressionStatementFor1Args", DataAccessMethod.Sequential)]
        public void GetMethodInvokeExpressionStatementForTest()
        {
            string comments = this.TestContext.DataRow["comments"].ToString();
            string baseMethodNames = this.TestContext.DataRow["baseMethodNames"].ToString();
            string parameters = this.TestContext.DataRow["parameters"].ToString();

            GetMethodInvokeExpressionStatementForTest(comments, baseMethodNames, parameters);
        }
#else
        DynamicData(nameof(OnCreatedMethodInvokeExpressionTestCases)),
        ]
#endif
        public void GetMethodInvokeExpressionStatementForTest(string comments, string baseMethodNames, string parameters)
        {
            string[] baseMethodNamesArray = baseMethodNames.Split(new char[] { ',' });

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
                CodeMethodInvokeExpression actualExpression = actual.Expression as CodeMethodInvokeExpression;

                Assert.AreEqual(actualExpression.Method.MethodName, "On" + baseMethodName);

                for (int idx = 0; idx < parameters.Length; idx++)
                {
                    string paramName = ((CodeArgumentReferenceExpression)actualExpression.Parameters[idx]).ParameterName;
                    Assert.AreEqual(paramName, parameters[idx].Name);
                }
            }
        }

        public static IEnumerable<object> AddMethodFor1TestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "AddMethodFor1Args", new[] { "comments" });

        [
        DeploymentItem("OpenRiaServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        TestMethod(),
#if NETFRAMEWORK
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "AddMethodFor1Args", DataAccessMethod.Sequential)]
        public void AddMethodFor1Test()
        {
            string comments = this.TestContext.DataRow["comments"].ToString();
            AddMethodFor1Test(comments);
        }
#else
        DynamicData(nameof(AddMethodFor1TestCases))]
#endif
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
        DeploymentItem("OpenRiaServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        TestMethod(),
#if NETFRAMEWORK
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "AddMethodFor2Args", DataAccessMethod.Sequential)]
        public void AddMethodFor2Test()
        {
            string comments = this.TestContext.DataRow["comments"].ToString();
            string parameterDeclaration = this.TestContext.DataRow["parameterDeclaration"].ToString();
            AddMethodFor2Test(comments, parameterDeclaration);
        }
#else
        DynamicData (nameof(AddMethodFor2TestCases))]
#endif
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
                string[] args = paramDeclArgs.Split(new char[] { ',' });
                parameterDeclaration = new CodeParameterDeclarationExpression(args[0], args[1]);
            }

            if (comments == "null") comments = null;

            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));

            target.AddMethodFor(baseMethodName, parameterDeclaration, comments);

            Assert.IsNotNull(GetSnippet(target, baseMethodName), "Could not find generated method, Language: " + (isCSharp ? "C#" : "VB"));
        }



        public static IEnumerable<object> AddMethodFor3TestCases
            => GetTestCasesFromXml("NotificationMethodGeneratorTests.xml", "AddMethodFor3Args", new[] { "comments", "parameters" });

        [
        DeploymentItem("OpenRiaServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        TestMethod(),
#if NETFRAMEWORK
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "AddMethodFor3Args", DataAccessMethod.Sequential)]
        public void AddMethodFor3Test()
        {
            string comments = this.TestContext.DataRow["comments"].ToString();
            string parameters = this.TestContext.DataRow["parameters"].ToString();
            AddMethodFor2Test(comments, parameters);
        }
#else
        DynamicData(nameof(AddMethodFor3TestCases))]
#endif
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

                string[] paramDecls = paramDeclsArgs.Split(new char[] { ';' });
                foreach (string paramDecl in paramDecls)
                {
                    if (paramDecl != "")
                    {
                        string[] args = paramDecl.Split(new char[] { ',' });
                        Assert.AreEqual(args.Length, 2, "Params definition file not in the correct format!");
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
