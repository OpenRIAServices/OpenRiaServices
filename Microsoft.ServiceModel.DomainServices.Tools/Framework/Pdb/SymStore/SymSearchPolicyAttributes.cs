// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace OpenRiaServices.DomainServices.Tools.Pdb.SymStore
{

    // Only statics, does not need to be marked with the serializable attribute    
    using System;

	/// <summary>
	/// Available search policies for symbols
	/// </summary>
    [Serializable(), FlagsAttribute()]
    internal enum SymSearchPolicies
    {
        // query the registry for symbol search paths
        AllowRegistryAccess = 1,
    
        // access a symbol server
        AllowSymbolServerAccess = 2,
    
        // Look at the path specified in Debug Directory
        AllowOriginalPathAccess = 4,
    
        // look for PDB in the place where the exe is.
        AllowReferencePathAccess = 8,
    
    }
}
