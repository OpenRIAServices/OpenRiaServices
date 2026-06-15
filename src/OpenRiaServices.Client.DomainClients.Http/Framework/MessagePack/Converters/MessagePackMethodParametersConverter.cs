using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients.Http;
using System;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters;

sealed class MessagePackMethodParametersConverter : MessagePackConverter<MessagePackMethodParameters>
{
    public override bool PreferAsyncSerialization => true;

    public override MessagePackMethodParameters Read(ref MessagePackReader reader, SerializationContext context)
    {
        throw new NotImplementedException();
    }

    public override void Write(ref MessagePackWriter writer, in MessagePackMethodParameters value, SerializationContext context)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        context.DepthStep();
        MethodParameters methodParameters = value.MethodParameters;
        writer.WriteMapHeader(value.Values.Count);

        foreach (var parameterValue in value.Values)
        {
            writer.Write(parameterValue.Key);
            Type parameterType = methodParameters.GetTypeForMethodParameter(parameterValue.Key);
            WriteValue(ref writer, parameterValue.Value, parameterType, context);
        }
    }

    public override async ValueTask<MessagePackMethodParameters> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
    {
        throw new NotImplementedException();
    }

    public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, MessagePackMethodParameters value, SerializationContext context)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        context.DepthStep();
        MethodParameters methodParameters = value.MethodParameters;
        writer.WriteMapHeader(value.Values.Count);

        foreach (var parameterValue in value.Values)
        {
            writer.Write(static (ref MessagePackWriter syncWriter, string key) => syncWriter.Write(key), parameterValue.Key);
            Type parameterType = methodParameters.GetTypeForMethodParameter(parameterValue.Key);
            await WriteValueAsync(writer, parameterValue.Value, parameterType, context).ConfigureAwait(false);
            await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
        }
    }

    private static void WriteValue(ref MessagePackWriter writer, object value, Type parameterType, SerializationContext context)
    {
        if (value is null)
            writer.WriteNil();
        else
            context.GetConverter(parameterType, context.TypeShapeProvider).WriteObject(ref writer, value, context);
    }

    private static ValueTask WriteValueAsync(MessagePackAsyncWriter writer, object value, Type parameterType, SerializationContext context)
        => value is null
            ? WriteNilAsync(writer)
            : context.GetConverter(parameterType, context.TypeShapeProvider).WriteObjectAsync(writer, value, context);

    private static ValueTask WriteNilAsync(MessagePackAsyncWriter writer)
    {
        writer.WriteNil();
        return default;
    }
}
