using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class HttpProcessor : IAsyncEventProcessor<ResourceReadArgs<Uri>>
    {
        public HttpMessageInvoker Invoker { get; }

        public HttpProcessor(HttpMessageInvoker invoker)
        {
            Invoker = invoker;
        }

        public async Task<bool> ProcessAsync(ResourceReadArgs<Uri> e)
        {
            var response = await Invoker.SendAsync(ToMessage(e), default);
            return e.Handle(new HttpResponse(response));
        }

        public static HttpRequestMessage ToMessage(ResourceReadArgs<Uri> args)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, args.Key);

            if (args is RestRequestArgs restArgs)
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
