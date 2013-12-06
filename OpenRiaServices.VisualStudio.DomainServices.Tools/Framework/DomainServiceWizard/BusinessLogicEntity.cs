using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Class the encapsulates the information for a single entity within a <see cref="BusinessLogicContext"/>
    /// </summary>
    /// <remarks>Different domain service types are expected to subclass this for their own entity types.
    /// </remarks>
    public class BusinessLogicEntity : IBusinessLogicEntity
    {
        private IBusinessLogicContext _businessLogicContext;
        private IEntityData _entityData;
        private string _name;
        private Type _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessLogicEntity"/> class.
        /// </summary>
        /// <param name="businessLogicContext">The context that owns this entity.  It cannot be null.</param>
        /// <param name="name">The name of the entity as it will appear in the UI</param>
        /// <param name="type">The CLR type of the entity as it will be used in code gen</param>
        public BusinessLogicEntity(IBusinessLogicContext businessLogicContext, string name, Type type)
        {
            this._businessLogicContext = businessLogicContext;
            this._name = name;
            this._type = type;
        }

        /// <summary>
        /// Gets or sets the data object used to share state across
        /// AppDomain boundaries with <see cref="EntityViewModel"/>.
        /// </summary>
        public IEntityData EntityData
        {
            get
            {
                if (this._entityData == null)
                {
                    this._entityData = new EntityData()
                    {
                        Name = this._name,
                        IsIncluded = false,
                        IsEditable = false,
                        CanBeIncluded = this.CanBeIncluded,
                        CanBeEdited = this.CanBeEdited,
                        AssemblyName = this.AssemblyName
                    };
                }
                return this._entityData;
            }
            set
            {
                this._entityData = value;
            }
        }

        /// <summary>
        /// Gets the BusinessLogicContext that owns this entity
        /// </summary>
        public IBusinessLogicContext BusinessLogicContext
        {
            get
            {
                return this._businessLogicContext;
            }
        }

        /// <summary>
        /// Gets the user visible name of the entity
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }
        }

        /// <summary>
        /// Gets the CLR type of the entity
        /// </summary>
        public Type ClrType
        {
            get
            {
                return this._type;
            }
        }

        /// <summary>
        /// Gets the short name of the assembly to which this entity type belongs.
        /// </summary>
        public string AssemblyName
        {
            get
            {
                return this.ClrType.Assembly.GetName().Name;
            }
        }

        /// <summary>
        /// Gets and sets a value indicating whether this
        /// entity should be included in code generation.
        /// </summary>
        public bool IsIncluded
        {
            get
            {
                return this.EntityData.IsIncluded;
            }
            set
            {
                this.EntityData.IsIncluded = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the code generator
        /// should generate CUD methods for this entity.
        /// </summary>
        public bool IsEditable
        {
            get
            {
                return this.EntityData.IsEditable;
            }
            set
            {
                this.EntityData.IsEditable = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether it is legal to include this entity
        /// </summary>
        public virtual bool CanBeIncluded
        {
            get
            {
                // TEntity is defined as "Entity, new()" for some types,
                // see OpenRiaServices.DomainServices.Server.ChangeSet.GetOriginal for an example.
                return CodeGenUtilities.IsValidGenericTypeParam(this.ClrType);
            }
        }

        /// <summary>
        /// Gets a value indicating whether it is legal for this entity to be edited.
        /// </summary>
        public virtual bool CanBeEdited
        {
            get
            {
                // Returns false if the type is marked [Editable(false)]
                EditableAttribute editableAttribute = this.GetTypeCustomAttributes().OfType<EditableAttribute>().FirstOrDefault();
                bool canBeEdited = editableAttribute == null || editableAttribute.AllowEdit;
                return canBeEdited && this.CanBeIncluded;
            }
        }

        /// <summary>
        /// Returns the set of type-level custom attributes for the entity's CLR type.
        /// </summary>
        /// <remarks>This set is aware of buddy classes and will add in any buddy class attributes</remarks>
        /// <returns>The collection of attributes.   It may be empty but it will not be null.</returns>
        private IEnumerable<Attribute> GetTypeCustomAttributes()
        {
            List<Attribute> attributes = this.ClrType.GetCustomAttributes(true).OfType<Attribute>().ToList();
            Type metadataClassType = TypeUtilities.GetAssociatedMetadataType(this.ClrType);
            if (metadataClassType != null)
            {
                attributes.AddRange(metadataClassType.GetCustomAttributes(true).OfType<Attribute>());
            }
            return attributes;
        }
    }
}
