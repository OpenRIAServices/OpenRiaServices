// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace OpenRiaServices.DomainServices.Tools.Pdb.SymStore
{

    // Interface does not need to be marked with the serializable attribute
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    [
        ComImport,
        Guid("F8B3534A-A46B-4980-B520-BEC4ACEABA8F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedSymbolSearchInfo 
    {
        void GetSearchPathLength(out int pcchPath);

        void GetSearchPath(int cchPath,
                              out int pcchPath,
                              [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szPath);

        void GetHRESULT(out int hr);
    }

    internal class SymSymbolSearchInfo : ISymbolSearchInfo
    {
        ISymUnmanagedSymbolSearchInfo m_target;

        public SymSymbolSearchInfo(ISymUnmanagedSymbolSearchInfo target)
        {
            m_target = target;
        }
        
        public int SearchPathLength
        {
            get 
            {
                int length;
                m_target.GetSearchPathLength(out length);
                return length;
            }
        }

        public String SearchPath
        {
            get 
            {
                int length;
                m_target.GetSearchPath(0, out length, null);
                StringBuilder path = new StringBuilder(length);
                m_target.GetSearchPath(length, out length, path);
                return path.ToString();
            }
        }

        public int HResult
        {
            get 
            {
                int hr;
                m_target.GetHRESULT(out hr);
                return hr;
            }
         }
      }

}
