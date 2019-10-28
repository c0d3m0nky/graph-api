using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AADB2C.GraphApi.Models;
using AADB2C.GraphApi.PutOnNuget.ConsoleOptions;
using AADB2C.GraphApi.PutOnNuget.Extensions;
using AADB2C.GraphApi.Resources;

namespace AADB2C.GraphApi
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                await Settings.Init(args.FirstOrDefault(a => a.StartsWith("-c:"))?.Substring(3),args.FirstOrDefault(a => a.StartsWith("-t:"))?.Substring(3));
            
                Log.PrintJson(Settings.Describe());

                Settings.ChangeActiveTenant();
            
                if(Settings.ActiveTenant == null) return;

                Log.Success($"Connected to {Settings.ActiveTenant.Id}");

                var script = args.FirstOrDefault(a => a.StartsWith("-s:"))?.Substring(3);

                if (script.IsNullOrWhitespace())
                {
                    while (Settings.ActiveTenant != null && await Run()) Settings.ChangeActiveTenant();
                }
                else
                {
                    ConsoleOptions.Script = ScriptEnumerator(script);
                    await Run();
                }
            }
            catch (Exception e)
            {
                Log.Error(e.UnwrapForLog());
            }
        }

        private static IEnumerator<string> ScriptEnumerator(string script)
        {
            const char openBrace = '[';
            const char closeBrace = ']';
            const char escapeChar = '\\';
            const char pauseChar = '#';

            var i = 0;
            string blockBuffer = null;

            while (i < script.Length)
            {
                var c = script[i];

                if (blockBuffer.IsNullOrWhitespace())
                {
                    if (c == escapeChar)
                    {
                        i++;
                        c = script[i];
                        yield return c.ToString();
                    }
                    else if (c == openBrace) blockBuffer += c;
                    else if (c == pauseChar)
                    {
                        var r = ConsoleOptions.GetInput($"Scripted response is {script[i + 1]}. Enter override value of blank to continue.", s => s.IfNullOrWhitespace(null)?.FirstOrDefault());

                        if (r != null)
                        {
                            yield return r.ToString();

                            i++;
                        }
                    }
                    else yield return c.ToString();
                }
                else if (c == escapeChar)
                {
                    i++;
                    c = script[i];
                    blockBuffer += c;
                }
                else if (c == closeBrace)
                {
                    yield return blockBuffer;

                    blockBuffer = null;
                }
                else blockBuffer += c;

                i++;
            }

            if (!blockBuffer.IsNullOrWhitespace()) yield return blockBuffer;
        }

        private static async Task<bool> Run()
        {
            var options = new ConsoleOptions<Resource>("What would you like to run?", new[]
            {
//                new Option<Resource>("export the auditing log", new AuditLog(Settings.ActiveTenant)),
//                new Option<Resource>("export the usage log", new UsageReport(Settings.ActiveTenant)),
                new Option<Resource>("User management", new Users(Settings.ActiveTenant)),
                new Option<Resource>("Settings", new Configuration(Settings.ActiveTenant))
            });

            var cmd = options.Ask();

            if (cmd == null) return false;

            await cmd.Value.Run();

            return Console.ReadLine().ParseBool() != false;
        }

        
    }
}