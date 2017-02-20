using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebApi.Controllers
{
    [RoutePrefix("api/customer")]
    public class CustomerController : ApiController
    {
        public ICustomerStore customerStore { get; private set; }

        public CustomerController(ICustomerStore custormerStore)
        {
            this.customerStore = custormerStore;
        }

        [Authorize]
        public async Task<CustomerService.CustomerInfo> Get()
        {
            var principal = User as ClaimsPrincipal;

            var subject = principal?.Identities?.FirstOrDefault()?.Claims?.FirstOrDefault(x => x.Type == IdentityModel.JwtClaimTypes.Subject)?.Value;

            Guid userId;
            if (Guid.TryParse(subject, out userId))
            {
                return await customerStore.Get(userId);
            }
            else
                return await Task.FromResult((CustomerService.CustomerInfo)null);

        }
    }

    public interface ICustomerStore
    {
        Task<CustomerService.CustomerInfo> Get(Guid id);
    }

    class CustomerStore:ICustomerStore
    {
        
        public async Task<CustomerService.CustomerInfo> Get(Guid id)
        {
            var s = new CustomerServiceReference.CustomerServiceClient();
            return await s.GetAsync(id);
        }

    }


}
