using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AADB2C.GraphApi.GraphClient;
using AADB2C.GraphApi.Models;
using AADB2C.GraphApi.PutOnNuget.Extensions;

namespace AADB2C.GraphApi.Resources.Original
{
    public class AuditLog : Resource
    {
        public AuditLog(Tenant tenant) : base(tenant) { }

        public override async Task Run()
        {
            // Create an output folder
            var outputFolder = Settings.OutputFolder.Subdirectory(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

            outputFolder.Create();

            // Set the report URL
            string graphUrl = _graph.BuildUrl("/activities/audit",
                $"$filter=category eq 'B2C'&$top={Settings.PageSize}");

            string url = graphUrl;

            int i = 0;
            do
            {
                i++;

                // Print page number
                Log.Info($"Getting page #{i}");

                // Query Graph 
                var json = await _graph.SendGraphRequest(HttpMethod.Get, url, null);

                // Get next link url
                GraphRootElementModel root = GraphRootElementModel.Parse(json);
                if (root == null || string.IsNullOrEmpty(root.odata_nextLink))
                    url = null;
                else
                    url = graphUrl + "&$" + root.odata_nextLink;

                // Save the data
                var file = outputFolder.File($"Audit_{i.ToString("0000")}.txt");
                
                File.WriteAllText(file.FullName, json);
            } while (string.IsNullOrEmpty(url) == false);
        }
    }
}