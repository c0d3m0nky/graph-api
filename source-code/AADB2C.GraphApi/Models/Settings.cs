using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AADB2C.GraphApi.PutOnNuget.ConsoleOptions;
using AADB2C.GraphApi.PutOnNuget.Extensions;
using AADB2C.GraphApi.PutOnNuget;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AADB2C.GraphApi.Models
{
    public static class Settings
    {
        private const string DefaultApiVersion = "1.6";
        private static AppSettingsModel _settings = null;

        public static async Task Init(string credentialsFilePath = null, string tenantId = null)
        {
            FileInfo credentialsFile = null;

            try
            {
                credentialsFile = credentialsFilePath.IsNullOrWhitespace() ? null : new FileInfo(credentialsFilePath);
            }
            catch (Exception e)
            {
                Log.Error($"Invalid credentials file path {credentialsFilePath}'");
                return;
            }

            tenantId = tenantId?.Trim();
            Credentials = null;
            ActiveTenant = null;

            var configFile = new FileInfo(Path.GetFullPath($"./startup.json"));

            if (configFile.Exists)
            {
                try
                {
                    _settings = JsonConvert.DeserializeObject<AppSettingsModel>(await configFile.ReadAllTextAsync());
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to lo load {configFile.Name}: {e.Message}");
                }
            }

            _settings = _settings ?? new AppSettingsModel();

            await SetCredentials(credentialsFile ?? _settings.CredentialsFile, tenantId);
        }

        public static int PageSize => _settings.PageSize;
        public static DirectoryInfo OutputFolder => _settings.OutputFolder;
        public static Credentials Credentials { get; private set; }
        public static FileInfo CredentialsFile => _settings.CredentialsFile;
        public static Tenant ActiveTenant { get; private set; }

        public static async Task SetCredentials(FileInfo credentialsFile, string tenantId = null)
        {
            _settings.CredentialsFile = credentialsFile;

            if (_settings.CredentialsFile.Exists)
            {
                try
                {
                    Credentials = JsonConvert.DeserializeObject<Credentials>(await _settings.CredentialsFile.ReadAllTextAsync());
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to lo load {_settings.CredentialsFile.Name}: {e.Message}");
                    Credentials = null;
                }
            }

            if (Credentials?.Tenants?.Any() != true)
            {
                Log.Info("No tenants in credentials file");

                var ok = false;
                Tenant tenant = null;

                while (!ok)
                {
                    tenant = AskForNewTenantDetails();

                    if (tenant == null) return;

                    Log.PrintJson(tenant);

                    ok = ConsoleOptions.YesNo("Ok?");
                }

                Credentials = new Credentials(new[] {tenant});

                var save = ConsoleOptions.YesNo("Save?");

                if (save) await SaveCredentials();
            }
            else if (Credentials.Tenants.Count() > 1)
            {
                if (!tenantId.IsNullOrWhitespace())
                {
                    ActiveTenant = Credentials.Tenants.FirstOrDefault(t => string.Equals(t.Id, tenantId, StringComparison.CurrentCultureIgnoreCase));

                    if (ActiveTenant == null) Log.Error($"Tenant {tenantId} not configured");
                }

                if (ActiveTenant == null) ChangeActiveTenant();
            }
            else ActiveTenant = Credentials.Tenants.First();
        }

        public static async Task SaveCredentials()
        {
            var temp = _settings.CredentialsFile.Directory.File($"{C.HiddenFilePrefix}tempcreds.json");

            do
            {
                try
                {
                    temp.Refresh();

                    if (temp.Exists) temp.Delete();

                    temp.Refresh();

                    if (C.IsWindows) temp.Attributes = FileAttributes.Hidden;

                    using (var w = temp.CreateText()) await w.WriteLineAsync(JsonConvert.SerializeObject(Credentials, Formatting.Indented));

                    temp.Refresh();
                    _settings.CredentialsFile.Refresh();

                    if (_settings.CredentialsFile.Exists) _settings.CredentialsFile.Delete();

                    temp.MoveTo(_settings.CredentialsFile.FullName);

                    break;
                }
                catch (Exception e)
                {
                    Log.Error($"Save failed. {e.Message}");

                    if (!ConsoleOptions.YesNo("Try again?")) break;
                }
            } while (true);
        }

        public static void ChangeActiveTenant() => ActiveTenant = GetTenant();

        public static Tenant GetTenant()
        {
            return new ConsoleOptions<Func<Tenant>>("Choose a tenant",
                Credentials.Tenants
                    .Select(t => new Option<Func<Tenant>>(t.Id, () => t))
                    .Union(new Option<Func<Tenant>>("Manual entry",
                        () =>
                        {
                            var t = AskForNewTenantDetails();

                            if (t != null && ConsoleOptions.YesNo("Save?"))
                            {
                                Credentials.Tenants.Add(t);
                                SaveCredentials().GetAwaiter().GetResult();
                            }

                            return t;
                        }))
            ).Ask()?.Value();
        }

        public static Tenant AskForNewTenantDetails(bool edit = false)
        {
            var id = ConsoleOptions.GetInput("Tenant Id:", s => edit || !s.IsNullOrWhitespace(), allowCancel: !edit);

            if (!edit && id.IsNullOrWhitespace()) return null;

            var emptyResponse = true;

            var clientId = ConsoleOptions.GetInput("Client Id:", r =>
            {
                emptyResponse = r.IsNullOrWhitespace();
                return r.ParseGuid();
            }, g => g.HasValue || (edit && emptyResponse), allowCancel: !edit);

            if (!edit && !clientId.HasValue) return null;

            var secret = ConsoleOptions.GetInput("Client Secret:", s => !s.IsNullOrWhitespace(), allowCancel: !edit);

            if (!edit && secret.IsNullOrWhitespace()) return null;

            var version = ConsoleOptions.GetInput($"Graph Api Version ({DefaultApiVersion}):", s => s.IfNullOrWhitespace(DefaultApiVersion), allowCancel: !edit);

            if (!edit && version.IsNullOrWhitespace()) return null;

            return new Tenant(id, clientId ?? default, secret, version, ConsoleOptions.GetFields("Enter additional fields"));
        }

        public static JObject Describe() => JObject.FromObject(_settings);

        private class AppSettingsModel
        {
            public AppSettingsModel() { }

            [JsonConstructor]
            public AppSettingsModel(int? pageSize, string outputFolder, string credentialsFile)
            {
                if (pageSize.HasValue) PageSize = pageSize.Value;
                try
                {
                    if (!outputFolder.IsNullOrWhitespace())
                    {
                        OutputFolder = new DirectoryInfo(Path.GetFullPath(outputFolder.Replace("~/", $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/")));
                        if (!OutputFolder.Exists)
                        {
                            OutputFolder.Create();
                            OutputFolder.Refresh();
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to lo load {nameof(outputFolder)} from config: {outputFolder} {e.Message}");
                }

                SetCredentials(credentialsFile);
            }

            public int PageSize { get; set; } = 200;

            [JsonConverter(typeof(FileSystemInfoJsonConverter))]
            public DirectoryInfo OutputFolder { get; set; } = new DirectoryInfo(Environment.CurrentDirectory);

            [JsonConverter(typeof(FileSystemInfoJsonConverter))]
            public FileInfo CredentialsFile { get; set; } = CredsFromPath("~/");

            public void SetCredentials(string creds) => CredentialsFile = CredsFromPath(creds) ?? CredentialsFile;

            private static FileInfo CredsFromPath(string creds)
            {
                if (creds.IsNullOrWhitespace()) return null;

                var defaultFileName = $"{C.HiddenFilePrefix}azuregraph.json";

                try
                {
                    creds = creds.Replace("~/", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                    return File.GetAttributes(creds).HasFlag(FileAttributes.Directory) ? new FileInfo(Path.Combine(creds, defaultFileName)) : new FileInfo(creds);
                }
                catch (Exception e)
                {
                    Log.Error($"Invalid path {creds}");
                    return null;
                }
            }
        }
    }
}