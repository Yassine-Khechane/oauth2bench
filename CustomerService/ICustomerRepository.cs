using System;
using System.Threading.Tasks;

namespace CustomerService
{
    public interface ICustomerRepository
    {
        Task<CustomerInfo> Get(Guid id);
        Task<CustomerInfo> GetByEmail(string email);

        Task InsertEvent(CustomerInfo item, string eventType, string clientId, string remoteClient);
    }
}