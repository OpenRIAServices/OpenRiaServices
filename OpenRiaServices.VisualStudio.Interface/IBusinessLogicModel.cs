using System;
using System.Runtime.Remoting;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface IBusinessLogicModel
    {
        /// <summary>
        /// Finishes initializing this instance.  This method must be called after
        /// instantiating an instance of this class and before using any of its
        /// other methods.
        /// </summary>
        /// <param name="businessLogicData">The <see cref="IBusinessLogicData"/> for this model.</param>
        void Initialize(IBusinessLogicData businessLogicData);

        /// <summary>
        /// Gets the language to use for code generation.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the data objects for the <see cref="BusinessLogicContext"/> instances
        /// known by the current model.  These data objects are marshalled across
        /// the AppDomain boundary by <see cref="ContextViewModel"/>.
        /// </summary>
        /// <returns>The set of data objects for the <see cref="BusinessLogicContext"/>s.  
        /// This collection will always contain at least one element for the
        /// default empty context.</returns>
        IContextData[] GetContextDataItems();

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
        /// <param name="contextData">The <see cref="IContextData"/> to use to locate
        /// the respective <see cref="BusinessLogicContext"/>.</param>
        /// <returns>The set of <see cref="EntityData"/> objects for the given context.</returns>
        IEntityData[] GetEntityDataItemsForContext(IContextData contextData);

        /// <summary>
        /// Generates source code for the specified context.
        /// </summary>
        /// <param name="contextData">The <see cref="IContextData"/> to use to locate the appropriate <see cref="BusinessLogicContext"/>.</param>
        /// <param name="className">The name of the class.</param>
        /// <param name="namespaceName">The namespace to use for the class.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        IGeneratedCode GenerateBusinessLogicClass(IContextData contextData, string className, string namespaceName, string rootNamespace);

        /// <summary>
        /// Generates the source code for the metadata class for the given context.
        /// </summary>
        /// <param name="contextData">The <see cref="IContextData"/> to use to locate the appropriate <see cref="BusinessLogicContext"/>.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <param name="optionalSuffix">If nonblank, the suffix to append to namespace and class names for testing</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        IGeneratedCode GenerateMetadataClasses(IContextData contextData, string rootNamespace, string optionalSuffix);

        bool IsMetadataGenerationRequired(IContextData contextData);

        /// <summary>
        /// Override of IDisposable.Dispose to handle implementation details of dispose
        /// </summary>
        void Dispose();

        object GetLifetimeService();
        object InitializeLifetimeService();
        ObjRef CreateObjRef(Type requestedType);
    }
}