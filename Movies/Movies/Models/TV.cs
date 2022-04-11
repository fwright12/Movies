namespace Movies.Models
{
    public class TVShow : Collection
    {
        public string Title => Name;

        public TVShow(string title)
        {
            Name = title;
        }
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
    }
}
