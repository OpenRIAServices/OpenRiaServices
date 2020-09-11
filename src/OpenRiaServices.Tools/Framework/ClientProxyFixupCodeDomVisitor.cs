using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Visits a <see cref="CodeCompileUnit"/> class and optimizes it for client proxy generation.
    /// </summary>
    internal sealed class ClientProxyFixupCodeDomVisitor : CodeDomVisitor
    {
        private readonly ClientCodeGenerationOptions _options;
        private readonly bool _isCSharp;

        /// <summary>
        /// Default constructor accepting the current <see cref="ClientCodeGenerationOptions"/> context.
        /// </summary>
        /// <param name="options">The current <see cref="ClientCodeGenerationOptions"/> options.</param>
        public ClientProxyFixupCodeDomVisitor(ClientCodeGenerationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this._options = options;
            this._isCSharp = (this._options.Language == "C#");
        }

        #region Properties

        /// <summary>
        /// Gets a value indicating whether or not the <see cref="CodeCompileUnit"/> will be
        /// emitted to a C# code.
        /// </summary>
        private bool IsCSharp
        {
            get
            {
                return this._isCSharp;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the <see cref="CodeCompileUnit"/> is 
        /// generating types using full type names.
        /// </summary>
        private bool UseFullTypeNames
        {
            get
            {
                return this._options.UseFullTypeNames;
            }
        }

        /// <summary>
        /// Gets the root namespace.  (For use with Visual Basic codegen only.)
        /// </summary>
        private string ClientRootNamespace
        {
            get
            {
                return this._options.ClientRootNamespace;
            }
        }

        #endregion // Properties

        #region Methods

        #region Visitor Overrides

        /// <summary>
        /// Visits a <see cref="CodeNamespaceCollection"/>.
        /// </summary>
        /// <param name="codeNamespaceCollection">The <see cref="CodeNamespaceCollection"/> to visit.</param>
        protected override void VisitCodeNamespaceCollection(CodeNamespaceCollection codeNamespaceCollection)
        {
            CodeNamespace[] orderedNamespaces = codeNamespaceCollection.Cast<CodeNamespace>().OrderBy(ns => ns.Name).ToArray();
            codeNamespaceCollection.Clear();

            if (this.IsCSharp)
            {
                codeNamespaceCollection.AddRange(orderedNamespaces);
            }
            else
            {
                foreach (CodeNamespace ns in orderedNamespaces)
                {
                    this.FixUpNamespaceRootNamespace(ns);
                    codeNamespaceCollection.Add(ns);
                }
            }

            base.VisitCodeNamespaceCollection(codeNamespaceCollection);
        }

        /// <summary>
        /// Visits a <see cref="CodeNamespaceImportCollection"/>.
        /// </summary>
        /// <param name="codeNamespaceImportCollection">The <see cref="CodeNamespaceImportCollection"/> to visit.</param>
        protected override void VisitCodeNamespaceImportCollection(CodeNamespaceImportCollection codeNamespaceImportCollection)
        {
            CodeNamespaceImport[] sortedImports = codeNamespaceImportCollection.Cast<CodeNamespaceImport>().OrderBy(i => i.Namespace, new NamespaceImportComparer()).Distinct().ToArray();

            codeNamespaceImportCollection.Clear();
            codeNamespaceImportCollection.AddRange(sortedImports);

            base.VisitCodeNamespaceImportCollection(codeNamespaceImportCollection);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeDeclarationCollection"/>.
        /// </summary>
        /// <param name="codeTypeDeclarationCollection">The <see cref="CodeTypeDeclarationCollection"/> to visit.</param>
        protected override void VisitCodeTypeDeclarationCollection(CodeTypeDeclarationCollection codeTypeDeclarationCollection)
        {
            CodeTypeDeclaration[] sortedTypeDeclarations = codeTypeDeclarationCollection.Cast<CodeTypeDeclaration>().OrderBy(c => c.Name).ToArray();
            codeTypeDeclarationCollection.Clear();
            codeTypeDeclarationCollection.AddRange(sortedTypeDeclarations);

            base.VisitCodeTypeDeclarationCollection(codeTypeDeclarationCollection);
        }

        /// <summary>
        /// Visits a <see cref="CodeAttributeDeclarationCollection"/>.
        /// </summary>
        /// <param name="codeAttributeDeclarationCollection">The <see cref="CodeAttributeDeclarationCollection"/> to visit.</param>
        protected override void VisitCodeAttributeDeclarationCollection(CodeAttributeDeclarationCollection codeAttributeDeclarationCollection)
        {
            CodeAttributeDeclaration[] sortedAttributes = codeAttributeDeclarationCollection.Cast<CodeAttributeDeclaration>().OrderBy(a => GetAttributeId(a)).ToArray();
            codeAttributeDeclarationCollection.Clear();
            codeAttributeDeclarationCollection.AddRange(sortedAttributes);

            base.VisitCodeAttributeDeclarationCollection(codeAttributeDeclarationCollection);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeReference"/>.
        /// </summary>
        /// <param name="codeTypeReference">The <see cref="CodeTypeReference"/> to visit.</param>
        protected override void VisitCodeTypeReference(CodeTypeReference codeTypeReference)
        {
            if (codeTypeReference == null)
            {
                return;
            }

            if (this.UseFullTypeNames)
            {
                codeTypeReference.Options |= CodeTypeReferenceOptions.GlobalReference;
            }

            base.VisitCodeTypeReference(codeTypeReference);
        }

        #endregion // Visitor Overrides

        #region Utility Methods

        /// <summary>
        /// Gets a unique identifier for a <see cref="CodeAttributeDeclaration"/> instance.
        /// </summary>
        /// <param name="attribute">The <see cref="CodeAttributeDeclaration"/> instance.</param>
        /// <returns>A unique identifier for a <see cref="CodeAttributeDeclaration"/> instance.</returns>
        private static string GetAttributeId(CodeAttributeDeclaration attribute)
        {
            StringBuilder id = new StringBuilder(attribute.Name);

            foreach (CodeAttributeArgument arg in attribute.Arguments)
            {
                CodePrimitiveExpression primitiveValue = arg.Value as CodePrimitiveExpression;

                id.Append(arg.Name);

                if (primitiveValue != null && primitiveValue.Value != null)
                {
                    id.Append(primitiveValue.Value.ToString());
                }
                else
                {
                    id.Append(arg.Value.ToString());
                }
            }

            return id.ToString();
        }

        /// <summary>
        /// Fixes up a <see cref="CodeNamespace"/>.  (For use with Visual Basic codegen.)
        /// </summary>
        /// <param name="ns">The <see cref="CodeNamespace"/> to fix.</param>
        private void FixUpNamespaceRootNamespace(CodeNamespace ns)
        {
            string rootNamespace = this.ClientRootNamespace;

            if (!this.IsCSharp && !string.IsNullOrEmpty(rootNamespace))
            {
                string namespaceName = ns.Name;

                if (namespaceName.Equals(rootNamespace, StringComparison.Ordinal))
                {
                    ns.Name = string.Empty;
                }
                else if (namespaceName.StartsWith(rootNamespace + ".", StringComparison.Ordinal))
                {
                    ns.Name = namespaceName.Substring(rootNamespace.Length + 1);
                }
            }
        }

        #endregion // Utility Methods

        #endregion // Methods

        #region Nested Types

        /// <summary>
        /// Nested type used to sort import statements, giving preference to System assemblies.
        /// </summary>
        private class NamespaceImportComparer : IComparer<string>
        {
            /// <summary>
            /// Compares two string values and returns a value indicating the preferred sort order.
            /// </summary>
            /// <param name="x">A string value to compare against.</param>
            /// <param name="y">A string value to compare.</param>
            /// <returns>-1 if the <paramref name="x"/> value is less, 0 if the values are equal, 1 if <paramref name="y"/> is less.</returns>
            public int Compare(string x, string y)
            {
                bool leftIsSystemNamespace = x.Equals("System", StringComparison.Ordinal) || x.StartsWith("System.", StringComparison.Ordinal);
                bool rightIsSystemNamespace = y.Equals("System", StringComparison.Ordinal) || y.StartsWith("System.", StringComparison.Ordinal);

                if (leftIsSystemNamespace && !rightIsSystemNamespace)
                {
                    return -1;
                }
                else if (!leftIsSystemNamespace && rightIsSystemNamespace)
                {
                    return 1;
                }

                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }

        #endregion // Nested Types
    }
}
