# Contributing to OpenRiaServices

For general contribution guidelines please look at the wiki at https://github.com/OpenRIAServices/OpenRiaServices/wiki/Contribution-Guidelines

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

You need to replace the OpenRiaServices assemblies in your project with your locally built versions. There are two recommended approaches:

**Option A: Replace files in NuGet package cache (Recommended)**

This is the most reliable method:

1. Locate your NuGet packages folder:
   - Global packages folder (most common): `%USERPROFILE%\.nuget\packages` (Windows) or `~/.nuget/packages` (Linux/Mac)
   - You can find the exact location by running: `dotnet nuget locals global-packages --list`

2. Navigate to the OpenRiaServices package folders, for example:
   - `%USERPROFILE%\.nuget\packages\openriaservices.codegen\{version}\`
   - `%USERPROFILE%\.nuget\packages\openriaservices.domainservices.tools\{version}\`
   - `%USERPROFILE%\.nuget\packages\openriaservices.domainservices.tools.texttemplate\{version}\` (if using text templating)

3. Within each package folder, navigate to the appropriate runtime/lib folder (e.g., `lib\net6.0\` or `tools\`)

4. Replace the DLL files with your locally built versions

5. Copy the corresponding PDB files next to the DLL files

**Option B: Replace files in project bin folder**

This method is simpler but may be less reliable:

1. Build your web project
2. Locate the bin folder of your web project (e.g., `YourWebProject\bin\Debug\net6.0\`)
3. Replace the OpenRiaServices DLL files with your locally built versions
4. Copy the corresponding PDB files next to the DLL files

**Option C: Use explicit references (Alternative)**

1. Remove the OpenRiaServices NuGet package references from your project
2. Add direct assembly references to your locally built OpenRiaServices DLLs
3. Ensure the PDB files are in the same directory as the DLLs

#### 3. Set Breakpoints

Open the OpenRiaServices source code in Visual Studio and set breakpoints in the code generation logic you want to debug. Common places to set breakpoints:

- Code generation entry points
- Error handling code
- Logging methods (especially in `ILoggingService` implementations)
- T4 template processing code (if using text templating)

#### 4. Attach Debugger

Code generation typically runs during the build process. To debug it:

1. Start your build process
2. Quickly attach the Visual Studio debugger to the process performing code generation:
   - For MSBuild: Attach to `MSBuild.exe` or `dotnet.exe`
   - For Visual Studio builds: Attach to `devenv.exe` or the relevant build process
3. Alternatively, you can add `System.Diagnostics.Debugger.Launch()` in the code generation entry point to automatically prompt for debugger attachment

#### 5. Trigger Code Generation

Rebuild your project to trigger code generation. If everything is set up correctly, your breakpoints should be hit.

### Troubleshooting Tips

- **Breakpoints not hit**: Ensure PDB files are present alongside DLLs and that they match the DLL version
- **Wrong source code shown**: The PDB files contain paths to source code; ensure the source code is at the expected location
- **Clean solution**: Sometimes you need to clean the solution and NuGet cache to ensure old assemblies are removed
- **Check build output**: Look for warnings about PDB files not matching or not being found
- **Multiple processes**: Build processes may spawn multiple MSBuild instances; you may need to attach to the correct child process

### Additional Resources

- [Code Generation Settings](https://github.com/OpenRIAServices/OpenRiaServices/wiki/Code-Generation-Settings)
- [Contribution Guidelines](https://github.com/OpenRIAServices/OpenRiaServices/wiki/Contribution-Guidelines)
