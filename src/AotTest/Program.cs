

using System.Diagnostics.CodeAnalysis;
using Nerdbank.MessagePack;
using OpenRiaServices.Client;
using OpenRiaServices.Client.DomainClients;
using Cities;
using PolyType;

Setup();

CityDomainContext context = new CityDomainContext();

try
{
    Console.WriteLine("TEST: LoadAsync");
    var loadedCitites = await context.LoadAsync(context.GetCitiesQuery());

    foreach (var city in loadedCitites.Entities)
    {
        Console.WriteLine($"City: {city.Name}, State: {city.StateName}");
    }
    var city0 = context.Cities.FirstOrDefault();

    Console.WriteLine("TEST: InvokeAsync");
    Console.WriteLine(await context.EchoAsync("Hello, World!"));

    //Console.WriteLine(city0.StateName == await context.GetStateIfUserAsync(city0) ? "State name matches" : "State name does not match");

    Console.WriteLine("TEST: SubmitChanges");

    // Submit changes
    city0.ZoneID = 999;
    await context.SubmitChangesAsync();
    Console.WriteLine("Changes submitted successfully.");

    // Entity action
    Console.WriteLine("TEST: EntityAction");
    city0.AssignCityZone("New Zone");
    await context.SubmitChangesAsync();
    Console.WriteLine("Changes submitted successfully.");

    // TODO: Validation, EntityValidate
    //city.Validate();

}
catch (MessagePackSerializationException ex)
{
    Console.WriteLine("Load failed: " + ex.Message);
    throw;
}

catch (Exception ex)
{
    Console.WriteLine("Load failed: " + ex.Message);
    throw;
}

// DomainContext looks up the generated query method by name for validation.

// Needed for "ValidateMethod" call that in all GetXXXQuery methods
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(CityDomainContext))]
// Needed for reflection on the generated service contract interface, replace by adding parameter information to InvokeArgs/QueryArgs 
//[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(CityDomainContext.ICityDomainServiceContract))]
static void Setup()
{
    var clientHandler = new HttpClientHandler();

    // Map enpoint names from WCF format to "FullName" format
    // We do this since all DomainContext were generated usign old WCF format
    Func<Uri, HttpClient> httpClientFactory = uri =>
    {
        HttpClient httpClient = new HttpClient(clientHandler, disposeHandler: false);

        // Remove ".svc/binary" from the URI
        const string toRemove = ".svc/binary/";
        string uriString = uri.AbsoluteUri;

        if (uriString.EndsWith(toRemove, StringComparison.Ordinal))
        {
            uri = new Uri(uriString.Remove(uriString.Length - toRemove.Length));
        }

        httpClient.BaseAddress = uri;
        return httpClient;
    };

    var msgPackSerializer = new MessagePackSerializer()
    {
    };

    DomainContext.DomainClientFactory = new MessagePackHttpDomainClientFactory(new Uri("http://localhost:5247/"), httpClientFactory, msgPackSerializer,
        //Witness.GeneratedTypeShapeProvider
        RPC.GeneratedTypeShapeProvider
        )
    {
        UseQueryHttpMethod = true,

    };

    // Some values seems to no be deserialized correctly when using the default serializer settings.
    // 
    // DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory(new Uri("http://localhost:5247/"), httpClientFactory);
}

/// <summary>
/// Withness
/// </summary>
[PolyType.GenerateShapeFor<CityWithInfo>]
[PolyType.GenerateShapeFor<CityWithEditHistory>]
[PolyType.GenerateShapeFor<City>]
[PolyType.GenerateShapeFor<Zip>]
[PolyType.GenerateShapeFor<ZipWithInfo>]
[PolyType.GenerateShapeFor<QueryResult<City>>]
public partial class Witness;

[PolyType.GenerateShapeFor<CityDomainContext.ICityDomainServiceContract>(IncludeMethods = PolyType.MethodShapeFlags.AllPublic)]
public partial class RPC;
