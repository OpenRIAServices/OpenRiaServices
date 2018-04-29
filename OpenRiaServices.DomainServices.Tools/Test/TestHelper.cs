using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using OpenRiaServices.DomainServices.Tools;
using OpenRiaServices.DomainServices.Tools.SharedTypes;
using OpenRiaServices.DomainServices.Tools.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Server.Test.Utilities;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    public class TestHelper
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

        // Returns the full path to a file in the test project's output dir
        public static string GetOutputPath(string projectOutputDir, string baseName)
        {
            return Path.Combine(projectOutputDir, baseName);
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

        // Validates that a generated file matches a reference file.
        // Strips off code-gen boilerplate that may cause comparison problems
        public static void ValidateFilesEqual(string relativeTestDir, string relativeDeployDir, string generatedFileName, string referenceFileName, string language)
        {
            string diffMessage = null;

            if (!FilesMatch(relativeTestDir, relativeDeployDir, generatedFileName, referenceFileName, language, out diffMessage))
            {
                Assert.Fail(diffMessage);
            }
        }

        public static bool FilesMatch(string relativeTestDir, string relativeDeployDir, string generatedFileName,
            string referenceFileName, string language, out string diffMessage)
        {
            string lineCommentStart = LineCommentFromLanguage(language);

            string s1, s2;
            using (StreamReader t1 = new StreamReader(generatedFileName))
            {
                s1 = t1.ReadToEnd();
            }

            using (StreamReader t2 = new StreamReader(referenceFileName))
            {
                s2 = t2.ReadToEnd();
            }
            s1 = StripAutoGenPrefix(s1, lineCommentStart);
            s2 = StripAutoGenPrefix(s2, lineCommentStart);

            if (!s1.Equals(s2))
            {
                string outDataDir = GetOutputTestDataDir(relativeDeployDir);
                string projectDataDir = GetProjectTestDataDir(relativeDeployDir, relativeTestDir);

                // Where the real reference file lives (the one we deployed *from*)
                string realFileInProject = Path.Combine(projectDataDir, Path.GetFileName(referenceFileName));

                // Generate CMD strings to diff and to copy
                string tfDiffCommand = "tf diff \"" + referenceFileName + "\" \"" + generatedFileName + "\"\r\n";
                string tfEditCommand = "tf edit \"" + realFileInProject + "\"\r\n";
                string copyCommand = "copy \"" + generatedFileName + "\" \"" + realFileInProject + "\"";

                // Write edit and copy commands to a common .bat file
                string outDataDirParent = Directory.GetParent(outDataDir).FullName;
                string updateAllBatFile = Path.Combine(outDataDirParent, "updateAllBaselines.bat");

                using (StreamWriter sw = new StreamWriter(updateAllBatFile, true))
                {
                    sw.Write("cmd /c " + tfEditCommand);
                    sw.WriteLine(copyCommand);
                }


                diffMessage = " Generated file is different than the expected reference file.\r\n" +
                    "    Expected file:       " + referenceFileName + "\r\n" +
                    "    Newly generated file: " + generatedFileName + "\r\n" +
                    "\r\n ------------------- To diff these files, execute this ------------------\r\n\r\n    " +
                    tfDiffCommand +
                    "\r\n ---------------- To make this the new reference file, execute this ------------------\r\n\r\n    " +
                    tfEditCommand + "    " +
                    copyCommand + "\r\n\r\n" +
                    " ------------------- To update all baselines, that failed in the current run, execute following from command prompt ------------------\r\n\r\n" +
                    "\"" + updateAllBatFile + "\"" + "\r\n\r\n";


                return false;
            }

            diffMessage = string.Empty;
            return true;
        }


        // The generated code has a prefix from the code generator.
        // Unfortunately, it embeds the version number under which
        // the code was generated, so we need to strip away this
        // prefix and compare only the real parts of the code
        private static string StripAutoGenPrefix(string s, string lineCommentStart)
        {
            while (s.StartsWith(lineCommentStart))
            {
                int crlfPos = s.IndexOf("\r\n");
                s = s.Substring(crlfPos + 2);
            }
            return s;
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


        // Returns the full path to a deployment item file
        public static string GetTestFileName(string relativeDir, string baseName)
        {
            string testFileName = Path.Combine(GetOutputTestDataDir(relativeDir), baseName);

            Assert.IsTrue(File.Exists(testFileName), "Cannot locate " + testFileName +
                " which is a necessary input file to this test.\r\n" +
                " Be sure to add a [DeploymentItem] attribute for every new test file.");

            return testFileName;
        }

        // Returns the folder under which this test is currently running.
        public static string TestDir
        {
            get
            {
                return Path.GetDirectoryName(typeof(TestHelper).Assembly.Location);
            }
        }

        /// <summary>
        /// Normalizes a full folder path by stripping an ending separator if present.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static string NormalizedFolder(string folder)
        {
            int len = folder.Length;
            return (len > 0 && folder[len-1] == Path.DirectorySeparatorChar) ? folder.Substring(0, folder.Length - 1) : folder;
        }

        public static string GetOutputTestDataDir(string subDir)
        {
            string path = Path.GetFullPath(Path.Combine(TestDir, subDir));
            return path;
        }

        public static string GetProjectTestDataDir(string relativeDeployDir, string relativeTestDir)
        {
            string projectDir = GetProjectDir(relativeDeployDir);
            string path = Path.GetFullPath(Path.Combine(projectDir, @"..\..\test\Desktop\OpenRiaServices.Common.DomainServices.Test\Baselines"));
            path = Path.Combine(path, relativeTestDir);
            if (!Directory.Exists(path))
                Assert.Fail("Could not find expected project directory at " + path);
            return path;
        }

        public static string GetProjectDir(string subDir)
        {
            string projectPath, outputPath;
            GetProjectPaths(subDir, out projectPath, out outputPath);
            return Path.GetDirectoryName(projectPath);
        }

        public static string GetOutputDir(string subDir)
        {
            string projectPath, outputPath;
            GetProjectPaths(subDir, out projectPath, out outputPath);
            return Path.GetDirectoryName(outputPath);
        }

        public static string CreateUniqueTemporaryDirectory()
        {
            string dirName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirName);
            return dirName;
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
        /// Converts a collection of messages into a single string, each message separated by a newline
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static string MessagesAsString(IEnumerable<string> messages)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in messages)
                sb.AppendLine(s);
            return sb.ToString();
        }

        /// <summary>
        /// Asserts if any of a set of warnings is not present in the logger's warnings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="warningStrings"></param>
        public static void AssertContainsWarnings(ConsoleLogger logger, params string[] warningStrings)
        {
            foreach (string warning in warningStrings)
            {
                bool foundIt = false;

                foreach (string msg in logger.WarningMessages)
                {
                    if (warning.Equals(msg))
                    {
                        foundIt = true;
                        break;
                    }
                }
                Assert.IsTrue(foundIt, ("Expected to see warning\r\n  <" + warning + ">" + " but instead saw\r\n  " + MessagesAsString(logger.WarningMessages)));
            }
        }

        /// <summary>
        /// Asserts if any of the given error messages are not present in the loggers set of errors
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="errorStrings"></param>
        public static void AssertContainsErrors(ConsoleLogger logger, params string[] errorStrings)
        {
            foreach (string error in errorStrings)
            {
                bool foundIt = false;

                foreach (string msg in logger.ErrorMessages)
                {
                    if (error.Equals(msg))
                    {
                        foundIt = true;
                        break;
                    }
                }
                Assert.IsTrue(foundIt, ("Expected to see error\r\n  <" + error + ">" + " but instead saw\r\n  " + MessagesAsString(logger.ErrorMessages)));
            }
        }

        public static void AssertContainsErrorPackets(ConsoleLogger logger, params ConsoleLogger.LogPacket[] packets)
        {
            foreach (ConsoleLogger.LogPacket expectedPacket in packets)
            {
                bool foundIt = false;

                foreach (ConsoleLogger.LogPacket packet in logger.ErrorPackets)
                {
                    if (expectedPacket.Message.Equals(packet.Message) &&
                        expectedPacket.Subcategory.Equals(packet.Subcategory) &&
                        expectedPacket.HelpString.Equals(packet.HelpString) &&
                        expectedPacket.ErrorCode.Equals(packet.ErrorCode) &&
                        expectedPacket.File.Equals(packet.File) &&
                        expectedPacket.LineNumber == packet.LineNumber &&
                        expectedPacket.ColumnNumber == packet.ColumnNumber &&
                        expectedPacket.EndLineNumber == packet.EndLineNumber &&
                        expectedPacket.EndColumnNumber == packet.EndColumnNumber)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (!foundIt)
                {
                    string error = "Expected to see error\r\n  <" + packets.ToString() + ">" + " but instead saw:" + Environment.NewLine;
                    foreach (ConsoleLogger.LogPacket packet in logger.ErrorPackets)
                    {
                        error += packet.ToString() + Environment.NewLine;
                    }
                    Assert.Fail(error);
                }
            }
        }

        public static void AssertContainsWarningPackets(ConsoleLogger logger, params ConsoleLogger.LogPacket[] packets)
        {
            foreach (ConsoleLogger.LogPacket expectedPacket in packets)
            {
                bool foundIt = false;

                foreach (ConsoleLogger.LogPacket packet in logger.WarningPackets)
                {
                    if (expectedPacket.Message.Equals(packet.Message) &&
                        expectedPacket.Subcategory.Equals(packet.Subcategory) &&
                        expectedPacket.HelpString.Equals(packet.HelpString) &&
                        expectedPacket.ErrorCode.Equals(packet.ErrorCode) &&
                        expectedPacket.File.Equals(packet.File) &&
                        expectedPacket.LineNumber == packet.LineNumber &&
                        expectedPacket.ColumnNumber == packet.ColumnNumber &&
                        expectedPacket.EndLineNumber == packet.EndLineNumber &&
                        expectedPacket.EndColumnNumber == packet.EndColumnNumber)
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (!foundIt)
                {
                    string error = "Expected to see warning\r\n  <" + packets.ToString() + ">" + " but instead saw:" + Environment.NewLine;
                    foreach (ConsoleLogger.LogPacket packet in logger.WarningPackets)
                    {
                        error += packet.ToString() + Environment.NewLine;
                    }
                    Assert.Fail(error);
                }
            }
        }

        /// <summary>
        /// Asserts if any of the given error messages are not present in the loggers set of information messages
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="errorStrings"></param>
        public static void AssertContainsMessages(ConsoleLogger logger, params string[] messageStrings)
        {
            foreach (string message in messageStrings)
            {
                bool foundIt = false;

                foreach (string msg in logger.InfoMessages)
                {
                    if (message.Equals(msg))
                    {
                        foundIt = true;
                        break;
                    }
                }
                Assert.IsTrue(foundIt, ("Expected to see message\r\n  <" + message + ">" + " but instead saw\r\n  " + MessagesAsString(logger.InfoMessages)));
            }
        }

        /// <summary>
        /// Asserts if any of the given error messages are not present in the loggers set of errors
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="errorStrings"></param>
        public static void AssertHasErrorThatStartsWith(ConsoleLogger logger, params string[] errorStrings)
        {
            foreach (string error in errorStrings)
            {
                bool foundIt = false;

                foreach (string msg in logger.ErrorMessages)
                {
                    if (msg.StartsWith(error))
                    {
                        foundIt = true;
                        break;
                    }
                }
                Assert.IsTrue(foundIt, ("Expected to see error\r\n  <" + error + ">" + " but instead saw\r\n  " + MessagesAsString(logger.ErrorMessages)));
            }
        }

        /// <summary>
        /// Asserts an error and warning free console logger
        /// </summary>
        /// <param name="logger"></param>
        public static void AssertNoErrorsOrWarnings(ConsoleLogger logger)
        {
            if (logger.ErrorMessages.Count() == 0 && logger.WarningMessages.Count() == 0)
                return;

            StringBuilder sb = new StringBuilder();
            foreach (string msg in logger.ErrorMessages)
                sb.AppendLine("Error: " + msg);
            foreach (string msg in logger.WarningMessages)
                sb.AppendLine("Warning: " + msg);

            Assert.Fail("Did not expect any errors or warnings:\r\n  " + sb.ToString());
        }

        public static void AssertCodeGenSuccess(string generatedCode, ConsoleLogger logger)
        {
            AssertNoErrorsOrWarnings(logger);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Expected generated code");
        }

        public static void AssertCodeGenFailure(string generatedCode, ConsoleLogger logger, params string[] errorStrings)
        {
            AssertContainsErrors(logger, errorStrings);
            Assert.IsTrue(string.IsNullOrEmpty(generatedCode), "Expected empty generated code");
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
        /// Assert that all the files named by shortNames exist in the files collection -- and no others
        /// </summary>
        /// <param name="files">list of files to check</param>
        /// <param name="projectPath">project owning the short named files</param>
        /// <param name="shortNames">collection of file short names that must be in list</param>
        public static void AssertContainsOnlyTheseFiles(IEnumerable<string> files, string projectPath, string[] shortNames)
        {
            Assert.AreEqual(shortNames.Length, files.Count());
            AssertContainsAtLeastTheseFiles(files, projectPath, shortNames);
        }

        /// <summary>
        /// Assert that all the files named by shortNames exist in the files collection
        /// </summary>
        /// <param name="files">list of files to check</param>
        /// <param name="projectPath">project owning the short named files</param>
        /// <param name="shortNames">collection of file short names that must be in list</param>
        public static void AssertContainsAtLeastTheseFiles(IEnumerable<string> files, string projectPath, string[] shortNames)
        {
            foreach (string shortName in shortNames)
            {
                string fullName = Path.Combine(Path.GetDirectoryName(projectPath), shortName);
                bool foundIt = false;
                foreach (string file in files)
                {
                    if (file.Equals(fullName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (!foundIt)
                {
                    string allFiles = string.Empty;
                    foreach (string file in files)
                        allFiles += ("\r\n" + file);

                    Assert.Fail("Expected to find " + fullName + " in list of files, but saw instead:" + allFiles);
                }
            }
        }

        internal static ClientCodeGenerationOptions CreateMockCodeGenContext(string language, bool useFullTypeNames)
        {
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = language,
                ClientProjectPath = "MockProject.csproj",
                UseFullTypeNames = useFullTypeNames
            };
            return options;
        }

        // Creates a MockSharedCodeService with some common files and types considered as shared
        internal static MockSharedCodeService CreateCommonMockSharedCodeService()
        {
            IEnumerable<Type> sharedTypes = new Type[] { 
                // Known test types
                typeof(Mock_CG_Attr_Entity_StringLength_ResourceType),
                typeof(Mock_CG_DisplayAttr_Shared_ResourceType),
                typeof(DifferentNamespace.Mock_CG_DisplayAttr_Shared_ResourceType_DifferentNamespace),
                typeof(TestDomainServices.SharedResource)
            };
            IEnumerable<MethodBase> sharedMethods = new MethodBase[] { 
                // Known test methods.
                typeof(Mock_CG_Attr_Entity_StringLength_ResourceType).GetProperty("TheResource").GetGetMethod(),
                typeof(TestDomainServices.SharedResource).GetProperty("String").GetGetMethod()
            };
            IEnumerable<string> sharedFiles = new string[] { };
            MockSharedCodeService mockSharedCodeService = new MockSharedCodeService(sharedTypes, sharedMethods, sharedFiles);

            return mockSharedCodeService;
        }

        // Creates a mock code generation host that uses the given logger and shared type service
        internal static MockCodeGenerationHost CreateMockCodeGenerationHost(ILoggingService logger, ISharedCodeService sharedCodeService)
        {
            MockCodeGenerationHost host = new MockCodeGenerationHost(logger, sharedCodeService);
            return host;
        }

        // Locates the code generator registered to work with the language for the given options
        internal static IDomainServiceClientCodeGenerator CreateCodeGenerator(ICodeGenerationHost host, ClientCodeGenerationOptions options)
        {
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                IDomainServiceClientCodeGenerator generator = dispatcher.FindCodeGenerator(host, options, /*compositionAssemblies*/ null, /*codeGeneratorName*/ null);
                return generator;
            }
        }

        // Invokes the code generator discovered via the host and options
        internal static string GenerateCode(ICodeGenerationHost host, ClientCodeGenerationOptions options, IEnumerable<Type> domainServiceTypes)
        {
            IDomainServiceClientCodeGenerator generator = CreateCodeGenerator(host, options);
            DomainServiceCatalog catalog = new DomainServiceCatalog(domainServiceTypes, host as ILogger);
            return generator.GenerateCode(host, catalog.DomainServiceDescriptions, options);
        }

        internal static string GenerateCode(string language, Type domainServiceType, ILoggingService logger)
        {
            ClientCodeGenerationOptions options = CreateMockCodeGenContext(language, false);
            ICodeGenerationHost host = CreateMockCodeGenerationHost(logger, null);
            return GenerateCode(host, options, new Type[] { domainServiceType });
        }

        internal static string GenerateCodeAssertSuccess(string language, Type domainServiceType)
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = CreateMockCodeGenContext(language, false);
            ICodeGenerationHost host = CreateMockCodeGenerationHost(logger, null);
            string generatedCode = GenerateCode(host, options, new Type[] { domainServiceType });
            TestHelper.AssertCodeGenSuccess(generatedCode, logger);
            return generatedCode;
        }


        internal static void GenerateCodeAssertFailure(string language, Type domainServiceType, params string[] errors)
        {
            TestHelper.GenerateCodeAssertFailure(language, new Type[] { domainServiceType }, errors);
        }

        internal static void GenerateCodeAssertFailure(string language, IEnumerable<Type> domainServiceTypes, params string[] errors)
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = CreateMockCodeGenContext(language, false);
            ICodeGenerationHost host = CreateMockCodeGenerationHost(logger, null);
            string generatedCode = GenerateCode(host, options, domainServiceTypes );
            TestHelper.AssertCodeGenFailure(generatedCode, logger, errors);
        }

        internal static string GenerateCodeAssertWarnings(string language, Type domainServiceType, params string[] warnings)
        {
            ConsoleLogger logger = new ConsoleLogger();
            ClientCodeGenerationOptions options = CreateMockCodeGenContext(language, false);
            ICodeGenerationHost host = CreateMockCodeGenerationHost(logger, null);
            string generatedCode = GenerateCode(host, options, new Type[] { domainServiceType });
            TestHelper.AssertContainsWarnings(logger, warnings);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Expected code to generate with warnings");
            return generatedCode;
        }

        internal static string GenerateCode(string language, IEnumerable<Type> domainServiceTypes, ILoggingService logger, ISharedCodeService typeService)
        {
            ClientCodeGenerationOptions options = CreateMockCodeGenContext(language, false);
            ICodeGenerationHost host = CreateMockCodeGenerationHost(logger, typeService);
            return GenerateCode(host, options, domainServiceTypes);
        }

        internal static string GenerateCodeAssertSuccess(string language, IEnumerable<Type> domainServiceTypes, ConsoleLogger logger, ISharedCodeService typeService, bool useFullNames)
        {
            ClientCodeGenerationOptions options = CreateMockCodeGenContext(language, useFullNames);
            ICodeGenerationHost host = CreateMockCodeGenerationHost(logger, typeService);
            string generatedCode = GenerateCode(host, options, domainServiceTypes);
            TestHelper.AssertCodeGenSuccess(generatedCode,  ((MockCodeGenerationHost)host).LoggingService as ConsoleLogger);
            return generatedCode;
        }

        internal static string GenerateCodeAssertSuccess(string language, IEnumerable<Type> domainServiceTypes, ConsoleLogger logger, ISharedCodeService typeService)
        {
            return TestHelper.GenerateCodeAssertSuccess(language, domainServiceTypes, logger, typeService, false);
        }

        internal static string GenerateCodeAssertSuccess(string language, IEnumerable<Type> domainServiceTypes, ISharedCodeService typeService)
        {
            return TestHelper.GenerateCodeAssertSuccess(language, domainServiceTypes, new ConsoleLogger(), typeService);
        }

        /// <summary>
        /// Validates code gen by comparing it against a file containing the expected output.
        /// </summary>
        /// <remarks>
        /// If no language is specified, C# and VB are both tested.
        /// </remarks>
        /// <param name="options">The options specifying the type of validation to perform</param>
        internal static void ValidateCodeGen(CodeGenValidationOptions options)
        {
            if (string.IsNullOrEmpty(options.Language))
            {
                string errorMessage = string.Empty;

                options.FailOnDiff = false;
                options.Language = "C#";
                errorMessage += TestHelper.ValidateLanguageCodeGen(options);

                options.Language = "VB";
                errorMessage += TestHelper.ValidateLanguageCodeGen(options);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Assert.Fail(errorMessage);
                }
            }
            else
            {
                TestHelper.ValidateLanguageCodeGen(options);
            }
        }


        /// <summary>
        /// Validates code gen for a specific language by comparing it against a file containing the expected output.
        /// </summary>
        /// <param name="codeGenOptions">The options specifying the type of validation to perform</param>
        /// <returns>A command that updates the comparison file</returns>
        internal static string ValidateLanguageCodeGen(CodeGenValidationOptions codeGenOptions)
        {
            Assert.IsFalse(string.IsNullOrEmpty(codeGenOptions.Language));

            string outDataDir = TestHelper.GetOutputTestDataDir(codeGenOptions.RelativeDeployDir);
            string extension = TestHelper.ExtensionFromLanguage(codeGenOptions.Language);
            string diffMessage = string.Empty;

            // Compose the abs path to where the test file got deployed by MSTest
            string referenceFileName = TestHelper.GetTestFileName(codeGenOptions.RelativeDeployDir, codeGenOptions.BaseReferenceFileName + extension);

            Assert.IsTrue(File.Exists(referenceFileName), "Cannot find reference file " + referenceFileName);
            string generatedCode = string.Empty;

            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = codeGenOptions.Language,
                ClientRootNamespace = codeGenOptions.RootNamespace,
                ClientProjectPath = "MockProject.proj",
                IsApplicationContextGenerationEnabled = codeGenOptions.GenerateApplicationContexts,
                UseFullTypeNames = codeGenOptions.UseFullTypeNames,
                ClientProjectTargetPlatform =  TargetPlatform.Silverlight
            };

            MockCodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(codeGenOptions.Logger, codeGenOptions.SharedCodeService);
            ILogger logger = host as ILogger;
            DomainServiceCatalog catalog = new DomainServiceCatalog(codeGenOptions.DomainServiceTypes, logger);
            IDomainServiceClientCodeGenerator generator;
            using (ClientCodeGenerationDispatcher dispatcher = new ClientCodeGenerationDispatcher())
            {
                generator = dispatcher.FindCodeGenerator(host, options, /*compositionAssemblies*/ null, /*codeGeneratorName*/ null);
            }
            Assert.IsNotNull(generator, "Failed to find a code generator");
            generatedCode = generator.GenerateCode(host, catalog.DomainServiceDescriptions, options);

            ConsoleLogger consoleLogger = logger as ConsoleLogger;
            string errors = consoleLogger == null ? "" : consoleLogger.Errors;
            Assert.IsTrue(generatedCode.Length > 0, "No code was generated: " + errors);

            // Dump the generated code into a file for comparison
            bool isCSharp = options.Language.Equals("C#", StringComparison.InvariantCultureIgnoreCase);
            string generatedFileName = Path.Combine(outDataDir, Path.GetFileName(referenceFileName) + ".testgen");
            File.WriteAllText(generatedFileName, generatedCode);

            // TODO: (ron M3) Solve inability to get right MSBuild after checkin
            // First see if we compile
            List<string> referenceAssemblies = CompilerHelper.GetSilverlightClientAssemblies(codeGenOptions.RelativeDeployDir);
            List<string> files = new List<string>();

            files.Add(generatedFileName);

            // Unconditionally force generation of Xml doc comments to catch errors
            string documentationFile = Path.GetTempFileName();

            try
            {
                if (isCSharp)
                {
                    files.AddRange(codeGenOptions.SharedFiles.Where(sharedFile => Path.GetExtension(sharedFile).Equals(".cs")));
                    CompilerHelper.CompileCSharpSourceFromFiles(files, referenceAssemblies, documentationFile);
                }
                else
                {
                    files.AddRange(codeGenOptions.SharedFiles.Where(sharedFile => Path.GetExtension(sharedFile).Equals(".vb")));
                    CompilerHelper.CompileVisualBasicSourceFromFiles(files, referenceAssemblies, options.ClientRootNamespace, documentationFile);
                }
            }
            finally
            {
                File.Delete(documentationFile);
            }

            // Do the diff
            if (codeGenOptions.FailOnDiff)
            {
                TestHelper.ValidateFilesEqual(codeGenOptions.RelativeTestDir, codeGenOptions.RelativeDeployDir, generatedFileName, referenceFileName, codeGenOptions.Language);
            }
            else
            {
                TestHelper.FilesMatch(codeGenOptions.RelativeTestDir, codeGenOptions.RelativeDeployDir, generatedFileName, referenceFileName, codeGenOptions.Language, out diffMessage);
            }

            return diffMessage;
        }

        /// <summary>
        /// Options specifying the type of code gen validation to perform.
        /// </summary>
        internal class CodeGenValidationOptions
        {
            private readonly List<Type> _sharedTypes = new List<Type>();
            private readonly List<MethodBase> _sharedMethods = new List<MethodBase>();
            public bool FailOnDiff { get; set; }
            public string RelativeTestDir { get; set; }
            public string RelativeDeployDir { get; set; }
            public string BaseReferenceFileName { get; set; }
            public string Language { get; set; }
            public string RootNamespace { get; set; }
            public IEnumerable<Type> DomainServiceTypes { get; private set; }
            public IEnumerable<string> SharedFiles { get; private set; }
            public ILoggingService Logger { get; set; }
            public bool GenerateApplicationContexts { get; set; }
            public bool UseFullTypeNames { get; private set; }

            public CodeGenValidationOptions(string relativeTestDir, string relativeDeployDir, string baseReferenceFileName,
                Type domainServiceType, IEnumerable<string> sharedFiles, bool useFullTypeNames)
                : this(relativeTestDir, relativeDeployDir, baseReferenceFileName, domainServiceType, null, sharedFiles, useFullTypeNames)
            {
            }

            public CodeGenValidationOptions(string relativeTestDir, string relativeDeployDir, string baseReferenceFileName,
                Type domainServiceType, string language, IEnumerable<string> sharedFiles, bool useFullTypeNames)
                : this(relativeTestDir, relativeDeployDir, baseReferenceFileName, new Type[] { domainServiceType }, language, sharedFiles, useFullTypeNames)
            {
            }

            public CodeGenValidationOptions(string relativeTestDir, string relativeDeployDir, string baseReferenceFileName,
                IEnumerable<Type> domainServiceTypes, string language, IEnumerable<string> sharedFiles, bool useFullTypeNames)
                : this(relativeTestDir, relativeDeployDir, baseReferenceFileName, domainServiceTypes, language, sharedFiles, null, useFullTypeNames)
            {
            }

            public CodeGenValidationOptions(string relativeTestDir, string relativeDeployDir, string baseReferenceFileName,
                IEnumerable<Type> domainServiceTypes, string language, IEnumerable<string> sharedFiles, string rootNamespace, bool useFullTypeNames)
                : this(relativeTestDir, relativeDeployDir, baseReferenceFileName, domainServiceTypes, language, sharedFiles, rootNamespace, new ConsoleLogger(), false, useFullTypeNames)
            {
            }

            public CodeGenValidationOptions(string relativeTestDir, string relativeDeployDir, string baseReferenceFileName,
                IEnumerable<Type> domainServiceTypes, string language, IEnumerable<string> sharedFiles, string rootNamespace, ILoggingService logger, bool generateApplicationContexts, bool useFullTypeNames)
            {
                this.FailOnDiff = true;
                this.RelativeTestDir = relativeTestDir;
                this.RelativeDeployDir = relativeDeployDir;
                this.BaseReferenceFileName = baseReferenceFileName;
                this.DomainServiceTypes = domainServiceTypes;
                this.Language = language;
                this.SharedFiles = sharedFiles;
                this.RootNamespace = rootNamespace;
                this.Logger = logger;
                this.GenerateApplicationContexts = generateApplicationContexts;
                this.UseFullTypeNames = useFullTypeNames;
            }

            public ISharedCodeService SharedCodeService
            {
                get { return new MockSharedCodeService(this._sharedTypes, this._sharedMethods, this.SharedFiles); }
            }

            // Adds the given Type to the list to consider shared during code gen
            public void AddSharedType(Type t)
            {
                this._sharedTypes.Add(t);
            }

            // Adds the given method to the list to consider shared during code gen
            public void AddSharedMethod(MethodBase method)
            {
                this._sharedMethods.Add(method);
            }
        }
    }
}
