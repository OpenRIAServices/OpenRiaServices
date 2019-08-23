
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Server.Authentication.AspNetMembership;

namespace $rootnamespace$
{
    [EnableClientAccess]
    public class $safeitemname$ : AuthenticationBase<User>
    {
        // To enable Forms/Windows Authentication for the Web Application, edit the appropriate section of web.config file.
    }

    public class User : UserBase
    {
        // NOTE: Profile properties can be added here 
        // To enable profiles, edit the appropriate section of web.config file.

        // public string MyProfileProperty { get; set; }
    }
}
