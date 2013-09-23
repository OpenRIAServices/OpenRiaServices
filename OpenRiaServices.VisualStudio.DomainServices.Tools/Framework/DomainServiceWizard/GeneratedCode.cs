using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Tuple to hold the source code and references for generated code
    /// </summary>
    internal class GeneratedCode : MarshalByRefObject
    {
        private string _sourceCode;
        private IEnumerable<string> _references;

        /// <summary>
        /// Initializes a new empty instance of the <see cref="GeneratedCode"/> class.
        /// </summary>
        internal GeneratedCode()
        {
            this._sourceCode = string.Empty;
            this._references = Enumerable.Empty<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedCode"/> class.
        /// </summary>
        /// <param name="sourceCode">The source code as a string.  It can be empty but it cannot be null.</param>
        /// <param name="references">The assembly references.  The list can be empty but it cannot be null.</param>
        internal GeneratedCode(string sourceCode, IEnumerable<string> references)
        {
            // Empty is allowed, null is not
            if (sourceCode == null)
            {
                throw new ArgumentNullException("sourceCode");
            }
            if (references == null)
            {
                throw new ArgumentNullException("references");
            }
            this._sourceCode = sourceCode;
            this._references = references;
        }

        /// <summary>
        /// Gets the generated source code.  It may be empty but it cannot be null.
        /// </summary>
        internal string SourceCode
        {
            get
            {
                return this._sourceCode;
            }
        }

        /// <summary>
        /// Gets the assembly references required to compile the code.  It may be empty but it cannot be null.
        /// </summary>
        internal IEnumerable<string> References
        {
            get
            {
                return this._references;
            }
        }
    }
}
