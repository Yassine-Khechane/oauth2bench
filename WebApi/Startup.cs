using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Owin;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using IdentityServer3.AccessTokenValidation;
/*using Serilog;
using SerilogWeb.Owin;
using Microsoft.Owin.Security.OAuth;*/
using System;
using System.Diagnostics;
using Microsoft.Owin.Security.OAuth;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(WebApi.Startup))]

namespace WebApi
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            /*var l = new LoggerConfiguration()
                .WriteTo.Trace(outputTemplate: "{Timestamp} [{Level}] ({Name}){NewLine} {Message}{NewLine}{Exception}")
                .WriteTo.File("c:\\temp\\webapi.log")
                .CreateLogger();

            l.Information("Hello");*/
            app.SetLoggerFactory(new MyLoggerFactory());

            JwtSecurityTokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();

            app.UseIdentityServerBearerTokenAuthentication(new IdentityServerBearerTokenAuthenticationOptions
                {                    
                    Authority = "https://identity.loc/core",
                    RequiredScopes = new[] { "read" },
                    EnableValidationResultCache = false,
                    
                    TokenProvider = new MyTokenProvider(),
                    //ValidationMode = ValidationMode.ValidationEndpoint                    
                });

            var resolver = UnityConfig.BuildResolver();

            app.UseWebApi(WebApiConfig.Register(resolver));
        }
    }

    public class MyTokenProvider: OAuthBearerAuthenticationProvider
    {
        public override Task RequestToken(OAuthRequestTokenContext context)
        {
            return base.RequestToken(context);
        }

        public override Task ValidateIdentity(OAuthValidateIdentityContext context)
        {
            return base.ValidateIdentity(context);
        }
    }

    public class MyLoggerFactory : ILoggerFactory
    {
        public Microsoft.Owin.Logging.ILogger Create(string name)
        {
            return new MyLogger(name);
        }

        private class MyLogger : Microsoft.Owin.Logging.ILogger
        {
            string Name { get; set; }
            public MyLogger(string name)
            {
                Name = name;
            }
            public bool WriteCore(TraceEventType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                Debug.WriteLine($"{Name} [{eventType}] [{eventId}] {formatter(state, exception)}");
                return true;
            }
        }
    }
}