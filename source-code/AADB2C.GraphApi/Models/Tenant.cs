using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AADB2C.GraphApi.Models
{
    public class Tenant
    {
        public Tenant(string id, Guid clientId, string clientSecret, string graphApiVersion, JObject additionalFields)
            : this(id, clientId, clientSecret, graphApiVersion, additionalFields.ToObject<Dictionary<string, object>>()) { }

        [JsonConstructor]
        public Tenant(string id, Guid clientId, string clientSecret, string graphApiVersion, Dictionary<string, object> additionalFields)
        {
            Id = id;
            ClientId = clientId;
            ClientSecret = clientSecret;
            GraphApiVersion = graphApiVersion;
            AdditionalFields = additionalFields ?? new Dictionary<string, object>();
        }

        public string Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string GraphApiVersion { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> AdditionalFields { get; set; }
    }
}