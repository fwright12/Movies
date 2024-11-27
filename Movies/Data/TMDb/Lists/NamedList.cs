using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB
    {
        private class NamedList : BaseList
        {
            public string ItemsPath { get; }
            public string StatusPath { get; }

            private string AccountID;
            private string SessionID;
            private Models.Collection Movies;
            private Models.Collection TV;

            public NamedList(string itemsPath, string statusPath, string bearer, string accountID, string sessionId, TMDB idSystem) : base(itemsPath, idSystem, bearer)
            {
                AccountID = accountID;
                SessionID = sessionId;

                ItemsPath = itemsPath;
                StatusPath = statusPath;

                Reverse = true;
                var sort = $"sort_by={(Reverse ? "created_at.desc" : "created_at.asc")}";

                Movies = new Models.Collection
                {
                    Reverse = true,
                    Items = Request<Models.Movie>(new ParameterizedPagedRequest(API.V4.ACCOUNT.GET_MOVIES, AccountID, ID), TryParseMovie, sort)
                };
                TV = new Models.Collection
                {
                    Reverse = true,
                    Items = Request<Models.TVShow>(new ParameterizedPagedRequest(API.V4.ACCOUNT.GET_TV_SHOWS, AccountID, ID), TryParseTVShow, sort)
                };
            }

            private async Task<bool> UpdateStatus(bool status, IEnumerable<Models.Item> items)
            {
                foreach (var item in items)
                {
                    if (TryGetMediaType(item, out var type) && TryGetId(item, out var id))
                    {
                        var json = JsonExtensions.JsonObject(
                            JsonExtensions.FormatJson("media_type", type),
                            JsonExtensions.FormatJson("media_id", id),
                            JsonExtensions.FormatJson(StatusPath, status));
                        //var content = new StringContent(json, Encoding.UTF8, "application/json");
                        await Client.TryPostAsync(string.Format(API.V3.ACCOUNT.ADD_TO_LIST.GetURL(), AccountID, StatusPath, SessionID), json);
                        //await Client.PostAsync(string.Format("https://api.themoviedb.org/3/account/{0}/" + StatusPath + "?session_id={1}", AccountID, SessionID), content);
                    }
                }

                return true;
            }

            protected override Task<bool> AddAsyncInternal(IEnumerable<Models.Item> items) => UpdateStatus(true, items);

            protected override async Task<bool?> ContainsAsyncInternal(Models.Item item)
            {
                if (TryGetMediaType(item, out var type) && TryGetId(item, out var id))
                {
                    var response = await Client.TryGetAsync(string.Format("3/" + type + "/{0}/account_states?session_id={1}", id, SessionID));
                    //var response = await Client.GetAsync(string.Format("https://api.themoviedb.org/3/" + type + "/{0}/account_states?session_id={1}", id, SessionID));

                    if (response?.IsSuccessStatusCode == true)
                    {
                        return JsonNode.Parse(await response.Content.ReadAsStringAsync())[StatusPath]?.GetValue<bool>() == true;
                    }
                }

                return null;
            }

            public override async Task<int> CountAsync()
            {
                var responses = await Task.WhenAll(Client.TryGetAsync(string.Format(API.V4.ACCOUNT.GET_MOVIES.GetURL(), AccountID, ID)), Client.TryGetAsync(string.Format(API.V4.ACCOUNT.GET_TV_SHOWS.GetURL(), AccountID, ID)));
                //var responses = await Task.WhenAll(Client.GetAsync(string.Format("https://api.themoviedb.org/4/account/{0}/movie/" + ID.ToString() + "?page=1", AccountID)), Client.GetAsync(string.Format("https://api.themoviedb.org/4/account/{0}/tv/" + ID.ToString() + "?page=1", AccountID)));

                if (responses[0]?.IsSuccessStatusCode == true && responses[1]?.IsSuccessStatusCode == true)
                {
                    var parsed = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
                    return parsed.Sum(json => JsonNode.Parse(json).TryGetValue<int>("total_results", out var count) ? count : 0);
                }

                return -1;
                //return JsonNode.Parse(await response.Content.ReadAsStringAsync())["total_results"]?.GetValue<int>() ?? -1;
            }

            public override Task Delete() => Task.CompletedTask;

            protected override Task<bool> RemoveAsyncInternal(IEnumerable<Models.Item> items) => UpdateStatus(false, items);

            public override Task Update() => Task.CompletedTask;

            public override async Task<List<Models.Item>> GetAdditionsAsync(Models.List list)
            {
                var added = new List<Models.Item>();

                if (Reverse)
                {
                    try
                    {
                        await foreach (var item in Movies)
                        {
                            if (await list.ContainsAsync(item) != true)
                            {
                                added.Insert(Reverse ? 0 : added.Count, item);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        added.Clear();
                    }
                }

                return added;
            }

            /*public override async Task<List<Models.Item>> GetAdditionsAsync(Models.List list, bool onlyFromFront = true)
            {
                var items = await GetAdditionsAsync(list);

                if (!(items.FirstOrDefault() is Models.Movie))
                {
                    items.AddRange(await Movies.GetAdditionsAsync(list));
                }

                return items.OfType<Models.TVShow>().Concat<Models.Item>(items.OfType<Models.Movie>()).ToList();
            }*/

            protected override async IAsyncEnumerable<Models.Item> GetItems()
            {
                await foreach (var tv in TV)
                {
                    yield return tv;
                }
                await foreach (var movie in Movies)
                {
                    yield return movie;
                }
            }

            public override IAsyncEnumerable<Models.Item> TryFilter(FilterPredicate filter, out FilterPredicate partial, CancellationToken cancellationToken = default)
            {
                var types = Models.ItemHelpers.RemoveTypes(filter, out partial);
                var movie = types.Contains(typeof(Models.Movie));
                var tv = types.Contains(typeof(Models.TVShow));

                if (movie ^ tv)
                {
                    var items = tv ? TV : Movies;
                    return items;
                    //return ItemHelpers.Filter(items, filter, cancellationToken);
                    //return items.WhereAsync(item => Models.ItemHelpers.Evaluate(item, filter, cancellationToken));
                }
                else
                {
                    return base.TryFilter(filter, out partial, cancellationToken);
                }
            }
        }
    }
}
