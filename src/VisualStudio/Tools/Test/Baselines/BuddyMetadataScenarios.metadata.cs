
namespace DataModels.ScenarioModelsBuddy
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using DataModels.ScenarioModels;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    
    
    // The MetadataTypeAttribute identifies EntityPropertyNamedPublicBuddyMetadata as the class
    // that carries additional metadata for the EntityPropertyNamedPublicBuddy class.
    [MetadataTypeAttribute(typeof(EntityPropertyNamedPublicBuddy.EntityPropertyNamedPublicBuddyMetadata))]
    public partial class EntityPropertyNamedPublicBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the EntityPropertyNamedPublic class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class EntityPropertyNamedPublicBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private EntityPropertyNamedPublicBuddyMetadata()
            {
            }
            
            public int publicPublic { get; set; }
        }
    }
}
