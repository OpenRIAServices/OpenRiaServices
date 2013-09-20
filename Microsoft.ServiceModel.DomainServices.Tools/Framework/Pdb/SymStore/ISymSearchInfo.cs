// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.ServiceModel.DomainServices.Tools.Pdb.SymStore
{
    // Interface does not need to be marked with the serializable attribute
    using System;
    using System.Runtime.InteropServices;

    // This interface is returned by ISymbolReaderSymbolSearchInfo
    // and thus must be public
    [ComVisible(false)]
    internal interface ISymbolSearchInfo
    {
        int SearchPathLength { get; }

        String SearchPath { get; }

        int HResult { get; }
    }
}
