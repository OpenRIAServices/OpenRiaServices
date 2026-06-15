using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MessagePack;

#nullable enable

namespace AspNetCoreWebsite.MessagePack;

/// <summary>
/// Converts a <see cref="Binary"/> object to and from MessagePack.
/// </summary>
/// <remarks>
/// <see cref="Binary"/> is declared in the "System.Data.Linq.Data" package.
/// </remarks>
internal class BinaryConverter : MessagePackConverter<Binary>
{
    // Note: MessagePack for C# does not support custom formatters for System.Memory types, so we need to convert to byte[] here.
    // Dynamically construct 
    public override void Write(ref MessagePackWriter writer, in Binary? value, SerializationContext context)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.Write(CollectionsMarshal.AsSpan(value));
    }

    public override Binary? Read(ref MessagePackReader reader, SerializationContext context)
    {
        return reader.ReadBytes() is { } bytes ? new Binary(System.Buffers.BuffersExtensions.ToArray(bytes)) : null;
    }
}
