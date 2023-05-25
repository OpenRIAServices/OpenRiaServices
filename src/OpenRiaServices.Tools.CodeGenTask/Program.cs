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
        var language = new Option<string>(
            name: "--Language",
            description: "The language to use")
        { 
            IsRequired = true
        };

        var rootCommand = new RootCommand("Sample app for running code generation")
        {
            language
        };

        var success = false;
        var options = new ClientCodeGenerationOptions{ };
        var sharedCodeServiceParameters = new SharedCodeServiceParameters { };
        var codeGeneratorName = string.Empty;

        rootCommand.SetHandler((language) =>
        {
                CreateOpenRiaClientFilesTask.CodeGenForNet6("filename.g.cs", options, null, sharedCodeServiceParameters, codeGeneratorName);
                Console.WriteLine("Code generation succeeded");  
                success = true;
            
        }, language);

        rootCommand.Invoke(args);
        if(success)
            return 0;
        else
            return -1;
    }

}
