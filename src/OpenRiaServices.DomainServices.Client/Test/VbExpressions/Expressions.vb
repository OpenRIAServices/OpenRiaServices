Public Class Expressions

    Public Shared Function IIfWithEqualComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (IIf(item.Title = "Supreme Leader", 1, 0) = 1)
    End Function

    Public Shared Function IIfWithEqualComparisonWithBools(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (IIf(item.Title = "Supreme Leader", True, False))
    End Function

    Public Shared Function IIfWithNotEqualComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (1 <> IIf(item.Title = "Supreme Leader", 1, 0))
    End Function

    Public Shared Function IIfWithLessThanComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (1 < IIf(item.Title = "Supreme Leader", 1, 0))
    End Function

    Public Shared Function IIfWithLessThanOrEqualComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (1 <= IIf(item.Title = "Supreme Leader", 1, 0))
    End Function

    Public Shared Function IIfWithGreaterThanComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (1 > IIf(item.Title = "Supreme Leader", 1, 0))
    End Function

    Public Shared Function IIfWithGreaterThanOrEqualComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where (1 >= IIf(item.Title = "Supreme Leader", 1, 0))
    End Function

    Public Shared Function IIfWithLessThanStringComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where ("1" < IIf(item.Title = "Supreme Leader", "1", "0"))
    End Function

    Public Shared Function IIfWithEqualStringComparison(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where ("1" = IIf(item.Title = "Supreme Leader", "1", "0"))
    End Function

    Public Shared Function AddAndNegateChecked(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where item.Key + -1 = 0
    End Function

    Public Shared Function SubtractChecked(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where item.Key - 1 = 0
    End Function

    Public Shared Function MultiplyChecked(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where item.Key * 1 = 1
    End Function

    Public Shared Function ConvertChecked(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where CType(item.Key, Single) = 1
    End Function

    Public Shared Function NullComparison1(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return entities.Where(Function(order) order.NullableInt > 0)
    End Function

    Public Shared Function NullComparison2(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where item.NullableInt > 0
    End Function

    Public Shared Function NullComparison3(ByVal entities As IQueryable(Of GenericEntity)) As IQueryable(Of GenericEntity)
        Return From item In entities _
               Where item.NullableInt > item.Key
    End Function

End Class
