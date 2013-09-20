using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.ServiceModel.DomainServices.WindowsAzure
{
    /// <summary>
    /// Type used to store the default metadata for the <see cref="TableEntity"/>.
    /// </summary>
    internal sealed class TableEntityMetadata
    {
        [Key]
        [Editable(false, AllowInitialValue = true)]
        [Display(AutoGenerateField = false)]
        public string PartitionKey { get; set; }

        [Key]
        [Editable(false, AllowInitialValue = true)]
        [Display(AutoGenerateField = false)]
        public string RowKey { get; set; }

        [Timestamp]
        [Editable(false)]
        [Display(AutoGenerateField = false)]
        public DateTime Timestamp { get; set; }
    }
}
