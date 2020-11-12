namespace OpenRiaServices.Hosting.Wcf.OData.Test
{
    #region Namespaces
    using System;
    using System.Data.Test.Astoria;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;
    #endregion

    public class TestUtil
    {
        /// <summary>Atom mime type.</summary>
        public static readonly string AtomFormat = "applicaTion/atom+xMl";
        
        /// <summary>Json mime type.</summary>
        public static readonly string JsonFormat = "applicatIon/jsOn";

        /// <summary>Name of OData test endpoint.</summary>
        public static readonly string ODataEndPointName = "dataservice";

        /// <summary>Path to OData test endpoint.</summary>
        public static readonly string ODataEndPointPath = "/" + ODataEndPointName + "/";

        /// <summary>Reusable namespace manager for tests.</summary>
        private static XmlNamespaceManager testNamespaceManager;

        /// <summary>Reusable name table for tests.</summary>
        private static XmlNameTable testNameTable;

        /// <summary>Reusable name table for tests.</summary>
        public static XmlNameTable TestNameTable
        {
            get
            {
                if (testNameTable == null)
                {
                    testNameTable = new NameTable();
                }

                return testNameTable;
            }
        }

        /// <summary>Reusable namespace manager for tests.</summary>
        public static XmlNamespaceManager TestNamespaceManager
        {
            get
            {
                if (testNamespaceManager == null)
                {
                    testNamespaceManager = new XmlNamespaceManager(TestNameTable);

                    // Some common namespaces used by legacy tests.
                    testNamespaceManager.AddNamespace("csdl", "http://schemas.microsoft.com/ado/2006/04/edm");
                    testNamespaceManager.AddNamespace("csdl1", "http://schemas.microsoft.com/ado/2007/05/edm");
                    testNamespaceManager.AddNamespace("csdl12", "http://schemas.microsoft.com/ado/2008/01/edm");
                    testNamespaceManager.AddNamespace("csdl2", "http://schemas.microsoft.com/ado/2008/09/edm");
                    testNamespaceManager.AddNamespace("ads", "http://schemas.microsoft.com/ado/2007/08/dataservices");
                    testNamespaceManager.AddNamespace("adsm", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
                    testNamespaceManager.AddNamespace("app", "http://www.w3.org/2007/app");
                    testNamespaceManager.AddNamespace("atom", "http://www.w3.org/2005/Atom");
                    testNamespaceManager.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2007/06/edmx");
                }

                return testNamespaceManager;
            }
        }

        /// <summary>Checks that <paramref name="argumentValue"/> is not null, throws an exception otherwise.</summary>
        /// <param name="argumentValue">Argument value to check.</param>
        /// <param name="argumentName">Argument name.</param>
        public static void CheckArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>Ensures that the specified stream can seek, possibly creating a new one.</summary>
        /// <param name="stream"><see cref="Stream"/> to ensure supports seeking.</param>
        /// <returns><paramref name="stream"/> if CanSeek is true, otherwise an in-memory copy.</returns>
        public static Stream EnsureStreamWithSeek(Stream stream)
        {
            CheckArgumentNotNull(stream, "stream");
            if (stream.CanSeek)
            {
                return stream;
            }
            else
            {
                MemoryStream result = new MemoryStream();
                IOUtil.CopyStream(stream, result);
                result.Position = 0;
                return result;
            }
        }

        /// <summary>Selects nodes from the specified node asserting their existence.</summary>
        /// <param name="node">Node to look in.</param>
        /// <param name="xpath">XPath for element to be returned.</param>
        /// <returns>The list of nodes found by the xpath.</returns>
        public static XmlNodeList AssertSelectNodes(XmlNode node, string xpath)
        {
            Debug.Assert(node != null, "node != null");
            Debug.Assert(xpath != null, "xpath != null");

            XmlNodeList result = node.SelectNodes(xpath, TestNamespaceManager);
            if (result.Count == 0)
            {
                TraceXml(node);
                throw new InvalidOperationException("Selection of [" + xpath + "] failed to return one or more nodes in last traced XML.");
            }

            return result;
        }

        /// <summary>Writes the specified node to the Trace debugging object.</summary>
        /// <param name="node">Node to write.</param>
        public static void TraceXml(XmlNode node)
        {
            if (node == null)
            {
                Trace.WriteLine("<null node>");
                return;
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CheckCharacters = false;
            settings.CloseOutput = false;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineHandling = NewLineHandling.None;
            StringBuilder output = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(output, settings);
            writer.WriteNode(node.CreateNavigator(), false);
            writer.Flush();
            Trace.WriteLine(output.ToString());
        }

        /// <summary>Runs the specified action and catches any thrown exception.</summary>
        /// <param name="action">Action to run.</param>
        /// <returns>Caught exception; null if none was thrown.</returns>
        public static Exception RunCatching(Action action)
        {
            Debug.Assert(action != null, "action != null");

            Exception result = null;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                result = exception;
            }

            return result;
        }

        /// <summary>
        /// Verifies that the specified XPath (or more) evaluate to true.
        /// </summary>
        /// <param name="node">Node to look in.</param>
        /// <param name="xpaths">The xpaths to verify.</param>
        public static void VerifyXPathExpressionResults(XNode node, object expectedResult, params string[] xpaths)
        {
            VerifyXPathExpressionResults(node.CreateNavigator(TestNameTable), expectedResult, xpaths);
        }

        /// <summary>
        /// Verifies that the specified XPath (or more) evaluate to true.
        /// </summary>
        /// <param name="navigable">Document to look in.</param>
        /// <param name="xpaths">The xpaths to verify.</param>
        public static void VerifyXPathExpressionResults(IXPathNavigable navigable, object expectedResult, params string[] xpaths)
        {
            XPathNavigator nav = navigable.CreateNavigator();
            foreach (string xpath in xpaths)
            {
                object actualResult = nav.Evaluate(xpath, TestNamespaceManager);
                Assert.AreEqual(expectedResult, actualResult, "Expression: " + xpath + " evaluated to " + actualResult.ToString());
            }
        }

        /// <summary>Creates an object that will restore a static value on disposal.</summary>
        /// <param name="type">Type to read static value from.</param>
        /// <param name="propertyName">Name of property to read value from.</param>
        /// <returns>An object that will restore a static value on disposal.</returns>
        /// <remarks>
        /// The usage pattern is:
        /// using (var r = TestUtil.RestoreStaticValueOnDispose(typeof(Foo), "Prop")) { ... }
        /// </remarks>
        public static IDisposable RestoreStaticValueOnDispose(Type type, string propertyName)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;
            MemberTypes memberTypes = MemberTypes.Property | MemberTypes.Field;
            MemberInfo propertyInfo = type.GetMember(propertyName, memberTypes, flags).FirstOrDefault();
            if (propertyInfo == null)
            {
                throw new Exception("Unable to find property " + propertyName + " on type " + type + ".");
            }
            return new StaticValueRestorer(propertyInfo);
        }

        /// <summary>Creates an object that will restore all static members of the given type on disposal</summary>
        /// <param name="type">Type to restore</param>
        /// <returns>An object that will restore all static members of the given type on disposal</returns>
        public static IDisposable RestoreStaticMembersOnDispose(Type type)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty;
            List<MemberInfo> memberInfos = new List<MemberInfo>();
            memberInfos.AddRange(type.GetFields(flags));
            memberInfos.AddRange(type.GetProperties(flags));
            return new StaticValueRestorer(memberInfos.ToArray());
        }

        /// <summary>A zero-length object array.</summary>
        public static readonly object[] EmptyObjectArray = Array.Empty<object>();

        private class StaticValueRestorer : IDisposable
        {
            private readonly Dictionary<MemberInfo, object> membersToRestore = new Dictionary<MemberInfo, object>();

            internal StaticValueRestorer(params MemberInfo[] memberInfos)
            {
                foreach (MemberInfo memberInfo in memberInfos)
                {
                    object value = null;
                    if (memberInfo is PropertyInfo)
                    {
                        value = ((PropertyInfo)memberInfo).GetValue(null, EmptyObjectArray);
                    }
                    else
                    {
                        FieldInfo fieldInfo = memberInfo as FieldInfo;
                        Debug.Assert(fieldInfo != null, "fieldInfo != null");
                        if (fieldInfo.IsLiteral)
                        {
                            // We do not need to restore const fields
                            continue;
                        }

                        value = fieldInfo.GetValue(null);
                    }

                    membersToRestore.Add(memberInfo, value);
                }
            }

            public void Dispose()
            {
                foreach (KeyValuePair<MemberInfo, object> member in this.membersToRestore)
                {
                    if (member.Key is PropertyInfo)
                    {
                        ((PropertyInfo)member.Key).SetValue(null, member.Value, EmptyObjectArray);
                    }
                    else
                    {
                        FieldInfo field = member.Key as FieldInfo;
                        Debug.Assert(field != null && !field.IsLiteral, "field != null && !field.IsLiteral");
                        field.SetValue(null, member.Value);
                    }
                }
            }
        }
    }
}
