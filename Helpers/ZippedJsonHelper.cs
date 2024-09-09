using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RateListener.Helpers
{
    public class ZippedJsonHelper
    {
        public static string GetSerializedAndZippedString(object obj, params JsonConverter[] converters)
        {
            var json = GetSerializedString(obj, converters);
            return GetZippedString(json);
        }

        public static byte[] GetSerializedAndZippedArray(object obj, params JsonConverter[] converters)
        {
            var json = GetSerializedString(obj, converters);
            return GetZippedArray(json);
        }

        public static T GetObjectFromZippedString<T>(string zippedJson, params JsonConverter[] converters)
        {
            var unzipped = GetUnzippedString(zippedJson);
            return JsonConvert.DeserializeObject<T>(unzipped, converters);
        }

        public static T GetObjectFromZippedArray<T>(byte[] zippedArray, params JsonConverter[] converters)
        {
            var unzipped = GetUnzippedStringFromArray(zippedArray);
            return JsonConvert.DeserializeObject<T>(unzipped, converters);
        }

        public static string GetUnzippedString(string zippedJson)
        {
            var bytes = Convert.FromBase64String(zippedJson);
            return GetUnzippedStringFromArray(bytes);
        }

        public static string GetUnzippedStringFromArray(byte[] zippedArray)
        {
            using (MemoryStream inputStream = new MemoryStream(zippedArray))
            {
                using (GZipStream gzip = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        public static byte[] GetUnzippedArray(byte[] zippedArray)
        {
            using (MemoryStream inputStream = new MemoryStream(zippedArray))
            {
                using (GZipStream gzip = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        gzip.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }
                }
            }
        }

        public static string GetSerializedString(object obj, params JsonConverter[] converters)
        {
            if (converters.Length == 0)
            {
                converters = new[] { new Newtonsoft.Json.Converters.StringEnumConverter() };
            }

            var settings = new JsonSerializerSettings
            {
                Converters = converters,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.SerializeObject(obj, settings);
        }

        public static T GetObjectFromString<T>(string json, params JsonConverter[] converters)
        {
            return JsonConvert.DeserializeObject<T>(json, converters);
        }

        public static T GetFullCopyOfObject<T>(T obj, params JsonConverter[] converters)
        {
            var json = GetSerializedString(obj, converters);
            return JsonConvert.DeserializeObject<T>(json, converters);
        }

        public static string GetZippedString(string source)
        {
            return Convert.ToBase64String(GetZippedArray(source));
        }

        public static byte[] GetZippedArray(string source)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    using (StreamWriter writer = new StreamWriter(gzip, Encoding.UTF8))
                    {
                        writer.Write(source);
                    }
                }
                return output.ToArray();
            }
        }

        public static string DecodeBase64String(string base64Source)
        {
            var bytes = Convert.FromBase64String(base64Source);
            using (MemoryStream inputStream = new MemoryStream(bytes))
            {
                using (StreamReader reader = new StreamReader(inputStream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string GetBase64String(string source)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(output, Encoding.UTF8))
                {
                    writer.Write(source);
                }
                return Convert.ToBase64String(output.ToArray());
            }
        }

        private static readonly Dictionary<string, string> _unicodeChars = new Dictionary<string, string>();
        private static readonly Regex _unicodeRegex = new Regex(@"\\u[0-9,a-f]{4}");

        public static string ReplaceUnicodeCharacters(string inS)
        {
            var sb = new StringBuilder(inS);
            var matches = _unicodeRegex.Matches(inS).OfType<Match>().Select(m => m.Value).Distinct().ToArray();
            foreach (var uniChar in matches)
            {
                if (!_unicodeChars.ContainsKey(uniChar))
                {
                    byte[] arr =
                    {
                        Convert.ToByte(uniChar.Substring(4,2), 16),
                        Convert.ToByte(uniChar.Substring(2,2), 16),
                    };
                    _unicodeChars[uniChar] = Encoding.Unicode.GetString(arr);
                }
                sb.Replace(uniChar, _unicodeChars[uniChar]);
            }
            return sb.ToString();
        }
    }
}
