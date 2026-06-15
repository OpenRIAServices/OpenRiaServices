using System.Collections.Generic;
using System.Xml.Linq;
using Nerdbank.MessagePack;
using PolyType;

class CustomComparerProvider : IComparerProvider
{
    public IComparer<T> GetComparer<T>(ITypeShape<T> shape)
    {
        return SecureComparerProvider.Default.GetComparer<T>(shape);
    }

    public IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape)
    {
        if (typeof(T) == typeof(XElement))
            return EqualityComparer<T>.Default;

        return SecureComparerProvider.Default.GetEqualityComparer<T>(shape);
    }
}
