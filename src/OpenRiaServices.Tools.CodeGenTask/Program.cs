﻿using System;
using System.IO;
using OpenRiaServices.Tools;
using System.Linq;
using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

class Program
{
    static int Main(string[] args) // Take in params to setup the build here
    {
        Console.WriteLine($"OpenRiaServices CodeGen running on {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

        // TODO: Coonsider setting "working diretory" (when launching exe? or here) to match old behaviour 

        // TODO: Remove dependency on MSBuild and then remove any PackageReferences to MSBuild and MSBuildLocator
        // * This will require splitting "OpenRiaServices.Tools" into 2 separate projects, one with MSbuild tasks and one without (just code generation)
        // For now register the most recent version of MSBuild
        Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(
           instance => instance.Version).First());

        var clientCodeGenerationOptionPath = new Option<string>(name: "--clientCodeGenerationOptionPath")
        {
            IsRequired = true
        };

        var sharedCodeServicePath = new Option<string>(name: "--sharedCodeServiceParameterPath")
        {
            IsRequired = true
        };

        var codeGeneratorName = new Option<string>(name: "--codeGeneratorName")
        {
            IsRequired = true
        };

        var generatedFileName = new Option<string>(name: "--generatedFileName")
        {
            IsRequired = true
        };

        var rootCommand = new RootCommand("Sample app for running code generation")
        {
            clientCodeGenerationOptionPath,
            sharedCodeServicePath,
            codeGeneratorName,
            generatedFileName
        };

        bool success = false;

        rootCommand.SetHandler((clientCodeGenerationOptionPath, sharedCodeServicePath, codeGeneratorName, generatedFileName)
            => RunCodeGenForNet6(clientCodeGenerationOptionPath, sharedCodeServicePath, codeGeneratorName, generatedFileName, out success),
            clientCodeGenerationOptionPath,
            sharedCodeServicePath,
            codeGeneratorName,
            generatedFileName);

        rootCommand.Invoke(args);
        if (success)
            return 0;
        else
            return -1;
    }

    private static void RunCodeGenForNet6(string clientCodeGenerationOptionPath, string sharedCodeServicePath, string codeGeneratorName, string generatedFileName, out bool success)
    {
        success = false;
        SharedCodeServiceParameters sharedCodeServiceParameters;
        ClientCodeGenerationOptions clientCodeGenerationOption;

        try
        {
#pragma warning disable SYSLIB0011
            using (Stream stream = File.Open(clientCodeGenerationOptionPath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                clientCodeGenerationOption = (ClientCodeGenerationOptions)binaryFormatter.Deserialize(stream);
            }

            using (Stream stream = File.Open(sharedCodeServicePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                sharedCodeServiceParameters = (SharedCodeServiceParameters)binaryFormatter.Deserialize(stream);
            }
            var log = new Logger();

            log.LogMessage(Environment.CommandLine);

            log.LogMessage("SymbolSearchPaths: " + string.Join(",", sharedCodeServiceParameters.SymbolSearchPaths));
            log.LogMessage("ServerAssemblies: " + string.Join(",", sharedCodeServiceParameters.ServerAssemblies));
            log.LogMessage("ClientAssemblies: " + string.Join(",", sharedCodeServiceParameters.ClientAssemblies));
            log.LogMessage("SharedSourceFiles: " + string.Join(",", sharedCodeServiceParameters.SharedSourceFiles));

            log.LogMessage("Language: " + clientCodeGenerationOption.Language);
            log.LogMessage("ClientProjectPath: " + clientCodeGenerationOption.ClientProjectPath);
            log.LogMessage("ServerProjectPath: " + clientCodeGenerationOption.ServerProjectPath);

            log.LogMessage("CodeGeneratorName" + codeGeneratorName);
            log.LogMessage("GeneratedFileName" + generatedFileName);

            log.DumpMessages();

            try
            {
                SetupAppConfig(clientCodeGenerationOption);

                RiaClientFilesTaskHelpers.CodeGenForNet6(generatedFileName, clientCodeGenerationOption, log, sharedCodeServiceParameters, codeGeneratorName);
            }
            finally
            {
                log.DumpMessages();
            }
            if (log.HasLoggedErrors)
            {
                throw log.Errors;
            }
            Console.WriteLine("Code generation succeeded");
            success = true;
        }
        catch (AggregateException aggregatedException)
        {
            using (StreamWriter writer = new StreamWriter("CodeGenLog.txt", true))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine();

                foreach (var innerException in aggregatedException.InnerExceptions)
                {
                    var exceptionToLog = innerException;
                    while (exceptionToLog != null)
                    {
                        writer.WriteLine(exceptionToLog.GetType().FullName);
                        writer.WriteLine("Message : " + exceptionToLog.Message);
                        writer.WriteLine("StackTrace : " + exceptionToLog.StackTrace);

                        exceptionToLog = exceptionToLog.InnerException;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            using (StreamWriter writer = new StreamWriter("CodeGenLog.txt", true))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine();

                while (ex != null)
                {
                    writer.WriteLine(ex.GetType().FullName);
                    writer.WriteLine("Message : " + ex.Message);
                    writer.WriteLine("StackTrace : " + ex.StackTrace);

                    ex = ex.InnerException;
                }
            }
        }
    }

    // TODO: Find app.config/web.config https://stackoverflow.com/questions/4738/using-configurationmanager-to-load-config-from-an-arbitrary-location/14246260#14246260
    // Ensure this code works (EF6 DbDomainContext (or ex EfCore) using ConfigurationManager API to get connection string should work)

    // Note: This just looks for "app.config" in the root,
    // we might want to be smarter when searching for them.
    // Note: Prefer web.config if running on NETFRAMEWORK
    // Note we probably want to change this to a recursive search
    // (using glob pattern to ignore bin/obj folders)
    private static void SetupAppConfig(ClientCodeGenerationOptions clientCodeGenerationOption)
    {
        var serverProjectPath = Path.GetDirectoryName(clientCodeGenerationOption.ServerProjectPath);

        var configFiles = Directory.GetFiles(serverProjectPath, "*.config");
        var configFile = configFiles.FirstOrDefault(f => f.EndsWith("app.config", StringComparison.InvariantCultureIgnoreCase));
        if (configFile != null)
        {
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFile);
        }
    }

    public class Logger : ILoggingService
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _messages = new List<string>();
        private readonly List<string> _exceptions = new List<string>();
        private readonly List<string> _warnings = new List<string>();
        public bool HasLoggedErrors => _errors.Count > 0;

        public AggregateException Errors => new AggregateException(_errors.Select(e => new Exception(e)));

        public void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            Console.WriteLine($"ERROR: {message}, errorCode: {errorCode} file: {file}:{lineNumber}-{endColumnNumber}");
            _errors.Add(message);
        }

        public void LogError(string message)
        {
            Console.WriteLine($"ERROR: {message}");
            _errors.Add(message);
        }

        public void LogException(Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}\n\t {ex}");
            _exceptions.Add(ex.ToString());
        }

        public void LogMessage(string message)
        {
            Console.WriteLine($"Info: {message}");
            _messages.Add(message);
        }

        public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            Console.WriteLine($"WARN: {message}, errorCode: {errorCode} file: {file}:{lineNumber}-{endColumnNumber}");
            _warnings.Add(message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"WARN: {message}");
            _warnings.Add($"{message}");
        }

        public void DumpMessages()
        {
            using (StreamWriter writer = new StreamWriter("CodeGenLog_Messages.txt", true))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine();

                foreach (var message in _messages)
                {
                    writer.WriteLine($"{message}");
                }
            }
        }
    }
}
