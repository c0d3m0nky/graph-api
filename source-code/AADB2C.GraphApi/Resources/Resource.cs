using System.Threading.Tasks;
using AADB2C.GraphApi.GraphClient;
using AADB2C.GraphApi.Models;

namespace AADB2C.GraphApi.Resources {
    public abstract class Resource
    {
        protected readonly AzureADGraphClient _graph;

        protected Resource(Tenant tenant)
        {
            _graph = new AzureADGraphClient(
                tenant.Id,
                tenant.ClientId.ToString(),
                tenant.ClientSecret,
                tenant.GraphApiVersion);
        }

        public abstract Task Run();

    }
}