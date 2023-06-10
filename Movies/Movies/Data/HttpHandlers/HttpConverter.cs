using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Movies
{
    public interface IHttpConverter<T>
    {
        Task<T> Convert(HttpContent content);
    }

    public abstract class HttpResourceCollectionConverter : IHttpConverter<IReadOnlyDictionary<Uri, object>>, IHttpConverter<object>
    {
        public abstract IEnumerable<Uri> Resources { get; }

        public abstract Task<IReadOnlyDictionary<Uri, object>> Convert(HttpContent content);

        async Task<object> IHttpConverter<object>.Convert(HttpContent content) => await Convert(content);
    }

    public class HttpJsonCollectionConverter<TDom> : HttpResourceCollectionConverter
    {
        public JsonCollectionConverter<TDom> Converter { get; }
        public JsonSerializerOptions Options { get; }

        public override IEnumerable<Uri> Resources => Converter.Parsers.Keys;

        public HttpJsonCollectionConverter(JsonCollectionConverter<TDom> converter, JsonSerializerOptions options = null)
        {
            Converter = converter;
            Options = options ?? new JsonSerializerOptions();
        }

        public override async Task<IReadOnlyDictionary<Uri, object>> Convert(HttpContent content) => Converter.Read(await content.ReadAsByteArrayAsync(), typeof(IReadOnlyDictionary<Uri, object>), Options);
    }
}
