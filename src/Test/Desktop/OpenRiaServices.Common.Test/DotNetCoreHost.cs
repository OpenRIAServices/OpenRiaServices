using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Common.Test
{
    public class DotNetCoreHost
    {

        Process _dotnetProcess;
        string siteName;

        public bool Start(string path)
        {

            string siteName = Path.GetFileName(path);

            // only start webserver if it is not already running
            if (!ShouldStartNewWebserverProcess(siteName))
            {
                return false;
            }

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "dotnet";
            startInfo.WorkingDirectory = path;
            //startInfo.Arguments = $"--project={path}";
            startInfo.Arguments = "run";
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            _dotnetProcess = Process.Start(startInfo);

            string str, last = string.Empty;
            while ((str = _dotnetProcess.StandardOutput.ReadLine()) != null)
            {
                if (str.Contains("Content root path"))
                {
                    new Thread(ProcessIISStandardOutput)
                            .Start();

                    return true;
                }
                last = str;
            }

            throw new Exception("Failed to start dotnet core app: " + last);

        }


        // Read and discard output from the web server so that 
        // it does not fill upp the standard output buffer and 
        // pause for it to be read
        private void ProcessIISStandardOutput()
        {
            try
            {
                string str;
                while (!_dotnetProcess.HasExited &&
                    (str = _dotnetProcess.StandardOutput.ReadLine()) != null)
                {

                }
            }
            catch (Exception)
            {
                if (!_dotnetProcess.HasExited)
                    throw;
            }
        }

        public void Stop()
        {
            //var parent = GetParent(_dotnetProcess);
            StopProcess(_dotnetProcess);
            //if (parent != null)
            //    StopProcess(parent);
        }

        private static void StopProcess(Process proc)
        {
            if (proc != null)
            {
                if (!proc.HasExited)
                {
                    try
                    {
                        proc.WaitForExit(1 * 1000); // wait 1 secs.
                        if (!proc.HasExited)
                        {
                            proc.Kill();
                            proc.WaitForExit(60 * 1000); // wait 60 secs.
                        }

                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Unable to kill local web server process : {ex.Message}");
                    }
                }

                proc.Dispose();
                proc = null;
            }
        }
        
        private static bool ShouldStartNewWebserverProcess(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                // Process was left from another test session.
                // Kill it so we can start a new with redirected intput/output
                //var parent = GetParent(process);
                StopProcess(process);
                //if (parent != null)
                //    StopProcess(parent);
                return true;
            }
            return true;
        }

        //private static Process GetParent(Process process)
        //{
        //    return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        //}

        //private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        //{
        //    var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
        //    return Process.GetProcessById((int)parentId.NextValue());
        //}

        //private static string FindIndexedProcessName(int pid)
        //{
        //    var processName = Process.GetProcessById(pid).ProcessName;
        //    var processesByName = Process.GetProcessesByName(processName);
        //    string processIndexdName = null;

        //    for (var index = 0; index < processesByName.Length; index++)
        //    {
        //        processIndexdName = index == 0 ? processName : processName + "#" + index;
        //        var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
        //        if ((int)processId.NextValue() == pid)
        //        {
        //            return processIndexdName;
        //        }
        //    }

        //    return processIndexdName;
        //}
    }
}
