using Nerdbank.MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters
{
    internal sealed class ChangeSetEntryConverter(PolyType.ITypeShapeProvider typeShapeProvider) : MessagePackConverter<ChangeSetEntry?>
    {
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
                        result.Entity = ReadEntity(ref reader, context)!;
                        break;
                    case nameof(ChangeSetEntry.Id):
                        result.Id = reader.ReadInt32();
                        break;
                    case nameof(ChangeSetEntry.HasMemberChanges):
                        result.HasMemberChanges = reader.ReadBoolean();
                        break;
                    case nameof(ChangeSetEntry.Operation):
                        result.Operation = ReadValue<EntityOperationType>(ref reader, context)!;
                        break;
                    case nameof(ChangeSetEntry.EntityActions):
                        reader.Skip(context);
                        break;
                    case nameof(ChangeSetEntry.OriginalEntity):
                        result.OriginalEntity = ReadEntity(ref reader, context);
                        break;
                    case nameof(ChangeSetEntry.StoreEntity):
                        result.StoreEntity = ReadEntity(ref reader, context);
                        break;
                    case nameof(ChangeSetEntry.ValidationErrors):
                        result.ValidationErrors = ReadValue<IEnumerable<ValidationResultInfo>?>(ref reader, context);
                        break;
                    case nameof(ChangeSetEntry.ConflictMembers):
                        result.ConflictMembers = ReadValue<IEnumerable<string>>(ref reader, context);
                        break;
                    case nameof(ChangeSetEntry.IsDeleteConflict):
                        result.IsDeleteConflict = ReadValue<bool>(ref reader, context)!;
                        break;
                    case nameof(ChangeSetEntry.Associations):
                        result.Associations = ReadValue<IDictionary<string, int[]>?>(ref reader, context);
                        break;
                    case nameof(ChangeSetEntry.OriginalAssociations):
                        result.OriginalAssociations = ReadValue<IDictionary<string, int[]>?>(ref reader, context);
                        break;
                    default:
                        reader.Skip(context);
                        break;
                }
            }

            return result;
        }

        private static T? ReadValue<T>(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
                return default(T);

            return context.GetConverter<T>(context.TypeShapeProvider).Read(ref reader, context);
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
            WriteValue(ref writer, value.Id, context);
            writer.Write(nameof(ChangeSetEntry.HasMemberChanges));
            WriteValue(ref writer, value.HasMemberChanges, context);
            writer.Write(nameof(ChangeSetEntry.Operation));
            WriteValue(ref writer, value.Operation, context);
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
                WriteValue(ref writer, value.ValidationErrors, context);
            }

            if (value.ConflictMembers is not null)
            {
                writer.Write(nameof(ChangeSetEntry.ConflictMembers));
                WriteValue(ref writer, value.ConflictMembers, context);
            }

            if (value.IsDeleteConflict)
            {
                writer.Write(nameof(ChangeSetEntry.IsDeleteConflict));
                WriteValue(ref writer, value.IsDeleteConflict, context);
            }

            if (value.Associations is not null)
            {
                writer.Write(nameof(ChangeSetEntry.Associations));
                WriteValue(ref writer, value.Associations, context);
            }

            if (value.OriginalAssociations is not null)
            {
                writer.Write(nameof(ChangeSetEntry.OriginalAssociations));
                WriteValue(ref writer, value.OriginalAssociations, context);
            }
        }

        private void WriteEntityActions(ref MessagePackWriter writer, IList<OpenRiaServices.Serialization.KeyValue<string, object[]>>? value, Type? entityType, SerializationContext context)
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
                }
                else
                {
                    writer.WriteArrayHeader(2);
                    writer.Write(action.Key);
                    WriteParameters(ref writer, action.Value, entityType, action.Key, context);
                }
            }
        }

        private void WriteParameters(ref MessagePackWriter writer, object?[]? values, Type entityType, string methodName, SerializationContext context)
        {
            if (values is null)
            {
                writer.WriteNil();
                return;
            }

            MethodInfo customMethod = entityType.GetMethod(methodName) ?? EntityActionDoesNotExist(entityType, methodName);
            ParameterInfo[] parameters = customMethod.GetParameters();
            if (parameters.Length != values.Length)
                throw new InvalidOperationException($"Parameter count mismatch for method '{methodName}' on entity type '{entityType.FullName}'. Expected {parameters.Length} parameters but got {values.Length}.");

            writer.WriteArrayHeader(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is null)
                    writer.WriteNil();
                else
                    context.GetConverter(parameters[i].ParameterType, typeShapeProvider).WriteObject(ref writer, values[i], context);
            }
        }

        [DoesNotReturn]
        private static MethodInfo EntityActionDoesNotExist(Type entityType, string methodName)
        {
            throw new InvalidOperationException($"This DomainService does not support operation '{methodName}' for entity '{entityType.Name}'.");
        }

        private static void WriteValue<T>(ref MessagePackWriter writer, T value, SerializationContext context)
        {
            if (value is null)
                writer.WriteNil();
            else
                context.GetConverter<T>(context.TypeShapeProvider).Write(ref writer, value, context);
        }

        private Entity? ReadEntity(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            return (Entity?)context.GetConverter<object>(typeShapeProvider).ReadObject(ref reader, context);
        }

        private void WriteEntity(ref MessagePackWriter writer, Entity? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.GetConverter<object>(typeShapeProvider).WriteObject(ref writer, value, context);
        }
    }
}
