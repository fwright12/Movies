using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public static class JsonExtensions
    {
        public static Task<JsonResponse> TryGetCachedAsync(this HttpClient client, string url, IJsonCache cache) => TryGetCachedAsync(client, new HttpRequestMessage(HttpMethod.Get, url), cache);
        public static async Task<JsonResponse> TryGetCachedAsync(this HttpClient client, HttpRequestMessage request, IJsonCache cache)
        {
            var url = request.RequestUri.ToString();
            var cached = cache.TryGetValueAsync(url);

            if (cached != null)
            {
                return await cached;
            }

            var content = await TryGetContentAsync(client, request);
            var response = new JsonResponse(content);

            if (content != null)
            {
                await cache.AddAsync(url, response);
            }

            return response;
        }

        public static Task<string> TryGetContentAsync(this HttpClient client, string url) => TryGetContentAsync(client, new HttpRequestMessage(HttpMethod.Get, url));
        public static async Task<string> TryGetContentAsync(this HttpClient client, HttpRequestMessage request)
        {
            var response = await TrySendAsync(client, request);

            if (response?.IsSuccessStatusCode == true)
            {
                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }

        public static Task<HttpResponseMessage> TryGetAsync(this HttpClient client, string url) => TrySendAsync(client, url);
        public static Task<HttpResponseMessage> TryPostAsync(this HttpClient client, string url, string content) => TrySendAsync(client, url, content, HttpMethod.Post);

        public static Task<HttpResponseMessage> TrySendAsync(this HttpClient client, string url, string content, HttpMethod method = null) => TrySendAsync(client, url, new StringContent(content, Encoding.UTF8, "application/json"), method);

        public static Task<HttpResponseMessage> TrySendAsync(this HttpClient client, string url, HttpContent content = null, HttpMethod method = null)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url, UriKind.RelativeOrAbsolute)
            };
            if (method != null)
            {
                request.Method = method;
            }
            if (content != null)
            {
                request.Content = content;
            }

            return TrySendAsync(client, request);
        }

        public static async Task<HttpResponseMessage> TrySendAsync(this HttpClient client, HttpRequestMessage request)
        {
            try
            {
                return await client.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public static T TryGetValue<T>(this JsonNode node) => TryGetValue<T>(node, out var value) ? value : default;
        public static bool TryGetValue<T>(this JsonNode node, string property, out T value)
        {
            if ((node as JsonObject)?.TryGetPropertyValue(property, out var temp) == true)
            {
                return TryGetValue(temp, out value);
            }

            value = default;
            return false;
        }
        public static bool TryGetValue<T>(this JsonNode node, out T value)
        {
            if (node is T t)
            {
                value = t;
                return true;
            }

            try
            {
                if (node == null)
                {
                    value = default;
                    return value == null;
                }
                else
                {
                    value = node.GetValue<T>();
                    return true;
                }
            }
            catch { }

            value = default;
            return false;
        }

        public static bool TryGetValue<T>(this JsonElement json, out T value, string path = "", JsonSerializerOptions options = null)
        {
            value = default;

            if (path != string.Empty && json.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in path.Split('.'))
            {
                if (!string.IsNullOrEmpty(property) && !json.TryGetProperty(property, out json))
                {
                    return false;
                }
            }

            if (json is T t)
            {
                value = t;
                return true;
            }

            try
            {
                value = json.Deserialize<T>(options);
                return true;
            }
            catch { }

            return false;
        }

        public static string JsonObject(params string[] items) => "{" + string.Join(", ", items) + "}";

        public static string FormatJson<T>(string name, T value) => string.Format("\"{0}\": {1}", name, JsonSerializer.Serialize(value));
    }
}
