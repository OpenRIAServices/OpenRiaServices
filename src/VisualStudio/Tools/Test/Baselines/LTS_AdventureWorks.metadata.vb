
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.AdventureWorks.LTS
Imports OpenRiaServices.Hosting
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Linq
Imports System.Linq
Imports System.Xml.Linq

Namespace DataTests.AdventureWorks.LTSBuddy
    
    'The MetadataTypeAttribute identifies AddressBuddyMetadata as the class
    ' that carries additional metadata for the AddressBuddy class.
    <MetadataTypeAttribute(GetType(AddressBuddy.AddressBuddyMetadata))>  _
    Partial Public Class AddressBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Address class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class AddressBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AddressID As Integer
            
            Public Property AddressLine1 As String
            
            Public Property AddressLine2 As String
            
            Public Property City As String
            
            Public Property CustomerAddresses As EntitySet(Of CustomerAddress)
            
            Public Property EmployeeAddresses As EntitySet(Of EmployeeAddress)
            
            Public Property ModifiedDate As DateTime
            
            Public Property PostalCode As String
            
            Public Property rowguid As Guid
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
            
            Public Property SalesOrderHeaders1 As EntitySet(Of SalesOrderHeader)
            
            Public Property StateProvince As StateProvince
            
            Public Property StateProvinceID As Integer
            
            Public Property VendorAddresses As EntitySet(Of VendorAddress)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies AddressTypeBuddyMetadata as the class
    ' that carries additional metadata for the AddressTypeBuddy class.
    <MetadataTypeAttribute(GetType(AddressTypeBuddy.AddressTypeBuddyMetadata))>  _
    Partial Public Class AddressTypeBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the AddressType class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class AddressTypeBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AddressTypeID As Integer
            
            Public Property CustomerAddresses As EntitySet(Of CustomerAddress)
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property rowguid As Guid
            
            Public Property VendorAddresses As EntitySet(Of VendorAddress)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies BillOfMaterialBuddyMetadata as the class
    ' that carries additional metadata for the BillOfMaterialBuddy class.
    <MetadataTypeAttribute(GetType(BillOfMaterialBuddy.BillOfMaterialBuddyMetadata))>  _
    Partial Public Class BillOfMaterialBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the BillOfMaterial class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class BillOfMaterialBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property BillOfMaterialsID As Integer
            
            Public Property BOMLevel As Short
            
            Public Property ComponentID As Integer
            
            Public Property EndDate As Nullable(Of DateTime)
            
            Public Property ModifiedDate As DateTime
            
            Public Property PerAssemblyQty As Decimal
            
            Public Property Product As Product
            
            Public Property Product1 As Product
            
            Public Property ProductAssemblyID As Nullable(Of Integer)
            
            Public Property StartDate As DateTime
            
            Public Property UnitMeasure As UnitMeasure
            
            Public Property UnitMeasureCode As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ContactBuddyMetadata as the class
    ' that carries additional metadata for the ContactBuddy class.
    <MetadataTypeAttribute(GetType(ContactBuddy.ContactBuddyMetadata))>  _
    Partial Public Class ContactBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Contact class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ContactBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AdditionalContactInfo As XElement
            
            Public Property ContactCreditCards As EntitySet(Of ContactCreditCard)
            
            Public Property ContactID As Integer
            
            Public Property EmailAddress As String
            
            Public Property EmailPromotion As Integer
            
            Public Property Employees As EntitySet(Of Employee)
            
            Public Property FirstName As String
            
            Public Property Individuals As EntitySet(Of Individual)
            
            Public Property LastName As String
            
            Public Property MiddleName As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property NameStyle As Boolean
            
            Public Property PasswordHash As String
            
            Public Property PasswordSalt As String
            
            Public Property Phone As String
            
            Public Property rowguid As Guid
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
            
            Public Property StoreContacts As EntitySet(Of StoreContact)
            
            Public Property Suffix As String
            
            Public Property Title As String
            
            Public Property VendorContacts As EntitySet(Of VendorContact)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ContactCreditCardBuddyMetadata as the class
    ' that carries additional metadata for the ContactCreditCardBuddy class.
    <MetadataTypeAttribute(GetType(ContactCreditCardBuddy.ContactCreditCardBuddyMetadata))>  _
    Partial Public Class ContactCreditCardBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ContactCreditCard class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ContactCreditCardBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Contact As Contact
            
            Public Property ContactID As Integer
            
            Public Property CreditCard As CreditCard
            
            Public Property CreditCardID As Integer
            
            Public Property ModifiedDate As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ContactTypeBuddyMetadata as the class
    ' that carries additional metadata for the ContactTypeBuddy class.
    <MetadataTypeAttribute(GetType(ContactTypeBuddy.ContactTypeBuddyMetadata))>  _
    Partial Public Class ContactTypeBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ContactType class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ContactTypeBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ContactTypeID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property StoreContacts As EntitySet(Of StoreContact)
            
            Public Property VendorContacts As EntitySet(Of VendorContact)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CountryRegionBuddyMetadata as the class
    ' that carries additional metadata for the CountryRegionBuddy class.
    <MetadataTypeAttribute(GetType(CountryRegionBuddy.CountryRegionBuddyMetadata))>  _
    Partial Public Class CountryRegionBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the CountryRegion class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CountryRegionBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CountryRegionCode As String
            
            Public Property CountryRegionCurrencies As EntitySet(Of CountryRegionCurrency)
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property StateProvinces As EntitySet(Of StateProvince)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CountryRegionCurrencyBuddyMetadata as the class
    ' that carries additional metadata for the CountryRegionCurrencyBuddy class.
    <MetadataTypeAttribute(GetType(CountryRegionCurrencyBuddy.CountryRegionCurrencyBuddyMetadata))>  _
    Partial Public Class CountryRegionCurrencyBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the CountryRegionCurrency class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CountryRegionCurrencyBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CountryRegion As CountryRegion
            
            Public Property CountryRegionCode As String
            
            Public Property Currency As Currency
            
            Public Property CurrencyCode As String
            
            Public Property ModifiedDate As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CreditCardBuddyMetadata as the class
    ' that carries additional metadata for the CreditCardBuddy class.
    <MetadataTypeAttribute(GetType(CreditCardBuddy.CreditCardBuddyMetadata))>  _
    Partial Public Class CreditCardBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the CreditCard class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CreditCardBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CardNumber As String
            
            Public Property CardType As String
            
            Public Property ContactCreditCards As EntitySet(Of ContactCreditCard)
            
            Public Property CreditCardID As Integer
            
            Public Property ExpMonth As Byte
            
            Public Property ExpYear As Short
            
            Public Property ModifiedDate As DateTime
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CultureBuddyMetadata as the class
    ' that carries additional metadata for the CultureBuddy class.
    <MetadataTypeAttribute(GetType(CultureBuddy.CultureBuddyMetadata))>  _
    Partial Public Class CultureBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Culture class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CultureBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CultureID As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ProductModelProductDescriptionCultures As EntitySet(Of ProductModelProductDescriptionCulture)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CurrencyBuddyMetadata as the class
    ' that carries additional metadata for the CurrencyBuddy class.
    <MetadataTypeAttribute(GetType(CurrencyBuddy.CurrencyBuddyMetadata))>  _
    Partial Public Class CurrencyBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Currency class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CurrencyBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CountryRegionCurrencies As EntitySet(Of CountryRegionCurrency)
            
            Public Property CurrencyCode As String
            
            Public Property CurrencyRates As EntitySet(Of CurrencyRate)
            
            Public Property CurrencyRates1 As EntitySet(Of CurrencyRate)
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CurrencyRateBuddyMetadata as the class
    ' that carries additional metadata for the CurrencyRateBuddy class.
    <MetadataTypeAttribute(GetType(CurrencyRateBuddy.CurrencyRateBuddyMetadata))>  _
    Partial Public Class CurrencyRateBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the CurrencyRate class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CurrencyRateBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AverageRate As Decimal
            
            Public Property Currency As Currency
            
            Public Property Currency1 As Currency
            
            Public Property CurrencyRateDate As DateTime
            
            Public Property CurrencyRateID As Integer
            
            Public Property EndOfDayRate As Decimal
            
            Public Property FromCurrencyCode As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
            
            Public Property ToCurrencyCode As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CustomerAddressBuddyMetadata as the class
    ' that carries additional metadata for the CustomerAddressBuddy class.
    <MetadataTypeAttribute(GetType(CustomerAddressBuddy.CustomerAddressBuddyMetadata))>  _
    Partial Public Class CustomerAddressBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the CustomerAddress class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CustomerAddressBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Address As Address
            
            Public Property AddressID As Integer
            
            Public Property AddressType As AddressType
            
            Public Property AddressTypeID As Integer
            
            Public Property Customer As Customer
            
            Public Property CustomerID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property rowguid As Guid
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CustomerBuddyMetadata as the class
    ' that carries additional metadata for the CustomerBuddy class.
    <MetadataTypeAttribute(GetType(CustomerBuddy.CustomerBuddyMetadata))>  _
    Partial Public Class CustomerBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Customer class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CustomerBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AccountNumber As String
            
            Public Property CustomerAddresses As EntitySet(Of CustomerAddress)
            
            Public Property CustomerID As Integer
            
            Public Property CustomerType As Char
            
            Public Property Individual As Individual
            
            Public Property ModifiedDate As DateTime
            
            Public Property rowguid As Guid
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
            
            Public Property SalesTerritory As SalesTerritory
            
            Public Property Store As Store
            
            Public Property TerritoryID As Nullable(Of Integer)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies DepartmentBuddyMetadata as the class
    ' that carries additional metadata for the DepartmentBuddy class.
    <MetadataTypeAttribute(GetType(DepartmentBuddy.DepartmentBuddyMetadata))>  _
    Partial Public Class DepartmentBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Department class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class DepartmentBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property DepartmentID As Short
            
            Public Property EmployeeDepartmentHistories As EntitySet(Of EmployeeDepartmentHistory)
            
            Public Property GroupName As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies DocumentBuddyMetadata as the class
    ' that carries additional metadata for the DocumentBuddy class.
    <MetadataTypeAttribute(GetType(DocumentBuddy.DocumentBuddyMetadata))>  _
    Partial Public Class DocumentBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Document class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class DocumentBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ChangeNumber As Integer
            
            Public Property Document1 As Binary
            
            Public Property DocumentID As Integer
            
            Public Property DocumentSummary As String
            
            Public Property FileExtension As String
            
            Public Property FileName As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property ProductDocuments As EntitySet(Of ProductDocument)
            
            Public Property Revision As String
            
            Public Property Status As Byte
            
            Public Property Title As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies EmployeeAddressBuddyMetadata as the class
    ' that carries additional metadata for the EmployeeAddressBuddy class.
    <MetadataTypeAttribute(GetType(EmployeeAddressBuddy.EmployeeAddressBuddyMetadata))>  _
    Partial Public Class EmployeeAddressBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the EmployeeAddress class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EmployeeAddressBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Address As Address
            
            Public Property AddressID As Integer
            
            Public Property Employee As Employee
            
            Public Property EmployeeID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property rowguid As Guid
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies EmployeeDepartmentHistoryBuddyMetadata as the class
    ' that carries additional metadata for the EmployeeDepartmentHistoryBuddy class.
    <MetadataTypeAttribute(GetType(EmployeeDepartmentHistoryBuddy.EmployeeDepartmentHistoryBuddyMetadata))>  _
    Partial Public Class EmployeeDepartmentHistoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the EmployeeDepartmentHistory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EmployeeDepartmentHistoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Department As Department
            
            Public Property DepartmentID As Short
            
            Public Property Employee As Employee
            
            Public Property EmployeeID As Integer
            
            Public Property EndDate As Nullable(Of DateTime)
            
            Public Property ModifiedDate As DateTime
            
            Public Property Shift As Shift
            
            Public Property ShiftID As Byte
            
            Public Property StartDate As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies EmployeePayHistoryBuddyMetadata as the class
    ' that carries additional metadata for the EmployeePayHistoryBuddy class.
    <MetadataTypeAttribute(GetType(EmployeePayHistoryBuddy.EmployeePayHistoryBuddyMetadata))>  _
    Partial Public Class EmployeePayHistoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the EmployeePayHistory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EmployeePayHistoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Employee As Employee
            
            Public Property EmployeeID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property PayFrequency As Byte
            
            Public Property Rate As Decimal
            
            Public Property RateChangeDate As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies IllustrationBuddyMetadata as the class
    ' that carries additional metadata for the IllustrationBuddy class.
    <MetadataTypeAttribute(GetType(IllustrationBuddy.IllustrationBuddyMetadata))>  _
    Partial Public Class IllustrationBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Illustration class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class IllustrationBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Diagram As XElement
            
            Public Property IllustrationID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property ProductModelIllustrations As EntitySet(Of ProductModelIllustration)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies IndividualBuddyMetadata as the class
    ' that carries additional metadata for the IndividualBuddy class.
    <MetadataTypeAttribute(GetType(IndividualBuddy.IndividualBuddyMetadata))>  _
    Partial Public Class IndividualBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Individual class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class IndividualBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Contact As Contact
            
            Public Property ContactID As Integer
            
            Public Property Customer As Customer
            
            Public Property CustomerID As Integer
            
            Public Property Demographics As XElement
            
            Public Property ModifiedDate As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies JobCandidateBuddyMetadata as the class
    ' that carries additional metadata for the JobCandidateBuddy class.
    <MetadataTypeAttribute(GetType(JobCandidateBuddy.JobCandidateBuddyMetadata))>  _
    Partial Public Class JobCandidateBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the JobCandidate class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class JobCandidateBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Employee As Employee
            
            Public Property EmployeeID As Nullable(Of Integer)
            
            Public Property JobCandidateID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property [Resume] As XElement
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies LocationBuddyMetadata as the class
    ' that carries additional metadata for the LocationBuddy class.
    <MetadataTypeAttribute(GetType(LocationBuddy.LocationBuddyMetadata))>  _
    Partial Public Class LocationBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Location class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class LocationBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Availability As Decimal
            
            Public Property CostRate As Decimal
            
            Public Property LocationID As Short
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ProductInventories As EntitySet(Of ProductInventory)
            
            Public Property WorkOrderRoutings As EntitySet(Of WorkOrderRouting)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductCategoryBuddyMetadata as the class
    ' that carries additional metadata for the ProductCategoryBuddy class.
    <MetadataTypeAttribute(GetType(ProductCategoryBuddy.ProductCategoryBuddyMetadata))>  _
    Partial Public Class ProductCategoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductCategory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductCategoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ProductCategoryID As Integer
            
            Public Property ProductSubcategories As EntitySet(Of ProductSubcategory)
            
            Public Property rowguid As Guid
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductCostHistoryBuddyMetadata as the class
    ' that carries additional metadata for the ProductCostHistoryBuddy class.
    <MetadataTypeAttribute(GetType(ProductCostHistoryBuddy.ProductCostHistoryBuddyMetadata))>  _
    Partial Public Class ProductCostHistoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductCostHistory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductCostHistoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property EndDate As Nullable(Of DateTime)
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property StandardCost As Decimal
            
            Public Property StartDate As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductDescriptionBuddyMetadata as the class
    ' that carries additional metadata for the ProductDescriptionBuddy class.
    <MetadataTypeAttribute(GetType(ProductDescriptionBuddy.ProductDescriptionBuddyMetadata))>  _
    Partial Public Class ProductDescriptionBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductDescription class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductDescriptionBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Description As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property ProductDescriptionID As Integer
            
            Public Property ProductModelProductDescriptionCultures As EntitySet(Of ProductModelProductDescriptionCulture)
            
            Public Property rowguid As Guid
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductDocumentBuddyMetadata as the class
    ' that carries additional metadata for the ProductDocumentBuddy class.
    <MetadataTypeAttribute(GetType(ProductDocumentBuddy.ProductDocumentBuddyMetadata))>  _
    Partial Public Class ProductDocumentBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductDocument class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductDocumentBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Document As Document
            
            Public Property DocumentID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductInventoryBuddyMetadata as the class
    ' that carries additional metadata for the ProductInventoryBuddy class.
    <MetadataTypeAttribute(GetType(ProductInventoryBuddy.ProductInventoryBuddyMetadata))>  _
    Partial Public Class ProductInventoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductInventory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductInventoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Bin As Byte
            
            Public Property Location As Location
            
            Public Property LocationID As Short
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property Quantity As Short
            
            Public Property rowguid As Guid
            
            Public Property Shelf As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductListPriceHistoryBuddyMetadata as the class
    ' that carries additional metadata for the ProductListPriceHistoryBuddy class.
    <MetadataTypeAttribute(GetType(ProductListPriceHistoryBuddy.ProductListPriceHistoryBuddyMetadata))>  _
    Partial Public Class ProductListPriceHistoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductListPriceHistory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductListPriceHistoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property EndDate As Nullable(Of DateTime)
            
            Public Property ListPrice As Decimal
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property StartDate As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductModelBuddyMetadata as the class
    ' that carries additional metadata for the ProductModelBuddy class.
    <MetadataTypeAttribute(GetType(ProductModelBuddy.ProductModelBuddyMetadata))>  _
    Partial Public Class ProductModelBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductModel class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductModelBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CatalogDescription As XElement
            
            Public Property Instructions As XElement
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ProductModelID As Integer
            
            Public Property ProductModelIllustrations As EntitySet(Of ProductModelIllustration)
            
            Public Property ProductModelProductDescriptionCultures As EntitySet(Of ProductModelProductDescriptionCulture)
            
            Public Property Products As EntitySet(Of Product)
            
            Public Property rowguid As Guid
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductModelIllustrationBuddyMetadata as the class
    ' that carries additional metadata for the ProductModelIllustrationBuddy class.
    <MetadataTypeAttribute(GetType(ProductModelIllustrationBuddy.ProductModelIllustrationBuddyMetadata))>  _
    Partial Public Class ProductModelIllustrationBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductModelIllustration class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductModelIllustrationBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Illustration As Illustration
            
            Public Property IllustrationID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property ProductModel As ProductModel
            
            Public Property ProductModelID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductModelProductDescriptionCultureBuddyMetadata as the class
    ' that carries additional metadata for the ProductModelProductDescriptionCultureBuddy class.
    <MetadataTypeAttribute(GetType(ProductModelProductDescriptionCultureBuddy.ProductModelProductDescriptionCultureBuddyMetadata))>  _
    Partial Public Class ProductModelProductDescriptionCultureBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductModelProductDescriptionCulture class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductModelProductDescriptionCultureBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Culture As Culture
            
            Public Property CultureID As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property ProductDescription As ProductDescription
            
            Public Property ProductDescriptionID As Integer
            
            Public Property ProductModel As ProductModel
            
            Public Property ProductModelID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductPhotoBuddyMetadata as the class
    ' that carries additional metadata for the ProductPhotoBuddy class.
    <MetadataTypeAttribute(GetType(ProductPhotoBuddy.ProductPhotoBuddyMetadata))>  _
    Partial Public Class ProductPhotoBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductPhoto class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductPhotoBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property LargePhoto As Binary
            
            Public Property LargePhotoFileName As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property ProductPhotoID As Integer
            
            Public Property ProductProductPhotos As EntitySet(Of ProductProductPhoto)
            
            Public Property ThumbNailPhoto As Binary
            
            Public Property ThumbnailPhotoFileName As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductProductPhotoBuddyMetadata as the class
    ' that carries additional metadata for the ProductProductPhotoBuddy class.
    <MetadataTypeAttribute(GetType(ProductProductPhotoBuddy.ProductProductPhotoBuddyMetadata))>  _
    Partial Public Class ProductProductPhotoBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductProductPhoto class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductProductPhotoBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Primary As Boolean
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property ProductPhoto As ProductPhoto
            
            Public Property ProductPhotoID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductReviewBuddyMetadata as the class
    ' that carries additional metadata for the ProductReviewBuddy class.
    <MetadataTypeAttribute(GetType(ProductReviewBuddy.ProductReviewBuddyMetadata))>  _
    Partial Public Class ProductReviewBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductReview class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductReviewBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Comments As String
            
            Public Property EmailAddress As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property ProductReviewID As Integer
            
            Public Property Rating As Integer
            
            Public Property ReviewDate As DateTime
            
            Public Property ReviewerName As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductSubcategoryBuddyMetadata as the class
    ' that carries additional metadata for the ProductSubcategoryBuddy class.
    <MetadataTypeAttribute(GetType(ProductSubcategoryBuddy.ProductSubcategoryBuddyMetadata))>  _
    Partial Public Class ProductSubcategoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductSubcategory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductSubcategoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ProductCategory As ProductCategory
            
            Public Property ProductCategoryID As Integer
            
            Public Property Products As EntitySet(Of Product)
            
            Public Property ProductSubcategoryID As Integer
            
            Public Property rowguid As Guid
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductVendorBuddyMetadata as the class
    ' that carries additional metadata for the ProductVendorBuddy class.
    <MetadataTypeAttribute(GetType(ProductVendorBuddy.ProductVendorBuddyMetadata))>  _
    Partial Public Class ProductVendorBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ProductVendor class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductVendorBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AverageLeadTime As Integer
            
            Public Property LastReceiptCost As Nullable(Of Decimal)
            
            Public Property LastReceiptDate As Nullable(Of DateTime)
            
            Public Property MaxOrderQty As Integer
            
            Public Property MinOrderQty As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property OnOrderQty As Nullable(Of Integer)
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property StandardPrice As Decimal
            
            Public Property UnitMeasure As UnitMeasure
            
            Public Property UnitMeasureCode As String
            
            Public Property Vendor As Vendor
            
            Public Property VendorID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesOrderDetailBuddyMetadata as the class
    ' that carries additional metadata for the SalesOrderDetailBuddy class.
    <MetadataTypeAttribute(GetType(SalesOrderDetailBuddy.SalesOrderDetailBuddyMetadata))>  _
    Partial Public Class SalesOrderDetailBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesOrderDetail class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesOrderDetailBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CarrierTrackingNumber As String
            
            Public Property LineTotal As Decimal
            
            Public Property ModifiedDate As DateTime
            
            Public Property OrderQty As Short
            
            Public Property ProductID As Integer
            
            Public Property rowguid As Guid
            
            Public Property SalesOrderDetailID As Integer
            
            Public Property SalesOrderHeader As SalesOrderHeader
            
            Public Property SalesOrderID As Integer
            
            Public Property SpecialOfferID As Integer
            
            Public Property SpecialOfferProduct As SpecialOfferProduct
            
            Public Property UnitPrice As Decimal
            
            Public Property UnitPriceDiscount As Decimal
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesOrderHeaderBuddyMetadata as the class
    ' that carries additional metadata for the SalesOrderHeaderBuddy class.
    <MetadataTypeAttribute(GetType(SalesOrderHeaderBuddy.SalesOrderHeaderBuddyMetadata))>  _
    Partial Public Class SalesOrderHeaderBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesOrderHeader class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesOrderHeaderBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AccountNumber As String
            
            Public Property Address As Address
            
            Public Property Address1 As Address
            
            Public Property BillToAddressID As Integer
            
            Public Property Comment As String
            
            Public Property Contact As Contact
            
            Public Property ContactID As Integer
            
            Public Property CreditCard As CreditCard
            
            Public Property CreditCardApprovalCode As String
            
            Public Property CreditCardID As Nullable(Of Integer)
            
            Public Property CurrencyRate As CurrencyRate
            
            Public Property CurrencyRateID As Nullable(Of Integer)
            
            Public Property Customer As Customer
            
            Public Property CustomerID As Integer
            
            Public Property DueDate As DateTime
            
            Public Property Freight As Decimal
            
            Public Property ModifiedDate As DateTime
            
            Public Property OnlineOrderFlag As Boolean
            
            Public Property OrderDate As DateTime
            
            Public Property PurchaseOrderNumber As String
            
            Public Property RevisionNumber As Byte
            
            Public Property rowguid As Guid
            
            Public Property SalesOrderDetails As EntitySet(Of SalesOrderDetail)
            
            Public Property SalesOrderHeaderSalesReasons As EntitySet(Of SalesOrderHeaderSalesReason)
            
            Public Property SalesOrderID As Integer
            
            Public Property SalesOrderNumber As String
            
            Public Property SalesPerson As SalesPerson
            
            Public Property SalesPersonID As Nullable(Of Integer)
            
            Public Property SalesTerritory As SalesTerritory
            
            Public Property ShipDate As Nullable(Of DateTime)
            
            Public Property ShipMethod As ShipMethod
            
            Public Property ShipMethodID As Integer
            
            Public Property ShipToAddressID As Integer
            
            Public Property Status As Byte
            
            Public Property SubTotal As Decimal
            
            Public Property TaxAmt As Decimal
            
            Public Property TerritoryID As Nullable(Of Integer)
            
            Public Property TotalDue As Decimal
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesOrderHeaderSalesReasonBuddyMetadata as the class
    ' that carries additional metadata for the SalesOrderHeaderSalesReasonBuddy class.
    <MetadataTypeAttribute(GetType(SalesOrderHeaderSalesReasonBuddy.SalesOrderHeaderSalesReasonBuddyMetadata))>  _
    Partial Public Class SalesOrderHeaderSalesReasonBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesOrderHeaderSalesReason class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesOrderHeaderSalesReasonBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property SalesOrderHeader As SalesOrderHeader
            
            Public Property SalesOrderID As Integer
            
            Public Property SalesReason As SalesReason
            
            Public Property SalesReasonID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesPersonBuddyMetadata as the class
    ' that carries additional metadata for the SalesPersonBuddy class.
    <MetadataTypeAttribute(GetType(SalesPersonBuddy.SalesPersonBuddyMetadata))>  _
    Partial Public Class SalesPersonBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesPerson class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesPersonBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Bonus As Decimal
            
            Public Property CommissionPct As Decimal
            
            Public Property Employee As Employee
            
            Public Property ModifiedDate As DateTime
            
            Public Property rowguid As Guid
            
            Public Property SalesLastYear As Decimal
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
            
            Public Property SalesPersonID As Integer
            
            Public Property SalesPersonQuotaHistories As EntitySet(Of SalesPersonQuotaHistory)
            
            Public Property SalesQuota As Nullable(Of Decimal)
            
            Public Property SalesTerritory As SalesTerritory
            
            Public Property SalesTerritoryHistories As EntitySet(Of SalesTerritoryHistory)
            
            Public Property SalesYTD As Decimal
            
            Public Property Stores As EntitySet(Of Store)
            
            Public Property TerritoryID As Nullable(Of Integer)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesPersonQuotaHistoryBuddyMetadata as the class
    ' that carries additional metadata for the SalesPersonQuotaHistoryBuddy class.
    <MetadataTypeAttribute(GetType(SalesPersonQuotaHistoryBuddy.SalesPersonQuotaHistoryBuddyMetadata))>  _
    Partial Public Class SalesPersonQuotaHistoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesPersonQuotaHistory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesPersonQuotaHistoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property QuotaDate As DateTime
            
            Public Property rowguid As Guid
            
            Public Property SalesPerson As SalesPerson
            
            Public Property SalesPersonID As Integer
            
            Public Property SalesQuota As Decimal
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesReasonBuddyMetadata as the class
    ' that carries additional metadata for the SalesReasonBuddy class.
    <MetadataTypeAttribute(GetType(SalesReasonBuddy.SalesReasonBuddyMetadata))>  _
    Partial Public Class SalesReasonBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesReason class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesReasonBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ReasonType As String
            
            Public Property SalesOrderHeaderSalesReasons As EntitySet(Of SalesOrderHeaderSalesReason)
            
            Public Property SalesReasonID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesTaxRateBuddyMetadata as the class
    ' that carries additional metadata for the SalesTaxRateBuddy class.
    <MetadataTypeAttribute(GetType(SalesTaxRateBuddy.SalesTaxRateBuddyMetadata))>  _
    Partial Public Class SalesTaxRateBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesTaxRate class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesTaxRateBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property rowguid As Guid
            
            Public Property SalesTaxRateID As Integer
            
            Public Property StateProvince As StateProvince
            
            Public Property StateProvinceID As Integer
            
            Public Property TaxRate As Decimal
            
            Public Property TaxType As Byte
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesTerritoryBuddyMetadata as the class
    ' that carries additional metadata for the SalesTerritoryBuddy class.
    <MetadataTypeAttribute(GetType(SalesTerritoryBuddy.SalesTerritoryBuddyMetadata))>  _
    Partial Public Class SalesTerritoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesTerritory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesTerritoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CostLastYear As Decimal
            
            Public Property CostYTD As Decimal
            
            Public Property CountryRegionCode As String
            
            Public Property Customers As EntitySet(Of Customer)
            
            Public Property Group As String
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property rowguid As Guid
            
            Public Property SalesLastYear As Decimal
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
            
            Public Property SalesPersons As EntitySet(Of SalesPerson)
            
            Public Property SalesTerritoryHistories As EntitySet(Of SalesTerritoryHistory)
            
            Public Property SalesYTD As Decimal
            
            Public Property StateProvinces As EntitySet(Of StateProvince)
            
            Public Property TerritoryID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SalesTerritoryHistoryBuddyMetadata as the class
    ' that carries additional metadata for the SalesTerritoryHistoryBuddy class.
    <MetadataTypeAttribute(GetType(SalesTerritoryHistoryBuddy.SalesTerritoryHistoryBuddyMetadata))>  _
    Partial Public Class SalesTerritoryHistoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SalesTerritoryHistory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SalesTerritoryHistoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property EndDate As Nullable(Of DateTime)
            
            Public Property ModifiedDate As DateTime
            
            Public Property rowguid As Guid
            
            Public Property SalesPerson As SalesPerson
            
            Public Property SalesPersonID As Integer
            
            Public Property SalesTerritory As SalesTerritory
            
            Public Property StartDate As DateTime
            
            Public Property TerritoryID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ScrapReasonBuddyMetadata as the class
    ' that carries additional metadata for the ScrapReasonBuddy class.
    <MetadataTypeAttribute(GetType(ScrapReasonBuddy.ScrapReasonBuddyMetadata))>  _
    Partial Public Class ScrapReasonBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ScrapReason class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ScrapReasonBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ScrapReasonID As Short
            
            Public Property WorkOrders As EntitySet(Of WorkOrder)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ShiftBuddyMetadata as the class
    ' that carries additional metadata for the ShiftBuddy class.
    <MetadataTypeAttribute(GetType(ShiftBuddy.ShiftBuddyMetadata))>  _
    Partial Public Class ShiftBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Shift class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ShiftBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property EmployeeDepartmentHistories As EntitySet(Of EmployeeDepartmentHistory)
            
            Public Property EndTime As DateTime
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property ShiftID As Byte
            
            Public Property StartTime As DateTime
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ShipMethodBuddyMetadata as the class
    ' that carries additional metadata for the ShipMethodBuddy class.
    <MetadataTypeAttribute(GetType(ShipMethodBuddy.ShipMethodBuddyMetadata))>  _
    Partial Public Class ShipMethodBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ShipMethod class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ShipMethodBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property PurchaseOrders As EntitySet(Of PurchaseOrder)
            
            Public Property rowguid As Guid
            
            Public Property SalesOrderHeaders As EntitySet(Of SalesOrderHeader)
            
            Public Property ShipBase As Decimal
            
            Public Property ShipMethodID As Integer
            
            Public Property ShipRate As Decimal
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ShoppingCartItemBuddyMetadata as the class
    ' that carries additional metadata for the ShoppingCartItemBuddy class.
    <MetadataTypeAttribute(GetType(ShoppingCartItemBuddy.ShoppingCartItemBuddyMetadata))>  _
    Partial Public Class ShoppingCartItemBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ShoppingCartItem class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ShoppingCartItemBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property DateCreated As DateTime
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property Quantity As Integer
            
            Public Property ShoppingCartID As String
            
            Public Property ShoppingCartItemID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SpecialOfferBuddyMetadata as the class
    ' that carries additional metadata for the SpecialOfferBuddy class.
    <MetadataTypeAttribute(GetType(SpecialOfferBuddy.SpecialOfferBuddyMetadata))>  _
    Partial Public Class SpecialOfferBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SpecialOffer class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SpecialOfferBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Category As String
            
            Public Property Description As String
            
            Public Property DiscountPct As Decimal
            
            Public Property EndDate As DateTime
            
            Public Property MaxQty As Nullable(Of Integer)
            
            Public Property MinQty As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property rowguid As Guid
            
            Public Property SpecialOfferID As Integer
            
            Public Property SpecialOfferProducts As EntitySet(Of SpecialOfferProduct)
            
            Public Property StartDate As DateTime
            
            Public Property Type As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SpecialOfferProductBuddyMetadata as the class
    ' that carries additional metadata for the SpecialOfferProductBuddy class.
    <MetadataTypeAttribute(GetType(SpecialOfferProductBuddy.SpecialOfferProductBuddyMetadata))>  _
    Partial Public Class SpecialOfferProductBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the SpecialOfferProduct class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SpecialOfferProductBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property rowguid As Guid
            
            Public Property SalesOrderDetails As EntitySet(Of SalesOrderDetail)
            
            Public Property SpecialOffer As SpecialOffer
            
            Public Property SpecialOfferID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies StateProvinceBuddyMetadata as the class
    ' that carries additional metadata for the StateProvinceBuddy class.
    <MetadataTypeAttribute(GetType(StateProvinceBuddy.StateProvinceBuddyMetadata))>  _
    Partial Public Class StateProvinceBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the StateProvince class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class StateProvinceBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Addresses As EntitySet(Of Address)
            
            Public Property CountryRegion As CountryRegion
            
            Public Property CountryRegionCode As String
            
            Public Property IsOnlyStateProvinceFlag As Boolean
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property rowguid As Guid
            
            Public Property SalesTaxRates As EntitySet(Of SalesTaxRate)
            
            Public Property SalesTerritory As SalesTerritory
            
            Public Property StateProvinceCode As String
            
            Public Property StateProvinceID As Integer
            
            Public Property TerritoryID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies StoreBuddyMetadata as the class
    ' that carries additional metadata for the StoreBuddy class.
    <MetadataTypeAttribute(GetType(StoreBuddy.StoreBuddyMetadata))>  _
    Partial Public Class StoreBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Store class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class StoreBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Customer As Customer
            
            Public Property CustomerID As Integer
            
            Public Property Demographics As XElement
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property rowguid As Guid
            
            Public Property SalesPerson As SalesPerson
            
            Public Property SalesPersonID As Nullable(Of Integer)
            
            Public Property StoreContacts As EntitySet(Of StoreContact)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies StoreContactBuddyMetadata as the class
    ' that carries additional metadata for the StoreContactBuddy class.
    <MetadataTypeAttribute(GetType(StoreContactBuddy.StoreContactBuddyMetadata))>  _
    Partial Public Class StoreContactBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the StoreContact class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class StoreContactBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Contact As Contact
            
            Public Property ContactID As Integer
            
            Public Property ContactType As ContactType
            
            Public Property ContactTypeID As Integer
            
            Public Property CustomerID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property rowguid As Guid
            
            Public Property Store As Store
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies TransactionHistoryArchiveBuddyMetadata as the class
    ' that carries additional metadata for the TransactionHistoryArchiveBuddy class.
    <MetadataTypeAttribute(GetType(TransactionHistoryArchiveBuddy.TransactionHistoryArchiveBuddyMetadata))>  _
    Partial Public Class TransactionHistoryArchiveBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the TransactionHistoryArchive class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class TransactionHistoryArchiveBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ActualCost As Decimal
            
            Public Property ModifiedDate As DateTime
            
            Public Property ProductID As Integer
            
            Public Property Quantity As Integer
            
            Public Property ReferenceOrderID As Integer
            
            Public Property ReferenceOrderLineID As Integer
            
            Public Property TransactionDate As DateTime
            
            Public Property TransactionID As Integer
            
            Public Property TransactionType As Char
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies TransactionHistoryBuddyMetadata as the class
    ' that carries additional metadata for the TransactionHistoryBuddy class.
    <MetadataTypeAttribute(GetType(TransactionHistoryBuddy.TransactionHistoryBuddyMetadata))>  _
    Partial Public Class TransactionHistoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the TransactionHistory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class TransactionHistoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ActualCost As Decimal
            
            Public Property ModifiedDate As DateTime
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property Quantity As Integer
            
            Public Property ReferenceOrderID As Integer
            
            Public Property ReferenceOrderLineID As Integer
            
            Public Property TransactionDate As DateTime
            
            Public Property TransactionID As Integer
            
            Public Property TransactionType As Char
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies UnitMeasureBuddyMetadata as the class
    ' that carries additional metadata for the UnitMeasureBuddy class.
    <MetadataTypeAttribute(GetType(UnitMeasureBuddy.UnitMeasureBuddyMetadata))>  _
    Partial Public Class UnitMeasureBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the UnitMeasure class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class UnitMeasureBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property BillOfMaterials As EntitySet(Of BillOfMaterial)
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property Products As EntitySet(Of Product)
            
            Public Property Products1 As EntitySet(Of Product)
            
            Public Property ProductVendors As EntitySet(Of ProductVendor)
            
            Public Property UnitMeasureCode As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies VendorAddressBuddyMetadata as the class
    ' that carries additional metadata for the VendorAddressBuddy class.
    <MetadataTypeAttribute(GetType(VendorAddressBuddy.VendorAddressBuddyMetadata))>  _
    Partial Public Class VendorAddressBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the VendorAddress class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class VendorAddressBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Address As Address
            
            Public Property AddressID As Integer
            
            Public Property AddressType As AddressType
            
            Public Property AddressTypeID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property Vendor As Vendor
            
            Public Property VendorID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies VendorBuddyMetadata as the class
    ' that carries additional metadata for the VendorBuddy class.
    <MetadataTypeAttribute(GetType(VendorBuddy.VendorBuddyMetadata))>  _
    Partial Public Class VendorBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Vendor class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class VendorBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AccountNumber As String
            
            Public Property ActiveFlag As Boolean
            
            Public Property CreditRating As Byte
            
            Public Property ModifiedDate As DateTime
            
            Public Property Name As String
            
            Public Property PreferredVendorStatus As Boolean
            
            Public Property ProductVendors As EntitySet(Of ProductVendor)
            
            Public Property PurchaseOrders As EntitySet(Of PurchaseOrder)
            
            Public Property PurchasingWebServiceURL As String
            
            Public Property VendorAddresses As EntitySet(Of VendorAddress)
            
            Public Property VendorContacts As EntitySet(Of VendorContact)
            
            Public Property VendorID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies VendorContactBuddyMetadata as the class
    ' that carries additional metadata for the VendorContactBuddy class.
    <MetadataTypeAttribute(GetType(VendorContactBuddy.VendorContactBuddyMetadata))>  _
    Partial Public Class VendorContactBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the VendorContact class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class VendorContactBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Contact As Contact
            
            Public Property ContactID As Integer
            
            Public Property ContactType As ContactType
            
            Public Property ContactTypeID As Integer
            
            Public Property ModifiedDate As DateTime
            
            Public Property Vendor As Vendor
            
            Public Property VendorID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies WorkOrderBuddyMetadata as the class
    ' that carries additional metadata for the WorkOrderBuddy class.
    <MetadataTypeAttribute(GetType(WorkOrderBuddy.WorkOrderBuddyMetadata))>  _
    Partial Public Class WorkOrderBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the WorkOrder class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class WorkOrderBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property DueDate As DateTime
            
            Public Property EndDate As Nullable(Of DateTime)
            
            Public Property ModifiedDate As DateTime
            
            Public Property OrderQty As Integer
            
            Public Property Product As Product
            
            Public Property ProductID As Integer
            
            Public Property ScrappedQty As Short
            
            Public Property ScrapReason As ScrapReason
            
            Public Property ScrapReasonID As Nullable(Of Short)
            
            Public Property StartDate As DateTime
            
            Public Property StockedQty As Integer
            
            Public Property WorkOrderID As Integer
            
            Public Property WorkOrderRoutings As EntitySet(Of WorkOrderRouting)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies WorkOrderRoutingBuddyMetadata as the class
    ' that carries additional metadata for the WorkOrderRoutingBuddy class.
    <MetadataTypeAttribute(GetType(WorkOrderRoutingBuddy.WorkOrderRoutingBuddyMetadata))>  _
    Partial Public Class WorkOrderRoutingBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the WorkOrderRouting class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class WorkOrderRoutingBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ActualCost As Nullable(Of Decimal)
            
            Public Property ActualEndDate As Nullable(Of DateTime)
            
            Public Property ActualResourceHrs As Nullable(Of Decimal)
            
            Public Property ActualStartDate As Nullable(Of DateTime)
            
            Public Property Location As Location
            
            Public Property LocationID As Short
            
            Public Property ModifiedDate As DateTime
            
            Public Property OperationSequence As Short
            
            Public Property PlannedCost As Decimal
            
            Public Property ProductID As Integer
            
            Public Property ScheduledEndDate As DateTime
            
            Public Property ScheduledStartDate As DateTime
            
            Public Property WorkOrder As WorkOrder
            
            Public Property WorkOrderID As Integer
        End Class
    End Class
End Namespace
