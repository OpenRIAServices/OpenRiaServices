
namespace NorthwindModelBuddy
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Linq;
    using NorthwindModel;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    
    
    // The MetadataTypeAttribute identifies CategoryBuddyMetadata as the class
    // that carries additional metadata for the CategoryBuddy class.
    [MetadataTypeAttribute(typeof(CategoryBuddy.CategoryBuddyMetadata))]
    public partial class CategoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Category class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CategoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CategoryBuddyMetadata()
            {
            }
            
            public int CategoryID { get; set; }
            
            public string CategoryName { get; set; }
            
            public string Description { get; set; }
            
            public byte[] Picture { get; set; }
            
            public EntityCollection<Product> Products { get; set; }
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
            
            public string Address { get; set; }
            
            public string City { get; set; }
            
            public string CompanyName { get; set; }
            
            public string ContactName { get; set; }
            
            public string ContactTitle { get; set; }
            
            public string Country { get; set; }
            
            public EntityCollection<CustomerDemographic> CustomerDemographics { get; set; }
            
            public string CustomerID { get; set; }
            
            public string Fax { get; set; }
            
            public EntityCollection<Order> Orders { get; set; }
            
            public string Phone { get; set; }
            
            public string PostalCode { get; set; }
            
            public string Region { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CustomerDemographicBuddyMetadata as the class
    // that carries additional metadata for the CustomerDemographicBuddy class.
    [MetadataTypeAttribute(typeof(CustomerDemographicBuddy.CustomerDemographicBuddyMetadata))]
    public partial class CustomerDemographicBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the CustomerDemographic class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CustomerDemographicBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CustomerDemographicBuddyMetadata()
            {
            }
            
            public string CustomerDesc { get; set; }
            
            public EntityCollection<Customer> Customers { get; set; }
            
            public string CustomerTypeID { get; set; }
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
            
            public string Address { get; set; }
            
            public Nullable<DateTime> BirthDate { get; set; }
            
            public string City { get; set; }
            
            public string Country { get; set; }
            
            public Employee Employee1 { get; set; }
            
            public int EmployeeID { get; set; }
            
            public EntityCollection<Employee> Employees1 { get; set; }
            
            public string Extension { get; set; }
            
            public string FirstName { get; set; }
            
            public Nullable<DateTime> HireDate { get; set; }
            
            public string HomePhone { get; set; }
            
            public string LastName { get; set; }
            
            public string Notes { get; set; }
            
            public EntityCollection<Order> Orders { get; set; }
            
            public byte[] Photo { get; set; }
            
            public string PhotoPath { get; set; }
            
            public string PostalCode { get; set; }
            
            public string Region { get; set; }
            
            public Nullable<int> ReportsTo { get; set; }
            
            public EntityCollection<Territory> Territories { get; set; }
            
            public string Title { get; set; }
            
            public string TitleOfCourtesy { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ShipperBuddyMetadata as the class
    // that carries additional metadata for the ShipperBuddy class.
    [MetadataTypeAttribute(typeof(ShipperBuddy.ShipperBuddyMetadata))]
    public partial class ShipperBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Shipper class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ShipperBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ShipperBuddyMetadata()
            {
            }
            
            public string CompanyName { get; set; }
            
            public EntityCollection<Order> Orders { get; set; }
            
            public string Phone { get; set; }
            
            public int ShipperID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SupplierBuddyMetadata as the class
    // that carries additional metadata for the SupplierBuddy class.
    [MetadataTypeAttribute(typeof(SupplierBuddy.SupplierBuddyMetadata))]
    public partial class SupplierBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Supplier class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SupplierBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SupplierBuddyMetadata()
            {
            }
            
            public string Address { get; set; }
            
            public string City { get; set; }
            
            public string CompanyName { get; set; }
            
            public string ContactName { get; set; }
            
            public string ContactTitle { get; set; }
            
            public string Country { get; set; }
            
            public string Fax { get; set; }
            
            public string HomePage { get; set; }
            
            public string Phone { get; set; }
            
            public string PostalCode { get; set; }
            
            public EntityCollection<Product> Products { get; set; }
            
            public string Region { get; set; }
            
            public int SupplierID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies TerritoryBuddyMetadata as the class
    // that carries additional metadata for the TerritoryBuddy class.
    [MetadataTypeAttribute(typeof(TerritoryBuddy.TerritoryBuddyMetadata))]
    public partial class TerritoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Territory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class TerritoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private TerritoryBuddyMetadata()
            {
            }
            
            public EntityCollection<Employee> Employees { get; set; }
            
            public Region Region { get; set; }
            
            public int RegionID { get; set; }
            
            public string TerritoryDescription { get; set; }
            
            public string TerritoryID { get; set; }
        }
    }
}
