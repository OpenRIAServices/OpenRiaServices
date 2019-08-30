// This should only be defined for local diagnostics.
//#define EMIT_DYNAMIC_ASSEMBLY

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpenRiaServices.DomainServices.Server;
using ReflectionAssemblyBuilder = System.Reflection.Emit.AssemblyBuilder;
using ReflectionModuleBuilder = System.Reflection.Emit.ModuleBuilder;

namespace OpenRiaServices.DomainServices.Hosting.Local
{
    /// <summary>
    /// Used to generate <see cref="DomainService"/> proxies.
    /// </summary>
    internal static class DomainServiceProxyGenerator
    {
        /// <summary>
        /// Dynamically generated assembly name.
        /// </summary>
        public const string ProxyAssemblyName = "OpenRiaServices.DomainServices.Hosting.Local.{DynamicProxies}";

        /// <summary>
        /// Type suffix used when defining dynamic proxy types.
        /// </summary>
        private const string ProxyTypeSuffix = "{Proxy}";

        /// <summary>
        /// Used to construct a dynamic assembly containing dynamic proxy types.
        /// </summary>
        private static ReflectionAssemblyBuilder assemblyBuilder;

        /// <summary>
        /// Used to construct a dynamic module containing dynamic proxy types.
        /// </summary>
        private static ReflectionModuleBuilder moduleBuilder;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static DomainServiceProxyGenerator()
        {
#if EMIT_DYNAMIC_ASSEMBLY
            // Initializes the AssemblyBuilder and ModuleBuilder for *local* debugging purposes. 
            // PDB information will be emitted into the module and the dynamic assembly will be initialized with
            // save-to-disk access permissions.
            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ProxyAssemblyName), AssemblyBuilderAccess.RunAndSave);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(ProxyAssemblyName, ProxyAssemblyName + ".dll", true);
#else
            // Initializes the AssemblyBuilder and ModuleBuilder for debug or release mode.  The assembly builder
            // will only have run access permission and no PDB information will be emitted.
            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(ProxyAssemblyName), AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(ProxyAssemblyName);
#endif
        }

        /// <summary>
        /// Generates a <see cref="DomainService"/> proxy type.
        /// </summary>
        /// <param name="domainServiceContract">The <see cref="DomainService"/> contract type to implement.</param>
        /// <param name="domainService">The <see cref="DomainService"/> type to generate a proxy for.</param>
        /// <returns>Returns a <see cref="DomainService"/> proxy type.</returns>
        public static Type Generate(Type domainServiceContract, Type domainService)
        {
            if (domainServiceContract == null)
            {
                throw new ArgumentNullException(nameof(domainServiceContract));
            }

            if (domainService == null)
            {
                throw new ArgumentNullException(nameof(domainService));
            }

            // Verify 'domainServiceContract' is actually a public interface.
            if (!domainServiceContract.IsInterface || (!domainServiceContract.IsPublic && !domainServiceContract.IsNestedPublic))
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceProxyGenerator_ExpectedPublicType, domainServiceContract);
                throw new ArgumentException(message, nameof(domainServiceContract));
            }

            // Verify 'domainService' is actually a public type.
            if (!domainService.IsPublic && !domainService.IsNestedPublic)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceProxyGenerator_ExpectedPublicType, domainService);
                throw new ArgumentException(message, nameof(domainService));
            }

            // Create a new context
            GenerationContext context = new GenerationContext(domainServiceContract, domainService);

            // Define the proxy type
            DefineProxyType(context);

            // Implement required internal state fields
            ImplementInternalStateFields(context);

            // Implement required internal static fields
            ImplementInternalStaticFields(context);

            // Implement IDisposable
            ImplementIDisposable(context);

            // Implement domainServiceContract (ex: expected to be DomainService operations such as 'GetCustomers(...)')
            GenerateOperationMethods(context);

            // If you build it, They will come.
            Type proxyType = context.CreateType();

#if EMIT_DYNAMIC_ASSEMBLY
            // Save the assembly to disk
            SaveAssembly();
#endif

            return proxyType;
        }

        /// <summary>
        /// Defines a proxy type <see cref="TypeBuilder"/>.
        /// </summary>
        /// <param name="context">The generation context.</param>
        private static void DefineProxyType(GenerationContext context)
        {
            TypeBuilder typeBuilder;

            // Emit:
            //
            //     public sealed class $(domainServiceContract.Name){Proxy} 
            //         : $(domainServiceContract), IDisposable
            //     {
            typeBuilder = moduleBuilder.DefineType(context.ContractType.Name + ProxyTypeSuffix, TypeAttributes.Public | TypeAttributes.Sealed);
            typeBuilder.AddInterfaceImplementation(context.ContractType);
            typeBuilder.AddInterfaceImplementation(typeof(IDisposable));

            context.TypeBuilder = typeBuilder;
        }

        /// <summary>
        /// Implements required internal fields on the provided proxy type builder.
        /// </summary>
        /// <param name="context">The generation context.</param>
        private static void ImplementInternalStateFields(GenerationContext context)
        {
            TypeBuilder typeBuilder = context.TypeBuilder;

            // *** BEGIN -> List<DomainService> DomainServiceInstances { get; }

            // Emit:
            // 
            //     private List<DomainService> _domainServiceInstances;
            FieldBuilder instancesField = typeBuilder.DefineField("_domainServiceInstances", typeof(List<DomainService>), FieldAttributes.Private);

            // Emit:
            //
            //      IList<DomainService> DomainServiceInstances;  (property definition)
            //
            //      IList<DomainService> get_DomainServiceInstances();  (property getter)
            PropertyBuilder instancesProperty = typeBuilder.DefineProperty("DomainServiceInstances", PropertyAttributes.None, typeof(IList<DomainService>), Type.EmptyTypes);
            MethodBuilder instancesGetter = typeBuilder.DefineMethod("get_DomainServiceInstances", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(IList<DomainService>), Type.EmptyTypes);
            ILGenerator instancesGetterIL = instancesGetter.GetILGenerator();

            // Emit:
            //
            //      public IList<DomainService> get_DomainServiceInstances()
            //      {
            //          if (this._domainServiceInstances != null)
            //          {
            //              goto instanceReturnValue;
            //          }
            //
            //          this._domainServiceInstances = new List<DomainService>();
            //          
            //          instanceReturnValue:
            //          return this._domainServiceInstances;
            //      }

            Label instanceReturnValue = instancesGetterIL.DefineLabel();

            // Emit:
            //
            //      if (this._domainServiceInstances != null)
            //      {
            //          goto instanceReturnValue;
            //      }
            instancesGetterIL.Emit(OpCodes.Ldarg_0);
            instancesGetterIL.Emit(OpCodes.Ldfld, instancesField);
            instancesGetterIL.Emit(OpCodes.Brtrue, instanceReturnValue);

            // Emit:
            //
            //      this._domainServiceInstances = new List<DomainService>();
            instancesGetterIL.Emit(OpCodes.Ldarg_0);
            instancesGetterIL.Emit(OpCodes.Newobj, typeof(List<DomainService>).GetConstructor(Type.EmptyTypes));
            instancesGetterIL.Emit(OpCodes.Stfld, instancesField);

            // Emit:
            //
            //      instanceReturnValue:
            //      return this._domainServiceInstances;
            instancesGetterIL.MarkLabel(instanceReturnValue);
            instancesGetterIL.Emit(OpCodes.Ldarg_0);
            instancesGetterIL.Emit(OpCodes.Ldfld, instancesField);
            instancesGetterIL.Emit(OpCodes.Ret);

            // Associate the getter
            instancesProperty.SetGetMethod(instancesGetter);

            // *** END -> IList<DomainService> DomainServiceInstances { get; }

            // *** BEGIN DomainServiceContext Context { get; }

            // Emit:
            // 
            //     private DomainServiceContext _domainServiceContext;
            FieldBuilder contextField = typeBuilder.DefineField("_context", typeof(DomainServiceContext), FieldAttributes.Private);

            // Emit:
            //
            //     public DomainServiceContext Context;  (property definition)
            PropertyBuilder contextProperty = typeBuilder.DefineProperty("Context", PropertyAttributes.None, typeof(DomainServiceContext), Type.EmptyTypes);
            MethodBuilder contextGetter = typeBuilder.DefineMethod("get_Context", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(DomainServiceContext), Type.EmptyTypes);

            // Emit:
            //
            //     public DomainServiceContext get_DomainServiceContext // property 'DomainServiceContext' getter
            //     {
            //         return this._domainServiceContext;
            //     }
            ILGenerator contextGetterIL = contextGetter.GetILGenerator();
            contextGetterIL.Emit(OpCodes.Ldarg_0);
            contextGetterIL.Emit(OpCodes.Ldfld, contextField);
            contextGetterIL.Emit(OpCodes.Ret);

            contextProperty.SetGetMethod(contextGetter);

            // *** END -> DomainServiceContext Context { get; }

            // *** BEGIN -> Type DomainServiceType { get; }

            // Emit:
            //
            //      private Type _domainServiceType;
            //
            //      public Type DomainServiceType;  (property definition)
            //
            //      public Type get_DomainServiceType();  (property getter)
            FieldBuilder typeField = typeBuilder.DefineField("_domainServiceType", typeof(Type), FieldAttributes.Private);
            PropertyBuilder typeProperty = typeBuilder.DefineProperty("DomainServiceType", PropertyAttributes.None, typeof(Type), Type.EmptyTypes);
            MethodBuilder typeGetter = typeBuilder.DefineMethod("get_DomainServiceType", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(Type), Type.EmptyTypes);
            ILGenerator typeGetterIL = typeGetter.GetILGenerator();

            // Emit:
            //
            //      public Type get_DomainServiceType()
            //      {
            //          return this._domainServiceType;
            //      }
            typeGetterIL.Emit(OpCodes.Ldarg_0);
            typeGetterIL.Emit(OpCodes.Ldfld, typeField);
            typeGetterIL.Emit(OpCodes.Ret);

            // Associate the getter
            typeProperty.SetGetMethod(typeGetter);

            // *** END -> Type DomainServiceType { get; }

            // *** BEGIN -> IDictionary<object, object> CurrentOriginalEntityMap { get; }

            // Emit:
            // 
            //     private IDictionary<object, object> _currentOriginalEntityMap;
            FieldBuilder mapField = typeBuilder.DefineField("_currentOriginalEntityMap", typeof(IDictionary<object, object>), FieldAttributes.Private);

            // Emit:
            //
            //      IDictionary<object, object> CurrentOriginalEntityMap;  (property definition)
            //
            //      IDictionary<object, object> get_CurrentOriginalEntityMap();  (property getter)
            PropertyBuilder mapProperty = typeBuilder.DefineProperty("CurrentOriginalEntityMap", PropertyAttributes.None, typeof(IDictionary<object, object>), Type.EmptyTypes);
            MethodBuilder mapGetter = typeBuilder.DefineMethod("get_CurrentOriginalEntityMap", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(IDictionary<object, object>), Type.EmptyTypes);
            ILGenerator mapGetterIL = mapGetter.GetILGenerator();

            // Emit:
            //
            //      public IDictionary<object, object> get_CurrentOriginalEntityMap()
            //      {
            //          if (this._currentOriginalEntityMap != null)
            //          {
            //              goto mapReturnValue;
            //          }
            //
            //          this._currentOriginalEntityMap = new Dictionary<object, object>();
            //          
            //          mapReturnValue:
            //          return this._currentOriginalEntityMap;
            //      }

            Label mapReturnValue = mapGetterIL.DefineLabel();

            // Emit:
            //
            //      if (this._currentOriginalEntityMap != null)
            //      {
            //          goto mapReturnValue;
            //      }
            mapGetterIL.Emit(OpCodes.Ldarg_0);
            mapGetterIL.Emit(OpCodes.Ldfld, mapField);
            mapGetterIL.Emit(OpCodes.Brtrue, mapReturnValue);

            // Emit:
            //
            //      this._currentOriginalEntityMap = new Dictionary<object, object>();
            mapGetterIL.Emit(OpCodes.Ldarg_0);
            mapGetterIL.Emit(OpCodes.Newobj, typeof(Dictionary<object, object>).GetConstructor(Type.EmptyTypes));
            mapGetterIL.Emit(OpCodes.Stfld, mapField);

            // Emit:
            //
            //      return this._currentOriginalEntityMap;
            mapGetterIL.MarkLabel(mapReturnValue);
            mapGetterIL.Emit(OpCodes.Ldarg_0);
            mapGetterIL.Emit(OpCodes.Ldfld, mapField);
            mapGetterIL.Emit(OpCodes.Ret);

            // Associate the getter
            mapProperty.SetGetMethod(mapGetter);

            // *** END -> IDictionary<object, object> CurrentOriginalEntityMap { get; }

            // *** BEGIN -> void Initialize(DomainServiceContext context);

            MethodBuilder initMethod = typeBuilder.DefineMethod("Initialize", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new[] { typeof(Type), typeof(DomainServiceContext) });
            ILGenerator initMethodIL = initMethod.GetILGenerator();

            // Emit:
            // 
            //     public void Initialize(Type domainService, DomainServiceContext context)
            //     {
            //         if (domainService != null)
            //             goto initCheckContext;
            //
            //         throw new ArgumentNullException("domainService");
            //
            //         :initCheckContext
            //         if (context != null)
            //             goto initProceed;
            //
            //         throw new ArgumentNullException("context");
            //         
            //         initProceed:
            //         this._domainServiceContext = context;
            //         this._domainServiceType = domainService;
            //     }

            Label initCheckContext = initMethodIL.DefineLabel();
            Label initProceed = initMethodIL.DefineLabel();

            // Emit:
            // 
            //         if (domainService != null)
            //             goto initCheckContext;
            initMethodIL.Emit(OpCodes.Ldarg_1);
            initMethodIL.Emit(OpCodes.Brtrue, initCheckContext);

            // Emit:
            //
            //         throw new ArgumentNullException("domainService");
            initMethodIL.Emit(OpCodes.Ldstr, "domainService");
            initMethodIL.Emit(OpCodes.Newobj, typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }));
            initMethodIL.Emit(OpCodes.Throw);

            // Emit:
            // 
            //         :initCheckContext
            //         if (context != null)
            //             goto initProceed;
            initMethodIL.MarkLabel(initCheckContext);
            initMethodIL.Emit(OpCodes.Ldarg_2);
            initMethodIL.Emit(OpCodes.Brtrue, initProceed);

            // Emit:
            //
            //         throw new ArgumentNullException("context");
            initMethodIL.Emit(OpCodes.Ldstr, "context");
            initMethodIL.Emit(OpCodes.Newobj, typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }));
            initMethodIL.Emit(OpCodes.Throw);

            // Emit:
            //   
            //         initProceed:
            //         this._domainServiceContext = context;
            //         this._domainServiceType = domainService;
            initMethodIL.MarkLabel(initProceed);
            initMethodIL.Emit(OpCodes.Ldarg_0);
            initMethodIL.Emit(OpCodes.Ldarg_1);
            initMethodIL.Emit(OpCodes.Stfld, typeField);
            initMethodIL.Emit(OpCodes.Ldarg_0);
            initMethodIL.Emit(OpCodes.Ldarg_2);
            initMethodIL.Emit(OpCodes.Stfld, contextField);
            initMethodIL.Emit(OpCodes.Ret);

            // *** END -> void Initialize(Type domainService, DomainServiceContext context);

            // Store references to field builders
            context.DomainServiceInstancesGetter = instancesGetter;
            context.DomainServiceTypeField = typeField;
            context.DomainServiceContextField = contextField;
            context.DomainServiceCurrentOriginalGetter = mapGetter;
        }

        /// <summary>
        /// Implements static fields.
        /// </summary>
        /// <param name="context">The <see cref="GenerationContext"/> context.</param>
        private static void ImplementInternalStaticFields(GenerationContext context)
        {
            // private static Delegate queryDelegate;
            context.QueryDelegateField = context.TypeBuilder.DefineField("queryDelegate", typeof(Delegate), FieldAttributes.Public | FieldAttributes.Static);

            // private static Delegate invokeDelegate;
            context.InvokeDelegateField = context.TypeBuilder.DefineField("invokeDelegate", typeof(Delegate), FieldAttributes.Public | FieldAttributes.Static);

            // private static Delegate submitDelegate;
            context.SubmitDelegateField = context.TypeBuilder.DefineField("submitDelegate", typeof(Delegate), FieldAttributes.Public | FieldAttributes.Static);
        }

        /// <summary>
        /// Implements IDisposable on the provided proxy type builder.
        /// </summary>
        /// <remarks>
        /// When Dispose() is called, the disposal method will iterate through the '_domainServiceInstances'
        /// values and call Dispose() on each instance found.
        /// </remarks>
        /// <param name="context">The generation context.</param>
        private static void ImplementIDisposable(GenerationContext context)
        {
            TypeBuilder typeBuilder = context.TypeBuilder;

            MethodBuilder disposeMethod = typeBuilder.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);
            ILGenerator disposeIL = disposeMethod.GetILGenerator();

            // Emit:
            // 
            //     public void Dispose()
            //     {
            //         if (this._domainServiceInstances != null)
            //         {
            //             for (int i = 0; i < this._domainServiceInstances.Count; ++i)
            //                 this._domainServiceInstances[i].Dispose();
            //
            //             this._domainServiceInstances.Clear();
            //         }
            //     }

            Label loopStart = disposeIL.DefineLabel();
            Label loopCheck = disposeIL.DefineLabel();
            Label disposeExit = disposeIL.DefineLabel();
            LocalBuilder disposeCounter = disposeIL.DeclareLocal(typeof(int));

            // Emit:
            //
            //         if (this._domainServiceInstances == null)
            //             goto disposeExit;
            disposeIL.Emit(OpCodes.Ldarg_0);
            disposeIL.Emit(OpCodes.Callvirt, context.DomainServiceInstancesGetter);
            disposeIL.Emit(OpCodes.Brfalse, disposeExit);

            // Emit:
            //
            //         for (int i = 0; i < this._domainServiceInstances.Count; ++i)
            //             this._domainServiceInstances[i].Dispose();
            disposeIL.Emit(OpCodes.Ldc_I4_0);
            disposeIL.Emit(OpCodes.Stloc, disposeCounter);
            disposeIL.Emit(OpCodes.Br_S, loopCheck);

            disposeIL.MarkLabel(loopStart);
            disposeIL.Emit(OpCodes.Ldarg_0);
            disposeIL.Emit(OpCodes.Callvirt, context.DomainServiceInstancesGetter);
            disposeIL.Emit(OpCodes.Ldloc, disposeCounter);
            disposeIL.Emit(OpCodes.Callvirt, typeof(IList<DomainService>).GetProperty("Item").GetGetMethod());
            disposeIL.Emit(OpCodes.Callvirt, typeof(IDisposable).GetMethod("Dispose"));

            disposeIL.Emit(OpCodes.Ldloc, disposeCounter);
            disposeIL.Emit(OpCodes.Ldc_I4_1);
            disposeIL.Emit(OpCodes.Add);
            disposeIL.Emit(OpCodes.Stloc, disposeCounter);

            disposeIL.MarkLabel(loopCheck);
            disposeIL.Emit(OpCodes.Ldloc, disposeCounter);
            disposeIL.Emit(OpCodes.Ldarg_0);
            disposeIL.Emit(OpCodes.Callvirt, context.DomainServiceInstancesGetter);
            disposeIL.Emit(OpCodes.Callvirt, typeof(ICollection<DomainService>).GetProperty("Count").GetGetMethod());
            disposeIL.Emit(OpCodes.Clt);
            disposeIL.Emit(OpCodes.Brtrue_S, loopStart);

            //         this._domainServiceInstances.Clear();
            disposeIL.Emit(OpCodes.Nop);
            disposeIL.Emit(OpCodes.Ldarg_0);
            disposeIL.Emit(OpCodes.Callvirt, context.DomainServiceInstancesGetter);
            disposeIL.Emit(OpCodes.Callvirt, typeof(ICollection<DomainService>).GetMethod("Clear"));

            // Emit:
            //
            //         disposeExit:
            //         return;
            //     }
            disposeIL.MarkLabel(disposeExit);
            disposeIL.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Defines overloads for <see cref="DomainService"/> operations.
        /// </summary>
        /// <param name="context">The generation context.</param>
        private static void GenerateOperationMethods(GenerationContext context)
        {
            Type domainServiceContract = context.ContractType;
            Type domainService = context.DomainServiceType;

            DomainServiceDescription domainServiceDescription = DomainServiceDescription.GetDescription(domainService);

            // Keep track of methods on the interface that we don't understand?
            IEnumerable<string> contractMethodNames = domainServiceContract.GetMethods().Select(m => m.Name);
            IEnumerable<string> methodsNotMatched = contractMethodNames.Except(domainServiceDescription.DomainOperationEntries.Select(d => d.Name));
            if (methodsNotMatched.Any())
            {
                string errorMessage = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_MethodCountMismatch,
                    domainServiceContract,
                    domainService,
                    string.Join(", ", methodsNotMatched.ToArray()));
                throw new InvalidOperationException(errorMessage);
            }

            // Iterate through all operations
            foreach (DomainOperationEntry domainOperationEntry in domainServiceDescription.DomainOperationEntries)
            {
                MethodInfo[] methods = domainServiceContract.GetMethods().Where(m => m.Name == domainOperationEntry.Name).ToArray();

                if (methods.Length == 0)
                {
                    // Skip the operation if it does not exist on the interface.
                    // This is not considered an error condition.
                    continue;
                }
                else if (methods.Length > 1)
                {
                    // We have multiple matches!  While this may be valid for CLR types, we don't support 
                    // DomainService operation overrides.
                    string errorMessage = string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.DomainServiceProxyGenerator_OverridesNotSupported,
                        domainServiceContract,
                        domainOperationEntry.Name);

                    throw new InvalidOperationException(errorMessage);
                }

                MethodInfo method = methods[0];

                // Validate our duck-typing match.
                ValidateMethodMatch(domainServiceContract, domainOperationEntry, method);

                switch (domainOperationEntry.Operation)
                {
                    // Generate CUD and Custom methods
                    case DomainOperation.Delete:
                    case DomainOperation.Insert:
                    case DomainOperation.Update:
                    case DomainOperation.Custom:
                        GenerateSubmitMethod(context, method, domainOperationEntry.Operation);
                        break;

                    // Generate Query methods
                    case DomainOperation.Query:
                        GenerateQueryOperation(context, method);
                        break;

                    // Generate InvokeOperation methods
                    case DomainOperation.Invoke:
                        GenerateInvokeOperation(context, method);
                        break;

                    // Operation not supported
                    default:
                        string errorMessage =
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resource.DomainServiceProxyGenerator_OperationNotSupported,
                                domainOperationEntry.Operation);

                        throw new NotSupportedException(errorMessage);
                }
            }
        }

        /// <summary>
        /// Validates that a given <see cref="DomainOperationEntry"/> has a valid <see cref="MethodInfo"/> match.
        /// </summary>
        /// <param name="domainServiceContract">The <see cref="DomainService"/> proxy contract.</param>
        /// <param name="domainOperationEntry">The <see cref="DomainOperationEntry"/> that is looking for a method match.</param>
        /// <param name="methodInfo">The candidate method match.</param>
        private static void ValidateMethodMatch(Type domainServiceContract, DomainOperationEntry domainOperationEntry, MethodInfo methodInfo)
        {
            var operationParameters = domainOperationEntry.Parameters.ToDictionary(p => p.Name, p => p.ParameterType);
            var methodParameters = methodInfo.GetParameters().ToDictionary(p => p.Name, p => p.ParameterType);

            // Verify that our return type and parameter names and types match
            if (methodInfo.ReturnType != domainOperationEntry.ReturnType ||
                operationParameters.Count != methodParameters.Count ||
                operationParameters.Except(methodParameters).Any())
            {
                string errorMessage = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_OperationMismatch,
                    domainServiceContract,
                    domainOperationEntry.Name);

                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Defines a proxy Query operation.
        /// </summary>
        /// <param name="context">The generation context.</param>
        /// <param name="method">The contract method.</param>
        private static void GenerateQueryOperation(GenerationContext context, MethodInfo method)
        {
            /* Here's the C# equivalent of what we want to generate:
             * 
             *     // If the helper method is non-void returning invoke operation or IEnumerable returning query
             *     [$(MethodAttributes)]
             *     public override $(ReturnType) $(MethodName)([$(ParameterAttributes)] $(Parameters) )
             *     {
             *         // Query delegate signature: IEnumerable query(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, string queryName, object[] parameters)
             *         $(ReturnType) result = ($(ReturnType))$(queryDelegate)(this._domainServiceType, this._domainServiceContext, this._domainServiceInstances, "$(MethodName)", new object[] { $(Parameters) });
             *         return result;
             *     }
             * 
             *     // If the helper method is singleton returning query
             *     [$(MethodAttributes)]
             *     public override $(ReturnType) $(MethodName)([$(ParameterAttributes)] $(Parameters) )
             *     {
             *         // Query delegate signature: IEnumerable query(Type domainService, DomainServiceContext context, IList<DomainService> domainServiceInstances, string queryName, object[] parameters)
             *         $(ReturnType)[] result = ($(ReturnType))$(queryDelegate)(this._domainServiceType, this._domainServiceContext, this._domainServiceInstances, "$(MethodName)", new object[] { $(Parameters) });
             *         
             *         if (result != null)
             *         {
             *             return result[0];
             *         }
             *          
             *         return null;
             *     }
             * 
             */

            ParameterInfo[] parameters = method.GetParameters();
            Type[] parameterTypes = parameters.Select(p => p.IsOut || p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType).ToArray();
            MethodBuilder queryMethod =
                context.TypeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    method.ReturnType,
                    parameters.Select(pi => pi.ParameterType).ToArray());

            InitializeMethod(queryMethod, method);

            ILGenerator methodBody = queryMethod.GetILGenerator();
            bool isSingletonQuery = !typeof(IEnumerable).IsAssignableFrom(method.ReturnType);

            Type resultType;
            if (isSingletonQuery)
            {
                resultType = method.ReturnType.MakeArrayType();
            }
            else
            {
                resultType = method.ReturnType;
            }

            // Emit:
            //
            //     $(ReturnType)[] result;
            //     object[] parameterValues;
            LocalBuilder resultLocal = methodBody.DeclareLocal(resultType);
            LocalBuilder paramValuesLocal = methodBody.DeclareLocal(typeof(object[]));

            // Emit:
            //
            //     // For all 'out' parameters
            //     <parameter> = default();
            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo parameter = parameters[i];

                if (parameter.IsOut && !parameter.IsIn)
                {
                    methodBody.Emit(OpCodes.Ldarg_S, i + 1); // <parameter>
                    methodBody.Emit(OpCodes.Initobj, parameterTypes[i]); // <parameter> = default();
                }
            }

            // Emit:
            //
            //     parameterValues = new object[ $(Parameters).Length ];
            methodBody.Emit(OpCodes.Ldc_I4_S, parameters.Length);
            methodBody.Emit(OpCodes.Newarr, typeof(object));
            methodBody.Emit(OpCodes.Stloc, paramValuesLocal);

            // Emit:
            //
            //     // Repeat for all parameter values...
            //     parameterValues[n] = $(Parameters)[n];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];

                methodBody.Emit(OpCodes.Ldloc, paramValuesLocal); // parameterValues
                methodBody.Emit(OpCodes.Ldc_I4_S, i);
                methodBody.Emit(OpCodes.Ldarg_S, i + 1); // parameterValues[n]

                // out/ref types?  push on stack
                if (param.IsOut || param.ParameterType.IsByRef)
                {
                    methodBody.Emit(OpCodes.Ldobj, parameterTypes[i]);
                }

                // value or generics?  box it up
                if (parameterTypes[i].IsValueType || parameterTypes[i].IsGenericParameter)
                {
                    methodBody.Emit(OpCodes.Box, parameterTypes[i]);
                }

                methodBody.Emit(OpCodes.Stelem_Ref); // parameterValues[n] = arg[n]
            }

            // Emit:
            // 
            //     object[] objectParams = new object[5];
            LocalBuilder objectParams = methodBody.DeclareLocal(typeof(object[]));
            methodBody.Emit(OpCodes.Ldc_I4_5);
            methodBody.Emit(OpCodes.Newarr, typeof(object));
            methodBody.Emit(OpCodes.Stloc_S, objectParams);

            // Emit:
            // 
            //     objectParams[0] = this._domainServiceType;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 0);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Ldfld, context.DomainServiceTypeField);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[1] = this._domainServiceContext;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 1);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Ldfld, context.DomainServiceContextField);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[2] = this.DomainServiceInstances;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 2);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Callvirt, context.DomainServiceInstancesGetter);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[3] = queryName;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 3);
            methodBody.Emit(OpCodes.Ldstr, method.Name);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[4] = queryParameters;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 4);
            methodBody.Emit(OpCodes.Ldloc_S, paramValuesLocal);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     queryDelegate.DynamicInvoke(objectParams);  // leaving result on stack
            EmitDelegateInvoke(methodBody, objectParams, context.QueryDelegateField);

            // Handle return type
            if (method.ReturnType.IsValueType || method.ReturnType.IsGenericParameter)
            {
                methodBody.Emit(OpCodes.Unbox_Any, resultType);
            }
            else
            {
                methodBody.Emit(OpCodes.Castclass, resultType);
            }

            // Save return type (unboxed or cast by now)
            methodBody.Emit(OpCodes.Stloc_S, resultLocal);

            // Check if we need to unwrap any return
            if (isSingletonQuery)
            {
                // Emit:
                //
                //     if (result != null)
                //     {
                //         return result[0];
                //     }
                Label returnArray0 = methodBody.DefineLabel();
                methodBody.Emit(OpCodes.Ldloc, resultLocal);
                methodBody.Emit(OpCodes.Brfalse, returnArray0);
                methodBody.Emit(OpCodes.Ldloc, resultLocal);
                methodBody.Emit(OpCodes.Ldc_I4_0);
                methodBody.Emit(OpCodes.Ldelem_Ref);
                methodBody.Emit(OpCodes.Ret);

                methodBody.MarkLabel(returnArray0);

                // Emit:
                //
                //     return null;
                methodBody.Emit(OpCodes.Ldnull, resultLocal);
            }
            else
            {
                // Emit:
                //
                //     return result;
                methodBody.Emit(OpCodes.Ldloc, resultLocal);
            }

            methodBody.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Defines a proxy Query operation.
        /// </summary>
        /// <param name="context">The generation context.</param>
        /// <param name="method">The contract method.</param>
        private static void GenerateInvokeOperation(GenerationContext context, MethodInfo method)
        {
            /* Here's the C# equivalent of what we want to generate:
             * 
             *     // If the helper method is non-void returning invoke operation or ienumerable returning query
             *     [$(MethodAttributes)]
             *     public override $(ReturnType) $(MethodName)([$(ParameterAttributes)] $(Parameters) )
             *     {
             *         $(ReturnType) result = ($(ReturnType))$(helperMethod)(this, "$(MethodName)", new object[] { $(Parameters) });
             *         return result;
             *     }
             * 
             *     // If the helper method is void returning invoke operation
             *     [$(MethodAttributes)]
             *     public override void $(MethodName)([$(ParameterAttributes)] $(Parameters) )
             *     {
             *         ($(ReturnType))$(helperMethod)(this, "$(MethodName)", new object[] { $(Parameters) });
             *     }
             * 
             */

            ParameterInfo[] parameters = method.GetParameters();
            Type[] parameterTypes = parameters.Select(p => p.IsOut || p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType).ToArray();
            MethodBuilder queryMethod =
                context.TypeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    method.ReturnType,
                    parameters.Select(pi => pi.ParameterType).ToArray());

            InitializeMethod(queryMethod, method);

            bool voidReturningMethod = method.ReturnType == typeof(void);
            Type resultType = voidReturningMethod ? typeof(object) : method.ReturnType;

            ILGenerator methodBody = queryMethod.GetILGenerator();

            // Emit:
            //
            //     $(ReturnType)[] result;
            //     object[] parameterValues;
            LocalBuilder resultLocal = methodBody.DeclareLocal(resultType);
            LocalBuilder paramValuesLocal = methodBody.DeclareLocal(typeof(object[]));

            // Emit:
            //
            //     // For all 'out' parameters
            //     <parameter> = default();
            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo parameter = parameters[i];

                if (parameter.IsOut && !parameter.IsIn)
                {
                    methodBody.Emit(OpCodes.Ldarg_S, i + 1); // <parameter>
                    methodBody.Emit(OpCodes.Initobj, parameterTypes[i]); // <parameter> = default();
                }
            }

            // Emit:
            //
            //     parameterValues = new object[ $(Parameters).Length ];
            methodBody.Emit(OpCodes.Ldc_I4_S, parameters.Length);
            methodBody.Emit(OpCodes.Newarr, typeof(object));
            methodBody.Emit(OpCodes.Stloc, paramValuesLocal);

            // Emit:
            //
            //     // Repeat for all parameter values...
            //     parameterValues[n] = $(Parameters)[n];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];

                methodBody.Emit(OpCodes.Ldloc, paramValuesLocal); // parameterValues
                methodBody.Emit(OpCodes.Ldc_I4_S, i);
                methodBody.Emit(OpCodes.Ldarg_S, i + 1); // parameterValues[n]

                // out/ref types?  push on stack
                if (param.IsOut || param.ParameterType.IsByRef)
                {
                    methodBody.Emit(OpCodes.Ldobj, parameterTypes[i]);
                }

                // value or generics?  box it up
                if (parameterTypes[i].IsValueType || parameterTypes[i].IsGenericParameter)
                {
                    methodBody.Emit(OpCodes.Box, parameterTypes[i]);
                }

                methodBody.Emit(OpCodes.Stelem_Ref); // parameterValues[n] = arg[n]
            }

            // Emit:
            // 
            //     object[] objectParams = new object[5];
            LocalBuilder objectParams = methodBody.DeclareLocal(typeof(object[]));
            methodBody.Emit(OpCodes.Ldc_I4_5);
            methodBody.Emit(OpCodes.Newarr, typeof(object));
            methodBody.Emit(OpCodes.Stloc_S, objectParams);

            // Emit:
            // 
            //     objectParams[0] = this._domainServiceType;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 0);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Ldfld, context.DomainServiceTypeField);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[1] = this._domainServiceContext;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 1);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Ldfld, context.DomainServiceContextField);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[2] = this.DomainServiceInstances;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 2);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Callvirt, context.DomainServiceInstancesGetter);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[3] = invokeName;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 3);
            methodBody.Emit(OpCodes.Ldstr, method.Name);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[4] = invokeParams;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 4);
            methodBody.Emit(OpCodes.Ldloc_S, paramValuesLocal);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     invokeDelegate.DynamicInvoke(objectParams);  // leaving result on stack
            EmitDelegateInvoke(methodBody, objectParams, context.InvokeDelegateField);

            if (!voidReturningMethod && (method.ReturnType.IsValueType || method.ReturnType.IsGenericParameter))
            {
                methodBody.Emit(OpCodes.Unbox_Any, resultType);
            }
            else
            {
                methodBody.Emit(OpCodes.Castclass, resultType);
            }

            methodBody.Emit(OpCodes.Stloc_S, resultLocal);

            if (!voidReturningMethod)
            {
                // Emit:
                //
                //     return result;
                methodBody.Emit(OpCodes.Ldloc, resultLocal);
            }

            methodBody.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Defines a proxy Submit operation.
        /// </summary>
        /// <param name="context">The generation context.</param>
        /// <param name="method">The contract method.</param>
        /// <param name="domainOperation">The type of <see cref="DomainOperation"/>.</param>
        private static void GenerateSubmitMethod(GenerationContext context, MethodInfo method, DomainOperation domainOperation)
        {
            /* Here's the C# equivalent of what we want to generate:
            * 
            *     public void $(MethodName)($(EntityParameter), (... [$(ParameterAttributes)] $(Parameters)))
            *     {
            *         DomainServiceProxyHelpers.Submit(this, entity, operationName, parameters, domainOperation)
            *     }
            * 
            */

            ParameterInfo[] parameters = method.GetParameters();
            Type[] parameterTypes = parameters.Select(p => p.IsOut || p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType).ToArray();
            MethodBuilder queryMethod =
                context.TypeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    method.ReturnType,
                    parameters.Select(pi => pi.ParameterType).ToArray());

            InitializeMethod(queryMethod, method);

            ILGenerator methodBody = queryMethod.GetILGenerator();

            // Emit:
            //
            //     object[] parameterValues;
            LocalBuilder paramValuesLocal = methodBody.DeclareLocal(typeof(object[]));

            // Emit:
            //
            //     // For all 'out' parameters
            //     <parameter> = default();
            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo parameter = parameters[i];

                if (parameter.IsOut && !parameter.IsIn)
                {
                    methodBody.Emit(OpCodes.Ldarg_S, i + 1); // <parameter>
                    methodBody.Emit(OpCodes.Initobj, parameterTypes[i]); // <parameter> = default();
                }
            }

            // Do we need to pass parameters along?  For a submit method, we can 
            // assume the first parameter is the entity so we check to see if
            // additional parameters exist beyond that.
            if ((parameters.Length - 1) < 1)
            {
                // Emit:
                //
                //     parameterValues = null;
                methodBody.Emit(OpCodes.Ldnull);
                methodBody.Emit(OpCodes.Stloc, paramValuesLocal);
            }
            else
            {
                // Emit:
                //
                //     parameterValues = new object[ $(Parameters).Length ];
                methodBody.Emit(OpCodes.Ldc_I4_S, parameters.Length - 1);
                methodBody.Emit(OpCodes.Newarr, typeof(object));
                methodBody.Emit(OpCodes.Stloc, paramValuesLocal);

                // Emit:
                //
                //     // Repeat for all parameter values...
                //     parameterValues[n] = $(Parameters)[n];
                for (int i = 1; i < parameters.Length; i++)
                {
                    ParameterInfo param = parameters[i];

                    methodBody.Emit(OpCodes.Ldloc, paramValuesLocal); // parameterValues
                    methodBody.Emit(OpCodes.Ldc_I4_S, i - 1);
                    methodBody.Emit(OpCodes.Ldarg_S, i + 1); // parameterValues[n]

                    // out/ref types?  push on stack
                    if (param.IsOut || param.ParameterType.IsByRef)
                    {
                        methodBody.Emit(OpCodes.Ldobj, parameterTypes[i]);
                    }

                    // value or generics?  box it up
                    if (parameterTypes[i].IsValueType || parameterTypes[i].IsGenericParameter)
                    {
                        methodBody.Emit(OpCodes.Box, parameterTypes[i]);
                    }

                    methodBody.Emit(OpCodes.Stelem_Ref); // parameterValues[n] = arg[n]
                }
            }

            // Emit:
            // 
            //     object[] objectParams = new object[8];
            LocalBuilder objectParams = methodBody.DeclareLocal(typeof(object[]));
            methodBody.Emit(OpCodes.Ldc_I4_8);
            methodBody.Emit(OpCodes.Newarr, typeof(object));
            methodBody.Emit(OpCodes.Stloc_S, objectParams);

            // Emit:
            // 
            //     objectParams[0] = this._domainServiceType;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 0);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Ldfld, context.DomainServiceTypeField);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[1] = this._domainServiceContext;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 1);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Ldfld, context.DomainServiceContextField);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[2] = this.DomainServiceInstances;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 2);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Callvirt, context.DomainServiceInstancesGetter);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[3] = this.CurrentOriginalEntityMap;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 3);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Callvirt, context.DomainServiceCurrentOriginalGetter);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[4] = entity;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 4);
            methodBody.Emit(OpCodes.Ldarg_1);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[5] = operationName; // if a custom operation
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 5);
            if (domainOperation == DomainOperation.Custom)
            {
                methodBody.Emit(OpCodes.Ldstr, method.Name);
            }
            else
            {
                methodBody.Emit(OpCodes.Ldnull);
            }
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[6] = parameters;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 6);
            methodBody.Emit(OpCodes.Ldloc_S, paramValuesLocal);
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     objectParams[7] = (int)domainOperation;
            methodBody.Emit(OpCodes.Ldloc_S, objectParams);
            methodBody.Emit(OpCodes.Ldc_I4_S, 7);
            methodBody.Emit(OpCodes.Ldc_I4_S, (int)domainOperation);
            methodBody.Emit(OpCodes.Box, typeof(int));
            methodBody.Emit(OpCodes.Stelem_Ref);

            // Emit:
            // 
            //     submitDelegate.DynamicInvoke(objectParams);  // leaving result on stack
            EmitDelegateInvoke(methodBody, objectParams, context.SubmitDelegateField);

            methodBody.Emit(OpCodes.Pop); // ignore result, its a void returning method
            methodBody.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits instructions to invoke a delegate, used by Submit|Invoke|Query generators.
        /// </summary>
        /// <param name="methodBody">The method body to emit IL into.</param>
        /// <param name="parameters">The delegate parameters to pass.</param>
        /// <param name="delegateField">The delegate to invoke.</param>
        private static void EmitDelegateInvoke(ILGenerator methodBody, LocalBuilder parameters, FieldBuilder delegateField)
        {
            methodBody.Emit(OpCodes.Ldsfld, delegateField);
            methodBody.Emit(OpCodes.Ldloc_S, parameters);
            methodBody.Emit(OpCodes.Callvirt, typeof(Delegate).GetMethod("DynamicInvoke", BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        /// Initializes a method.
        /// </summary>
        /// <param name="methodBuilder">The current constructor builder.</param>
        /// <param name="methodInfo">The <see cref="MethodInfo"/> associated with the <paramref name="methodBuilder"/>.</param>
        private static void InitializeMethod(MethodBuilder methodBuilder, MethodInfo methodInfo)
        {
            // Apply method attributes
            foreach (CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(methodInfo))
            {
                CustomAttributeBuilder cab = CreateCustomAttributeBuilder(cad);
                methodBuilder.SetCustomAttribute(cab);
            }

            // Build parameters
            var parameters = methodInfo.GetParameters();
            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo parameterInfo = parameters[i];

                string parameterName = string.IsNullOrEmpty(parameterInfo.Name) ?
                    "param" + i.ToString(CultureInfo.InvariantCulture) :
                    parameterInfo.Name;

                ParameterBuilder paramBuilder =
                    methodBuilder.DefineParameter(
                        i + 1, // 1-based index!  (0 == return type)
                        parameterInfo.Attributes,
                        parameterName);

                // Apply parameter attributes
                foreach (CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(parameterInfo))
                {
                    CustomAttributeBuilder cab = CreateCustomAttributeBuilder(cad);
                    paramBuilder.SetCustomAttribute(cab);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="CustomAttributeBuilder"/> with the provided <see cref="CustomAttributeData"/>.
        /// </summary>
        /// <param name="customAttributeData">The <see cref="CustomAttributeData"/> to build.</param>
        /// <returns>A <see cref="CustomAttributeBuilder"/>.</returns>
        private static CustomAttributeBuilder CreateCustomAttributeBuilder(CustomAttributeData customAttributeData)
        {
            IEnumerable<CustomAttributeNamedArgument> properties = customAttributeData.NamedArguments.Where(na => na.MemberInfo is PropertyInfo);
            IEnumerable<CustomAttributeNamedArgument> fields = customAttributeData.NamedArguments.Where(na => na.MemberInfo is FieldInfo);

            return
                new CustomAttributeBuilder(
                    customAttributeData.Constructor, // ctor ref
                    customAttributeData.ConstructorArguments.Select(ca => ca.Value).ToArray(), // ctor values
                    properties.Select(p => p.MemberInfo).Cast<PropertyInfo>().ToArray(), // property refs
                    properties.Select(p => p.TypedValue.Value).ToArray(), // property values
                    fields.Select(f => f.MemberInfo).Cast<FieldInfo>().ToArray(), // field refs
                    fields.Select(f => f.TypedValue.Value).ToArray()); // field values
        }

#if EMIT_DYNAMIC_ASSEMBLY

        /// <summary>
        /// This method can be used to save the dynamic assembly to disk. Note that the assembly will be saved 
        /// into the current executing directory using a name similar to "OpenRiaServices.DomainServices.Hosting.Local.{DynamicProxies}.dll".
        /// </summary>
        /// <remarks>
        /// Once the dynamic assembly is written to disk, additional modifications (e.g., creating a new proxy type) will
        /// be prohibited.
        /// </remarks>
        [Obsolete("The 'EMIT_DYNAMIC_ASSEMBLY' symbol should only be defined in local builds.", false)] // REVIEW: Is there a better way of ensuring this symbol is not defined?
        private static void SaveAssembly()
        {
            assemblyBuilder.Save(ProxyAssemblyName + ".dll");
        }

#endif

        #region Nested Types

        /// <summary>
        /// Private classed used to ferry contextual information during proxy type generation.
        /// </summary>
        private sealed class GenerationContext
        {
            public GenerationContext(Type contractType, Type domainServiceType)
            {
                this.ContractType = contractType;
                this.DomainServiceType = domainServiceType;
            }

            public Type ContractType
            {
                get;
                private set;
            }

            public Type DomainServiceType
            {
                get;
                private set;
            }

            public TypeBuilder TypeBuilder
            {
                get;
                set;
            }

            public MethodBuilder DomainServiceInstancesGetter
            {
                get;
                set;
            }

            public FieldBuilder DomainServiceContextField
            {
                get;
                set;
            }

            public FieldBuilder DomainServiceTypeField
            {
                get;
                set;
            }

            public MethodBuilder DomainServiceCurrentOriginalGetter
            {
                get;
                set;
            }

            public FieldBuilder QueryDelegateField
            {
                get;
                set;
            }

            public FieldBuilder SubmitDelegateField
            {
                get;
                set;
            }

            public FieldBuilder InvokeDelegateField
            {
                get;
                set;
            }

            public Type CreateType()
            {
                return this.TypeBuilder.CreateType();
            }
        }

        #endregion
    }
}