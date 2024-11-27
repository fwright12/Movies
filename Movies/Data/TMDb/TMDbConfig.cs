using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Movies
{
    public partial class TMDB
    {
#if DEBUG
        public static readonly string BaseAddress = DebugConfig.AllowTMDbRequests ? "https://api.themoviedb.org/" : "https://mock.themoviedb/";
#else
        public static readonly string BaseAddress = "https://api.themoviedb.org/";
#endif

#if DEBUG
        private static readonly string SECURE_BASE_IMAGE_URL = DebugConfig.AllowTMDbImages ? "https://image.tmdb.org/t/p" : string.Empty;
#else
        private static readonly string SECURE_BASE_IMAGE_URL = "https://image.tmdb.org/t/p";
#endif

        private static readonly string POSTER_SIZE = "/w342";
        private static readonly string PROFILE_SIZE = "/original";
        private static readonly string STILL_SIZE = "/original";
        private static readonly string STREAMING_LOGO_SIZE = "/w45";
        private static readonly string LOGO_SIZE = "/w300";
    }
}
