﻿using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class JsonResponse
    {
        public HttpContent Content { get; }
        public DateTime Timestamp { get; }
        public DateTimeOffset? ExpiresAt { get; }
        public string ETag { get; }

        public JsonResponse(string json) : this(json, DateTime.Now) { }
        public JsonResponse(byte[] bytes) : this(bytes, DateTime.Now) { }
        public JsonResponse(HttpContent content) : this(content, DateTime.Now) { }
        public JsonResponse(HttpContent content, string etag, DateTimeOffset? expiresAt) : this(content, DateTime.Now)
        {
            ETag = etag;
            ExpiresAt = expiresAt;
        }

        public JsonResponse(string json, DateTime timeStamp) : this(new StringContent(json), timeStamp) { }
        public JsonResponse(byte[] bytes, DateTime timeStamp) : this(new ByteArrayContent(bytes), timeStamp) { }
        public JsonResponse(byte[] bytes, DateTime timeStamp, string etag, DateTimeOffset? expiresAt) : this(new ByteArrayContent(bytes), timeStamp)
        {
            ETag = etag;
            ExpiresAt = expiresAt;
        }

        public JsonResponse(HttpContent content, DateTime timeStamp)
        {
            Content = content;
            Timestamp = timeStamp;
        }

        public static async Task<JsonResponse> Create(Func<Task<HttpResponseMessage>> request)
        {
            var response = await request();

            if (response?.IsSuccessStatusCode == true)
            {
                return new JsonResponse(await response.Content.ReadAsStringAsync());
            }

            return null;
        }
    }
}
