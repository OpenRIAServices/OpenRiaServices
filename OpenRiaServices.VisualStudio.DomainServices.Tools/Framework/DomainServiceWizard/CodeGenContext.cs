using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// This class captures the CodeDom provider, root namespace, etc and provides some common code gen helper methods
    /// </summary>
    public sealed class CodeGenContext : IDisposable, ICodeGenContext
    {
        private CodeDomProvider _provider;
        private CodeCompileUnit _compileUnit = new CodeCompileUnit();
        private CodeGeneratorOptions _options = new CodeGeneratorOptions();
        private Dictionary<string, CodeNamespace> _namespaces = new Dictionary<string, CodeNamespace>();
        private List<string> _references = new List<string>();
        private string _rootNamespace;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="language">Must be "C#" or "VB" currently</param>
        /// <param name="rootNamespace">If non-null, please do not generate any namespace directives starting with it.</param>
        public CodeGenContext(string language, string rootNamespace)
        {
            this._provider = language.Equals("C#") ? (CodeDomProvider)new CSharpCodeProvider() : (CodeDomProvider)new VBCodeProvider();
            this._rootNamespace = rootNamespace;
            this.Initialize();
        }

        /// <summary>
        /// Sets up the generator options
        /// </summary>
        private void Initialize()
        {
            CodeGeneratorOptions cgo = new CodeGeneratorOptions();      // TODO: get from VS???
            cgo.IndentString = "    ";

            // We require verbatim order for predictability for baselines
            cgo.VerbatimOrder = true;

            cgo.BlankLinesBetweenMembers = true;
            cgo.BracingStyle = "C";
            this._options = cgo;
        }

        /// <summary>
        /// Gets the CodeCompileUnit for this context
        /// </summary>
        private CodeCompileUnit CompileUnit
        {
            get
            {
                return this._compileUnit;
            }
        }

        /// <summary>
        /// Gets the CodeGeneratorOptions for this context
        /// </summary>
        public CodeGeneratorOptions CodeGeneratorOptions
        {
            get
            {
                return this._options;
            }
        }

        /// <summary>
        /// Gets the CodeDomProvider for this context
        /// </summary>
        public CodeDomProvider Provider
        {
            get
            {
                return this._provider;
            }
        }

        /// <summary>
        /// Gets the value indicating whether the language is C#
        /// </summary>
        public bool IsCSharp
        {
            get
            {
                return this._provider is CSharpCodeProvider;
            }
        }

        /// <summary>
        /// Gets the set of assembly references generated code needs to compile
        /// </summary>
        /// <remarks>
        /// This set represents only those additional references required for
        /// the entity types and DAL types appearing in the generated code
        /// </remarks>
        public IEnumerable<string> References
        {
            get
            {
                return this._references;
            }
        }

        /// <summary>
        /// Gets the root namespace.  For use with VB codegen only.
        /// </summary>
        public string RootNamespace
        {
            get
            {
                return this._rootNamespace;
            }
        }

        /// <summary>
        /// Adds the given assembly reference to the list of known assembly references
        /// necessary to compile
        /// </summary>
        /// <param name="reference">The full name of the assembly reference.</param>
        public void AddReference(string reference)
        {
            if (!this._references.Contains(reference, StringComparer.OrdinalIgnoreCase))
            {
                this._references.Add(reference);
            }
        }

        /// <summary>
        /// Returns <c>true</c> only if the given identifier is valid for the current language
        /// </summary>
        /// <param name="identifier">The string to check for validity.  Null will cause a <c>false</c> to be returned.</param>
        /// <returns><c>true</c> if the identifier is valid</returns>
        public bool IsValidIdentifier(string identifier)
        {
            return !string.IsNullOrEmpty(identifier) && this._provider.IsValidIdentifier(identifier);
        }

        /// <summary>
        /// Generates code for the current compile unit into a string.  Also strips out auto-generated comments/
        /// </summary>
        /// <returns>The generated code and necessary references.</returns>
        public IGeneratedCode GenerateCode()
        {
            string generatedCode = string.Empty;
            using (TextWriter t = new StringWriter(CultureInfo.InvariantCulture))
            {
                this.FixUpCompileUnit(this.CompileUnit);
                this.Provider.GenerateCodeFromCompileUnit(this.CompileUnit, t, this._options);
                generatedCode = this.FixupVBOptionStatements(t.ToString());
            }

            // Remove the auto-generated comment about "please don't modify this code"
            string sourceCode = CodeGenUtilities.StripAutoGenPrefix(generatedCode, this.IsCSharp);
            return new GeneratedCode(sourceCode, this.References);
        }

        /// <summary>
        /// Fixes up a <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="compileUnit">The <see cref="CodeCompileUnit"/> to fix up.</param>
        private void FixUpCompileUnit(CodeCompileUnit compileUnit)
        {
            DomainServiceFixupCodeDomVisitor visitor = new DomainServiceFixupCodeDomVisitor(this);
            visitor.Visit(compileUnit);
        }

        // WARNING:
        // This code is copied verbatim from the OpenRiaServices.DomainServices.Tools.ClientProxyGenerator class
        // changes in this code will likely be required to be ported to that class as well.
        // See ClientProxyGenerator class for details.
        private string FixupVBOptionStatements(string code)
        {
            if (!this.IsCSharp && code != null)
            {
                StringBuilder strBuilder = new StringBuilder(code);

                // We need to change Option Strict from Off to On and add
                // Option Infer and Compare.  Option Explict is ok.
                string optionStrictOff = "Option Strict Off";
                string optionStrictOn = "Option Strict On";
                string optionInferOn = "Option Infer On";
                string optionCompareBinary = "Option Compare Binary";

                int idx = code.IndexOf(optionStrictOff, StringComparison.Ordinal);

                if (idx != -1)
                {
                    strBuilder.Replace(optionStrictOff, optionStrictOn, idx, optionStrictOff.Length);
                    strBuilder.Insert(idx, optionInferOn + Environment.NewLine);
                    strBuilder.Insert(idx, optionCompareBinary + Environment.NewLine);
                }
                return strBuilder.ToString();
            }
            return code;
        }

        /// <summary>
        /// Generates a new CodeNamespace or reuses an existing one of the given name
        /// </summary>
        /// <param name="namespaceName">The namespace in which to generate code.</param>
        /// <returns>namespace with the given name</returns>
        public CodeNamespace GetOrGenNamespace(string namespaceName)
        {
            CodeNamespace ns = null;

            if (string.IsNullOrEmpty(namespaceName))
            {
                return null;
            }

            string adjustedNamespaceName = this.GetNamespaceName(namespaceName);
            if (!this._namespaces.TryGetValue(adjustedNamespaceName, out ns))
            {
                ns = new CodeNamespace(adjustedNamespaceName);

                this._namespaces[adjustedNamespaceName] = ns;

                // Add all the fixed namespace imports
                foreach (string fixedImport in BusinessLogicClassConstants.FixedImports)
                {
                    CodeNamespaceImport import = new CodeNamespaceImport(fixedImport);
                    ns.Imports.Add(import);
                }

                this.CompileUnit.Namespaces.Add(ns);
            }
            return ns;
        }

        /// <summary>
        /// If the project has a rootnamespace, it strips it out from the namespace passed in and returns the actual namespace to be generated in code.
        /// </summary>
        /// <param name="namespaceName">The full namespace (including the root namespace, if the project has one)</param>
        /// <returns>The actual namespace to be generated in code.</returns>
        private string GetNamespaceName(string namespaceName)
        {
            string adjustedNamespaceName = namespaceName;
            if (!String.IsNullOrEmpty(this._rootNamespace) && namespaceName.Equals(this._rootNamespace, StringComparison.Ordinal))
            {
                adjustedNamespaceName = String.Empty;
            }
            else if (!String.IsNullOrEmpty(this._rootNamespace) && namespaceName.StartsWith(this._rootNamespace + ".", StringComparison.Ordinal))
            {
                adjustedNamespaceName = namespaceName.Substring(this._rootNamespace.Length + 1);
            }
            return adjustedNamespaceName;
        }

        /// <summary>
        /// Gets the <see cref="CodeNamespace"/> for a <see cref="CodeTypeDeclaration"/>.
        /// </summary>
        /// <param name="typeDecl">A <see cref="CodeTypeDeclaration"/>.</param>
        /// <returns>A <see cref="CodeNamespace"/> or null.</returns>
        public CodeNamespace GetNamespace(CodeTypeDeclaration typeDecl)
        {
            string namespaceName = typeDecl.UserData["Namespace"] as string;
            Debug.Assert(namespaceName != null, "Null namespace");
            CodeNamespace ns = null;
            this._namespaces.TryGetValue(namespaceName, out ns);
            return ns;
        }

        #region IDisposable Members

        /// <summary>
        /// Override of IDisposable.Dispose to handle implementation details of dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            CodeDomProvider provider = this._provider;
            this._provider = null;
            if (provider != null)
            {
                provider.Dispose();
            }
            this._compileUnit = null;
            this._namespaces = null;
        }

        #endregion
    }
}