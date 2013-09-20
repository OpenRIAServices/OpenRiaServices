
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.Scenarios.EF.Northwind
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data
Imports System.Linq
Imports System.ServiceModel.DomainServices.EntityFramework
Imports System.ServiceModel.DomainServices.Hosting
Imports System.ServiceModel.DomainServices.Server

Namespace BizLogic.Test
    
    'Implements application logic using the NorthwindEntities_Scenarios context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class EF_Northwind_Scenarios
        Inherits LinqToEntitiesDomainService(Of NorthwindEntities_Scenarios)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CustomerSet' query.
        Public Function GetCustomerSet() As IQueryable(Of Customer)
            Return Me.ObjectContext.CustomerSet
        End Function
        
        Public Sub InsertCustomer(ByVal customer As Customer)
            If ((customer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Added)
            Else
                Me.ObjectContext.CustomerSet.AddObject(customer)
            End If
        End Sub
        
        Public Sub UpdateCustomer(ByVal currentCustomer As Customer)
            Me.ObjectContext.CustomerSet.AttachAsModified(currentCustomer, Me.ChangeSet.GetOriginal(currentCustomer))
        End Sub
        
        Public Sub DeleteCustomer(ByVal customer As Customer)
            If ((customer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Deleted)
            Else
                Me.ObjectContext.CustomerSet.Attach(customer)
                Me.ObjectContext.CustomerSet.DeleteObject(customer)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'EmployeeSet' query.
        Public Function GetEmployeeSet() As IQueryable(Of Employee)
            Return Me.ObjectContext.EmployeeSet
        End Function
        
        Public Sub InsertEmployee(ByVal employee As Employee)
            If ((employee.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Added)
            Else
                Me.ObjectContext.EmployeeSet.AddObject(employee)
            End If
        End Sub
        
        Public Sub UpdateEmployee(ByVal currentEmployee As Employee)
            Me.ObjectContext.EmployeeSet.AttachAsModified(currentEmployee, Me.ChangeSet.GetOriginal(currentEmployee))
        End Sub
        
        Public Sub DeleteEmployee(ByVal employee As Employee)
            If ((employee.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Deleted)
            Else
                Me.ObjectContext.EmployeeSet.Attach(employee)
                Me.ObjectContext.EmployeeSet.DeleteObject(employee)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'EmployeeWithCTs' query.
        Public Function GetEmployeeWithCTs() As IQueryable(Of EmployeeWithCT)
            Return Me.ObjectContext.EmployeeWithCTs
        End Function
        
        Public Sub InsertEmployeeWithCT(ByVal employeeWithCT As EmployeeWithCT)
            If ((employeeWithCT.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeeWithCT, EntityState.Added)
            Else
                Me.ObjectContext.EmployeeWithCTs.AddObject(employeeWithCT)
            End If
        End Sub
        
        Public Sub UpdateEmployeeWithCT(ByVal currentEmployeeWithCT As EmployeeWithCT)
            Me.ObjectContext.EmployeeWithCTs.AttachAsModified(currentEmployeeWithCT, Me.ChangeSet.GetOriginal(currentEmployeeWithCT))
        End Sub
        
        Public Sub DeleteEmployeeWithCT(ByVal employeeWithCT As EmployeeWithCT)
            If ((employeeWithCT.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeeWithCT, EntityState.Deleted)
            Else
                Me.ObjectContext.EmployeeWithCTs.Attach(employeeWithCT)
                Me.ObjectContext.EmployeeWithCTs.DeleteObject(employeeWithCT)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'EntitiesWithNullFacetValuesForTimestampComparison' query.
        Public Function GetEntitiesWithNullFacetValuesForTimestampComparison() As IQueryable(Of EntityWithNullFacetValuesForTimestampComparison)
            Return Me.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison
        End Function
        
        Public Sub InsertEntityWithNullFacetValuesForTimestampComparison(ByVal entityWithNullFacetValuesForTimestampComparison As EntityWithNullFacetValuesForTimestampComparison)
            If ((entityWithNullFacetValuesForTimestampComparison.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(entityWithNullFacetValuesForTimestampComparison, EntityState.Added)
            Else
                Me.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.AddObject(entityWithNullFacetValuesForTimestampComparison)
            End If
        End Sub
        
        Public Sub UpdateEntityWithNullFacetValuesForTimestampComparison(ByVal currentEntityWithNullFacetValuesForTimestampComparison As EntityWithNullFacetValuesForTimestampComparison)
            Me.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.AttachAsModified(currentEntityWithNullFacetValuesForTimestampComparison)
        End Sub
        
        Public Sub DeleteEntityWithNullFacetValuesForTimestampComparison(ByVal entityWithNullFacetValuesForTimestampComparison As EntityWithNullFacetValuesForTimestampComparison)
            If ((entityWithNullFacetValuesForTimestampComparison.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(entityWithNullFacetValuesForTimestampComparison, EntityState.Deleted)
            Else
                Me.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.Attach(entityWithNullFacetValuesForTimestampComparison)
                Me.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.DeleteObject(entityWithNullFacetValuesForTimestampComparison)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'RequiredAttributeTestEntities' query.
        Public Function GetRequiredAttributeTestEntities() As IQueryable(Of RequiredAttributeTestEntity)
            Return Me.ObjectContext.RequiredAttributeTestEntities
        End Function
        
        Public Sub InsertRequiredAttributeTestEntity(ByVal requiredAttributeTestEntity As RequiredAttributeTestEntity)
            If ((requiredAttributeTestEntity.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(requiredAttributeTestEntity, EntityState.Added)
            Else
                Me.ObjectContext.RequiredAttributeTestEntities.AddObject(requiredAttributeTestEntity)
            End If
        End Sub
        
        Public Sub UpdateRequiredAttributeTestEntity(ByVal currentRequiredAttributeTestEntity As RequiredAttributeTestEntity)
            Me.ObjectContext.RequiredAttributeTestEntities.AttachAsModified(currentRequiredAttributeTestEntity, Me.ChangeSet.GetOriginal(currentRequiredAttributeTestEntity))
        End Sub
        
        Public Sub DeleteRequiredAttributeTestEntity(ByVal requiredAttributeTestEntity As RequiredAttributeTestEntity)
            If ((requiredAttributeTestEntity.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(requiredAttributeTestEntity, EntityState.Deleted)
            Else
                Me.ObjectContext.RequiredAttributeTestEntities.Attach(requiredAttributeTestEntity)
                Me.ObjectContext.RequiredAttributeTestEntities.DeleteObject(requiredAttributeTestEntity)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'TimestampEntities' query.
        Public Function GetTimestampEntities() As IQueryable(Of TimestampEntity)
            Return Me.ObjectContext.TimestampEntities
        End Function
        
        Public Sub InsertTimestampEntity(ByVal timestampEntity As TimestampEntity)
            If ((timestampEntity.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(timestampEntity, EntityState.Added)
            Else
                Me.ObjectContext.TimestampEntities.AddObject(timestampEntity)
            End If
        End Sub
        
        Public Sub UpdateTimestampEntity(ByVal currentTimestampEntity As TimestampEntity)
            Me.ObjectContext.TimestampEntities.AttachAsModified(currentTimestampEntity)
        End Sub
        
        Public Sub DeleteTimestampEntity(ByVal timestampEntity As TimestampEntity)
            If ((timestampEntity.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(timestampEntity, EntityState.Deleted)
            Else
                Me.ObjectContext.TimestampEntities.Attach(timestampEntity)
                Me.ObjectContext.TimestampEntities.DeleteObject(timestampEntity)
            End If
        End Sub
    End Class
End Namespace
