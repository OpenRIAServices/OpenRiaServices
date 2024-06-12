using System;
using System.IO;
using System.Linq;
using System.CommandLine;
using System.Collections.Generic;

namespace OpenRiaServices.Tools.CodeGenTask;

static class Program
{
    static int Main(string[] args)
    {
        // TODO: Remove dependency on MSBuild and then remove any PackageReferences to MSBuild and MSBuildLocator
        // * This will require splitting "OpenRiaServices.Tools" into 2 separate projects, one with MSbuild tasks and one without (just code generation)
        // For now register the most recent version of MSBuild
        Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(
           instance => instance.Version).First());

        // TODO: Look at which options should be required. Should be the same as in OpenRiaServices.Client.CodeGen.targets
        var languageOption = new Option<string>("--language");
        var clientFrameworkOption = new Option<string>("--clientFrameworkPath");
        var serverProjectPathOption = new Option<string>("--serverProjectPath");
        var clientProjectPathOption = new Option<string>("--clientProjectPath");
        var clientRootNamespaceOption = new Option<string>("--clientRootNamespace");
        var serverRootNamespaceOption = new Option<string>("--serverRootNamespace");
        var isApplicationContextGenerationEnabledOption = new Option<bool>("--isApplicationContextGenerationEnabled");
        var useFullTypeNamesOption = new Option<bool>("--useFullTypeNames");

        var sharedSourceFilesOption = new Option<IEnumerable<string>>("--sharedSourceFiles") { AllowMultipleArgumentsPerToken = true };
        var symbolSearchPathsOption = new Option<IEnumerable<string>>("--symbolSearchPaths") { AllowMultipleArgumentsPerToken = true };
        var serverAssembliesOption = new Option<IEnumerable<string>>("--serverAssemblies") { AllowMultipleArgumentsPerToken = true };
        var clientAssembliesOption = new Option<IEnumerable<string>>("--clientAssemblies") { AllowMultipleArgumentsPerToken = true };
        var clientAssemblyPathsNormalizedOption = new Option<IEnumerable<string>>("--clientAssemblyPathsNormalized") { AllowMultipleArgumentsPerToken = true };

        var codeGeneratorName = new Option<string>(name: "--codeGeneratorName") { };

        var generatedFileName = new Option<string>(name: "--generatedFileName")
        {
            IsRequired = true
        };

        var loggingPipe = new Option<string>(name: "--loggingPipe");

        var rootCommand = new RootCommand("Sample app for running code generation")
        {
            languageOption,
            clientFrameworkOption,
            serverProjectPathOption,
            clientProjectPathOption,
            clientRootNamespaceOption,
            serverRootNamespaceOption,
            isApplicationContextGenerationEnabledOption,
            useFullTypeNamesOption,
            sharedSourceFilesOption,
            symbolSearchPathsOption,
            serverAssembliesOption,
            clientAssembliesOption,
            clientAssemblyPathsNormalizedOption,
            codeGeneratorName,
            generatedFileName,
            loggingPipe,
        };

        bool success = false;

       rootCommand.SetHandler((clientCodeGenerationOptionValue, sharedCodeServiceParametersValue, codeGeneratorName, generatedFileName, pipeName)
            => success = RunCodeGenForNet6(clientCodeGenerationOptionValue, sharedCodeServiceParametersValue, codeGeneratorName, generatedFileName, pipeName),
            new ClientCodeGenerationOptionsBinder(
                languageOption, clientFrameworkOption, serverProjectPathOption, clientProjectPathOption, clientRootNamespaceOption, serverRootNamespaceOption,
                isApplicationContextGenerationEnabledOption, useFullTypeNamesOption),
            new SharedCodeServiceParametersBinder(sharedSourceFilesOption, symbolSearchPathsOption, serverAssembliesOption, clientAssembliesOption, clientAssemblyPathsNormalizedOption),
            codeGeneratorName,
            generatedFileName,
            loggingPipe);

        rootCommand.Invoke(args);
        if (success)
            return 0;
        else
            return -1;
    }

    private static bool RunCodeGenForNet6(ClientCodeGenerationOptions clientCodeGenerationOption, SharedCodeServiceParameters sharedCodeServiceParameters, string codeGeneratorName, string generatedFileName, string loggingPipe)
    {
        ILoggingService log = string.IsNullOrEmpty(loggingPipe) ? new ConsoleLogger() : new OpenRiaServices.Tools.Logging.CrossProcessLoggingWriter(loggingPipe);
        
        try
        {
            log.LogMessage($"OpenRiaServices CodeGen running on {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

            SetupAppConfig(clientCodeGenerationOption);
            RiaClientFilesTaskHelpers.CodeGenForNet6(generatedFileName, clientCodeGenerationOption, log, sharedCodeServiceParameters, codeGeneratorName);
            log.LogMessage("Code generation succeeded");
            return true;
        }
        catch (Exception ex)
        {
            log.LogException(ex);
            return false;
        }
        finally
        {
            (log as IDisposable)?.Dispose();
        }
    }

    // Find app.config/web.config based on https://stackoverflow.com/questions/4738/using-configurationmanager-to-load-config-from-an-arbitrary-location/14246260#14246260
    // Note: This just looks for "app.config" in the root, we might want to be smarter when searching for them.
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
}
