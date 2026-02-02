using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Globalization;

namespace RateListener.Helpers;

public class JsonHelper
{
    public static string GetSerializedString(object obj, params JsonConverter[] converters)
    {
        if (converters.Length == 0)
            converters = [new Newtonsoft.Json.Converters.StringEnumConverter()];

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

public class SpacedDoubleConverter : JsonConverter<double>
{
    public override double ReadJson(JsonReader reader, Type objectType, 
                                    double existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
            {
                var value = reader.Value?.ToString() ?? string.Empty;
                value = value.Trim().Replace(" ", "").Replace("\u00A0", "");
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                    return result;
                break;
            }
            case JsonToken.Float:
            case JsonToken.Integer:
                return Convert.ToDouble(reader.Value);
        }
        throw new JsonSerializationException($"Unable to convert \"{reader.Value}\" (Token: {reader.TokenType}) to double.");
    }

    public override void WriteJson(JsonWriter writer, double value, JsonSerializer serializer) => 
        writer.WriteValue(value);
}