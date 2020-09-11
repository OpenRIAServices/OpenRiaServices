
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.Scenarios.EF.Northwind
Imports OpenRiaServices.Hosting
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq

Namespace DataTests.Scenarios.EF.NorthwindBuddy
    
    'The MetadataTypeAttribute identifies AddressCTBuddyMetadata as the class
    ' that carries additional metadata for the AddressCTBuddy class.
    <MetadataTypeAttribute(GetType(AddressCTBuddy.AddressCTBuddyMetadata))>  _
    Partial Public Class AddressCTBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the AddressCT class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class AddressCTBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property AddressLine As String
            
            Public Property City As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ContactInfoCTBuddyMetadata as the class
    ' that carries additional metadata for the ContactInfoCTBuddy class.
    <MetadataTypeAttribute(GetType(ContactInfoCTBuddy.ContactInfoCTBuddyMetadata))>  _
    Partial Public Class ContactInfoCTBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the ContactInfoCT class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ContactInfoCTBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Address As AddressCT
            
            Public Property HomePhone As String
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
            
            Public Property ContactName As String
            
            Public Property CustomerID As String
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
            
            Public Property EmployeeID As Integer
            
            Public Property FirstName As String
            
            Public Property LastName As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies EmployeeWithCTBuddyMetadata as the class
    ' that carries additional metadata for the EmployeeWithCTBuddy class.
    <MetadataTypeAttribute(GetType(EmployeeWithCTBuddy.EmployeeWithCTBuddyMetadata))>  _
    Partial Public Class EmployeeWithCTBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the EmployeeWithCT class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EmployeeWithCTBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ContactInfo As ContactInfoCT
            
            Public Property EmployeeID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies EntityWithNullFacetValuesForTimestampComparisonBuddyMetadata as the class
    ' that carries additional metadata for the EntityWithNullFacetValuesForTimestampComparisonBuddy class.
    <MetadataTypeAttribute(GetType(EntityWithNullFacetValuesForTimestampComparisonBuddy.EntityWithNullFacetValuesForTimestampComparisonBuddyMetadata))>  _
    Partial Public Class EntityWithNullFacetValuesForTimestampComparisonBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the EntityWithNullFacetValuesForTimestampComparison class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EntityWithNullFacetValuesForTimestampComparisonBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ConcurrencyTimestamp As String
            
            Public Property Id As Integer
            
            Public Property StringWithoutComputed As String
            
            Public Property StringWithoutConcurrencyMode As String
            
            Public Property StringWithoutFixedLength As String
            
            Public Property StringWithoutMaxLength As String
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies TimestampEntityBuddyMetadata as the class
    ' that carries additional metadata for the TimestampEntityBuddy class.
    <MetadataTypeAttribute(GetType(TimestampEntityBuddy.TimestampEntityBuddyMetadata))>  _
    Partial Public Class TimestampEntityBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the TimestampEntity class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class TimestampEntityBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property A As Integer
            
            Public Property Id As Integer
            
            Public Property Timestamp() As Byte
        End Class
    End Class
End Namespace
