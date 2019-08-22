using System.Runtime.CompilerServices;
using OpenRiaServices.Data.DomainServices;

// While these classes belong to the MVVM package 
// they have been part of the portable release for some versions
// so moving them back here would be a breaking change.
// Instead we moved them also silverlight to so that assemblies 
// compiled against the portable dll can still work with the Silverlight versions.
[assembly: TypeForwardedTo(typeof(EntityList<>))]
[assembly: TypeForwardedTo(typeof(QueryBuilder<>))]