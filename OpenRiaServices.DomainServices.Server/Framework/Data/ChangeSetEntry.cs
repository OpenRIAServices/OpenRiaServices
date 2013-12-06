using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Represents a domain operation to be performed on an entity. This is the message
    /// type passed between DomainClient and <see cref="DomainService"/> both for sending operations to
    /// the <see cref="DomainService"/> as well as for returning operation results back to the DomainClient.
    /// </summary>
    [DataContract(Namespace = "DomainServices")]
    [DebuggerDisplay("Operation = {Operation}, Type = {Entity.GetType().Name}")]
    public sealed class ChangeSetEntry
    {
        private object _entity;
        private int _id;
        private DomainOperation _operation;
        private object _originalEntity;
        private object _storeEntity;
        private bool _hasMemberChanges;

        /// <summary>
        /// Constructs an instance of <see cref="ChangeSetEntry"/>
        /// </summary>
        public ChangeSetEntry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetEntry"/> class
        /// </summary>
        /// <param name="id">The client Id for the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="originalEntity">The original entity. May be null.</param>
        /// <param name="operation">The operation to be performed</param>
        public ChangeSetEntry(int id, object entity, object originalEntity, DomainOperation operation)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            this._id = id;
            this._entity = entity;
            // Setting original entity through a property so that _hasMemberChanges is set to correct value
            this.OriginalEntity = originalEntity;
            this.Operation = operation;
        }

        /// <summary>
        /// Gets a value indicating whether any errors were encountered 
        /// during processing of the operation.
        /// </summary>
        public bool HasError
        {
            get
            {
                return this.HasConflict || (this.ValidationErrors != null && this.ValidationErrors.Any());
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ChangeSetEntry"/> contains conflicts.
        /// </summary>
        public bool HasConflict
        {
            get
            {
                return (this.IsDeleteConflict || (this.ConflictMembers != null && this.ConflictMembers.Any()));
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Entity"/> being operated on
        /// </summary>
        [DataMember]
        public object Entity
        {
            get
            {
                return this._entity;
            }
            set
            {
                this._entity = value;
            }
        }

        /// <summary>
        /// Gets or sets the client ID for the entity
        /// </summary>
        [DataMember]
        public int Id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity for
        /// this operation has property modifications.
        /// <remarks>Note that even if OriginalEntity hasn't been
        /// set, in the case of entities using a timestamp member
        /// for concurrency, the entity may still be modified. This
        /// flag allows us to distinguish that case from an Update
        /// operation that represents a custom method invocation only.
        /// </remarks>
        /// </summary>
        [DataMember]
        public bool HasMemberChanges
        {
            get
            {
                return this._hasMemberChanges;
            }
            set
            {
                this._hasMemberChanges = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="DomainOperation"/> to be performed on the entity.
        /// </summary>
        [DataMember]
        public DomainOperation Operation
        {
            get
            {
                return this._operation;
            }
            set
            {
                if (!(value == DomainOperation.Query || value == DomainOperation.Insert ||
                      value == DomainOperation.Update || value == DomainOperation.Delete ||
                      value == DomainOperation.None))
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        Resource.InvalidDomainOperationType, Enum.GetName(typeof(DomainOperation), value), "value"));
                }

                this._operation = value;
            }
        }

        /// <summary>
        /// Gets or sets the custom methods invoked on the entity, as a set
        /// of method name / parameter set pairs.
        /// </summary>
        [DataMember]
        public IDictionary<string, object[]> EntityActions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the original state of the entity being operated on
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public object OriginalEntity
        {
            get
            {
                return this._originalEntity;
            }
            set
            {
                this._originalEntity = value;

                if (value != null)
                {
                    this._hasMemberChanges = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the state of the entity in the data store
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public object StoreEntity
        {
            get
            {
                return this._storeEntity;
            }
            set
            {
                this._storeEntity = value;
            }
        }

        /// <summary>
        /// Gets or sets the validation errors encountered during the processing of the operation. 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IEnumerable<ValidationResultInfo> ValidationErrors
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of members in conflict. The <see cref="StoreEntity"/> property
        /// contains the current store value for each property.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IEnumerable<string> ConflictMembers
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether the conflict is a delete conflict, meaning the
        /// entity no longer exists in the store.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IsDeleteConflict
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of IDs of the associated entities for
        /// each association of the Entity
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<string, int[]> Associations
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of IDs for each association of the <see cref="OriginalEntity"/>
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<string, int[]> OriginalAssociations
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="DomainOperationEntry"/> for the operation. May be
        /// null if the operation is for a composed Type that doesn't have an explicit
        /// operation defined.
        /// </summary>
        internal DomainOperationEntry DomainOperationEntry
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the parent operation for composed operations.
        /// </summary>
        internal ChangeSetEntry ParentOperation
        {
            get;
            set;
        }
        ///// <summary>
        ///// Gets or sets the string identity of the Entity
        ///// </summary>
        //[DataMember]
        //public string Identity { get; set; }
    }
}
