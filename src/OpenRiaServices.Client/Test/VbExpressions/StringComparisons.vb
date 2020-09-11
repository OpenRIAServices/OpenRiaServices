Option Compare Binary

Public Class StringComparisons

    Public Shared Function CompareStringEqual(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (item.Title = "Supreme Leader")
    End Function

    Public Shared Function CompareStringNotEqual(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (item.Title <> "Supreme Leader")
    End Function

    Public Shared Function CompareStringGreaterThan(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (item.Title > "Supreme Leader")
    End Function

    Public Shared Function CompareStringGreaterThanOrEqual(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (item.Title >= "Supreme Leader")
    End Function

    Public Shared Function CompareStringLessThan(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (item.Title < "Supreme Leader")
    End Function

    Public Shared Function CompareStringLessThanOrEqual(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (item.Title <= "Supreme Leader")
    End Function

End Class
