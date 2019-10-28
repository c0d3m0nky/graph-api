using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AADB2C.GraphApi.GraphClient;
using AADB2C.GraphApi.Models;
using AADB2C.GraphApi.PutOnNuget.Extensions;

namespace AADB2C.GraphApi.Resources.Original
{
    public class UsageReport : Resource
    {
        public UsageReport(Tenant tenant) : base(tenant) { }

        public override async Task Run()
        {
            // Create an output folder
            var outputFolder = Settings.OutputFolder.Subdirectory(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            
            outputFolder.Create();

            await Export(this._graph.BuildUrl("/reports/tenantUserCount",
                $"$top={Settings.PageSize}"), outputFolder.FullName, "UserCount");

            await Export(this._graph.BuildUrl("/reports/b2cAuthenticationCountSummary",
                $"$top={Settings.PageSize}"), outputFolder.FullName, "AuthenticationCountSummary");

            await Export(this._graph.BuildUrl("/reports/b2cAuthenticationCount",
                $"$top={Settings.PageSize}"), outputFolder.FullName, "AuthenticationCount");

            await Export(this._graph.BuildUrl("/reports/b2cMfaRequestCountSummary",
                $"$top={Settings.PageSize}"), outputFolder.FullName, "MFAAuthenticationCount");
        }


        private async Task Export(string graphUrl, string outputFolder, string fileName)
        {
            string url = graphUrl;

            int i = 0;
            do
            {
                i++;

                // Print page number
                Log.Info($"Getting page #{i}");

                // Query Graph 
                var json = await this._graph.SendGraphRequest(HttpMethod.Get, url, null);

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