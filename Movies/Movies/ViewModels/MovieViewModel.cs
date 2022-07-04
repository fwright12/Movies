using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Movies.Models;

namespace Movies.ViewModels
{
    public class MovieViewModel : MediaViewModel<Movie>
    {
        public DateTime? Year => RequestValue(Movie.RELEASE_DATE);
        public long? Budget => RequestValue(Movie.BUDGET);
        public long? Revenue => RequestValue(Movie.REVENUE);
        public CollectionViewModel ParentCollection => _ParentCollection ??= (RequestValue(Movie.PARENT_COLLECTION) is Collection collection ? new CollectionViewModel(DataManager, collection) : null);

        protected override Property<string> ContentRatingProperty => Movie.CONTENT_RATING;
        protected override MultiProperty<Genre> GenresProperty => Movie.GENRES;
        protected override MultiProperty<WatchProvider> WatchProvidersProperty => Movie.WATCH_PROVIDERS;

        private CollectionViewModel _ParentCollection;
        protected override MediaService<Movie> MediaService => DataManager.MovieService;

        public MovieViewModel(DataManager dataManager, Movie movie) : base(dataManager, movie) { }
    }
}