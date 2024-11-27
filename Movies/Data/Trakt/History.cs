using Movies.Models;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public partial class Trakt
    {
        public class History : BaseNamedList
        {
            protected override string ItemsEndpoint => string.Format("{0}/movies", ID);
            protected override string ReorderEndpoint => null;

            public History(Trakt trakt) : base(trakt, "history", 20)
            {
                AllowedTypes = ItemType.Movie | ItemType.TVEpisode;
            }

            protected override async Task<bool?> ContainsAsyncInternal(Item item)
            {
                if (await Trakt.TryGetId(item) is int id)
                {
                    string type;
                    if (item is Movie)
                    {
                        type = "movies";
                    }
                    else if (item is TVShow)
                    {
                        type = "shows";
                    }
                    else if (item is TVSeason)
                    {
                        type = "seasons";
                    }
                    else if (item is TVEpisode)
                    {
                        type = "episodes";
                    }
                    else
                    {
                        return false;
                    }

                    var response = await Client.TryGetAsync(string.Format("history/{0}/{1}", type, id));

                    if (response?.IsSuccessStatusCode == true)
                    {
                        return JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsArray().Count > 0;
                    }
                }

                return null;
            }

            public override async Task<int> CountAsync()
            {
                var response = await Client.TrySendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.trakt.tv/users/me/stats"));

                if (response?.IsSuccessStatusCode == true)
                {
                    var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    int total = 0;

                    foreach (var kvp in json.AsObject())
                    {
                        if ((kvp.Key.ToLower() == "movies" || kvp.Key.ToLower() == "shows") && kvp.Value?.TryGetValue("watched", out int count) == true)
                        {
                            total += count;
                        }
                    }

                    return total;
                }

                return 0;
            }
        }
    }
}
