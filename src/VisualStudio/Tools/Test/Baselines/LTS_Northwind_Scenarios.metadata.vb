
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.Scenarios.LTS.Northwind
Imports OpenRiaServices.Hosting
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq

Namespace DataTests.Scenarios.LTS.NorthwindBuddy
    
    'The MetadataTypeAttribute identifies Bug843965_ABuddyMetadata as the class
    ' that carries additional metadata for the Bug843965_ABuddy class.
    <MetadataTypeAttribute(GetType(Bug843965_ABuddy.Bug843965_ABuddyMetadata))>  _
    Partial Public Class Bug843965_ABuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Bug843965_A class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class Bug843965_ABuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property ID As Integer
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies Customer_Bug479436BuddyMetadata as the class
    ' that carries additional metadata for the Customer_Bug479436Buddy class.
    <MetadataTypeAttribute(GetType(Customer_Bug479436Buddy.Customer_Bug479436BuddyMetadata))>  _
    Partial Public Class Customer_Bug479436Buddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Customer_Bug479436 class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class Customer_Bug479436BuddyMetadata
            
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
            
            Public Property CustomerID As String
            
            Public Property Fax As String
            
            Public Property Phone As String
            
            Public Property PostalCode As String
            
            Public Property Region As String
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
            
            Public Property ID As Integer
            
            Public Property Timestamp() As Byte
        End Class
    End Class
End Namespace
