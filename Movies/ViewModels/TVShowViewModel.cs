using MauiExtensions.Services;
using Microsoft.Maui.Controls.Compatibility;
using Movies.Models;
using System.Collections.ObjectModel;

namespace Movies.ViewModels
{
    public class TVSeriesViewModel : CollectionViewModel
    {
        public string PosterPath => null;// RequestSingle(DataManager.TVShowService.PosterPathRequested);

        public TVSeriesViewModel(TVShow tvShow) : base(tvShow.Name, Concat(tvShow))
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

    public class MediaPageTriggerAction : TriggerAction<ScrollView>
    {
        protected override void Invoke(ScrollView sender)
        {
            var page = sender.Parent<Page>();
            if (page == null)
            {
                return;
            }

            var navBarHeight = 0;// PlatformService.NavBarHeight;
            VisualStateManager.GoToState(page, sender.ScrollY > sender.Content.Margin.Top - navBarHeight ? "Details" : "Poster");
        }
    }

    public class TVShowViewModel : MediaViewModel
    {
        public IEnumerable<ItemViewModel> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new ObservableCollection<ItemViewModel> { this };
                    AddSeasonsAsync();
                }

                return _Items;
            }
        }
        public CollectionViewModel Seasons => _Seasons ??= (RequestValue(TVShow.SEASONS) is IEnumerable<TVSeason> items ? new CollectionViewModel("Seasons", items.ToAsyncEnumerable()) : null);

        public DateTime? FirstAirDate => TryRequestValue(TVShow.FIRST_AIR_DATE, out var first) ? first : (DateTime?)null;
        public DateTime? LastAirDate => TryRequestValue(TVShow.LAST_AIR_DATE, out var last) ? last : (DateTime?)null;
        public IEnumerable<Company> Networks => RequestValue(TVShow.NETWORKS);

        protected override Property<string> ContentRatingProperty => TVShow.CONTENT_RATING;
        protected override MultiProperty<Genre> GenresProperty => TVShow.GENRES;
        protected override MultiProperty<WatchProvider> WatchProvidersProperty => TVShow.WATCH_PROVIDERS;

        private CollectionViewModel _Seasons;
        private ObservableCollection<ItemViewModel> _Items;

        public TVShowViewModel(TVShow show) : base(show) { }

        private async void AddSeasonsAsync()
        {
            return;
            await Task.Delay(5000);
            for (int i = 0; i < 5; i++)
                _Items.Add(new TVSeasonViewModel(new TVSeason((TVShow)Item, i)));
        }
    }

    public class TVSeasonViewModel : CollectionViewModel
    {
        public int Number { get; }

        public DateTime? Year => TryRequestValue(TVSeason.YEAR, out var year) ? year : (DateTime?)null;
        public TimeSpan? AvgRuntime => null;// RequestSingle(TVSeasonService.AvgRuntimeRequested);
        public List<Group<Credit>> Cast => MediaViewModel.GetCrew(RequestValue(TVSeason.CAST));
        public List<Group<Credit>> Crew => MediaViewModel.GetCrew(RequestValue(TVSeason.CREW));

        public override string PrimaryImagePath => (Item as Collection)?.PosterPath ?? base.PrimaryImagePath;

        public TVShowViewModel TVShowViewModel => _TVShowViewModel ??= new TVShowViewModel(((TVSeason)Item).TVShow);
        private TVShowViewModel _TVShowViewModel;

        //public TVSeasonViewModel(TVSeason season) : base(season)
        public TVSeasonViewModel(TVSeason season) : base(season.Name, GetEpisodes(season), null, season)
        {
            Number = season.SeasonNumber;
            DescriptionLabel = "Summary";
            ListLabel = "Episodes";
        }

        public static async IAsyncEnumerable<Item> GetEpisodes(TVSeason season)
        {
            await DataService.Instance.Batch;
            var request = await DataService.Instance.Controller.TryGet<IEnumerable<Item>>(new UniformItemIdentifier(season, TVSeason.EPISODES));

            if (!request.IsHandled)
            {
                yield break;
            }

            foreach (var episode in request.Value)
            {
                yield return episode;
            }
        }
    }

    public class TVEpisodeViewModel : MediaViewModel
    {
        //new public string Title => Name + " (The Office S" + Season + ""
        //public string Name => RequestSingle<string>("Title") ?? RequestSingle<string>();

        public int? Season { get; }
        public int? Number { get; }

        public DateTime? AirDate => TryRequestValue(TVEpisode.AIR_DATE, out var date) ? date : (DateTime?)null;

        protected override Property<string> ContentRatingProperty => TVShow.CONTENT_RATING;
        protected override MultiProperty<Genre> GenresProperty => TVShow.GENRES;
        protected override MultiProperty<WatchProvider> WatchProvidersProperty => TVShow.WATCH_PROVIDERS;

        public TVEpisodeViewModel(TVEpisode episode) : base(episode)
        {
            Season = episode.Season?.SeasonNumber;
            Number = episode.EpisodeNumber;
        }
    }
}
