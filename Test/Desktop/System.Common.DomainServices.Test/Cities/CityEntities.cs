using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.ServiceModel.DomainServices;

namespace Cities
{
    /// <summary>
    /// Enum declared in user code that will be generated into the client code.
    /// Demonstrates propagation of <see cref="DataContract"/> and <see cref="EnumMember"/>
    /// </summary>
    [DataContract(Name="CityName", Namespace="CityNamespace")]
    public enum ShippingZone
    {
        [EnumMember(Value="P")]
        Pacific = 0,    // default

        [EnumMember(Value="C")]
        Central,

        [EnumMember(Value="E")]
        Eastern
    }

    /// <summary>
    /// This collection of types enhances the simple City types
    /// through the use of buddy classes that contribute metadata.
    /// </summary>
    [MetadataType(typeof(CityMetadata))]
    public partial class City
    {
    }

    public partial class CityMetadata
    {
        [Key]
        [Required]
        [StringLength(32)]
        [RegularExpression("^[A-Z]+[a-z A-Z]*$")]
        [Display(ResourceType = typeof(Cities_Resources), ShortName = "CityCaption", Name = "CityName", Prompt = "CityPrompt", Description = "CityHelpText")]
        [RoundtripOriginal]
        public string Name { get; set; }

        [Key]
        [RoundtripOriginal]
        public string CountyName { get; set; }

        [Key]
        [StringLength(2)]
        [RegularExpression("^[A-Z]+[a-z A-Z]*$")]
        [RoundtripOriginal]
        public string StateName { get; set; }

        [Display(AutoGenerateField = false)]
        [RoundtripOriginal]
        [CustomValidation(typeof(CityPropertyValidator), "IsValidZoneName")]
        public string ZoneName { get; set; }

        [Range(0, 9999)]
        [RoundtripOriginal]
        public int ZoneID { get; set; }

        [Editable(false)]
        public string CalculatedCounty { get; set; }

        [Association("County_City", "CountyName,StateName", "Name,StateName", IsForeignKey = true)]
        public County County { get; set; }

        [Association("City_Zip", "Name, CountyName, StateName", "CityName,  CountyName, StateName")]
        public List<Zip> ZipCodes { get; set; }
    }

    [MetadataType(typeof(CityWithInfoMetadata))]
    public partial class CityWithInfo
    {
    }

    public partial class CityWithInfoMetadata
    {
        [Required]
        [StringLength(32)]
        public string Info { get; set; }

        [Association("CityWithInfo_ZipWithInfo", "Name, CountyName, StateName", "CityName,  CountyName, StateName")]
        public List<ZipWithInfo> ZipCodesWithInfo { get; set; }
    }

    [MetadataType(typeof(CountyMetadata))]
    public partial class County
    {
    }
    public partial class CountyMetadata
    {
        [Key]
        [Required]
        [StringLength(32)]
        [RegularExpression("^[A-Z]+[a-z A-Z]*$")]
        [RoundtripOriginal]
        public string Name { get; set; }

        [Key]
        [Required]
        [RoundtripOriginal]
        public string StateName { get; set; }

        [Association("State_County", "StateName", "Name", IsForeignKey = true)]
        public State State { get; set; }

        [Association("County_City", "Name,StateName", "CountyName,StateName")]
        public List<City> Cities { get; set; }
    }


    [MetadataType(typeof(StateMetadata))]
    public partial class State
    {
    }
    public partial class StateMetadata
    {
        [Key]
        [Required]
        [StringLength(2)]
        [RegularExpression("^[A-Z]*")]
        [CustomValidation(typeof(StateNameValidator), "IsStateNameValid")]
        [RoundtripOriginal]
        public string Name { get; set; }

        [Key]
        [Required]
        [RegularExpression("^[A-Z]+[a-z A-Z]*$")]
        [RoundtripOriginal]
        public string FullName { get; set; }

        [Association("State_County", "Name", "StateName")]
        [CustomValidation(typeof(CountiesValidator), "AreCountiesValid")]
        public List<County> Counties { get; set; }
    }


    [MetadataType(typeof(ZipMetadata))]
    public partial class Zip
    {
    }
    [CustomValidation(typeof(ZipValidator), "IsZipValid", ErrorMessage = "Zip codes cannot have matching city and state names")]
    [DomainIdentifier("ZipPattern")]
    [Description("Zip code entity")]
    public partial class ZipMetadata
    {
        [Key]
        [Range(0, 99999)]
        [MustStartWith(9)]
        [DisplayFormat(DataFormatString = "nnnnn")]
        [Description("Zip codes must be 5 digits starting with 9")]
        [RoundtripOriginal]
        public int Code { get; set; }

        [Key]
        [Range(0, 9999)]
        [DisplayFormat(NullDisplayText = "(optional)")]
        [UIHint("DataGrid", "Jolt", "stringParam", "hello", "doubleParam", 2.0)]
        [RoundtripOriginal]
        public int FourDigit { get; set; }

        [Required]
        [RoundtripOriginal]
        public string CityName { get; set; }

        [RoundtripOriginal]
        public string CountyName { get; set; }

        [Required]
        [RoundtripOriginal]
        public string StateName { get; set; }

        [CustomValidation(typeof(CityPropertyValidator), "IsValidCity")]
        [Association("City_Zip", "CityName,  CountyName, StateName", "Name, CountyName, StateName", IsForeignKey = true)]
        public City City { get; set; }
    }
}
