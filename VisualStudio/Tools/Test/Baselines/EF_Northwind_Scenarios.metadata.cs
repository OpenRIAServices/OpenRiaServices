
namespace DataTests.Scenarios.EF.NorthwindBuddy
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using DataTests.Scenarios.EF.Northwind;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    
    
    // The MetadataTypeAttribute identifies AddressCTBuddyMetadata as the class
    // that carries additional metadata for the AddressCTBuddy class.
    [MetadataTypeAttribute(typeof(AddressCTBuddy.AddressCTBuddyMetadata))]
    public partial class AddressCTBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the AddressCT class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class AddressCTBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private AddressCTBuddyMetadata()
            {
            }
            
            public string AddressLine { get; set; }
            
            public string City { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ContactInfoCTBuddyMetadata as the class
    // that carries additional metadata for the ContactInfoCTBuddy class.
    [MetadataTypeAttribute(typeof(ContactInfoCTBuddy.ContactInfoCTBuddyMetadata))]
    public partial class ContactInfoCTBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ContactInfoCT class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ContactInfoCTBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ContactInfoCTBuddyMetadata()
            {
            }
            
            public AddressCT Address { get; set; }
            
            public string HomePhone { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CustomerBuddyMetadata as the class
    // that carries additional metadata for the CustomerBuddy class.
    [MetadataTypeAttribute(typeof(CustomerBuddy.CustomerBuddyMetadata))]
    public partial class CustomerBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Customer class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CustomerBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CustomerBuddyMetadata()
            {
            }
            
            public string ContactName { get; set; }
            
            public string CustomerID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies EmployeeBuddyMetadata as the class
    // that carries additional metadata for the EmployeeBuddy class.
    [MetadataTypeAttribute(typeof(EmployeeBuddy.EmployeeBuddyMetadata))]
    public partial class EmployeeBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Employee class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class EmployeeBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private EmployeeBuddyMetadata()
            {
            }
            
            public int EmployeeID { get; set; }
            
            public string FirstName { get; set; }
            
            public string LastName { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies EmployeeWithCTBuddyMetadata as the class
    // that carries additional metadata for the EmployeeWithCTBuddy class.
    [MetadataTypeAttribute(typeof(EmployeeWithCTBuddy.EmployeeWithCTBuddyMetadata))]
    public partial class EmployeeWithCTBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the EmployeeWithCT class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class EmployeeWithCTBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private EmployeeWithCTBuddyMetadata()
            {
            }
            
            public ContactInfoCT ContactInfo { get; set; }
            
            public int EmployeeID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies EntityWithNullFacetValuesForTimestampComparisonBuddyMetadata as the class
    // that carries additional metadata for the EntityWithNullFacetValuesForTimestampComparisonBuddy class.
    [MetadataTypeAttribute(typeof(EntityWithNullFacetValuesForTimestampComparisonBuddy.EntityWithNullFacetValuesForTimestampComparisonBuddyMetadata))]
    public partial class EntityWithNullFacetValuesForTimestampComparisonBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the EntityWithNullFacetValuesForTimestampComparison class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class EntityWithNullFacetValuesForTimestampComparisonBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private EntityWithNullFacetValuesForTimestampComparisonBuddyMetadata()
            {
            }
            
            public string ConcurrencyTimestamp { get; set; }
            
            public int Id { get; set; }
            
            public string StringWithoutComputed { get; set; }
            
            public string StringWithoutConcurrencyMode { get; set; }
            
            public string StringWithoutFixedLength { get; set; }
            
            public string StringWithoutMaxLength { get; set; }
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
            
            public int Id { get; set; }
            
            public byte[] Timestamp { get; set; }
        }
    }
}
