namespace MoviesTests
{
    public static class Constants
    {
        public static readonly Movie Movie = new Movie("test").WithID(TMDB.IDKey, 0);
        public static readonly TVShow TVShow = new TVShow("test").WithID(TMDB.IDKey, 0);
        public static readonly TVSeason TVSeason = new TVSeason(TVShow, 3).WithID(TMDB.IDKey, 0);
        public static readonly TVEpisode TVEpisode = new TVEpisode(TVSeason, "test", 1).WithID(TMDB.IDKey, 0);
        public static readonly Person Person = new Person("test").WithID(TMDB.IDKey, 0);

        public const string TAGLINE = "lakjflkasjdflkajsdklj";
        public static readonly Language US_ENGLISH_LANGUAGE = new Language("en-US");
        public static readonly Movies.Region US_REGION = new Movies.Region("US");
        public static readonly TimeSpan INTERSTELLAR_RUNTIME = new TimeSpan(2, 49, 0);
        public static readonly TimeSpan HARRY_POTTER_RUNTIME = new TimeSpan(2, 10, 0);

        public const string APPEND_TO_RESPONSE = "append_to_response";
    }
}
