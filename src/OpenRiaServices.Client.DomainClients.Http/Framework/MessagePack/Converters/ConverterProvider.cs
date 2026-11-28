using System;
using System.Collections.Generic;
using System.Linq;
using Nerdbank.MessagePack;
using PolyType;

#nullable enable

namespace OpenRiaServices.Client.DomainClients.MessagePack.Converters
{
    /// <summary>
    /// Provides a MessagePack serializer that can retrieve converters for specified types.
    /// </summary>
    record class ConverterProvider : MessagePackSerializer
    {
        private readonly ITypeShapeProvider _typeShapeProvider;

        public ConverterProvider(MessagePackSerializer original, ITypeShapeProvider typeShapeProvider) : base(original)
        {
            _typeShapeProvider = typeShapeProvider;
        }

        /// <summary>
        /// Gets the converters for the specified types.
        /// </summary>
        /// <param name="types">The types to get converters for.</param>
        /// <returns>A list of key-value pairs containing each specified type and its converter.</returns>
        public List<KeyValuePair<Type, MessagePackConverter>> GetConverters(IEnumerable<Type> types)
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
