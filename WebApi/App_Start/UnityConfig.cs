using Microsoft.Practices.Unity;
using System.Web.Http;
using System.Web.Http.Dependencies;
using Unity.WebApi;

namespace WebApi
{
    public static class UnityConfig
    {
        public static IDependencyResolver BuildResolver()
        {
			var container = new UnityContainer();
            
            container.RegisterType<Controllers.ICustomerStore, Controllers.CustomerStore>();
            
            return new UnityDependencyResolver(container);
        }
    }
}