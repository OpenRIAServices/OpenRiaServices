// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.ServiceModel.DomainServices.Tools.Pdb.SymStore 
{
    // Interface does not need to be marked with the serializable attribute
    using System.Diagnostics.SymbolStore;
    using System.Runtime.InteropServices;

    // This interface isn't directly returned, but SymbolScope which implements ISymbolScope
    // also implements ISymbolScope2 and thus you may want to explicitly cast it to use these methods.
    [ComVisible(false)]
    internal interface ISymbolScope2 : ISymbolScope
    {
        int LocalCount{ get; }
        
        int ConstantCount{ get; }

        ISymbolConstant[] GetConstants();
    }
}
