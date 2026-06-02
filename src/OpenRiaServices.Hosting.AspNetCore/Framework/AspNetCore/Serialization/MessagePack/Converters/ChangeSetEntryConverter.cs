using Nerdbank.MessagePack;
using OpenRiaServices.Server;
using PolyType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters
{
    // TODO: FIX THIS
    // AI Generated code
    internal sealed class ChangeSetEntryConverter : MessagePackConverter<ChangeSetEntry?>
    {
        private const int MaxPreallocation = 50;

        public override bool PreferAsyncSerialization => false;

        public override ChangeSetEntry? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            context.DepthStep();
            var result = new ChangeSetEntry();
            int count = reader.ReadMapHeader();

            for (int i = 0; i < count; i++)
            {
                string? name = reader.ReadString();
                switch (name)
                {
                    case nameof(ChangeSetEntry.Entity):
                        result.Entity = ReadValue(ref reader, typeof(object), context)!;
                        break;
                    case nameof(ChangeSetEntry.Id):
                        result.Id = reader.ReadInt32();
                        break;
                    case nameof(ChangeSetEntry.HasMemberChanges):
                        result.HasMemberChanges = reader.ReadBoolean();
                        break;
                    case nameof(ChangeSetEntry.Operation):
                        result.Operation = (DomainOperation)ReadValue(ref reader, typeof(DomainOperation), context)!;
                        break;
                    case nameof(ChangeSetEntry.EntityActions):
                        result.EntityActions = ReadEntityActions(ref reader, result.Entity?.GetType(), context);
                        break;
                    case nameof(ChangeSetEntry.OriginalEntity):
                        result.OriginalEntity = ReadValue(ref reader, typeof(object), context);
                        break;
                    case nameof(ChangeSetEntry.StoreEntity):
                        result.StoreEntity = ReadValue(ref reader, typeof(object), context);
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
            WriteValue(ref writer, value.Entity, typeof(object), context);
            writer.Write(nameof(ChangeSetEntry.Id));
            WriteValue(ref writer, value.Id, typeof(int), context);
            writer.Write(nameof(ChangeSetEntry.HasMemberChanges));
            WriteValue(ref writer, value.HasMemberChanges, typeof(bool), context);
            writer.Write(nameof(ChangeSetEntry.Operation));
            WriteValue(ref writer, value.Operation, typeof(DomainOperation), context);
            writer.Write(nameof(ChangeSetEntry.EntityActions));
            WriteEntityActions(ref writer, value.EntityActions, value.Entity?.GetType(), context);

            if (value.OriginalEntity is not null)
            {
                writer.Write(nameof(ChangeSetEntry.OriginalEntity));
                WriteValue(ref writer, value.OriginalEntity, typeof(object), context);
            }

            if (value.StoreEntity is not null)
            {
                writer.Write(nameof(ChangeSetEntry.StoreEntity));
                WriteValue(ref writer, value.StoreEntity, typeof(object), context);
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

        private static List<OpenRiaServices.Serialization.KeyValue<string, object[]>>? ReadEntityActions(ref MessagePackReader reader, Type? entityType, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            if (entityType is null)
                throw new MessagePackSerializationException("Entity metadata is required to deserialize entity action parameters.");

            DomainServiceDescription description = DomainServiceDescription.GetDescription(MethodParametersConverter.GetOperation(context).DomainServiceType);
            int count = reader.ReadArrayHeader();
            var actions = new List<OpenRiaServices.Serialization.KeyValue<string, object[]>>(Math.Min(count, MaxPreallocation));

            for (int i = 0; i < count; i++)
            {
                string? key = null;
                object[]? value = null;
                // TODO: ReadArray here and validate count is 1 or 2
                int memberCount = reader.ReadMapHeader();

                // TODO: Change to array? [["action", {"name": value1, "param2": value2}], ..]
                for (int j = 0; j < memberCount; j++)
                {
                    string? memberName = reader.ReadString();
                    if (memberName == nameof(OpenRiaServices.Serialization.KeyValue<string, object[]>.Key))
                    {
                        key = reader.ReadString();
                    }
                    else if (memberName == nameof(OpenRiaServices.Serialization.KeyValue<string, object[]>.Value))
                    {
                        if (key is null)
                            throw new MessagePackSerializationException("Entity action key must be read before its value.");

                        value = ReadParameters(ref reader, description, entityType, key, context);
                    }
                    else
                    {
                        reader.Skip(context);
                    }
                }

                if (key is null)
                    throw new MessagePackSerializationException("Entity action key is required.");

                actions.Add(new OpenRiaServices.Serialization.KeyValue<string, object[]>(key, value ?? Array.Empty<object>()));
            }

            return actions;
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

            DomainServiceDescription description = DomainServiceDescription.GetDescription(MethodParametersConverter.GetOperation(context).DomainServiceType);
            writer.WriteArrayHeader(value.Count);

            foreach (var action in value)
            {
                writer.WriteMapHeader(2);
                writer.Write(nameof(OpenRiaServices.Serialization.KeyValue<string, object[]>.Key));
                writer.Write(action.Key);
                writer.Write(nameof(OpenRiaServices.Serialization.KeyValue<string, object[]>.Value));
                WriteParameters(ref writer, action.Value, description, entityType, action.Key, context);
            }
        }

        private static object[] ReadParameters(ref MessagePackReader reader, DomainServiceDescription description, Type entityType, string methodName, SerializationContext context)
        {
            if (reader.TryReadNil())
                return Array.Empty<object>();

            DomainOperationEntry customMethod = GetCustomMethod(description, entityType, methodName);
            DomainOperationParameter[] parameters = customMethod.Parameters.Skip(1).ToArray();
            int count = reader.ReadArrayHeader();
            object[] result = new object[parameters.Length];

            for (int i = 0; i < count; i++)
            {
                if (i < parameters.Length)
                    result[i] = ReadValue(ref reader, parameters[i].ParameterType, context)!;
                else
                    reader.Skip(context);
            }

            return result;
        }

        private static void WriteParameters(ref MessagePackWriter writer, object[]? values, DomainServiceDescription description, Type entityType, string methodName, SerializationContext context)
        {
            if (values is null)
            {
                writer.WriteNil();
                return;
            }

            DomainOperationEntry customMethod = GetCustomMethod(description, entityType, methodName);
            DomainOperationParameter[] parameters = customMethod.Parameters.Skip(1).ToArray();
            writer.WriteArrayHeader(values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                Type parameterType = i < parameters.Length ? parameters[i].ParameterType : values[i]?.GetType() ?? typeof(object);
                WriteValue(ref writer, values[i], parameterType, context);
            }
        }

        private static DomainOperationEntry GetCustomMethod(DomainServiceDescription description, Type entityType, string methodName)
            => description.GetCustomMethod(entityType, methodName)
                ?? throw new MessagePackSerializationException($"Custom method '{methodName}' was not found for entity type '{entityType.FullName}'.");

        private static object? ReadValue(ref MessagePackReader reader, Type type, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            return context.GetConverter(type).ReadObject(ref reader, context);
        }

        private static void WriteValue(ref MessagePackWriter writer, object? value, Type type, SerializationContext context)
        {
            if (value is null)
                writer.WriteNil();
            else
                context.GetConverter(type).WriteObject(ref writer, value, context);
        }
    }
}
