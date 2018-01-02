The RIA Services linker has moved on Open RIA Services from the Silverlight tab of the project properties to its own right click menu on the Project itself. It is also available from the Project menu in the menu bar of Visual Studio. Otherwise, everything should work exactly as it did with WCF RIA Services.

The DomainService wizard requires a separate installation to work as an interface file needs to be installed in the GAC and that cannot be installed from a VSIX.. The installer can be found at [https://openriaservices.codeplex.com/releases/view/116623](https://openriaservices.codeplex.com/releases/view/116623). The interface file should need to be updated very rarely if at all.

The NuGet packages included with the tools are the 4.4.0.0 versions that are current as of the original release date of the tooling. The packages had to be updated to accommodate signing and fixes to EF support

*New Features
	* Link Open RIA Services right click menu renamed to Manage Open RIA Services Project Link.
	* Signed versions of all templates have been added. Signed templates use OpenRiaServices.Signed.* NuGet packages.

* Bug Fixes
	* The Domain Service Wizard was at times having trouble loading due to loading issues with the EntityFramework dll. The code that recognizes a DbContext using a string instead of a direct type check.
	* 1.3.2.2 - Some of the NuGet packages included with 1.3.2.1 had references to EntityFramework that should not have been there. 1.3.2.2 includes fixed packages.
* Documentation Change
	* Before the Domain Service Wizard will find Entity Framework entities the correct NuGet package (i.e. OpenRiaServices.EntityFramework or OpenRiaServices.Signed.EntityFramework) needs to be installed first. A message has been added on the wizard to remind users of this requirement.