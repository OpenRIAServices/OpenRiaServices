
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq

Namespace BizLogic.Test
    
    'TODO: Create methods containing your application logic.
    <EnableClientAccess()>  _
    Public Class Empty_DomainService_OData
        Inherits DomainService
    End Class
End Namespace
