using System.CommandLine;
using System.CommandLine.Binding;

namespace OpenRiaServices.Tools.CodeGenTask;

/// <summary>
/// Binder class for <see cref="ClientCodeGenerationOptions"/>. Used to bind arguments to handlers
/// </summary>
public class ClientCodeGenerationOptionsBinder : BinderBase<ClientCodeGenerationOptions>
{
    private readonly Option<string> _language;
    private readonly Option<string> _clientFrameworkPath;
    private readonly Option<string> _serverProjectPath;
    private readonly Option<string> _clientProjectPath;
    private readonly Option<string> _clientRootNamespace;
    private readonly Option<string> _serverRootNamespace;
    private readonly Option<bool> _isApplicationContextGenerationEnabled;
    private readonly Option<TargetPlatform> _clientProjectTargetPlatform;
    private readonly Option<bool> _useFullTypeNames;

    /// <summary>
    /// Constructor that sets all arguments
    /// </summary>
    public ClientCodeGenerationOptionsBinder(Option<string> language, Option<string> clientFrameworkPath, Option<string> serverProjectPath, Option<string> clientProjectPath, Option<string> clientRootNamespace, Option<string> serverRootNamespace, Option<bool> isApplicationContextGenerationEnabled, Option<TargetPlatform> clientProjectTargetPlatform, Option<bool> useFullTypeNames)
    {
        _language = language;
        _clientFrameworkPath = clientFrameworkPath;
        _serverProjectPath = serverProjectPath;
        _clientProjectPath = clientProjectPath;
        _clientRootNamespace = clientRootNamespace;
        _serverRootNamespace = serverRootNamespace;
        _isApplicationContextGenerationEnabled = isApplicationContextGenerationEnabled;
        _clientProjectTargetPlatform = clientProjectTargetPlatform;
        _useFullTypeNames = useFullTypeNames;
    }

    /// <summary>
    /// Parse result in binding context to create and return <see cref="ClientCodeGenerationOptions"/>
    /// </summary>
    protected override ClientCodeGenerationOptions GetBoundValue(BindingContext bindingContext)
    {
        return new ClientCodeGenerationOptions
        {
            Language = bindingContext.ParseResult.GetValueForOption(_language),
            ClientFrameworkPath = bindingContext.ParseResult.GetValueForOption(_clientFrameworkPath),
            ServerProjectPath = bindingContext.ParseResult.GetValueForOption(_serverProjectPath),
            ClientProjectPath = bindingContext.ParseResult.GetValueForOption(_clientProjectPath),
            ClientRootNamespace = bindingContext.ParseResult.GetValueForOption(_clientRootNamespace),
            ServerRootNamespace = bindingContext.ParseResult.GetValueForOption(_serverRootNamespace),
            IsApplicationContextGenerationEnabled = bindingContext.ParseResult.GetValueForOption(_isApplicationContextGenerationEnabled),
            ClientProjectTargetPlatform = bindingContext.ParseResult.GetValueForOption(_clientProjectTargetPlatform),
            UseFullTypeNames = bindingContext.ParseResult.GetValueForOption(_useFullTypeNames),
        };
    }
}
