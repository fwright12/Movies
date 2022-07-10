using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Movies
{
    public class TMDbRequest
    {
        public static string DEFAULT_LANGUAGE { get; set; } = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        public static string DEFAULT_REGION { get; set; } = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
        public static bool ADULT { get; set; } = false;

        public static string DEFAULT_ISO_639_1 => DEFAULT_LANGUAGE;
        public static string DEFAULT_ISO_3166_1 => DEFAULT_REGION;

        public HttpMethod Method { get; set; }
        public string Endpoint { get; set; }
        public int Version { get; set; } = 3;

        public AuthenticationHeaderValue Authorization { get; set; }
        public bool HasLanguageParameter { get; set; }
        public bool HasRegionParameter { get; set; }
        public bool HasAdultParameter { get; set; }
        public bool SupportsAppendToResponse { get; set; }

        public TMDbRequest() { }
        public TMDbRequest(string endpoint) : this(endpoint, HttpMethod.Get) { }
        public TMDbRequest(string endpoint, HttpMethod method)
        {
            Method = method;
            Endpoint = endpoint;
        }

        public static implicit operator TMDbRequest(string url) => new TMDbRequest(url);


        public string GetURL(string language = null, string region = null, bool adult = false, params string[] otherParameters)
        {
            var parameters = new List<string>();

            if (HasLanguageParameter) parameters.Add($"language={language ?? DEFAULT_LANGUAGE}");
            if (HasRegionParameter) parameters.Add($"region={region ?? DEFAULT_REGION}");
            if (HasAdultParameter) parameters.Add($"adult={adult || ADULT}");

            parameters.AddRange(otherParameters);

            return TMDB.BuildApiCall($"{Version}/" + Endpoint, parameters);
        }

        public HttpRequestMessage GetMessage()
        {
            var message = new HttpRequestMessage(Method, GetURL(null, null, false))
            {
                Headers =
                {
                    Authorization = Authorization
                }
            };

            return message;
        }
    }

    public class PagedTMDbRequest : TMDbRequest, IPagedRequest
    {
        public PagedTMDbRequest(string url) : base(url) { }

        public static implicit operator PagedTMDbRequest(string url) => new PagedTMDbRequest(url);

        public string GetURL(int page, params string[] parameters) => GetURL(null, null, false, parameters.Prepend($"page={page}").ToArray());

        public Task<int?> GetTotalPages(HttpResponseMessage response) => Helpers.GetTotalPages(response, new JsonPropertyParser<int>("total_pages"));
    }
}