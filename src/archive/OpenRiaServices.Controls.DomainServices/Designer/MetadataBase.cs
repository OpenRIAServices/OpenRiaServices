﻿extern alias Silverlight;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Controls;
using OpenRiaServices.DomainServices;
using System.Xml.Linq;
using Microsoft.Windows.Design.Metadata;
using SSW = Silverlight::System.Windows;

namespace System.Windows.Controls.Design.Common
{
    /// <summary>
    /// MetadataRegistration class.
    /// </summary>
    public class MetadataRegistrationBase
    {
        /// <summary>
        /// Build design time metadata attribute table.
        /// </summary>
        /// <returns>Custom attribute table.</returns>
        protected virtual AttributeTable BuildAttributeTable()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();

            this.AddDescriptions(builder);
            this.AddAttributes(builder);
            AddTables(builder);

            return builder.CreateTable();
        }

        /// <summary>
        /// Find all AttributeTableBuilder subclasses in the assembly 
        /// and add their attributes to the assembly attribute table.
        /// </summary>
        /// <param name="builder">The assembly attribute table builder.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Design time dll should not fail!")]
        private static void AddTables(AttributeTableBuilder builder)
        {
            Debug.Assert(builder != null, "AddTables is called with null parameter!");

            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (Type t in asm.GetTypes())
            {
                if (t.IsSubclassOf(typeof(AttributeTableBuilder)))
                {
                    try
                    {
                        AttributeTableBuilder atb = (AttributeTableBuilder)Activator.CreateInstance(t);
                        builder.AddTable(atb.CreateTable());
                    }
                    catch (Exception e)
                    {
                        if (e.IsFatal())
                        {
                            throw;
                        }
                        Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Exception in AddTables method: {0}", e));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the case sensitive resource name of the embedded XML file.
        /// </summary>
        protected string XmlResourceName { get; set; }

        /// <summary>
        /// Gets or sets the FullName of the corresponding run time assembly.
        /// </summary>
        protected string AssemblyFullName { get; set; }

        /// <summary>
        /// Create description attribute from run time assembly xml file.
        /// </summary>
        /// <param name="builder">The assembly attribute table builder.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Design time dll should not fail.")]
        private void AddDescriptions(AttributeTableBuilder builder)
        {
            Debug.Assert(builder != null, "AddDescriptions is called with null parameter!");

            if (string.IsNullOrEmpty(this.XmlResourceName) ||
                string.IsNullOrEmpty(this.AssemblyFullName))
            {
                return;
            }

            XDocument xdoc = XDocument.Load(new StreamReader(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(this.XmlResourceName)));
            if (xdoc == null)
            {
                return;
            }

            foreach (XElement member in xdoc.Descendants("member"))
            {
                try
                {
                    string name = (string)member.Attribute("name");
                    bool isType = name.StartsWith("T:", StringComparison.OrdinalIgnoreCase);
                    if (isType ||
                        name.StartsWith("P:", StringComparison.OrdinalIgnoreCase))
                    {
                        int lastDot = name.Length;
                        string typeName;
                        if (isType)
                        {
                            typeName = name.Substring(2); // skip leading "T:"
                        }
                        else
                        {
                            lastDot = name.LastIndexOf('.');
                            typeName = name.Substring(2, lastDot - 2);
                        }
                        typeName += this.AssemblyFullName;

                        Type t = Type.GetType(typeName);
                        if (t != null && t.IsPublic && t.IsClass &&
                            t.IsSubclassOf(typeof(SSW::FrameworkElement)))
                        {
                            string desc = member.Descendants("summary").FirstOrDefault().Value;
                            desc = desc.Trim();
                            desc = string.Join(" ", desc.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries));

                            if (isType)
                            {
                                builder.AddCallback(t, b => b.AddCustomAttributes(new DescriptionAttribute(desc)));
                            }
                            else
                            {
                                string propName = name.Substring(lastDot + 1);
                                PropertyInfo pi = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                                if (pi != null)
                                {
                                    builder.AddCallback(t, b => b.AddCustomAttributes(propName, new DescriptionAttribute(desc)));
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.IsFatal())
                    {
                        throw;
                    }
                    Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Exception: {0}\n For member : {1}", e, member));
                }
            }
        }

        /// <summary>
        /// Provide a place to add custom attributes without creating a AttributeTableBuilder subclass.
        /// </summary>
        /// <param name="builder">The assembly attribute table builder.</param>
        protected virtual void AddAttributes(AttributeTableBuilder builder)
        {
            // Allow FilterDescriptor.IgnoredValue to be edited as a string
            builder.AddCustomAttributes(typeof(FilterDescriptor), "IgnoredValue", new TypeConverterAttribute(typeof(StringConverter)));

            // Allow FilterDescriptor.Value to be edited as a string
            builder.AddCustomAttributes(typeof(FilterDescriptor), "Value", new TypeConverterAttribute(typeof(StringConverter)));

            // Allow Parameter.Value (for QueryParameters) to be edited as a string
            builder.AddCustomAttributes(typeof(Parameter), "Value", new TypeConverterAttribute(typeof(StringConverter)));
        }
    }
}