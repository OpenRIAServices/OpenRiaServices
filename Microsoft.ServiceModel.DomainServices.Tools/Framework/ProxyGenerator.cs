namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Abstract base class for all proxy generators.
    /// </summary>
    internal abstract class ProxyGenerator
    {
        private CodeDomClientCodeGenerator _proxyGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyGenerator"/> class.
        /// </summary>
        /// <param name="proxyGenerator">Our root client proxy generator holding the compilation context.  Cannot be null.</param>
        protected ProxyGenerator(CodeDomClientCodeGenerator proxyGenerator)
        {
            this._proxyGenerator = proxyGenerator;
        }

        /// <summary>
        /// Gets the main client proxy generator in whose compilation context we are generating code
        /// </summary>
        public CodeDomClientCodeGenerator ClientProxyGenerator
        {
            get
            {
                return this._proxyGenerator;
            }
        }

        /// <summary>
        /// Invoked after initialization to product proxy code
        /// </summary>
        public abstract void Generate();
    }
}
