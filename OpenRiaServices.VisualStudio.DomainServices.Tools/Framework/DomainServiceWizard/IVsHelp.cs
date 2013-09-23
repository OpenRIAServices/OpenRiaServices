using System;
using System.Runtime.InteropServices;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    [ComImport, Guid("4A791148-19E4-11D3-B86B-00C04F79F802"), TypeLibType((short)0x1040)]
    public interface IVsHelp
    {
        [DispId(1)]  // show TOC tool wnd
        void Contents();

        [DispId(2)] // show Index tool wnd
        void Index();

        [DispId(3)] // show Search tool wnd
        void Search();

        [DispId(4)] // show Index Results tool wnd
        void IndexResults();

        [DispId(5)] // show Search Results tool wnd
        void SearchResults();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "The interface is implemented in COM and the names need to match the COM implementation.")]
        [DispId(6)]
        void DisplayTopicFromId(
            [In][MarshalAs(UnmanagedType.BStr)] string bstrFile,
            [In][MarshalAs(UnmanagedType.U4)] int Id);

        [DispId(7)]
        void DisplayTopicFromURL(
            [In][MarshalAs(UnmanagedType.BStr)] string pszURL);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "The interface is implemented in COM and the names need to match the COM implementation.")]
        [DispId(8)]
        void DisplayTopicFromURLEx(
            [In][MarshalAs(UnmanagedType.BStr)] string pszURL,
            [In][MarshalAs(UnmanagedType.Interface)] object pIVsHelpTopicShowEvents);

        [DispId(9)] //Do not use this method for F1
        void DisplayTopicFromKeyword(
            [In][MarshalAs(UnmanagedType.BStr)] string pszKeyword);

        [DispId(10)] //Use this method to bring up help for F1 and from dialogs
        void DisplayTopicFromF1Keyword(
            [In][MarshalAs(UnmanagedType.BStr)] string pszKeyword);
        //we dont care about the rest of the interface, only the method with dispid = 10
    }
}
