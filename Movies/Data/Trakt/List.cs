using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public partial class Trakt
    {
        public class List : BaseList
        {
            protected override string ItemsEndpoint => string.Format("{0}/items", ID);
            protected override string AddEndpoint => string.Format("{0}/items", ID);
            protected override string RemoveEndpoint => string.Format("{0}/items/remove", ID);
            protected override string ReorderEndpoint => string.Format("{0}/items/reorder", ID);

            public List(Trakt trakt) : this(trakt, null) { }

            private List(Trakt trakt, object id) : base(trakt, id)
            {
                AllowedTypes = ItemType.AllMedia | ItemType.Person;
                Client.BaseAddress = new Uri("https://api.trakt.tv/users/me/lists/");
            }

            private static bool TryParseListID(JsonNode json, out int id)
            {
                if (json["ids"]?.TryGetValue("trakt", out id) == true)
                {
                    return true;
                }

                id = -1;
                return false;
            }

            public static bool TryParse(JsonNode json, Trakt trakt, out Models.List list)
            {
                list = null;

                if (!TryParseListID(json, out int id))
                {
                    return false;
                }

                var localList = new List(trakt, id)
                {
                    Name = json["name"]?.TryGetValue<string>(),
                    Description = json["description"]?.TryGetValue<string>(),
                    Count = json["item_count"]?.TryGetValue<int?>(),
                    Public = json["privacy"]?.TryGetValue<string>()?.ToLower() == "public"
                };

                if (DateTime.TryParse(json["updated_at"]?.TryGetValue<string>(), out DateTime date))
                {
                    localList.LastUpdated = date.ToUniversalTime();
                }

                localList.DetailsBackup = localList.DetailsAsJson();

                list = localList;
                return true;
            }

            public override Task Delete() => Client.TrySendAsync(ID, method: HttpMethod.Delete);

            private string DetailsBackup;
            private string DetailsAsJson() => JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("name", CleanString(Name)),
                JsonExtensions.FormatJson("description", CleanString(Description)),
                JsonExtensions.FormatJson("privacy", Public ? "public" : "private"));

            private string CleanString(string value) => (value ?? string.Empty).Trim();

            public override async Task Update()
            {
                string json = DetailsAsJson();

                if (ID == null)
                {
                    var response = await Client.TrySendAsync(new HttpRequestMessage(HttpMethod.Post, Client.BaseAddress.ToString())
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    });

                    if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode parsed && TryParseListID(parsed, out int id))
                    {
                        _ID = id;
                    }
                    else
                    {
                        return;
                    }
                }
                else if (json != DetailsBackup)
                {
                    var response = await Client.TrySendAsync(ID, json, HttpMethod.Put);

                    if (response?.IsSuccessStatusCode != true)
                    {
                        return;
                    }
                }

                DetailsBackup = json;
            }
        }
    }
}
