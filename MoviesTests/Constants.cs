﻿namespace MoviesTests
{
    public static class Constants
    {
        public static readonly Movie Movie = new Movie("test").WithID(TMDB.IDKey, 0);
        public static readonly TVShow TVShow = new TVShow("test").WithID(TMDB.IDKey, 0);
        public static readonly TVSeason TVSeason = new TVSeason(TVShow, 1).WithID(TMDB.IDKey, 0);
        public static readonly TVEpisode TVEpisode = new TVEpisode(TVSeason, "test", 1).WithID(TMDB.IDKey, 0);
        public static readonly Person Person = new Person("test").WithID(TMDB.IDKey, 0);

        public const string TAGLINE = "lakjflkasjdflkajsdklj";
        public static readonly Language LANGUAGE = new Language("en");
        public static readonly TimeSpan INTERSTELLAR_RUNTIME = new TimeSpan(2, 49, 0);
        public static readonly TimeSpan HARRY_POTTER_RUNTIME = new TimeSpan(2, 10, 0);

        public const string APPEND_TO_RESPONSE = "append_to_response";
    }
}
