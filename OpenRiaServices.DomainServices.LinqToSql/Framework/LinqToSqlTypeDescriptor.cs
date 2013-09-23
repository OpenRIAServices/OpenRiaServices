using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Linq.Mapping;
using System.Globalization;
using System.Linq;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.LinqToSql
{
    /// <summary>
    /// CustomTypeDescriptor for LINQ To SQL entities
    /// </summary>
    internal class LinqToSqlTypeDescriptor : TypeDescriptorBase
    {
        private LinqToSqlTypeDescriptionContext _typeDescriptionContext;
        private MetaType _metaType;
        private bool _keyIsEditable;

        /// <summary>
        /// Constructor that takes the metadata context, a metadata type and a parent custom type descriptor
        /// </summary>
        /// <param name="typeDescriptionContext">The <see cref="LinqToSqlTypeDescriptionContext"/> context.</param>
        /// <param name="metaType">The <see cref="MetaType"/> type.</param>
        /// <param name="parent">The parent custom type descriptor.</param>
        public LinqToSqlTypeDescriptor(LinqToSqlTypeDescriptionContext typeDescriptionContext, MetaType metaType, ICustomTypeDescriptor parent)
            : base(parent)
        {
            this._typeDescriptionContext = typeDescriptionContext;
            this._metaType = metaType;

            // if any FK member of any association is also part of the primary key, then the key cannot be marked
            // Editable(false)
            this._keyIsEditable = this._metaType.Associations.Any(p => p.IsForeignKey && p.ThisKey.Any(q => q.IsPrimaryKey));
        }

        /// <summary>
        /// Gets the metadata context
        /// </summary>
        public LinqToSqlTypeDescriptionContext TypeDescriptionContext
        {
            get
            {
                return this._typeDescriptionContext;
            }
        }

        /// <summary>
        /// Returns a collection of all the <see cref="Attribute"/>s we infer from the metadata associated
        /// with the metadata member corresponding to the given property descriptor
        /// </summary>
        /// <param name="pd">A <see cref="PropertyDescriptor"/>.</param>
        /// <returns>A collection of attributes inferred from metadata in the given descriptor.</returns>
        protected override IEnumerable<Attribute> GetMemberAttributes(PropertyDescriptor pd)
        {
            List<Attribute> attributes = new List<Attribute>();
            MetaDataMember member = this._metaType.DataMembers.Where(p => p.Name == pd.Name).SingleOrDefault();
            if (member != null)
            {
                EditableAttribute editableAttribute = null;
                bool hasKeyAttribute = (pd.Attributes[typeof(KeyAttribute)] != null);
                if (member.IsPrimaryKey && !hasKeyAttribute)
                {
                    attributes.Add(new KeyAttribute());
                    hasKeyAttribute = true;
                }

                // Check if the member is DB generated and add the DatabaseGeneratedAttribute to it if not already present.
                if (member.IsDbGenerated && pd.Attributes[typeof(DatabaseGeneratedAttribute)] == null)
                {
                    if (member.AutoSync == AutoSync.OnInsert)
                    {
                        attributes.Add(new DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity));
                    }
                    else if (member.AutoSync == AutoSync.Always)
                    {
                        attributes.Add(new DatabaseGeneratedAttribute(DatabaseGeneratedOption.Computed));
                    }
                }

                if (hasKeyAttribute && !this._keyIsEditable)
                {
                    editableAttribute = new EditableAttribute(false) { AllowInitialValue = true };
                }

                if (member.IsAssociation &&
                    pd.Attributes[typeof(System.ComponentModel.DataAnnotations.AssociationAttribute)] == null)
                {
                    System.ComponentModel.DataAnnotations.AssociationAttribute assocAttrib = this.TypeDescriptionContext.CreateAssociationAttribute(member);
                    attributes.Add(assocAttrib);
                }

                // Add Required attribute to metdata if the member cannot be null and it is either a reference type or a Nullable<T>
                bool isStringType = pd.PropertyType == typeof(string) || pd.PropertyType == typeof(char[]);
                if (!member.CanBeNull && (!pd.PropertyType.IsValueType || IsNullableType(pd.PropertyType)) &&
                    pd.Attributes[typeof(RequiredAttribute)] == null)
                {
                    attributes.Add(new RequiredAttribute());
                }

                // Add implicit ConcurrencyCheck attribute to metadata if UpdateCheck is anything other than UpdateCheck.Never
                if (member.UpdateCheck != UpdateCheck.Never &&
                    pd.Attributes[typeof(ConcurrencyCheckAttribute)] == null)
                {
                    attributes.Add(new ConcurrencyCheckAttribute());
                }

                bool hasTimestampAttribute = (pd.Attributes[typeof(TimestampAttribute)] != null);
                if (member.IsVersion && !hasTimestampAttribute)
                {
                    attributes.Add(new TimestampAttribute());
                    hasTimestampAttribute = true;
                }

                // All members marked with TimestampAttribute (inferred or explicit) need to
                // have [Editable(false)] applied
                if (hasTimestampAttribute && editableAttribute == null)
                {
                    editableAttribute = new EditableAttribute(false);
                }

                // Add RoundtripOriginal attribute to this member unless
                // - this entity has a timestamp member, in which case that member should be the ONLY
                //   member we apply RTO to.
                // - the member is marked with AssociationAttribute
                if (!member.IsAssociation && 
                    pd.Attributes[typeof(System.ComponentModel.DataAnnotations.AssociationAttribute)] == null
                    && (this._metaType.VersionMember == null || member.IsVersion))
                {
                    if (pd.Attributes[typeof(RoundtripOriginalAttribute)] == null)
                    {
                        attributes.Add(new RoundtripOriginalAttribute());
                    }
                }

                if (isStringType && member.DbType != null && member.DbType.Length > 0 &&
                    pd.Attributes[typeof(StringLengthAttribute)] == null)
                {
                    InferStringLengthAttribute(member.DbType, attributes);
                }

                // Add EditableAttribute if required
                if (editableAttribute != null && pd.Attributes[typeof(EditableAttribute)] == null)
                {
                    attributes.Add(editableAttribute);
                }
            }
            return attributes.ToArray();
        }

        /// <summary>
        /// Parse the DbType to determine whether a StringLengthAttribute should be added.
        /// </summary>
        /// <param name="dbType">The DbType from the MetaDataMember.</param>
        /// <param name="attributes">The list of attributes to append to.</param>
        internal static void InferStringLengthAttribute(string dbType, List<Attribute> attributes)
        {
            if (dbType == null || dbType.Length <= 0)
            {
                return;
            }

            // we can assume that the SqlType if specified will be the first part of the string,
            // so the string will be of the form "NVarChar(80)", "char(15)", etc.
            string sqlType = dbType.Trim();
            int i = sqlType.IndexOf(' ');
            if (i != -1)
            {
                sqlType = sqlType.Substring(0, i);
            }
            i = sqlType.IndexOf("char(", StringComparison.OrdinalIgnoreCase);
            if (i != -1)
            {
                i += 5;
                int j = sqlType.IndexOf(")", i, StringComparison.Ordinal);
                if (j != -1)
                {
                    // if the portion between the parenthesis is integral
                    // add the attribute. Note that "VarChar(max)" will be
                    // skipped.
                    string stringLen = sqlType.Substring(i, j - i);
                    int maxLength;
                    if (int.TryParse(stringLen, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxLength))
                    {
                        attributes.Add(new StringLengthAttribute(maxLength));
                    }
                }
            }
        }
    }
}
