
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataModels.ScenarioModels
Imports OpenRiaServices.Hosting
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq

Namespace DataModels.ScenarioModelsBuddy
    
    'The MetadataTypeAttribute identifies EntityPropertyNamedPublicBuddyMetadata as the class
    ' that carries additional metadata for the EntityPropertyNamedPublicBuddy class.
    <MetadataTypeAttribute(GetType(EntityPropertyNamedPublicBuddy.EntityPropertyNamedPublicBuddyMetadata))>  _
    Partial Public Class EntityPropertyNamedPublicBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the EntityPropertyNamedPublic class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class EntityPropertyNamedPublicBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property publicPublic As Integer
        End Class
    End Class
End Namespace
