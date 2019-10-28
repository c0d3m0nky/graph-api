using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AADB2C.GraphApi.GraphClient;
using AADB2C.GraphApi.Models;
using AADB2C.GraphApi.PutOnNuget.ConsoleOptions;
using AADB2C.GraphApi.PutOnNuget.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AADB2C.GraphApi.Resources.Original
{
    public class GetUsers : Resource
    {
        public GetUsers(Tenant tenant) : base(tenant)
        {
            throw new NotImplementedException();
        }

        public override async Task Run()
        {
            Log.Info(
                @"To export users who created recently, type the nubmer of days
To export users who created from specific date, type the date in YYYY-MM-DD format.
Leave empty to export all users.
");
            var fromDate = Console.ReadLine();
            int days;

            if (int.TryParse(fromDate, out days)) fromDate = DateTime.Now.AddDays(days * (-1)).ToString("yyyy-MM-dd");

            var graphApiUrl = string.Empty;

            if (string.IsNullOrEmpty(fromDate)) graphApiUrl = _graph.BuildUrl("/users", $"$top={Settings.PageSize}");
            else graphApiUrl = _graph.BuildUrl("/users", $"$filter=createdDateTime ge datetime'{fromDate}T00:00:00Z'&$top={Settings.PageSize}");

            var result = await Search(graphApiUrl);

            if (ConsoleOptions.YesNo("Minify?")) result = result.Minify(MinifyOptions.RemoveEmpty);

            if (ConsoleOptions.YesNo("Export to file?"))
            {
                var split = result.Count > 1 && ConsoleOptions.YesNo("Split?");

                if (split)
                {
                    var outputFolder = Settings.OutputFolder.Subdirectory(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

                    outputFolder.Create();

                    result.ForEach((t, i) => File.WriteAllText(outputFolder.File($"users_{i:0000}.json").FullName, t.ToString(Formatting.Indented)));
                }
                else File.WriteAllText(Settings.OutputFolder.File($"users_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json").FullName, result.ToString(Formatting.Indented));
            }
            else Log.PrintJson(result);
        }

        private async Task<JArray> Search(string graphUrl)
        {
            var url = graphUrl;
            var result = new List<JObject>();
            var i = 0;

            do
            {
                i++;

                // Print page number
                Log.Info($"Getting page #{i}");

                // Query Graph 
                var json = await _graph.SendGraphRequest(HttpMethod.Get, url, null);

                result.AddRange((JObject.Parse(json)["value"] as JArray).Cast<JObject>());

                // Get next link url
                var root = GraphRootElementModel.Parse(json);

                if (root == null || string.IsNullOrEmpty(root.odata_nextLink)) url = null;
                else url = graphUrl + "&$" + root.odata_nextLink;
            } while (string.IsNullOrEmpty(url) == false);

            return JArray.FromObject(result);
        }
    }
}