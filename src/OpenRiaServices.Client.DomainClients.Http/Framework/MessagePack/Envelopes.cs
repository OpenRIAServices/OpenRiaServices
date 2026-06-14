using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack
{
    abstract class MessagePackRequestEnvelopeBase
    {
        public MessagePackMethodParameters? Parameters { get; set; }
    }

    sealed class MessagePackQueryRequestEnvelope : MessagePackRequestEnvelopeBase
    {
        public List<ServiceQueryPart>? QueryOptions { get; set; }
        public bool IncludeTotalCount { get; set; }
    }

    sealed class MessagePackInvokeRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    sealed class MessagePackSubmitRequestEnvelope : MessagePackRequestEnvelopeBase
    {
    }

    sealed class ChangeSetEntry<T>()
        where T : Entity
    {
        private readonly ChangeSetEntry _changeSetEntry;

        public ChangeSetEntry(ChangeSetEntry changeSetEntry)
            : this()
        {
            this._changeSetEntry = changeSetEntry;
        }

        public T Entity
        {
            get => (T)_changeSetEntry.Entity;
            set => _changeSetEntry.Entity = value;
        }

        public T OriginalEntity
        {
            get => (T)_changeSetEntry.OriginalEntity;
            set => _changeSetEntry.OriginalEntity = value;
        }

        public T? StoreEntity
        {
            get => (T?)_changeSetEntry.StoreEntity;
            set => _changeSetEntry.StoreEntity = value;
        }

        public int Id
        {
            get => _changeSetEntry.Id;
            set => _changeSetEntry.Id = value;
        }

        public bool HasMemberChanges
        {
            get => _changeSetEntry.HasMemberChanges;
            set => _changeSetEntry.HasMemberChanges = value;
        }

        public EntityOperationType Operation
        {
            get => _changeSetEntry.Operation;
            set => _changeSetEntry.Operation = value;
        }

        public IList<Serialization.KeyValue<string, object[]>>? EntityActions
        {
            get => _changeSetEntry.EntityActions;
            set => _changeSetEntry.EntityActions = value;
        }

        public IEnumerable<ValidationResultInfo>? ValidationErrors
        {
            get => _changeSetEntry.ValidationErrors;
            set => _changeSetEntry.ValidationErrors = value;
        }

        public IEnumerable<string>? ConflictMembers
        {
            get => _changeSetEntry.ConflictMembers;
            set => _changeSetEntry.ConflictMembers = value;
        }

        public bool IsDeleteConflict
        {
            get => _changeSetEntry.IsDeleteConflict;
            set => _changeSetEntry.IsDeleteConflict = value;
        }

        public IDictionary<string, int[]>? Associations
        {
            get => _changeSetEntry.Associations;
            set => _changeSetEntry.Associations = value;
        }

        public IDictionary<string, int[]>? OriginalAssociations
        {
            get => _changeSetEntry.OriginalAssociations;
            set => _changeSetEntry.OriginalAssociations = value;
        }
    }

    abstract class MessagePackResponseEnvelopeBase
    {
        public DomainServiceFault? Fault { get; set; }
        public abstract object? GetResult();
    }


    sealed class MessagePackQueryResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public TResult? Result { get; set; }
        public override object? GetResult() => Result;
    }

    sealed class MessagePackInvokeResponseEnvelope<TResult> : MessagePackResponseEnvelopeBase
    {
        public TResult? Result { get; set; }
        public override object? GetResult() => Result;
    }

    sealed class MessagePackSubmitResponseEnvelope : MessagePackResponseEnvelopeBase
    {
        public IEnumerable<ChangeSetEntry?>? Result { get; set; }
        public override object? GetResult() => Result;
    }
}
