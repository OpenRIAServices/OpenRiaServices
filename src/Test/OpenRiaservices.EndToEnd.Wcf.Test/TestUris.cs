using System;

public static class TestURIs
{
#if SILVERLIGHT
    public static readonly Uri RootURI = System.Windows.Browser.HtmlPage.Document.DocumentUri;
#elif VBTests
    public static readonly Uri RootURI = new Uri("http://localhost:60000/");
#elif ASPNETCORE
    public static readonly Uri RootURI = new Uri("http://localhost:5246/");
#else
    public static readonly Uri RootURI = new Uri("http://localhost:60002/");
#endif

    // Cities
    public static readonly Uri Cities = new Uri(RootURI, "Cities-CityDomainService.svc");
    //public static readonly Uri Cities_AuthRequired = new Uri(RootURI, "Cities-CityDomainService_AuthRequired.svc");
    //public static readonly Uri Cities_RoleRequired = new Uri(RootURI, "Cities-CityDomainService_RoleRequired.svc");

    // EF
    public static readonly Uri EF_Catalog = new Uri(RootURI, "TestDomainServices-EF-Catalog.svc");
    public static readonly Uri EF_Northwind = new Uri(RootURI, "TestDomainServices-EF-Northwind.svc");
    public static readonly Uri EF_Northwind_CUD = new Uri(RootURI, "TestDomainServices-EF-Northwind_CUD.svc");
    public static readonly Uri EF_Northwind_POCO_CUD = new Uri(RootURI, "TestDomainServices-EF-NorthwindPOCO_CUD.svc");

    // EF CodeFirst
    public static readonly Uri EFCF_Northwind_CUD = new Uri("TestDomainServices-EFCF-Northwind_CUD.svc", UriKind.Relative);
    public static readonly Uri DbCtx_Northwind_CUD = new Uri("TestDomainServices-DbCtx-Northwind_CUD.svc", UriKind.Relative);
    public static readonly Uri DbCtx_Northwind = new Uri("TestDomainServices-DbCtx-Northwind.svc", UriKind.Relative);
    public static readonly Uri DbCtx_Catalog = new Uri("TestDomainServices-DbCtx-Catalog.svc", UriKind.Relative);
    // Not used?  public static readonly Uri EFCodeFirst = new Uri(RootURI, "EFCodeFirst-EFCodeFirstTestDomainService.svc");

    // EF Core
    public static readonly Uri EFCore_Northwind = new Uri(RootURI, "TestDomainServices-EFCore-Northwind.svc");
    public static readonly Uri EFCore_Northwind_CUD = new Uri(RootURI, "TestDomainServices-EFCore-Northwind_CUD.svc");
    public static readonly Uri EFCore_Catalog = new Uri(RootURI, "TestDomainServices-EFCore-Catalog.svc");

    // LTS
    public static readonly Uri LTS_Catalog = new Uri("TestDomainServices-LTS-Catalog.svc", UriKind.Relative);
    public static readonly Uri LTS_Northwind = new Uri("TestDomainServices-LTS-Northwind.svc", UriKind.Relative);
    public static readonly Uri LTS_Northwind_CUD = new Uri("TestDomainServices-LTS-Northwind_CUD.svc", UriKind.Relative);

    // TestProvider
    public static readonly Uri TestProvider_Scenarios = new Uri(RootURI, "TestDomainServices-TestProvider_Scenarios.svc");
    public static readonly Uri TestService_RequiresSecureEndpoint = new Uri(RootURI, "TestDomainServices-TestService_RequiresSecureEndpoint.svc");

    // Mock Customers
    public static readonly Uri MockCustomers = new Uri("TestDomainServices-MockCustomerDomainService.svc", UriKind.Relative);

    // Authentication
    public static readonly Uri AuthenticationService1 = new Uri("RootNamespace-TestNamespace-AuthenticationService1.svc", UriKind.Relative);

    // Share Entities
    public static readonly Uri SharedEntitiesChild = new Uri("SharedEntities-ExposeChildEntityDomainService.svc", UriKind.Relative);
    public static readonly Uri SharedEntitiesParent = new Uri("SharedEntities-ExposeParentEntityDomainService.svc", UriKind.Relative);

    public static readonly Uri ComplexTypes_TestService = new Uri("TestDomainServices-ComplexTypes_TestService.svc", UriKind.Relative);
    public static readonly Uri ComplexTypes_DomainService = new Uri("TestDomainServices-ComplexTypes_DomainService.svc", UriKind.Relative);

    // Server side async
    public static readonly Uri ServerSideAsync = new Uri("TestDomainServices-ServerSideAsyncDomainService.svc", UriKind.Relative);
}
