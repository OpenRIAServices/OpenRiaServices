using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}
