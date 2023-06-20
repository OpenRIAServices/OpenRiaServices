using System.Linq;
using System.CommandLine;
using System.Collections.Generic;
using System.CommandLine.Binding;

namespace OpenRiaServices.Tools.CodeGenTask;

/// <summary>
/// Binder class for <see cref="SharedCodeServiceParameters"/>. Used to bind arguments to handlers
/// </summary>
internal class SharedCodeServiceParametersBinder : BinderBase<SharedCodeServiceParameters>
{
    private readonly Option<IEnumerable<string>> _sharedSourceFiles;
    private readonly Option<IEnumerable<string>> _symbolSearchPaths;
    private readonly Option<IEnumerable<string>> _serverAssemblies;
    private readonly Option<IEnumerable<string>> _clientAssemblies;
    private readonly Option<IEnumerable<string>> _clientAssemblyPathsNomalized;

    /// <summary>
    /// Constructor that sets all arguments
    /// </summary>
    internal SharedCodeServiceParametersBinder(Option<IEnumerable<string>> sharedSourceFiles, Option<IEnumerable<string>> symbolSearchPaths, Option<IEnumerable<string>> serverAssemblies, Option<IEnumerable<string>> clientAssemblies, Option<IEnumerable<string>> clientAssemblyPathsNomalized)
    {
        _sharedSourceFiles = sharedSourceFiles;
        _symbolSearchPaths = symbolSearchPaths;
        _serverAssemblies = serverAssemblies;
        _clientAssemblies = clientAssemblies;
        _clientAssemblyPathsNomalized = clientAssemblyPathsNomalized;
    }

    /// <summary>
    /// Parse result in binding context to create and return <see cref="SharedCodeServiceParameters"/>
    /// </summary>
    protected override SharedCodeServiceParameters GetBoundValue(BindingContext bindingContext)
    {
        return new SharedCodeServiceParameters
        {
            // TODO: should we use default names such as "--shared-source-files" 
            SharedSourceFiles = bindingContext.ParseResult.GetValueForOption(_sharedSourceFiles).ToArray(),
            SymbolSearchPaths = bindingContext.ParseResult.GetValueForOption(_symbolSearchPaths).ToArray(),
            ServerAssemblies = bindingContext.ParseResult.GetValueForOption(_serverAssemblies).ToArray(),
            ClientAssemblies = bindingContext.ParseResult.GetValueForOption(_clientAssemblies).ToArray(),
            ClientAssemblyPathsNormalized = bindingContext.ParseResult.GetValueForOption(_clientAssemblyPathsNomalized).ToArray(),
        };
    }
}
