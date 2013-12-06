using System;
using System.CodeDom;
using System.Collections.Generic;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface IBusinessLogicContext
    {
        /// <summary>
        /// Gets or sets the mutable state shared with <see cref="ContextViewModel"/>
        /// across AppDomain boundaries.
        /// </summary>
        IContextData ContextData { get; set; }

        /// <summary>
        /// Gets the user visible name of the context (typically the type name)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the name of the DAL technology of this context
        /// </summary>
        string DataAccessLayerName { get; }

        /// <summary>
        /// Gets the name of the context and the DAL technology it uses
        /// </summary>
        string NameAndDataAccessLayerName { get; }

        /// <summary>
        /// Gets the type of the context (e.g. subclass of ObjectContext or DomainContext for EDM and LinqToSql, respectively).
        /// </summary>
        Type ContextType { get; }

        /// <summary>
        /// Gets the value indicating whether [RequiresClientAccess] will be generated
        /// </summary>
        bool IsClientAccessEnabled { get; }

        /// <summary>
        /// Gets the value indicating whether an OData endpoint will be exposed
        /// </summary>
        bool IsODataEndpointEnabled { get; }

        /// <summary>
        /// Gets the collection of entities exposed by this context, sorted by name
        /// </summary>
        IEnumerable<IBusinessLogicEntity> Entities { get; }

        /// <summary>
        /// Gets the collection of <see cref="EntityData"/> data objects for all the
        /// entities that belong to the current context.
        /// </summary>
        /// <remarks>
        /// This property does lazy evaluation so that the view models can populate
        /// the UI quickly but pay for the cost of scanning the context only when
        /// the UI requires it.
        /// </remarks>
        IEnumerable<IEntityData> EntityDataItems { get; }

        bool NeedToGenerateMetadataClasses { get; }

        /// <summary>
        /// Creates all the domain operation entries for the given entity.
        /// </summary>
        /// <param name="codeGenContext">The context into which to generate code.  It cannot be null.</param>
        /// <param name="businessLogicClass">The class into which to generate the method</param>
        /// <param name="entity">The entity which will be affected by this method</param>
        void GenerateEntityDomainOperationEntries(ICodeGenContext codeGenContext, CodeTypeDeclaration businessLogicClass, IBusinessLogicEntity entity);

        /// <summary>
        /// Generates the code for the domain service class.
        /// </summary>
        /// <param name="language">The language to use.</param>
        /// <param name="className">The name of the class.</param>
        /// <param name="namespaceName">The namespace to use for the class.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        IGeneratedCode GenerateBusinessLogicClass(string language, string className, string namespaceName, string rootNamespace);

        /// <summary>
        /// Generates the code for the domain service class.
        /// </summary>
        /// <param name="language">The language to use.</param>
        /// <param name="rootNamespace">The root namespace (VB).</param>
        /// <param name="optionalSuffix">If nonblank, the suffix to append to namespace and class names for testing</param>
        /// <returns>A value containing the generated source code and necessary references.</returns>
        IGeneratedCode GenerateMetadataClasses(string language, string rootNamespace, string optionalSuffix);

        /// <summary>
        /// Generates the metadata class for the given object (entity or complex object)
        /// </summary>
        /// <param name="codeGenContext">The context to use to generate code.</param>
        /// <param name="optionalSuffix">If not null, optional suffix to class name and namespace</param>
        /// <param name="type">The type of the object for which to generate the metadata class.</param>
        /// <returns><c>true</c> means at least some code was generated.</returns>
        bool GenerateMetadataClass(ICodeGenContext codeGenContext, string optionalSuffix, Type type);

        /// <summary>
        /// Overrides <see cref="Object.ToString()"/> to  facilitate debugging
        /// </summary>
        /// <returns>The name of the context.</returns>
        string ToString();
    }
}