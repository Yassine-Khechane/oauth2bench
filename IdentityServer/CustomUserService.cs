using IdentityServer3.Core.Services.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using System.Security.Claims;
using IdentityServer3.Core;
using IdentityServer3.Core.Services;
using Microsoft.Owin;

namespace IdentityServer
{
    class CustomUserService: UserServiceBase
    {
        OwinContext owinContext
        {
            get; set;
        }

        public CustomUserService(OwinEnvironmentService env)
        {
            owinContext = new OwinContext(env.Environment);
        }

        public override async Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {

            var s = new CustomerServiceReference.CustomerServiceClient();
            var res = await s.CheckPasswordAsync(context.UserName, context.Password, context.SignInMessage.ClientId, $"{owinContext.Request.RemoteIpAddress}:{owinContext.Request.RemotePort}"  );

            if (res.Success) {
                var name = res.CustomerInfo.FirstName + " " + res.CustomerInfo.LastName;
                var claims = new Claim[] {
                        new Claim(Constants.ClaimTypes.Name, name),
                        new Claim(Constants.ClaimTypes.GivenName, res.CustomerInfo.FirstName),
                        new Claim(Constants.ClaimTypes.FamilyName, res.CustomerInfo.LastName),
                        new Claim(Constants.ClaimTypes.Email, res.CustomerInfo.EMail)
                };
                context.AuthenticateResult = new AuthenticateResult(res.CustomerInfo.UserId.ToString(), name, claims);
            }
            else
            {
                context.AuthenticateResult = new AuthenticateResult("Authentication failed");
            }
        }
    }
}
