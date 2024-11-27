using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;

namespace Movies
{
    public partial class TMDB : IAccount
    {
        public Models.Company Company { get; } = TMDb;
        public string Name => Company.Name;
        public string Username { get; private set; }

        private string UserAccessToken;
        private string AccountID;
        private string SessionID;
        private string RequestToken;

        public async Task<string> GetOAuthURL(Uri redirectUri)
        {
            var response = await WebClient.TrySendAsync("4/auth/request_token", JsonExtensions.JsonObject(JsonExtensions.FormatJson("redirect_to", redirectUri)), HttpMethod.Post);

            if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode json && json["success"]?.GetValue<bool>() == true)
            {
                RequestToken = json["request_token"]?.GetValue<string>();
                return string.Format("https://www.themoviedb.org/auth/access?request_token={0}", RequestToken);
            }

            return null;
        }

        public async Task<object> Login(object credentials)
        {
            JsonNode json = null;

            if (RequestToken != null)
            {
                var response = await WebClient.TryPostAsync("4/auth/access_token", JsonExtensions.JsonObject(JsonExtensions.FormatJson("request_token", RequestToken)));

                if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode parsed && parsed["success"]?.GetValue<bool>() == true)
                {
                    json = parsed;
                    RequestToken = null;
                }
            }
            else if (credentials != null)
            {
                try
                {
                    json = JsonNode.Parse(credentials.ToString());
                }
                catch (JsonException) { }
            }

            if (json == null)
            {
                return credentials;
            }

            UserAccessToken = json["access_token"]?.TryGetValue<string>();
            AccountID = json["account_id"]?.TryGetValue<string>();
            SessionID = json["session_id"]?.TryGetValue<string>();
            Username = json["username"]?.TryGetValue<string>();

            if (SessionID == null)
            {
                //var content = new StringContent(JsonObject(FormatJson("access_token", UserAccessToken)), Encoding.UTF8, "application/json");
                //var response = await WebClient.PostAsync(string.Format("https://api.themoviedb.org/3/authentication/session/convert/4"), content);
                var response = await WebClient.TryPostAsync(string.Format("3/authentication/session/convert/4"), JsonExtensions.JsonObject(JsonExtensions.FormatJson("access_token", UserAccessToken)));

                if (response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync()) is JsonNode parsed && parsed["success"]?.GetValue<bool>() == true)
                {
                    SessionID = parsed["session_id"]?.TryGetValue<string>();
                }
            }

            if (Username == null)
            {
                //var response = await WebClient.GetAsync(string.Format("https://api.themoviedb.org/3/account?session_id={0}", SessionID));
                var response = await WebClient.TryGetAsync(string.Format("3/account?session_id={0}", SessionID));

                if (response?.IsSuccessStatusCode == true)
                {
                    var parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    Username = parsed["username"]?.TryGetValue<string>();// ?? parsed["name"]?.GetValue<string>();
                    //AccountID = parsed["id"]?.TryGetValue<int>().ToString();
                }
            }

            UserAuth = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserAccessToken);
            return JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("access_token", UserAccessToken),
                JsonExtensions.FormatJson("account_id", AccountID),
                JsonExtensions.FormatJson("session_id", SessionID),
                JsonExtensions.FormatJson("username", Username));
        }

        public async Task<bool> Logout()
        {
            /*HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonObject(FormatJson("access_token", UserAccessToken)), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("https://api.themoviedb.org/4/auth/access_token"),
            };*/

            UserAccessToken = Username = AccountID = SessionID = null;

            //var response = await WebClient.SendAsync(request);
            var response = await WebClient.TrySendAsync("4/auth/access_token", JsonExtensions.JsonObject(JsonExtensions.FormatJson("access_token", UserAccessToken)), HttpMethod.Delete);

            return response?.IsSuccessStatusCode == true && JsonNode.Parse(await response.Content.ReadAsStringAsync())["success"]?.GetValue<bool>() == true;
        }
    }
}
