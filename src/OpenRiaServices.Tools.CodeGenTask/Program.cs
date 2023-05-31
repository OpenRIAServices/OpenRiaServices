﻿using System;
using System.IO;
using OpenRiaServices.Tools;
using System.Linq;
using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class CodeGenTask
{
    static int Main(string[] args) // Take in params to setup the build here
    {
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

        var success = false;
        SharedCodeServiceParameters sharedCodeServiceParameters;
        ClientCodeGenerationOptions clientCodeGenerationOption;

        rootCommand.SetHandler((
                clientCodeGenerationOptionPath,
                sharedCodeServicePath,
                codeGeneratorName,
                generatedFileName
            ) =>
        {
            try
            {

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


                RiaClientFilesTaskHelpers.CodeGenForNet6(generatedFileName, clientCodeGenerationOption, log, sharedCodeServiceParameters, codeGeneratorName);
                if (log.HasLoggedErrors)
                {
                    throw log.Errors;
                }
                Console.WriteLine("Code generation succeeded");
                success = true;

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
        },
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
            _errors.Add(message);
        }

        public void LogError(string message)
        {
            _errors.Add(message);
        }

        public void LogException(Exception ex)
        {
            _exceptions.Add(ex.ToString());
        }

        public void LogMessage(string message)
        {
            _messages.Add(message);
        }

        public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            _warnings.Add(message);
        }

        public void LogWarning(string message)
        {
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