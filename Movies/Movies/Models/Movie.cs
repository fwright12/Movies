using System;
using System.Collections.Generic;

namespace Movies.Models
{
    public class Movie : Media
    {
        public Movie(string title, int? year = null) : base(title, year) { }

        public static readonly Property<DateTime> RELEASE_DATE = new Property<DateTime>("Release Date", new SteppedValueRange
        {
            Step = TimeSpan.FromDays(1)
        });
        public static readonly Property<long> BUDGET = new Property<long>("Budget", new SteppedValueRange
        {
            First = (long)0,
            Step = 1
        });
        public static readonly Property<long> REVENUE = new Property<long>("Revenue", new SteppedValueRange
        {
            First = (long)0,
            Step = 1
        });
        public static readonly Property<Collection> PARENT_COLLECTION = new Property<Collection>("Parent Collection");

        public static readonly Property<string> CONTENT_RATING = new Property<string>("Content Rating", new List<string> { "G", "PG", "PG-13", "R", "NC-17" });
        public static readonly MultiProperty<Genre> GENRES = new MultiProperty<Genre>("Genres", System.Linq.Enumerable.Select(new List<string> { "Action", "Adventure", "Romance", "Comedy", "Thriller", "Mystery", "Sci-Fi", "Horror", "Documentary" }, name => new Genre { Name = name }));
        public static readonly MultiProperty<WatchProvider> WATCH_PROVIDERS = new MultiProperty<WatchProvider>("Watch Providers", new List<WatchProvider> { MockData.NetflixStreaming });
    }
}
