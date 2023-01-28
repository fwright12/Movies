using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Movies
{
    public class AppendedContent : HttpContent
    {
        private Task<LazyJson> Json { get; }
        private string PropertyName { get; }

        public AppendedContent(Task<LazyJson> json, string propertyName)
        {
            Json = json;
            PropertyName = propertyName;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var json = await Json;
            var result = false;
            var bytes = new ArraySegment<byte>();

            await json.Semaphore.WaitAsync();
            try
            {
                result = json.TryGetValue(PropertyName, out bytes);
            }
            finally
            {
                json.Semaphore.Release();
            }

            if (result)
            {
                await stream.WriteAsync(bytes);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
