using System;
using System.ComponentModel.Composition;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Derived <see cref="ExportAttribute"/> used for all code generators
    /// that support <see cref="IDomainServiceClientCodeGenerator"/>.
    /// </summary>
    /// <remarks>
    /// This attribute exports both the type of the code generator
    /// (<see cref="IDomainServiceClientCodeGenerator"/>) as well
    /// as the metadata that describe the code generator.
    /// </remarks>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DomainServiceClientCodeGeneratorAttribute : ExportAttribute, ICodeGeneratorMetadata
    {
        private string _generatorName;
        private string _language;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceClientCodeGeneratorAttribute"/> class
        /// for a generator name specified by <paramref name="generatorName"/>.
        /// </summary>
        /// <param name="generatorName">The unique name of this generator.</param>
        /// <param name="language">The language supported by this generator.</param>
        public DomainServiceClientCodeGeneratorAttribute(string generatorName, string language)
            : base(typeof(IDomainServiceClientCodeGenerator))
        {
            this._generatorName = generatorName;
            this._language = language;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceClientCodeGeneratorAttribute"/> class
        /// for a generator name specified by <paramref name="generatorType"/>.
        /// </summary>
        /// <remarks>
        /// This overload will use the <paramref name="generatorType"/>'s name for
        /// the <see cref="GeneratorName"/> property.
        /// </remarks>
        /// <param name="generatorType">The type of the generator.</param>
        /// <param name="language">The language supported by this generator.</param>
        public DomainServiceClientCodeGeneratorAttribute(Type generatorType, string language) : this(generatorType != null ? generatorType.FullName : string.Empty, language)
        {
        }

        /// <summary>
        /// Gets the language supported by this code generator.
        /// </summary>
        public string Language { get { return this._language; } }

        /// <summary>
        /// Gets the logical name of this generator.
        /// </summary>
        /// <value>
        /// This value provides a unique identity to this code generator
        /// that can be used to select among multiple code generators.
        /// </value>
        public string GeneratorName { get { return this._generatorName; } }
    }
}
