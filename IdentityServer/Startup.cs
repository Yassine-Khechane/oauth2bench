using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Configuration;
using IdentityServer3.Core.Configuration;
using Serilog;
using IdentityServer3.Core.Services;
using IdentityServer;
using Microsoft.Owin.Logging;
using IdentityServer3.Core.Logging;
using IdentityServer3.Core.Services.InMemory;

[assembly: OwinStartup(typeof(IdentityServer.Startup))]

namespace IdentityServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var l = new LoggerConfiguration()
                .WriteTo.Trace(outputTemplate: "{Timestamp} [{Level}] ({Name}){NewLine} {Message}{NewLine}{Exception}")
                .WriteTo.File("c:\\temp\\identityServer.log")
                .CreateLogger();

            //appBuilder.SetLoggerFactory(new SerilogWeb.Owin.LoggerFactory(l));

            Log.Logger = l;

            /*var factory = new IdentityServerServiceFactory()
                        .UseInMemoryScopes(Scopes.Get());

            factory.UserService = new Registration<IUserService, CustomUserService>();
            factory.ViewService = new Registration<IViewService,CustomViewService>();
            factory.ClientStore = new Registration<IClientStore, CustomClientStore>();
            //factory.RefreshTokenService = new Registration<IRefreshTokenService, CustomRefreshTokenService>();
            factory.EventService = new Registration<IEventService, CustomEventService>();*/

            // Create and modify default settings
            var settings = IdentityServer3.MongoDb.StoreSettings.DefaultSettings();
            settings.ConnectionString = "mongodb://admin:admin@localhost:27017/?connectTimeoutMS=30000&authMechanism=SCRAM-SHA-1";
            settings.Database = "oauth2bench";

            // Create the MongoDB factory
            var factory = new IdentityServer3.MongoDb.ServiceFactory(new Registration<IUserService, CustomUserService>(), settings);

            // Overwrite services, e.g. with in memory stores
            factory.ClientStore = new Registration<IClientStore, CustomClientStore>();
            factory.ViewService = new Registration<IViewService, CustomViewService>();
            factory.EventService = new Registration<IEventService, CustomEventService>();
            factory.ScopeStore = new Registration<IScopeStore>(new InMemoryScopeStore(Scopes.Get()));

            var options = new IdentityServerOptions
            {
                
                SigningCertificate = Certificate.Load(),
                Factory = factory,
                EventsOptions = new EventsOptions { RaiseSuccessEvents=true, RaiseFailureEvents=true, RaiseErrorEvents=true, RaiseInformationEvents=true }
                
            };

            appBuilder.Map("/core", idsrvApp =>
            {
                idsrvApp.UseIdentityServer(options);
            });

            appBuilder.Map("/winrthelper", app =>
            {
                app.Run(ctx =>
                {
                    ctx.Response.ContentType = "text/html";
                    return ctx.Response.WriteAsync($@"<!DOCTYPE html>
<html>
<head><script>window.external.notify(document.location);</script></head><body>QueryString : {ctx.Request.QueryString}</body></html>");
                });
            });
        }
    }
}