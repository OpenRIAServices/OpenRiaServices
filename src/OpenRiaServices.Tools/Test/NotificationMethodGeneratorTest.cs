using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools.Test
{
    [TestClass()]
    public class NotificationMethodGeneratorTest
    {
        private static readonly string s_expectedSnippetSummary = "<summary>This method is invoked from the constructor once initialization is complete andcan be used for further object setup.</summary>        ";
        private static readonly string[] s_expectedSnippets = 
        [
            "partial void OnCreated();These are the comments for the generated method.        partial void OnMyPropertyChanged(int arg1, bool arg2, string arg3);",
            "Private Partial Sub OnCreated()        End SubThese are the comments for the generated method.        Private Partial Sub OnMyPropertyChanged(ByVal arg1 As Integer, ByVal arg2 As Boolean, ByVal arg3 As String)        End Sub",
            "partial void OnCreated();        partial void OnMyProperty();        partial void OnInvoke();",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnMyProperty()        End Sub        Private Partial Sub OnInvoke()        End Sub",
            "partial void OnCreated();        partial void OnMyProperty(int arg1, bool arg2, string arg3);        partial void OnInvoke(int arg1, bool arg2, string arg3);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnMyProperty(ByVal arg1 As Integer, ByVal arg2 As Boolean, ByVal arg3 As String)        End Sub        Private Partial Sub OnInvoke(ByVal arg1 As Integer, ByVal arg2 As Boolean, ByVal arg3 As String)        End Sub",
            "partial void OnCreated();        partial void OnIsPublic(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnIsPublic(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub",
            "partial void OnCreated();        partial void Onispublic(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub Onispublic(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub",
            "partial void OnCreated();        partial void OnpublicProp(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnpublicProp(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub",
            "partial void OnCreated();        partial void OnPublicProp(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnPublicProp(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub",
            "partial void OnCreated();        partial void OnIsPartial(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnIsPartial(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub",
            "partial void OnCreated();        partial void Onispartial(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub Onispartial(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub",
            "partial void OnCreated();        partial void OnpartialProp(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnpartialProp(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub",
            "partial void OnCreated();        partial void OnPartialProp(int publicArg1, int PublicArg2, bool ispublic, bool isPublic);",
            "Private Partial Sub OnCreated()        End Sub        Private Partial Sub OnPartialProp(ByVal publicArg1 As Integer, ByVal PublicArg2 As Integer, ByVal ispublic As Boolean, ByVal isPublic As Boolean)        End Sub"
        ];

        static readonly string s_notificationMethodGeneratorTests = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<TestMethods>
  <AddMethodFor1Args comments=""These are the comments for the generated method.""/>
  <AddMethodFor1Args comments=""""/>
  <AddMethodFor1Args comments=""null""/>

  <AddMethodFor2Args comments=""These are the comments for the generated method."" parameterDeclaration=""System.Int32,value""/>
  <AddMethodFor2Args comments=""null"" parameterDeclaration=""""/>
  <AddMethodFor2Args comments=""null"" parameterDeclaration=""null""/>
  <AddMethodFor2Args comments=""These are the comments for the generated method."" parameterDeclaration=""null""/>

  <AddMethodFor3Args comments=""These are the comments for the generated method."" parameters=""System.System.Int32,arg1;System.System.Boolean,arg2;System.String,arg3""/>
  <AddMethodFor3Args comments=""null"" parameters=""""/>
  <AddMethodFor3Args comments=""null"" parameters=""null""/>
  <AddMethodFor3Args comments=""These are the comments for the generated method."" parameters=""null""/>
  
  <GetMethodInvokeExpressionStatementFor1Args comments=""These are the comments for the generated method."" parameters=""System.Int32,arg1;System.Boolean,arg2;System.String,arg3"" baseMethodNames=""MyProperty""/>
  <GetMethodInvokeExpressionStatementFor1Args comments="""" parameters=""System.Int32,arg1;System.Boolean,arg2;System.String,arg3"" baseMethodNames=""MyProperty""/>
  <GetMethodInvokeExpressionStatementFor1Args comments=""These are the comments for the generated method."" parameters="""" baseMethodNames=""MyProperty,Invoke""/>
  <GetMethodInvokeExpressionStatementFor1Args comments="""" parameters=""System.Int32,arg1;System.Boolean,arg2;System.String,arg3"" baseMethodNames=""MyProperty,Invoke""/>
  <GetMethodInvokeExpressionStatementFor1Args comments="""" parameters="""" baseMethodNames=""MyProperty,Invoke""/>
  <GetMethodInvokeExpressionStatementFor1Args comments=""These are the comments for the generated method."" parameters=""System.Int32,arg1"" baseMethodNames=""PropertyChanged""/>
  <GetMethodInvokeExpressionStatementFor1Args comments="""" parameters=""System.Int32,arg1"" baseMethodNames=""PropertyChanged""/>
  
  <OnCreatedMethodInvokeExpressionArgs isCSharp=""true""></OnCreatedMethodInvokeExpressionArgs>
  
  <PartialMethodsSnippetBlockArgs comments=""These are the comments for the generated method."" parameters=""System.Int32,arg1;System.Boolean,arg2;System.String,arg3"" baseMethodNames=""MyPropertyChanged"" index=""0""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters="""" baseMethodNames=""MyProperty,Invoke"" index=""2""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,arg1;System.Boolean,arg2;System.String,arg3"" baseMethodNames=""MyProperty,Invoke"" index=""4""/>
  
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""IsPublic"" index=""6""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""ispublic"" index=""8""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""publicProp"" index=""10""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""PublicProp"" index=""12""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""IsPartial"" index=""14""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""ispartial"" index=""16""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""partialProp"" index=""18""/>
  <PartialMethodsSnippetBlockArgs comments="""" parameters=""System.Int32,publicArg1;System.Int32,PublicArg2;System.Boolean,ispublic;System.Boolean,isPublic"" baseMethodNames=""PartialProp"" index=""20""/>
</TestMethods>";

        static IEnumerable<object[]> GetTestCasesFromXml(string xml, string nodeName, string[] attributes)
        {
            using StringReader stringReader = new StringReader(xml);
            using XmlReader reader = XmlReader.Create(stringReader);
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
            => GetTestCasesFromXml(s_notificationMethodGeneratorTests, "PartialMethodsSnippetBlockArgs", ["comments", "baseMethodNames", "parameters", "index"]);

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
            string expectedSnippet = s_expectedSnippetSummary + s_expectedSnippets[index];
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
            => GetTestCasesFromXml(s_notificationMethodGeneratorTests, "GetMethodInvokeExpressionStatementFor1Args", ["comments", "baseMethodNames", "parameters"]);

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
            => GetTestCasesFromXml(s_notificationMethodGeneratorTests, "AddMethodFor1Args", ["comments"]);

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
            => GetTestCasesFromXml(s_notificationMethodGeneratorTests, "AddMethodFor2Args", ["comments", "parameterDeclaration"]);

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
            => GetTestCasesFromXml(s_notificationMethodGeneratorTests, "AddMethodFor3Args", ["comments", "parameters"]);

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
