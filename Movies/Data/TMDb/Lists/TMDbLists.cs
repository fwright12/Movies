using Movies.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB : IListProvider
    {
        public Models.List CreateList() => new List(UserAccessToken, this);

        public async Task<List<Models.List>> GetAllLists()
        {
            var lists = new List<Models.List>();
            var request = new PagedTMDbRequest($"account/{AccountID}/lists")
            {
                Version = 4,
                Authorization = UserAuth
            };

            await foreach (var list in Request<Models.List>(request, TryParseList))
            {
                lists.Add(list);
            }

            return lists;
        }

        public async IAsyncEnumerable<Models.List> GetAllListsAsync()
        {
            List<Models.List> lists;

            try
            {
                lists = await LazyAllLists.Value;
            }
            catch
            {
                yield break;
            }

            foreach (var list in lists)
            {
                yield return list;
            }
        }

        private bool TryParseList(JsonNode json, out Models.List list) => List.TryParse(json, UserAccessToken, this, out list);

        public Task<Models.List> GetWatchlist() => Task.FromResult(GetNamedList("watchlist", "watchlist"));

        public Task<Models.List> GetFavorites() => Task.FromResult(GetNamedList("favorites", "favorite"));

        public Task<Models.List> GetHistory() => Task.FromResult<Models.List>(null);

        public async Task<Models.List> GetListAsync(string id)
        {
            //return new DummyList(id) { Name = "Tmdb list" };
            //var response = await WebClient.GetAsync(string.Format("https://api.themoviedb.org/4/list/{0}", 

            if ((await LazyAllLists.Value).FirstOrDefault(list => list.ID == id) is Models.List temp)
            {
                return temp;
            }

            var response = await WebClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, string.Format("4/list/{0}", id))
            {
                Headers =
                {
                    Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserAccessToken)
                }
            });

            return response?.IsSuccessStatusCode == true && List.TryParse(JsonNode.Parse(await response.Content.ReadAsStringAsync()), UserAccessToken, this, out var list) ? list : null;
        }

        private Models.List GetNamedList(string itemsPath, string statusPath) => new NamedList(itemsPath, statusPath, UserAccessToken, AccountID, SessionID, this);
    }

}
