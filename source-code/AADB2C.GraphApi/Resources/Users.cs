using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using AADB2C.GraphApi.GraphClient;
using AADB2C.GraphApi.Models;
using AADB2C.GraphApi.PutOnNuget;
using AADB2C.GraphApi.PutOnNuget.ConsoleOptions;
using AADB2C.GraphApi.PutOnNuget.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AADB2C.GraphApi.Resources
{
    public class Users : Resource
    {
        private static HttpMethod HttpPatch = new HttpMethod("PATCH");
        private const string ApiPath = "/users";

        public Users(Tenant tenant) : base(tenant) { }

        public override async Task Run()
        {
            var options = new ConsoleOptions<Func<Task>>("What would you like to do?", new[]
            {
                new Option<Func<Task>>("Export user list", UserLists),
                new Option<Func<Task>>("Edit a user", ManageUser),
                new Option<Func<Task>>("Create a user", CreateUser)
            });

            Func<Task> f;

            do
            {
                try
                {
                    f = options.Ask()?.Value;
                    if (f == null) break;

                    await f.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error(e.UnwrapForLog());
                }
            } while (true);
        }

        private async Task CreateUser()
        {
            return;

            var fields = new Dictionary<string, (Func<string, string> mutate, Func<string, bool> isValid)>
            {
                {"extension_06dadf3521af4230bd90cdfaaef028b3_authApi", (null, null)}
            };
            JObject user = null;
            bool? ready = false;

            do
            {
                user = new JObject();

                foreach (var field in fields)
                {
                    ConsoleOptions.GetInput($"{field.Key}: ", field.Value.mutate, field.Value.isValid);
                }

                Log.PrintJson(user);
                ready = ConsoleOptions.YesNoCancel("Submit?");
            } while (ready != false);

            if (!ready.HasValue) return;



            try
            {
                var resp = await _graph.SendGraphRequest(HttpMethod.Post, _graph.BuildUrl(ApiPath, null), user.ToString());

                if (resp.IsNullOrWhitespace()) Log.Success("Update completed");
                else Log.Error($"Update failed: {resp}");
            }
            catch (Exception e)
            {
                if (Json.TryParse(e.Message) is JToken jt) Log.Error(jt.ToString(Formatting.Indented));
                else Log.Error(e.UnwrapForLog());
            }
        }

        private async Task UserLists()
        {
            const string dateFormat = "YYYY-MM-DD";
            var userFilter = ConsoleOptions.GetInput($"Filter (empty for none | # of days | {dateFormat})", s =>
            {
                if (s.IsNullOrWhitespace()) return new UserFilter(null, $"$top={Settings.PageSize}");

                var date = s.ParseDate(dateFormat);

                if (!date.HasValue)
                {
                    var i = s.ParseInt();

                    if (i.HasValue) date = DateTime.Today.AddDays(i.Value * -1);
                }

                return date.HasValue ? new UserFilter(null, $"$filter=createdDateTime ge datetime'{date}T00:00:00Z'&$top={Settings.PageSize}") : null;
            }, f => f != null);

            if (userFilter == null) return;

            var result = await QueryUsers(userFilter);

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

        private async Task ManageUser()
        {
            UserFilter userFilter = null;
            JObject user = null;
            string name = null;

            async Task askForUser()
            {
                userFilter = ConsoleOptions.GetInput("Which user? (objectId || signInName || displayName)", mutate: r =>
                {
                    var id = r.ParseGuid();

                    if (id.HasValue) return new UserFilter(id.Value, null);

                    try
                    {
                        var mailAddress = new MailAddress(r);

                        return new UserFilter(null, $"$filter=signInNames / any(x: x / value eq '{r}')");
                    }
                    catch (Exception)
                    {
                        return new UserFilter(null, $"$filter=displayName eq '{r}'");
                    }
                });
            }

            async Task search()
            {
                if (userFilter == null) return;

                user = await QueryUser(userFilter);

                if (user == null) return;

                name = user["displayName"].ToString();

                if (ConsoleOptions.YesNo($"Found {name}. Print user details?")) Log.PrintJson(user);
            }

            await askForUser();

            do
            {
                await search();
                if (user == null && !ConsoleOptions.YesNo("Search again?")) return;
            } while (user == null);


            var options = new ConsoleOptions<Func<JObject, Task>>($"What would you like to do with {name}?", new[]
            {
                new Option<Func<JObject, Task>>("Update", j => Update(j).Then(search)),
                new Option<Func<JObject, Task>>("Print user details", async j => Log.PrintJson(j)),
                new Option<Func<JObject, Task>>("Get another user", async j => await askForUser().Then(search))
            });

            Func<JObject, Task> f = null;

            while (userFilter != null)
            {
                try
                {
                    f = options.Ask()?.Value;
                    if (f == null) break;

                    await f.Invoke(user);
                }
                catch (Exception e)
                {
                    Log.Error(e.UnwrapForLog());
                }
            }
        }

        private async Task Update(JObject user)
        {
            const char singleQuote = '\'';
            const char doubleQuote = '"';
            var sample = "field1=value; field2=; field3=\"\"";

            Dictionary<string, string> Mutate(string input)
            {
                var fields = input.Split(";", StringSplitOptions.RemoveEmptyEntries)
                    .Select(fp =>
                    {
                        var spl = fp.Trim().Split("=", StringSplitOptions.RemoveEmptyEntries).Select(ss => ss.Trim()).ToArray();

                        if (spl.Length > 2) return (KeyValuePair<string, string>?) null;

                        var f = spl[0];
                        var v = spl.Length == 2 ? spl[1] : null;

                        if (v?.StartsWith(doubleQuote) == true) v = v.Trim(doubleQuote);
                        else if (v?.StartsWith(singleQuote) == true) v = v.Trim(singleQuote);

                        return new KeyValuePair<string, string>(f, v);
                    })
                    .ToArray();

                if (fields.Any(f => f == null)) return null;

                var dupes = fields.GroupBy(f => f.Value.Key).Where(f => f.Count() > 1).Select(f => f.Key).ToArray();

                if (dupes.Any())
                {
                    Log.Error($"Duplicate field(s) defined {dupes.Join(", ")}");
                    return null;
                }

                return fields.ToDictionary();
            }

            bool IsValid(Dictionary<string, string> result)
            {
                if (result?.Any() != true)
                {
                    Log.Error($"Invalid input '{sample}'");
                    return false;
                }

                return true;
            }

            var fieldsToUpdate = ConsoleOptions.GetInput($"Enter values as '{sample}'", Mutate, IsValid);

            if (fieldsToUpdate?.Any() == true)
            {
                try
                {
                    var resp = await _graph.SendGraphRequest(HttpPatch, _graph.BuildUrl($"{ApiPath}/{user["objectId"]}", null), JsonConvert.SerializeObject(fieldsToUpdate));

                    if (resp.IsNullOrWhitespace()) Log.Success("Update completed");
                    else Log.Error($"Update failed: {resp}");
                }
                catch (Exception e)
                {
                    if (Json.TryParse(e.Message) is JToken jt) Log.Error(jt.ToString(Formatting.Indented));
                    else Log.Error(e.UnwrapForLog());
                }
            }
        }

        private async Task<JObject> QueryUser(UserFilter filter)
        {
            string json = null;

            try
            {
                var url = _graph.BuildUrl($"{ApiPath}/{filter.Id?.ToString()}", filter.Filter);

                json = await _graph.SendGraphRequest(HttpMethod.Get, url, null);

                var jt = Json.TryParse(json);

                if (jt is JObject jo)
                {
                    if (!(jo["value"] is JArray ja)) return jo;

                    var details = ja.Cast<JObject>()
                        .Select(u => new
                        {
                            user = u,
                            display = JObject.FromObject(new
                            {
                                displayName = u["displayName"],
                                signInNames = (u["signInNames"] as JArray)?.Select(s => s["value"].ToString())
                            }).ToString(Formatting.Indented)
                        });

                    if (!details.Any())
                    {
                        Log.Error("No users found");
                        return null;
                    }

                    return new ConsoleOptions<JObject>("Multiple users found", details.Select(u => new Option<JObject>(u.display, u.user))).Ask()?.Value;
                }

                if (jt != null) Log.Error($"Invalid response {jt.ToString(Formatting.Indented)}");
                else Log.Error($"Invalid response {json}");

                return null;
            }
            catch (Exception e)
            {
                Log.Error(
                    $@"Failed to retrieve user.
Response:
{json ?? "null"}

Exception:
{e.UnwrapForLog()}");

                return null;
            }
        }

        private async Task<JArray> QueryUsers(UserFilter filter)
        {
            var url = _graph.BuildUrl(ApiPath, filter.Filter);
            var result = new List<JObject>();
            var i = 0;

            do
            {
                i++;

                // Print page number
                Log.Info($"Getting page #{i}");

                // Query Graph 
                var json = await _graph.SendGraphRequest(HttpMethod.Get, url, null);
                var jt = Json.TryParse(json);

                if (jt is JObject jo && jo["value"] is JArray ja) result.AddRange(ja.Cast<JObject>());
                else
                {
                    if (jt != null) Log.Error($"Invalid response {jt.ToString(Formatting.Indented)}");
                    else Log.Error($"Invalid response {json}");

                    return null;
                }

                // Get next link url
                var root = GraphRootElementModel.Parse(json);

                if (root == null || string.IsNullOrEmpty(root.odata_nextLink)) url = null;
                else url = url + "&$" + root.odata_nextLink;
            } while (string.IsNullOrEmpty(url) == false);

            return JArray.FromObject(result);
        }

        private class UserFilter
        {
            public UserFilter(Guid? id, string filter)
            {
                Id = id;
                Filter = filter;
            }

            public Guid? Id { get; }
            public string Filter { get; }
        }
    }
}