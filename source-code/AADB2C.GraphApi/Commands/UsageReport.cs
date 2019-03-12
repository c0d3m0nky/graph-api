using AADB2C.GraphApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AADB2C.GraphApi.Commands
{
    public class UsageReport
    {
        private AppSettingsModel AppSettings;
        AzureADGraphClient AzureADGraphClient;

        public UsageReport(AppSettingsModel appSettings)
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

            await Export(this.AzureADGraphClient.BuildUrl("/reports/tenantUserCount",
                        $"$top={this.AppSettings.PageSize}"), outputFolder, "UserCount");

            await Export(this.AzureADGraphClient.BuildUrl("/reports/b2cAuthenticationCountSummary",
                        $"$top={this.AppSettings.PageSize}"), outputFolder, "AuthenticationCountSummary");

            await Export(this.AzureADGraphClient.BuildUrl("/reports/b2cAuthenticationCount",
                        $"$top={this.AppSettings.PageSize}"), outputFolder, "AuthenticationCount");

            await Export(this.AzureADGraphClient.BuildUrl("/reports/b2cMfaRequestCountSummary",
                        $"$top={this.AppSettings.PageSize}"), outputFolder, "MFAAuthenticationCount");
        }


        private async Task Export(string graphUrl, string outputFolder,  string fileName)
        {
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
                string pageNumber = string.Empty;
                if (i > 1)
                    pageNumber = "_" + i.ToString();

                string filePath = Path.Combine(outputFolder, $"{fileName}{pageNumber}.txt");
                File.WriteAllText(filePath, json);

            } while (string.IsNullOrEmpty(url) == false);

        }
    }
}
