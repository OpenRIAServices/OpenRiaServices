using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Design.Common;
using Microsoft.Windows.Design.Metadata;

[assembly: ProvideMetadata(typeof(System.Windows.Controls.DomainServices.Design.MetadataRegistration))]

namespace System.Windows.Controls.DomainServices.Design
{
    /// <summary>
    /// MetadataRegistration class.
    /// </summary>
    public partial class MetadataRegistration : MetadataRegistrationBase, IProvideAttributeTable
    {
        /// <summary>
        /// Design time metadata registration class.
        /// </summary>
        public MetadataRegistration()
            : base()
        {
            AssemblyName asmName = typeof(DomainDataSource).Assembly.GetName();
            XmlResourceName = asmName.Name + ".Design." + asmName.Name + ".XML";
            AssemblyFullName = ", " + asmName.FullName;
        }

        /// <summary>
        /// Gets the AttributeTable for design time metadata.
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
