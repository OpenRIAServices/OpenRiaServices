using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Security;
using System.Threading;

namespace OpenRiaServices.Common.Test
{
    public class IISExpressWebserver
    {
        Process _iisProcess;

        public bool Start(string path, int port)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            string siteName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(siteName))
                throw new ArgumentException("path cannot end with directory separator", nameof(path));
            if (_iisProcess != null)
                throw new InvalidOperationException("server already started");

            string iisexpress = Environment.ExpandEnvironmentVariables(@"%programfiles%\IIS Express\iisexpress.exe");

            string portNumberText = port.ToString(CultureInfo.InvariantCulture);
            string arguments = $"/port:{portNumberText} /path:\"{path}\"";

            // only start webserver if it is not already running
            if (!ShouldStartNewWebserverProcess(arguments, $"/site:\"{siteName}\""))
            {
                return false;
            }

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = iisexpress;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            _iisProcess = Process.Start(startInfo);

            string str;
            while ((str = _iisProcess.StandardOutput.ReadLine()) != null)
            {
                if (str.Contains("IIS Express is running")
                    || str.Contains("'Q'"))
                {
                    new Thread(ProcessIISStandardOutput)
                            .Start();

                    return true;
                }

            }

            throw new Exception("Failed to start IIS express");
        }

        // Read and discard output from the web server so that 
        // it does not fill upp the standard output buffer and 
        // pause for it to be read
        private void ProcessIISStandardOutput()
        {
            try
            {
                string str;
                while (!_iisProcess.HasExited &&
                    (str = _iisProcess.StandardOutput.ReadLine()) != null)
                {

                }
            }
            catch (Exception)
            {
                if (!_iisProcess.HasExited)
                    throw;
            }
        }

        /// <summary>Performs cleanup and ensures that there are no active web servers.</summary>
        public void Stop()
        {
            if (_iisProcess != null)
            {
                if (!_iisProcess.HasExited)
                {
                    try
                    {
                        _iisProcess.StandardInput.WriteLine("Q");
                        _iisProcess.WaitForExit(1 * 1000); // wait 1 secs.

                        if (!_iisProcess.HasExited)
                        {
                            _iisProcess.Kill();
                            _iisProcess.WaitForExit(60 * 1000); // wait 60 secs.
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        Trace.WriteLine("Unable to kill local web server process.");
                    }
                }

                _iisProcess.Dispose();
                _iisProcess = null;
            }
        }

        [SecuritySafeCritical]
        private static string GetCommandLine(Process processs)
        {
            var commandLineSearcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processs.Id}");
            foreach (ManagementObject commandLineObject in commandLineSearcher.Get())
            {
                return commandLineObject["CommandLine"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool ShouldStartNewWebserverProcess(string commandlineArguments, string dontKillCommandLine)
        {
            foreach (var process in Process.GetProcessesByName("iisexpress"))
            {
                var commandline = GetCommandLine(process);
                if (commandline.Contains(commandlineArguments))
                {
                    // Process was left from another test session.
                    // Kill it so we can start a new with redirected intput/output
                    process.Kill();
                    process.WaitForExit();
                    return true;
                }
                else if (commandline.Contains(dontKillCommandLine))
                {
                    // return true and do nothing if different command line (started by user/vs)
                    return false;
                }
            }
            return true;
        }
    }
}
