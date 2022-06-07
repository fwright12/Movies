using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public class ApiCall<T> : ApiCall
    {
        private IJsonParser<T> Parser;

        public ApiCall(string endpoint, IJsonParser<T> parser) : base(endpoint)
        {
            Parser = parser;
        }

        public async Task<T> GetValue(Task<HttpResponseMessage> call)
        {
            var response = await call;

            if (response?.IsSuccessStatusCode == true)
            {
                var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                
                if (Parser.TryGetValue(json, out var value))
                {
                    return value;
                }
            }

            return default;
        }
    }

    public class ApiCall
    {
        public string Endpoint { get; }
        public List<Parser> Parsers { get; }

        public ApiCall(string endpoint)
        {
            Endpoint = endpoint;
        }

        public ApiCall(string endpoint, IList<Parser> parsers) : this(endpoint)
        {
            Parsers = new List<Parser>(parsers);
        }
    }
}