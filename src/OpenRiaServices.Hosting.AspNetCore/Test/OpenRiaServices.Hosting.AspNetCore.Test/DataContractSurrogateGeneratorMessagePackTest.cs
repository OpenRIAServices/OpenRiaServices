using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using PolyType;
using PolyType.Abstractions;
using PolyType.ReflectionProvider;
using TestDomainServices;

namespace OpenRiaServices.Hosting.UnitTests
{
    [TestClass]
    public class DataContractSurrogateGeneratorMessagePackTest
    {
        [TestMethod]
        public void PolyTypePropertyShapePropagation()
        {
            Type entityType = typeof(SurrogateTestEntity_PropertyShape);
            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(new HashSet<Type> { entityType }, entityType);
            ITypeShapeProvider provider = ReflectionTypeShapeProvider.Default;
            IObjectTypeShape entityShape = (IObjectTypeShape)provider.GetTypeShape(entityType);
            IObjectTypeShape surrogateShape = (IObjectTypeShape)provider.GetTypeShape(surrogateType);

            CollectionAssert.AreEqual(
                entityShape.Properties.Select(property => property.Name).ToArray(),
                surrogateShape.Properties.Select(property => property.Name).ToArray());
            Assert.IsNotNull(surrogateType.GetCustomAttribute<TypeShapeAttribute>());
            Assert.AreEqual(
                entityType.GetProperty(nameof(SurrogateTestEntity_PropertyShape.Renamed)).GetCustomAttribute<Nerdbank.MessagePack.KeyAttribute>().Index,
                surrogateType.GetProperty(nameof(SurrogateTestEntity_PropertyShape.Renamed)).GetCustomAttribute<Nerdbank.MessagePack.KeyAttribute>().Index);
            var comparer = surrogateType.GetProperty(nameof(SurrogateTestEntity_PropertyShape.Compared))
                .GetCustomAttribute<Nerdbank.MessagePack.UseComparerAttribute>();
            Assert.AreEqual(typeof(StringComparer), comparer.ComparerType);
            Assert.AreEqual(nameof(StringComparer.OrdinalIgnoreCase), comparer.MemberName);

            foreach (string name in new[] { nameof(SurrogateTestEntity_PropertyShape.Renamed), nameof(SurrogateTestEntity_PropertyShape.ShapeOnly), nameof(SurrogateTestEntity_PropertyShape.Optional) })
            {
                PropertyInfo property = surrogateType.GetProperty(name);
                PropertyShapeAttribute original = entityType.GetProperty(name).GetCustomAttribute<PropertyShapeAttribute>();
                PropertyShapeAttribute copied = property.GetCustomAttribute<PropertyShapeAttribute>();
                Assert.AreEqual(original.Name, copied.Name);
                Assert.AreEqual(original.Order, copied.Order);
                Assert.AreEqual(original.Ignore, copied.Ignore);
                Assert.AreEqual(original.IsRequired, copied.IsRequired);
                CustomAttributeData data = property.GetCustomAttributesData().Single(attribute => attribute.AttributeType == typeof(PropertyShapeAttribute));
                Assert.AreEqual(name != nameof(SurrogateTestEntity_PropertyShape.Optional), data.NamedArguments.Any(argument => argument.MemberName == nameof(PropertyShapeAttribute.IsRequired)));
            }
        }

        [TestMethod]
        public void SurrogateTypeShapeOnlyCopiesKind()
        {
            Type entityType = typeof(SurrogateTestEntity_TypeShape);
            Type derivedType = typeof(SurrogateTestEntity_DerivedTypeShape);
            var knownTypes = new HashSet<Type> { entityType, derivedType };
            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownTypes, entityType);
            Type derivedSurrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownTypes, derivedType);

            Assert.IsNotNull(entityType.GetCustomAttribute<DerivedTypeShapeAttribute>());
            Assert.IsNull(surrogateType.GetCustomAttribute<DerivedTypeShapeAttribute>());
            Assert.IsNull(derivedSurrogateType.GetCustomAttribute<DerivedTypeShapeAttribute>());
            Assert.IsNull(surrogateType.GetCustomAttribute<Nerdbank.MessagePack.MessagePackConverterAttribute>());
            TypeShapeAttribute shape = surrogateType.GetCustomAttribute<TypeShapeAttribute>();
            Assert.AreEqual(TypeShapeKind.None, shape.Kind);
            Assert.AreEqual(default(MethodShapeFlags), shape.IncludeMethods);
            Assert.IsNull(shape.Marshaler);
            CollectionAssert.AreEqual(
                new[] { nameof(TypeShapeAttribute.Kind) },
                surrogateType.GetCustomAttributesData().Single(attribute => attribute.AttributeType == typeof(TypeShapeAttribute))
                    .NamedArguments.Select(argument => argument.MemberName).ToArray());
            Assert.AreEqual(surrogateType, derivedSurrogateType.BaseType);
        }

        [TestMethod]
        public void NerdbankPropertyAttributesPropagation()
        {
            Type entityType = typeof(SurrogateTestEntity_NerdbankAttributes);
            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(new HashSet<Type> { entityType }, entityType);
            Assert.AreEqual(typeof(SurrogateTestStringConverter), surrogateType.GetProperty(nameof(SurrogateTestEntity_NerdbankAttributes.Converted))
                .GetCustomAttribute<Nerdbank.MessagePack.MessagePackConverterAttribute>().ConverterType);

            foreach (string name in new[] { nameof(SurrogateTestEntity_NerdbankAttributes.Compared), nameof(SurrogateTestEntity_NerdbankAttributes.ComparedWithMember) })
            {
                var original = entityType.GetProperty(name).GetCustomAttribute<Nerdbank.MessagePack.UseComparerAttribute>();
                var copied = surrogateType.GetProperty(name).GetCustomAttribute<Nerdbank.MessagePack.UseComparerAttribute>();
                Assert.AreEqual(original.ComparerType, copied.ComparerType);
                Assert.AreEqual(original.MemberName, copied.MemberName);
            }
        }

        [TestMethod]
        public void VirtualSerializationAttributesPropagation()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualSerializationAttributes);
            var provider = new MockTypeDescriptionProvider(() => TypeDescriptor.CreateProperty(entityType, "VirtualProperty", typeof(string),
                new PropertyShapeAttribute { Name = "virtual-name", Order = 10, IsRequired = false },
                new Nerdbank.MessagePack.KeyAttribute(4),
                new Nerdbank.MessagePack.MessagePackConverterAttribute(typeof(SurrogateTestStringConverter))));
            TypeDescriptor.AddProvider(provider, entityType);
            try
            {
                Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(new HashSet<Type> { entityType }, entityType);
                PropertyInfo property = surrogateType.GetProperty("VirtualProperty");
                PropertyShapeAttribute shape = property.GetCustomAttribute<PropertyShapeAttribute>();
                Assert.AreEqual("virtual-name", shape.Name);
                Assert.AreEqual(10, shape.Order);
                Assert.IsFalse(shape.IsRequired);
                Assert.AreEqual(4, property.GetCustomAttribute<Nerdbank.MessagePack.KeyAttribute>().Index);
                Assert.AreEqual(typeof(SurrogateTestStringConverter), property.GetCustomAttribute<Nerdbank.MessagePack.MessagePackConverterAttribute>().ConverterType);
            }
            finally
            {
                TypeDescriptor.RemoveProvider(provider, entityType);
            }
        }

        [TestMethod]
        public void CitySerializationAttributesPropagation()
        {
            DomainServiceDescription.GetDescription(typeof(Cities.CityDomainService));
            Type entityType = typeof(Cities.State);
            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(new HashSet<Type> { entityType }, entityType);
            PropertyInfo surrogateName = surrogateType.GetProperty(nameof(Cities.State.Name));

            Assert.IsNotNull(surrogateType.GetCustomAttribute<TypeShapeAttribute>());
            Assert.IsNotNull(surrogateName.GetCustomAttribute<PropertyShapeAttribute>());
            Assert.AreEqual(0, surrogateName.GetCustomAttribute<Nerdbank.MessagePack.KeyAttribute>().Index);
        }

        [TestMethod]
        public void MessagePackRejectsConvertersOnExposedProperties()
        {
            var types = new HashSet<Type> { typeof(ConverterEntity), typeof(ConverterComplex) };
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => MessagePackSerializationProvider.ValidateMessagePackAttributes(types));
            StringAssert.Contains(error.Message, nameof(ConverterEntity.Converted));
            StringAssert.Contains(error.Message, typeof(ConverterComplex).FullName);

            // The same property is allowed when its type is not an exposed surrogate type.
            MessagePackSerializationProvider.ValidateMessagePackAttributes(new HashSet<Type> { typeof(ConverterEntity) });
        }

        [TestMethod]
        public void MessagePackRejectsConvertersOnComplexTypeProperties()
        {
            var types = new HashSet<Type> { typeof(ConverterContainer), typeof(ConverterComplex) };
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => MessagePackSerializationProvider.ValidateMessagePackAttributes(types));
            StringAssert.Contains(error.Message, nameof(ConverterContainer.Converted));
        }

        [TestMethod]
        public void MessagePackRejectsTypeConverterAndMarshaler()
        {
            var converterError = Assert.ThrowsExactly<InvalidOperationException>(() =>
                MessagePackSerializationProvider.ValidateMessagePackAttributes(new HashSet<Type> { typeof(TypeWithConverter) }));
            StringAssert.Contains(converterError.Message, nameof(MessagePackConverterAttribute));
            StringAssert.Contains(converterError.Message, typeof(TypeWithConverter).FullName);

            var marshalerError = Assert.ThrowsExactly<InvalidOperationException>(() =>
                MessagePackSerializationProvider.ValidateMessagePackAttributes(new HashSet<Type> { typeof(TypeWithMarshaler) }));
            StringAssert.Contains(marshalerError.Message, nameof(TypeShapeAttribute.Marshaler));
            StringAssert.Contains(marshalerError.Message, typeof(TypeWithMarshaler).FullName);
        }

        [TestMethod]
        public void MessagePackAllowsScalarConvertersAndExcludedProperties()
        {
            MessagePackSerializationProvider.ValidateMessagePackAttributes(new HashSet<Type>
            {
                typeof(AllowedConverterProperties), typeof(ConverterComplex), typeof(SurrogateTestEntity_NerdbankAttributes)
            });
        }

        [TestMethod]
        public void MessagePackChecksVirtualProperties()
        {
            Type type = typeof(VirtualConverterContainer);
            var provider = new MockTypeDescriptionProvider(() => TypeDescriptor.CreateProperty(type, "VirtualProperty", typeof(ConverterComplex),
                new MessagePackConverterAttribute(typeof(ConverterComplexConverter))));
            TypeDescriptor.AddProvider(provider, type);
            try
            {
                var error = Assert.ThrowsExactly<InvalidOperationException>(() => MessagePackSerializationProvider.ValidateMessagePackAttributes(
                    new HashSet<Type> { type, typeof(ConverterComplex) }));
                StringAssert.Contains(error.Message, "VirtualProperty");
            }
            finally
            {
                TypeDescriptor.RemoveProvider(provider, type);
            }
        }

        [TestMethod]
        public void MessagePackRejectsDuplicateDiscriminators()
        {
            var error = Assert.ThrowsExactly<InvalidOperationException>(() =>
                MessagePackSerializationProvider.ValidateMessagePackAttributes(new HashSet<Type>
                {
                    typeof(DuplicateDiscriminatorBase),
                    typeof(DuplicateDiscriminatorDerived1),
                    typeof(DuplicateDiscriminatorDerived2),
                }));

            StringAssert.Contains(error.Message, "duplicate");
            StringAssert.Contains(error.Message, typeof(DuplicateDiscriminatorDerived1).FullName);
            StringAssert.Contains(error.Message, typeof(DuplicateDiscriminatorDerived2).FullName);
        }

        [TestMethod]
        public void MessagePackProviderValidatesDomainServiceExposedTypes()
        {
            var description = DomainServiceDescription.GetDescription(typeof(ConverterDomainService));
            Assert.IsTrue(description.EntityTypes.Contains(typeof(ConverterEntity)));
            Assert.IsTrue(description.ComplexTypes.Contains(typeof(ConverterComplex)));
            var operation = description.GetQueryMethod(nameof(ConverterDomainService.GetEntities));

            // XML can create and cache the surrogates without bypassing MessagePack validation.
            var xmlProvider = new BinaryXmlSerializationProvider();
            Assert.IsNotNull(xmlProvider.GetRequestSerializer(operation));

            var provider = new MessagePackSerializationProvider(new MessagePackSerializationOptions());
            var error = Assert.ThrowsExactly<InvalidOperationException>(() => provider.GetRequestSerializer(operation));
            StringAssert.Contains(error.Message, nameof(ConverterEntity.Converted));
        }

        public class ConverterEntity
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }

            [MessagePackConverter(typeof(ConverterComplexConverter))]
            public ConverterComplex Converted { get; set; }
        }

        public class ConverterComplex
        {
            public string Value { get; set; }
        }

        public class ConverterContainer
        {
            [MessagePackConverter(typeof(ConverterComplexConverter))]
            public ConverterComplex Converted { get; set; }
        }

        [MessagePackConverter(typeof(TypeWithConverterConverter))]
        public class TypeWithConverter { }

        [TypeShape(Marshaler = typeof(TypeWithMarshalerMarshaler))]
        public class TypeWithMarshaler { }

        public class AllowedConverterProperties
        {
            [MessagePackConverter(typeof(SurrogateTestStringConverter))]
            public string Value { get; set; }

            [Exclude]
            [MessagePackConverter(typeof(ConverterComplexConverter))]
            public ConverterComplex Excluded { get; set; }
        }

        public class VirtualConverterContainer { }

        public sealed class ConverterComplexConverter : MessagePackConverter<ConverterComplex>
        {
            public override ConverterComplex Read(ref MessagePackReader reader, SerializationContext context) => new() { Value = reader.ReadString() };
            public override void Write(ref MessagePackWriter writer, in ConverterComplex value, SerializationContext context) => writer.Write(value.Value);
        }

        public sealed class TypeWithConverterConverter : MessagePackConverter<TypeWithConverter>
        {
            public override TypeWithConverter Read(ref MessagePackReader reader, SerializationContext context) { reader.ReadNil(); return new(); }
            public override void Write(ref MessagePackWriter writer, in TypeWithConverter value, SerializationContext context) => writer.WriteNil();
        }

        public sealed class TypeWithMarshalerMarshaler : IMarshaler<TypeWithMarshaler, string>
        {
            public string Marshal(TypeWithMarshaler value) => "";
            public TypeWithMarshaler Unmarshal(string value) => new();
        }

        public class ConverterDomainService : DomainService
        {
            public IQueryable<ConverterEntity> GetEntities() => Array.Empty<ConverterEntity>().AsQueryable();
        }

        [DerivedTypeShape(typeof(DuplicateDiscriminatorDerived1), Name = "duplicate")]
        [DerivedTypeShape(typeof(DuplicateDiscriminatorDerived2), Name = "duplicate")]
        public class DuplicateDiscriminatorBase { }

        public class DuplicateDiscriminatorDerived1 : DuplicateDiscriminatorBase { }
        public class DuplicateDiscriminatorDerived2 : DuplicateDiscriminatorBase { }
    }
}
