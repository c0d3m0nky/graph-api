using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AADB2C.GraphApi.Models;
using AADB2C.GraphApi.PutOnNuget.ConsoleOptions;
using AADB2C.GraphApi.PutOnNuget.Extensions;
using Newtonsoft.Json.Linq;

namespace AADB2C.GraphApi.Resources
{
    public class Configuration : Resource
    {
        private static Func<FileSystemInfo, Task> OpenDirectory = GetOpenDirFunc();
        private const string EditTenantNull = "!null!";

        public Configuration(Tenant tenant) : base(tenant) { }

        public override async Task Run()
        {
            var opt = new ConsoleOptions<Func<Task>>("Choose what to edit", new[]
            {
                new Option<Func<Task>>("Credentials - Switch file", SwitchCreds),
                new Option<Func<Task>>("Credentials - Edit", EditCreds),
                OpenDirectory != null ? new Option<Func<Task>>("Credentials - Open directory", () => OpenDirectory(Settings.CredentialsFile)) : null,
                new Option<Func<Task>>("Tenant - Edit", EditTenant),
                new Option<Func<Task>>("Tenant - Add", AddTenant),
                new Option<Func<Task>>("Tenant - Delete", DeleteTenant)
            }.Where(o => o != null), "Done");

            Func<Task> func;

            do
            {
                if (!Settings.Credentials.Tenants.Any()) opt.Options.ForEach(o => o.Value.Show = !o.Value.Description.StartsWith("Tenant"));
                func = opt.Ask()?.Value;

                if (func == null) break;

                await func();
            } while (true);
        }

        private Tenant WhichTenant()
            => new ConsoleOptions<Tenant>("Which tenant?",
                Settings.Credentials.Tenants
                    .Select(t =>
                    {
                        var isActive = t == Settings.ActiveTenant;

                        return new Option<Tenant>($"{t.Id}{(isActive ? " (Active)" : "")}", t, isActive ? ConsoleColor.Green : (ConsoleColor?) null);
                    })).Ask()?.Value;

        private async Task SwitchCreds() { }

        private async Task EditTenant()
        {
            var tenantEdit = WhichTenant();

            if (tenantEdit == null) return;

            Console.WriteLine($"Leave empty for unchanged. Enter {EditTenantNull} to null out");
            var tenant = Settings.AskForNewTenantDetails(true);

            tenant.Id = tenant.Id.IfNullOrWhitespace(tenantEdit.Id);
            tenant.ClientId = tenant.ClientId == default ? tenantEdit.ClientId : tenant.ClientId;
            tenant.ClientSecret = tenant.ClientSecret.IfNullOrWhitespace(tenantEdit.ClientSecret);
            tenant.GraphApiVersion = tenant.GraphApiVersion.IfNullOrWhitespace(tenantEdit.GraphApiVersion);
            tenant.AdditionalFields =
                tenantEdit.AdditionalFields.Merge(tenant.AdditionalFields, v => v.merge is string s && s == EditTenantNull ? null : v.merge ?? v.source);

            var cjs = JObject.FromObject(tenantEdit);
            var njs = JObject.FromObject(tenant);

            Log.PrintDiff(cjs, njs);

            if (ConsoleOptions.YesNo("Save?"))
            {
                tenantEdit.Id = tenant.Id;
                tenantEdit.ClientId = tenant.ClientId;
                tenantEdit.ClientSecret = tenant.ClientSecret;
                tenantEdit.GraphApiVersion = tenant.GraphApiVersion;
                tenantEdit.AdditionalFields = tenant.AdditionalFields;
                await Settings.SaveCredentials();
            }
        }

        private async Task DeleteTenant()
        {
            var tenant = WhichTenant();

            if (tenant == null) return;

            var isActive = tenant == Settings.ActiveTenant;
            var isActiveMsg = isActive ? ", this is the active tenant" : "";

            if (ConsoleOptions.YesNo($"Are you sure you want to delete {tenant.Id}{isActiveMsg}"))
            {
                await Settings.SaveCredentials();
                if (isActive) Settings.ChangeActiveTenant();
            }
        }

        private Task AddTenant()
        {
            throw new NotImplementedException();
        }

        private Task EditCreds()
        {
            throw new NotImplementedException();
        }

        private static Func<FileSystemInfo, Task> GetOpenDirFunc()
        {
            Func<FileSystemInfo, DirectoryInfo> clean = fsi =>
            {
                if (!(fsi is DirectoryInfo dir))
                {
                    if (fsi is FileInfo fi) return fi.Directory;

                    Log.Error($"Can't open directory for {fsi}'");
                    return null;
                }

                return dir;
            };

            if (C.IsWindows)
            {
                return async fsi =>
                {
                    var dir = clean(fsi);

                    if (dir == null) return;

                    var escapedArgs = $"explorer {dir}";
                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"\"{escapedArgs}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };

                    process.Start();

                    var result = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!error.IsNullOrWhitespace()) Log.Error(error);

                    if (!result.IsNullOrWhitespace()) Log.Info(result);
                };
            }

            if (C.IsMac)
            {
                return async fsi =>
                {
                    var dir = clean(fsi);

                    if (dir == null) return;

                    var escapedArgs = $"open {dir}";
                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "/bin/bash",
                            Arguments = $"-c \"{escapedArgs}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };

                    process.Start();

                    var result = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!error.IsNullOrWhitespace()) Log.Error(error);

                    if (!result.IsNullOrWhitespace()) Log.Info(result);
                };
            }

            return null;
        }
    }
}