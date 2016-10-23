namespace OpenRiaServices.DomainServices.Tools.SharedTypes
{
    internal interface ISharedAssemblies
    {
        string GetSharedAssemblyPath(CodeMemberKey key);
    }
}