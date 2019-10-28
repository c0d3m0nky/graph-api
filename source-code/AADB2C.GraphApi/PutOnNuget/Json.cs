using System;
using AADB2C.GraphApi.PutOnNuget.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AADB2C.GraphApi.PutOnNuget
{
    public static class Json
    {
        public static JObject EmptyObject = JObject.Parse("{}");
        public static JArray EmptyArray = JArray.Parse("[]");

        public static JToken TryParse(string json) => TryParse<JToken>(json);

        public static TJ TryParse<TJ>(string str) where TJ : JToken
        {
            try
            {
                return JToken.Parse(str) as TJ;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool TryParse<TJ>(string json, out TJ j) where TJ : JToken
        {
            j = null;

            try
            {
                j = JToken.Parse(json) as TJ;
                return j != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static JValue Parse(string rawValue)
        {
            if (rawValue == null) return null;
            if (rawValue == "null" || rawValue == "undefined") return null;
            if (rawValue.StartsWith('"') && rawValue.EndsWith('"')) return new JValue(rawValue.Trim('"'));
            if (int.TryParse(rawValue, out var i)) return new JValue(i);
            if (double.TryParse(rawValue, out var d)) return new JValue(d);
            if (long.TryParse(rawValue, out var l)) return new JValue(l);
            if (bool.TryParse(rawValue, out var b)) return new JValue(b);
            if (DateTime.TryParse(rawValue, out var dt)) return new JValue(dt);
            if (Guid.TryParse(rawValue, out var g)) return new JValue(g);
            
            return new JValue(rawValue);
        }
    }
}