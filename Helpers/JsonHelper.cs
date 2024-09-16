using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RateListener.Helpers
{
    public class JsonHelper
    {
        public static string GetSerializedString(object obj, params JsonConverter[] converters)
        {
            if (converters.Length == 0)
            {
                converters = [new Newtonsoft.Json.Converters.StringEnumConverter()];
            }

            var settings = new JsonSerializerSettings
            {
                Converters = converters, 
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };
            return JsonConvert.SerializeObject(obj, settings);
        }

        public static T GetObjectFromString<T>(string json, params JsonConverter[] converters) =>
            JsonConvert.DeserializeObject<T>(json, converters);
    }
}
