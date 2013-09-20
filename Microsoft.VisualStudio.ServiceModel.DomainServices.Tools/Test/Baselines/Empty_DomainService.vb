
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.ServiceModel.DomainServices.Hosting
Imports System.ServiceModel.DomainServices.Server

Namespace BizLogic.Test
    
    'TODO: Create methods containing your application logic.
    <EnableClientAccess()>  _
    Public Class Empty_DomainService
        Inherits DomainService
    End Class
End Namespace
