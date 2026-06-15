using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Nerdbank.MessagePack;
using OpenRiaServices.Server;
using PolyType;

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
                    result.Values[name] = ReadValue(ref reader, parameter.ParameterType, context);
                }
                else
                {
                    reader.Skip(context);
                }
            }

            return result;
        }

        public override void Write(ref MessagePackWriter writer, in MethodParameters? value, SerializationContext context)
            => throw new NotImplementedException("Can only read method parameters.");

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

        internal static DomainOperationEntry GetOperation(SerializationContext context)
            => (DomainOperationEntry?)context[OperationKey]
                ?? throw new MessagePackSerializationException("Domain operation metadata is required to serialize method parameters.");

        private static object? ReadValue(ref MessagePackReader reader, Type parameterType, SerializationContext context)
        {
            if (reader.TryReadNil())
                return null;

            if (parameterType == typeof(ChangeSet))
            {
                parameterType = typeof(System.Collections.Generic.IEnumerable<ChangeSetEntry>);
            }

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

            if (parameterType == typeof(ChangeSet))
            {
                parameterType = typeof(System.Collections.Generic.IEnumerable<ChangeSetEntry>);
            }
            return await context.GetConverter(parameterType).ReadObjectAsync(reader, context).ConfigureAwait(false);
        }
    }
}
