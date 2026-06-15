using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Nerdbank.MessagePack;

#nullable enable

namespace AspNetCoreWebsite.MessagePack;

/// <summary>
/// Converts a <see cref="XElement"/> object to and from MessagePack as plain text
/// </summary>
internal class XElementConverter : MessagePackConverter<XElement>
{
    public override void Write(ref MessagePackWriter writer, in XElement? value, SerializationContext context)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.Write(value.ToString(SaveOptions.DisableFormatting));
    }

    public override XElement? Read(ref MessagePackReader reader, SerializationContext context)
    {
        if (reader.TryReadNil())
            return null;

        return XElement.Parse(reader.ReadString()!, LoadOptions.PreserveWhitespace);
    }
}
