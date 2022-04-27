
namespace AdventureWorksModelBuddy
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Linq;
    using AdventureWorksModel;
    using OpenRiaServices.Server;
    
    
    // The MetadataTypeAttribute identifies AddressBuddyMetadata as the class
    // that carries additional metadata for the AddressBuddy class.
    [MetadataTypeAttribute(typeof(AddressBuddy.AddressBuddyMetadata))]
    public partial class AddressBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Address class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class AddressBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private AddressBuddyMetadata()
            {
            }
            
            public int AddressID { get; set; }
            
            public string AddressLine1 { get; set; }
            
            public string AddressLine2 { get; set; }
            
            public string City { get; set; }
            
            public EntityCollection<CustomerAddress> CustomerAddresses { get; set; }
            
            public EntityCollection<EmployeeAddress> EmployeeAddresses { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string PostalCode { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders1 { get; set; }
            
            public StateProvince StateProvince { get; set; }
            
            public int StateProvinceID { get; set; }
            
            public EntityCollection<VendorAddress> VendorAddresses { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies AddressTypeBuddyMetadata as the class
    // that carries additional metadata for the AddressTypeBuddy class.
    [MetadataTypeAttribute(typeof(AddressTypeBuddy.AddressTypeBuddyMetadata))]
    public partial class AddressTypeBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the AddressType class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class AddressTypeBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private AddressTypeBuddyMetadata()
            {
            }
            
            public int AddressTypeID { get; set; }
            
            public EntityCollection<CustomerAddress> CustomerAddresses { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<VendorAddress> VendorAddresses { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies AWBuildVersionBuddyMetadata as the class
    // that carries additional metadata for the AWBuildVersionBuddy class.
    [MetadataTypeAttribute(typeof(AWBuildVersionBuddy.AWBuildVersionBuddyMetadata))]
    public partial class AWBuildVersionBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the AWBuildVersion class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class AWBuildVersionBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private AWBuildVersionBuddyMetadata()
            {
            }
            
            public string Database_Version { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public byte SystemInformationID { get; set; }
            
            public DateTime VersionDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies BillOfMaterialBuddyMetadata as the class
    // that carries additional metadata for the BillOfMaterialBuddy class.
    [MetadataTypeAttribute(typeof(BillOfMaterialBuddy.BillOfMaterialBuddyMetadata))]
    public partial class BillOfMaterialBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the BillOfMaterial class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class BillOfMaterialBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private BillOfMaterialBuddyMetadata()
            {
            }
            
            public int BillOfMaterialsID { get; set; }
            
            public short BOMLevel { get; set; }
            
            public int ComponentID { get; set; }
            
            public Nullable<DateTime> EndDate { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public decimal PerAssemblyQty { get; set; }
            
            public Product Product { get; set; }
            
            public Product Product1 { get; set; }
            
            public Nullable<int> ProductAssemblyID { get; set; }
            
            public DateTime StartDate { get; set; }
            
            public UnitMeasure UnitMeasure { get; set; }
            
            public string UnitMeasureCode { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ContactBuddyMetadata as the class
    // that carries additional metadata for the ContactBuddy class.
    [MetadataTypeAttribute(typeof(ContactBuddy.ContactBuddyMetadata))]
    public partial class ContactBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Contact class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ContactBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ContactBuddyMetadata()
            {
            }
            
            public string AdditionalContactInfo { get; set; }
            
            public EntityCollection<ContactCreditCard> ContactCreditCards { get; set; }
            
            public int ContactID { get; set; }
            
            public string EmailAddress { get; set; }
            
            public int EmailPromotion { get; set; }
            
            public EntityCollection<Employee> Employees { get; set; }
            
            public string FirstName { get; set; }
            
            public EntityCollection<Individual> Individuals { get; set; }
            
            public string LastName { get; set; }
            
            public string MiddleName { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public bool NameStyle { get; set; }
            
            public string PasswordHash { get; set; }
            
            public string PasswordSalt { get; set; }
            
            public string Phone { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
            
            public EntityCollection<StoreContact> StoreContacts { get; set; }
            
            public string Suffix { get; set; }
            
            public string Title { get; set; }
            
            public EntityCollection<VendorContact> VendorContacts { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ContactCreditCardBuddyMetadata as the class
    // that carries additional metadata for the ContactCreditCardBuddy class.
    [MetadataTypeAttribute(typeof(ContactCreditCardBuddy.ContactCreditCardBuddyMetadata))]
    public partial class ContactCreditCardBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ContactCreditCard class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ContactCreditCardBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ContactCreditCardBuddyMetadata()
            {
            }
            
            public Contact Contact { get; set; }
            
            public int ContactID { get; set; }
            
            public CreditCard CreditCard { get; set; }
            
            public int CreditCardID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ContactTypeBuddyMetadata as the class
    // that carries additional metadata for the ContactTypeBuddy class.
    [MetadataTypeAttribute(typeof(ContactTypeBuddy.ContactTypeBuddyMetadata))]
    public partial class ContactTypeBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ContactType class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ContactTypeBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ContactTypeBuddyMetadata()
            {
            }
            
            public int ContactTypeID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public EntityCollection<StoreContact> StoreContacts { get; set; }
            
            public EntityCollection<VendorContact> VendorContacts { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CountryRegionBuddyMetadata as the class
    // that carries additional metadata for the CountryRegionBuddy class.
    [MetadataTypeAttribute(typeof(CountryRegionBuddy.CountryRegionBuddyMetadata))]
    public partial class CountryRegionBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the CountryRegion class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CountryRegionBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CountryRegionBuddyMetadata()
            {
            }
            
            public string CountryRegionCode { get; set; }
            
            public EntityCollection<CountryRegionCurrency> CountryRegionCurrencies { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public EntityCollection<StateProvince> StateProvinces { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CountryRegionCurrencyBuddyMetadata as the class
    // that carries additional metadata for the CountryRegionCurrencyBuddy class.
    [MetadataTypeAttribute(typeof(CountryRegionCurrencyBuddy.CountryRegionCurrencyBuddyMetadata))]
    public partial class CountryRegionCurrencyBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the CountryRegionCurrency class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CountryRegionCurrencyBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CountryRegionCurrencyBuddyMetadata()
            {
            }
            
            public CountryRegion CountryRegion { get; set; }
            
            public string CountryRegionCode { get; set; }
            
            public Currency Currency { get; set; }
            
            public string CurrencyCode { get; set; }
            
            public DateTime ModifiedDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CreditCardBuddyMetadata as the class
    // that carries additional metadata for the CreditCardBuddy class.
    [MetadataTypeAttribute(typeof(CreditCardBuddy.CreditCardBuddyMetadata))]
    public partial class CreditCardBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the CreditCard class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CreditCardBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CreditCardBuddyMetadata()
            {
            }
            
            public string CardNumber { get; set; }
            
            public string CardType { get; set; }
            
            public EntityCollection<ContactCreditCard> ContactCreditCards { get; set; }
            
            public int CreditCardID { get; set; }
            
            public byte ExpMonth { get; set; }
            
            public short ExpYear { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CultureBuddyMetadata as the class
    // that carries additional metadata for the CultureBuddy class.
    [MetadataTypeAttribute(typeof(CultureBuddy.CultureBuddyMetadata))]
    public partial class CultureBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Culture class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CultureBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CultureBuddyMetadata()
            {
            }
            
            public string CultureID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public EntityCollection<ProductModelProductDescriptionCulture> ProductModelProductDescriptionCultures { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CurrencyBuddyMetadata as the class
    // that carries additional metadata for the CurrencyBuddy class.
    [MetadataTypeAttribute(typeof(CurrencyBuddy.CurrencyBuddyMetadata))]
    public partial class CurrencyBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Currency class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CurrencyBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CurrencyBuddyMetadata()
            {
            }
            
            public EntityCollection<CountryRegionCurrency> CountryRegionCurrencies { get; set; }
            
            public string CurrencyCode { get; set; }
            
            public EntityCollection<CurrencyRate> CurrencyRates { get; set; }
            
            public EntityCollection<CurrencyRate> CurrencyRates1 { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CurrencyRateBuddyMetadata as the class
    // that carries additional metadata for the CurrencyRateBuddy class.
    [MetadataTypeAttribute(typeof(CurrencyRateBuddy.CurrencyRateBuddyMetadata))]
    public partial class CurrencyRateBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the CurrencyRate class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CurrencyRateBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CurrencyRateBuddyMetadata()
            {
            }
            
            public decimal AverageRate { get; set; }
            
            public Currency Currency { get; set; }
            
            public Currency Currency1 { get; set; }
            
            public DateTime CurrencyRateDate { get; set; }
            
            public int CurrencyRateID { get; set; }
            
            public decimal EndOfDayRate { get; set; }
            
            public string FromCurrencyCode { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
            
            public string ToCurrencyCode { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies CustomerAddressBuddyMetadata as the class
    // that carries additional metadata for the CustomerAddressBuddy class.
    [MetadataTypeAttribute(typeof(CustomerAddressBuddy.CustomerAddressBuddyMetadata))]
    public partial class CustomerAddressBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the CustomerAddress class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CustomerAddressBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CustomerAddressBuddyMetadata()
            {
            }
            
            public Address Address { get; set; }
            
            public int AddressID { get; set; }
            
            public AddressType AddressType { get; set; }
            
            public int AddressTypeID { get; set; }
            
            public Customer Customer { get; set; }
            
            public int CustomerID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Guid rowguid { get; set; }
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
            
            public string AccountNumber { get; set; }
            
            public EntityCollection<CustomerAddress> CustomerAddresses { get; set; }
            
            public int CustomerID { get; set; }
            
            public string CustomerType { get; set; }
            
            public Individual Individual { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
            
            public SalesTerritory SalesTerritory { get; set; }
            
            public Store Store { get; set; }
            
            public Nullable<int> TerritoryID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies DatabaseLogBuddyMetadata as the class
    // that carries additional metadata for the DatabaseLogBuddy class.
    [MetadataTypeAttribute(typeof(DatabaseLogBuddy.DatabaseLogBuddyMetadata))]
    public partial class DatabaseLogBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the DatabaseLog class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class DatabaseLogBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private DatabaseLogBuddyMetadata()
            {
            }
            
            public int DatabaseLogID { get; set; }
            
            public string DatabaseUser { get; set; }
            
            public string Event { get; set; }
            
            public string Object { get; set; }
            
            public DateTime PostTime { get; set; }
            
            public string Schema { get; set; }
            
            public string TSQL { get; set; }
            
            public string XmlEvent { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies DepartmentBuddyMetadata as the class
    // that carries additional metadata for the DepartmentBuddy class.
    [MetadataTypeAttribute(typeof(DepartmentBuddy.DepartmentBuddyMetadata))]
    public partial class DepartmentBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Department class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class DepartmentBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private DepartmentBuddyMetadata()
            {
            }
            
            public short DepartmentID { get; set; }
            
            public EntityCollection<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; }
            
            public string GroupName { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies DocumentBuddyMetadata as the class
    // that carries additional metadata for the DocumentBuddy class.
    [MetadataTypeAttribute(typeof(DocumentBuddy.DocumentBuddyMetadata))]
    public partial class DocumentBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Document class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class DocumentBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private DocumentBuddyMetadata()
            {
            }
            
            public int ChangeNumber { get; set; }
            
            public byte[] Document1 { get; set; }
            
            public int DocumentID { get; set; }
            
            public string DocumentSummary { get; set; }
            
            public string FileExtension { get; set; }
            
            public string FileName { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public EntityCollection<ProductDocument> ProductDocuments { get; set; }
            
            public string Revision { get; set; }
            
            public byte Status { get; set; }
            
            public string Title { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies EmployeeAddressBuddyMetadata as the class
    // that carries additional metadata for the EmployeeAddressBuddy class.
    [MetadataTypeAttribute(typeof(EmployeeAddressBuddy.EmployeeAddressBuddyMetadata))]
    public partial class EmployeeAddressBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the EmployeeAddress class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class EmployeeAddressBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private EmployeeAddressBuddyMetadata()
            {
            }
            
            public Address Address { get; set; }
            
            public int AddressID { get; set; }
            
            public Employee Employee { get; set; }
            
            public int EmployeeID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Guid rowguid { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies EmployeeDepartmentHistoryBuddyMetadata as the class
    // that carries additional metadata for the EmployeeDepartmentHistoryBuddy class.
    [MetadataTypeAttribute(typeof(EmployeeDepartmentHistoryBuddy.EmployeeDepartmentHistoryBuddyMetadata))]
    public partial class EmployeeDepartmentHistoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the EmployeeDepartmentHistory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class EmployeeDepartmentHistoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private EmployeeDepartmentHistoryBuddyMetadata()
            {
            }
            
            public Department Department { get; set; }
            
            public short DepartmentID { get; set; }
            
            public Employee Employee { get; set; }
            
            public int EmployeeID { get; set; }
            
            public Nullable<DateTime> EndDate { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Shift Shift { get; set; }
            
            public byte ShiftID { get; set; }
            
            public DateTime StartDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies EmployeePayHistoryBuddyMetadata as the class
    // that carries additional metadata for the EmployeePayHistoryBuddy class.
    [MetadataTypeAttribute(typeof(EmployeePayHistoryBuddy.EmployeePayHistoryBuddyMetadata))]
    public partial class EmployeePayHistoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the EmployeePayHistory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class EmployeePayHistoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private EmployeePayHistoryBuddyMetadata()
            {
            }
            
            public Employee Employee { get; set; }
            
            public int EmployeeID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public byte PayFrequency { get; set; }
            
            public decimal Rate { get; set; }
            
            public DateTime RateChangeDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ErrorLogBuddyMetadata as the class
    // that carries additional metadata for the ErrorLogBuddy class.
    [MetadataTypeAttribute(typeof(ErrorLogBuddy.ErrorLogBuddyMetadata))]
    public partial class ErrorLogBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ErrorLog class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ErrorLogBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ErrorLogBuddyMetadata()
            {
            }
            
            public Nullable<int> ErrorLine { get; set; }
            
            public int ErrorLogID { get; set; }
            
            public string ErrorMessage { get; set; }
            
            public int ErrorNumber { get; set; }
            
            public string ErrorProcedure { get; set; }
            
            public Nullable<int> ErrorSeverity { get; set; }
            
            public Nullable<int> ErrorState { get; set; }
            
            public DateTime ErrorTime { get; set; }
            
            public string UserName { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies IllustrationBuddyMetadata as the class
    // that carries additional metadata for the IllustrationBuddy class.
    [MetadataTypeAttribute(typeof(IllustrationBuddy.IllustrationBuddyMetadata))]
    public partial class IllustrationBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Illustration class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class IllustrationBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private IllustrationBuddyMetadata()
            {
            }
            
            public string Diagram { get; set; }
            
            public int IllustrationID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public EntityCollection<ProductModelIllustration> ProductModelIllustrations { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies IndividualBuddyMetadata as the class
    // that carries additional metadata for the IndividualBuddy class.
    [MetadataTypeAttribute(typeof(IndividualBuddy.IndividualBuddyMetadata))]
    public partial class IndividualBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Individual class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class IndividualBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private IndividualBuddyMetadata()
            {
            }
            
            public Contact Contact { get; set; }
            
            public int ContactID { get; set; }
            
            public Customer Customer { get; set; }
            
            public int CustomerID { get; set; }
            
            public string Demographics { get; set; }
            
            public DateTime ModifiedDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies JobCandidateBuddyMetadata as the class
    // that carries additional metadata for the JobCandidateBuddy class.
    [MetadataTypeAttribute(typeof(JobCandidateBuddy.JobCandidateBuddyMetadata))]
    public partial class JobCandidateBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the JobCandidate class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class JobCandidateBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private JobCandidateBuddyMetadata()
            {
            }
            
            public Employee Employee { get; set; }
            
            public Nullable<int> EmployeeID { get; set; }
            
            public int JobCandidateID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Resume { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies LocationBuddyMetadata as the class
    // that carries additional metadata for the LocationBuddy class.
    [MetadataTypeAttribute(typeof(LocationBuddy.LocationBuddyMetadata))]
    public partial class LocationBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Location class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class LocationBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private LocationBuddyMetadata()
            {
            }
            
            public decimal Availability { get; set; }
            
            public decimal CostRate { get; set; }
            
            public short LocationID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public EntityCollection<ProductInventory> ProductInventories { get; set; }
            
            public EntityCollection<WorkOrderRouting> WorkOrderRoutings { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductCategoryBuddyMetadata as the class
    // that carries additional metadata for the ProductCategoryBuddy class.
    [MetadataTypeAttribute(typeof(ProductCategoryBuddy.ProductCategoryBuddyMetadata))]
    public partial class ProductCategoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductCategory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductCategoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductCategoryBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public int ProductCategoryID { get; set; }
            
            public EntityCollection<ProductSubcategory> ProductSubcategories { get; set; }
            
            public Guid rowguid { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductCostHistoryBuddyMetadata as the class
    // that carries additional metadata for the ProductCostHistoryBuddy class.
    [MetadataTypeAttribute(typeof(ProductCostHistoryBuddy.ProductCostHistoryBuddyMetadata))]
    public partial class ProductCostHistoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductCostHistory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductCostHistoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductCostHistoryBuddyMetadata()
            {
            }
            
            public Nullable<DateTime> EndDate { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public decimal StandardCost { get; set; }
            
            public DateTime StartDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductDescriptionBuddyMetadata as the class
    // that carries additional metadata for the ProductDescriptionBuddy class.
    [MetadataTypeAttribute(typeof(ProductDescriptionBuddy.ProductDescriptionBuddyMetadata))]
    public partial class ProductDescriptionBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductDescription class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductDescriptionBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductDescriptionBuddyMetadata()
            {
            }
            
            public string Description { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public int ProductDescriptionID { get; set; }
            
            public EntityCollection<ProductModelProductDescriptionCulture> ProductModelProductDescriptionCultures { get; set; }
            
            public Guid rowguid { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductDocumentBuddyMetadata as the class
    // that carries additional metadata for the ProductDocumentBuddy class.
    [MetadataTypeAttribute(typeof(ProductDocumentBuddy.ProductDocumentBuddyMetadata))]
    public partial class ProductDocumentBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductDocument class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductDocumentBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductDocumentBuddyMetadata()
            {
            }
            
            public Document Document { get; set; }
            
            public int DocumentID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductInventoryBuddyMetadata as the class
    // that carries additional metadata for the ProductInventoryBuddy class.
    [MetadataTypeAttribute(typeof(ProductInventoryBuddy.ProductInventoryBuddyMetadata))]
    public partial class ProductInventoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductInventory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductInventoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductInventoryBuddyMetadata()
            {
            }
            
            public byte Bin { get; set; }
            
            public Location Location { get; set; }
            
            public short LocationID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public short Quantity { get; set; }
            
            public Guid rowguid { get; set; }
            
            public string Shelf { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductListPriceHistoryBuddyMetadata as the class
    // that carries additional metadata for the ProductListPriceHistoryBuddy class.
    [MetadataTypeAttribute(typeof(ProductListPriceHistoryBuddy.ProductListPriceHistoryBuddyMetadata))]
    public partial class ProductListPriceHistoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductListPriceHistory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductListPriceHistoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductListPriceHistoryBuddyMetadata()
            {
            }
            
            public Nullable<DateTime> EndDate { get; set; }
            
            public decimal ListPrice { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public DateTime StartDate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductModelBuddyMetadata as the class
    // that carries additional metadata for the ProductModelBuddy class.
    [MetadataTypeAttribute(typeof(ProductModelBuddy.ProductModelBuddyMetadata))]
    public partial class ProductModelBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductModel class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductModelBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductModelBuddyMetadata()
            {
            }
            
            public string CatalogDescription { get; set; }
            
            public string Instructions { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public int ProductModelID { get; set; }
            
            public EntityCollection<ProductModelIllustration> ProductModelIllustrations { get; set; }
            
            public EntityCollection<ProductModelProductDescriptionCulture> ProductModelProductDescriptionCultures { get; set; }
            
            public EntityCollection<Product> Products { get; set; }
            
            public Guid rowguid { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductModelIllustrationBuddyMetadata as the class
    // that carries additional metadata for the ProductModelIllustrationBuddy class.
    [MetadataTypeAttribute(typeof(ProductModelIllustrationBuddy.ProductModelIllustrationBuddyMetadata))]
    public partial class ProductModelIllustrationBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductModelIllustration class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductModelIllustrationBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductModelIllustrationBuddyMetadata()
            {
            }
            
            public Illustration Illustration { get; set; }
            
            public int IllustrationID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public ProductModel ProductModel { get; set; }
            
            public int ProductModelID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductModelProductDescriptionCultureBuddyMetadata as the class
    // that carries additional metadata for the ProductModelProductDescriptionCultureBuddy class.
    [MetadataTypeAttribute(typeof(ProductModelProductDescriptionCultureBuddy.ProductModelProductDescriptionCultureBuddyMetadata))]
    public partial class ProductModelProductDescriptionCultureBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductModelProductDescriptionCulture class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductModelProductDescriptionCultureBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductModelProductDescriptionCultureBuddyMetadata()
            {
            }
            
            public Culture Culture { get; set; }
            
            public string CultureID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public ProductDescription ProductDescription { get; set; }
            
            public int ProductDescriptionID { get; set; }
            
            public ProductModel ProductModel { get; set; }
            
            public int ProductModelID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductPhotoBuddyMetadata as the class
    // that carries additional metadata for the ProductPhotoBuddy class.
    [MetadataTypeAttribute(typeof(ProductPhotoBuddy.ProductPhotoBuddyMetadata))]
    public partial class ProductPhotoBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductPhoto class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductPhotoBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductPhotoBuddyMetadata()
            {
            }
            
            public byte[] LargePhoto { get; set; }
            
            public string LargePhotoFileName { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public int ProductPhotoID { get; set; }
            
            public EntityCollection<ProductProductPhoto> ProductProductPhotoes { get; set; }
            
            public byte[] ThumbNailPhoto { get; set; }
            
            public string ThumbnailPhotoFileName { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductProductPhotoBuddyMetadata as the class
    // that carries additional metadata for the ProductProductPhotoBuddy class.
    [MetadataTypeAttribute(typeof(ProductProductPhotoBuddy.ProductProductPhotoBuddyMetadata))]
    public partial class ProductProductPhotoBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductProductPhoto class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductProductPhotoBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductProductPhotoBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public bool Primary { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public ProductPhoto ProductPhoto { get; set; }
            
            public int ProductPhotoID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductReviewBuddyMetadata as the class
    // that carries additional metadata for the ProductReviewBuddy class.
    [MetadataTypeAttribute(typeof(ProductReviewBuddy.ProductReviewBuddyMetadata))]
    public partial class ProductReviewBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductReview class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductReviewBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductReviewBuddyMetadata()
            {
            }
            
            public string Comments { get; set; }
            
            public string EmailAddress { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public int ProductReviewID { get; set; }
            
            public int Rating { get; set; }
            
            public DateTime ReviewDate { get; set; }
            
            public string ReviewerName { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductSubcategoryBuddyMetadata as the class
    // that carries additional metadata for the ProductSubcategoryBuddy class.
    [MetadataTypeAttribute(typeof(ProductSubcategoryBuddy.ProductSubcategoryBuddyMetadata))]
    public partial class ProductSubcategoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductSubcategory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductSubcategoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductSubcategoryBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public ProductCategory ProductCategory { get; set; }
            
            public int ProductCategoryID { get; set; }
            
            public EntityCollection<Product> Products { get; set; }
            
            public int ProductSubcategoryID { get; set; }
            
            public Guid rowguid { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductVendorBuddyMetadata as the class
    // that carries additional metadata for the ProductVendorBuddy class.
    [MetadataTypeAttribute(typeof(ProductVendorBuddy.ProductVendorBuddyMetadata))]
    public partial class ProductVendorBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ProductVendor class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductVendorBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductVendorBuddyMetadata()
            {
            }
            
            public int AverageLeadTime { get; set; }
            
            public Nullable<decimal> LastReceiptCost { get; set; }
            
            public Nullable<DateTime> LastReceiptDate { get; set; }
            
            public int MaxOrderQty { get; set; }
            
            public int MinOrderQty { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Nullable<int> OnOrderQty { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public decimal StandardPrice { get; set; }
            
            public UnitMeasure UnitMeasure { get; set; }
            
            public string UnitMeasureCode { get; set; }
            
            public Vendor Vendor { get; set; }
            
            public int VendorID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesOrderDetailBuddyMetadata as the class
    // that carries additional metadata for the SalesOrderDetailBuddy class.
    [MetadataTypeAttribute(typeof(SalesOrderDetailBuddy.SalesOrderDetailBuddyMetadata))]
    public partial class SalesOrderDetailBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesOrderDetail class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesOrderDetailBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesOrderDetailBuddyMetadata()
            {
            }
            
            public string CarrierTrackingNumber { get; set; }
            
            public decimal LineTotal { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public short OrderQty { get; set; }
            
            public int ProductID { get; set; }
            
            public Guid rowguid { get; set; }
            
            public int SalesOrderDetailID { get; set; }
            
            public SalesOrderHeader SalesOrderHeader { get; set; }
            
            public int SalesOrderID { get; set; }
            
            public int SpecialOfferID { get; set; }
            
            public SpecialOfferProduct SpecialOfferProduct { get; set; }
            
            public decimal UnitPrice { get; set; }
            
            public decimal UnitPriceDiscount { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesOrderHeaderBuddyMetadata as the class
    // that carries additional metadata for the SalesOrderHeaderBuddy class.
    [MetadataTypeAttribute(typeof(SalesOrderHeaderBuddy.SalesOrderHeaderBuddyMetadata))]
    public partial class SalesOrderHeaderBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesOrderHeader class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesOrderHeaderBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesOrderHeaderBuddyMetadata()
            {
            }
            
            public string AccountNumber { get; set; }
            
            public Address Address { get; set; }
            
            public Address Address1 { get; set; }
            
            public int BillToAddressID { get; set; }
            
            public string Comment { get; set; }
            
            public Contact Contact { get; set; }
            
            public int ContactID { get; set; }
            
            public CreditCard CreditCard { get; set; }
            
            public string CreditCardApprovalCode { get; set; }
            
            public Nullable<int> CreditCardID { get; set; }
            
            public CurrencyRate CurrencyRate { get; set; }
            
            public Nullable<int> CurrencyRateID { get; set; }
            
            public Customer Customer { get; set; }
            
            public int CustomerID { get; set; }
            
            public DateTime DueDate { get; set; }
            
            public decimal Freight { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public bool OnlineOrderFlag { get; set; }
            
            public DateTime OrderDate { get; set; }
            
            public string PurchaseOrderNumber { get; set; }
            
            public byte RevisionNumber { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<SalesOrderDetail> SalesOrderDetails { get; set; }
            
            public EntityCollection<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReasons { get; set; }
            
            public int SalesOrderID { get; set; }
            
            public string SalesOrderNumber { get; set; }
            
            public SalesPerson SalesPerson { get; set; }
            
            public Nullable<int> SalesPersonID { get; set; }
            
            public SalesTerritory SalesTerritory { get; set; }
            
            public Nullable<DateTime> ShipDate { get; set; }
            
            public ShipMethod ShipMethod { get; set; }
            
            public int ShipMethodID { get; set; }
            
            public int ShipToAddressID { get; set; }
            
            public byte Status { get; set; }
            
            public decimal SubTotal { get; set; }
            
            public decimal TaxAmt { get; set; }
            
            public Nullable<int> TerritoryID { get; set; }
            
            public decimal TotalDue { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesOrderHeaderSalesReasonBuddyMetadata as the class
    // that carries additional metadata for the SalesOrderHeaderSalesReasonBuddy class.
    [MetadataTypeAttribute(typeof(SalesOrderHeaderSalesReasonBuddy.SalesOrderHeaderSalesReasonBuddyMetadata))]
    public partial class SalesOrderHeaderSalesReasonBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesOrderHeaderSalesReason class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesOrderHeaderSalesReasonBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesOrderHeaderSalesReasonBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public SalesOrderHeader SalesOrderHeader { get; set; }
            
            public int SalesOrderID { get; set; }
            
            public SalesReason SalesReason { get; set; }
            
            public int SalesReasonID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesPersonBuddyMetadata as the class
    // that carries additional metadata for the SalesPersonBuddy class.
    [MetadataTypeAttribute(typeof(SalesPersonBuddy.SalesPersonBuddyMetadata))]
    public partial class SalesPersonBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesPerson class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesPersonBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesPersonBuddyMetadata()
            {
            }
            
            public decimal Bonus { get; set; }
            
            public decimal CommissionPct { get; set; }
            
            public Employee Employee { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Guid rowguid { get; set; }
            
            public decimal SalesLastYear { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
            
            public int SalesPersonID { get; set; }
            
            public EntityCollection<SalesPersonQuotaHistory> SalesPersonQuotaHistories { get; set; }
            
            public Nullable<decimal> SalesQuota { get; set; }
            
            public SalesTerritory SalesTerritory { get; set; }
            
            public EntityCollection<SalesTerritoryHistory> SalesTerritoryHistories { get; set; }
            
            public decimal SalesYTD { get; set; }
            
            public EntityCollection<Store> Stores { get; set; }
            
            public Nullable<int> TerritoryID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesPersonQuotaHistoryBuddyMetadata as the class
    // that carries additional metadata for the SalesPersonQuotaHistoryBuddy class.
    [MetadataTypeAttribute(typeof(SalesPersonQuotaHistoryBuddy.SalesPersonQuotaHistoryBuddyMetadata))]
    public partial class SalesPersonQuotaHistoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesPersonQuotaHistory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesPersonQuotaHistoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesPersonQuotaHistoryBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public DateTime QuotaDate { get; set; }
            
            public Guid rowguid { get; set; }
            
            public SalesPerson SalesPerson { get; set; }
            
            public int SalesPersonID { get; set; }
            
            public decimal SalesQuota { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesReasonBuddyMetadata as the class
    // that carries additional metadata for the SalesReasonBuddy class.
    [MetadataTypeAttribute(typeof(SalesReasonBuddy.SalesReasonBuddyMetadata))]
    public partial class SalesReasonBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesReason class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesReasonBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesReasonBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public string ReasonType { get; set; }
            
            public EntityCollection<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReasons { get; set; }
            
            public int SalesReasonID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesTaxRateBuddyMetadata as the class
    // that carries additional metadata for the SalesTaxRateBuddy class.
    [MetadataTypeAttribute(typeof(SalesTaxRateBuddy.SalesTaxRateBuddyMetadata))]
    public partial class SalesTaxRateBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesTaxRate class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesTaxRateBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesTaxRateBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public Guid rowguid { get; set; }
            
            public int SalesTaxRateID { get; set; }
            
            public StateProvince StateProvince { get; set; }
            
            public int StateProvinceID { get; set; }
            
            public decimal TaxRate { get; set; }
            
            public byte TaxType { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesTerritoryBuddyMetadata as the class
    // that carries additional metadata for the SalesTerritoryBuddy class.
    [MetadataTypeAttribute(typeof(SalesTerritoryBuddy.SalesTerritoryBuddyMetadata))]
    public partial class SalesTerritoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesTerritory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesTerritoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesTerritoryBuddyMetadata()
            {
            }
            
            public decimal CostLastYear { get; set; }
            
            public decimal CostYTD { get; set; }
            
            public string CountryRegionCode { get; set; }
            
            public EntityCollection<Customer> Customers { get; set; }
            
            public string Group { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public Guid rowguid { get; set; }
            
            public decimal SalesLastYear { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
            
            public EntityCollection<SalesPerson> SalesPersons { get; set; }
            
            public EntityCollection<SalesTerritoryHistory> SalesTerritoryHistories { get; set; }
            
            public decimal SalesYTD { get; set; }
            
            public EntityCollection<StateProvince> StateProvinces { get; set; }
            
            public int TerritoryID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SalesTerritoryHistoryBuddyMetadata as the class
    // that carries additional metadata for the SalesTerritoryHistoryBuddy class.
    [MetadataTypeAttribute(typeof(SalesTerritoryHistoryBuddy.SalesTerritoryHistoryBuddyMetadata))]
    public partial class SalesTerritoryHistoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SalesTerritoryHistory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SalesTerritoryHistoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SalesTerritoryHistoryBuddyMetadata()
            {
            }
            
            public Nullable<DateTime> EndDate { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Guid rowguid { get; set; }
            
            public SalesPerson SalesPerson { get; set; }
            
            public int SalesPersonID { get; set; }
            
            public SalesTerritory SalesTerritory { get; set; }
            
            public DateTime StartDate { get; set; }
            
            public int TerritoryID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ScrapReasonBuddyMetadata as the class
    // that carries additional metadata for the ScrapReasonBuddy class.
    [MetadataTypeAttribute(typeof(ScrapReasonBuddy.ScrapReasonBuddyMetadata))]
    public partial class ScrapReasonBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ScrapReason class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ScrapReasonBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ScrapReasonBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public short ScrapReasonID { get; set; }
            
            public EntityCollection<WorkOrder> WorkOrders { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ShiftBuddyMetadata as the class
    // that carries additional metadata for the ShiftBuddy class.
    [MetadataTypeAttribute(typeof(ShiftBuddy.ShiftBuddyMetadata))]
    public partial class ShiftBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Shift class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ShiftBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ShiftBuddyMetadata()
            {
            }
            
            public EntityCollection<EmployeeDepartmentHistory> EmployeeDepartmentHistories { get; set; }
            
            public DateTime EndTime { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public byte ShiftID { get; set; }
            
            public DateTime StartTime { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ShipMethodBuddyMetadata as the class
    // that carries additional metadata for the ShipMethodBuddy class.
    [MetadataTypeAttribute(typeof(ShipMethodBuddy.ShipMethodBuddyMetadata))]
    public partial class ShipMethodBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ShipMethod class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ShipMethodBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ShipMethodBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public EntityCollection<PurchaseOrder> PurchaseOrderHeaders { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<SalesOrderHeader> SalesOrderHeaders { get; set; }
            
            public decimal ShipBase { get; set; }
            
            public int ShipMethodID { get; set; }
            
            public decimal ShipRate { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ShoppingCartItemBuddyMetadata as the class
    // that carries additional metadata for the ShoppingCartItemBuddy class.
    [MetadataTypeAttribute(typeof(ShoppingCartItemBuddy.ShoppingCartItemBuddyMetadata))]
    public partial class ShoppingCartItemBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the ShoppingCartItem class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ShoppingCartItemBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ShoppingCartItemBuddyMetadata()
            {
            }
            
            public DateTime DateCreated { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public int Quantity { get; set; }
            
            public string ShoppingCartID { get; set; }
            
            public int ShoppingCartItemID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SpecialOfferBuddyMetadata as the class
    // that carries additional metadata for the SpecialOfferBuddy class.
    [MetadataTypeAttribute(typeof(SpecialOfferBuddy.SpecialOfferBuddyMetadata))]
    public partial class SpecialOfferBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SpecialOffer class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SpecialOfferBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SpecialOfferBuddyMetadata()
            {
            }
            
            public string Category { get; set; }
            
            public string Description { get; set; }
            
            public decimal DiscountPct { get; set; }
            
            public DateTime EndDate { get; set; }
            
            public Nullable<int> MaxQty { get; set; }
            
            public int MinQty { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Guid rowguid { get; set; }
            
            public int SpecialOfferID { get; set; }
            
            public EntityCollection<SpecialOfferProduct> SpecialOfferProducts { get; set; }
            
            public DateTime StartDate { get; set; }
            
            public string Type { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies SpecialOfferProductBuddyMetadata as the class
    // that carries additional metadata for the SpecialOfferProductBuddy class.
    [MetadataTypeAttribute(typeof(SpecialOfferProductBuddy.SpecialOfferProductBuddyMetadata))]
    public partial class SpecialOfferProductBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the SpecialOfferProduct class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class SpecialOfferProductBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private SpecialOfferProductBuddyMetadata()
            {
            }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<SalesOrderDetail> SalesOrderDetails { get; set; }
            
            public SpecialOffer SpecialOffer { get; set; }
            
            public int SpecialOfferID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies StateProvinceBuddyMetadata as the class
    // that carries additional metadata for the StateProvinceBuddy class.
    [MetadataTypeAttribute(typeof(StateProvinceBuddy.StateProvinceBuddyMetadata))]
    public partial class StateProvinceBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the StateProvince class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class StateProvinceBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private StateProvinceBuddyMetadata()
            {
            }
            
            public EntityCollection<Address> Addresses { get; set; }
            
            public CountryRegion CountryRegion { get; set; }
            
            public string CountryRegionCode { get; set; }
            
            public bool IsOnlyStateProvinceFlag { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public Guid rowguid { get; set; }
            
            public EntityCollection<SalesTaxRate> SalesTaxRates { get; set; }
            
            public SalesTerritory SalesTerritory { get; set; }
            
            public string StateProvinceCode { get; set; }
            
            public int StateProvinceID { get; set; }
            
            public int TerritoryID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies StoreBuddyMetadata as the class
    // that carries additional metadata for the StoreBuddy class.
    [MetadataTypeAttribute(typeof(StoreBuddy.StoreBuddyMetadata))]
    public partial class StoreBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Store class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class StoreBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private StoreBuddyMetadata()
            {
            }
            
            public Customer Customer { get; set; }
            
            public int CustomerID { get; set; }
            
            public string Demographics { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public Guid rowguid { get; set; }
            
            public SalesPerson SalesPerson { get; set; }
            
            public Nullable<int> SalesPersonID { get; set; }
            
            public EntityCollection<StoreContact> StoreContacts { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies StoreContactBuddyMetadata as the class
    // that carries additional metadata for the StoreContactBuddy class.
    [MetadataTypeAttribute(typeof(StoreContactBuddy.StoreContactBuddyMetadata))]
    public partial class StoreContactBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the StoreContact class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class StoreContactBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private StoreContactBuddyMetadata()
            {
            }
            
            public Contact Contact { get; set; }
            
            public int ContactID { get; set; }
            
            public ContactType ContactType { get; set; }
            
            public int ContactTypeID { get; set; }
            
            public int CustomerID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Guid rowguid { get; set; }
            
            public Store Store { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies sysdiagramBuddyMetadata as the class
    // that carries additional metadata for the sysdiagramBuddy class.
    [MetadataTypeAttribute(typeof(sysdiagramBuddy.sysdiagramBuddyMetadata))]
    public partial class sysdiagramBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the sysdiagram class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class sysdiagramBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private sysdiagramBuddyMetadata()
            {
            }
            
            public byte[] definition { get; set; }
            
            public int diagram_id { get; set; }
            
            public string name { get; set; }
            
            public int principal_id { get; set; }
            
            public Nullable<int> version { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies TransactionHistoryArchiveBuddyMetadata as the class
    // that carries additional metadata for the TransactionHistoryArchiveBuddy class.
    [MetadataTypeAttribute(typeof(TransactionHistoryArchiveBuddy.TransactionHistoryArchiveBuddyMetadata))]
    public partial class TransactionHistoryArchiveBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the TransactionHistoryArchive class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class TransactionHistoryArchiveBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private TransactionHistoryArchiveBuddyMetadata()
            {
            }
            
            public decimal ActualCost { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public int ProductID { get; set; }
            
            public int Quantity { get; set; }
            
            public int ReferenceOrderID { get; set; }
            
            public int ReferenceOrderLineID { get; set; }
            
            public DateTime TransactionDate { get; set; }
            
            public int TransactionID { get; set; }
            
            public string TransactionType { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies TransactionHistoryBuddyMetadata as the class
    // that carries additional metadata for the TransactionHistoryBuddy class.
    [MetadataTypeAttribute(typeof(TransactionHistoryBuddy.TransactionHistoryBuddyMetadata))]
    public partial class TransactionHistoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the TransactionHistory class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class TransactionHistoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private TransactionHistoryBuddyMetadata()
            {
            }
            
            public decimal ActualCost { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public int Quantity { get; set; }
            
            public int ReferenceOrderID { get; set; }
            
            public int ReferenceOrderLineID { get; set; }
            
            public DateTime TransactionDate { get; set; }
            
            public int TransactionID { get; set; }
            
            public string TransactionType { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies UnitMeasureBuddyMetadata as the class
    // that carries additional metadata for the UnitMeasureBuddy class.
    [MetadataTypeAttribute(typeof(UnitMeasureBuddy.UnitMeasureBuddyMetadata))]
    public partial class UnitMeasureBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the UnitMeasure class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class UnitMeasureBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private UnitMeasureBuddyMetadata()
            {
            }
            
            public EntityCollection<BillOfMaterial> BillOfMaterials { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public EntityCollection<Product> Products { get; set; }
            
            public EntityCollection<Product> Products1 { get; set; }
            
            public EntityCollection<ProductVendor> ProductVendors { get; set; }
            
            public string UnitMeasureCode { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies VendorAddressBuddyMetadata as the class
    // that carries additional metadata for the VendorAddressBuddy class.
    [MetadataTypeAttribute(typeof(VendorAddressBuddy.VendorAddressBuddyMetadata))]
    public partial class VendorAddressBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the VendorAddress class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class VendorAddressBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private VendorAddressBuddyMetadata()
            {
            }
            
            public Address Address { get; set; }
            
            public int AddressID { get; set; }
            
            public AddressType AddressType { get; set; }
            
            public int AddressTypeID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Vendor Vendor { get; set; }
            
            public int VendorID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies VendorBuddyMetadata as the class
    // that carries additional metadata for the VendorBuddy class.
    [MetadataTypeAttribute(typeof(VendorBuddy.VendorBuddyMetadata))]
    public partial class VendorBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Vendor class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class VendorBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private VendorBuddyMetadata()
            {
            }
            
            public string AccountNumber { get; set; }
            
            public bool ActiveFlag { get; set; }
            
            public byte CreditRating { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public string Name { get; set; }
            
            public bool PreferredVendorStatus { get; set; }
            
            public EntityCollection<ProductVendor> ProductVendors { get; set; }
            
            public EntityCollection<PurchaseOrder> PurchaseOrderHeaders { get; set; }
            
            public string PurchasingWebServiceURL { get; set; }
            
            public EntityCollection<VendorAddress> VendorAddresses { get; set; }
            
            public EntityCollection<VendorContact> VendorContacts { get; set; }
            
            public int VendorID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies VendorContactBuddyMetadata as the class
    // that carries additional metadata for the VendorContactBuddy class.
    [MetadataTypeAttribute(typeof(VendorContactBuddy.VendorContactBuddyMetadata))]
    public partial class VendorContactBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the VendorContact class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class VendorContactBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private VendorContactBuddyMetadata()
            {
            }
            
            public Contact Contact { get; set; }
            
            public int ContactID { get; set; }
            
            public ContactType ContactType { get; set; }
            
            public int ContactTypeID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public Vendor Vendor { get; set; }
            
            public int VendorID { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies WorkOrderBuddyMetadata as the class
    // that carries additional metadata for the WorkOrderBuddy class.
    [MetadataTypeAttribute(typeof(WorkOrderBuddy.WorkOrderBuddyMetadata))]
    public partial class WorkOrderBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the WorkOrder class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class WorkOrderBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private WorkOrderBuddyMetadata()
            {
            }
            
            public DateTime DueDate { get; set; }
            
            public Nullable<DateTime> EndDate { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public int OrderQty { get; set; }
            
            public Product Product { get; set; }
            
            public int ProductID { get; set; }
            
            public short ScrappedQty { get; set; }
            
            public ScrapReason ScrapReason { get; set; }
            
            public Nullable<short> ScrapReasonID { get; set; }
            
            public DateTime StartDate { get; set; }
            
            public int StockedQty { get; set; }
            
            public int WorkOrderID { get; set; }
            
            public EntityCollection<WorkOrderRouting> WorkOrderRoutings { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies WorkOrderRoutingBuddyMetadata as the class
    // that carries additional metadata for the WorkOrderRoutingBuddy class.
    [MetadataTypeAttribute(typeof(WorkOrderRoutingBuddy.WorkOrderRoutingBuddyMetadata))]
    public partial class WorkOrderRoutingBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the WorkOrderRouting class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class WorkOrderRoutingBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private WorkOrderRoutingBuddyMetadata()
            {
            }
            
            public Nullable<decimal> ActualCost { get; set; }
            
            public Nullable<DateTime> ActualEndDate { get; set; }
            
            public Nullable<decimal> ActualResourceHrs { get; set; }
            
            public Nullable<DateTime> ActualStartDate { get; set; }
            
            public Location Location { get; set; }
            
            public short LocationID { get; set; }
            
            public DateTime ModifiedDate { get; set; }
            
            public short OperationSequence { get; set; }
            
            public decimal PlannedCost { get; set; }
            
            public int ProductID { get; set; }
            
            public DateTime ScheduledEndDate { get; set; }
            
            public DateTime ScheduledStartDate { get; set; }
            
            public WorkOrder WorkOrder { get; set; }
            
            public int WorkOrderID { get; set; }
        }
    }
}
