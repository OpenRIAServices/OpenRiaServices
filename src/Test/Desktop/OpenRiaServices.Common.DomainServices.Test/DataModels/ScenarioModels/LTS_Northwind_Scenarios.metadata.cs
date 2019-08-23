using System.ComponentModel.DataAnnotations;

namespace DataTests.Scenarios.LTS.Northwind
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
