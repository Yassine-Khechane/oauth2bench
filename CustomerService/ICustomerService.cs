using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace CustomerService
{

    [ServiceContract]
    public interface ICustomerService
    {
        [OperationContract]
        CustomerInfo Get(Guid id);

        [OperationContract]
        CheckPasswordInfo CheckPassword(string email, string password, string clientId, string remoteClient);
    }

    [DataContract]
    [BsonIgnoreExtraElements]
    public class CustomerInfo
    {
        [BsonId]
        internal ObjectId _id { get; set; }

        [DataMember]
        public Guid UserId { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public string EMail { get; set; }
        [DataMember]
        public DateTime DateOfBirth { get; set; }
        [DataMember]
        public string LoyaltyCardNumber { get; set; }

        [IgnoreDataMember]
        public byte[] HashedPassword { get; set; }
    }

    public class CheckPasswordInfo
    {
        public bool Success { get; set; }
        public CustomerInfo CustomerInfo { get; set; }
    }

}
