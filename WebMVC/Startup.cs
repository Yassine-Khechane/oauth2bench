using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Owin;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using IdentityServer3.AccessTokenValidation;
using Serilog;
using SerilogWeb.Owin;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.Cookies;
using System.Security.Claims;
using DummyOwinAuth;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Linq;

[assembly: OwinStartup(typeof(WebMVC.Startup))]

namespace WebMVC
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var l = new LoggerConfiguration()
                .WriteTo.Trace(outputTemplate: "{Timestamp} [{Level}] ({Name}){NewLine} {Message}{NewLine}{Exception}")
                .WriteTo.File("c:\\webmvc.log")
                .CreateLogger();

            app.Use((context, next) => { var lg = new InternalLogger(); context.Set("InternalLogger",lg); context.TraceOutput = lg.Output; return next.Invoke(); });

            app.SetLoggerFactory(new SerilogWeb.Owin.LoggerFactory(l));

            JwtSecurityTokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();

            /*app.Use(async (context, next) =>
            {
                context.TraceOutput.WriteLine(context.Authentication.User.Identity.Name);
                await next.Invoke();
            });*/

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies",
                Provider = new MyCookieAuthProvider()
            });
            
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                ClientId = "webmvcimplicit",
                Authority = "https://identity.loc/core",
                RedirectUri = "http://webmvc.loc/",
                ResponseType = "id_token token",
                Scope = "openid email write",

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                    SecurityTokenValidated = async n =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                    {
                        n.OwinContext.TraceOutput.WriteLine($"OpenId.SecurityTokenValidated");
                        var token = n.ProtocolMessage.AccessToken;
                        // persist access token in cookie
                        if (!string.IsNullOrEmpty(token))
                        {
                            n.AuthenticationTicket.Identity.AddClaim(
                                new Claim("access_token", token));
                        }
                    },
                    AuthenticationFailed = async n=> { n.OwinContext.TraceOutput.WriteLine($"OpenId.AuthenticationFailed"); },
                    AuthorizationCodeReceived = async n => { n.OwinContext.TraceOutput.WriteLine($"OpenId.AuthorizationCodeReceived"); },
                    MessageReceived = async n => { n.OwinContext.TraceOutput.WriteLine($"OpenId.MessageReceived"); },
                    RedirectToIdentityProvider = async n => { n.OwinContext.TraceOutput.WriteLine($"OpenId.RedirectToIdentityProvider"); },
                    SecurityTokenReceived = async n => { n.OwinContext.TraceOutput.WriteLine($"OpenId.SecurityTokenReceived"); }

                },

                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "sub",
                    AuthenticationType = "Cookies"
                }
            });
            
            app.UseDummyAuthentication(new DummyAuthenticationOptions("", "") { SignInAsAuthenticationType = "Cookies" });

            app.UseErrorPage(new Microsoft.Owin.Diagnostics.ErrorPageOptions { ShowCookies = true, ShowEnvironment = true, ShowExceptionDetails = true, ShowHeaders = true, ShowQuery = true, ShowSourceCode = true, SourceCodeLineCount = 15 });
        }
    }

    public class InternalLogger
    {
        public TextWriter Output { get; internal set; }

        private MemoryStream memStream;
        public InternalLogger()
        {
            memStream = new MemoryStream();
            Output=new StreamWriter(memStream);
        }

        public List<string> RetrieveOutput()
        {
            Output.Flush();
            var b = memStream.ToArray();
            return Encoding.UTF8.GetString(b).Split('\n').ToList();
        }
    }

    public class MyCookieAuthProvider : ICookieAuthenticationProvider
    {
        public void ApplyRedirect(CookieApplyRedirectContext context)
        {
            context.OwinContext.TraceOutput.WriteLine($"ApplyRedirect {context.RedirectUri}");
        }

        public void Exception(CookieExceptionContext context)
        {
            context.OwinContext.TraceOutput.WriteLine($"Exception {context.Exception.Message}");
        }

        public void ResponseSignedIn(CookieResponseSignedInContext context)
        {
            context.OwinContext.TraceOutput.WriteLine($"ResponseSignedIn {context.AuthenticationType} {context.Identity.Name}");
        }

        public void ResponseSignIn(CookieResponseSignInContext context)
        {
            context.OwinContext.TraceOutput.WriteLine($"ResponseSignIn {context.AuthenticationType} {context.Identity.Name}");
        }

        public void ResponseSignOut(CookieResponseSignOutContext context)
        {
            context.OwinContext.TraceOutput.WriteLine($"ResponseSignOut");
        }

        public async Task ValidateIdentity(CookieValidateIdentityContext context)
        {
            context.OwinContext.TraceOutput.WriteLine($"ValidateIdentity {context.Identity.Name}");
            foreach(var e in context.Properties.Dictionary)
            {
                context.OwinContext.TraceOutput.WriteLine($"ValidateIdentity     {e.Key}={e.Value}");
            }
        }
    }
}