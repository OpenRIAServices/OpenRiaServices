
namespace DataTests.Scenarios.LTS.NorthwindBuddy
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using DataTests.Scenarios.LTS.Northwind;
    using OpenRiaServices.Hosting;
    using OpenRiaServices.Server;
    
    
    // The MetadataTypeAttribute identifies Bug843965_ABuddyMetadata as the class
    // that carries additional metadata for the Bug843965_ABuddy class.
    [MetadataTypeAttribute(typeof(Bug843965_ABuddy.Bug843965_ABuddyMetadata))]
    public partial class Bug843965_ABuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Bug843965_A class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class Bug843965_ABuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private Bug843965_ABuddyMetadata()
            {
            }
            
            public int ID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies Customer_Bug479436BuddyMetadata as the class
    // that carries additional metadata for the Customer_Bug479436Buddy class.
    [MetadataTypeAttribute(typeof(Customer_Bug479436Buddy.Customer_Bug479436BuddyMetadata))]
    public partial class Customer_Bug479436Buddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Customer_Bug479436 class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class Customer_Bug479436BuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private Customer_Bug479436BuddyMetadata()
            {
            }
            
            public string Address { get; set; }
            
            public string City { get; set; }
            
            public string CompanyName { get; set; }
            
            public string ContactName { get; set; }
            
            public string ContactTitle { get; set; }
            
            public string Country { get; set; }
            
            public string CustomerID { get; set; }
            
            public string Fax { get; set; }
            
            public string Phone { get; set; }
            
            public string PostalCode { get; set; }
            
            public string Region { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies TimestampEntityBuddyMetadata as the class
    // that carries additional metadata for the TimestampEntityBuddy class.
    [MetadataTypeAttribute(typeof(TimestampEntityBuddy.TimestampEntityBuddyMetadata))]
    public partial class TimestampEntityBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the TimestampEntity class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class TimestampEntityBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private TimestampEntityBuddyMetadata()
            {
            }
            
            public int A { get; set; }
            
            public int ID { get; set; }
            
            public byte[] Timestamp { get; set; }
        }
    }
}
