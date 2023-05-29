using System;
//using Microsoft.Build.Utilities;
using System.IO;
using OpenRiaServices.Tools;
using System.Linq;
using System.CommandLine;
using System.Runtime.CompilerServices;


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
            CreateOpenRiaClientFilesTask.CodeGenForNet6(generatedFileName, clientCodeGenerationOption, new Logger(), sharedCodeServiceParameters, codeGeneratorName);
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
