using Nerdbank.MessagePack;
using OpenRiaServices.Client;
using OpenRiaServices.Client.Internal;
using PolyType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters
{
    /**
    // ChangeSetEntryConverter v2
      Entry = ["entity.full.type.name", [changesetentry_surrogate]]
      on read: will set entity type in SerializationContext
       * todo: Cache serializer or context ??

    Can do and register converter per entity ?

    // [MessagePackConverter(typeof(MyConverter {}))] is maybe not a good idea
    //  use CustomConverter ChangeSetEntry and delegate to base class

    class ChangeSetEntry{TEntity} : ChangeSetEntry
    {
       public new TEntity Entity;
       public new IList{EntityAction{TEntity}} EntityActions;
    };

    [Converter(EntityActionConverter{})]
    class EntityAction{TEntity}
    {
    }
    EntityActionConverter{}
    {


    changeset_surrogate is like ChangeSetEntry
    * maybe generic typed ChangeSetEntrySurrogate{TEntity} ??? (how does it affect serializer cache etc)
    *   * otherwise use "KnownTypeConverter/ObjectConverter" that support all EntityTypes (fullname as discriminator)
    * has list of EntityAction instead of KeyValuePair {string, object[]}
      *  EntityAction  is (string MethodName, Dictionary{string, object})
        where Dictionary{string, object} should use existing MethodParametersConverter


    */

    // TODO: FIX THIS
    // AI Generated code
    internal sealed class ChangeSetEntryConverter : MessagePackConverter<ChangeSetEntry?>
    {
        // TODO: Can use context.Security.MaxCollectionPreallocation instead
        private const int MaxPreallocation = 50;

        public override bool PreferAsyncSerialization => false;

        public override ChangeSetEntry? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            context.DepthStep();
            var result = new ChangeSetEntry(default, default, default);
            int count = reader.ReadMapHeader();

            for (int i = 0; i < count; i++)
            {
                string? name = reader.ReadString();
                switch (name)
                {
                    case nameof(ChangeSetEntry.Entity):
                        result.Entity = (Entity)ReadValue(ref reader, typeof(Entity), context)!;
                        break;
                    case nameof(ChangeSetEntry.Id):
                        result.Id = reader.ReadInt32();
                        break;
                    case nameof(ChangeSetEntry.HasMemberChanges):
                        result.HasMemberChanges = reader.ReadBoolean();
                        break;
                    case nameof(ChangeSetEntry.Operation):
                        result.Operation = (EntityOperationType)ReadValue(ref reader, typeof(EntityOperationType), context)!;
                        break;
                    case nameof(ChangeSetEntry.EntityActions):
                        reader.Skip(context);
                        break;
                    case nameof(ChangeSetEntry.OriginalEntity):
                        result.OriginalEntity = (Entity?)ReadValue(ref reader, typeof(Entity), context);
                        break;
                    case nameof(ChangeSetEntry.StoreEntity):
                        result.StoreEntity = (Entity?)ReadValue(ref reader, typeof(Entity), context);
                        break;
                    case nameof(ChangeSetEntry.ValidationErrors):
                        result.ValidationErrors = (IEnumerable<ValidationResultInfo>?)ReadValue(ref reader, typeof(IEnumerable<ValidationResultInfo>), context);
                        break;
                    case nameof(ChangeSetEntry.ConflictMembers):
                        result.ConflictMembers = (IEnumerable<string>?)ReadValue(ref reader, typeof(IEnumerable<string>), context);
                        break;
                    case nameof(ChangeSetEntry.IsDeleteConflict):
                        result.IsDeleteConflict = (bool)ReadValue(ref reader, typeof(bool), context)!;
                        break;
                    case nameof(ChangeSetEntry.Associations):
                        result.Associations = (IDictionary<string, int[]>?)ReadValue(ref reader, typeof(IDictionary<string, int[]>), context);
                        break;
                    case nameof(ChangeSetEntry.OriginalAssociations):
                        result.OriginalAssociations = (IDictionary<string, int[]>?)ReadValue(ref reader, typeof(IDictionary<string, int[]>), context);
                        break;
                    default:
                        reader.Skip(context);
                        break;
                }
            }

            return result;
        }

        private static object? ReadValue(ref MessagePackReader reader, Type type, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            return context.GetConverter(type).ReadObject(ref reader, context);
        }

        public override void Write(ref MessagePackWriter writer, in ChangeSetEntry? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();

            int count = 5;
            if (value.OriginalEntity is not null)
                count++;
            if (value.StoreEntity is not null)
                count++;
            if (value.ValidationErrors is not null)
                count++;
            if (value.ConflictMembers is not null)
                count++;
            if (value.IsDeleteConflict)
                count++;
            if (value.Associations is not null)
                count++;
            if (value.OriginalAssociations is not null)
                count++;

            writer.WriteMapHeader(count);
            writer.Write(nameof(ChangeSetEntry.Entity));
            WriteEntity(ref writer, value.Entity, context);
            writer.Write(nameof(ChangeSetEntry.Id));
            WriteValue(ref writer, value.Id, typeof(int), context);
            writer.Write(nameof(ChangeSetEntry.HasMemberChanges));
            WriteValue(ref writer, value.HasMemberChanges, typeof(bool), context);
            writer.Write(nameof(ChangeSetEntry.Operation));
            WriteValue(ref writer, value.Operation, typeof(EntityOperationType), context);
            writer.Write(nameof(ChangeSetEntry.EntityActions));
            WriteEntityActions(ref writer, value.EntityActions, value.Entity?.GetType(), context);

            if (value.OriginalEntity is not null)
            {
                writer.Write(nameof(ChangeSetEntry.OriginalEntity));
                WriteEntity(ref writer, value.OriginalEntity, context);
            }

            if (value.StoreEntity is not null)
            {
                writer.Write(nameof(ChangeSetEntry.StoreEntity));
                WriteEntity(ref writer, value.StoreEntity, context);
            }

            if (value.ValidationErrors is not null)
            {
                writer.Write(nameof(ChangeSetEntry.ValidationErrors));
                WriteValue(ref writer, value.ValidationErrors, typeof(IEnumerable<ValidationResultInfo>), context);
            }

            if (value.ConflictMembers is not null)
            {
                writer.Write(nameof(ChangeSetEntry.ConflictMembers));
                WriteValue(ref writer, value.ConflictMembers, typeof(IEnumerable<string>), context);
            }

            if (value.IsDeleteConflict)
            {
                writer.Write(nameof(ChangeSetEntry.IsDeleteConflict));
                WriteValue(ref writer, value.IsDeleteConflict, typeof(bool), context);
            }

            if (value.Associations is not null)
            {
                writer.Write(nameof(ChangeSetEntry.Associations));
                WriteValue(ref writer, value.Associations, typeof(IDictionary<string, int[]>), context);
            }

            if (value.OriginalAssociations is not null)
            {
                writer.Write(nameof(ChangeSetEntry.OriginalAssociations));
                WriteValue(ref writer, value.OriginalAssociations, typeof(IDictionary<string, int[]>), context);
            }
        }

        private static void WriteEntityActions(ref MessagePackWriter writer, IList<OpenRiaServices.Serialization.KeyValue<string, object[]>>? value, Type? entityType, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            if (entityType is null)
                throw new MessagePackSerializationException("Entity metadata is required to serialize entity action parameters.");

            writer.WriteArrayHeader(value.Count);
            foreach (var action in value)
            {
                if (action.Value.Length == 0)
                {
                    // can write as string if no parameters
                    writer.WriteArrayHeader(1);
                    writer.Write(action.Key);
                    continue;
                }
                else
                {
                    writer.WriteArrayHeader(2);
                    writer.Write(action.Key);
                    WriteParameters(ref writer, action.Value, entityType, action.Key, context);
                }
            }
        }

        private static void WriteParameters(ref MessagePackWriter writer, object?[]? values, Type entityType, string methodName, SerializationContext context)
        {
            if (values is null)
            {
                writer.WriteNil();
                return;
            }

            MethodInfo customMethod = entityType.GetMethod(methodName) ?? throw new InvalidOperationException("Could not get entity action parameters");
            ParameterInfo[] parameters = customMethod.GetParameters();
            if (parameters.Length != values.Length)
                throw new MessagePackSerializationException($"Parameter count mismatch for method '{methodName}' on entity type '{entityType.FullName}'. Expected {parameters.Length} parameters but got {values.Length}.");

            writer.WriteArrayHeader(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                WriteValue(ref writer, values[i], parameterType, context);
            }
        }
        private static void WriteValue(ref MessagePackWriter writer, object? value, Type type, SerializationContext context)
        {
            if (value is null)
                writer.WriteNil();
            else
                context.GetConverter(type).WriteObject(ref writer, value, context);
        }

        private static void WriteEntity(ref MessagePackWriter writer, Entity? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }
            context.GetConverter(typeof(Entity)).WriteObject(ref writer, value, context);
        }
    }
}
