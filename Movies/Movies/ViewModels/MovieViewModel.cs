using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Movies.Models;

namespace Movies.ViewModels
{
    public class MovieViewModel : MediaViewModel<Movie>
    {
        public DateTime? Year => RequestSingle(DataManager.MovieService.ReleaseDateRequested);
        public long? Budget => RequestSingle(DataManager.MovieService.BudgetRequested);
        public long? Revenue => RequestSingle(DataManager.MovieService.RevenueRequested);
        public CollectionViewModel ParentCollection => _ParentCollection ??= (RequestSingle(DataManager.MovieService.ParentCollectionRequested) is Collection collection ? ForceLoad(new CollectionViewModel(DataManager, collection)) : null);

        private CollectionViewModel _ParentCollection;
        protected override MediaService<Movie> MediaService => DataManager.MovieService;

        public MovieViewModel(DataManager dataManager, Movie movie) : base(dataManager, movie) { }
    }
}