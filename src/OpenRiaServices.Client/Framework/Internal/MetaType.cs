using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.Client.Internal
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
    [DebuggerDisplay("Type = {Type.Name}")]
    public sealed class MetaType
    {
        private static readonly ConcurrentDictionary<Type, MetaType> s_metaTypes = new();
        private readonly bool _requiresValidation;
        private readonly bool _requiresObjectValidation;
        private readonly Type[] _childTypes;
        private readonly Dictionary<string, MetaMember> _metaMembers = new Dictionary<string, MetaMember>();
        private readonly ReadOnlyCollection<MetaMember> _dataMembers;
        private readonly ReadOnlyCollection<ValidationAttribute> _validationAttributes;
        private readonly Dictionary<string, EntityActionAttribute> _customUpdateMethods;
        private readonly MetaMember[] _associationMembers;

        /// <summary>
        /// Returns the MetaType for the specified Type.
        /// </summary>
        /// <param name="type">The Type to provide the MetaType for.</param>
        /// <returns>The constructed MetaType.</returns>
        public static MetaType GetMetaType(Type type)
        {
            Debug.Assert(!TypeUtility.IsPredefinedType(type), "Should never attempt to create a MetaType for a base type.");

            return s_metaTypes.GetOrAdd(type, static key => new MetaType(key));
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

            bool isRoundtripEntity = TypeUtility.IsAttributeDefined(type, typeof(RoundtripOriginalAttribute), true);
            bool hasOtherRoundtripMembers = false;

            IEnumerable<PropertyInfo> properties = type.GetProperties(MemberBindingFlags)
                .Where(p => p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name);
            foreach (PropertyInfo property in properties)
            {
                MetaMember metaMember = new MetaMember(this, property, isRoundtripEntity);

                if (metaMember.IsComplex)
                    this.HasComplexMembers = true;

                if (metaMember.IsComposition)
                    this.HasComposition = true;

                if (metaMember.RequiresValidation)
                    this._requiresValidation = true;

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
            _requiresObjectValidation = _validationAttributes.Any() || typeof(IValidatableObject).IsAssignableFrom(type);
            _requiresValidation = _requiresValidation || _requiresObjectValidation;

            // for identity purposes, we need to make sure values are always ordered
            KeyMembers = new ReadOnlyCollection<MetaMember>(_metaMembers.Values.Where(m => m.IsKeyMember).OrderBy(m => m.Name).ToArray());
            _dataMembers = new ReadOnlyCollection<MetaMember>(_metaMembers.Values.Where(m => m.IsDataMember).ToArray());
            _associationMembers = _metaMembers.Values.Where(m => m.IsAssociationMember).ToArray();

            if (!_requiresValidation && HasComplexMembers)
            {
                // Reqursivly search properties on all complex members for validation attribues and IValidatableObject to 
                // determine if validation is required
                var visitedTypes = new HashSet<Type>() { type };
                foreach (var member in Members)
                {
                    if (member.IsComplex && SearchForValidationAttributesRecursive(member.PropertyType, visitedTypes))
                    {
                        _requiresValidation = true;
                        break;
                    }
                }
            }
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
                if (this._metaMembers.TryGetValue(memberName, out MetaMember mm))
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
        public EntityActionAttribute GetEntityAction(string name)
        {
            EntityActionAttribute res;
            _customUpdateMethods.TryGetValue(name, out res);
            return res;
        }

        /// <summary>
        /// Gets <see cref="EntityActionAttribute" /> for all custom update method on the MetaType.
        /// </summary>
        /// <returns>Meta information about all entity actions on the type</returns>
        public IEnumerable<EntityActionAttribute> GetEntityActions()
        {
            return _customUpdateMethods.Values;
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
        /// Gets the collection of all members for this Type.
        /// </summary>
        public IEnumerable<MetaMember> Members => this._metaMembers.Values;

        /// <summary>
        /// Gets the collection of key members for this entity Type.
        /// The entries are sorted by Name for identity purposes.
        /// </summary>
        public ReadOnlyCollection<MetaMember> KeyMembers { get; }

        /// <summary>
        /// Gets the collection of data members for this Type.
        /// </summary>
        public IEnumerable<MetaMember> DataMembers => _dataMembers;

        /// <summary>
        /// Gets the collection of association members for this entity Type.
        /// </summary>
        public IEnumerable<MetaMember> AssociationMembers => _associationMembers;

        /// <summary>
        /// Gets the collection of child types this entity Type composes.
        /// </summary>
        public IEnumerable<Type> ChildTypes => this._childTypes;

        /// <summary>
        /// Gets a value indicating whether the Type requires any Type or member level
        /// validation. The check is recursive through any complex type members.
        /// </summary>
        public bool RequiresValidation => this._requiresValidation;

        /// <summary>
        /// Gets a value indicating whether the Type requires any Type level
        /// validation.
        /// </summary>
        internal bool RequiresObjectValidation => this._requiresObjectValidation;

        /// <summary>
        /// Gets a value indicating whether the Type has any members marked with
        /// CompositionAttribute.
        /// </summary>
        public bool HasComposition { get; }

        /// <summary>
        /// Gets the Type level validation attributes for the underlying Type.
        /// </summary>
        public IEnumerable<ValidationAttribute> ValidationAttributes => _validationAttributes;

        /// <summary>
        /// This recursive function visits every property in the type tree. For each property,
        /// we inspect the type and attributes if validation is required.
        /// </summary>
        /// <param name="type">The root type to calculate attributes for.</param>
        /// <param name="visited">Visited set for recursion.</param>
        /// <returns><c>true</c> if any nested type or property requires validation</returns>
        private static bool SearchForValidationAttributesRecursive(Type type, HashSet<Type> visited)
        {
            // For collections and similar, get the type in the collection
            type = TypeUtility.GetElementType(type);

            // If already visited the type then we don't need to visit it again
            if (!visited.Add(type))
            {
                return false;
            }

            // Check metatype cache to se if we have already
            // determined if the type requires validation
            MetaType metaType;
            if (s_metaTypes.TryGetValue(type, out metaType))
            {
                return metaType.RequiresValidation;
            }

            // Check for type level validation
            if (TypeUtility.IsAttributeDefined(type, typeof(ValidationAttribute), true))
                return true;

            if (typeof(IValidatableObject).IsAssignableFrom(type))
                return true;

            // visit all data members
            IEnumerable<PropertyInfo> properties = type.GetProperties(MemberBindingFlags).Where(p => p.GetIndexParameters().Length == 0).OrderBy(p => p.Name);
            foreach (PropertyInfo property in properties)
            {
                if (TypeUtility.IsAttributeDefined(property, typeof(ValidationAttribute), true))
                    return true;

                // for complex members we must drill in recursively
                if (TypeUtility.IsSupportedComplexType(property.PropertyType))
                {
                    if (SearchForValidationAttributesRecursive(property.PropertyType, visited))
                        return true;
                }
            }

            return false;
        }
    }
}
