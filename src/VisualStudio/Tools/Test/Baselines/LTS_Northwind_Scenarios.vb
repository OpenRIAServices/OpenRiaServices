
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.Scenarios.LTS.Northwind
Imports OpenRiaServices.LinqToSql
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Linq
Imports System.Linq

Namespace BizLogic.Test
    
    'Implements application logic using the NorthwindScenarios context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class LTS_Northwind_Scenarios
        Inherits LinqToSqlDomainService(Of NorthwindScenarios)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetBug843965_As() As IQueryable(Of Bug843965_A)
            Return Me.DataContext.Bug843965_As
        End Function
        
        Public Sub InsertBug843965_A(ByVal bug843965_A As Bug843965_A)
            Me.DataContext.Bug843965_As.InsertOnSubmit(bug843965_A)
        End Sub
        
        Public Sub UpdateBug843965_A(ByVal currentBug843965_A As Bug843965_A)
            Me.DataContext.Bug843965_As.Attach(currentBug843965_A, Me.ChangeSet.GetOriginal(currentBug843965_A))
        End Sub
        
        Public Sub DeleteBug843965_A(ByVal bug843965_A As Bug843965_A)
            Me.DataContext.Bug843965_As.Attach(bug843965_A)
            Me.DataContext.Bug843965_As.DeleteOnSubmit(bug843965_A)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCustomer_Bug479436s() As IQueryable(Of Customer_Bug479436)
            Return Me.DataContext.Customer_Bug479436s
        End Function
        
        Public Sub InsertCustomer_Bug479436(ByVal customer_Bug479436 As Customer_Bug479436)
            Me.DataContext.Customer_Bug479436s.InsertOnSubmit(customer_Bug479436)
        End Sub
        
        Public Sub UpdateCustomer_Bug479436(ByVal currentCustomer_Bug479436 As Customer_Bug479436)
            Me.DataContext.Customer_Bug479436s.Attach(currentCustomer_Bug479436, Me.ChangeSet.GetOriginal(currentCustomer_Bug479436))
        End Sub
        
        Public Sub DeleteCustomer_Bug479436(ByVal customer_Bug479436 As Customer_Bug479436)
            Me.DataContext.Customer_Bug479436s.Attach(customer_Bug479436)
            Me.DataContext.Customer_Bug479436s.DeleteOnSubmit(customer_Bug479436)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetOrder_Bug479436s() As IQueryable(Of Order_Bug479436)
            Return Me.DataContext.Order_Bug479436s
        End Function
        
        Public Sub InsertOrder_Bug479436(ByVal order_Bug479436 As Order_Bug479436)
            Me.DataContext.Order_Bug479436s.InsertOnSubmit(order_Bug479436)
        End Sub
        
        Public Sub UpdateOrder_Bug479436(ByVal currentOrder_Bug479436 As Order_Bug479436)
            Me.DataContext.Order_Bug479436s.Attach(currentOrder_Bug479436, Me.ChangeSet.GetOriginal(currentOrder_Bug479436))
        End Sub
        
        Public Sub DeleteOrder_Bug479436(ByVal order_Bug479436 As Order_Bug479436)
            Me.DataContext.Order_Bug479436s.Attach(order_Bug479436)
            Me.DataContext.Order_Bug479436s.DeleteOnSubmit(order_Bug479436)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetTimestampEntities() As IQueryable(Of TimestampEntity)
            Return Me.DataContext.TimestampEntities
        End Function
        
        Public Sub InsertTimestampEntity(ByVal timestampEntity As TimestampEntity)
            Me.DataContext.TimestampEntities.InsertOnSubmit(timestampEntity)
        End Sub
        
        Public Sub UpdateTimestampEntity(ByVal currentTimestampEntity As TimestampEntity)
            Me.DataContext.TimestampEntities.Attach(currentTimestampEntity, true)
        End Sub
        
        Public Sub DeleteTimestampEntity(ByVal timestampEntity As TimestampEntity)
            Me.DataContext.TimestampEntities.Attach(timestampEntity)
            Me.DataContext.TimestampEntities.DeleteOnSubmit(timestampEntity)
        End Sub
    End Class
End Namespace
