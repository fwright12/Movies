using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Movies.Models;

namespace Movies.ViewModels
{
    public class MovieViewModel : MediaViewModel<Movie>
    {
#if DEBUG
        public DateTime? Year => RequestValue(Movie.RELEASE_DATE);
        public long? Budget => RequestValue(Movie.BUDGET);
        public long? Revenue => RequestValue(Movie.REVENUE);
        public CollectionViewModel ParentCollection => _ParentCollection;// ??= (RequestSingle(DataManager.MovieService.ParentCollectionRequested) is Collection collection ? ForceLoad(new CollectionViewModel(DataManager, collection)) : null);
#else
        public DateTime? Year => RequestSingle(DataManager.MovieService.ReleaseDateRequested);
        public long? Budget => RequestSingle(DataManager.MovieService.BudgetRequested);
        public long? Revenue => RequestSingle(DataManager.MovieService.RevenueRequested);
        public CollectionViewModel ParentCollection => _ParentCollection;// ??= (RequestSingle(DataManager.MovieService.ParentCollectionRequested) is Collection collection ? ForceLoad(new CollectionViewModel(DataManager, collection)) : null);
#endif

        private CollectionViewModel _ParentCollection;
        protected override MediaService<Movie> MediaService => DataManager.MovieService;

        public MovieViewModel(DataManager dataManager, Movie movie) : base(dataManager, movie) { }
    }
}