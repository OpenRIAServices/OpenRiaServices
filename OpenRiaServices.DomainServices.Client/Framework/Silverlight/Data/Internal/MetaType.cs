using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices.DomainServices.Server.Data;

namespace OpenRiaServices.DomainServices.Client.Internal
{
    /// <summary>
    /// INTERNAL - Public API surface might change from version to version.
    /// 
    /// Class providing additional "meta" information for a Type.
    /// <remarks>
    /// Consider adding any commonly accessed or computed information about a Type
    /// to this class, to improve performance and code factoring.
    /// MetaType must be fully readonly before GetMetaType returns. While it is cached per thread,
    /// it may be cached on types elsewhere providing multithreaded access to visible properties.
    /// </remarks>
    /// </summary>
    [DebuggerDisplay("Type = {_type.Name}")]
    public sealed class MetaType
    {
        /// <summary>
        /// We're using TLS for performance to avoid taking locks. In multithreaded
        /// scenarios this means that there may be multiple MetaType caches around
        /// but that shouldn't be a problem.
        /// </summary>
        [ThreadStatic]
        private static Dictionary<Type, MetaType> _metaTypes;
        private bool _requiresValidation;
        private readonly Type[] _childTypes;
        private readonly Dictionary<string, MetaMember> _metaMembers = new Dictionary<string, MetaMember>();
        private readonly ReadOnlyCollection<MetaMember> _dataMembers;
        private readonly ReadOnlyCollection<ValidationAttribute> _validationAttributes;
        private readonly Dictionary<string, EntityActionAttribute> _customUpdateMethods;

        /// <summary>
        /// Returns the MetaType for the specified Type.
        /// </summary>
        /// <param name="type">The Type to provide the MetaType for.</param>
        /// <returns>The constructed MetaType.</returns>
        public static MetaType GetMetaType(Type type)
        {
            Debug.Assert(!TypeUtility.IsPredefinedType(type), "Should never attempt to create a MetaType for a base type.");

            MetaType metaType = null;
            if (!MetaTypes.TryGetValue(type, out metaType))
            {
                metaType = new MetaType(type);
                MetaTypes[type] = metaType;
            }
            return metaType;
        }

        private MetaType(Type type)
        {
            _customUpdateMethods = (from method in type.GetMethods(MemberBindingFlags)
                                    let attributes = method.GetCustomAttributes(typeof(EntityActionAttribute), false)
                                    let attribute = (EntityActionAttribute)attributes.FirstOrDefault()
                                    where attribute != null
                                    select attribute
                                   ).ToDictionary(cua => cua.Name, cua => cua);

            this.IsComplex = TypeUtility.IsComplexType(type);

            bool hasOtherRoundtripMembers = false;

            IEnumerable<PropertyInfo> properties = type.GetProperties(MemberBindingFlags)
                .Where(p => p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name);
            foreach (PropertyInfo property in properties)
            {
                MetaMember metaMember = new MetaMember(this, property);

                if (metaMember.IsComplex)
                    this.HasComplexMembers = true;

                if (metaMember.IsComposition)
                    this.HasComposition = true;

                if (metaMember.IsRoundtripMember)
                {
                    if (TypeUtility.IsAttributeDefined(property, typeof(TimestampAttribute), false) &&
                        TypeUtility.IsAttributeDefined(property, typeof(ConcurrencyCheckAttribute), false))
                    {
                        // Look for a concurrency version member. There should be only one
                        // (DomainService validation ensures this).
                        this.VersionMember = metaMember;
                    }
                    else if (!metaMember.IsKeyMember)
                    {
                        // Look for non-key, non-timestamp roundtripped members. We can skip key members,
                        // since they're read only and cannot be modified by the client anyways.
                        hasOtherRoundtripMembers = true;
                    }
                }

                this._metaMembers.Add(property.Name, metaMember);
            }


            this.ShouldRoundtripOriginal = (hasOtherRoundtripMembers || VersionMember == null);
            if (this.HasComposition)
            {
                this._childTypes = _metaMembers.Values
                        .Where(m => m.IsComposition)
                        .Select(p => TypeUtility.GetElementType(p.PropertyType)).ToArray();
            }
            else
            {
                this._childTypes = TypeUtility.EmptyTypes;
            }

            this.Type = type;

            _validationAttributes = new ReadOnlyCollection<ValidationAttribute>(this.Type.GetCustomAttributes(typeof(ValidationAttribute), true).OfType<ValidationAttribute>().ToArray());
            _requiresValidation = _validationAttributes.Any();

            this.CalculateAttributesRecursive(type, new HashSet<Type>());

            // for identity purposes, we need to make sure values are always ordered
            KeyMembers = new ReadOnlyCollection<MetaMember>(_metaMembers.Values.Where(m => m.IsKeyMember).OrderBy(m => m.Name).ToArray());
            _dataMembers = new ReadOnlyCollection<MetaMember>(_metaMembers.Values.Where(m => m.IsDataMember).ToArray());
        }

        /// <summary>
        /// Gets the correct property binding flags to use for data members.
        /// </summary>
        private static BindingFlags MemberBindingFlags => BindingFlags.Instance | BindingFlags.Public;

        /// <summary>
        /// Get the <see cref="MetaMember"/> for the property with name <paramref name="memberName"/>
        /// or <c>null</c> if no member with the name exist.
        /// </summary>
        public MetaMember this[string memberName]
        {
            get
            {
                MetaMember mm = null;
                if (this._metaMembers.TryGetValue(memberName, out mm))
                {
                    return mm;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="EntityActionAttribute"/> for the custom update method with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>the EntityActionAttribute for the custom method; or <c>null</c> if no method was found</returns>
        internal EntityActionAttribute GetEntityAction(string name)
        {
            EntityActionAttribute res;
            _customUpdateMethods.TryGetValue(name, out res);
            return res;
        }

        /// <summary>
        /// Gets <see cref="EntityActionAttribute" /> for all custom update method on the MetaType.
        /// </summary>
        /// <returns>Meta information about all entity actions on the type</returns>
        internal IEnumerable<EntityActionAttribute> GetEntityActions()
        {
            return _customUpdateMethods.Values;
        }

        private static Dictionary<Type, MetaType> MetaTypes
        {
            get
            {
                if (_metaTypes == null)
                {
                    _metaTypes = new Dictionary<Type, MetaType>();
                }
                return _metaTypes;
            }
        }

        /// <summary>
        /// Gets the underlying CLR type for this MetaType
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets a value indicating whether this is a complex type.
        /// </summary>
        public bool IsComplex { get; }

        /// <summary>
        /// Gets a value indicating whether this type has complex members. This does include
        /// complex object collections.
        /// </summary>
        public bool HasComplexMembers { get; }

        /// <summary>
        /// Gets a value indicating whether the original should be roundtripped for this Type.
        /// </summary>
        public bool ShouldRoundtripOriginal { get; }

        /// <summary>
        /// Gets the concurrency version member for this type. May be null.
        /// </summary>
        public MetaMember VersionMember { get; }

        /// <summary>
        /// Gets the collection of members for this Type.
        /// </summary>
        public IEnumerable<MetaMember> Members
        {
            get
            {
                return this._metaMembers.Values;
            }
        }

        /// <summary>
        /// Gets the collection of key members for this entity Type.
        /// The entries are sorted by Name for identity purposes.
        /// </summary>
        public ReadOnlyCollection<MetaMember> KeyMembers { get; }

        /// <summary>
        /// Gets the collection of data members for this Type.
        /// </summary>
        public IEnumerable<MetaMember> DataMembers
        {
            get
            {
                return _dataMembers;
            }
        }

        /// <summary>
        /// Gets the collection of members that should be roundtripped in
        /// the original entity.
        /// </summary>
        public IEnumerable<PropertyInfo> RoundtripMembers
        {
            get
            {
                return this._dataMembers.Where(m => m.IsRoundtripMember).Select(m => m.Member);
            }
        }

        /// <summary>
        /// Gets the collection of association members for this entity Type.
        /// </summary>
        public IEnumerable<MetaMember> AssociationMembers
        {
            get
            {
                return this._metaMembers.Values.Where(m => m.IsAssociationMember);
            }
        }

        internal IEnumerable<MetaMember> MergableMembers
        {
            get
            {
                return this._metaMembers.Values.Where(m => m.IsMergable);
            }
        }

        /// <summary>
        /// Gets the collection of child types this entity Type composes.
        /// </summary>
        public IEnumerable<Type> ChildTypes => this._childTypes;

        /// <summary>
        /// Gets a value indicating whether the Type has any Type or member level
        /// validation attributes applied. The check is recursive through any complex
        /// type members.
        /// </summary>
        public bool RequiresValidation => this._requiresValidation;

        /// <summary>
        /// Gets a value indicating whether the Type has any members marked with
        /// CompositionAttribute.
        /// </summary>
        public bool HasComposition { get; }

        /// <summary>
        /// Gets the Type level validation errors for the underlying Type.
        /// </summary>
        public IEnumerable<ValidationAttribute> ValidationAttributes => _validationAttributes;

        /// <summary>
        /// This recursive function visits every property in the type tree. For each property,
        /// we inspect the attributes and set meta attributes as needed.
        /// </summary>
        /// <param name="type">The root type to calculate attributes for.</param>
        /// <param name="visited">Visited set for recursion.</param>
        private void CalculateAttributesRecursive(Type type, HashSet<Type> visited)
        {
            if (!visited.Add(type))
            {
                return;
            }

            if (!this._requiresValidation)
            {
                this._requiresValidation = TypeUtility.IsAttributeDefined(type, typeof(ValidationAttribute), true);
            }

            // visit all data members
            IEnumerable<PropertyInfo> properties = type.GetProperties(MemberBindingFlags).Where(p => p.GetIndexParameters().Length == 0).OrderBy(p => p.Name);
            foreach (PropertyInfo property in properties)
            {
                if (!this._requiresValidation)
                {
                    this._requiresValidation = TypeUtility.IsAttributeDefined(property, typeof(ValidationAttribute), true);
                }

                // for complex members we must drill in recursively
                if (TypeUtility.IsSupportedComplexType(property.PropertyType))
                {
                    Type elementType = TypeUtility.GetElementType(property.PropertyType);
                    this.CalculateAttributesRecursive(elementType, visited);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether entity actions for code using code gen before version 4.4.0
        /// has been discovered.
        /// </summary>
        internal bool IsLegacyEntityActionsDiscovered { get; set; }

        /// <summary>
        /// Add's a EntityAction with the given name and property names.
        /// This method is only used when discovering EntityActions for code which has used the code gen 
        /// before version 4.4.0.
        /// </summary>
        /// <param name="name">The name of the entity action.</param>
        /// <param name="canInvokePropertyName">Name of the can invoke property.</param>
        /// <param name="isInvokedPropertyName">Name of the is invoked property.</param>
        internal void TryAddLegacyEntityAction(string name, string canInvokePropertyName, string isInvokedPropertyName)
        {
            if (!_customUpdateMethods.ContainsKey(name))
            {
                _customUpdateMethods.Add(name, new EntityActionAttribute(name)
                {
                    CanInvokePropertyName = canInvokePropertyName,
                    IsInvokedPropertyName = isInvokedPropertyName,
                });
            }
        }
    }
}
