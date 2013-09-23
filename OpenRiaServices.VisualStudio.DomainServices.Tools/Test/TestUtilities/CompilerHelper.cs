using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using OpenRiaServices.DomainServices.Tools.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Microsoft.AppFx.UnitTests.Setup.Utilities
{
    internal class CompilerHelper
    {
        /// <summary>
        /// Invokes CSC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="lang"></param>
        public static bool CompileCSharpSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies)
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            foreach (string f in files)
                sources.Add(new TaskItem(f));

            List<ITaskItem> references = new List<ITaskItem>();
            foreach (string s in referenceAssemblies)
                references.Add(new TaskItem(s));

            Csc csc = new Csc();
            MockBuildEngine buildEngine = new MockBuildEngine();
            csc.BuildEngine = buildEngine;  // needed before task can log

            csc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it silverlight
            csc.NoConfig = true;        // don't load the csc.rsp file to get references
            csc.TargetType = "library";
            csc.Sources = sources.ToArray();
            csc.References = references.ToArray();

            bool result = false;
            try
            {
                result = csc.Execute();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking CSC task on " + sources[0].ItemSpec + ":\r\n" + ex);
            }

            if (!result)
            {
                string sourceList = string.Empty;
                foreach (TaskItem t in sources)
                {
                    sourceList += "    " + t.ItemSpec + "\r\n";
                }
                Assert.Fail("CSC failed to compile sources:\r\n" + sourceList + "\r\n" + buildEngine.ConsoleLogger.Errors + "\r\n\r\nReference assemblies were:\r\n" + ReferenceAssembliesAsString(referenceAssemblies));
            }
            return result;
        }

        /// <summary>
        /// Invokes VBC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        public static bool CompileVisualBasicSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string rootNamespace)
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            foreach (string f in files)
                sources.Add(new TaskItem(f));

            List<ITaskItem> references = new List<ITaskItem>();
            foreach (string s in referenceAssemblies)
            {
                // VB cannot accept mscorlib in the references
                if (s.IndexOf("mscorlib", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    references.Add(new TaskItem(s));
                }
            }

            Vbc vbc = new Vbc();
            MockBuildEngine buildEngine = new MockBuildEngine();
            vbc.BuildEngine = buildEngine;  // needed before task can log

            vbc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it silverlight
            vbc.NoConfig = true;        // don't load the vbc.rsp file to get references
            vbc.TargetType = "library";
            vbc.Sources = sources.ToArray();
            vbc.References = references.ToArray();
            //$$$ vbc.SdkPath = GetSilverlightPath();
            vbc.RootNamespace = rootNamespace;

            bool result = false;
            try
            {
                result = vbc.Execute();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking VBC task on " + sources[0].ItemSpec + ":\r\n" + ex);
            }

            if (!result)
            {
                string sourceList = string.Empty;
                foreach (TaskItem t in sources)
                {
                    sourceList += "    " + t.ItemSpec + "\r\n";
                }

                Assert.Fail("VBC failed to compile sources:\r\n" + sourceList + "\r\n" + buildEngine.ConsoleLogger.Errors + "\r\n\r\nReference assemblies were:\r\n" + ReferenceAssembliesAsString(referenceAssemblies));
            }
            return result;
        }

        private static string ReferenceAssembliesAsString(IEnumerable<string> referenceAssemblies)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string assemblyName in referenceAssemblies)
            {
                sb.AppendLine("  " + assemblyName);
            }
            return sb.ToString();
        }
    }
}
