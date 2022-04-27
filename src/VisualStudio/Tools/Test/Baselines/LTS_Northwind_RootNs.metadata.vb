
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.Northwind.LTS
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Linq
Imports System.Linq

Namespace DataTests.Northwind.LTSBuddy
    
    'The MetadataTypeAttribute identifies CategoryBuddyMetadata as the class
    ' that carries additional metadata for the CategoryBuddy class.
    <MetadataTypeAttribute(GetType(CategoryBuddy.CategoryBuddyMetadata))>  _
    Partial Public Class CategoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Category class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CategoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CategoryID As Integer
            
            Public Property CategoryName As String
            
            Public Property Description As String
            
            Public Property Picture As Binary
            
            Public Property Products As EntitySet(Of Product)
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
            
            Public Property Address As String
            
            Public Property City As String
            
            Public Property CompanyName As String
            
            Public Property ContactName As String
            
            Public Property ContactTitle As String
            
            Public Property Country As String
            
            Public Property CustomerCustomerDemos As EntitySet(Of CustomerCustomerDemo)
            
            Public Property CustomerID As String
            
            Public Property Fax As String
            
            Public Property Orders As EntitySet(Of Order)
            
            Public Property Phone As String
            
            Public Property PostalCode As String
            
            Public Property Region As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CustomerCustomerDemoBuddyMetadata as the class
    ' that carries additional metadata for the CustomerCustomerDemoBuddy class.
    <MetadataTypeAttribute(GetType(CustomerCustomerDemoBuddy.CustomerCustomerDemoBuddyMetadata))>  _
    Partial Public Class CustomerCustomerDemoBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the CustomerCustomerDemo class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CustomerCustomerDemoBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Customer As Customer
            
            Public Property CustomerDemographic As CustomerDemographic
            
            Public Property CustomerID As String
            
            Public Property CustomerTypeID As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies CustomerDemographicBuddyMetadata as the class
    ' that carries additional metadata for the CustomerDemographicBuddy class.
    <MetadataTypeAttribute(GetType(CustomerDemographicBuddy.CustomerDemographicBuddyMetadata))>  _
    Partial Public Class CustomerDemographicBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the CustomerDemographic class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CustomerDemographicBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CustomerCustomerDemos As EntitySet(Of CustomerCustomerDemo)
            
            Public Property CustomerDesc As String
            
            Public Property CustomerTypeID As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies EmployeeBuddyMetadata as the class
    ' that carries additional metadata for the EmployeeBuddy class.
    <MetadataTypeAttribute(GetType(EmployeeBuddy.EmployeeBuddyMetadata))>  _
    Partial Public Class EmployeeBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Employee class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EmployeeBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Address As String
            
            Public Property BirthDate As Nullable(Of DateTime)
            
            Public Property City As String
            
            Public Property Country As String
            
            Public Property Employee1 As Employee
            
            Public Property EmployeeID As Integer
            
            Public Property Employees As EntitySet(Of Employee)
            
            Public Property EmployeeTerritories As EntitySet(Of EmployeeTerritory)
            
            Public Property Extension As String
            
            Public Property FirstName As String
            
            Public Property HireDate As Nullable(Of DateTime)
            
            Public Property HomePhone As String
            
            Public Property LastName As String
            
            Public Property Notes As String
            
            Public Property Orders As EntitySet(Of Order)
            
            Public Property Photo As Binary
            
            Public Property PhotoPath As String
            
            Public Property PostalCode As String
            
            Public Property Region As String
            
            Public Property ReportsTo As Nullable(Of Integer)
            
            Public Property Title As String
            
            Public Property TitleOfCourtesy As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies EmployeeTerritoryBuddyMetadata as the class
    ' that carries additional metadata for the EmployeeTerritoryBuddy class.
    <MetadataTypeAttribute(GetType(EmployeeTerritoryBuddy.EmployeeTerritoryBuddyMetadata))>  _
    Partial Public Class EmployeeTerritoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the EmployeeTerritory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EmployeeTerritoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Employee As Employee
            
            Public Property EmployeeID As Integer
            
            Public Property Territory As Territory
            
            Public Property TerritoryID As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ShipperBuddyMetadata as the class
    ' that carries additional metadata for the ShipperBuddy class.
    <MetadataTypeAttribute(GetType(ShipperBuddy.ShipperBuddyMetadata))>  _
    Partial Public Class ShipperBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Shipper class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ShipperBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CompanyName As String
            
            Public Property Orders As EntitySet(Of Order)
            
            Public Property Phone As String
            
            Public Property ShipperID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies SupplierBuddyMetadata as the class
    ' that carries additional metadata for the SupplierBuddy class.
    <MetadataTypeAttribute(GetType(SupplierBuddy.SupplierBuddyMetadata))>  _
    Partial Public Class SupplierBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Supplier class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class SupplierBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Address As String
            
            Public Property City As String
            
            Public Property CompanyName As String
            
            Public Property ContactName As String
            
            Public Property ContactTitle As String
            
            Public Property Country As String
            
            Public Property Fax As String
            
            Public Property HomePage As String
            
            Public Property Phone As String
            
            Public Property PostalCode As String
            
            Public Property Products As EntitySet(Of Product)
            
            Public Property Region As String
            
            Public Property SupplierID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies TerritoryBuddyMetadata as the class
    ' that carries additional metadata for the TerritoryBuddy class.
    <MetadataTypeAttribute(GetType(TerritoryBuddy.TerritoryBuddyMetadata))>  _
    Partial Public Class TerritoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Territory class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class TerritoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property EmployeeTerritories As EntitySet(Of EmployeeTerritory)
            
            Public Property Region As Region
            
            Public Property RegionID As Integer
            
            Public Property TerritoryDescription As String
            
            Public Property TerritoryID As String
        End Class
    End Class
End Namespace
