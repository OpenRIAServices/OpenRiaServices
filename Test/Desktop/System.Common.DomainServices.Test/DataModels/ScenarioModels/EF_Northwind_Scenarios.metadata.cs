using System.ComponentModel.DataAnnotations;

namespace DataTests.Scenarios.EF.Northwind
{
    [MetadataType(typeof(RequiredAttributeTestEntity.Metadata))]
    public partial class RequiredAttributeTestEntity
    {
        private class Metadata
        {
            [Required(AllowEmptyStrings = true)]
            public string RequiredStringOverride { get; set; }
        }
    }
}
