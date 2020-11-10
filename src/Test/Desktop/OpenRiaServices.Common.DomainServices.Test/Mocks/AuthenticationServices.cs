using OpenRiaServices.Server;
using OpenRiaServices.Server.Authentication.AspNetMembership;

namespace RootNamespace.TestNamespace
{
    // AuthenticationServices used in RiaContext generation

    [EnableClientAccess]
    public class AuthenticationService1 : AuthenticationBase<User1> { }
    [EnableClientAccess]
    public class AuthenticationService2 : AuthenticationBase<User2> { }

    public class User1 : UserBase { }
    public class User2 : UserBase { }
}
