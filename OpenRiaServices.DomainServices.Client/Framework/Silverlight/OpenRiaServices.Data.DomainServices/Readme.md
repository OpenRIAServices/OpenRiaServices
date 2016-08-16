_Why are these classes here?_

While these classes belong to the MVVM package (OpenRiaServices.Data.DomainServices) 
they have been part of the portable release for some versions
so moving them back here would be a breaking change.
Instead we moved them here for silverlight too so that assemblies 
compiled against the portable dll can still work with the Silverlight versions.