using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Movies.Models;

namespace Movies.ViewModels
{
    public class TVSeriesViewModel : CollectionViewModel
    {
        public string PosterPath => RequestSingle(DataManager.TVShowService.PosterPathRequested);

        public TVSeriesViewModel(DataManager dataManager, TVShow tvShow) : base(dataManager, tvShow.Name, Concat(tvShow))
        {
            //LoadMoreCommand.Execute(int.MaxValue);
        }

        private static async IAsyncEnumerable<Item> Concat(TVShow show)
        {
            yield return show;

            /*if (show.Items == null)
            {
                yield break;
            }*/
            
            await foreach (Item item in show)
            {
                if (item is TVSeason season)
                {
                    yield return season;
                }
            }
        }
    }

    public class TVShowViewModel : MediaViewModel<TVShow>
    {
        public CollectionViewModel Seasons { get; }

#if DEBUG
        public DateTime? FirstAirDate => RequestValue(TVShow.FIRST_AIR_DATE);
        public DateTime? LastAirDate => RequestValue(TVShow.LAST_AIR_DATE);
        public IEnumerable<Company> Networks => RequestValue(TVShow.NETWORKS);
#else
        public DateTime? FirstAirDate => RequestSingle(DataManager.TVShowService.FirstAirDateRequested);
        public DateTime? LastAirDate => RequestSingle(DataManager.TVShowService.LastAirDateRequested);
        public IEnumerable<Company> Networks => RequestSingle(DataManager.TVShowService.NetworksRequested);
#endif


        protected override MediaService<TVShow> MediaService => DataManager.TVShowService;
        protected override Property<string> ContentRatingProperty => TVShow.CONTENT_RATING;
        protected override MultiProperty<string> GenresProperty => TVShow.GENRES;
        protected override MultiProperty<WatchProvider> WatchProvidersProperty => TVShow.WATCH_PROVIDERS;

        public TVShowViewModel(DataManager dataManager, TVShow show) : base(dataManager, show)
        {
            Seasons = new CollectionViewModel(dataManager, "Seasons", show);
        }
    }

    public class TVSeasonViewModel : CollectionViewModel
    {
        public int Number { get; }

#if DEBUG
        public DateTime? Year => RequestValue(TVSeason.YEAR);
        public TimeSpan? AvgRuntime => null;// RequestSingle(TVSeasonService.AvgRuntimeRequested);
        public List<Group<Credit>> Cast => MediaViewModel.GetCrew(RequestValue(TVSeason.CAST));
        public List<Group<Credit>> Crew => MediaViewModel.GetCrew(RequestValue(TVSeason.CREW));
#else
        public DateTime? Year => RequestSingle(TVSeasonService.YearRequested);
        public TimeSpan? AvgRuntime => null;// RequestSingle(TVSeasonService.AvgRuntimeRequested);
        public List<Group<Credit>> Cast => MediaViewModel.GetCrew(RequestSingle(TVSeasonService.CastRequested));
        public List<Group<Credit>> Crew => MediaViewModel.GetCrew(RequestSingle(TVSeasonService.CrewRequested));
#endif

        public override string PrimaryImagePath => (Item as Collection)?.PosterPath ?? base.PrimaryImagePath;

        private TVSeasonService TVSeasonService;

        public TVSeasonViewModel(DataManager dataManager, TVSeason season) : base(dataManager, season)
        {
            TVSeasonService = dataManager.TVSeasonService;
            Number = season.SeasonNumber;
            DescriptionLabel = "Summary";
            ListLabel = "Episodes";
        }
    }

    public class TVEpisodeViewModel : MediaViewModel<TVEpisode>
    {
        //new public string Title => Name + " (The Office S" + Season + ""
        //public string Name => RequestSingle<string>("Title") ?? RequestSingle<string>();

        public int? Season { get; }
        public int? Number { get; }

#if DEBUG
        public DateTime? AirDate => RequestValue(TVEpisode.AIR_DATE);
#else
        public DateTime? AirDate => RequestSingle(DataManager.TVEpisodeService.AirDateRequested);
#endif

        protected override MediaService<TVEpisode> MediaService => DataManager.TVEpisodeService;
        protected override Property<string> ContentRatingProperty => TVShow.CONTENT_RATING;
        protected override MultiProperty<string> GenresProperty => TVShow.GENRES;
        protected override MultiProperty<WatchProvider> WatchProvidersProperty => TVShow.WATCH_PROVIDERS;

        public TVEpisodeViewModel(DataManager dataManager, TVEpisode episode) : base(dataManager, episode)
        {
            Season = episode.Season?.SeasonNumber;
            Number = episode.EpisodeNumber;
        }
    }
}
