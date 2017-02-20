using Swashbuckle.Application;
using Swashbuckle.Swagger;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.Description;
//using System.Web.Http.Cors;

namespace WebApi
{
    public static class WebApiConfig
    {
        public static HttpConfiguration Register(IDependencyResolver resolver)
        {
            // Web API configuration and services
            var config = new HttpConfiguration();

            config.DependencyResolver = resolver;

            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Web API routes
            config.MapHttpAttributeRoutes();

            //config.EnableCors(new EnableCorsAttribute("http://localhost:21575, http://localhost:37045", "accept, authorization", "GET", "WWW-Authenticate"));

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}",
                defaults: new { id = RouteParameter.Optional }
            );

            config
                .EnableSwagger(c => {
                    c.SingleApiVersion("v1", "A title for your API");
                    c.OAuth2("oauth2")
                            .Description("OAuth2 Implicit Grant")
                            .Flow("implicit")
                            .AuthorizationUrl("https://identity.loc/core/connect/authorize")
                            .TokenUrl("https://identity.loc/core/connect/token")
                            .Scopes(scopes =>
                            {
                                scopes.Add("read", "");
                            });
                    c.OperationFilter<AssignOAuth2SecurityRequirements>();
                })
                .EnableSwaggerUi("sandbox/{*assetPath}", c=> {
                   
                    c.EnableOAuth2Support("webapiimplicit", "https://identity.loc/core", "Swagger Ui for WebApi");
                });
            
            return config;
        }
    }
    
    public class AssignOAuth2SecurityRequirements : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            // Correspond each "Authorize" role to an oauth2 scope
            var scopes = apiDescription.ActionDescriptor.GetFilterPipeline()
                .Select(filterInfo => filterInfo.Instance)
                .OfType<AuthorizeAttribute>();
                //.SelectMany(attr => attr.Roles.Split(','))
                //.Distinct();

            if (scopes.Any())
            {
                if (operation.security == null)
                    operation.security = new List<IDictionary<string, IEnumerable<string>>>();

                var oAuthRequirements = new Dictionary<string, IEnumerable<string>>
                {
                    { "oauth2", new [] { "read" } }
                };

                operation.security.Add(oAuthRequirements);
            }
        }
    }
}