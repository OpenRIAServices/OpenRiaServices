using System;
using System.Collections.Generic;
using System.Linq;
using Nerdbank.MessagePack;
using PolyType;

#pragma warning disable NBMsgPack030 // Converters should not call top-level `MessagePackSerializer` methods
#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters
{
    /// <summary>
    /// Provides a MessagePack serializer that can retrieve converters for specified types.
    /// </summary>
    record class CustomMessagePackSerializer : MessagePackSerializer
    {
        private readonly ITypeShapeProvider _typeShapeProvider;

        public CustomMessagePackSerializer(MessagePackSerializer original, ITypeShapeProvider typeShapeProvider) : base(original)
        {
            _typeShapeProvider = typeShapeProvider;
        }

        public List<KeyValuePair<Type, MessagePackConverter>> CopyConverters(IEnumerable<Type> types)
        {
            using var scope = this.CreateSerializationContext(_typeShapeProvider);

            return types.Select(x =>
            {
                ITypeShape typeShape = _typeShapeProvider.GetTypeShapeOrThrow(x);
                if (typeShape is PolyType.Abstractions.IUnionTypeShape union)
                    typeShape = union.BaseType;
                return new KeyValuePair<Type, MessagePackConverter>(x, scope.Value.GetConverter(typeShape));
            }).ToList();
        }
    }
}
