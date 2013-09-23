using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Custom TypeDescriptor for domain Types exposed by a <see cref="DomainService"/>.
    /// </summary>
    internal class DomainTypeDescriptor : CustomTypeDescriptor
    {
        private static ConcurrentDictionary<Type, Dictionary<PropertyDescriptor, IncludeAttribute[]>> _includeMaps = new ConcurrentDictionary<Type, Dictionary<PropertyDescriptor, IncludeAttribute[]>>();
        private PropertyDescriptorCollection _properties;
        private Type _entityType;
        private HashSet<string> _foreignKeyMembers;
        private bool _keyIsEditable;

        public DomainTypeDescriptor(Type entityType, ICustomTypeDescriptor parent, bool keyIsEditable, HashSet<string> foreignKeyMembers)
            : base(parent)
        {
            this._entityType = entityType;
            this._keyIsEditable = keyIsEditable;
            this._foreignKeyMembers = foreignKeyMembers;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            if (this._properties == null)
            {
                // Get properties from our parent
                PropertyDescriptorCollection originalCollection = base.GetProperties();

                // Set this._properties to avoid a stack overflow when CreateProjectionProperties 
                // ends up recursively calling TypeDescriptor.GetProperties on a type.
                this._properties = originalCollection;

                bool customDescriptorsCreated = false;
                List<PropertyDescriptor> tempPropertyDescriptors = new List<PropertyDescriptor>();

                // for every property exposed by our parent, see if we have additional metadata to add,
                // and if we do we need to add a wrapper PropertyDescriptor to add the new attributes
                foreach (PropertyDescriptor propDescriptor in this._properties)
                {
                    Attribute[] newMetadata = this.GetAdditionalAttributes(propDescriptor);
                    if (newMetadata.Length > 0)
                    {
                        tempPropertyDescriptors.Add(new DomainPropertyDescriptor(propDescriptor, newMetadata));
                        customDescriptorsCreated = true;
                    }
                    else
                    {
                        tempPropertyDescriptors.Add(propDescriptor);
                    }
                }

                // Add virtual properties for any member projections
                ICollection<PropertyDescriptor> additionalProperties = this.CreateProjectionProperties(tempPropertyDescriptors);

                if (additionalProperties.Count > 0)
                {
                    // We receive a readonly copy of the property descriptor collection -- make a readwrite one
                    foreach (PropertyDescriptor pd in additionalProperties)
                    {
                        tempPropertyDescriptors.Add(pd);
                    }

                    customDescriptorsCreated = true;
                }

                if (customDescriptorsCreated)
                {
                    this._properties = new PropertyDescriptorCollection(tempPropertyDescriptors.ToArray(), true);
                }
            }

            return this._properties;
        }

        /// <summary>
        /// Return an array of new attributes for the specified PropertyDescriptor. If no
        /// attributes need to be added, return an empty array.
        /// </summary>
        /// <param name="pd">The property to add attributes for.</param>
        /// <returns>The collection of new attributes.</returns>
        private Attribute[] GetAdditionalAttributes(PropertyDescriptor pd)
        {
            List<Attribute> additionalAttributes = new List<Attribute>();

            if (ShouldAddRoundTripAttribute(pd, this._foreignKeyMembers.Contains(pd.Name)))
            {
                additionalAttributes.Add(new RoundtripOriginalAttribute());
            }

            bool allowInitialValue;
            if (ShouldAddEditableFalseAttribute(pd, this._keyIsEditable, out allowInitialValue))
            {
                additionalAttributes.Add(new EditableAttribute(false) { AllowInitialValue = allowInitialValue });
            }

            return additionalAttributes.ToArray();
        }

        private ICollection<PropertyDescriptor> CreateProjectionProperties(IEnumerable<PropertyDescriptor> existingProperties)
        {
            // Get the map of projection includes for each property
            List<PropertyDescriptor> projectionProperties = new List<PropertyDescriptor>();
            Dictionary<PropertyDescriptor, IncludeAttribute[]> includeMap = DomainTypeDescriptor.GetProjectionIncludeMap(this._entityType, existingProperties);
            if (includeMap.Count == 0)
            {
                return projectionProperties;
            }

            // for each existing property add projection properties for any member projections
            foreach (PropertyDescriptor pd in existingProperties)
            {
                IncludeAttribute[] memberProjections;
                includeMap.TryGetValue(pd, out memberProjections);
                if (memberProjections == null)
                {
                    // no member projections for this property
                    continue;
                }

                foreach (IncludeAttribute memberProjection in memberProjections)
                {
                    // If the projection member already exists as a property on the source type
                    // skip, otherwise we'll generate a duplicate
                    if (existingProperties.SingleOrDefault(p => p.Name == memberProjection.MemberName) != null)
                    {
                        continue;
                    }

                    // Drill through the projection path to get the target property
                    PropertyDescriptor targetProperty = pd;
                    foreach (string pathMember in memberProjection.Path.Split('.'))
                    {
                        if (targetProperty.PropertyType == this._entityType)
                        {
                            // avoid recursion
                            targetProperty = existingProperties.SingleOrDefault(p => p.Name == pathMember);
                        }
                        else
                        {
                            targetProperty = TypeDescriptor.GetProperties(targetProperty.PropertyType)[pathMember];
                        }

                        if (targetProperty == null)
                        {
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidMemberProjection_Path, memberProjection.Path, pd.PropertyType, pd.Name));
                        }
                    }

                    // verify the projected type is a supported type
                    if (!TypeUtility.IsPredefinedType(targetProperty.PropertyType))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidMemberProjection_InvalidProjectedType, targetProperty.ComponentType, targetProperty.Name, pd.PropertyType, pd.Name));
                    }

                    // add the virtual projection property
                    projectionProperties.Add(new MemberProjectionPropertyDescriptor(pd, targetProperty, memberProjection));
                }
            }

            return projectionProperties;
        }

        private static Dictionary<PropertyDescriptor, IncludeAttribute[]> GetProjectionIncludeMap(Type entityType, IEnumerable<PropertyDescriptor> properties)
        {
            // First check cache and return if we've already computed the map
            return _includeMaps.GetOrAdd(entityType, type =>
            {
                Dictionary<PropertyDescriptor, IncludeAttribute[]> includeMap = new Dictionary<PropertyDescriptor, IncludeAttribute[]>();
                foreach (PropertyDescriptor pd in properties)
                {
                    IncludeAttribute[] includes = pd.Attributes.OfType<IncludeAttribute>().Where(p => p.IsProjection).ToArray();
                    if (includes.Length > 0)
                    {
                        includeMap[pd] = includes;
                    }
                }
                return includeMap;
            });
        }

        /// <summary>
        /// Determines whether a type uses any features requiring the
        /// <see cref="DomainTypeDescriptionProvider"/> to be registered. We do this
        /// check as an optimization so we're not adding additional TDPs to the
        /// chain when they're not necessary.
        /// </summary>
        /// <param name="descriptor">The descriptor for the type to check.</param>
        /// <param name="keyIsEditable">Indicates whether the key for this Type is editable.</param>
        /// <param name="foreignKeyMembers">The set of foreign key members for the Type.</param>
        /// <returns>Returns <c>true</c> if the type uses any features requiring the
        /// <see cref="DomainTypeDescriptionProvider"/> to be registered.</returns>
        internal static bool ShouldRegister(ICustomTypeDescriptor descriptor, bool keyIsEditable, HashSet<string> foreignKeyMembers)
        {
            foreach (PropertyDescriptor pd in descriptor.GetProperties())
            {
                // If the Type has any member projections, we'll need to register so we can
                // add virtual projection PDs
                if (pd.Attributes.OfType<IncludeAttribute>().Any(a => a.IsProjection))
                {
                    return true;
                }

                // If there are any attributes that should be inferred for this member, then
                // we will register the descriptor
                if (ShouldInferAttributes(pd, keyIsEditable, foreignKeyMembers))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the specified member requires a RoundTripOriginalAttribute
        /// and one isn't already present.
        /// </summary>
        /// <param name="pd">The member to check.</param>
        /// <param name="isFkMember">True if the member is a foreign key, false otherwise.</param>
        /// <returns>True if RoundTripOriginalAttribute should be added, false otherwise.</returns>
        private static bool ShouldAddRoundTripAttribute(PropertyDescriptor pd, bool isFkMember)
        {
            if (pd.Attributes[typeof(RoundtripOriginalAttribute)] != null || pd.Attributes[typeof(AssociationAttribute)] != null)
            {
                // already has the attribute or is an association 
                return false;
            }

            if (isFkMember || pd.Attributes[typeof(ConcurrencyCheckAttribute)] != null ||
                pd.Attributes[typeof(TimestampAttribute)] != null || pd.Attributes[typeof(KeyAttribute)] != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if the specified member requires an <see cref="EditableAttribute"/>
        /// to make the member read-only and one isn't already present.
        /// </summary>
        /// <param name="pd">The member to check.</param>
        /// <param name="keyIsEditable">Indicates whether the key for this Type is editable.</param>
        /// <param name="allowInitialValue">
        /// The default that should be used for <see cref="EditableAttribute.AllowInitialValue"/> if the attribute
        /// should be added to the member.
        /// </param>
        /// <returns><c>true</c> if <see cref="EditableAttribute"/> should be added, <c>false</c> otherwise.</returns>
        private static bool ShouldAddEditableFalseAttribute(PropertyDescriptor pd, bool keyIsEditable, out bool allowInitialValue)
        {
            allowInitialValue = false;

            if (pd.Attributes[typeof(EditableAttribute)] != null)
            {
                // already has the attribute
                return false;
            }

            bool hasKeyAttribute = (pd.Attributes[typeof(KeyAttribute)] != null);
            if (hasKeyAttribute && keyIsEditable)
            {
                return false;
            }

            if (hasKeyAttribute || pd.Attributes[typeof(TimestampAttribute)] != null)
            {
                // If we're inferring EditableAttribute because of a KeyAttribute
                // we want to allow initial value for the member.
                allowInitialValue = hasKeyAttribute;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if there are any attributes that can be inferred for the specified member.
        /// </summary>
        /// <param name="pd">The member to check.</param>
        /// <param name="keyIsEditable">Indicates whether the key for this Type is editable.</param>
        /// <param name="foreignKeyMembers">Collection of foreign key members for the Type.</param>
        /// <returns><c>true</c> if there are attributes to be inferred, <c>false</c> otherwise.</returns>
        private static bool ShouldInferAttributes(PropertyDescriptor pd, bool keyIsEditable, IEnumerable<string> foreignKeyMembers)
        {
            bool allowInitialValue;

            return ShouldAddEditableFalseAttribute(pd, keyIsEditable, out allowInitialValue) ||
                   ShouldAddRoundTripAttribute(pd, foreignKeyMembers.Contains(pd.Name));
        }
    }
}
