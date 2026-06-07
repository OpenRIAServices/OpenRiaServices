using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using PolyType;
using PolyType.Abstractions;
using OpenRiaServices.Server;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;

public sealed class FilteredTypeShapeProvider : ITypeShapeProvider
{
    private readonly ITypeShapeProvider _baseProvider;
    private readonly FilteredTypeShapeProviderOptions _options;
    private readonly ConcurrentDictionary<Type, Lazy<ITypeShape?>> _shapeCache = new();

    public FilteredTypeShapeProvider(ITypeShapeProvider baseProvider, FilteredTypeShapeProviderOptions? options = null)
    {
        _baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
        _options = options ?? FilteredTypeShapeProviderOptions.Default;
    }

    public ITypeShape? GetTypeShape(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Lazy<ITypeShape?> lazy = _shapeCache.GetOrAdd(
            type,
            static (t, state) => new Lazy<ITypeShape?>(
                () => state.WrapCore(state._baseProvider.GetTypeShape(t)),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        return lazy.Value;
    }

    internal ITypeShape Wrap(ITypeShape inner)
    {
        if (inner is IObjectTypeShape objectTypeShape)
            return GetTypeShape(objectTypeShape.Type)!;

        return WrapCore(inner)!;
    }

    internal ITypeShape? WrapCore(ITypeShape? inner)
    {
        if (inner is null)
        {
            return null;
        }

        if (ReferenceEquals(inner.Provider, this))
        {
            return inner;
        }

        if (inner is IObjectTypeShape)
        {
            Type wrapperType = typeof(ObjectTypeShapeWrapper<>).MakeGenericType(inner.Type);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        if (inner is IEnumTypeShape)
        {
            Type[] args = GetGenericArguments(inner, typeof(IEnumTypeShape<,>));
            Type wrapperType = typeof(EnumTypeShapeWrapper<,>).MakeGenericType(args);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        if (inner is IOptionalTypeShape)
        {
            Type[] args = GetGenericArguments(inner, typeof(IOptionalTypeShape<,>));
            Type wrapperType = typeof(OptionalTypeShapeWrapper<,>).MakeGenericType(args);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        if (inner is IEnumerableTypeShape)
        {
            Type[] args = GetGenericArguments(inner, typeof(IEnumerableTypeShape<,>));
            Type wrapperType = typeof(EnumerableTypeShapeWrapper<,>).MakeGenericType(args);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        if (inner is IDictionaryTypeShape)
        {
            Type[] args = GetGenericArguments(inner, typeof(IDictionaryTypeShape<,,>));
            Type wrapperType = typeof(DictionaryTypeShapeWrapper<,,>).MakeGenericType(args);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        if (inner is ISurrogateTypeShape)
        {
            Type[] args = GetGenericArguments(inner, typeof(ISurrogateTypeShape<,>));
            Type wrapperType = typeof(SurrogateTypeShapeWrapper<,>).MakeGenericType(args);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        if (inner is IUnionTypeShape)
        {
            Type[] args = GetGenericArguments(inner, typeof(IUnionTypeShape<>));
            Type wrapperType = typeof(UnionTypeShapeWrapper<>).MakeGenericType(args);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        if (inner is IFunctionTypeShape)
        {
            Type[] args = GetGenericArguments(inner, typeof(IFunctionTypeShape<,,>));
            Type wrapperType = typeof(FunctionTypeShapeWrapper<,,>).MakeGenericType(args);
            return (ITypeShape)Activator.CreateInstance(wrapperType, this, inner)!;
        }

        Type defaultWrapperType = typeof(GenericTypeShapeWrapper<>).MakeGenericType(inner.Type);
        return (ITypeShape)Activator.CreateInstance(defaultWrapperType, this, inner)!;
    }

    internal IPropertyShape WrapProperty(IPropertyShape propertyShape)
    {
        Type[] args = GetGenericArguments(propertyShape, typeof(IPropertyShape<,>));
        Type wrapperType = typeof(PropertyShapeWrapper<,>).MakeGenericType(args);
        return (IPropertyShape)Activator.CreateInstance(wrapperType, this, propertyShape)!;
    }

    internal IMethodShape WrapMethod(IMethodShape methodShape)
    {
        Type[] args = GetGenericArguments(methodShape, typeof(IMethodShape<,,>));
        Type wrapperType = typeof(MethodShapeWrapper<,,>).MakeGenericType(args);
        return (IMethodShape)Activator.CreateInstance(wrapperType, this, methodShape)!;
    }

    internal IEventShape WrapEvent(IEventShape eventShape)
    {
        Type[] args = GetGenericArguments(eventShape, typeof(IEventShape<,>));
        Type wrapperType = typeof(EventShapeWrapper<,>).MakeGenericType(args);
        return (IEventShape)Activator.CreateInstance(wrapperType, this, eventShape)!;
    }

    internal IConstructorShape? WrapConstructor(IConstructorShape? constructorShape)
    {
        if (constructorShape is null)
        {
            return null;
        }

        Type[] args = GetGenericArguments(constructorShape, typeof(IConstructorShape<,>));
        Type wrapperType = typeof(ConstructorShapeWrapper<,>).MakeGenericType(args);
        return (IConstructorShape)Activator.CreateInstance(wrapperType, this, constructorShape)!;
    }

    internal IParameterShape WrapParameter(IParameterShape parameterShape)
    {
        Type[] args = GetGenericArguments(parameterShape, typeof(IParameterShape<,>));
        Type wrapperType = typeof(ParameterShapeWrapper<,>).MakeGenericType(args);
        return (IParameterShape)Activator.CreateInstance(wrapperType, this, parameterShape)!;
    }

    internal IUnionCaseShape WrapUnionCase(IUnionCaseShape unionCaseShape)
    {
        Type[] args = GetGenericArguments(unionCaseShape, typeof(IUnionCaseShape<,>));
        Type wrapperType = typeof(UnionCaseShapeWrapper<,>).MakeGenericType(args);
        return (IUnionCaseShape)Activator.CreateInstance(wrapperType, this, unionCaseShape)!;
    }

    internal bool ShouldIncludeProperty(Type declaringType, IPropertyShape property)
    {
        if (TypeUtility.IsPredefinedType(declaringType))
            return true;

        // Skip hosting specific envelope types
        if (declaringType.Assembly == typeof(MessagePackResponseEnvelopeBase).Assembly
                || declaringType.BaseType == typeof(QueryResult))
        {
            return true;
        }

        // Similart to SerializationUtility.IsSerializableDataMember.
        if (!(TypeUtility.IsPredefinedType(property.PropertyType.Type) || TypeUtility.IsSupportedComplexType(property.PropertyType.Type)))
        {
            return false;
        }

        MetaType metaType = MetaType.GetMetaType(declaringType);
        MetaMember metaMember = metaType[property.Name];

        if (metaMember is null)
            return false;

        if (metaMember.IsAssociationMember)
        {
            return _options.AssociationMemberSerializationMode == AssociationMemberSerializationMode.Include
                && metaType.IncludedAssociations.Find(property.Name, ignoreCase: false) is not null;
        }

        return metaMember.IsDataMember;

        /*

        var props = TypeDescriptor.GetProperties(declaringType);
        var propDesc = props[property.Name];

        bool epected1 = propDesc is not null
            && SerializationUtility.IsSerializableDataMember(propDesc);
*/
    }

    //internal static IGenericCustomAttributeProvider CreateTypeAttributeProvider(Type type)
    //{
    //    return new TypeDescriptorAttributeProvider(TypeDescriptor.GetAttributes(type));
    //}

    //internal IGenericCustomAttributeProvider CreatePropertyAttributeProvider(Type declaringType, string propertyName, IGenericCustomAttributeProvider fallback)
    //{
    //    PropertyDescriptor? descriptor = TypeDescriptor.GetProperties(declaringType)[propertyName];
    //    return descriptor is null ? fallback : new TypeDescriptorAttributeProvider(descriptor.Attributes);
    //}

    private static Type[] GetGenericArguments(object instance, Type genericInterfaceDefinition)
    {
        Type? interfaceType = instance.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceDefinition);

        if (interfaceType is null)
        {
            throw new InvalidOperationException($"Type '{instance.GetType()}' does not implement '{genericInterfaceDefinition}'.");
        }

        return interfaceType.GetGenericArguments();
    }
}

#pragma warning disable PT0019

internal sealed class TypeDescriptorAttributeProvider : IGenericCustomAttributeProvider
{
    private readonly AttributeCollection _attributes;

    public TypeDescriptorAttributeProvider(AttributeCollection attributes)
    {
        _attributes = attributes;
    }

    public object[] GetCustomAttributes(bool inherit)
    {
        return _attributes.Cast<Attribute>().ToArray();
    }

    public object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        ArgumentNullException.ThrowIfNull(attributeType);
        return _attributes.Cast<Attribute>().Where(attributeType.IsInstanceOfType).ToArray();
    }

    public bool IsDefined(Type attributeType, bool inherit)
    {
        ArgumentNullException.ThrowIfNull(attributeType);
        return _attributes.Cast<Attribute>().Any(attributeType.IsInstanceOfType);
    }

    public TAttribute? GetCustomAttribute<TAttribute>(bool inherit = true) where TAttribute : Attribute
    {
        return _attributes.OfType<TAttribute>().FirstOrDefault();
    }

    public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>(bool inherit = true) where TAttribute : Attribute
    {
        return _attributes.OfType<TAttribute>();
    }

    public bool IsDefined<TAttribute>(bool inherit = true) where TAttribute : Attribute
    {
        return _attributes.OfType<TAttribute>().Any();
    }
}

internal abstract class TypeShapeWrapper<T> : ITypeShape<T>
{
    private readonly ITypeShape<T> _inner;
    private readonly FilteredTypeShapeProvider _provider;
    private readonly Lazy<IReadOnlyList<IMethodShape>> _methods;
    private readonly Lazy<IReadOnlyList<IEventShape>> _events;
    //private readonly Lazy<IGenericCustomAttributeProvider> _attributes;

    protected TypeShapeWrapper(FilteredTypeShapeProvider provider, ITypeShape<T> inner)
    {
        _provider = provider;
        _inner = inner;
        _methods = new Lazy<IReadOnlyList<IMethodShape>>(() => inner.Methods.Select(provider.WrapMethod).ToArray());
        _events = new Lazy<IReadOnlyList<IEventShape>>(() => inner.Events.Select(provider.WrapEvent).ToArray());

        // TODO: Merge attributes with inner attributes (we only need to add missing attributes)
        //inner.AttributeProvider
        //_attributes = new Lazy<IGenericCustomAttributeProvider>(() => provider.CreateTypeAttributeProvider(inner.Type));
    }

    protected ITypeShape<T> Inner => _inner;
    protected FilteredTypeShapeProvider ProviderCore => _provider;

    public Type Type => _inner.Type;
    public TypeShapeKind Kind => _inner.Kind;
    public ITypeShapeProvider Provider => _provider;
    public IGenericCustomAttributeProvider AttributeProvider => _inner.AttributeProvider;
    public IReadOnlyList<IMethodShape> Methods => _methods.Value;
    public IReadOnlyList<IEventShape> Events => _events.Value;

    public virtual object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return _inner.Accept(visitor, state);
    }

    public virtual object? Invoke(ITypeShapeFunc func, object? state = null)
    {
        return func.Invoke(this, state);
    }

    public ITypeShape? GetAssociatedTypeShape(Type associatedType)
    {
        ITypeShape? associatedShape = _inner.GetAssociatedTypeShape(associatedType);
        return associatedShape is null ? null : _provider.Wrap(associatedShape);
    }
}

internal sealed class GenericTypeShapeWrapper<T>(FilteredTypeShapeProvider provider, ITypeShape<T> inner)
    : TypeShapeWrapper<T>(provider, inner)
{
}

internal sealed class ObjectTypeShapeWrapper<T> : TypeShapeWrapper<T>, IObjectTypeShape<T>
{
    private readonly IObjectTypeShape<T> _inner;
    private readonly Lazy<HashSet<string>> _includedPropertyNames;
    private readonly Lazy<IReadOnlyList<IPropertyShape>> _properties;

    public ObjectTypeShapeWrapper(FilteredTypeShapeProvider provider, IObjectTypeShape inner)
        : base(provider, (IObjectTypeShape<T>)inner)
    {
        _inner = (IObjectTypeShape<T>)inner;
        _includedPropertyNames = new Lazy<HashSet<string>>(() =>
            _inner.Properties
                .Where(property => ProviderCore.ShouldIncludeProperty(typeof(T), property))
                .Select(property => property.MemberInfo?.Name ?? property.Name)
                .ToHashSet(StringComparer.Ordinal));
        _properties = new Lazy<IReadOnlyList<IPropertyShape>>(() =>
            _inner.Properties.Where(property => ProviderCore.ShouldIncludeProperty(typeof(T), property)).Select(ProviderCore.WrapProperty).ToArray());
    }

    public bool IsRecordType => _inner.IsRecordType;
    public bool IsTupleType => _inner.IsTupleType;
    public IReadOnlyList<IPropertyShape> Properties => _properties.Value;
    public IConstructorShape? Constructor
    {
        get
        {
            IConstructorShape? wrapped = ProviderCore.WrapConstructor(_inner.Constructor);
            if (wrapped is null)
            {
                return null;
            }

            bool hasFilteredInitializerParameters = wrapped.Parameters.Any(parameter =>
                parameter.Kind == ParameterKind.MemberInitializer &&
                !_includedPropertyNames.Value.Contains(parameter.MemberInfo?.Name ?? parameter.Name));

            return hasFilteredInitializerParameters ? null : wrapped;
        }
    }

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitObject(this, state);
    }
}

internal sealed class PropertyShapeWrapper<TDeclaringType, TPropertyType>(FilteredTypeShapeProvider provider, IPropertyShape inner)
    : IPropertyShape<TDeclaringType, TPropertyType>
{
    private readonly IPropertyShape<TDeclaringType, TPropertyType> _inner = (IPropertyShape<TDeclaringType, TPropertyType>)inner;
    private readonly Lazy<IObjectTypeShape<TDeclaringType>> _declaringType = new(() => (IObjectTypeShape<TDeclaringType>)provider.Wrap(((IPropertyShape<TDeclaringType, TPropertyType>)inner).DeclaringType)!);
    private readonly Lazy<ITypeShape<TPropertyType>> _propertyType = new(() => (ITypeShape<TPropertyType>)provider.Wrap(((IPropertyShape<TDeclaringType, TPropertyType>)inner).PropertyType)!);
    //private readonly Lazy<IGenericCustomAttributeProvider> _attributeProvider = new(() => provider.CreatePropertyAttributeProvider(typeof(TDeclaringType), ((IPropertyShape<TDeclaringType, TPropertyType>)inner).Name, ((IPropertyShape<TDeclaringType, TPropertyType>)inner).AttributeProvider));

    // TODO: Decide what to do about Projection Includes
    // Throw on error, includeAttribute.IsProjection
    // Can they be supported given that such a "Path" exist
    //  and we chain several existing properties together.
    // Should it be OBSOLETE
    // If the name is new then should a separate property be added
    // 

    string _name = (inner.AttributeProvider.GetCustomAttribute<IncludeAttribute>() is IncludeAttribute includeAttribute
        && includeAttribute.IsProjection) ? includeAttribute.MemberName : inner.Name;

    public int Position => _inner.Position;
    public string Name => _inner.Name;
    public MemberInfo? MemberInfo => _inner.MemberInfo;
    public IGenericCustomAttributeProvider AttributeProvider => _inner.AttributeProvider;
    public IObjectTypeShape DeclaringType => _declaringType.Value;
    IObjectTypeShape<TDeclaringType> IPropertyShape<TDeclaringType, TPropertyType>.DeclaringType => _declaringType.Value;
    public ITypeShape PropertyType => _propertyType.Value;
    ITypeShape<TPropertyType> IPropertyShape<TDeclaringType, TPropertyType>.PropertyType => _propertyType.Value;
    public bool HasGetter => _inner.HasGetter;
    public bool HasSetter => true;
    public bool IsField => _inner.IsField;
    public bool IsGetterPublic => _inner.IsGetterPublic;
    public bool IsSetterPublic => _inner.IsSetterPublic;
    public bool IsGetterNonNullable => _inner.IsGetterNonNullable;
    public bool IsSetterNonNullable => _inner.IsSetterNonNullable;

    public object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitProperty(this, state);
    }

    public Getter<TDeclaringType, TPropertyType> GetGetter()
    {
        return _inner.GetGetter();
    }

    public Setter<TDeclaringType, TPropertyType> GetSetter()
    {
        if (_inner.HasSetter)
            return _inner.GetSetter();
        else
            return (ref TDeclaringType instance, TPropertyType value) => { };
    }
}

internal sealed class EnumerableTypeShapeWrapper<TEnumerable, TElement>(FilteredTypeShapeProvider provider, ITypeShape inner)
    : TypeShapeWrapper<TEnumerable>(provider, (IEnumerableTypeShape<TEnumerable, TElement>)inner), IEnumerableTypeShape<TEnumerable, TElement>
{
    private readonly IEnumerableTypeShape<TEnumerable, TElement> _inner = (IEnumerableTypeShape<TEnumerable, TElement>)inner;

    public ITypeShape ElementType => ProviderCore.Wrap(_inner.ElementType)!;
    ITypeShape<TElement> IEnumerableTypeShape<TEnumerable, TElement>.ElementType => (ITypeShape<TElement>)ProviderCore.Wrap(_inner.ElementType)!;
    public CollectionConstructionStrategy ConstructionStrategy => _inner.ConstructionStrategy;
    public CollectionComparerOptions SupportedComparer => _inner.SupportedComparer;
    public int Rank => _inner.Rank;
    public bool IsAsyncEnumerable => _inner.IsAsyncEnumerable;
    public bool IsSetType => _inner.IsSetType;

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitEnumerable(this, state);
    }

    public Func<TEnumerable, IEnumerable<TElement>> GetGetEnumerable() => _inner.GetGetEnumerable();
    public MutableCollectionConstructor<TElement, TEnumerable> GetDefaultConstructor() => _inner.GetDefaultConstructor();
    public EnumerableAppender<TEnumerable, TElement> GetAppender() => _inner.GetAppender();
    public ParameterizedCollectionConstructor<TElement, TElement, TEnumerable> GetParameterizedConstructor() => _inner.GetParameterizedConstructor();
}

internal sealed class DictionaryTypeShapeWrapper<TDictionary, TKey, TValue>(FilteredTypeShapeProvider provider, ITypeShape inner)
    : TypeShapeWrapper<TDictionary>(provider, (IDictionaryTypeShape<TDictionary, TKey, TValue>)inner), IDictionaryTypeShape<TDictionary, TKey, TValue>
    where TKey : notnull
{
    private readonly IDictionaryTypeShape<TDictionary, TKey, TValue> _inner = (IDictionaryTypeShape<TDictionary, TKey, TValue>)inner;

    public ITypeShape KeyType => ProviderCore.Wrap(_inner.KeyType)!;
    public ITypeShape ValueType => ProviderCore.Wrap(_inner.ValueType)!;
    ITypeShape<TKey> IDictionaryTypeShape<TDictionary, TKey, TValue>.KeyType => (ITypeShape<TKey>)ProviderCore.Wrap(_inner.KeyType)!;
    ITypeShape<TValue> IDictionaryTypeShape<TDictionary, TKey, TValue>.ValueType => (ITypeShape<TValue>)ProviderCore.Wrap(_inner.ValueType)!;
    public CollectionConstructionStrategy ConstructionStrategy => _inner.ConstructionStrategy;
    public CollectionComparerOptions SupportedComparer => _inner.SupportedComparer;
    public DictionaryInsertionMode AvailableInsertionModes => _inner.AvailableInsertionModes;

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitDictionary(this, state);
    }

    public Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> GetGetDictionary() => _inner.GetGetDictionary();
    public MutableCollectionConstructor<TKey, TDictionary> GetDefaultConstructor() => _inner.GetDefaultConstructor();
    public DictionaryInserter<TDictionary, TKey, TValue> GetInserter(DictionaryInsertionMode insertionMode = DictionaryInsertionMode.None) => _inner.GetInserter(insertionMode);
    public ParameterizedCollectionConstructor<TKey, KeyValuePair<TKey, TValue>, TDictionary> GetParameterizedConstructor() => _inner.GetParameterizedConstructor();
}

internal sealed class OptionalTypeShapeWrapper<TOptional, TElement>(FilteredTypeShapeProvider provider, ITypeShape inner)
    : TypeShapeWrapper<TOptional>(provider, (IOptionalTypeShape<TOptional, TElement>)inner), IOptionalTypeShape<TOptional, TElement>
{
    private readonly IOptionalTypeShape<TOptional, TElement> _inner = (IOptionalTypeShape<TOptional, TElement>)inner;

    public ITypeShape ElementType => ProviderCore.Wrap(_inner.ElementType)!;
    ITypeShape<TElement> IOptionalTypeShape<TOptional, TElement>.ElementType => (ITypeShape<TElement>)ProviderCore.Wrap(_inner.ElementType)!;

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitOptional(this, state);
    }

    public Func<TOptional> GetNoneConstructor() => _inner.GetNoneConstructor();
    public Func<TElement, TOptional> GetSomeConstructor() => _inner.GetSomeConstructor();
    public OptionDeconstructor<TOptional, TElement> GetDeconstructor() => _inner.GetDeconstructor();
}

internal sealed class SurrogateTypeShapeWrapper<T, TSurrogate>(FilteredTypeShapeProvider provider, ITypeShape inner)
    : TypeShapeWrapper<T>(provider, (ISurrogateTypeShape<T, TSurrogate>)inner), ISurrogateTypeShape<T, TSurrogate>
{
    private readonly ISurrogateTypeShape<T, TSurrogate> _inner = (ISurrogateTypeShape<T, TSurrogate>)inner;

    public IMarshaler<T, TSurrogate> Marshaler => _inner.Marshaler;
    public ITypeShape SurrogateType => ProviderCore.Wrap(_inner.SurrogateType)!;
    ITypeShape<TSurrogate> ISurrogateTypeShape<T, TSurrogate>.SurrogateType => (ITypeShape<TSurrogate>)ProviderCore.Wrap(_inner.SurrogateType)!;

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitSurrogate(this, state);
    }
}

internal sealed class UnionTypeShapeWrapper<TUnion> : TypeShapeWrapper<TUnion>, IUnionTypeShape<TUnion>
{
    private readonly IUnionTypeShape<TUnion> _inner;
    private readonly Lazy<IReadOnlyList<IUnionCaseShape>> _unionCases;
    private readonly Lazy<ITypeShape<TUnion>> _base;

    public UnionTypeShapeWrapper(FilteredTypeShapeProvider provider, ITypeShape inner)
        : base(provider, (IUnionTypeShape<TUnion>)inner)
    {
        _inner = (IUnionTypeShape<TUnion>)inner;
        _unionCases = new Lazy<IReadOnlyList<IUnionCaseShape>>(() => _inner.UnionCases.Select(ProviderCore.WrapUnionCase).ToArray());
        // The base and "union" case have the same Type so need to call wrap core
        _base = new (() => (ITypeShape<TUnion>)provider.WrapCore(_inner.BaseType)!);
    }

    public ITypeShape BaseType => _base.Value;
    ITypeShape<TUnion> IUnionTypeShape<TUnion>.BaseType => _base.Value;
    public IReadOnlyList<IUnionCaseShape> UnionCases => _unionCases.Value;

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitUnion(this, state);
    }

    public Getter<TUnion, int> GetGetUnionCaseIndex() => _inner.GetGetUnionCaseIndex();
}

internal sealed class UnionCaseShapeWrapper<TUnionCase, TUnion>(FilteredTypeShapeProvider provider, IUnionCaseShape inner)
    : IUnionCaseShape<TUnionCase, TUnion>
{
    private readonly IUnionCaseShape<TUnionCase, TUnion> _inner = (IUnionCaseShape<TUnionCase, TUnion>)inner;

    public string Name => _inner.Name;
    public int Tag => _inner.Tag;
    public bool IsTagSpecified => _inner.IsTagSpecified;
    public int Index => _inner.Index;
    public ITypeShape UnionCaseType => provider.Wrap(_inner.UnionCaseType)!;
    ITypeShape<TUnionCase> IUnionCaseShape<TUnionCase, TUnion>.UnionCaseType => (ITypeShape<TUnionCase>)provider.Wrap(_inner.UnionCaseType)!;
    public IMarshaler<TUnionCase, TUnion> Marshaler => _inner.Marshaler;

    public object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitUnionCase(this, state);
    }
}

internal sealed class EnumTypeShapeWrapper<TEnum, TUnderlying>(FilteredTypeShapeProvider provider, ITypeShape inner)
    : TypeShapeWrapper<TEnum>(provider, (IEnumTypeShape<TEnum, TUnderlying>)inner), IEnumTypeShape<TEnum, TUnderlying>
    where TEnum : struct, Enum
    where TUnderlying : unmanaged
{
    private readonly IEnumTypeShape<TEnum, TUnderlying> _inner = (IEnumTypeShape<TEnum, TUnderlying>)inner;

    public ITypeShape UnderlyingType => ProviderCore.Wrap(_inner.UnderlyingType)!;
    ITypeShape<TUnderlying> IEnumTypeShape<TEnum, TUnderlying>.UnderlyingType => (ITypeShape<TUnderlying>)ProviderCore.Wrap(_inner.UnderlyingType)!;
    public bool IsFlags => _inner.IsFlags;
    public IReadOnlyDictionary<string, TUnderlying> Members => _inner.Members;

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitEnum(this, state);
    }
}

internal sealed class FunctionTypeShapeWrapper<TFunction, TArgumentState, TResult> : TypeShapeWrapper<TFunction>, IFunctionTypeShape<TFunction, TArgumentState, TResult>
    where TArgumentState : IArgumentState
{
    private readonly IFunctionTypeShape<TFunction, TArgumentState, TResult> _inner;
    private readonly Lazy<IReadOnlyList<IParameterShape>> _parameters;

    public FunctionTypeShapeWrapper(FilteredTypeShapeProvider provider, ITypeShape inner)
        : base(provider, (IFunctionTypeShape<TFunction, TArgumentState, TResult>)inner)
    {
        _inner = (IFunctionTypeShape<TFunction, TArgumentState, TResult>)inner;
        _parameters = new Lazy<IReadOnlyList<IParameterShape>>(() => _inner.Parameters.Select(ProviderCore.WrapParameter).ToArray());
    }

    public ITypeShape ReturnType => ProviderCore.Wrap(_inner.ReturnType)!;
    ITypeShape<TResult> IFunctionTypeShape<TFunction, TArgumentState, TResult>.ReturnType => (ITypeShape<TResult>)ProviderCore.Wrap(_inner.ReturnType)!;
    public bool IsVoidLike => _inner.IsVoidLike;
    public bool IsAsync => _inner.IsAsync;
    public IReadOnlyList<IParameterShape> Parameters => _parameters.Value;

    public override object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitFunction(this, state);
    }

    public Func<TArgumentState> GetArgumentStateConstructor() => _inner.GetArgumentStateConstructor();
    public MethodInvoker<TFunction, TArgumentState, TResult> GetFunctionInvoker() => _inner.GetFunctionInvoker();
    public TFunction FromDelegate(RefFunc<TArgumentState, TResult> innerFunc) => _inner.FromDelegate(innerFunc);
    public TFunction FromAsyncDelegate(RefFunc<TArgumentState, ValueTask<TResult>> innerFunc) => _inner.FromAsyncDelegate(innerFunc);
}

internal sealed class MethodShapeWrapper<TDeclaringType, TArgumentState, TResult> : IMethodShape<TDeclaringType, TArgumentState, TResult>
    where TArgumentState : IArgumentState
{
    private readonly FilteredTypeShapeProvider _provider;
    private readonly IMethodShape<TDeclaringType, TArgumentState, TResult> _inner;
    private readonly Lazy<IReadOnlyList<IParameterShape>> _parameters;

    public MethodShapeWrapper(FilteredTypeShapeProvider provider, IMethodShape inner)
    {
        _provider = provider;
        _inner = (IMethodShape<TDeclaringType, TArgumentState, TResult>)inner;
        _parameters = new Lazy<IReadOnlyList<IParameterShape>>(() => _inner.Parameters.Select(_provider.WrapParameter).ToArray());
    }

    public ITypeShape DeclaringType => _provider.Wrap(_inner.DeclaringType)!;
    ITypeShape<TDeclaringType> IMethodShape<TDeclaringType, TArgumentState, TResult>.DeclaringType => (ITypeShape<TDeclaringType>)_provider.Wrap(_inner.DeclaringType)!;
    public ITypeShape ReturnType => _provider.Wrap(_inner.ReturnType)!;
    ITypeShape<TResult> IMethodShape<TDeclaringType, TArgumentState, TResult>.ReturnType => (ITypeShape<TResult>)_provider.Wrap(_inner.ReturnType)!;
    public string Name => _inner.Name;
    public bool IsPublic => _inner.IsPublic;
    public bool IsStatic => _inner.IsStatic;
    public bool IsVoidLike => _inner.IsVoidLike;
    public bool IsAsync => _inner.IsAsync;
    public MethodBase? MethodBase => _inner.MethodBase;
    public IGenericCustomAttributeProvider AttributeProvider => _inner.AttributeProvider;
    public IReadOnlyList<IParameterShape> Parameters => _parameters.Value;

    public object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitMethod(this, state);
    }

    public Func<TArgumentState> GetArgumentStateConstructor() => _inner.GetArgumentStateConstructor();
    public MethodInvoker<TDeclaringType?, TArgumentState, TResult> GetMethodInvoker() => _inner.GetMethodInvoker();
}

internal sealed class ConstructorShapeWrapper<TDeclaringType, TArgumentState> : IConstructorShape<TDeclaringType, TArgumentState>
    where TArgumentState : IArgumentState
{
    private readonly FilteredTypeShapeProvider _provider;
    private readonly IConstructorShape<TDeclaringType, TArgumentState> _inner;
    private readonly Lazy<IReadOnlyList<IParameterShape>> _parameters;

    public ConstructorShapeWrapper(FilteredTypeShapeProvider provider, IConstructorShape inner)
    {
        _provider = provider;
        _inner = (IConstructorShape<TDeclaringType, TArgumentState>)inner;
        _parameters = new Lazy<IReadOnlyList<IParameterShape>>(() => _inner.Parameters.Select(_provider.WrapParameter).ToArray());
    }

    public IObjectTypeShape DeclaringType => (IObjectTypeShape)_provider.Wrap(_inner.DeclaringType)!;
    IObjectTypeShape<TDeclaringType> IConstructorShape<TDeclaringType, TArgumentState>.DeclaringType => (IObjectTypeShape<TDeclaringType>)_provider.Wrap(_inner.DeclaringType)!;
    public bool IsPublic => _inner.IsPublic;
    public MethodBase? MethodBase => _inner.MethodBase;
    public IGenericCustomAttributeProvider AttributeProvider => _inner.AttributeProvider;
    public IReadOnlyList<IParameterShape> Parameters => _parameters.Value;

    public object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitConstructor(this, state);
    }

    public Func<TDeclaringType> GetDefaultConstructor() => _inner.GetDefaultConstructor();
    public Func<TArgumentState> GetArgumentStateConstructor() => _inner.GetArgumentStateConstructor();
    public Constructor<TArgumentState, TDeclaringType> GetParameterizedConstructor() => _inner.GetParameterizedConstructor();
}

internal sealed class ParameterShapeWrapper<TArgumentState, TParameterType>(FilteredTypeShapeProvider provider, IParameterShape inner)
    : IParameterShape<TArgumentState, TParameterType>
    where TArgumentState : IArgumentState
{
    private readonly IParameterShape<TArgumentState, TParameterType> _inner = (IParameterShape<TArgumentState, TParameterType>)inner;

    public int Position => _inner.Position;
    public ITypeShape ParameterType => provider.Wrap(_inner.ParameterType)!;
    ITypeShape<TParameterType> IParameterShape<TArgumentState, TParameterType>.ParameterType => (ITypeShape<TParameterType>)provider.Wrap(_inner.ParameterType)!;
    public string Name => _inner.Name;
    public ParameterKind Kind => _inner.Kind;
    public bool HasDefaultValue => _inner.HasDefaultValue;
    object? IParameterShape.DefaultValue => _inner.DefaultValue;
    public TParameterType? DefaultValue => _inner.DefaultValue;
    public bool IsRequired => _inner.IsRequired;
    public bool IsNonNullable => _inner.IsNonNullable;
    public bool IsPublic => _inner.IsPublic;
    public ParameterInfo? ParameterInfo => _inner.ParameterInfo;
    public MemberInfo? MemberInfo => _inner.MemberInfo;
    public IGenericCustomAttributeProvider AttributeProvider => _inner.AttributeProvider;

    public object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitParameter(this, state);
    }

    public Getter<TArgumentState, TParameterType> GetGetter() => _inner.GetGetter();
    public Setter<TArgumentState, TParameterType> GetSetter() => _inner.GetSetter();
}

internal sealed class EventShapeWrapper<TDeclaringType, TEventHandler>(FilteredTypeShapeProvider provider, IEventShape inner)
    : IEventShape<TDeclaringType, TEventHandler>
{
    private readonly IEventShape<TDeclaringType, TEventHandler> _inner = (IEventShape<TDeclaringType, TEventHandler>)inner;

    public string Name => _inner.Name;
    public bool IsStatic => _inner.IsStatic;
    public bool IsPublic => _inner.IsPublic;
    public ITypeShape DeclaringType => provider.Wrap(_inner.DeclaringType)!;
    ITypeShape<TDeclaringType> IEventShape<TDeclaringType, TEventHandler>.DeclaringType => (ITypeShape<TDeclaringType>)provider.Wrap(_inner.DeclaringType)!;
    public IFunctionTypeShape HandlerType => (IFunctionTypeShape)provider.Wrap(_inner.HandlerType)!;
    public EventInfo? EventInfo => _inner.EventInfo;
    public IGenericCustomAttributeProvider AttributeProvider => _inner.AttributeProvider;

    public object? Accept(TypeShapeVisitor visitor, object? state = null)
    {
        return visitor.VisitEvent(this, state);
    }

    public Setter<TDeclaringType?, TEventHandler> GetAddHandler() => _inner.GetAddHandler();
    public Setter<TDeclaringType?, TEventHandler> GetRemoveHandler() => _inner.GetRemoveHandler();
}
