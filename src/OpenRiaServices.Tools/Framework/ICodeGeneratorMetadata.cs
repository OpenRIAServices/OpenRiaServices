namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Metadata interface for code generators.
    /// </summary>
    /// <remarks>
    /// This interface provides a strongly-typed contract for
    /// the metadata exported by code generators through export
    /// attributes such as <see cref="DomainServiceClientCodeGeneratorAttribute"/>.
    /// <para>
    /// Code generators are uniquely identified by the combination of the <see cref="GeneratorName"/> 
    /// and <see cref="Language"/> properties.
    /// </para>
    /// </remarks>
    public interface ICodeGeneratorMetadata
    {
        /// <summary>
        /// Gets the language the code generator supports.
        /// </summary>
        /// <value>
        /// This value is required and contains the string name of the language supported
        /// by this code generator, such as "C#" or "VB".
        /// </value>
        string Language { get; }

        /// <summary>
        /// Gets the logical name of the code generator.
        /// </summary>
        /// <value>
        /// This value is required and is used to provide a unique identity
        /// of the code generator.   When multiple code generators for a
        /// specific language are available, this name will be used to 
        /// allow clients to specify which one to use.
        /// </value>
        string GeneratorName { get; }
    }
}
