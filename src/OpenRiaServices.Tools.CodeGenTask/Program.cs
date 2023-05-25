using System;
//using Microsoft.Build.Utilities;
using System.IO;
using OpenRiaServices.Tools;
using System.Linq;
using System.CommandLine;

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

        var isCsharp = false;

        rootCommand.SetHandler((language) =>
        {
            //var codeGenTask = new CreateOpenRiaClientFilesTask();
            // Run the codegen
            var success = true;//codeGenTask.Execute();
            if (success && language == "C#")
            {
                isCsharp = true;
                Console.WriteLine("Code generation succeeded");
                //Console.WriteLine($"Generated code can be found at: {codeGenTask.GeneratedCodePath}");
            }
            else
            {
                Console.WriteLine("Code generation failed");
            }
        },
            language);

        rootCommand.Invoke(args);
        if(isCsharp)
            return 0;
        else
            return -1;
    }

}
