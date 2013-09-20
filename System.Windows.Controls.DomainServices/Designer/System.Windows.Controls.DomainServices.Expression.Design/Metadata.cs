using System.Windows.Controls.Design.Common;
using Microsoft.Windows.Design.Metadata;

[assembly: ProvideMetadata(typeof(System.Windows.Controls.DomainServices.Expression.Design.MetadataRegistration))]

namespace System.Windows.Controls.DomainServices.Expression.Design
{
    /// <summary>
    /// MetadataRegistration class.
    /// </summary>
    public partial class MetadataRegistration : MetadataRegistrationBase, IProvideAttributeTable
    {
        /// <summary>
        /// Gets the AttributeTable value.
        /// </summary>
        public AttributeTable AttributeTable
        {
            get
            {
                return BuildAttributeTable();
            }
        }
    }
}
