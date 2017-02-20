using IdentityModel;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DummyOwinAuth
{
    // Created by the factory in the DummyAuthenticationMiddleware class.
    class DummyAuthenticationHandler : AuthenticationHandler<DummyAuthenticationOptions>
    {
        private readonly ILogger _logger;

        public DummyAuthenticationHandler(ILogger logger)
        {
            _logger = logger;
        }
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            var access_token = Request.Query["access_token"];

            if (access_token != null)
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                var tok1 = tokenHandler.ReadToken(access_token) as JwtSecurityToken;
                SecurityToken tok2 = null;

                ClaimsPrincipal user = null;

                try
                {
                    user = tokenHandler.ValidateToken(access_token, new TokenValidationParameters
                    {
                        ValidIssuer = "https://identity.loc/core",
                        ValidAudience = "https://identity.loc/core/resources",
                        IssuerSigningToken = new X509SecurityToken(new X509Certificate2(Base64Url.Decode("MIIDBTCCAfGgAwIBAgIQNQb+T2ncIrNA6cKvUA1GWTAJBgUrDgMCHQUAMBIxEDAOBgNVBAMTB0RldlJvb3QwHhcNMTAwMTIwMjIwMDAwWhcNMjAwMTIwMjIwMDAwWjAVMRMwEQYDVQQDEwppZHNydjN0ZXN0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqnTksBdxOiOlsmRNd+mMS2M3o1IDpK4uAr0T4/YqO3zYHAGAWTwsq4ms+NWynqY5HaB4EThNxuq2GWC5JKpO1YirOrwS97B5x9LJyHXPsdJcSikEI9BxOkl6WLQ0UzPxHdYTLpR4/O+0ILAlXw8NU4+jB4AP8Sn9YGYJ5w0fLw5YmWioXeWvocz1wHrZdJPxS8XnqHXwMUozVzQj+x6daOv5FmrHU1r9/bbp0a1GLv4BbTtSh4kMyz1hXylho0EvPg5p9YIKStbNAW9eNWvv5R8HN7PPei21AsUqxekK0oW9jnEdHewckToX7x5zULWKwwZIksll0XnVczVgy7fCFwIDAQABo1wwWjATBgNVHSUEDDAKBggrBgEFBQcDATBDBgNVHQEEPDA6gBDSFgDaV+Q2d2191r6A38tBoRQwEjEQMA4GA1UEAxMHRGV2Um9vdIIQLFk7exPNg41NRNaeNu0I9jAJBgUrDgMCHQUAA4IBAQBUnMSZxY5xosMEW6Mz4WEAjNoNv2QvqNmk23RMZGMgr516ROeWS5D3RlTNyU8FkstNCC4maDM3E0Bi4bbzW3AwrpbluqtcyMN3Pivqdxx+zKWKiORJqqLIvN8CT1fVPxxXb/e9GOdaR8eXSmB0PgNUhM4IjgNkwBbvWC9F/lzvwjlQgciR7d4GfXPYsE1vf8tmdQaY8/PtdAkExmbrb9MihdggSoGXlELrPA91Yce+fiRcKY3rQlNWVd4DOoJ/cPXsXwry8pWjNCo5JD8Q+RQ5yZEy7YPoifwemLhTdsBz3hlZr28oCGJ3kbnpW0xGvQb3VHSTVVbeei0CfXoW6iz1"))),
                    }, out tok2);

                    if (user==null)
                    {
                        _logger.WriteWarning("invalid token received");
                        return Task.FromResult<AuthenticationTicket>(null);
                    }

                    DateTimeOffset currentUtc = Options.SystemClock.UtcNow;

                    if (tok2.ValidTo < currentUtc) {
                        _logger.WriteWarning("expired token received");
                        return Task.FromResult<AuthenticationTicket>(null);
                    }
                    if (tok2.ValidFrom > currentUtc) {
                        _logger.WriteWarning("not yet valid token received");
                        return Task.FromResult<AuthenticationTicket>(null);
                    }

                    var identity = new ClaimsIdentity(
                                        user.Identity,
                                        new[] { new Claim("access_token",access_token) },
                                        Options.SignInAsAuthenticationType,
                                        "sub",
                                        ClaimsIdentity.DefaultRoleClaimType
                                    );

                    var properties = new AuthenticationProperties();
                    properties.IssuedUtc = tok2.ValidFrom;
                    properties.ExpiresUtc = tok2.ValidTo;

                    return Task.FromResult(new AuthenticationTicket(identity, properties));

                }
                catch (Exception e)
                {
                    _logger.WriteError("error validating token",e);
                    user = null;
                }

            }
            return Task.FromResult<AuthenticationTicket>(null);
        }

        protected override Task ApplyResponseChallengeAsync()
        {
          /*  if (Response.StatusCode == 401)
            {
                var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

                // Only react to 401 if there is an authentication challenge for the authentication 
                // type of this handler.
                if (challenge != null)
                {
                    var state = challenge.Properties;

                    if (string.IsNullOrEmpty(state.RedirectUri))
                    {
                        state.RedirectUri = Request.Uri.ToString();
                    }

                    var stateString = Options.StateDataFormat.Protect(state);

                    Response.Redirect(WebUtilities.AddQueryString(Options.CallbackPath.Value, "state", stateString));
                }
            }
            */
            return Task.FromResult<object>(null);
        }

        public override async Task<bool> InvokeAsync()
        {
         /*   // This is always invoked on each request. For passive middleware, only do anything if this is
            // for our callback path when the user is redirected back from the authentication provider.
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                var ticket = await AuthenticateAsync();

                if (ticket != null)
                {
                    Context.Authentication.SignIn(ticket.Properties, ticket.Identity);

                    Response.Redirect(ticket.Properties.RedirectUri);

                    // Prevent further processing by the owin pipeline.
                    return true;
                }
            }*/
            // Let the rest of the pipeline run.
            var access_token = Request.Query["access_token"];

            if (access_token != null)
            {
                var ticket = await AuthenticateAsync();
                if (ticket != null)
                {
                    Context.Authentication.SignIn(ticket.Properties, ticket.Identity);
                    UriBuilder uri = new UriBuilder(Request.Uri);
                    var u=string.Join("&", Request.Query.Where(x => x.Key != "access_token"));
                    uri.Query = u;

                    Response.Redirect(uri.Uri.ToString());

                    // Prevent further processing by the owin pipeline.
                    return true;

                }
            }
            return false;
        }
    }
}
