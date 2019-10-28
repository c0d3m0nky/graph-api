using System;
using AADB2C.GraphApi.PutOnNuget.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace AADB2C.GraphApi.Models {
    public static class Log
    {
        public static void Error(string msg)
        {
            var fg = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine(BuildLogMsg(msg));
            Console.ForegroundColor = fg;
        }

        public static void Success(string msg)
        {
            var fg = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine(BuildLogMsg(msg));
            Console.ForegroundColor = fg;
        }

        public static void Info(string msg)
        {
            var fg = Console.ForegroundColor;

            Console.ForegroundColor = C.BaseForegroundColor;
            Console.WriteLine();
            Console.WriteLine(BuildLogMsg(msg));
            Console.ForegroundColor = fg;
        }

        private static string BuildLogMsg(string msg)
        {
            msg = msg.Trim();
            var now = $"{DateTime.Now:HH:mm:ss.ff}";

            return msg.Contains("\n") ? $"{now}{C.NL}{msg.Replace("\n", "\n\t")}" : $"{now}\t{msg}";
        }

        public static void PrintJson(object o)
        {
            var fg = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.DarkCyan;

            if (!(o is JToken j)) j = JToken.FromObject(o);

            Console.WriteLine(j.ToString(Formatting.Indented, MinifyOptions.RemoveEmpty));
            Console.ForegroundColor = fg;
        }
        
        private static readonly ConsoleColor AddedColor = ConsoleColor.Green;
        private const string AddedPrefix = "+";
        private static readonly ConsoleColor EditedColor = ConsoleColor.Blue;
        private const string EditedPrefix = ">";
        private static readonly ConsoleColor RemovedColor = ConsoleColor.Magenta;
        private const string RemovedPrefix = "-";
        private static readonly ConsoleColor UnchangedColor = ConsoleColor.DarkCyan;
        private const string UnchangedPrefix = " ";
        
        public static void PrintDiff(JObject currentJObj, JObject newObj)
        {
            var keys = currentJObj.Properties().Select(p => p.Name).Union(newObj.Properties().Select(p => p.Name)).Distinct();
            var currentColor = Console.ForegroundColor;

            Console.ForegroundColor = UnchangedColor;

            Console.WriteLine("\t{");

            foreach (var k in keys)
            {
                string prefix;
                ConsoleColor color;
                var cval = currentJObj[k]?.ToString();
                var nval = newObj[k]?.ToString();

                if (cval == nval)
                {
                    prefix = UnchangedPrefix;
                    color = UnchangedColor;
                }
                else if (cval.IsNullOrWhitespace())
                {
                    prefix = AddedPrefix;
                    color = AddedColor;
                }
                else if (nval.IsNullOrWhitespace())
                {
                    prefix = RemovedPrefix;
                    color = RemovedColor;
                }
                else
                {
                    prefix = EditedPrefix;
                    color = EditedColor;
                }

                Console.ForegroundColor = color;
                Console.WriteLine($"{prefix}\t\t{k}: {cval.IfNullOrWhitespace("null")} -> {nval.IfNullOrWhitespace("null")}");
                Console.ForegroundColor = UnchangedColor;
            }

            Console.WriteLine("\t}");
            Console.ForegroundColor = currentColor;
        }

    }
}