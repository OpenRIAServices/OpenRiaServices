using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Server
{
    internal static class DynamicMethodUtility
    {
        private static readonly MethodInfo s_serviceContextGetter = typeof(DomainService).GetProperty(nameof(DomainService.ServiceContext)).GetGetMethod();
        private static readonly MethodInfo s_cancellationTokenGetter = typeof(DomainServiceContext).GetProperty(nameof(DomainServiceContext.CancellationToken)).GetGetMethod();
        private static readonly MethodInfo s_getService = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService));
        private static readonly MethodInfo s_typeGetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
        private static readonly MethodInfo s_unwrapVoidTask = typeof(DynamicMethodUtility).GetMethod(nameof(UnwrapVoidTask), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_unwrapVoidValueTask = typeof(DynamicMethodUtility).GetMethod(nameof(UnwrapVoidValueTask), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_unwrapTask = typeof(DynamicMethodUtility).GetMethod(nameof(UnwrapTask), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_unwrapValueTask = typeof(DynamicMethodUtility).GetMethod(nameof(UnwrapValueTask), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly ConstructorInfo s_valueTaskCtorObject = typeof(ValueTask<object>).GetConstructor(new Type[] { typeof(object) });
        private static readonly ConstructorInfo s_valueTaskCtorTask = typeof(ValueTask<object>).GetConstructor(new Type[] { typeof(Task<object>) });

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
        public static Func<DomainService, object[], ValueTask<object>> GetDelegateForMethod(MethodInfo method)
        {
            var dynamicMethod = GetDynamicMethod(method);

            try
            {
                return (Func<DomainService, object[], ValueTask<object>>)dynamicMethod.CreateDelegate(typeof(Func<DomainService, object[], ValueTask<object>>));
            }
            catch (InvalidProgramException ex)
            {
                // Since we set restrictedSkipVisibility to true for improved invoke performance
                // the method will get JITted directy so invalid methods throw here before validation
                // We capture the exception for now and expect that validation will throw another more informative exception
                // - we never expect the method to be invoked in these cases, but we return a function which throw the same exception on invoke instead
                return (domainService, parameters) => throw new InvalidProgramException(ex.Message);
            }
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
            Debug.Assert(!method.IsStatic, "Cannot create DynamicMethods for static methods.");

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

            DynamicMethod proxyMethod = new DynamicMethod(method.Name, typeof(ValueTask<object>), parameterTypes, restrictedSkipVisibility: true);
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
                int paramindex = 0;
                for (int i = 0; i < conceptualParameterLength; i++)
                {
                    var parameter = parameters[i];

                    if (parameter.ParameterType.IsByRef || parameter.ParameterType.IsPointer)
                        throw new InvalidOperationException(string.Format(Resource.InvalidDomainOperationEntry_ParamMustBeByVal, method.Name, parameter.Name));

                    if (parameter.ParameterType == typeof(CancellationToken))
                    {
                        // domainService.ServiceContext.CancellationToken
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.EmitCall(OpCodes.Call, s_serviceContextGetter, null);
                        generator.EmitCall(OpCodes.Call, s_cancellationTokenGetter, null);
                    }
                    else if (parameter.GetCustomAttribute(typeof(InjectParameterAttribute)) != null)
                    {
                        // generate ((IServiceProvider)ServiceContext).GetService( typeof(parameterType) )
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.EmitCall(OpCodes.Call, s_serviceContextGetter, null);
                        // emit type
                        generator.Emit(OpCodes.Ldtoken, parameter.ParameterType);
                        generator.EmitCall(OpCodes.Call, s_typeGetTypeFromHandle, null);

                        // ((IServiceProvider)ServiceContext).GetService( ... )
                        generator.EmitCall(OpCodes.Callvirt, s_getService, null);

                        EmitFromObjectConversion(generator, parameter.ParameterType);
                    }
                    else
                    {
                        generator.Emit(parameterReference);
                        generator.Emit(OpCodes.Ldc_I4, paramindex++);
                        generator.Emit(OpCodes.Ldelem_Ref);
                        EmitFromObjectConversion(generator, parameter.ParameterType);
                    }
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

            // Convert the return value to ValueTask<object>.
            if (isVoid)
            {
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Newobj, s_valueTaskCtorObject);
            }
            else if (returnType.IsValueType)
            {
                if (returnType == typeof(ValueTask))
                {
                    generator.EmitCall(OpCodes.Call, s_unwrapVoidValueTask, null);
                }
                else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    if (returnType != typeof(ValueTask<object>))
                        generator.EmitCall(OpCodes.Call, s_unwrapValueTask.MakeGenericMethod(returnType.GenericTypeArguments), null);
                }
                else
                {
                    EmitToObjectConversion(generator, returnType);
                    generator.Emit(OpCodes.Newobj, s_valueTaskCtorObject);
                }
            }
            else if (returnType == typeof(Task))
            {
                generator.EmitCall(OpCodes.Call, s_unwrapVoidTask, null);
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                if (returnType != typeof(Task<object>))
                    generator.EmitCall(OpCodes.Call, s_unwrapTask.MakeGenericMethod(returnType.GenericTypeArguments), null);
                else
                    generator.Emit(OpCodes.Newobj, s_valueTaskCtorTask);
            }
            else // any reference type, 
            {
                generator.Emit(OpCodes.Newobj, s_valueTaskCtorObject);
            }

            generator.Emit(OpCodes.Ret);

            return proxyMethod;
        }

        private static async ValueTask<object> UnwrapVoidTask(Task t)
        {
            await t.ConfigureAwait(false);
            return null;
        }

        private static async ValueTask<object> UnwrapVoidValueTask(ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            return null;
        }

        private static async ValueTask<object> UnwrapTask<T>(Task<T> task)
        {
            return await task.ConfigureAwait(false);
        }

        private static async ValueTask<object> UnwrapValueTask<T>(ValueTask<T> valueTask)
        {
            return await valueTask.ConfigureAwait(false);
        }
    }
}
