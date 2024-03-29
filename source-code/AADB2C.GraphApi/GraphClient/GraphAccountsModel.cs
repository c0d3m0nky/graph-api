﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace AADB2C.GraphApi.GraphClient
{
    public class GraphAccountsModel
    {
        public string odatametadata { get; set; }
        public List<GraphAccountModel> value { get; set; }

        public static GraphAccountsModel Parse(string JSON)
        {
            return JsonConvert.DeserializeObject(JSON.Replace("odata.metadata", "odatametadata"), typeof(GraphAccountsModel)) as GraphAccountsModel;
        }
    }
}
