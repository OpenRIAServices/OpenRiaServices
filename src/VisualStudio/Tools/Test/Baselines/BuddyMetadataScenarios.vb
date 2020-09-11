
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataModels.ScenarioModels
Imports OpenRiaServices.Hosting
Imports OpenRiaServices.LinqToSql
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Linq
Imports System.Linq

Namespace BizLogic.Test
    
    'Implements application logic using the BuddyMetadataScenariosDataContext context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class BuddyMetadataScenarios
        Inherits LinqToSqlDomainService(Of BuddyMetadataScenariosDataContext)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetEntities() As IQueryable(Of EntityPropertyNamedPublic)
            Return Me.DataContext.Entities
        End Function
        
        Public Sub InsertEntityPropertyNamedPublic(ByVal entityPropertyNamedPublic As EntityPropertyNamedPublic)
            Me.DataContext.Entities.InsertOnSubmit(entityPropertyNamedPublic)
        End Sub
        
        Public Sub UpdateEntityPropertyNamedPublic(ByVal currentEntityPropertyNamedPublic As EntityPropertyNamedPublic)
            Me.DataContext.Entities.Attach(currentEntityPropertyNamedPublic, Me.ChangeSet.GetOriginal(currentEntityPropertyNamedPublic))
        End Sub
        
        Public Sub DeleteEntityPropertyNamedPublic(ByVal entityPropertyNamedPublic As EntityPropertyNamedPublic)
            Me.DataContext.Entities.Attach(entityPropertyNamedPublic)
            Me.DataContext.Entities.DeleteOnSubmit(entityPropertyNamedPublic)
        End Sub
    End Class
End Namespace
