using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using PolyType;
using PolyType.Abstractions;
using PolyType.ReflectionProvider;
using IgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

#nullable enable
namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack.Tests;

[TestClass]
//[Ignore("Not used")] 
public sealed class FilteredTypeShapeProviderTests
{
    [TestMethod]
    [IgnoreAttribute("Used for manual testing")] 
    public void Properties_Should_Match_DataContract_Surrogates_Exactly()
    {
        ServiceCollection collection = new();
        collection.AddDomainServices(typeof(Cities.CityDomainService).Assembly);
        var provider = collection.BuildServiceProvider();

        ITypeShapeProvider baseProvider = ReflectionTypeShapeProvider.Default;
        var typeShapeProvider = new FilteredTypeShapeProvider(baseProvider);

        foreach (var serviceRegistration in collection)
        {
            DomainServiceDescription domainServiceDescription;
            try
            {
                domainServiceDescription = DomainServiceDescription.GetDescription(serviceRegistration.ImplementationType);
                using DomainService? service = (DomainService?)provider.GetService(serviceRegistration.ServiceType);
                if (service is null)
                    continue;
            }
            catch (System.Exception)
            {
                // Ignore throwing domain services
                continue;
            }

            // Get data contract surrogates
            var knownTypes = new HashSet<Type>(domainServiceDescription.EntityTypes);
            foreach (var type in domainServiceDescription.EntityTypes.Concat(domainServiceDescription.ComplexTypes))
            {


                Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownTypes, type);

                if (surrogateType is null)
                {
                    Console.WriteLine($"No surrogate for {type}");
                    surrogateType = type;
                }

                if (type == typeof(Cities.City)
                    //|| type == typeof(TestDomainServices.D)
                    //|| type == typeof(TestDomainServices.MixedType)
                    //|| type == typeof(NorthwindModel.Product)
                    )
                {
                    Debugger.Break();
                }

                ITypeShape shape = typeShapeProvider.GetTypeShapeOrThrow(type);
                AssertTypesAreEqual(typeShapeProvider, shape, surrogateType);
            }
        }

        static void AssertTypesAreEqual(FilteredTypeShapeProvider typeShapeProvider, ITypeShape shape, Type surrogateType)
        {
            IUnionTypeShape? unionType = shape as IUnionTypeShape;

            string errors = string.Empty;

            if (shape is IObjectTypeShape objectType)
            {
                string actualProperties = string.Join(", ", objectType.Properties.Select(p => p.Name).OrderBy(n => n));
                string expectedProperties = string.Join(", ", surrogateType.GetProperties().Select(p => p.Name).OrderBy(n => n));

                //Assert.AreEqual(expectedProperties, actualProperties, $"Properties of surrogate {surrogateType.FullName} should match properties of original {surrogateType.FullName}");

                // DataContractSurrogateGenerator replaces Binary with byte[] in surrogate, so normalize that for comparison
                actualProperties = actualProperties.Replace("System.Data.Linq.Binary", "System.Byte[]");

                if (expectedProperties != actualProperties)
                {
                    errors = $"\t\tProperties of surrogate {surrogateType.FullName} should match properties of original {surrogateType.FullName}";
                    errors += $"\n\t\tExpected: {expectedProperties}";
                    errors += $"\n\t\tActual: {actualProperties}";
                }
                else
                {
                    string actualTypes = string.Join(", ", objectType.Properties.OrderBy(n => n.Name).Select(p => p.PropertyType.Type));
                    string expectedTypes = string.Join(", ", surrogateType.GetProperties().OrderBy(p => p.Name).Select(p => p.PropertyType));

                    //Assert.AreEqual(expectedTypes, actualTypes, $"Property types of surrogate {surrogateType.FullName} should match property types of original {surrogateType.FullName}");

                    if (expectedTypes != actualTypes)
                    {
                        errors = $"\t\tProperty types of surrogate {surrogateType.FullName} should match property types of original {surrogateType.FullName}";
                        errors += $"\n\t\tExpected: {expectedTypes}";
                        errors += $"\n\t\tActual: {actualTypes}";
                    }
                }
            }
            else if (unionType is not null)
            {
                Console.WriteLine($"SKIPPING UNION {shape.Type}");
                // TODO: Implement
                return;

                // TODO: Check known derived types of union type match nested types of surrogate
                var derivedTypes = KnownTypeUtilities.ImportKnownTypes(surrogateType, inherit: true)
                    .OrderBy(t => t.Name)
                    .ToList(); // Force load known types from surrogate
                var unionKnownTypes = unionType.UnionCases.Select(t => t.UnionCaseType).OrderBy(t => t.Type.Name)
                    .ToList();

                CollectionAssert.AreEqual(derivedTypes, unionKnownTypes.Select(t => t.Type).ToList());
                for (int i = 0; i < derivedTypes.Count; i++)
                {
                    AssertTypesAreEqual(typeShapeProvider, unionKnownTypes[i], derivedTypes[i]);
                }
            }

            if (string.IsNullOrEmpty(errors))
            {
                //Console.WriteLine($"OK: {shape.Type}");
            }
            else
            {
                Console.WriteLine($"FAIL: {shape.Type} - {errors}");
            }
        }
    }


    [TestMethod]
    public void ObjectShape_FiltersToMetaTypeDataMembers_ByDefault()
    {
        TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(FilterTestModel), typeof(FilterTestModelMetadata)), typeof(FilterTestModel));

        ITypeShapeProvider baseProvider = ReflectionTypeShapeProvider.Default;
        var provider = new FilteredTypeShapeProvider(baseProvider);

        var shape = (IObjectTypeShape?)provider.GetTypeShape(typeof(FilterTestModel));

        Assert.IsNotNull(shape);
        CollectionAssert.AreEquivalent(new[] { nameof(FilterTestModel.Included) }, shape.Properties.Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public void ObjectShape_IncludeAssociationMembersOption_IncludesAssociationProperty()
    {
        TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(FilterTestModel), typeof(FilterTestModelMetadata)), typeof(FilterTestModel));

        var options = new FilteredTypeShapeProviderOptions
        {
            AssociationMemberSerializationMode = AssociationMemberSerializationMode.Include,
        };

        ITypeShapeProvider baseProvider = ReflectionTypeShapeProvider.Default;
        var provider = new FilteredTypeShapeProvider(baseProvider, options);

        var shape = (IObjectTypeShape?)provider.GetTypeShapeOrThrow(typeof(FilterTestModel));
        var baseShape = (IObjectTypeShape)baseProvider.GetTypeShapeOrThrow(typeof(FilterTestModel));

        Assert.IsTrue(provider.ShouldIncludeProperty(typeof(FilterTestModel), baseShape.Properties.First(f => f.Name == nameof(FilterTestModel.Included))));
        Assert.IsTrue(provider.ShouldIncludeProperty(typeof(FilterTestModel), baseShape.Properties.First(f => f.Name == nameof(FilterTestModel.Association))));
        Assert.IsFalse(provider.ShouldIncludeProperty(typeof(FilterTestModel), baseShape.Properties.First(f => f.Name == nameof(FilterTestModel.ExcludedByMetadata))));

        Assert.IsNotNull(shape);
        CollectionAssert.AreEquivalent(new[] { nameof(FilterTestModel.Included), nameof(FilterTestModel.Association) }, shape.Properties.Select(p => p.Name).ToArray());
    }

    [TestMethod]
    [Ignore("The code is commented out in FilteredTypeShapeProvider")]
    public void TypeAndPropertyAttributes_AreResolvedFromTypeDescriptor()
    {
        TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(AttributeModel), typeof(AttributeModelMetadata)), typeof(AttributeModel));

        ITypeShapeProvider baseProvider = ReflectionTypeShapeProvider.Default;
        var provider = new FilteredTypeShapeProvider(baseProvider);

        var typeShape = provider.GetTypeShape(typeof(AttributeModel));
        Assert.IsNotNull(typeShape);

        var typeDisplay = typeShape.AttributeProvider.GetCustomAttribute<DisplayAttribute>();
        Assert.IsNotNull(typeDisplay);
        Assert.AreEqual("TypeDisplay", typeDisplay.Name);

        var objectShape = (IObjectTypeShape?)typeShape;
        Assert.IsNotNull(objectShape);

        var valueProperty = objectShape.Properties.Single(p => p.Name == nameof(AttributeModel.Value));
        var required = valueProperty.AttributeProvider.GetCustomAttribute<RequiredAttribute>();
        Assert.IsNotNull(required);
    }

    [TestMethod]
    public void Cycles_AreHandled_AndNestedShapesPointToFilteredProvider()
    {
        ITypeShapeProvider baseProvider = ReflectionTypeShapeProvider.Default;
        var provider = new FilteredTypeShapeProvider(baseProvider, new() { AssociationMemberSerializationMode = AssociationMemberSerializationMode.Include });

        var nodeShape = (IObjectTypeShape)provider.GetTypeShapeOrThrow(typeof(CycleNode));
        Assert.IsNotNull(nodeShape);
        Assert.AreSame(provider, nodeShape.Provider);


        Assert.HasCount(2, nodeShape.Properties, "Cycle");

        var childrenProperty = nodeShape.Properties.Single(p => p.Name == nameof(CycleNode.Children));
        var enumerableShape = (IEnumerableTypeShape)childrenProperty.PropertyType;
        var childType = (IObjectTypeShape)enumerableShape.ElementType;

        Assert.AreSame(provider, childrenProperty.PropertyType.Provider);
        Assert.AreSame(provider, enumerableShape.ElementType.Provider);

        var parentProperty = childType.Properties.Single(p => p.Name == nameof(CycleChild.Parent));

        Assert.AreSame(nodeShape, parentProperty.PropertyType);
        Assert.HasCount(3, childType.Properties, "Child should have 3 properties serialized");
    }

    [TestMethod]
    public void GetTypeShape_ReusesWrapperInstancePerType()
    {
        ITypeShapeProvider baseProvider = ReflectionTypeShapeProvider.Default;
        var provider = new FilteredTypeShapeProvider(baseProvider);

        var shape1 = provider.GetTypeShape(typeof(CycleNode));
        var shape2 = provider.GetTypeShape(typeof(CycleNode));

        Assert.IsNotNull(shape1);
        Assert.IsNotNull(shape2);
        Assert.AreSame(shape1, shape2);
    }

    [MetadataType(typeof(FilterTestModelMetadata))]
    public sealed class FilterTestModel
    {
        public int Included { get; set; }
        public int ExcludedByMetadata { get; set; }

        public FilterAssociationModel? Association { get; set; }
    }

    private sealed class FilterTestModelMetadata
    {
        [Editable(true)]
        public int Included { get; set; }

        [Exclude]
        public int ExcludedByMetadata { get; set; }

        [Include]
        [EntityAssociation("Filter_Association", nameof(FilterTestModelMetadata.Included), nameof(FilterAssociationModel.Id))]
        public FilterAssociationModel? Association { get; set; }
    }

    public sealed class FilterAssociationModel
    {
        [Key]
        public int Id { get; set; }
    }

    public sealed class AttributeModel
    {
        public string Value { get; set; } = string.Empty;
    }

    [Display(Name = "TypeDisplay")]
    [MetadataType(typeof(AttributeModelMetadata))]
    public sealed class AttributeModelMetadata
    {
        [Required]
        public string Value { get; set; } = string.Empty;
    }

    public class CycleNode
    {
        [Key]
        public string? Name { get; set; }

        [Include]
        [EntityAssociation("fk_name", nameof(Name), nameof(CycleChild.ParentId))]
        public List<CycleChild> Children { get; set; } = [];

        [EntityAssociation("node_children2", nameof(Name), nameof(CycleChild.Name))]
        public List<CycleChild> ChildrenNoIncluded { get; set; } = [];
    }

    public class CycleChild
    {
        [Key]
        public string? ParentId { get; set; }

        [Key]
        public string? Name { get; set; }

        [Exclude]
        public string? Excluded { get; set; }

        [Include]
        [EntityAssociation("fk_name", nameof(ParentId), nameof(CycleNode.Name))]
        public CycleNode? Parent { get; set; }
    }
}

