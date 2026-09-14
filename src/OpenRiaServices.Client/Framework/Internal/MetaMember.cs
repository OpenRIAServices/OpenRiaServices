using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenRiaServices.Client.Internal
{
    /// <summary>
    /// INTERNAL - Public API surface might change from version to version.
    /// This class caches all the interesting attributes of an property.
    /// </summary>
    [DebuggerDisplay("Name = {Member.Name}")]
    public sealed class MetaMember
    {
        private static MethodInfo s_getterDelegateHelper = typeof(MetaMember).GetMethod(nameof(MetaMember.CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        private static MethodInfo s_setterDelegateHelper = typeof(MetaMember).GetMethod(nameof(MetaMember.CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        private static readonly MethodInfo s_typedGetterDelegateHelper = typeof(MetaMember).GetMethod(nameof(MetaMember.CreateTypedGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        private Func<object, object> _getter;
        private Action<object, object> _setter;
        private Delegate _typedGetter;
        private IValueAccessor _valueAccessor;

        internal MetaMember(MetaType metaType, PropertyInfo property, bool isRoundtripEntity)
        {
            this.Member = property;
            this.MetaType = metaType;

            bool hasGetter = (property.GetGetMethod() != null);

            IsCollection = TypeUtility.IsSupportedCollectionType(property.PropertyType);
            IsComplex = TypeUtility.IsSupportedComplexType(property.PropertyType);
            if (hasGetter && (property.GetSetMethod() != null))
            {
                IsDataMember = (IsComplex || TypeUtility.IsPredefinedType(property.PropertyType))
                        && !TypeUtility.IsAttributeDefined(property, typeof(IgnoreDataMemberAttribute), false);
            }

            if (hasGetter)
            {
                RequiresValidation = hasGetter && TypeUtility.IsAttributeDefined(property, typeof(ValidationAttribute), true);
            }

            IsExternalReference = TypeUtility.IsAttributeDefined(property, typeof(ExternalReferenceAttribute), true);
            IsKeyMember = TypeUtility.IsAttributeDefined(property, typeof(KeyAttribute), false);
            IsComposition = TypeUtility.IsAttributeDefined(property, typeof(CompositionAttribute), false);

            if (property.GetCustomAttribute<EntityAssociationAttribute>(false) is { } association)
            {
                this.AssociationAttribute = association;
            }
#pragma warning disable CS0618 // Type or member is obsolete
            // TODO: Remove fallback when code generation has used the never attribute for a little while
            else if (property.GetCustomAttribute<AssociationAttribute>(false) is { } associationAttribute)
            {
                this.AssociationAttribute = new EntityAssociationAttribute(associationAttribute.Name, associationAttribute.ThisKeyMembers.ToArray(), associationAttribute.OtherKeyMembers.ToArray())
                {
                    IsForeignKey = associationAttribute.IsForeignKey,
                };
            }
#pragma warning restore CS0618 // Type or member is obsolete

            EditableAttribute = (EditableAttribute)property.GetCustomAttributes(typeof(EditableAttribute), false).SingleOrDefault();

            IsRoundtripMember = CheckIfRoundtripMember(this, isRoundtripEntity);
            IsMergable = CheckIfMergeableMember(this);
        }

        /// <summary>
        /// Gets the name of the property
        /// </summary>
        public string Name => Member.Name;

        /// <summary>
        /// Gets the CLR type of this property
        /// </summary>
        public Type PropertyType => Member.PropertyType;

        /// <summary>
        /// Get the <see cref="MetaType"/> where the property is defined
        /// </summary>
        public MetaType MetaType { get; }

        /// <summary>
        /// Gets any <see cref="EditableAttribute"/> applied to the property, or <c>null</c>
        /// if no attribute is specified for the property
        /// </summary>
        public EditableAttribute EditableAttribute { get; }

        /// <summary>
        /// Gets the underlying <see cref="PropertyInfo"/> for the property which this 
        /// instance represents.
        /// </summary>
        internal PropertyInfo Member { get; }

        /// <summary>
        /// Returns <c>true</c> if the property has a <see cref="AssociationAttribute"/> applied
        /// and is therefore part of a data relationship (such as a foreign key)
        /// </summary>
        public bool IsAssociationMember { get { return AssociationAttribute != null; } }

        /// <summary>
        /// Gets any <see cref="AssociationAttribute"/> applied to the property, or <c>null</c>
        /// if no attribute is specified for the property
        /// </summary>
        public EntityAssociationAttribute AssociationAttribute { get; }

        /// <summary>
        /// Gets a value indicating whether this member is one of the supported values to send between 
        /// server and client (and it has both getter and setter).
        /// It is used to determine if it is part of the data contract between server and client. 
        /// </summary>
        public bool IsDataMember { get; }

        /// <summary>
        /// Gets a value indicating whether this member is part of the <see cref="MetaType"/>s key.
        /// </summary>
        public bool IsKeyMember { get; }

        /// <summary>
        /// Gets a value indicating whether this member is part of the <see cref="MetaType"/>s key.
        /// </summary>
        public bool IsRoundtripMember { get; }

        /// <summary>
        /// Determines whether the specified type is a complex type or a collection of
        /// complex types.
        /// </summary>
        public bool IsComplex { get; }

        /// <summary>
        /// <c>true</c> if the member is annotated with a <see cref="ExternalReferenceAttribute"/>
        /// </summary>
        public bool IsExternalReference { get; }

        /// <summary>
        /// Gets a value indicating whether this member is a supported collection type.
        /// </summary>
        public bool IsCollection { get; }

        /// <summary>
        /// Returns <c>true</c> if the member has a property validator.
        /// </summary>
        /// <remarks>The return value does not take into account whether or not the member requires
        /// type validation.</remarks>
        public bool RequiresValidation { get; }

        /// <summary>
        /// Get the value of the member
        /// </summary>
        /// <param name="instance">the instance from which the member should be accessed</param>
        /// <returns>the value of the property</returns>
        public object GetValue(object instance)
        {
            if (_getter == null)
            {
                _getter = CreateGetterDelegate(Member);
            }
            return _getter(instance);
        }

        /// <summary>
        /// Set the value of the member
        /// </summary>
        /// <param name="instance">the instance from which the member should be accessed</param>
        /// <param name="value">the value to set</param>
        /// <returns>the value of the property</returns>
        public void SetValue(object instance, object value)
        {
            if (_setter == null)
            {
                _setter = CreateSetterDelegate(Member);
            }
            _setter(instance, value);
        }

        /// <summary>
        /// Gets a cached, strongly typed getter delegate for use by internal property-value accessors.
        /// </summary>
        /// <returns>A delegate that reads this member from an entity instance.</returns>
        internal Delegate GetTypedGetter()
        {
            if (_typedGetter == null)
            {
                var helper = s_typedGetterDelegateHelper.MakeGenericMethod(Member.DeclaringType, Member.PropertyType);
                _typedGetter = (Delegate)helper.Invoke(null, new[] { Member });
            }

            return _typedGetter;
        }

        /// <summary>
        /// Gets accessor used to read this member without boxing.
        /// </summary>
        /// <returns>The accessor for this member.</returns>
        internal IValueAccessor GetValueAccessor()
        {
            return _valueAccessor ??= CreateValueAccessorFactory(this);
        }

        /// <summary>
        /// Gets accessor used to read this member as object.
        /// </summary>
        internal IValueAccessor<object> GetObjectValueAccessor()
            => new MemberValueAccessor(this);

        /// <summary>
        /// Gets a value indicating whether this member is mergable 
        /// (should be updated when loading with behaviour <see cref="LoadBehavior.MergeIntoCurrent"/> or <see cref="LoadBehavior.RefreshCurrent"/>).
        /// Supported types are mergable by default unless a <see cref="IgnoreDataMemberAttribute"/> is defined.
        /// </summary>
        public bool IsMergable { get; }

        /// <summary>
        /// <c>true</c> if the member is marked with a <see cref="CompositionAttribute"/>
        /// </summary>
        public bool IsComposition { get; }

        /// <summary>
        /// Helper method which creates a delegate which can be used to invoke a specific getter
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private static Func<object, object> CreateGetterDelegate(PropertyInfo propertyInfo)
        {
            var helper = s_getterDelegateHelper.MakeGenericMethod(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            return (Func<object, object>)helper.Invoke(null, new[] { propertyInfo });
        }

        private static Func<object, object> CreateGetterDelegateHelper<T, Tprop>(PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                // If no getter was found, fallback to method throw same type of exception
                // which exception would do, these should never propagate to the user
                return obj => { throw new ArgumentException("Internal error: No getter"); };
            }

            var getter = (Func<T, Tprop>)Delegate.CreateDelegate(typeof(Func<T, Tprop>), getMethod);
            // Add a wrapper which performs boxing of the function
            return (object instance) => (object)getter((T)instance);
        }

        private static Delegate CreateTypedGetterDelegateHelper<T, Tprop>(PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
            {
                // If no getter was found, fallback to method throw same type of exception
                // which exception would do, these should never propagate to the user
                return (Func<object, Tprop>)(obj => throw new ArgumentException("Internal error: No getter"));
            }

            var getter = (Func<T, Tprop>)Delegate.CreateDelegate(typeof(Func<T, Tprop>), getMethod);
            return (Func<object, Tprop>)(instance => getter((T)instance));
        }

        /// <summary>
        /// Helper method which creates a delegate which can be used to invoke a specific getter
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private static Action<object, object> CreateSetterDelegate(PropertyInfo propertyInfo)
        {
            var helper = s_setterDelegateHelper.MakeGenericMethod(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            return (Action<object, object>)helper.Invoke(null, new[] { propertyInfo });
        }

        private static Action<object, object> CreateSetterDelegateHelper<T, Tprop>(PropertyInfo propertyInfo)
        {
            // If no getter was found, fallback to using reflection which will throw exception
            var setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
            {
                // If no setter was found, fallback to method throw same type of exception
                // which exception would do, these should never propagate to the user
                return (obj, val) => { throw new ArgumentException("Internal error: No setter"); };
            }

            var setter = (Action<T, Tprop>)Delegate.CreateDelegate(typeof(Action<T, Tprop>), setMethod);
            // Add a wrapper which performs unboxing for the function
            return (object obj, object value) => setter((T)obj, (Tprop)value);
        }

        private static IValueAccessor CreateValueAccessorFactory(MetaMember member)
        {
            Type propertyType = member.PropertyType;
            if (propertyType == typeof(int))
            {
                return new NonNullableValueAccessor<int>((Func<object, int>)member.GetTypedGetter());
            }
            if (propertyType == typeof(int?))
            {
                return new NullableValueAccessor<int>((Func<object, int?>)member.GetTypedGetter());
            }
            if (propertyType == typeof(long))
            {
                return new NonNullableValueAccessor<long>((Func<object, long>)member.GetTypedGetter());
            }
            if (propertyType == typeof(long?))
            {
                return new NullableValueAccessor<long>((Func<object, long?>)member.GetTypedGetter());
            }
            if (propertyType == typeof(string))
            {
                return new ReferenceValueAccessor<string>((Func<object, string>)member.GetTypedGetter());
            }
            if (propertyType == typeof(Guid))
            {
                return new NonNullableValueAccessor<Guid>((Func<object, Guid>) member.GetTypedGetter());
            }
            if (propertyType == typeof(Guid?))
            {
                return new NullableValueAccessor<Guid>((Func<object, Guid?>)member.GetTypedGetter());
            }

            // Reflection based fallback for other types
            if (propertyType.IsValueType)
            {
                if (TypeUtility.IsNullableType(propertyType))
                    return (IValueAccessor)Activator.CreateInstance(typeof(NullableValueAccessor<>).MakeGenericType(TypeUtility.GetNonNullableType(propertyType))
                        , member.GetTypedGetter());
                else
                    return (IValueAccessor)Activator.CreateInstance(typeof(NonNullableValueAccessor<>).MakeGenericType(propertyType)
                        , member.GetTypedGetter());
            }
            else
                return Activator.CreateInstance(typeof(ReferenceValueAccessor<>).MakeGenericType(propertyType), member.GetTypedGetter()) as IValueAccessor;
        }

        private static bool CheckIfMergeableMember(MetaMember metaMember)
        {
            return metaMember.IsDataMember && !metaMember.IsAssociationMember;
        }


        private static bool CheckIfRoundtripMember(MetaMember metaMember, bool isRoundtripEntity)
        {
            return metaMember.IsDataMember && !metaMember.IsAssociationMember &&
                       (isRoundtripEntity || TypeUtility.IsAttributeDefined(metaMember.Member, typeof(RoundtripOriginalAttribute), false));
        }

        /// <summary>
        /// Defines an internal accessor for reading a value without boxing
        /// </summary>
        /// <remarks>
        /// Cast to <see cref="IValueAccessor{TKey}"/> where <see cref="KeyType"/> corresponds to <c>TKey</c>,
        /// to be able to retrieve a value of Type <see cref="KeyType"/> without boxing.
        /// </remarks>
        internal interface IValueAccessor
        {
            /// <summary>
            /// Gets the CLR type of the key value produced by this accessor.
            /// </summary>
            Type KeyType { get; }
        }

        /// <summary>
        /// Defines a strongly typed internal accessor for reading a single member value used as an index key.
        /// </summary>
        /// <typeparam name="TKey">The CLR type of the key value.</typeparam>
        internal interface IValueAccessor<TKey> : IValueAccessor
        {
            /// <summary>
            /// Attempts to read the member value from an entity instance as an index key.
            /// </summary>
            /// <param name="instance">The entity instance from which to read the member value.</param>
            /// <param name="value">When this method returns <c>true</c>, the non-null key value</param>
            /// <returns><c>true</c> when the member contains a non-null key value; otherwise, <c>false</c>.</returns>
            bool TryGetValue(object instance, out TKey value);
        }

        private abstract class ValueAccessor<TKey> : IValueAccessor<TKey>
        {
            public Type KeyType => typeof(TKey);

            public abstract bool TryGetValue(object instance, out TKey value);
        }

        /// <summary>
        /// Implements <see cref="IValueAccessor{TKey}"/> for struct types.
        /// </summary>
        private sealed class NonNullableValueAccessor<TKey>(Func<object, TKey> getter) : ValueAccessor<TKey>
        {
            public override bool TryGetValue(object instance, out TKey value)
            {
                value = getter(instance);
                return true;
            }
        }

        /// <summary>
        /// Implements <see cref="IValueAccessor{TKey}"/> for Nullable{TKey}
        /// </summary>
        private sealed class NullableValueAccessor<TKey>(Func<object, TKey?> getter) : ValueAccessor<TKey> where TKey : struct
        {
            public override bool TryGetValue(object instance, out TKey value)
            {
                TKey? nullableValue = getter(instance);
                if (nullableValue.HasValue)
                {
                    value = nullableValue.Value;
                    return true;
                }

                value = default;
                return false;
            }
        }

        /// <summary>
        /// Implements <see cref="IValueAccessor{TKey}"/> for reference types
        /// </summary>
        private sealed class ReferenceValueAccessor<TKey>(Func<object, TKey> getter) : ValueAccessor<TKey>
            where TKey : class
        {
            public override bool TryGetValue(object instance, out TKey value)
            {
                value = getter(instance);
                return value != null;
            }
        }

        /// <summary>
        /// Reads an association key through reflection when no typed member accessor is available.
        /// </summary>
        private sealed class MemberValueAccessor(MetaMember member) : IValueAccessor<object>
        {
            public Type KeyType => typeof(object);

            public bool TryGetValue(object instance, out object value)
            {
                object memberValue = member.GetValue(instance);
                if (memberValue == null)
                {
                    value = default;
                    return false;
                }

                value = memberValue;
                return true;
            }
        }
    }
}
