using Steeltoe.Security.Authentication.CloudFoundry;

namespace CloudPlatformDemo.Models;

public class SecurityPolicy
{
    public const string LoggedIn = "loggedin";
    public const string SameSpace = CloudFoundryDefaults.SameSpaceAuthorizationPolicy;
        
}