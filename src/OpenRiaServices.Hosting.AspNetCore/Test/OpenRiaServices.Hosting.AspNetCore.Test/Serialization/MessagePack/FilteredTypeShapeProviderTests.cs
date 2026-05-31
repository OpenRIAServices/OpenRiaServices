using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Server;
using PolyType;
using PolyType.Abstractions;
using PolyType.ReflectionProvider;
using IgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

#nullable enable
namespace OpenRiaServices.Hosting.Serialization.MessagePack.Tests;

[TestClass]
public sealed class FilteredTypeShapeProviderTests
{
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

