# Contributing to OpenRiaServices

For general contribution guidelines please look at the wiki at https://github.com/OpenRIAServices/OpenRiaServices/wiki/Contribution-Guidelines

## General feedback or discussions

Create a new discussion at [GitHub](https://github.com/OpenRIAServices/OpenRiaServices/discussions)

## I've found a bug or have a feature request

Feel free to post an issue [here on GitHub](https://github.com/OpenRIAServices/OpenRiaServices/). 
Please have a look at the good bug filing template for asp.net  found at [https://github.com/aspnet/Home/wiki/Functional-bug-template](https://github.com/aspnet/Home/wiki/Functional-bug-template)

## First steps in contributing code or other content

Download the code, compile and have a look around.
There are several [sample projects in Samples repository](https://github.com/OpenRIAServices/Samples) to play around with

What to contribute with:

* If you are starting you can look for issues marked as Up For Grabs or which looks simple.
* If you find an issue which you want to provide a fix for then please do add a comment that you want to or will investigate it. 
* The documentation is currently very sparse and lacking, please feel free to contribute.  You can always post issues with proposed documentation.
* Not all tests currently run and some are non-trivial to get going, so any help in getting more tests passing or improving the getting started experience are welcome.

### Code style

Try to follow the existing coding style.
Make sure that you can compile the project with your changes using release build without any new compilation warning.

We generally try to follow [Microsoft .NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)

### Comments

All new public, protected and internal methods should be documented, using standard "{{///}}" comments.
Make sure that you can compile the project with your changes using release build without any new compilation warning.

### Commit messages

* Make sure that the first line always contains a short **descriptive** summary of the changes.
* The remainting rows can be used to provide more details about the changes.
* You can use "Fix # BUG TITLE" if bug title is describing and the contribution is a "small" one-commit pull request
* If the commits 

Example of a commit message for a fix which only containst a single commit:
```
Fix #X THE BUG DESCRIPTION /summary of changes
 Commit detail 1
 Commit detail 2
 Commit detail 3
```

For multi part commits, it is a good idea to embed the issue number in the commit message, but it is not mandatory at the moment.
```
[Issue #232](Issue-#232) Short summary of changes
 Commit detail 1
 Commit detail 2
 Commit detail 3
```

### Tests

Please take your time and add test for any new functionality added.


## Debugging Open RIA Services Code Generation

If you need to debug the code generation process (for example, when troubleshooting code generation failures), follow these steps:

### Prerequisites

- Visual Studio with debugger
- Your OpenRiaServices source code compiled locally
- PDB files generated alongside your assemblies (ensure Debug configuration is used)

### Steps to Debug Code Generation

#### 1. Build OpenRiaServices Locally

Build the OpenRiaServices solution in Debug mode to generate assemblies with their corresponding PDB files. The PDB files are essential for the debugger to map the execution to your source code.

#### 2. Copy Assemblies and PDB Files

You need to replace the OpenRiaServices assemblies in your project with your locally built versions.
The most reliable method is to replace files in NuGet package cache:

1. Locate your NuGet packages folder:
   - Global packages folder (most common): `%USERPROFILE%\.nuget\packages` (Windows) or `~/.nuget/packages` (Linux/Mac)
   - You can find the exact location by running: `dotnet nuget locals global-packages --list`

2. Navigate to the OpenRiaServices package folders, for example:
   - `%USERPROFILE%\.nuget\packages\openriaservices.codegen\{version}\`
   - `%USERPROFILE%\.nuget\packages\openriaservices.domainservices.tools.texttemplate\{version}\` (if using text templating)

3. Within each package folder, navigate to the appropriate runtime/lib folder (e.g., `lib\net6.0\` or `tools\`)

4. Replace the DLL , and PDB files with your locally built versions.

**Option B: Use explicit references (Alternative)**

1. Remove the OpenRiaServices NuGet package references from your project
2. Add direct assembly references to your locally built OpenRiaServices DLLs

#### 3. Set Breakpoints

Open the OpenRiaServices source code in Visual Studio and set breakpoints in the code generation logic you want to debug. Common places to set breakpoints:

- Code generation entry points
- Error handling code
- Logging methods (especially in `ILoggingService` implementations)
- T4 template processing code (if using text templating)

#### 4. Attach Debugger

Code generation typically runs during the build process.

1. Start your build process
2. Quickly attach the Visual Studio debugger to the process performing code generation:
   - For MSBuild: Attach to `MSBuild.exe` or `dotnet.exe`
   - For Visual Studio builds: Attach to `devenv.exe` or the relevant build process
3. **To make debugging a lot easier**, you can add `System.Diagnostics.Debugger.Launch()` in the code generation entry point to automatically prompt for debugger attachment

#### 5. Trigger Code Generation

Rebuild your project to trigger code generation (you can delete the `obj` folder and do a normal build).
If everything is set up correctly, your breakpoints should be hit.

### Troubleshooting Tips

- **Breakpoints not hit**: Ensure PDB files are present alongside DLLs and that they match the DLL version. 
Add  `System.Diagnostics.Debugger.Launch()` to the entry point.
- **Wrong source code shown**: The PDB files contain paths to source code; ensure the source code is at the expected location
- **Clean solution**: Sometimes you need to  delete the `obj` folder of the client project or clean the solution to ensure a new code generation is triggered
- **Check build output**: Look for warnings about PDB files not matching or not being found
- **Multiple processes**: Build processes may spawn multiple MSBuild instances; you may need to attach to the correct child process

### Additional Resources

- [Code Generation Settings](https://github.com/OpenRIAServices/OpenRiaServices/wiki/Code-Generation-Settings)

