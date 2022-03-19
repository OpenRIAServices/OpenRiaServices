using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security;
using OpenRiaServices.Server;
using System.Threading;

namespace OpenRiaServices.Hosting.Wcf
{
    /// <summary>
    /// Takes care of generating CLR types based on the virtual shape of an exposed type. E.g. we may be dealing with 
    /// projection properties that don't physically exist on the CLR type. Because WCF doesn't know about TypeDescriptor, 
    /// it won't see these virtual properties. This is why we generate CLR types that contain first-class properties.
    /// At serialization/deserialization time, the surrogate type will delegate to both the physical and virtual 
    /// properties on the real exposed object.
    /// </summary>
    internal static class DataContractSurrogateGenerator
    {
        private static HashSet<string> coveredContractNamespaces = new HashSet<string>();
        private static ModuleBuilder moduleBuilder = DataContractSurrogateGenerator.CreateModuleBuilder();
        private static Dictionary<Type, Type> surrogateTypes = new Dictionary<Type, Type>();
        private static ReaderWriterLockSlim surrogateTypeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Emits a surrogate type for the specified type.
        /// </summary>
        /// <param name="knownExposedTypes">The known set of exposed types.</param>
        /// <param name="type">The original type.</param>
        /// <returns>The surrogate type.</returns>
        public static Type GetSurrogateType(HashSet<Type> knownExposedTypes, Type type)
        {
            DataContractSurrogateGenerator.surrogateTypeLock.EnterUpgradeableReadLock();
            Type surrogateType;

            try
            {
                // Get the type for which we'll generate a surrogate.
                type = GetSerializationType(knownExposedTypes, type);
                if (type == null)
                {
                    return null;
                }

                if (!DataContractSurrogateGenerator.surrogateTypes.TryGetValue(type, out surrogateType))
                {
                    // We couldn't find a surrogate for the type, so create one now. We don't ever 
                    // want to create multiple surrogate types (not even from multiple threads), so 
                    // make sure we serialize that process by taking a write lock.
                    // We don't use a ConcurrentDictionary because it doesn't help us serializing 
                    // surrogate generation.
                    DataContractSurrogateGenerator.surrogateTypeLock.EnterWriteLock();
                    try
                    {
                        surrogateType = CreateSurrogateType(knownExposedTypes, type);
                        DataContractSurrogateGenerator.surrogateTypes.Add(type, surrogateType);
                    }
                    finally
                    {
                        if (DataContractSurrogateGenerator.surrogateTypeLock.IsWriteLockHeld)
                        {
                            DataContractSurrogateGenerator.surrogateTypeLock.ExitWriteLock();
                        }
                    }
                }
            }
            finally
            {
                if (DataContractSurrogateGenerator.surrogateTypeLock.IsUpgradeableReadLockHeld)
                {
                    DataContractSurrogateGenerator.surrogateTypeLock.ExitUpgradeableReadLock();
                }
            }

            return surrogateType;
        }

        private static Type GetSerializationType(HashSet<Type> knownExposedTypes, Type type)
        {
            // Verify the specified type is a known exposed type.
            if (!knownExposedTypes.Contains(type))
            {
                // Check if maybe one of its base types is a known exposed type.
                for (type = type.BaseType; type != null; type = type.BaseType)
                {
                    if (knownExposedTypes.Contains(type))
                    {
                        break;
                    }
                }
            }
            return type;
        }

        private static Type CreateSurrogateType(HashSet<Type> knownExposedTypes, Type type)
        {
            CustomAttributeBuilder dataContractAttBuilder = DataContractSurrogateGenerator.GetDataContractAttributeBuilder(type);

            // Emit a dynamic type.
            TypeAttributes typeAttributes = TypeAttributes.Public;
            if (type.IsSealed)
            {
                typeAttributes |= TypeAttributes.Sealed;
            }

            TypeBuilder typeBuilder = DataContractSurrogateGenerator.moduleBuilder.DefineType(type.FullName, typeAttributes);
            if (dataContractAttBuilder != null)
            {
                typeBuilder.SetCustomAttribute(dataContractAttBuilder);
            }

            // Attach a [SecuritySafeCritical] to the surrogate type to permit its setters to deal with
            // types that may be SafeCritical or Critical
            // If the AppDomain is running in medium trust, this attribute won't have any effect.
            CustomAttributeBuilder safeCriticalAttrBuilder = new CustomAttributeBuilder(
                typeof(SecuritySafeCriticalAttribute).GetConstructor(Type.EmptyTypes),
                Array.Empty<object>());
            typeBuilder.SetCustomAttribute(safeCriticalAttrBuilder);

            Type actualParentType;
            Type parentSurrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownExposedTypes, type.BaseType);
            if (parentSurrogateType != null)
            {
                // Figure out what the actual parent of the exposed type is.
                actualParentType = type.BaseType;
                while (actualParentType != typeof(object))
                {
                    if (knownExposedTypes.Contains(actualParentType))
                    {
                        break;
                    }

                    actualParentType = actualParentType.BaseType;
                }

                typeBuilder.SetParent(parentSurrogateType);
            }
            else
            {
                parentSurrogateType = typeof(object);
                actualParentType = typeof(object);
            }

            FieldInfo wrapperField = EmitICloneableImplementation(typeBuilder, type, parentSurrogateType);
            EmitConstructorsAndProperties(typeBuilder, type, parentSurrogateType, actualParentType, wrapperField);
            EmitOnDeserializingMethod(typeBuilder, type, wrapperField);
            EmitContractNamespaceAttributes(type);

            return typeBuilder.CreateType();
        }

        // Makes sure the surrogate type implements ICloneable. We use ICloneable during deserialization 
        // to get the real exposed object from a surrogate object.
        // Note that ideally we'd use an internal "IWrapper" interface, but the interface needs to be public 
        // because the surrogates live in their own assembly. Instead of defining a public interface, we decided 
        // to reuse ICloneable.
        private static FieldInfo EmitICloneableImplementation(TypeBuilder typeBuilder, Type type, Type parentSurrogateType)
        {
            if (parentSurrogateType == typeof(object))
            {
                // protected Entity _wrapper
                FieldInfo wrapperField = typeBuilder.DefineField("_$wrapper", type, FieldAttributes.Family);

                // Implement ICloneable.
                typeBuilder.AddInterfaceImplementation(typeof(ICloneable));

                // public object GetUnderlyingObject() {
                //     return _$wrapper;
                // }
                MethodBuilder getUnderlyingObjectMethodBuilder = typeBuilder.DefineMethod("Clone", MethodAttributes.Public | MethodAttributes.Virtual, typeof(object), Type.EmptyTypes);

                // The surrogate assembly is SecurityCritical, but ICloneable is Transparent, so make sure our Clone method 
                // is marked as SecuritySafeCritical.
                getUnderlyingObjectMethodBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        typeof(SecuritySafeCriticalAttribute).GetConstructor(Type.EmptyTypes),
                        Array.Empty<object>()));

                ILGenerator getUnderlyingObjectMethodGenerator = getUnderlyingObjectMethodBuilder.GetILGenerator();

                getUnderlyingObjectMethodGenerator.Emit(OpCodes.Ldarg_0);
                getUnderlyingObjectMethodGenerator.Emit(OpCodes.Ldfld, wrapperField);
                getUnderlyingObjectMethodGenerator.Emit(OpCodes.Ret);
                return wrapperField;
            }
            else
            {
                return parentSurrogateType.GetField("_$wrapper", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        // Emits a constructor that takes an object that the surrogate wraps, as well as 
        // a type initializer that will load (for this AppDomain) all the PropertyDescriptors 
        // for virtual properties.
        private static void EmitConstructorsAndProperties(TypeBuilder typeBuilder, Type type, Type parentSurrogateType, Type parentType, FieldInfo wrapperField)
        {
            // public Surrogate(Entity entity) : base(entity)
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { type });
            ILGenerator constructorGenerator = constructorBuilder.GetILGenerator();

            if (parentSurrogateType == typeof(object))
            {
                // base()
                constructorGenerator.Emit(OpCodes.Ldarg_0);
                constructorGenerator.Emit(OpCodes.Call, parentSurrogateType.GetConstructor(Type.EmptyTypes));
                
                // _wrapper = wrapper
                constructorGenerator.Emit(OpCodes.Ldarg_0);
                constructorGenerator.Emit(OpCodes.Ldarg_1);
                constructorGenerator.Emit(OpCodes.Stfld, wrapperField);
            }
            else
            {
                // base(entity)
                constructorGenerator.Emit(OpCodes.Ldarg_0);
                constructorGenerator.Emit(OpCodes.Ldarg_1);
                constructorGenerator.Emit(OpCodes.Call, parentSurrogateType.GetConstructor(new Type[] { parentType }));
            }

            constructorGenerator.Emit(OpCodes.Ret);

            // Only generate a type initializer if there are any virtual properties.
            Lazy<ILGenerator> typeInitializerFactory = new Lazy<ILGenerator>(() =>
            {
                // static Surrogate() {
                //     PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(Entity));
                //     (_Property = properties["Property"];)*
                // }
                ConstructorBuilder typeInitializerBuilder = typeBuilder.DefineTypeInitializer();
                ILGenerator typeInitializerGenerator = typeInitializerBuilder.GetILGenerator();

                // PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(Entity));
                typeInitializerGenerator.DeclareLocal(typeof(PropertyDescriptorCollection));

                typeInitializerGenerator.Emit(OpCodes.Ldtoken, type);
                typeInitializerGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                typeInitializerGenerator.Emit(OpCodes.Call, typeof(TypeDescriptor).GetMethod("GetProperties", new Type[] { typeof(Type) }));
                typeInitializerGenerator.Emit(OpCodes.Stloc_0);
                return typeInitializerGenerator;
            });

            // Generate all the properties from here, because properties potentially add code to the type initializer.
            EmitProperties(typeBuilder, type, parentSurrogateType, wrapperField, typeInitializerFactory);

            if (typeInitializerFactory.IsValueCreated)
            {
                typeInitializerFactory.Value.Emit(OpCodes.Ret);
            }
        }

        // During deserialization, none of our constructors will be invoked. Instead, only our 
        // OnDeserializing method will be invoked. We implement this to create an instance 
        // of the real object, such that our surrogate properties can set the properties 
        // on the real object.
        private static void EmitOnDeserializingMethod(TypeBuilder typeBuilder, Type type, FieldInfo wrapperField)
        {
            if (type.IsAbstract)
            {
                return;
            }

            // public void OnDeserializing(StreamingContext)
            MethodBuilder onDeserializingMethodBuilder = typeBuilder.DefineMethod("OnDeserializing", MethodAttributes.Public, null, new Type[] { typeof(StreamingContext) });
            ILGenerator onDeserializingMethodGenerator = onDeserializingMethodBuilder.GetILGenerator();

            onDeserializingMethodBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    typeof(OnDeserializingAttribute).GetConstructor(Type.EmptyTypes),
                    Array.Empty<object>()));

            // _$wrapper = new Entity();
            onDeserializingMethodGenerator.Emit(OpCodes.Ldarg_0);
            onDeserializingMethodGenerator.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            onDeserializingMethodGenerator.Emit(OpCodes.Stfld, wrapperField);

            onDeserializingMethodGenerator.Emit(OpCodes.Ret);
        }

        private static void EmitProperties(TypeBuilder typeBuilder, Type type, Type parentSurrogateType, FieldInfo wrapperField, Lazy<ILGenerator> typeInitializerGenerator)
        {
            bool hasParent = (parentSurrogateType != typeof(object));

            // Create fields for each of the properties.
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(type))
            {
                // Skip properties inherited from a base class.
                // We allow "holes" in the visible entity hierarchy (i.e. some entity types are not exposed
                // via the DomainService and hence do not appear in the list of known entity types).
                // Properties appearing in unexposed entities are lifted to the first visible subclass.
                // So if our parent surrogate does not declare a property, it means we have discovered
                // a hole in the visible hierarchy and must lift that property.
                if (hasParent && pd.ComponentType != type && parentSurrogateType.GetProperty(pd.Name) != null)
                {
                    continue;
                }

                if (SerializationUtility.IsSerializableDataMember(pd))
                {
                    DataContractSurrogateGenerator.EmitProperty(typeBuilder, wrapperField, typeInitializerGenerator, pd, pd.Name);
                }
            }
        }

        // Emit a surrogate property.
        private static void EmitProperty(TypeBuilder typeBuilder, FieldInfo wrapperField, Lazy<ILGenerator> typeInitializerGenerator, PropertyDescriptor pd, string name)
        {
            PropertyInfo pi = pd.ComponentType.GetProperty(pd.Name);
            if (pi != null)
            {
                EmitClrProperty(typeBuilder, wrapperField, pd, pi, name);
            }
            else
            {
                EmitAttachedProperty(typeBuilder, wrapperField, typeInitializerGenerator, pd, name);
            }
        }

        // Emit a surrogate property for a virtual property (a property that doesn't exist on the physical CLR type).
        // The PropertyDescriptor for each virtual property is initialized in the type initializer of a surrogate type.
        // The surrogate code we'll generate will look like this:
        // public <PropertyType> <PropertyName> {
        //     get {
        //         // For reference types.
        //         return (<PropertyType>)$<PropertyName>.GetValue(_$wrapper);
        //
        //         // For value types.
        //         object value = $<PropertyName>.GetValue(_$wrapper);
        //         if (value == null) {
        //             return default(value);
        //         }
        //         return (<PropertyType>)value;
        //
        //         // For Binary.
        //         Binary value = (Binary)$<PropertyName>.GetValue(_$wrapper);
        //         if (value == null) {
        //             return null;
        //         }
        //         return value.ToArray();
        //     }
        //     set {
        //         if (value == null) {
        //             return;
        //         }
        //
        //         // For normal types.
        //         $<PropertyName>.SetValue(_$wrapper, value);
        //
        //         // For value types.
        //         $<PropertyName>.SetValue(_$wrapper, (object)value);
        //
        //         // For Binary.
        //         Binary valueToStore;
        //         if (value == null) {
        //             valueToStore = null;
        //         }
        //         else {
        //             valueToStore = new Binary(value);
        //         }
        //         $<PropertyName>.SetValue(_$wrapper, valueToStore);
        //     }
        // }
        private static void EmitAttachedProperty(TypeBuilder typeBuilder, FieldInfo wrapperField, Lazy<ILGenerator> typeInitializerFactory, PropertyDescriptor pd, string name)
        {
            // private static PropertyDescriptor $property;
            FieldBuilder propertyDescFieldBuilder = typeBuilder.DefineField("$" + name, typeof(PropertyDescriptor), FieldAttributes.Private | FieldAttributes.Static);

            EmitPropertyInitializer(propertyDescFieldBuilder, typeInitializerFactory, name);

            Type propertyType = SerializationUtility.GetClientType(pd.PropertyType);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, propertyType, null);

            CustomAttributeBuilder dataMemberAtt = DataContractSurrogateGenerator.GetDataMemberAttributeBuilder(
                pd.Attributes[typeof(DataMemberAttribute)] as DataMemberAttribute);
            propertyBuilder.SetCustomAttribute(dataMemberAtt);

            // get {
            //     return $property.GetValue(_$wrapper);
            // }
            MethodBuilder getPropertyMethodBuilder = typeBuilder.DefineMethod("get_" + name, MethodAttributes.Public, propertyType, Type.EmptyTypes);
            ILGenerator generator = getPropertyMethodBuilder.GetILGenerator();

            // Get the PropertyDescriptor.
            generator.Emit(OpCodes.Ldsfld, propertyDescFieldBuilder);

            // Push the wrapper object onto the stack. We'll use it as an argument for our 
            // call to GetValue later on.
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, wrapperField);

            // PropertyDescriptor.GetValue(_$wrapper).
            generator.Emit(OpCodes.Callvirt, typeof(PropertyDescriptor).GetMethod("GetValue"));

            // Unbox/cast.
            DynamicMethodUtility.EmitFromObjectConversion(generator, pd.PropertyType);

            // Deal with client-server type conversions.
            if (propertyType != pd.PropertyType)
            {
                EmitToClientConversion(generator, pd.PropertyType, propertyType);
            }

            generator.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropertyMethodBuilder);

            MethodBuilder setPropertyMethodBuilder = typeBuilder.DefineMethod("set_" + name, MethodAttributes.Public, null, new Type[] { propertyType });
            generator = setPropertyMethodBuilder.GetILGenerator();

            Label returnLabel = generator.DefineLabel();

            // Data members require a getter and setter. However, if the real property is read-only, make sure 
            // our surrogate property setter is a no-op.
            if (!pd.IsReadOnly)
            {
                // NOTE: We don't ever set null values, because a property may be required. For 
                //       original objects however it's possible that required properties are not 
                //       roundtripped, as they may not have RoundtripOriginalAttribute.
                // set {
                //     if (value != null) {
                //         $property.SetValue(_$wrapper, value);
                //     }
                // }

                // If the value is null, return.
                if (!propertyType.IsValueType)
                {
                    generator.Emit(OpCodes.Ldarg_1);
                    EmitBranchIfNull(generator, propertyType, returnLabel);
                }
                else if (TypeUtility.IsNullableType(propertyType))
                {
                    generator.Emit(OpCodes.Ldarga_S, 1);
                    EmitBranchIfNull(generator, propertyType, returnLabel);
                }

                // Get the PropertyDescriptor.
                generator.Emit(OpCodes.Ldsfld, propertyDescFieldBuilder);

                // Push the wrapper object onto the stack. We'll use it as an argument for our 
                // call to SetValue later on.
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, wrapperField);

                // Push the value onto the stack. We'll use it as the 2nd argument for 
                // our call to SetValue.
                generator.Emit(OpCodes.Ldarg_1);

                // Deal with client-server type conversions.
                if (propertyType != pd.PropertyType)
                {
                    EmitToServerConversion(generator, propertyType, pd.PropertyType);
                }

                // Box value types.
                DynamicMethodUtility.EmitToObjectConversion(generator, pd.PropertyType);

                // PropertyDescriptor.SetValue(_$wrapper, value).
                generator.Emit(OpCodes.Callvirt, typeof(PropertyDescriptor).GetMethod("SetValue"));
            }

            generator.MarkLabel(returnLabel);
            generator.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setPropertyMethodBuilder);
        }

        // Emit a surrogate property for a CLR property.
        // The surrogate code we'll generate will look like this:
        // public <PropertyType> <PropertyName> {
        //     get {
        //         // For normal types.
        //         return _$wrapper.<PropertyName>;
        // 
        //         // For Binary.
        //         Binary value = _$wrapper.<PropertyName>;
        //         if (value == null) {
        //             return null;
        //         }
        //         return value.ToArray();
        //     }
        //     set {
        //         if (value == null) {
        //             return;
        //         }
        //
        //         // For normal types.
        //         _$wrapper.<PropertyName> = value;
        // 
        //         // For Binary.
        //         Binary valueToStore;
        //         if (value == null) {
        //             valueToStore = null;
        //         }
        //         else {
        //             valueToStore = new Binary(value);
        //         }
        //         _$wrapper.<PropertyName> = valueToStore;
        //     }
        // }
        private static void EmitClrProperty(TypeBuilder typeBuilder, FieldInfo wrapperField, PropertyDescriptor pd, PropertyInfo pi, string name)
        {
            Type propertyType = SerializationUtility.GetClientType(pi.PropertyType);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, propertyType, null);

            CustomAttributeBuilder dataMemberAtt = DataContractSurrogateGenerator.GetDataMemberAttributeBuilder(
                pd.Attributes.OfType<DataMemberAttribute>().FirstOrDefault());
            propertyBuilder.SetCustomAttribute(dataMemberAtt);

            // get {
            //     return ((Entity)$wrapper).Property;
            // }
            MethodBuilder getPropertyMethodBuilder = typeBuilder.DefineMethod("get_" + name, MethodAttributes.Public, propertyType, Type.EmptyTypes);
            ILGenerator generator = getPropertyMethodBuilder.GetILGenerator();

            // Push the wrapper object onto the stack.
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, wrapperField);
            generator.Emit(OpCodes.Castclass, pi.DeclaringType);

            // Call the property getter.
            generator.Emit(OpCodes.Callvirt, pi.GetGetMethod());

            // Deal with client-server type conversions.
            if (propertyType != pi.PropertyType)
            {
                EmitToClientConversion(generator, pi.PropertyType, propertyType);
            }

            generator.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropertyMethodBuilder);

            MethodBuilder setPropertyMethodBuilder = typeBuilder.DefineMethod("set_" + name, MethodAttributes.Public, null, new Type[] { propertyType });
            generator = setPropertyMethodBuilder.GetILGenerator();

            Label returnLabel = generator.DefineLabel();

            // Data members require a getter and setter. However, if the real property is read-only, make sure 
            // our surrogate property setter is a no-op.
            MethodInfo setMethod = pi.GetSetMethod();
            if (setMethod != null && setMethod.IsPublic)
            {
                // NOTE: We don't ever set null values, because a property may be required. For 
                //       original objects however it's possible that required properties are not 
                //       roundtripped, as they may not have RoundtripOriginalAttribute.
                // set {
                //     if (value != null) {
                //         _$wrapper.Property = value;
                //     }
                // }

                // If the value is null, return.
                if (!propertyType.IsValueType)
                {
                    generator.Emit(OpCodes.Ldarg_1);
                    EmitBranchIfNull(generator, propertyType, returnLabel);
                }
                else if (TypeUtility.IsNullableType(propertyType))
                {
                    generator.Emit(OpCodes.Ldarga_S, 1);
                    EmitBranchIfNull(generator, propertyType, returnLabel);
                }

                // Push the wrapper object onto the stack.
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, wrapperField);
                generator.Emit(OpCodes.Castclass, pi.DeclaringType);

                // Push the value onto the stack.
                generator.Emit(OpCodes.Ldarg_1);

                // Deal with client-server type conversions.
                if (propertyType != pi.PropertyType)
                {
                    EmitToServerConversion(generator, propertyType, pi.PropertyType);
                }

                // Call the property setter.
                generator.Emit(OpCodes.Callvirt, setMethod);
            }

            generator.MarkLabel(returnLabel);
            generator.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setPropertyMethodBuilder);
        }

        // EmitToClientConversion and EmitToServerConversion need to be kept in sync with the Utility type.
        private static void EmitToClientConversion(ILGenerator methodGenerator, Type sourceType, Type targetType)
        {
            if (targetType == typeof(byte[]) && BinaryTypeUtility.IsTypeBinary(sourceType))
            {
                Label continueLabel = methodGenerator.DefineLabel();
                Label notNullLabel = methodGenerator.DefineLabel();

                // Check if the value is null.
                methodGenerator.Emit(OpCodes.Dup);
                methodGenerator.Emit(OpCodes.Brtrue, notNullLabel);

                // If it is, replace it with an "untyped" null.
                methodGenerator.Emit(OpCodes.Pop);
                methodGenerator.Emit(OpCodes.Ldnull);
                methodGenerator.Emit(OpCodes.Br, continueLabel);

                // Otherwise, call ToArray().
                MethodInfo toArrayMethod = sourceType.GetMethod("ToArray", Type.EmptyTypes);
                methodGenerator.MarkLabel(notNullLabel);
                methodGenerator.Emit(OpCodes.Call, toArrayMethod);

                methodGenerator.MarkLabel(continueLabel);
            }
        }

        // EmitToClientConversion and EmitToServerConversion need to be kept in sync with the Utility type.
        private static void EmitToServerConversion(ILGenerator methodGenerator, Type sourceType, Type targetType)
        {
            if (BinaryTypeUtility.IsTypeBinary(targetType) && sourceType == typeof(byte[]))
            {
                Label continueLabel = methodGenerator.DefineLabel();
                Label notNullLabel = methodGenerator.DefineLabel();

                // Check if the value is null.
                methodGenerator.Emit(OpCodes.Dup);
                methodGenerator.Emit(OpCodes.Brtrue, notNullLabel);

                // If it is, replace it with an "untyped" null.
                methodGenerator.Emit(OpCodes.Pop);
                methodGenerator.Emit(OpCodes.Ldnull);
                methodGenerator.Emit(OpCodes.Br, continueLabel);

                // Otherwise, call new Binary(byte[]).
                ConstructorInfo toBinaryConstructor = targetType.GetConstructor(new Type[] { typeof(byte[]) });
                methodGenerator.MarkLabel(notNullLabel);
                methodGenerator.Emit(OpCodes.Newobj, toBinaryConstructor);

                methodGenerator.MarkLabel(continueLabel);
            }
        }

        private static void EmitBranchIfNull(ILGenerator generator, Type propertyType, Label target)
        {
            if (!propertyType.IsValueType)
            {
                generator.Emit(OpCodes.Brfalse, target);
            }
            else if (TypeUtility.IsNullableType(propertyType))
            {
                generator.Emit(OpCodes.Call, propertyType.GetProperty("HasValue").GetGetMethod());
                generator.Emit(OpCodes.Brfalse, target);
            }
        }

        // Emit code in the type initializer of the surrogate type to get all the PropertyDescriptors for all 
        // virtual properties.
        // This will generate code that looks like:
        // $<PropertyName> = properties["<PropertyName>"]; // properties is defined at the beginning of the type initializer.
        private static void EmitPropertyInitializer(FieldBuilder propertyDescFieldBuilder, Lazy<ILGenerator> typeInitializerFactory, string name)
        {
            ILGenerator generator = typeInitializerFactory.Value;
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldstr, name);
            generator.Emit(OpCodes.Callvirt, typeof(PropertyDescriptorCollection).GetMethod("get_Item", new Type[] { typeof(string) }));
            generator.Emit(OpCodes.Stsfld, propertyDescFieldBuilder);
        }

        private static ModuleBuilder CreateModuleBuilder()
        {
            AppDomain myDomain = AppDomain.CurrentDomain;
            AssemblyName assemName = new AssemblyName();
            string name = string.Format(CultureInfo.InvariantCulture, "DataContractSurrogates_{0}", Guid.NewGuid().ToString());
            assemName.Name = name;

            // The following code seems have been part of the original WCF Ria Services.
            // Using it in signed build might enable medium trust support but, I haven't tested it
#if MEDIUM_TRUST
            // If the AppDomain is running in full trust, then put SecurityCriticalAttribute on the dynamic assembly 
            // such that our surrogates can call into entity types that are critical (which is the default for 
            // applications running in full trust).
            // Otherwise, use SecurityTransparentAttribute.
            CustomAttributeBuilder securityAttribute;
            if (myDomain.IsFullyTrusted)
            {
                securityAttribute = new CustomAttributeBuilder(
                       typeof(AllowPartiallyTrustedCallersAttribute).GetConstructor(Type.EmptyTypes),
                       Array.Empty<object>());
            }
            else
            {
                securityAttribute = new CustomAttributeBuilder(
                        typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes),
                        Array.Empty<object>());
            }

            AssemblyBuilder assemblyBuilder = myDomain.DefineDynamicAssembly(
                assemName,
                AssemblyBuilderAccess.Run,
                new CustomAttributeBuilder[] 
                {
                    securityAttribute,
                    new CustomAttributeBuilder(
                        typeof(SecurityRulesAttribute).GetConstructor(new Type[] { typeof(SecurityRuleSet) }),
                        new object[] { SecurityRuleSet.Level2 })
                },
                SecurityContextSource.CurrentAppDomain);
#elif NET5_0_OR_GREATER
// Dev note: the SecurityContextSource.CurrentAppDomain is new in CLR 4.0
            // and permits the assembly builder to inherit the security permissions of the
            // app domain. - CDB Removed, Medium trust support removed
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemName,
                AssemblyBuilderAccess.Run);
#else
            // Dev note: the SecurityContextSource.CurrentAppDomain is new in CLR 4.0
            // and permits the assembly builder to inherit the security permissions of the
            // app domain. - CDB Removed, Medium trust support removed
            AssemblyBuilder assemblyBuilder = myDomain.DefineDynamicAssembly(
                assemName,
                AssemblyBuilderAccess.Run);
#endif

            return assemblyBuilder.DefineDynamicModule(name);
        }

        private static void EmitContractNamespaceAttributes(Type type)
        {
            // CONSIDER: Fix this. Things aren't correct when entity types come from multiple assemblies. We ought to be 
            //           generating a new assembly as well.
            if (!DataContractSurrogateGenerator.coveredContractNamespaces.Contains(type.Assembly.FullName))
            {
                Type cnaType = typeof(ContractNamespaceAttribute);
                foreach (ContractNamespaceAttribute contractNamespaceAtt in type.Assembly.GetCustomAttributes(cnaType, false).OfType<ContractNamespaceAttribute>())
                {
                    CustomAttributeBuilder contractNamespaceAttBuilder =
                        new CustomAttributeBuilder(
                            cnaType.GetConstructor(new Type[] { typeof(string) }),
                            new object[] { contractNamespaceAtt.ContractNamespace },
                            new PropertyInfo[] { cnaType.GetProperty("ClrNamespace") },
                            new object[] { contractNamespaceAtt.ClrNamespace });
                    DataContractSurrogateGenerator.moduleBuilder.SetCustomAttribute(contractNamespaceAttBuilder);
                }

                DataContractSurrogateGenerator.coveredContractNamespaces.Add(type.Assembly.FullName);
            }
        }

        private static CustomAttributeBuilder GetDataContractAttributeBuilder(Type type)
        {
            Dictionary<string, object> dataContractProperties = new Dictionary<string, object>();

            Type dataContractType = typeof(DataContractAttribute);
            DataContractAttribute dataContractAtt = TypeDescriptor.GetAttributes(type).OfType<DataContractAttribute>().SingleOrDefault();
            if (dataContractAtt != null)
            {
                if (dataContractAtt.Name != null)
                {
                    dataContractProperties["Name"] = dataContractAtt.Name;
                }

                if (dataContractAtt.Namespace != null)
                {
                    dataContractProperties["Namespace"] = dataContractAtt.Namespace;
                }
            }

            return GetAttributeBuilder(dataContractType, dataContractProperties);
        }

        private static CustomAttributeBuilder GetDataMemberAttributeBuilder(DataMemberAttribute dataMemberAtt)
        {
            Dictionary<string, object> dataMemberProperties = new Dictionary<string, object>();
            Type dataMemberType = typeof(DataMemberAttribute);

            if (dataMemberAtt != null)
            {
                if (dataMemberAtt.Name != null)
                {
                    dataMemberProperties["Name"] = dataMemberAtt.Name;
                }
                if (!dataMemberAtt.EmitDefaultValue)
                {
                    dataMemberProperties["EmitDefaultValue"] = false;
                }
                if (dataMemberAtt.IsRequired)
                {
                    dataMemberProperties["IsRequired"] = true;
                }

                // The only other property is Order. Since we don't allow this in code gen, we should not allow it here.
            }

            return GetAttributeBuilder(dataMemberType, dataMemberProperties);
        }

        private static CustomAttributeBuilder GetAttributeBuilder(Type type, Dictionary<string, object> attProperties)
        {
            List<PropertyInfo> propertyInfos = new List<PropertyInfo>(attProperties.Count);
            List<object> propertyValues = new List<object>(attProperties.Count);
            foreach (KeyValuePair<string, object> entry in attProperties)
            {
                propertyInfos.Add(type.GetProperty(entry.Key));
                propertyValues.Add(entry.Value);
            }

            return new CustomAttributeBuilder(
                type.GetConstructor(Type.EmptyTypes), Array.Empty<object>(),
                propertyInfos.ToArray(), propertyValues.ToArray());
        }
    }
}
