namespace OpenRiaServices.Tools.SourceLocation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.SymbolStore;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
   

    /// <summary>
    /// Class that supports the dynamically generated <c>SourceInfoAttribute</c> in Live Intellisense
    /// builds.
    /// </summary>
    /// <remarks>
    ///  Becuase Live Intellisense builds creates an assembly for analysis using synthesized
    ///  files, the PDB information will not reliably report the original source files where
    ///  types and members were declared.  This defeats both the discovery of shared files as
    ///  well as use of the PDB for reporting error positions.
    ///  <para>
    ///  To address this issue, the synthesized assembly will include a <c>SourceInfoAttribute</c>
    ///  type, and it will add this attribute to each type and member.  This serves as an out-of-band
    ///  PDB mechanism to locate the original files and positions.
    ///  </para>
    /// </remarks>
    internal class SourceInfo
    {
        // The full type name of the internal SourceInfoAttribute *ends* with this string.
        // VB may prepend a root name space, and this attribute is generated in the context of the server's project.
        private const string SourceInfoAttributeTypeName = "Microsoft.VisualStudio.ServiceModel.DomainServices.Intellisense.SourceInfoAttribute";

        private static Type sourceInfoAttributeType;
        private static PropertyInfo fileNameProperty;
        private static PropertyInfo lineProperty;
        private static PropertyInfo columnProperty;

        private readonly string _fileName;
        private readonly int _line;
        private readonly int _column;

        /// <summary>
        /// Initializes a new <see cref="SourceInfo"/> instance.
        /// </summary>
        /// <param name="fileName">The full path to the original source file.  It may be null or empty.</param>
        /// <param name="line">The line number in the file where the member was declared.</param>
        /// <param name="column">The column number in the file where the member was declared.</param>
        private SourceInfo(string fileName, int line, int column)
        {
            this._fileName = fileName;
            this._line = line;
            this._column = column;
        }

        /// <summary>
        /// Gets the full path of the original source file.
        /// </summary>
        internal string FileName
        {
            get
            {
                return this._fileName;
            }
        }

        /// <summary>
        /// Gets the line number where the corresponding type
        /// or member was declare in <see cref="FileName"/>.
        /// </summary>
        internal int Line
        {
            get
            {
                return this._line;
            }
        }

        /// <summary>
        /// Gets the column number where the corresponding type
        /// or member was declare in <see cref="FileName"/>.
        /// </summary>
        internal int Column
        {
            get
            {
                return this._column;
            }
        }

        /// <summary>
        /// Helper class to extract values out of the <paramref name="attribute"/> that is known
        /// to be a <c>SourceInfoAttribute</c> and to create a new <see cref="SourceInfo"/> instance.
        /// </summary>
        /// <param name="attribute">An attribute instance known to be a <c>SourceInfoAttribute</c>.</param>
        /// <returns>A new <see cref="SourceInfo"/> instance, or null if there is a problem with the attribute structure.</returns>
        private static SourceInfo GetSourceInfoFromAttribute(object attribute)
        {
            Debug.Assert(attribute != null, "attribute cannot be null");
            Debug.Assert(attribute.GetType().FullName.EndsWith(SourceInfo.SourceInfoAttributeTypeName, StringComparison.Ordinal), "attribute was not a SourceInfoAttribute");

            // The first discovery of this type triggers Reflection to validate it
            // and to extract the property getters we need
            if (SourceInfo.sourceInfoAttributeType == null)
            {
                Type type = attribute.GetType();
                SourceInfo.sourceInfoAttributeType = type;

                // Because this is really a private interface, any errors in the structure of
                // the attribute silently revert to a "do no harm" mode.
                SourceInfo.fileNameProperty = type.GetProperty("FileName", BindingFlags.Public | BindingFlags.Instance);
                if (SourceInfo.fileNameProperty != null && SourceInfo.fileNameProperty.PropertyType != typeof(string))
                {
                    SourceInfo.fileNameProperty = null;
                }

                SourceInfo.lineProperty = type.GetProperty("Line", BindingFlags.Public | BindingFlags.Instance);
                if (SourceInfo.lineProperty != null && SourceInfo.lineProperty.PropertyType != typeof(int))
                {
                    SourceInfo.lineProperty = null;
                }

                SourceInfo.columnProperty = type.GetProperty("Column", BindingFlags.Public | BindingFlags.Instance);
                if (SourceInfo.columnProperty != null && SourceInfo.columnProperty.PropertyType != typeof(int))
                {
                    SourceInfo.columnProperty = null;
                }
            }

            // Any internal errors with the structure of the attribute type cause this quick return
            // that effectively says "this is not really a SourceInfoAttribute" and continues running
            if (SourceInfo.fileNameProperty == null || SourceInfo.lineProperty == null || SourceInfo.columnProperty == null)
            {
                return null;
            }

            // We are assured the following getters should succeed in their casts
            string fileName = (string) SourceInfo.fileNameProperty.GetValue(attribute, null);
            int line = (int) SourceInfo.lineProperty.GetValue(attribute, null);
            int column = (int)SourceInfo.columnProperty.GetValue(attribute, null);

            return new SourceInfo(fileName, line, column);
        }

        /// <summary>
        /// Examines the set of <paramref name="attributes"/> and returns a new <see cref="SourceInfo"/>
        /// instance from the first <c>SourceInfoAttribute</c> in the set.
        /// </summary>
        /// <param name="attributes">A list of attribute instances that may contain a <c>SourceInfoAttribute</c>.</param>
        /// <returns>A new <see cref="SourceInfo"/> instance or <c>null</c> if the set of <paramref name="attributes"/>
        /// did not contain a <c>SourceInfoAttribute</c>.
        /// </returns>
        internal static SourceInfo GetSourceInfoFromAttributes(IEnumerable<object> attributes)
        {
            SourceInfo sourceInfo = null;

            foreach (object attribute in attributes)
            {
                Type attributeType = attribute.GetType();
                if ((SourceInfo.sourceInfoAttributeType != null && attributeType == SourceInfo.sourceInfoAttributeType) ||
                    attributeType.FullName.EndsWith(SourceInfo.SourceInfoAttributeTypeName, StringComparison.Ordinal))
                {
                    return SourceInfo.GetSourceInfoFromAttribute(attribute);   
                }
            }

            return sourceInfo;
        }
    }
}
