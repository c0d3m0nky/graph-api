using AADB2C.GraphApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AADB2C.GraphApi.Commands
{
    public class GetUsers
    {
        private AppSettingsModel AppSettings;
        AzureADGraphClient AzureADGraphClient;

        public GetUsers(AppSettingsModel appSettings)
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
            Console.WriteLine("To export users who created recently, type the nubmer of days");
            Console.WriteLine("To export users who created from specific date, type the date in YYYY-MM-DD format.");
            Console.WriteLine("Leave empty to export all users.");
            string fromDate = Console.ReadLine();
            int days;

            if (int.TryParse(fromDate, out days))
                fromDate = DateTime.Now.AddDays(days * (-1)).ToString("yyyy-MM-dd");

            string graphApiUrl = string.Empty;

            if (string.IsNullOrEmpty(fromDate))
            {
                graphApiUrl = this.AzureADGraphClient.BuildUrl("/users",
                                            $"$top={AppSettings.PageSize}");
            }
            else
            {
                graphApiUrl = this.AzureADGraphClient.BuildUrl("/users",
                                $"$filter=createdDateTime ge datetime'{fromDate}T00:00:00Z'&$top={this.AppSettings.PageSize}");
            }

            await Search(graphApiUrl);
        }


        private async Task Search(string graphUrl)
        {

            // Create an output folder
            string outputFolder = Path.Combine(this.AppSettings.OutputFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            string url = graphUrl;
            Directory.CreateDirectory(outputFolder);

            int i = 0;
            do
            {
                i++;

                // Print page number
                Console.WriteLine($"Getting page #{i}");

                // Query Graph 
                var json = await this.AzureADGraphClient.SendGraphRequest(HttpMethod.Get, url, null);

                // Get next link url
                GraphRootElementModel root = GraphRootElementModel.Parse(json);
                if (root ==  null || string.IsNullOrEmpty(root.odata_nextLink))
                    url = null;
                else
                    url = graphUrl + "&$" + root.odata_nextLink;

                // Save the data
                string filePath = Path.Combine(outputFolder, $"Users_{i.ToString("0000")}.txt");
                File.WriteAllText(filePath, json);

            } while (string.IsNullOrEmpty(url) == false);

        }
    }
}
