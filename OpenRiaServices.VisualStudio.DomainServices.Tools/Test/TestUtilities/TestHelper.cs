using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRiaServices.DomainServices.LinqToSql;
using Microsoft.VisualStudio.ServiceModel.DomainServices.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace Microsoft.AppFx.UnitTests.Setup.Utilities
{
    internal class TestHelper
    {
        public static string ExtensionFromLanguage(string language)
        {
            return language == "C#" ? ".cs" : ".vb";
        }

        public static string LineCommentFromLanguage(string language)
        {
            string extension = ExtensionFromLanguage(language);
            return extension.EndsWith("cs") ? "//" : "'";
        }

        public static string GetProjectPath()
        {
            string projectPathFile = Path.Combine(TestHelper.TestDir, "ProjectPath.txt");
            if (!File.Exists(projectPathFile))
                Assert.Fail("Could not find " + projectPathFile + ".  Did you forget a [Deployment] attribute?");

            string projectPath = string.Empty;
            string inputString = string.Empty;
            using (StreamReader t1 = new StreamReader(projectPathFile))
            {
                inputString = t1.ReadToEnd();
            }

            string[] split = inputString.Split(',');
            projectPath = split[0];
            return projectPath;
        }

        public static string GetProjectDir()
        {
            return Path.GetDirectoryName(GetProjectPath());
        }

        // Validates that a generated file matches a reference file.
        // Strips off code-gen boilerplate that may cause comparison problems
        public static void ValidateFilesEqual(string generatedFileName, string referenceFileName, bool updateBaselines)
        {
            string s1, s2;
            using (StreamReader t1 = new StreamReader(generatedFileName))
            {
                s1 = t1.ReadToEnd();
            }

            using (StreamReader t2 = new StreamReader(referenceFileName))
            {
                s2 = t2.ReadToEnd();
            }

            if (!s1.Equals(s2))
            {
                if (updateBaselines)
                {
                    FileInfo fInfo = new FileInfo(referenceFileName);
                    fInfo.Attributes &= ~FileAttributes.ReadOnly;
                    File.Copy(generatedFileName, referenceFileName, true);
                    System.Diagnostics.Debug.WriteLine("Updated baseline for \"" + referenceFileName + "\"");
                    return;
                }

                // Generate CMD strings to diff and to copy
                string tfDiffCommand = "tf diff \"" + referenceFileName + "\" \"" + generatedFileName + "\"\r\n";
                string tfEditCommand = "tf edit \"" + referenceFileName + "\"\r\n";
                string copyCommand = "copy \"" + generatedFileName + "\" \"" + referenceFileName + "\"";
                Assert.Fail(
                    "Generated file is different than the expected reference file.\r\n" +
                    "    Expected file:       " + referenceFileName + "\r\n" +
                    "    Newly generated file: " + generatedFileName + "\r\n" +
                    "\r\n------------------- To diff these files, execute this ------------------\r\n\r\n    " +
                    tfDiffCommand +
                    "\r\n---------------- To make this the new reference file, execute this ------------------\r\n\r\n    " +
                    tfEditCommand + "    " +
                    copyCommand + "\r\n\r\n-------------------------"
                    );
            }
        }

        // Returns the folder under which this test is currently running.
        public static string TestDir
        {
            get
            {
                return Path.GetDirectoryName(typeof(TestHelper).Assembly.Location);
            }
        }

        public static string GetOutputTestDataDir(string subDir)
        {
            string path = Path.GetFullPath(Path.Combine(TestDir, subDir));
            return path;
        }

        /// <summary>
        /// Converts all the whitespace in the given string into a single space char.
        /// This is intended to give us resilience to formatting and whitespace changes to CodeDom
        /// </summary>
        /// <param name="s">String to convert</param>
        /// <returns>Collapsed version of string</returns>
        public static string NormalizeWhitespace(string s)
        {
            StringBuilder sb = new StringBuilder();
            int maxLen = s.Length;
            for (int i = 0; i < maxLen; /* no incr */)
            {
                char c = s[i++];
                if (Char.IsWhiteSpace(c))
                {
                    sb.Append(' ');
                    while (i < maxLen && Char.IsWhiteSpace(s[i]))
                        ++i;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Asserts that the generated code contains all the expected code.
        /// Whitespace is normalized, so the input strings can use spaces to represent arbitrary whitespace
        /// </summary>
        /// <param name="generatedCode"></param>
        /// <param name="expectedCode"></param>
        public static void AssertGeneratedCodeContains(string generatedCode, params string[] expectedCode)
        {
            string normalizedGeneratedCode = NormalizeWhitespace(generatedCode);
            StringBuilder sb = new StringBuilder();
            foreach (string code in expectedCode)
            {
                string s = NormalizeWhitespace(code);
                if (!normalizedGeneratedCode.Contains(s))
                    Assert.Fail("Generated code did not contain:\r\n<" + code + ">");
            }
        }

        /// <summary>
        /// Assert none of the given strings are present in the generated code
        /// </summary>
        /// <param name="generatedCode"></param>
        /// <param name="expectedCode"></param>
        public static void AssertGeneratedCodeDoesNotContain(string generatedCode, params string[] expectedCode)
        {
            string normalizedGeneratedCode = NormalizeWhitespace(generatedCode);
            StringBuilder sb = new StringBuilder();
            foreach (string code in expectedCode)
            {
                string s = NormalizeWhitespace(code);
                if (normalizedGeneratedCode.Contains(s))
                    Assert.Fail("Generated code was not expected to contain:\r\n<" + code + ">");
            }
        }

        /// <summary>
        /// Asserts that all the expected items appear in the list of actual items
        /// </summary>
        /// <param name="expectedItems">The expected list.</param>
        /// <param name="actualItems">The list actually found.</param>
        /// <param name="noOthers">No additional items permitted</param>
        public static void AssertReferenceListContains(IEnumerable<string> expectedItems, IEnumerable<string> actualItems, bool noOthers)
        {
            if (noOthers && actualItems.Count() != expectedItems.Count())
            {
                StringBuilder actuals = new StringBuilder();
                foreach (string s in actualItems)
                {
                    actuals.AppendLine(s);
                }
                StringBuilder expecteds = new StringBuilder();
                foreach (string s in expectedItems)
                {
                    expecteds.AppendLine(s);
                }
                Assert.Fail("Expected exact list match but instead found:\r\nExpected:\r\n" + expecteds + "\r\nActual:\r\n" + actuals);
            }

            foreach (string expected in expectedItems)
            {
                // Exact match is good
                if (actualItems.Any(s => string.Equals(s, expected, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // Maybe fully qualified, so search for one that Contains the same short name, this could come from an assembly in the GAC
                // in the form of 'System.Linq, Version=...' or one from the file system with the full file path.
                if (!actualItems.Any(s => s.Contains(expected)))
                {
                    StringBuilder msg = new StringBuilder();
                    foreach (string s in actualItems)
                    {
                        msg.AppendLine(s);
                    }
                    Assert.Fail("Expected to find <" + expected + "> but found:\r\n" + msg.ToString());
                }
            }
        }

        
        /// <summary>
        /// Enables or disables L2S support.
        /// </summary>
        /// <param name="enable">when true, L2S support is enabled as it would when the toolkit is installed</param>
        public static void EnsureL2SSupport(bool enable)
        {
            if (enable)
            {
                LinqToSqlContext.OverrideAssemblyPath(typeof(LinqToSqlDomainService<>).Assembly.FullName);
            }
            else
            {
                LinqToSqlContext.OverrideAssemblyPath(null);
            }
        }
    }
}
