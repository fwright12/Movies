﻿using System;
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
        //public static string DEFAULT_LANGUAGE = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        //public static string DEFAULT_REGION = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;

        //public static string DEFAULT_ISO_639_1 => DEFAULT_LANGUAGE;
        //public static string DEFAULT_ISO_3166_1 => DEFAULT_REGION;

        public const string LANGUAGE_PARAMETER_KEY = "language";
        public const string REGION_PARAMETER_KEY = "region";
        public const string ADULT_PARAMETER_KEY = "adult";

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

        public string GetURL(params string[] parameters) => GetURL(TMDB.LANGUAGE.Iso_639, TMDB.REGION.Iso_3166, TMDB.ADULT, parameters);
        //public string GetURL(params string[] parameters) => GetURL(null, null, null, parameters);
        public string GetURL(string language, string region, bool? adult, params string[] otherParameters)
        {
            var parameters = new List<string>();

            if (HasLanguageParameter && !string.IsNullOrEmpty(language)) parameters.Add($"{LANGUAGE_PARAMETER_KEY}={language}");
            if (HasRegionParameter && !string.IsNullOrEmpty(region)) parameters.Add($"{REGION_PARAMETER_KEY}={region}");
            //if (HasAdultParameter && adult.HasValue) parameters.Add($"{ADULT_PARAMETER_KEY}={adult}");

            parameters.AddRange(otherParameters.Where(p => !string.IsNullOrEmpty(p)));

            return TMDB.BuildApiCall((Version >= 0 ? $"{Version}/" : string.Empty) + Endpoint, parameters);
        }

        public HttpRequestMessage GetRequest(params string[] parameters) => new HttpRequestMessage(Method, GetURL(parameters))
        {
            Headers =
            {
                Authorization = Authorization
            }
        };
    }

    public class PagedTMDbRequest : TMDbRequest, IPagedRequest
    {
        public PagedTMDbRequest(string url) : base(url) { }

        public static implicit operator PagedTMDbRequest(string url) => new PagedTMDbRequest(url);

        public HttpRequestMessage GetRequest(int page, params string[] parameters) => GetRequest(parameters.Prepend($"page={page}").ToArray());

        public Task<int?> GetTotalPages(HttpResponseMessage response) => Helpers.GetTotalPages(response, new JsonPropertyParser<int>("total_pages"));
    }
}