using System.CodeDom;
using System.Collections.Generic;
using System.ServiceModel.DomainServices.Server;
using System.ServiceModel.DomainServices.Server.Test.Utilities;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
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

        #region Additional test attributes

        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            UnitTestTraceListener.Initialize(testContext, true);
        }

        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            UnitTestTraceListener.Reset();
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
        }

        #endregion

        private static XmlReader XmlReader;

        [
        DeploymentItem("Microsoft.ServiceModel.DomainServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        DeploymentItem("Microsoft.ServiceModel.DomainServices.Tools\\Test\\NotificationMethodGeneratorTestCodeSnippets.xml"),
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "PartialMethodsSnippetBlockArgs", DataAccessMethod.Sequential),
        TestMethod()
        ]
        public void PartialMethodsSnippetBlockTest()
        {
            PartialMethodsSnippetBlockTest(true);
            PartialMethodsSnippetBlockTest(false);
        }

        public void PartialMethodsSnippetBlockTest(bool isCSharp)
        {
            string comments = this.TestContext.DataRow["comments"].ToString();
            string[] baseMethodNames = this.TestContext.DataRow["baseMethodNames"].ToString().Split(new char[] { ',' });
            string paramDeclsArgs = this.TestContext.DataRow["parameters"].ToString();

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

        [
        DeploymentItem("Microsoft.ServiceModel.DomainServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "OnCreatedMethodInvokeExpressionArgs", DataAccessMethod.Sequential),
        TestMethod()
        ]
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

        [
        DeploymentItem("Microsoft.ServiceModel.DomainServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "GetMethodInvokeExpressionStatementFor1Args", DataAccessMethod.Sequential),
        TestMethod()
        ]
        public void GetMethodInvokeExpressionStatementForTest()
        {
            GetMethodInvokeExpressionStatementForTest(true);
            GetMethodInvokeExpressionStatementForTest(false);
        }
        public void GetMethodInvokeExpressionStatementForTest(bool isCSharp)
        {
            string comments = this.TestContext.DataRow["comments"].ToString();
            string[] baseMethodNames = this.TestContext.DataRow["baseMethodNames"].ToString().Split(new char[] { ',' });
            string paramDeclsArgs = this.TestContext.DataRow["parameters"].ToString();

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

        [
        DeploymentItem("Microsoft.ServiceModel.DomainServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "AddMethodFor1Args", DataAccessMethod.Sequential),
        TestMethod()
        ]
        public void AddMethodFor1Test()
        {
            AddMethodFor1Test(true);
            AddMethodFor1Test(false);
        }
        public void AddMethodFor1Test(bool isCSharp)
        {
            string baseMethodName = "MyMethod"; // required param

            string comments = this.TestContext.DataRow["comments"].ToString();

            if (comments == "null") comments = null;

            NotificationMethodGenerator target = new NotificationMethodGenerator(CreateProxyGenerator(isCSharp));

            target.AddMethodFor(baseMethodName, comments);

            Assert.IsNotNull(GetSnippet(target, baseMethodName), "Could not find generated method, Language: " + (isCSharp ? "C#" : "VB"));
        }

        [
        DeploymentItem("Microsoft.ServiceModel.DomainServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "AddMethodFor2Args", DataAccessMethod.Sequential),
        TestMethod()
        ]
        public void AddMethodFor2Test()
        {
            AddMethodFor2Test(true);
            AddMethodFor2Test(false);
        }
        public void AddMethodFor2Test(bool isCSharp)
        {
            string baseMethodName = "MyMethod"; // required param

            string comments = this.TestContext.DataRow["comments"].ToString();
            string paramDeclArgs = this.TestContext.DataRow["parameterDeclaration"].ToString();

            CodeParameterDeclarationExpression parameterDeclaration = null;

            if (paramDeclArgs == "")
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

        [
        DeploymentItem("Microsoft.ServiceModel.DomainServices.Tools\\Test\\NotificationMethodGeneratorTests.xml"),
        DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\NotificationMethodGeneratorTests.xml", "AddMethodFor3Args", DataAccessMethod.Sequential),
        TestMethod()
        ]
        public void AddMethodFor3Test()
        {
            AddMethodFor3Test(true);
            AddMethodFor3Test(false);
        }
        public void AddMethodFor3Test(bool isCSharp)
        {
            string baseMethodName = "MyMethod"; // required param

            string comments = this.TestContext.DataRow["comments"].ToString();
            string paramDeclsArgs = this.TestContext.DataRow["parameters"].ToString();

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

            if (paramDeclsArgs == "" || paramDeclsArgs != "null")
            {
                parameters = new CodeParameterDeclarationExpressionCollection();

                if (paramDeclsArgs != "null")
                {
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
                                                        ? (CodeDomClientCodeGenerator) new CSharpCodeDomClientCodeGenerator() 
                                                        : (CodeDomClientCodeGenerator) new VisualBasicCodeDomClientCodeGenerator();
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = isCSharp ? "C#" : "VB",
            };
            generator.Initialize(host, new DomainServiceDescription[] { DomainServiceDescription.GetDescription(typeof(MockOrder_DomainService))}, options );
            return generator;
        }
    }
}
