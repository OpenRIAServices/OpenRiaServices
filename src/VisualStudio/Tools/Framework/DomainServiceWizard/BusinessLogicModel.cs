using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Web.Hosting;
using OpenRiaServices.DomainServices.Tools;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    
    /// <summary>
    /// Class that can be used across AppDomain boundaries by
    /// <see cref="BusinessLogicViewModel"/> to retrieve available
    /// contexts and their entities, to choose which ones to use, and
    /// to generate code.
    /// </summary>
    public class BusinessLogicModel : MarshalByRefObject, IRegisteredObject, IDisposable
    {
        private BusinessLogicData _businessLogicData;
        private List<BusinessLogicContext> _contexts;
        private readonly Action<string> _logger;

        /// <summary>
        /// Creates a new uninitialized instance of the <see cref="BusinessLogicModel"/> class.
        /// </summary>
        /// <remarks>Because this class is used cross AppDomain boundaries, this is the
        /// only supported constructor.  Callers must call <see cref="Initialize"/> to
        /// complete the initialization after construction.
        /// </remarks>
        public BusinessLogicModel() : this(s => System.Diagnostics.Debug.WriteLine(s))
        {
        }

        /// <summary>
        /// Creates a new uninitialized instance of the <see cref="BusinessLogicModel"/> class
        /// with a callback to receive error messages.
        /// </summary>
        /// <param name="logger">Callback to invoke to report errors during processing.</param>
        public BusinessLogicModel(Action<string> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this._logger = logger;
        }

        /// <summary>
        /// Finishes initializing this instance.  This method must be called after
        /// instantiating an instance of this class and before using any of its
        /// other methods.
        /// </summary>
        /// <param name="businessLogicData">The <see cref="BusinessLogicData"/> for this model.</param>
        public void Initialize(BusinessLogicData businessLogicData)
        {
            System.Diagnostics.Debug.Assert(businessLogicData != null, "BusinessLogicData cannot be null");
            this._businessLogicData = businessLogicData;

            // Note: the LinqToSqlContext in this AppDomain is not the same
            // one in the caller's AppDomain, so we have to propagate this
            // override across to this version of the type.
            if (businessLogicData.LinqToSqlPath != null)
            {
                LinqToSqlContext.OverrideAssemblyPath(businessLogicData.LinqToSqlPath);
            }
        }

        /// <summary>
        /// Gets the data object used to share state across
        /// the AppDomain boundary with <see cref="BusinessLogicViewModel"/>.
        /// </summary>
        private BusinessLogicData BusinessLogicData
        {
            get
            {
                if (this._businessLogicData == null)
                {
                    throw new InvalidOperationException(Resources.BusinessLogicClass_Not_Initialized);
                }
                return this._businessLogicData;
            }
        }

        /// <summary>
        /// Gets the language to use for code generation.
        /// </summary>
        public string Language
        {
            get
            {
                return this.BusinessLogicData.Language;
            }
        }

        /// <summary>
        /// Gets the data objects for the <see cref="BusinessLogicContext"/> instances
        /// known by the current model.  These data objects are marshalled across
        /// the AppDomain boundary by <see cref="ContextViewModel"/>.
        /// </summary>
        /// <returns>The set of data objects for the <see cref="BusinessLogicContext"/>s.  
        /// This collection will always contain at least one element for the
        /// default empty context.</returns>
        public ContextData[] GetContextDataItems()
        {
            // This is lazily computed and cached.
            if (this._contexts == null)
            {
                List<Assembly> loadedAssemblies = new List<Assembly>();
                this._contexts = new List<BusinessLogicContext>();

                // First load into this AppDomain all the assemblies we have been
                // asked to load.  These 2 lists represent the assemblies of the candidate
                // types as well as all assemblies referenced by the candidate types'
                // assemblies.
                // Note, order is important here. Paths to reference assemblies were collected in the VS
                // app domain and could be static from when they were originally loaded. Assemblies
                // containing the context types were created dynamically and may be more recent. We
                // must load the assemblies containing the context types first since multiple assemblies
                // with the same name may not be loaded.
                this.LoadAssemblies((string[])this.BusinessLogicData.AssemblyPaths, loadedAssemblies);
                this.LoadAssemblies((string[])this.BusinessLogicData.ReferenceAssemblyPaths, loadedAssemblies);

                // Next, find the context types we were asked to use from those loaded assemblies
                List<Type> foundTypes = new List<Type>();
                HashSet<string> contextTypeNames = new HashSet<string>(this.BusinessLogicData.ContextTypeNames);

                // We match the type from the other AppDomain to the corresponding one in our
                // AppDomain in one of two ways:
                //  1. Type.GetType()
                //  2. Scanning Assembly.GetExportedTypes
                // We do this because Type.GetType() is faster, but it will not work
                // for assemblies that cannot be loaded by the AssemblyName alone
                // (which occurs with binary references outside the project and in
                // class library projects when ASP.NET does not use the ASP.NET
                // temporary folders).

                // Pass 1: try to get the type directly
                foreach (string typeName in this.BusinessLogicData.ContextTypeNames)
                {
                    Type t = Type.GetType(typeName, /* throwOnError */ false);
                    if (t != null)
                    {
                        foundTypes.Add(t);
                        contextTypeNames.Remove(t.AssemblyQualifiedName);
                    }
                }

                // Pass 2: for any types not loaded above, locate them manually.
                foreach (Assembly a in loadedAssemblies)
                {
                    if (contextTypeNames.Count > 0)
                    {
                        // Ask for all exported types.  Use the safe version that logs issues
                        // and unconditionally returns the collection (which could be empty).
                        Type[] types = AssemblyUtilities.GetExportedTypes(a, this._logger).ToArray();

                        foreach (Type t in types)
                        {
                            // We remove each match from the candidate list and will take an early out
                            // once that list is empty.
                            if (contextTypeNames.Count > 0 && contextTypeNames.Contains(t.AssemblyQualifiedName))
                            {
                                foundTypes.Add(t);
                                contextTypeNames.Remove(t.AssemblyQualifiedName);
                            }
                        }
                    }
                }

                // Add the default empty context at the top always
                this._contexts.Add(new BusinessLogicContext(null, Resources.BusinessLogic_Class_Empty_Class_Name));

                // Create a BusinessLogicContext object for every type we matched in the new AppDomain
                foreach (Type t in foundTypes)
                {
                    try
                    {
                        if (typeof(DataContext).IsAssignableFrom(t))
                        {
                            this._contexts.Add(new LinqToSqlContext(t));
                        }
                        else if (typeof(ObjectContext).IsAssignableFrom(t))
                        {
                            this._contexts.Add(new LinqToEntitiesContext(t));
                        }
                        else if (typeof(DbContext).IsAssignableFrom(t))
                            this._contexts.Add(new LinqToEntitiesDbContext(t));
                        else
                        {
                            
                                this._logger(string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_InvalidContextType, t.FullName));
                            
                        }
                    }
                    catch(Exception ex)
                    {
                        this._logger(string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Failed_Load, t.FullName, ex.ToString()));
                    }
                }

                // Report every type we did not match
                foreach (string typeName in contextTypeNames)
                {
                    this._logger(string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Failed_Type_Load, typeName));
                }
            }

            // just select out all the ContextData's.  The other AppDomain does not get access
            // to the BusinessLogicContext objects, because we maintain type separation between
            // the AppDomains.
            return this._contexts.Select<BusinessLogicContext, ContextData>(blc => blc.ContextData).ToArray();
        }

        /// <summary>
        /// Retrieves the set of <see cref="EntityData"/> data objects for all the entities
        /// belonging to the specified context.
        /// </summary>
        /// <remarks>
        /// The primary purpose of this method is lazy loading.  The view model will ask for
        /// these only when it needs them.  It allows us to defer examining metadata until
        /// we absolutely need it, which could be a large performance win when multiple
        /// contexts are available.
        /// </remarks>
        /// <param name="contextData">The <see cref="ContextData"/> to use to locate
        /// the respective <see cref="BusinessLogicContext"/>.</param>
        /// <returns>The set of <see cref="EntityData"/> objects for the given context.</returns>
        public EntityData[] GetEntityDataItemsForContext(ContextData contextData)
        {
            BusinessLogicContext context = this._contexts[contextData.ID];
            List<BusinessLogicEntity> entities = (context == null) ? new List<BusinessLogicEntity>() : context.Entities.ToList();
            return entities.Select<BusinessLogicEntity, EntityData>(ble => ble.EntityData).ToArray();
        }

        /// <summary>
        /// Generates source code for the specified context.
        /// </summary>
        /// <param name="contextData">The <see cref="ContextData"/> to use to locate the appropriate <see cref="BusinessLogicContext"/>.</param>
        /// <param name="className">The name of the class.</param>
        /// <param name="namespaceName">The namespace to use for the class.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        public GeneratedCode GenerateBusinessLogicClass(ContextData contextData, string className, string namespaceName, string rootNamespace)
        {
            BusinessLogicContext context = this._contexts.SingleOrDefault(c => c.ContextData.ID == contextData.ID);
            return context != null
                        ? context.GenerateBusinessLogicClass(this.Language, className, namespaceName, rootNamespace)
                        : new GeneratedCode();
        }

        /// <summary>
        /// Generates the source code for the metadata class for the given context.
        /// </summary>
        /// <param name="contextData">The <see cref="ContextData"/> to use to locate the appropriate <see cref="BusinessLogicContext"/>.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <param name="optionalSuffix">If nonblank, the suffix to append to namespace and class names for testing</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        public GeneratedCode GenerateMetadataClasses(ContextData contextData, string rootNamespace, string optionalSuffix)
        {
            BusinessLogicContext context = this._contexts.Single(c => c.ContextData.ID == contextData.ID);
            return (context) != null
                        ? context.GenerateMetadataClasses(this.Language, rootNamespace, optionalSuffix)
                        : new GeneratedCode(string.Empty, Array.Empty<string>());
        }

        public bool IsMetadataGenerationRequired(ContextData contextData)
        {
            BusinessLogicContext context = this._contexts.SingleOrDefault(c => c.ContextData.ID == contextData.ID);
            return (context) != null
                        ? context.NeedToGenerateMetadataClasses
                        : false;
        }

        private void LoadAssemblies(string[] assembliesToLoad, IList<Assembly> loadedAssemblies)
        {
            foreach (string assemblyName in assembliesToLoad)
            {
                // Attempt to load assembly. Failures call logger and return null.
                Assembly a = AssemblyUtilities.LoadAssembly(assemblyName, this._logger);
                if (a != null)
                {
                    loadedAssemblies.Add(a);
                }
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Override of IDisposable.Dispose to handle implementation details of dispose
        /// </summary>
        public void Dispose()
        {
            this._contexts = null;
        }
        #endregion

        #region IRegisteredObject Members
        void IRegisteredObject.Stop(bool immediate)
        {
        }
        #endregion
    }
}
