using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Class providing additional "meta" information for a Type.
    /// <remarks>
    /// Consider adding any commonly accessed or computed information about a Type
    /// to this class, to improve performance and code factoring.
    /// </remarks>
    /// </summary>
    [DebuggerDisplay("Type = {_type.Name}")]
    internal sealed class MetaType
    {
        private static ConcurrentDictionary<Type, MetaType> _metaTypes = new ConcurrentDictionary<Type, MetaType>();

        private Dictionary<PropertyDescriptor, IncludeAttribute[]> _projectionMemberMap = new Dictionary<PropertyDescriptor, IncludeAttribute[]>();
        private bool _requiresValidation;
        private PropertyDescriptorCollection _includedAssociations;
        private bool _hasComposition;
        private bool _isComplex;
        private Type _type;
        private Dictionary<string, MetaMember> _metaMembers = new Dictionary<string, MetaMember>();

        /// <summary>
        /// Returns the MetaType for the specified Type.
        /// <remarks>The MetaType should only be accessed AFTER all TypeDescriptors have
        /// been registered (i.e. after all DomainServiceDescriptions for services exposing
        /// the Type have been initialized).</remarks>
        /// </summary>
        /// <param name="type">The Type to provide the MetaType for.</param>
        /// <returns>The constructed MetaType.</returns>
        public static MetaType GetMetaType(Type type)
        {
            Debug.Assert(!TypeUtility.IsPredefinedType(type), "Should never attempt to create a MetaType for a base type.");

            return _metaTypes.GetOrAdd(type, (t) => { return new MetaType(type); });
        }

        private MetaType(Type type)
        {
            this._type = type;
            this._isComplex = TypeUtility.IsComplexType(type);

            // enumerate all properties and initialize property level info
            List<PropertyDescriptor> includedAssociations = new List<PropertyDescriptor>();
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(type))
            {
                MetaMember metaMember = new MetaMember(this, pd);

                if (pd.Attributes[typeof(ExcludeAttribute)] == null)
                {
                    bool isPredefinedType = TypeUtility.IsPredefinedType(pd.PropertyType);
                    metaMember.IsCollection = TypeUtility.IsSupportedCollectionType(pd.PropertyType);
                    bool isComplex = TypeUtility.IsSupportedComplexType(pd.PropertyType);
                    metaMember.IsDataMember = isPredefinedType || isComplex;
                    metaMember.IsComplex = isComplex;
                    metaMember.RequiresValidation = pd.Attributes.OfType<ValidationAttribute>().Any();
                }
                
                if (pd.Attributes[typeof(AssociationAttribute)] != null)
                {
                    if (pd.Attributes.OfType<IncludeAttribute>().Any(p => !p.IsProjection))
                    {
                        includedAssociations.Add(pd);
                    }
                    metaMember.IsAssociationMember = true;
                }

                IncludeAttribute[] memberProjections = pd.Attributes.OfType<IncludeAttribute>().Where(p => p.IsProjection).ToArray();
                if (memberProjections.Length > 0)
                {
                    this._projectionMemberMap[pd] = memberProjections;
                }

                if (pd.Attributes[typeof(CompositionAttribute)] != null)
                {
                    this._hasComposition = true;
                }
                this._metaMembers.Add(pd.Name, metaMember);
            }

            this._includedAssociations = new PropertyDescriptorCollection(includedAssociations.ToArray(), true);

            this.CalculateAttributesRecursive(type, new HashSet<Type>());
        }

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
        /// Gets the collection of data members for this Type.
        /// </summary>
        public IEnumerable<MetaMember> DataMembers
        {
            get
            {
                return this._metaMembers.Values.Where(m => m.IsDataMember);
            }
        }

        /// <summary>
        /// Gets the underlying CLR type for this MetaType
        /// </summary>
        public Type Type
        {
            get
            {
                return this._type;
            }
        }

        /// <summary>
        /// Gets the collection of association members that have an IncludeAttribute applied
        /// to them.
        /// </summary>
        public PropertyDescriptorCollection IncludedAssociations
        {
            get
            {
                return this._includedAssociations;
            }
        }

        /// <summary>
        /// Gets a map of all projection includes for each property.
        /// </summary>
        public IDictionary<PropertyDescriptor, IncludeAttribute[]> ProjectionMemberMap
        {
            get
            {
                return this._projectionMemberMap;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a ComplexType.
        /// </summary>
        public bool IsComplex
        {
            get
            {
                return this._isComplex;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Type has any Type or member level
        /// validation attributes applied. The check is recursive through any complex
        /// type members.
        /// </summary>
        public bool RequiresValidation
        {
            get
            {
                return this._requiresValidation;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Type has any members marked with
        /// CompositionAttribute.
        /// </summary>
        public bool HasComposition
        {
            get
            {
                return this._hasComposition;
            }
        }

        /// <summary>
        /// Gets the Type level validation errors for the underlying Type.
        /// </summary>
        public IEnumerable<ValidationAttribute> ValidationAttributes
        {
            get
            {
                return TypeDescriptor.GetAttributes(this._type).OfType<ValidationAttribute>();
            }
        }

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
                this._requiresValidation = TypeDescriptor.GetAttributes(this._type)[typeof(ValidationAttribute)] != null;
            }

            // visit all data members
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(type))
            {
                if (!this._requiresValidation)
                {
                    this._requiresValidation = property.Attributes.OfType<ValidationAttribute>().Any();
                }

                // for complex members we must drill in recursively
                if (TypeUtility.IsSupportedComplexType(property.PropertyType))
                {
                    Type elementType = TypeUtility.GetElementType(property.PropertyType);
                    this.CalculateAttributesRecursive(elementType, visited);
                }
            }
        }
    }

    /// <summary>
    /// This class caches all the interesting attributes of an property.
    /// </summary>
    [DebuggerDisplay("Name = {Member.Name}")]
    internal sealed class MetaMember
    {
        public MetaMember(MetaType metaType, PropertyDescriptor property)
        {
            this.Member = property;
            this.MetaType = metaType;
        }

        public MetaType MetaType
        {
            get;
            private set;
        }

        public PropertyDescriptor Member
        {
            get;
            private set;
        }

        public bool IsAssociationMember
        {
            get;
            set;
        }

        public bool IsDataMember
        {
            get;
            set;
        }

        public bool IsComplex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this member is a supported collection type.
        /// </summary>
        public bool IsCollection
        {
            get;
            set;
        }

        /// <summary>
        /// Returns <c>true</c> if the member has a property validator.
        /// </summary>
        /// <remarks>The return value does not take into account whether or not the member requires
        /// type validation.</remarks>
        public bool RequiresValidation
        {
            get;
            set;
        }

        public object GetValue(object instance)
        {
            // TODO : In the future as a performance optimization we should emit a delegate
            // to invoke the getter, rather than use reflection.
            return this.Member.GetValue(instance);
        }
    }
}
