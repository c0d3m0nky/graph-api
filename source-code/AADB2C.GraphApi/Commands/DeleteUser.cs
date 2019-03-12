using AADB2C.GraphApi.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AADB2C.GraphApi.Commands
{
    public class DeleteUser
    {
        private AppSettingsModel AppSettings;
        AzureADGraphClient AzureADGraphClient;

        public DeleteUser(AppSettingsModel appSettings)
        {
            AppSettings = appSettings;

            this.AzureADGraphClient = new AzureADGraphClient(
                appSettings.Tenant,
                appSettings.ClientId,
                appSettings.ClientSecret,
                appSettings.GraphApiVersion);
        }

        public async Task Run()
        {
            string graphApiUrl = string.Empty;

            Console.WriteLine("  To delete a user, please type the user objectId");

            string value = Console.ReadLine();

            // Search by user object Id
            graphApiUrl = this.AzureADGraphClient.BuildUrl($"/users/{value}", null);

            // Query Graph 
            var json = await this.AzureADGraphClient.SendGraphRequest(HttpMethod.Delete, graphApiUrl, null);

            // Output the data
            Console.WriteLine(json);
        }
    }
}
