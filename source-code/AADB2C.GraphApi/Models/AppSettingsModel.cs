using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AADB2C.GraphApi.Models
{
    public class AppSettingsModel
    {
        public string Tenant { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public int PageSize { get; set; }
        public string OutputFolder { get; set; }
        public string GraphApiVersion { get; set; }

        public readonly string GraphApiBetaVersion = "beta";
    }
}
