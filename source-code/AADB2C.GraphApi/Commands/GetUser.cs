using AADB2C.GraphApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AADB2C.GraphApi.Commands
{
    public class GetUser
    {
        private AppSettingsModel AppSettings;
        AzureADGraphClient AzureADGraphClient;

        public GetUser(AppSettingsModel appSettings)
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

            Console.WriteLine("  1) To search by a sign-in name, type an email address (for example: someone@contoso.com)");
            Console.WriteLine("  2) To search by a display name, type user dispaly name (for example: John Smith)");
            Console.WriteLine("  3) To search by a user ObjectId, type the Id (for example: 4500251d-3e66-480a-9210-ceb997e7b561)");

            string value = Console.ReadLine();

            if (value.Split("-").Length == 5)
            {
                // Search by user object Id
                graphApiUrl = this.AzureADGraphClient.BuildUrl($"/users/{value}", null);
            }
            else if (value.Contains("@") && value.Contains("."))
            {
                // Search by sign-in name
                graphApiUrl = this.AzureADGraphClient.BuildUrl("/users",
                                $"$filter=signInNames / any(x: x / value eq '{value}')");
            }
            else
            {
                // Serach by display name
                graphApiUrl = this.AzureADGraphClient.BuildUrl("/users",
                                $"$filter=displayName eq '{value}'");
            }

            await Search(graphApiUrl);
        }

        private async Task Search(string url)
        {
            // Query Graph 
            var json = await this.AzureADGraphClient.SendGraphRequest(HttpMethod.Get, url, null);

            // Output the data
            Console.WriteLine(json);
        }
    }
}
