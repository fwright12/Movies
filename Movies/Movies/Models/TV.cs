using System.Collections.Generic;
using System;

namespace Movies.Models
{
    public class TVShow : Collection
    {
        public string Title => Name;

        public TVShow(string title)
        {
            Name = title;
        }

        public static readonly Property<DateTime> FIRST_AIR_DATE = new Property<DateTime>("First Air Date");
        public static readonly Property<DateTime> LAST_AIR_DATE = new Property<DateTime>("Last Air Date");
        public static readonly MultiProperty<Company> NETWORKS = new MultiProperty<Company>("Networks");
        public static readonly Property<string> CONTENT_RATING = new Property<string>("Content Rating", new List<string> { "TV", "TV-14", "TV-MA" });
        public static readonly MultiProperty<string> GENRES = new MultiProperty<string>("Genres", new List<string> { "Action", "Adventure", "Romance", "Comedy", "Thriller", "Mystery", "Sci-Fi", "Horror", "Mockumentary" });
        public static readonly MultiProperty<WatchProvider> WATCH_PROVIDERS = new MultiProperty<WatchProvider>("Watch Providers", new List<WatchProvider> { MockData.NetflixStreaming });
    }

    public class TVSeason : Collection
    {
        public TVShow TVShow { get; }
        public int SeasonNumber { get; }

        public TVSeason(TVShow tvShow, int number)
        {
            TVShow = tvShow;
            Name = (tvShow?.Name ?? string.Empty) + " - Season " + number;
            SeasonNumber = number;
        }

        public static readonly Property<DateTime> YEAR = new Property<DateTime>("Year");
        public static readonly Property<TimeSpan> AVERAGE_RUNTIME = new Property<TimeSpan>("Average Runtime");
    }

    public class TVEpisode : Media
    {
        public TVSeason Season { get; }
        public int EpisodeNumber { get; }

        public TVEpisode(TVSeason season, string title, int episodeNumber) : base(title)
        {
            Season = season;
            EpisodeNumber = episodeNumber;
            //Name = (season?.TVShow?.Name ?? string.Empty) + " S" + season.SeasonNumber + "E" + episodeNumber;
        }

        public static readonly Property<DateTime> AIR_DATE = new Property<DateTime>("Air Date");
    }
}
