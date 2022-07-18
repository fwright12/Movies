using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Movies.Models;

namespace Movies.ViewModels
{
    public class MovieViewModel : MediaViewModel<Movie>
    {
        public DateTime? Year => TryRequestValue(Movie.RELEASE_DATE, out var year) ? year : (DateTime?)null;
        public long? Budget => TryRequestValue(Movie.BUDGET, out var budget) ? budget : (long?)null;
        public long? Revenue => TryRequestValue(Movie.REVENUE, out var revenue) ? revenue : (long?)null;
        public CollectionViewModel ParentCollection => _ParentCollection ??= (RequestValue(Movie.PARENT_COLLECTION) is Collection collection ? new CollectionViewModel(DataManager, collection) : null);

        protected override Property<string> ContentRatingProperty => Movie.CONTENT_RATING;
        protected override MultiProperty<Genre> GenresProperty => Movie.GENRES;
        protected override MultiProperty<WatchProvider> WatchProvidersProperty => Movie.WATCH_PROVIDERS;

        private CollectionViewModel _ParentCollection;

        public MovieViewModel(DataManager dataManager, Movie movie) : base(dataManager, movie) { }
    }
}