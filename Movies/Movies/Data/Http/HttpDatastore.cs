using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class HttpDatastore : IAsyncDataStore<Uri, State>
    {
        public HttpMessageInvoker Invoker { get; }

        public HttpDatastore(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        public async Task<bool> CreateAsync(Uri key, State value) => await SendAsync(HttpMethod.Post, key, value) != null;

        public Task<State> DeleteAsync(Uri key) => SendAsync(HttpMethod.Delete, key);

        public Task<State> ReadAsync(Uri key) => SendAsync(HttpMethod.Get, key);

        public async Task<bool> UpdateAsync(Uri key, State updatedValue) => await SendAsync(HttpMethod.Put, key, updatedValue) != null;

        public virtual bool TryGetConverter(Uri uri, out IHttpConverter<object> resource)
        {
            resource = default;
            return false;
        }

        private async Task<State> SendAsync(HttpMethod method, Uri uri, State body = null)
        {
            var message = new HttpRequestMessage(method, uri);
            if (body != null && TryGetHttpContent(body, out var content))
            {
                message.Content = content;
            }

            var response = await Invoker.SendAsync(message, default);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var state = State.Create(await response.Content.ReadAsByteArrayAsync());

            if (TryGetConverter(uri, out var converter))
            {
                var converted = await converter.Convert(response.Content);
                //var type = converter is HttpResourceCollectionConverter ? typeof(IEnumerable<KeyValuePair<Uri, object>>) : converted.GetType();
                state.Add(converted.GetType(), converted);
            }

            return state;
        }

        public static bool TryGetHttpContent(State state, out HttpContent content)
        {
            if (state.TryGetValue<byte[]>(out var bytes))
            {
                content = new ByteArrayContent(bytes);
            }
            else if (state.TryGetValue<string>(out var str))
            {
                content = new StringContent(str);
            }
            else
            {
                content = default;
                return false;
            }

            return true;
        }
    }
}