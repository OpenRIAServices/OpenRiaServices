using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Custom TypeDescriptionProvider conditionally registered for Types exposed by a <see cref="DomainService"/>.
    /// This provider is used to dynamically add properties or member attributes to Types.
    /// </summary>
    internal class DomainTypeDescriptionProvider : TypeDescriptionProvider
    {
        private readonly DomainServiceDescriptionProvider _descriptionProvider;
        private ICustomTypeDescriptor _customTypeDescriptor;
        private readonly Type _type;

        public DomainTypeDescriptionProvider(Type type, DomainServiceDescriptionProvider descriptionProvider)
            : base(TypeDescriptor.GetProvider(type))
        {
            if (descriptionProvider == null)
            {
                throw new ArgumentNullException(nameof(descriptionProvider));
            }

            this._type = type;
            this._descriptionProvider = descriptionProvider;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type type, object instance)
        {
            if (type == null && instance != null)
            {
                type = instance.GetType();
            }

            if (this._type != type)
            {
                // In inheritance scenarios, we might be called to provide a descriptor
                // for a derived Type. In that case, we just return base.
                return base.GetTypeDescriptor(type, instance);
            }

            if (this._customTypeDescriptor == null)
            {
                // CLR, buddy class type descriptors
                this._customTypeDescriptor = base.GetTypeDescriptor(type, instance);
 
                // EF, any other custom type descriptors provided through DomainServiceDescriptionProviders.
                this._customTypeDescriptor = this._descriptionProvider.GetTypeDescriptor(type, this._customTypeDescriptor);
                
                // initialize FK members AFTER our type descriptors have chained
                HashSet<string> foreignKeyMembers = this.GetForeignKeyMembers();

                // if any FK member of any association is also part of the primary key, then the key cannot be marked
                // Editable(false)
                bool keyIsEditable = false;
                foreach (PropertyDescriptor pd in this._customTypeDescriptor.GetProperties())
                {
                    if (pd.Attributes[typeof(KeyAttribute)] != null && 
                        foreignKeyMembers.Contains(pd.Name))
                    {
                        keyIsEditable = true;
                        break;
                    }
                }

                if (DomainTypeDescriptor.ShouldRegister(this._customTypeDescriptor, keyIsEditable, foreignKeyMembers))
                {
                    // Extend the chain with one more descriptor.
                    this._customTypeDescriptor = new DomainTypeDescriptor(type, this._customTypeDescriptor, keyIsEditable, foreignKeyMembers);
                }
            }

            return this._customTypeDescriptor;
        }

        /// <summary>
        /// Returns the set of all foreign key members for the entity.
        /// </summary>
        /// <returns>The set of foreign keys.</returns>
        private HashSet<string> GetForeignKeyMembers()
        {
            HashSet<string> foreignKeyMembers = new HashSet<string>();
            foreach (PropertyDescriptor pd in this._customTypeDescriptor.GetProperties())
            {
                AssociationAttribute assoc = (AssociationAttribute)pd.Attributes[typeof(AssociationAttribute)];
                if (assoc != null && assoc.IsForeignKey)
                {
                    foreach (string foreignKeyMember in assoc.ThisKeyMembers)
                    {
                        foreignKeyMembers.Add(foreignKeyMember);
                    }
                }
            }

            return foreignKeyMembers;
        }
    }
}
