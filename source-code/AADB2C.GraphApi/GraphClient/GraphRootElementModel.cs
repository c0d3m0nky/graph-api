using Newtonsoft.Json;

namespace AADB2C.GraphApi.GraphClient
{
    public class GraphRootElementModel
    {
        public string odata_nextLink { get; set; }

        public static GraphRootElementModel Parse(string JSON)
        {
            GraphRootElementModel graphRootElementModel =  JsonConvert.DeserializeObject(JSON.Replace("odata.", "odata_"), typeof(GraphRootElementModel)) as GraphRootElementModel;

            if (graphRootElementModel == null || string.IsNullOrEmpty(graphRootElementModel.odata_nextLink))
                return graphRootElementModel;

            int index = graphRootElementModel.odata_nextLink.IndexOf("skiptoken=");

            if (index > 1)
            {
                graphRootElementModel.odata_nextLink = graphRootElementModel.odata_nextLink.Substring(index);
            }
            return graphRootElementModel;
        }
    }
}
