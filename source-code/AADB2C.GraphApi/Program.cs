using AADB2C.GraphApi.Commands;
using AADB2C.GraphApi.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AADB2C.GraphApi
{

    class Program
    {
        static private AppSettingsModel AppSettings;

        static async Task Main(string[] args)
        {
            // Get the app settings
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            Program.AppSettings = new AppSettingsModel();
            config.GetSection("AppSettings").Bind(Program.AppSettings);

            // Set the output folder
            if (string.IsNullOrEmpty(AppSettings.OutputFolder))
            {
                Program.AppSettings.OutputFolder = Directory.GetCurrentDirectory();
            }
            else
            {
                if (!Directory.Exists(Program.AppSettings.OutputFolder))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Output folder '{Program.AppSettings.OutputFolder}' not found.");
                }
            }

            // Get command
            Console.WriteLine("Type the command number and click enter");
            Console.WriteLine("  Type 1 to export the auditing log");
            Console.WriteLine("  Type 2 to export the usage log");
            Console.WriteLine("  Type 3 to export all users in the directory");
            Console.WriteLine("  Type 4 to get a specific user");
            Console.WriteLine("  Type 5 to delete a user");
            bool correctInput = false;
            while (!correctInput)
            {
                correctInput = true;

                string decision = Console.ReadLine();
                int iDecision;
                if (int.TryParse(decision, out iDecision))
                    switch (iDecision)
                    {
                        case 1:
                            AuditLog auditLogs = new AuditLog(Program.AppSettings);
                            await auditLogs.Run();
                            break;
                        case 2:
                            UsageReport usageReport = new UsageReport(Program.AppSettings);
                            await usageReport.Run();
                            break;
                        case 3:
                            GetUsers getUsers = new GetUsers(Program.AppSettings);
                            await getUsers.Run();
                            break;
                        case 4:
                            GetUser getUser = new GetUser(Program.AppSettings);
                            await getUser.Run();
                            break;
                        case 5:
                            DeleteUser deleteUser = new DeleteUser(Program.AppSettings);
                            await deleteUser.Run();
                            break;                    }
                else
                {
                    correctInput = false;
                }
            }

            Console.WriteLine("The report is ready!!!");
            Console.ReadLine();
        }


    }
}
