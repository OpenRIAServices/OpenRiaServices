using Nerdbank.MessagePack;
using OpenRiaServices.Server;
using PolyType;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Converters
{

    internal sealed class MethodParametersConverter : MessagePackConverter<MethodParameters?>
    {
        internal static readonly object OperationKey = new();

        public override bool PreferAsyncSerialization => true;

        public override MethodParameters? Read(ref MessagePackReader reader, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            // TODO: consider passing ITypeShapeProvider using context
            // So that generated type shapes can be used for "envelopes"
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            var result = new MethodParameters();

            int count = reader.ReadMapHeader();
            for (int i = 0; i < count; i++)
            {
                string? name = reader.ReadString();
                if (name is not null && parametersByName.TryGetValue(name, out DomainOperationParameter? parameter))
                {
                    Type type = parameter.ParameterType;
                    // HACK, TODO: Fix
                    if (type == typeof(ChangeSet))
                    {
                        type = typeof(System.Collections.Generic.IEnumerable<ChangeSetEntry>);
                    }

                    result.Values[name] = ReadValue(ref reader, type, context);
                }
                else
                {
                    reader.Skip(context);
                }
            }

            return result;
        }

        public override void Write(ref MessagePackWriter writer, in MethodParameters? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            writer.WriteMapHeader(value.Values.Count);

            foreach (var parameterValue in value.Values)
            {
                writer.Write(parameterValue.Key);
                if (parametersByName.TryGetValue(parameterValue.Key, out DomainOperationParameter? parameter))
                {
                    WriteValue(ref writer, parameterValue.Value, parameter.ParameterType, context);
                }
                else
                {
                    writer.WriteNil();
                }
            }
        }

        public override async ValueTask<MethodParameters?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
        {
            await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
            MessagePackReader bufferedReader = reader.CreateBufferedReader();
            if (bufferedReader.TryReadNil())
            {
                reader.ReturnReader(ref bufferedReader);
                return null;
            }

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            var result = new MethodParameters();

            int count = bufferedReader.ReadMapHeader();
            reader.ReturnReader(ref bufferedReader);

            for (int i = 0; i < count; i++)
            {
                await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
                bufferedReader = reader.CreateBufferedReader();
                string? name = bufferedReader.ReadString();
                reader.ReturnReader(ref bufferedReader);

                if (name is not null && parametersByName.TryGetValue(name, out DomainOperationParameter? parameter))
                {
                    result.Values[name] = await ReadValueAsync(reader, parameter.ParameterType, context).ConfigureAwait(false);
                }
                else
                {
                    await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
                    bufferedReader = reader.CreateBufferedReader();
                    bufferedReader.Skip(context);
                    reader.ReturnReader(ref bufferedReader);
                }
            }

            return result;
        }

        public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, MethodParameters? value, SerializationContext context)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            context.DepthStep();
            DomainOperationEntry operation = GetOperation(context);
            var parametersByName = operation.Parameters.ToDictionary(p => p.Name, StringComparer.Ordinal);
            writer.WriteMapHeader(value.Values.Count);

            foreach (var parameterValue in value.Values)
            {
                writer.Write(static (ref MessagePackWriter syncWriter, string key) => syncWriter.Write(key), parameterValue.Key);
                if (parametersByName.TryGetValue(parameterValue.Key, out DomainOperationParameter? parameter))
                {
                    await WriteValueAsync(writer, parameterValue.Value, parameter.ParameterType, context).ConfigureAwait(false);
                }
                else
                {
                    writer.WriteNil();
                }

                await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
            }
        }

        internal static DomainOperationEntry GetOperation(SerializationContext context)
            => (DomainOperationEntry?)context[OperationKey]
                ?? throw new MessagePackSerializationException("Domain operation metadata is required to serialize method parameters.");

        private static object? ReadValue(ref MessagePackReader reader, Type parameterType, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            return context.GetConverter(parameterType).ReadObject(ref reader, context);
        }

        private static async ValueTask<object?> ReadValueAsync(MessagePackAsyncReader reader, Type parameterType, SerializationContext context)
        {
            await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
            MessagePackReader bufferedReader = reader.CreateBufferedReader();
            if (bufferedReader.TryReadNil())
            {
                reader.ReturnReader(ref bufferedReader);
                return null;
            }

            reader.ReturnReader(ref bufferedReader);
            return await context.GetConverter(parameterType).ReadObjectAsync(reader, context).ConfigureAwait(false);
        }

        private static void WriteValue(ref MessagePackWriter writer, object? value, Type parameterType, SerializationContext context)
        {
            if (value is null)
                writer.WriteNil();
            else
                context.GetConverter(parameterType).WriteObject(ref writer, value, context);
        }

        private static ValueTask WriteValueAsync(MessagePackAsyncWriter writer, object? value, Type parameterType, SerializationContext context)
            => value is null
                ? WriteNilAsync(writer)
                : context.GetConverter(parameterType).WriteObjectAsync(writer, value, context);

        private static ValueTask WriteNilAsync(MessagePackAsyncWriter writer)
        {
            writer.WriteNil();
            return default;
        }
    }
}
