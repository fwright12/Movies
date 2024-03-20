using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class HttpProcessor : IAsyncEventProcessor<ResourceRequestArgs<Uri>>
    {
        public HttpMessageInvoker Invoker { get; }

        public HttpProcessor(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        public async Task<bool> ProcessAsync(ResourceRequestArgs<Uri> e)
        {
            var response = await Invoker.SendAsync(ToMessage(e.Request), default);
            return response.IsSuccessStatusCode ? e.Handle(new HttpResponse(response)) : false;
        }

        public static HttpRequestMessage ToMessage(KeyValueReadEventArgs<Uri> args)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, args.Key);

            if (args is RestRequestEventArgs restArgs)
            {
                foreach (var kvp in restArgs.ControlData)
                {
                    message.Headers.Add(kvp.Key, kvp.Value);
                }

                if (restArgs.Representation != null)
                {
                    message.Content = new StringContent(restArgs.Representation.Value);
                }
            }

            return message;
        }
    }
}
