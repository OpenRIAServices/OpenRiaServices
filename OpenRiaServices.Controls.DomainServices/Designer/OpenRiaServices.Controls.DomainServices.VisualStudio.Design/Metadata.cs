using System.Windows.Controls.Design.Common;
using Microsoft.Windows.Design.Metadata;

namespace System.Windows.Controls.DomainServices.VisualStudio.Design
{
    /// <summary>
    /// MetadataRegistration class.
    /// </summary>
    public partial class MetadataRegistration : MetadataRegistrationBase, IProvideAttributeTable
    {
        private static AttributeTable _attribTable;

        AttributeTable IProvideAttributeTable.AttributeTable
        {
            get
            {
                if (_attribTable == null)
                {
                    _attribTable = BuildAttributeTable();
                }
                return _attribTable;
            }
        }
    }
}
