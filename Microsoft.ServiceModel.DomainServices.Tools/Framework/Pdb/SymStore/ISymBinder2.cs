// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.ServiceModel.DomainServices.Tools.Pdb.SymStore 
{
    using System;
    using System.Diagnostics.SymbolStore;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    [ComVisible(false)]
    internal interface ISymbolBinder2
    {
        ISymbolReader GetReaderForFile(Object importer, String filename, String searchPath);
                                
        ISymbolReader GetReaderForFile(Object importer, String fileName,
                                           String searchPath, SymSearchPolicies searchPolicy);
        
        ISymbolReader GetReaderForFile(Object importer, String fileName,
                                           String searchPath, SymSearchPolicies searchPolicy,
                                           object callback);
      
        ISymbolReader GetReaderFromStream(Object importer, IStream stream);
    }
}
