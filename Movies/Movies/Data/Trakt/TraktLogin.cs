using Movies.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Movies
{
    public partial class Trakt : IAccount
    {
        public string Username { get; private set; }
        public string Name => Company.Name;
        public Company Company { get; } = new Company
        {
            Name = "Trakt"
        };
        public Uri RedirectUri { get; set; }

        private string ClientID;
        private string ClientSecret;
        private string UserAccessToken;
        private string UserSlug;

        public Task<string> GetOAuthURL(Uri redirectUri) => Task.FromResult(string.Format("https://trakt.tv/oauth/authorize?response_type=code&client_id={0}&redirect_uri={1}", ClientID, RedirectUri = redirectUri));

        public async Task<object> Login(object credentials)
        {
            JsonObject json = null;

            if (credentials != null)
            {
                string tokenProperty = "code";
                string grantType = "authorization_code";

                try
                {
                    json = JsonNode.Parse(credentials.ToString()).AsObject();

                    if (json.TryGetValue("refresh_token", out string refreshToken) && json.TryGetValue("expires_in", out int expiresIn) && json.TryGetValue("created_at", out long createdAt) && DateTimeOffset.FromUnixTimeSeconds(createdAt).UtcDateTime + TimeSpan.FromSeconds(expiresIn) < DateTime.UtcNow)
                    {
                        credentials = "code=" + refreshToken;
                        tokenProperty = "refresh_token";
                        grantType = "refresh_token";
#if !DEBUG
                        throw new JsonException("Access token expired");
#endif
                    }
                }
                catch (JsonException)
                {
                    if (credentials is Uri uri)
                    {
                        credentials = uri.Query;
                    }

                    var parameters = credentials.ToString().TrimStart('?').Split('&');
                    var code = parameters.Select(parameter => parameter.Split('=')).FirstOrDefault(parts => parts.FirstOrDefault() == "code")?.LastOrDefault();

                    if (code != null)
                    {
                        var response = await Client.TryPostAsync("oauth/token", JsonExtensions.JsonObject(
                            JsonExtensions.FormatJson(tokenProperty, code),
                            JsonExtensions.FormatJson("client_id", ClientID),
                            JsonExtensions.FormatJson("client_secret", ClientSecret),
                            JsonExtensions.FormatJson("redirect_uri", RedirectUri),
                            JsonExtensions.FormatJson("grant_type", grantType)));

                        if (response?.IsSuccessStatusCode == true)
                        {
                            json = JsonNode.Parse(await response.Content.ReadAsStringAsync()).AsObject();
                        }
                    }
                }
            }

            if (json == null)
            {
                return credentials;
            }

            UserAccessToken = json["access_token"]?.TryGetValue<string>();
            Username = json["username"]?.TryGetValue<string>();
            UserSlug = json["user_slug"]?.TryGetValue<string>();

            if (Username == null || UserSlug == null)
            {
                var response = await Client.TrySendAsync(AuthedRequest(HttpMethod.Get, "users/settings"));

                if (response?.IsSuccessStatusCode == true)
                {
                    var user = JsonNode.Parse(await response.Content.ReadAsStringAsync())["user"];

                    if (user?.TryGetValue("username", out string username) == true)
                    {
                        Username = username;
                    }
                    if (user?["ids"]?.TryGetValue("slug", out string userSlug) == true)
                    {
                        UserSlug = userSlug;
                    }
                }
            }

            if (!json.ContainsKey("username"))
            {
                json.Add("username", Username);
            }
            if (!json.ContainsKey("user_slug"))
            {
                json.Add("user_slug", Username);
            }

            return json.ToJsonString();
        }

        public async Task<bool> Logout()
        {
            var response = await Client.TryPostAsync("oauth/revoke", JsonExtensions.JsonObject(
                JsonExtensions.FormatJson("token", UserAccessToken),
                JsonExtensions.FormatJson("client_id", ClientID),
                JsonExtensions.FormatJson("client_secret", ClientSecret)));

            if (response?.IsSuccessStatusCode == true)
            {
                UserAccessToken = Username = UserSlug = null;
                return true;
            }

            return false;
        }
    }
}
