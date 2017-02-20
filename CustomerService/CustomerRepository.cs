using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace CustomerService
{
    public class CustomerRepository: ICustomerRepository
    {
        protected IMongoClient _client;
        protected IMongoDatabase _database;
        protected IMongoCollection<CustomerInfo> _collection;


        public CustomerRepository(string connectionString)
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase("oauth2bench");
            _collection = _database.GetCollection<CustomerInfo>("customer");
            InitIfNeeded().Wait();
        }

        public async Task InitIfNeeded()
        {
            var t = await _collection.CountAsync(x => true);
            if (t==0)
            {
                await _collection.InsertOneAsync(
                    new CustomerInfo { UserId = Guid.Parse("079290E8-1A30-4145-A576-263FE21193E6"), EMail = "bob@bob.com", HashedPassword = PasswordUtils.CalcHash("bob"), FirstName = "Bob", LastName = "Smith", DateOfBirth = new DateTime(1980, 06, 23), LoyaltyCardNumber = "888555444333" }
                );
                await _collection.InsertOneAsync(
                    new CustomerInfo { UserId = Guid.Parse("A25D1FBC-860F-4AF2-9D89-1B65A43B00F4"), EMail = "alice@alice.com", HashedPassword = PasswordUtils.CalcHash("alice"), FirstName = "Alice", LastName = "Granger", DateOfBirth = new DateTime(1983, 03, 07), LoyaltyCardNumber = "888222111666" }
                );
            }
        }

        public async Task<CustomerInfo> Get(Guid id)
        {
            var ret = await _collection.Find(x => x.UserId == id).FirstOrDefaultAsync();
            return ret;
        }

        public async Task<CustomerInfo> GetByEmail(string email)
        {
            var ret = await _collection.Find( x => x.EMail == email.ToLowerInvariant() ).FirstOrDefaultAsync();
            return ret;
        }

        public async Task InsertEvent(CustomerInfo item, string eventType, string clientId, string remoteClient)
        {
            await _collection.UpdateOneAsync<CustomerInfo>(x => x._id == item._id, Builders<CustomerInfo>.Update.Push("Events", 
                new { EventType = eventType, ClientId=clientId, RemoteClient=remoteClient, Date=DateTime.UtcNow }));
        }

    }
}