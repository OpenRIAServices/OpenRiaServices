// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.ServiceModel.DomainServices.Tools.Pdb.SymStore
{
    // Interface does not need to be marked with the serializable attribute
    using System.Diagnostics.SymbolStore;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    //// <include file='doc\ISymENCUpdate.uex' path='docs/doc[@for="SymbolLineDelta"]/*' />
    [StructLayout(LayoutKind.Sequential)]
    internal struct SymbolLineDelta
    {
        SymbolToken mdMethod;
        int delta;
    }

    //// <include file='doc\ISymScope.uex' path='docs/doc[@for="ISymbolScope"]/*' />
    [ComVisible(false)]
    internal interface ISymbolEncUpdate
    {
        //// <include file='doc\ISymENCUpdate.uex' path='docs/doc[@for="ISymbolEncUpdate.UpdateSymbolStore"]/*' />

        void UpdateSymbolStore(IStream stream, SymbolLineDelta[] symbolLineDeltas);
        //// <include file='doc\ISymENCUpdate.uex' path='docs/doc[@for="ISymbolEncUpdate.GetLocalVariableCount"]/*' />

        int GetLocalVariableCount(SymbolToken mdMethodToken);
        //// <include file='doc\ISymENCUpdate.uex' path='docs/doc[@for="ISymbolEncUpdate.GetLocalVariables"]/*' />

        ISymbolVariable[] GetLocalVariables(SymbolToken mdMethodToken);
    }
}
