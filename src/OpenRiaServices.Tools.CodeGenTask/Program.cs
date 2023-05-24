using System;
using Microsoft.Build.Utilities;
using System.IO;
using OpenRiaServices.Tools;
using System.Linq;

public class CodeGenTask
{    static void Main(string[] args) // Take in params to setup the build here
     {
        // Setup the codegen task based on the parameters
        ClientCodeGenerationOptions options = CreateCodeGenOptions(args);
        var codeGenTask = new CreateOpenRiaClientFilesTask();
        codeGenTask.BuildEngine = null; // TODO - migth not be ne
        codeGenTask.Language = "C#"; // Take in to decide if we should use vb or C#
        codeGenTask.ServerProjectPath = ""; // TODO
        // ... Set up the rest of the parameters
        // Run the codegen
        var success = codeGenTask.Execute();
        if (success)
        {
            Console.WriteLine("Code generation succeeded");
            Console.WriteLine($"Generated code can be found at: {codeGenTask.GeneratedCodePath}");
        }
        else
        {
            Console.WriteLine("Code generation failed");
        }
    }
}
