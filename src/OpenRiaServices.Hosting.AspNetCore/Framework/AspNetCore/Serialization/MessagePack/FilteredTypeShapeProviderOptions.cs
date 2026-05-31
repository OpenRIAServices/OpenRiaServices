namespace OpenRiaServices.Hosting.Serialization.MessagePack;

public enum AssociationMemberSerializationMode
{
    Exclude = 0,
    Include = 1,
}

public sealed class FilteredTypeShapeProviderOptions
{
    public static FilteredTypeShapeProviderOptions Default { get; } = new();

    public AssociationMemberSerializationMode AssociationMemberSerializationMode { get; init; } = AssociationMemberSerializationMode.Exclude;
}
