using AADB2C.GraphApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AADB2C.GraphApi.Commands
{
    public class AuditLog
    {
        private AppSettingsModel AppSettings;
        AzureADGraphClient AzureADGraphClient;

        public AuditLog(AppSettingsModel appSettings)
        {
            AppSettings = appSettings;

            this.AzureADGraphClient = new AzureADGraphClient(
                appSettings.Tenant,
                appSettings.ClientId,
                appSettings.ClientSecret,
                appSettings.GraphApiBetaVersion);
        }

        public async Task Run()
        {
            // Create an output folder
            string outputFolder = Path.Combine(this.AppSettings.OutputFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            Directory.CreateDirectory(outputFolder);

            // Set the report URL
            string graphUrl = this.AzureADGraphClient.BuildUrl("/activities/audit",
                $"$filter=category eq 'B2C'&$top={AppSettings.PageSize}");

            string url = graphUrl;

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
                if (root == null || string.IsNullOrEmpty(root.odata_nextLink))
                    url = null;
                else
                    url = graphUrl + "&$" + root.odata_nextLink;

                // Save the data
                string filePath = Path.Combine(outputFolder, $"Audit_{i.ToString("0000")}.txt");
                File.WriteAllText(filePath, json);

            } while (string.IsNullOrEmpty(url) == false);

        }
    }
}
