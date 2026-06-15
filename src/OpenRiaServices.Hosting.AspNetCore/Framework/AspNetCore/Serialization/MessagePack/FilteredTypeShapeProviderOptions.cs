namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack;

enum AssociationMemberSerializationMode
{
    Exclude = 0,
    Include = 1,
}

sealed class FilteredTypeShapeProviderOptions
{
    public static FilteredTypeShapeProviderOptions Default { get; } = new();

    public AssociationMemberSerializationMode AssociationMemberSerializationMode { get; init; } = AssociationMemberSerializationMode.Exclude;
}
