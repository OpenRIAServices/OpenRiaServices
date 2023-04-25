using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Standard custom attribute builder.
    /// </summary>
    /// <remarks>This custom attribute builder class generates a CodeDom custom attribute declaration
    /// for most well-formed custom attribute types.  To be well formed, an attribute must either expose
    /// public setters for every public property it exposes, or it must provide a constructor whose
    /// parameter names match properties for which there is no public setter.
    /// </remarks>
    internal class StandardCustomAttributeBuilder : ICustomAttributeBuilder
    {
        /// <summary>
        /// Returns a representative <see cref="AttributeDeclaration"/> for a given <see cref="Attribute"/> instance.
        /// </summary>
        /// <param name="attribute">An attribute instance to create a <see cref="AttributeDeclaration"/> for.</param>
        /// <returns>A <see cref="AttributeDeclaration"/> representing the <paramref name="attribute"/>.</returns>
        public virtual AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            Type attributeType = attribute.GetType();
            AttributeDeclaration attributeDeclaration = new AttributeDeclaration(attributeType);

            // Strategy is as follows:
            //  - Fetch all the public property values from the current attribute
            //  - Determine the default value for all of these properties
            //  - From these 2 lists, determine the set of "non-default" properties.  These are what we must code gen.
            //  - From this list, determine which of these can be set only through a ctor
            //  - From the list of ctor properties and values, find the best ctor pattern for it and code gen that
            //  - For all remaining non-default properties, code gen named argument setters
            List<PropertyMap> propertyMaps = this.BuildPropertyMaps(attribute);
            Dictionary<string, object> currentValues = GetPropertyValues(propertyMaps, attribute);
            Dictionary<string, object> defaultValues = GetDefaultPropertyValues(propertyMaps, attribute, currentValues);
            List<PropertyMap> nonDefaultProperties = GetNonDefaultProperties(propertyMaps, currentValues, defaultValues);
            List<PropertyMap> unsettableProperties = GetUnsettableProperties(nonDefaultProperties);

            // "Unsettable" properties are all those that can be set only through a ctor (they have no public setter).
            // Go find the best ctor pattern for them and code gen that much
            ParameterInfo[] ctorParameters = FindBestConstructor(unsettableProperties, currentValues, attributeType);
            if (ctorParameters == null)
            {
                // Return null, indicating we cannot build this attribute.
                return null;
            }

            // We found a ctor that will accept all our properties that need to be set.
            // Generate ctor arguments to match this signature.
            // Note: the ctor pattern obviously may require other arguments that are also settable,
            // so if we pass a value to the ctor, we omit it from the set of named parameters below
            foreach (ParameterInfo parameter in ctorParameters)
            {
                PropertyMap matchedPropertyMap = null;
                foreach (PropertyMap map in propertyMaps)
                {
                    PropertyInfo propertyInfo = map.Setter;
                    if (propertyInfo.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase) && CanValueBeAssignedToType(parameter.ParameterType, currentValues[propertyInfo.Name]))
                    {
                        matchedPropertyMap = map;
                        break;
                    }
                }
                object value = matchedPropertyMap != null ? currentValues[matchedPropertyMap.Getter.Name] : DefaultInstanceForType(parameter.ParameterType);
                attributeDeclaration.ConstructorArguments.Add(value);

                // Remove this from our list of properties we need to set so the code below skips it
                if (matchedPropertyMap != null)
                {
                    nonDefaultProperties.Remove(matchedPropertyMap);
                }
            }

            // For all remaining non-default properties, generate a named argument setter.
            // We sort these so the named parameters appear in a predictable order in generated code -- primarily for unit testing
            nonDefaultProperties.Sort(new Comparison<PropertyMap>((x, y) => string.Compare(x.Setter.Name, y.Setter.Name, StringComparison.Ordinal)));

            foreach (PropertyMap map in nonDefaultProperties)
            {
                attributeDeclaration.NamedParameters.Add(map.Setter.Name, currentValues[map.Getter.Name]);
            }

            return attributeDeclaration;
        }

        /// <summary>
        /// Given the name of a property from which we fetched a property, returns the
        /// name of the property we should use to set the value.  A null return means
        /// this property should be ignored by code gen.
        /// </summary>
        /// <param name="propertyInfo">The getter property to consider.</param>
        /// <param name="attribute">The current attribute instance we are considering.</param>
        /// <returns>The name of the property we should use as the setter or null to suppress codegen.</returns>
        protected virtual string MapProperty(PropertyInfo propertyInfo, Attribute attribute)
        {
            string propertyName = propertyInfo.Name;

            // We strip "TypeId" from all Attributes.  It is a property we cannot set via setters or ctor
            if (propertyName.Equals("TypeId", StringComparison.Ordinal))
                return null;
#if NET6_0_OR_GREATER
            // We strip "TypeId" from all CustomValidationAttribute for NET 6.0 since we cannot set it, it is calculated indirectly
            else if (attribute is CustomValidationAttribute && propertyInfo.Name == nameof(CustomValidationAttribute.RequiresValidationContext))
                return null;
#endif
            return propertyName;
        }

        /// <summary>
        /// Returns the default object instance for the given type, effectively default(T)
        /// </summary>
        /// <param name="t">The type whose default is needed</param>
        /// <returns>The default value for that type.</returns>
        private static object DefaultInstanceForType(Type t)
        {
            // All value types have default ctors.  This should always work
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        /// <summary>
        /// Determines whether the given value may legally be assigned to the specified type
        /// </summary>
        /// <param name="type">The type to which it will be assigned</param>
        /// <param name="value">The value to test</param>
        /// <returns><c>true</c> if the value can legally be assigned</returns>
        private static bool CanValueBeAssignedToType(Type type, object value)
        {
            // Null is allowed on reference types or nullables
            if (value == null)
            {
                return !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
            }

            // Non-null -- simply ask type
            Type objectType = value.GetType();
            bool result = type.IsAssignableFrom(objectType);
            return result;
        }

        /// <summary>
        /// Retrieves the list of properties that have no public setter
        /// </summary>
        /// <param name="propertyMaps">A list of properties to consider</param>
        /// <returns>The subset of the input list containing all properties lacking a public setter</returns>
        private static List<PropertyMap> GetUnsettableProperties(IEnumerable<PropertyMap> propertyMaps)
        {
            List<PropertyMap> result = new List<PropertyMap>();
            foreach (PropertyMap map in propertyMaps)
            {
                PropertyInfo propertyInfo = map.Setter;
                MethodInfo setter = propertyInfo.GetSetMethod();
                if (setter == null || !setter.IsPublic)
                {
                    result.Add(map);
                }
            }
            return result;
        }

        /// <summary>
        /// Locates the best constructor that allows all the given properties to be set.
        /// </summary>
        /// <param name="propertyMaps">The set of properties we want to set via a constructor</param>
        /// <param name="currentValues">The set of current values for these properties</param>
        /// <param name="attributeType">The type of attribute whose ctor we need to call</param>
        /// <returns>The list of constructor parameters, in the order they must be passed to the ctor, to invoke the best constructor.  
        /// Null means no constructor is available that can set them.</returns>
        private static ParameterInfo[] FindBestConstructor(ICollection<PropertyMap> propertyMaps, IDictionary<string, object> currentValues, Type attributeType)
        {
            ConstructorInfo[] ctors = attributeType.GetConstructors();
            if (propertyMaps.Count > 0)
            {
                foreach (ConstructorInfo ctor in ctors)
                {
                    ParameterInfo[] parameters = ctor.GetParameters();
                    if (ParametersAcceptAllProperties(parameters, propertyMaps, currentValues))
                    {
                        return parameters;
                    }
                }
            }
            else
            {
                // If there are no unsettable properties, try to find a constructor with following preferences:
                // 1. Parameterless ctor
                // 2. Else If there is only 1 current value, return the constructor that takes a parameter of the same type.
                // 3. Else return the 1st constructor

                ParameterInfo[] returnParameters = null;
                foreach (ConstructorInfo ctor in ctors)
                {
                    ParameterInfo[] parameters = ctor.GetParameters();
                    if (parameters.Length == 0)
                    {
                        return parameters;
                    }
                    else if (parameters.Length == 1 && currentValues.Count == 1 &&
                        CanValueBeAssignedToType(parameters[0].ParameterType, currentValues.Values.Single()))
                    {
                        returnParameters = parameters;
                    }
                    else if (returnParameters == null)
                    {
                        returnParameters = parameters;
                    }
                }
                return returnParameters;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the given set of constuctor parameters can legally be assigned the
        /// given set of property values.
        /// </summary>
        /// <param name="parameters">The list of constructor parameters to consider</param>
        /// <param name="propertyMaps">The list of properties we would assign into these parameters</param>
        /// <param name="currentValues">The current values for the properties</param>
        /// <returns><c>true</c> if all the properties can be assigned to those parameters</returns>
        private static bool ParametersAcceptAllProperties(ParameterInfo[] parameters, ICollection<PropertyMap> propertyMaps, IDictionary<string, object> currentValues)
        {
            foreach (PropertyMap map in propertyMaps)
            {
                // Note use of Setter -- we are assuming the ctor params match the name of the property we use to *set* the values
                PropertyInfo property = map.Setter;
                bool foundMatch = false;
                foreach (ParameterInfo parameter in parameters)
                {
                    if (property.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase) && CanValueBeAssignedToType(parameter.ParameterType, currentValues[property.Name]))
                    {
                        foundMatch = true;
                        break;
                    }
                }

                // If did not find parameter with the same name, we will tolerate if we have only a single
                // parameter that can legally be assigned a single value.
                if (!foundMatch)
                {
                    return
                        propertyMaps.Count == 1 &&
                        parameters.Length == 1 &&
                        CanValueBeAssignedToType(parameters[0].ParameterType, currentValues[propertyMaps.First().Setter.Name]);
                }
            }
            return true;
        }

        /// <summary>
        /// Builds and returns a dictionary of all the current values for all the specified properties in a given
        /// object instance.
        /// </summary>
        /// <param name="propertyMaps">The set of properties to read</param>
        /// <param name="attribute">The attribute instance to fetch the property values from</param>
        /// <returns>The dictionary of name-value pairs, one per property</returns>
        /// <exception cref="AttributeBuilderException">When a property on the attribute throws an exception</exception>
        private static Dictionary<string, object> GetPropertyValues(IEnumerable<PropertyMap> propertyMaps, Attribute attribute)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (PropertyMap map in propertyMaps)
            {
                PropertyInfo propertyInfo = map.Getter;
                object value = null;
                MethodInfo getterMethod;

                // HACK This is temporary until we have property builders.  DisplayAttribute.AutoGenerateField and
                // DisplayAttribute.AutoGenerateFilter will throw if they have not been set.  Instead, we have to call
                // the method of GetAutoGenerate(Field|Filter).
                if (attribute is DisplayAttribute && (propertyInfo.Name == "AutoGenerateField" || propertyInfo.Name == "AutoGenerateFilter"))
                {
                    getterMethod = attribute.GetType().GetMethod("Get" + propertyInfo.Name);

                    if (getterMethod != null && getterMethod.IsPublic)
                    {
                        value = getterMethod.Invoke(attribute, null);
                    }
                }
                else
                {
                    getterMethod = propertyInfo.GetGetMethod();

                    if (getterMethod != null && getterMethod.IsPublic)
                    {
                        // If we cannot get the attribute property, we wrap the exception
                        // in an AttributeBuilderException to provide the context of which
                        // property on the attribute caused the exception.
                        // One common exception path is InvalidOperationException arising from
                        // attributes that have been improperly constructed (see DisplayAttribute)
                        try
                        {
                            value = propertyInfo.GetValue(attribute, null);
                        }
                        catch (TargetInvocationException ex)
                        {
                            // Note that it's unusual to catch all exceptions like this.  But in this context,
                            // we're operating in a temporary app domain specific to code generation.  So even
                            // if the exception would corrupt state at run-time, here for code generation, we
                            // don't need to worry about the state since the app domain will be torn down upon
                            // code generation completion.  By catching all exceptions here, we prevent build
                            // errors that look like exceptions from our code generation (that would be perceived
                            // as bugs in our product) and we're reporting those exceptions as build warnings
                            // so the developer can address them in the application code.
                            throw new AttributeBuilderException(ex.InnerException, attribute.GetType(), propertyInfo.Name);
                        }
                    }
                }

                result[propertyInfo.Name] = value;
            }
            return result;
        }

        /// <summary>
        /// Builds and returns a dictionary of all the default values for the given properties for a given attribute type
        /// </summary>
        /// <param name="propertyMaps">The set of properties to read</param>
        /// <param name="attribute">The current attribute instance</param>
        /// <param name="currentValues">The current values for the attribute instance for which we are generating code</param>
        /// <returns>The dictionary of name-value pairs containing default values for each property</returns>
        private static Dictionary<string, object> GetDefaultPropertyValues(IEnumerable<PropertyMap> propertyMaps, Attribute attribute, Dictionary<string, object> currentValues)
        {
            Type attributeType = attribute.GetType();
            Attribute defaultAttributeInstance = null;

            // If have default ctor, simply create a new instance of the attribute and read out its values
            ConstructorInfo constructorInfo = attributeType.GetConstructor(TypeUtility.EmptyTypes);
            bool haveDefaultCtor = constructorInfo != null && constructorInfo.IsPublic;
            if (haveDefaultCtor && !attributeType.IsAbstract)
            {
                defaultAttributeInstance = Activator.CreateInstance(attributeType) as Attribute;
            }

            // If we were able to construct an attribute, simply fetch all its property values
            // and consider those the defaults
            if (defaultAttributeInstance != null)
            {
                return GetPropertyValues(propertyMaps, defaultAttributeInstance);
            }

            // If we were not able to construct an attribute, simply construct
            // default values for every property
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (PropertyMap map in propertyMaps)
            {
                // Note: we'd like to say default(t) below, but that is not supported for
                // runtime computed types, only static types.  The following pattern works
                // because every known value type has a default ctor.
                // Extra twist -- sometimes the property type itself is very general (e.g. Object)
                // so the current value may indicate a more appropriate type to use for a default
                PropertyInfo propertyInfo = map.Getter;
                object currentValue = currentValues[propertyInfo.Name];
                Type t = (currentValue == null) ? propertyInfo.PropertyType : currentValue.GetType();
                object value = DefaultInstanceForType(t);
                result[propertyInfo.Name] = value;
            }
            return result;
        }

        /// <summary>
        /// Retrieves the list of properties that contain values other than the defaults for an attribute
        /// </summary>
        /// <remarks>This list becomes the set of properties for which explicit code gen is required, assuming
        /// that if they are not set, they will retain their default values.</remarks>
        /// <param name="propertyMaps">The set of properties to consider</param>
        /// <param name="currentValues">The current values for the properties</param>
        /// <param name="defaultValues">The default values for the properties</param>
        /// <returns>The set of properties whose current values are other than their default values</returns>
        private static List<PropertyMap> GetNonDefaultProperties(IEnumerable<PropertyMap> propertyMaps, Dictionary<string, object> currentValues, Dictionary<string, object> defaultValues)
        {
            List<PropertyMap> result = new List<PropertyMap>();
            foreach (PropertyMap map in propertyMaps)
            {
                PropertyInfo propertyInfo = map.Getter;
                object currentValue = currentValues[propertyInfo.Name];
                object defaultValue = defaultValues[propertyInfo.Name];
                if (!Object.Equals(currentValue, defaultValue))
                {
                    result.Add(map);
                }
            }
            return result;
        }

        /// <summary>
        /// Determines whether the given property could legally be set via an
        /// Attribute declaration, where only constants or arrays of constants are allowed
        /// </summary>
        /// <param name="property">The property to test</param>
        /// <returns><c>true</c> means the property could be set via an Attribute declaration</returns>
        private static bool CanPropertyBeSetDeclaratively(PropertyInfo property)
        {
            Type t = property.PropertyType;
            if (t.IsArray && t.HasElementType)
            {
                t = t.GetElementType();
            }

            // We permit Type and any simple type, such as int, string, GUID, etc
            // Note: typeof(object) is a special case.  Properties declared as Object exist to
            // allow arbitrary object types.  We cannot determine how these properties will be
            // used and must signal it is legal.
            if (t == typeof(Type) || t == typeof(object) || TypeUtility.IsPredefinedSimpleType(t))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Constructs a collection of PropertyMaps.  A PropertyMap allows a "getter" property to
        /// redirect to a different "setter" property.  Properties that should not be treated by
        /// code gen don't appear in the map.  The getter/setter mapping is done because it is
        /// a relatively common scenario to have calculated properties that either cannot be set
        /// directly or should be effectively set through a different property.
        /// </summary>
        /// <param name="attribute">The current attribute instance</param>
        /// <returns>A list of <see cref="PropertyMap"/> instances.</returns>
        private List<PropertyMap> BuildPropertyMaps(Attribute attribute)
        {
            List<PropertyMap> result = new List<PropertyMap>();
            PropertyInfo[] properties = attribute.GetType().GetProperties();
            foreach (PropertyInfo getterInfo in properties)
            {
                // Allow subclasses to redirect the getter to a different setter.
                // Null means don't codegen this property.
                string setterPropertyName = this.MapProperty(getterInfo, attribute);
                if (setterPropertyName != null)
                {
                    PropertyInfo setterInfo = null;
                    if (String.Equals(setterPropertyName, getterInfo.Name, StringComparison.Ordinal))
                    {
                        setterInfo = getterInfo;
                    }
                    else
                    {
                        setterInfo = attribute.GetType().GetProperty(setterPropertyName);
                    }
                    if (setterInfo != null && CanPropertyBeSetDeclaratively(setterInfo))
                    {
                        result.Add(new PropertyMap(getterInfo, setterInfo));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Helper class used internally.  A PropertyMap is simply a tuple that relates a property getter with its appropriate setter.
        /// Normally these are identical, but a surprising number of attributes expose calculated getters that are actually influenced
        /// by some other setter.  By keeping these concepts separate, it is easier to allow derived classes to map computed getters
        /// to a better property.
        /// </summary>
        internal class PropertyMap
        {
            private readonly PropertyInfo _getter;
            private readonly PropertyInfo _setter;

            /// <summary>
            /// Initializes a new instance of the <see cref="PropertyMap"/> class.
            /// </summary>
            /// <param name="getter">The property's getter <see cref="PropertyInfo"/>.</param>
            /// <param name="setter">The property's setter <see cref="PropertyInfo"/>.</param>
            public PropertyMap(PropertyInfo getter, PropertyInfo setter)
            {
                this._getter = getter;
                this._setter = setter;
            }

            /// <summary>
            /// Gets the property's getter <see cref="PropertyInfo"/>.
            /// </summary>
            public PropertyInfo Getter { get { return this._getter; } }

            /// <summary>
            /// Gets the property's setter <see cref="PropertyInfo"/>.
            /// </summary>
            public PropertyInfo Setter { get { return this._setter; } }
        }
    }
}
