using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace OpenRiaServices.DomainServices.Server
{
    internal static class DynamicMethodUtility
    {
        /// <summary>
        /// Gets a factory method for a late-bound type.
        /// </summary>
        /// <remarks>
        /// This method will return a delegate to a factory method that looks like this:
        /// <code>
        /// public object FactoryMethod([object[, object]*]) {
        ///     return &lt;Constructor&gt;([object[, object]*]);
        /// }
        /// </code>
        /// </remarks>
        /// <param name="ctor">The constructor to invoke.</param>
        /// <param name="delegateType">The type of delegate to return.</param>
        /// <returns>A factory method delegate.</returns>
        public static Delegate GetFactoryMethod(ConstructorInfo ctor, Type delegateType)
        {
            ParameterInfo[] parameters = ctor.GetParameters();
            Type[] ctorArgTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ctorArgTypes[i] = typeof(object);
            }

            DynamicMethod proxyMethod = new DynamicMethod("FactoryMethod", typeof(object), ctorArgTypes);
            ILGenerator generator = proxyMethod.GetILGenerator();
            for (int i = 0; i < parameters.Length; i++)
            {
                generator.Emit(OpCodes.Ldarg, i);
                EmitFromObjectConversion(generator, parameters[i].ParameterType);
            }
            generator.Emit(OpCodes.Newobj, ctor);
            generator.Emit(OpCodes.Ret);

            return proxyMethod.CreateDelegate(delegateType);
        }

        /// <summary>
        /// Gets an early-bound delegate for an instance method.
        /// </summary>
        /// <remarks>
        /// This method will return a delegate to a proxy method that looks like this:
        /// <code>
        /// public object &lt;MethodName&gt;(DomainService target, object[] parameters) {
        ///     return ((&lt;TargetType&gt;)target).&lt;MethodName&gt;();
        ///     return ((&lt;TargetType&gt;)target).&lt;MethodName&gt;((&lt;ParameterType&gt;)parameters[0]);
        /// }
        /// </code>
        /// </remarks>
        /// <param name="method">The method that the delegate should invoke.</param>
        /// <returns>A delegate.</returns>
        public static Func<DomainService, object[], object> GetDelegateForMethod(MethodInfo method)
        {
            return (Func<DomainService, object[], object>)GetDynamicMethod(method).CreateDelegate(typeof(Func<DomainService, object[], object>));
        }

        /// <summary>
        /// Emits a conversion to type object for the value on the stack.
        /// </summary>
        /// <param name="generator">The code generator to use.</param>
        /// <param name="sourceType">The type of value on the stack.</param>
        public static void EmitToObjectConversion(ILGenerator generator, Type sourceType)
        {
            if (sourceType.IsValueType)
            {
                generator.Emit(OpCodes.Box, sourceType);
            }
        }

        /// <summary>
        /// Emits a conversion from type object for the value on the stack.
        /// </summary>
        /// <param name="generator">The code generator to use.</param>
        /// <param name="targetType">The type to which the value on the stack needs to be converted.</param>
        public static void EmitFromObjectConversion(ILGenerator generator, Type targetType)
        {
            if (targetType.IsValueType)
            {
                Label continueLabel = generator.DefineLabel();
                Label nonNullLabel = generator.DefineLabel();
                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Brtrue, nonNullLabel);

                // If the value is null, put a default value on the stack.
                generator.Emit(OpCodes.Pop);

                if (targetType == typeof(bool) 
                    || targetType == typeof(byte)
                    || targetType == typeof(char)
                    || targetType == typeof(short)
                    || targetType == typeof(int)
                    || targetType == typeof(sbyte)
                    || targetType == typeof(ushort)
                    || targetType == typeof(uint))
                {
                    generator.Emit(OpCodes.Ldc_I4_0);
                }
                else if (targetType == typeof(long) || targetType == typeof(ulong))
                {
                    generator.Emit(OpCodes.Ldc_I8, (long)0);
                }
                else if (targetType == typeof(float))
                {
                    generator.Emit(OpCodes.Ldc_R4, (float)0);
                }
                else if (targetType == typeof(double))
                {
                    generator.Emit(OpCodes.Ldc_R8, (double)0);
                }
                else
                {
                    LocalBuilder defaultValueLocal = generator.DeclareLocal(targetType);
                    generator.Emit(OpCodes.Ldloca, defaultValueLocal);
                    generator.Emit(OpCodes.Initobj, targetType);
                    generator.Emit(OpCodes.Ldloc, defaultValueLocal);
                }
                generator.Emit(OpCodes.Br, continueLabel);

                // If the value is not null, unbox it.
                generator.MarkLabel(nonNullLabel);
                generator.Emit(OpCodes.Unbox_Any, targetType);

                generator.MarkLabel(continueLabel);
            }
            else
            {
                generator.Emit(OpCodes.Castclass, targetType);
            }
        }

        private static DynamicMethod GetDynamicMethod(MethodInfo method)
        {
            Debug.Assert(!method.IsGenericMethodDefinition, "Cannot create DynamicMethods for generic methods.");

            // We'll return null for void methods.
            Type returnType = method.ReturnType;
            bool isVoid = (returnType == typeof(void));

            Type[] parameterTypes;
            if (method.IsStatic)
            {
                parameterTypes = new Type[] { typeof(object[]) };
            }
            else
            {
                parameterTypes = new Type[] { typeof(DomainService), typeof(object[]) };
            }

            DynamicMethod proxyMethod = new DynamicMethod(method.Name, typeof(object), parameterTypes, /* restrictedSkipVisibility */ !method.IsPublic);
            ILGenerator generator = proxyMethod.GetILGenerator();

            // Cast the target object to its actual type.
            if (!method.IsStatic)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Castclass, method.DeclaringType);
            }

            OpCode parameterReference = method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1;

            ParameterInfo[] parameters = method.GetParameters();
            int conceptualParameterLength = parameters.Length;
            LocalBuilder outCountLocal = null;
            if (conceptualParameterLength > 0)
            {
                // Check if the last parameter is an out parameter used for count.
                if (parameters[conceptualParameterLength - 1].IsOut)
                {
                    conceptualParameterLength--;
                    outCountLocal = generator.DeclareLocal(typeof(int));
                }

                // Push the parameters on the stack.
                for (int i = 0; i < conceptualParameterLength; i++)
                {
                    generator.Emit(parameterReference);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Ldelem_Ref);
                    EmitFromObjectConversion(generator, parameters[i].ParameterType);
                }

                // Load an address on the stack that points to a location 
                // where the count value should be stored.
                if (outCountLocal != null)
                {
                    generator.Emit(OpCodes.Ldloca, outCountLocal);
                }
            }

            // Invoke the method.
            if (method.IsVirtual)
            {
                generator.Emit(OpCodes.Callvirt, method);
            }
            else
            {
                generator.Emit(OpCodes.Call, method);
            }

            // Store the out parameter in the list of parameters.
            if (outCountLocal != null)
            {
                generator.Emit(parameterReference);
                generator.Emit(OpCodes.Ldc_I4, conceptualParameterLength);
                generator.Emit(OpCodes.Ldloc, outCountLocal);
                EmitToObjectConversion(generator, typeof(int));
                generator.Emit(OpCodes.Stelem_Ref);
            }

            // Convert the return value to an object.
            if (isVoid)
            {
                generator.Emit(OpCodes.Ldnull);
            }
            else if (returnType.IsValueType)
            {
                EmitToObjectConversion(generator, returnType);
            }

            generator.Emit(OpCodes.Ret);

            return proxyMethod;
        }
    }
}
