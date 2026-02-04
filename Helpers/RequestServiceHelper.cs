using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using RateListener.ExtensionMethods;

namespace RateListener.Helpers;

// ReSharper disable once AccessToDisposedClosure
public static class RequestServiceHelper
{
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
        using var client = new HttpClient();
        headers?.ForEach(kvp => client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value));
        using var content = new ByteArrayContent(data);
        var response = client.PostAsync(url, content).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public static byte[] DownloadDataFromWebMethod(string url, out Dictionary<string, string> responseHeaders,
        Dictionary<string, string> headers = null)
    {
        using var client = new HttpClient();
        headers?.ForEach(kvp => client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value));
        var httpResponse = client.GetAsync(url).GetAwaiter().GetResult();
        httpResponse.EnsureSuccessStatusCode();
        responseHeaders = httpResponse.Headers.Concat(httpResponse.Content.Headers)
            .GroupBy(h => h.Key)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.SelectMany(hh => hh.Value)));
        return httpResponse.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }

    public static async Task<ResponseInfo> Post(string url, object model, Dictionary<string, string> headers = null)
    {
        var response = new ResponseInfo();
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(1);
        headers?.ForEach(pair => client.DefaultRequestHeaders.Add(pair.Key, pair.Value));

        HttpContent httpContent = new StringContent(JsonHelper.GetSerializedString(model));
        httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        var httpResponse = await client.PostAsync(url, httpContent).ConfigureAwait(false);
        response.StatusCode = (int)httpResponse.StatusCode;
        response.Headers = httpResponse.Content.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault());
        if (httpResponse.Content.Headers.ContentType?.MediaType == "application/octet-stream")
            response.BinaryContent =
                await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        else
            response.Content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        return response;
    }

    public static async Task<ResponseInfo> Get(string url, Dictionary<string, string> headers = null,
        Action<double> progressChangedAction = null, Func<bool> isNeedToAbortFunc = null)
    {
        var response = new ResponseInfo();
        var startTime = DateTime.Now;
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(1);
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
                await using var stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                if (httpResponse.Content.Headers.ContentLength != null)
                {
                    var length = (int)httpResponse.Content.Headers.ContentLength;
                    response.BinaryContent = new byte[length];
                    const int chunkSize = 1024 * 1024;
                    var buffer = new byte[chunkSize];
                    var totalRead = 0;
                    while (true)
                    {
                        if (isNeedToAbortFunc?.Invoke() == true)
                        {
                            response.BinaryContent = null;
                            break;
                        }
                        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize));
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
        else
        {
            response.Content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        return response;
    }


    public class ResponseInfo
    {
        public bool IsSuccess => StatusCode is >= 200 and < 300;
        public int StatusCode { get; set; }
        public string Content { get; set; }
        public byte[] BinaryContent { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}