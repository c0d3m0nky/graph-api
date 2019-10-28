using System;
using System.Net.Http;
using System.Threading.Tasks;
using AADB2C.GraphApi.Models;

namespace AADB2C.GraphApi.Resources.Original
{
    public class DeleteUser : Resource
    {
        public DeleteUser(Tenant tenant) : base(tenant)
        {
            throw new NotImplementedException();
        }

        public override async Task Run()
        {
            string graphApiUrl = string.Empty;

            Log.Info("  To delete a user, please type the user objectId");

            string value = Console.ReadLine();

            // Search by user object Id
            graphApiUrl = this._graph.BuildUrl($"/users/{value}", null);

            // Query Graph 
            var json = await this._graph.SendGraphRequest(HttpMethod.Delete, graphApiUrl, null);

            // Output the data
            Log.Info(json);
        }
    }
}