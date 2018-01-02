# Debugging Open Ria Services Code Generation

1.  Build your own version of the Code generation tools by compiling the _OpenRiaServices.DomainServices.Tools_ project and and optionally _OpenRiaServices.DomainServices.Server_.
2.  Copy your assemblies **including pdb** files (pdb files are required in order to point out where the source is located on your machine)
2b.  If you use the text templating tools then also make sure to compile and reference your version of "OpenRiaServices.DomainServices.Tools.TextTemplate"

3. Make it easier to find the correct msbuild process by
3a. Open Visual Studio options and make sure that you only enable 1 parallel build.
3b.. Close down all instances of visual studio before you start debugging

4. Open your project against which you wan't to debug the code generation code against
5. Build the project (client project) once in order to be sure that the msbuild process is created and it loads/runs the code generation
6. Open RiaServices.sln in a separate visual studio instance
In this second Visual Studio instance choose "Debug -> Attach to Process" in the menu and select the msbuild process. If you don't see one it is probably because you forgot step 6.
7. Now you can use the second Visual  Studio instance to place breakpoints anywhere in the code generation projects.
_OpenRiaServices.DomainServices.Tools.ClientCodeGenerationDispatcher.GenerateCode_ is a good place to start the first time.

**Note:** If you make changes to "OpenRiaServices.DomainServices.Tools" or "OpenRiaServices.DomainServices.Server" you need to close the Visual Studio instance associated with the client project (or kill it's msbuild process) since the files will otherwise be locked.

# Debug code generation for IntelliSense 

You can always place a Debug.Assert or similar to allow you to start debugging at a specific point in the code generation process and it will be applied to IntelliSense code generation too.

The following steps was suggested to by the roslyn team when trying to find out why some people had problem with IntelliSense in VS 2015 (https://github.com/dotnet/roslyn/issues/4893).

"
You can see the results of the design time build to confirm that it's a similar exception by:
# Starting a Developer Command Prompt
# Deleting the .vs directory beside your project
# Running  set TraceDesignTime=true  
# Running  devenv  
# Opening your project
# Navigating to your temp directory and looking at the  <guid>designtime.log  files for the one that runs the "Compile" target.
"