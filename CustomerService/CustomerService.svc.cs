using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace CustomerService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    class CustomerService : ICustomerService
    {
        ICustomerRepository _repo;

        public CustomerService(/*ICustomerRepository repo*/)
        {
            //_repo = repo;
            _repo=new CustomerRepository("mongodb://CustomerService:customer@localhost:27017/oauth2bench?connectTimeoutMS=30000&authMechanism=SCRAM-SHA-1");
        }

        public CustomerInfo Get(Guid id)
        {
            var t = _repo.Get(id);
            t.Wait();
            return t.Result;
        }

        public CheckPasswordInfo CheckPassword(string email, string password, string clientId, string remoteClient)
        {
            var t = _repo.GetByEmail(email);
            t.Wait();
            var customer = t.Result;
            if (customer!=null)
            {
                var hash2 = PasswordUtils.CalcHash(password);
                if (customer.HashedPassword.SequenceEqual(hash2))
                {
                    _repo.InsertEvent(customer, "Login succeeded", clientId, remoteClient);
                    return new CheckPasswordInfo { Success = true, CustomerInfo = customer };
                }
                _repo.InsertEvent(customer, "Login failed", clientId, remoteClient);
            }
            return new CheckPasswordInfo { Success = false };
        }

    }

    public static class PasswordUtils
    {
        private static byte[] seed = { 0x85, 0xF2, 0x12, 0x99, 0xA9, 0x42, 0x02, 0x75 };

        public static byte[] CalcHash(string password)
        {
            var hashalg = SHA256.Create();
            var encoded = Encoding.UTF8.GetBytes(password);
            var pwd = new byte[encoded.Length + seed.Length];
            Array.Copy(seed, pwd, seed.Length);
            Array.Copy(encoded, 0, pwd, seed.Length, encoded.Length);
            return hashalg.ComputeHash(pwd);
        }
    }

}
