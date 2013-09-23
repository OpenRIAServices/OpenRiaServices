// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace OpenRiaServices.DomainServices.Tools.Pdb.SymStore
{
    using System;
    using System.Diagnostics.SymbolStore;
    using System.Runtime.InteropServices;

    // This interface isn't directly returned or used by any of the classes,
    // but the implementation of the ISymbolMethod also implements ISymEncMethod
    // so you could explicitly cast it to that.
    [ComVisible(false)]
    internal interface ISymbolEnCMethod : ISymbolMethod
    {
        String GetFileNameFromOffset(int dwOffset);

        int GetLineFromOffset(int dwOffset,
                                  out int column,
                                  out int endLine,
                                  out int endColumn,
                                  out int startOffset);
    }
}

