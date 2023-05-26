using System;
//using Microsoft.Build.Utilities;
using System.IO;
using OpenRiaServices.Tools;
using System.Linq;
using System.CommandLine;
using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;

public class CodeGenTask
{    
    static int Main(string[] args) // Take in params to setup the build here
     {
        var language = new Option<string>(name: "--Language")
        {
            IsRequired = true
        };

        var ClientRootNamespace = new Option<string>(name: "--ClientRootNamespace")
        {
            IsRequired = true
        };
        var ServerRootNamespace = new Option<string>(name: "--ServerRootNamespace")
        {
            IsRequired = true
        };
        var ClientProjectPath = new Option<string>(name: "--ClientProjectPath")
        {
            IsRequired = true
        };
        var ServerProjectPath = new Option<string>(name: "--ServerProjectPath")
        {
            IsRequired = true
        };
        var IsApplicationContextGenerationEnabled = new Option<bool>(name: "--IsApplicationContextGenerationEnabled")
        {
            IsRequired = true
        };
        var UseFullTypeNames = new Option<bool>(name: "--UseFullTypeNames")
        {
            IsRequired = true
        };
        var ClientProjectTargetPlatform = new Option<string>(name: "--ClientProjectTargetPlatform")
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
            language,
            sharedCodeServicePath,
            codeGeneratorName,
        };

        var success = false;
        SharedCodeServiceParameters sharedCodeServiceParameters;


        rootCommand.SetHandler((language, 
            sharedCodeServicePath, 
            codeGeneratorName, 
            generatedFileName,
            ) =>
        {
            var options = new ClientCodeGenerationOptions
            {
                Language = language,
            };
            using (Stream stream = File.Open(sharedCodeServicePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                sharedCodeServiceParameters = (SharedCodeServiceParameters)binaryFormatter.Deserialize(stream);
            }
            //CreateOpenRiaClientFilesTask.CodeGenForNet6(generatedFileName, options, new Logger(), sharedCodeServiceParameters, codeGeneratorName);
            Console.WriteLine("Code generation succeeded");
            success = true;

        }, language, sharedCodeServicePath, codeGeneratorName, generatedFileName);

        rootCommand.Invoke(args);
        if(success)
            return 0;
        else
            return -1;
    }

    public class Logger : ILoggingService
    {
        public bool HasLoggedErrors => false;

        public void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            // Intentionally left empty
        }

        public void LogError(string message)
        {
            // Intentionally left empty
        }

        public void LogException(Exception ex)
        {
            // Intentionally left empty
        }

        public void LogMessage(string message)
        {
            // Intentionally left empty
        }

        public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            // Intentionally left empty
        }

        public void LogWarning(string message)
        {
            // Intentionally left empty
        }
    }

}
