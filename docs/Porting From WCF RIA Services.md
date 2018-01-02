
**Important**: The content of this page is a straight copy from http://www.openriaservices.net/blog/post/The-Open-RIA-Services-Blog/1017/Porting-from-WCF-RIA-Services-to-Open-RIA-Services/ it might be slightly out of date but should probably work.


1. Remove all WCF RIA Services NuGet packages and DLLs. Look for DLLs with the System.ServiceModel.DomainServices and Microsoft.ServiceModel.DomainServices namespaces.
2.  Install the OpenRiaServices.Server package for all project that previously referenced System.ServiceModel.DomainServices.Server
3. If you are using Entity Framework 4 (ObjectContext), install the OpenRiaServices.EntityFramework.EF4 package.
4. If you are using Entity Framework 5 , you will need to upgrade to 6.
5. If you are using Entity Framework 6, install the OpenRiaServics.EntityFramework package.
6. Install the OpenRiaServices.Client package in the Silverlight projects that previously had the System.ServiceModel.DomainServices.Client DLL.
7. The OpenRiaServices.Client package includes the OpenRiaServices.Client.Core, OpenRiaServices.Silverlight.CodeGen, and OpenRiaServices.ViewModel packages in one install.
8. Unload the Silverlight project and edit the project file.

    8.1. Find the LinkedServerProject tag inside the project file. Rename the tag to LinkedOpenRiaServerProject and then reload the project. Repeat for any other projects that are RIA Linked.
   
    8.2. If you are using the RiaClientUseFullTypesNames in the project file, rename it to OpenRiaClientUsefullTypesNames.
9. Find all references to the System.ServiceModel.DomainServices namespace in your solution and replace them with OpenRiaServices.DomainServices.
10. If you are using the DomainDataSource, then remove the System.Windows.Controls.DomainServices dll and install the OpenRiaServices.Silverlight.DomainDataSource package. The namespace changes match the dll changes.




