using OpenRiaServices.Server;

namespace Cities
{
    /// <summary>
    /// This class is an extension of CityDomainService that uses the Permission attribute with authentication required.
    /// </summary>
    [RequiresAuthentication]
    public class CityDomainService_AuthRequired : CityDomainService { }

    /// <summary>
    /// This class is an extension of CityDomainService that uses the Permission attribute with a role "manager" required.
    /// </summary>
    [RequiresRole("manager")]
    public class CityDomainService_RoleRequired : CityDomainService { }
}

