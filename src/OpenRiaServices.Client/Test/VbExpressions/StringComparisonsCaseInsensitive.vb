Option Compare Text

Public Class StringComparisonsCaseInsensitive

    Public Shared Function CompareStringEqualCaseInsensitive(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (item.Title = "peon")
    End Function

End Class
