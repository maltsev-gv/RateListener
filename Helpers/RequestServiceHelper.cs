using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using RateListener.ExtensionMethods;

namespace RateListener.Helpers
{
    public static class RequestServiceHelper
    {
        static RequestServiceHelper()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }

        public static async Task<string> FetchWebResponse(string baseUrl, string requestUrl, string jsonString, Dictionary<string, string> headers = null,
            ICredentials credentials = null)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await client.GetStringAsync(requestUrl);
        }

        public static string PutDataToWebMethod(string url, byte[] data, Dictionary<string, string> headers = null)
        {
            using (WebClient webClient = new WebClient())
            {
                headers?.ForEach(kvp =>
                {
                    webClient.Headers.Add(kvp.Key, kvp.Value);
                });
                var byteResult = webClient.UploadData(url, data);

                using (var responseStream = new MemoryStream(byteResult))
                {
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        var result = reader.ReadToEnd();

                        return result;
                    }
                }
            }
        }

        public static byte[] DownloadDataFromWebMethod(string url, out Dictionary<string, string> responseHeaders,
            Dictionary<string, string> headers = null)
        {
            using (WebClient webClient = new WebClient())
            {
                headers?.ForEach(kvp =>
                {
                    webClient.Headers.Add(kvp.Key, kvp.Value);
                });
                var byteResult = webClient.DownloadData(url);

                responseHeaders = new Dictionary<string, string>();
                for (int i = 0; i < webClient.ResponseHeaders.Count; i++)
                {
                    responseHeaders.Add(webClient.ResponseHeaders.GetKey(i), webClient.ResponseHeaders.Get(i));
                }

                return byteResult;
            }
        }

        public static async Task<ResponseInfo> Post(string url, object model, Dictionary<string, string> headers = null)
        {
            var response = new ResponseInfo();
            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(1) })
            {
                headers?.ForEach(pair => client.DefaultRequestHeaders.Add(pair.Key, pair.Value));
                HttpContent httpContent;

                httpContent = new StringContent(ZippedJsonHelper.GetSerializedString(model));
                httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var httpResponse = await client.PostAsync(url, httpContent).ConfigureAwait(false);
                response.StatusCode = (int)httpResponse.StatusCode;
                response.Headers = httpResponse.Content.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault());
                if (httpResponse.Content.Headers.ContentType?.MediaType == "application/octet-stream")
                {
                    response.BinaryContent =
                        await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
                else
                {
                    response.Content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }

            return response;
        }

        public static async Task<ResponseInfo> Get(string url, Dictionary<string, string> headers = null,
            Action<double> progressChangedAction = null, Func<bool> isNeedToAbortFunc = null)
        {
            var response = new ResponseInfo();
            var startTime = DateTime.Now;
            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(1) })
            {
                headers?.ForEach(pair => client.DefaultRequestHeaders.Add(pair.Key, pair.Value));

                var httpResponse = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.StatusCode = (int)httpResponse.StatusCode;
                response.Headers = httpResponse.Content.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault());
                if (httpResponse.Content.Headers.ContentType?.MediaType == "application/octet-stream")
                {
                    if (progressChangedAction == null)
                    {
                        response.BinaryContent =
                            await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        using (var stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            if (httpResponse.Content.Headers.ContentLength != null)
                            {
                                var length = (int)httpResponse.Content.Headers.ContentLength;
                                response.BinaryContent = new byte[length];
                                var chunkSize = 1024 * 1024;
                                var buffer = new byte[chunkSize];
                                var totalRead = 0;
                                while (true)
                                {
                                    if (isNeedToAbortFunc?.Invoke() == true)
                                    {
                                        response.BinaryContent = null;
                                        break;
                                    }
                                    var bytesRead = stream.Read(buffer, 0, chunkSize);
                                    if (bytesRead == 0)
                                    {
                                        break;
                                    }

                                    Array.Copy(buffer, 0, response.BinaryContent, totalRead, bytesRead);
                                    totalRead += bytesRead;
                                    Debug.WriteLine(
                                        $"{(DateTime.Now - startTime).TotalMilliseconds}: скачано {bytesRead}, всего {totalRead} ({(double)totalRead / length * 100:F1})");
                                    progressChangedAction.Invoke((double)totalRead / length);
                                }
                            }
                        }
                    }
                }
                else
                {
                    response.Content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }

            return response;
        }


        public class ResponseInfo
        {
            public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
            public int StatusCode { get; set; }
            public string Content { get; set; }
            public byte[] BinaryContent { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }
    }
}

