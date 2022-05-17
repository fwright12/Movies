using Movies.ViewModels;
using System.Collections.Generic;
using System;

namespace Movies.Models
{
    public abstract class Media : Item
    {
        public string Title => Name;
        public int? Year { get; }

        public Media(string title, int? year = null)
        {
            Name = title;
            Year = year;
        }

        public override string ToString() => (Name ?? string.Empty) + (Year.HasValue ? "(" + Year.Value + ")" : string.Empty);

        public static readonly Property<string> TITLE = new Property<string>("Title");
        public static readonly Property<string> TAGLINE = new Property<string>("Tagline");
        public static readonly Property<string> DESCRIPTION = new Property<string>("Description");
        public static readonly Property<string> CONTENT_RATING = new Property<string>("Content Rating", new DiscreteValueRange<string>(new List<string> { "G", "PG", "PG-13", "R", "NC-17" }));
        public static readonly Property<TimeSpan> RUNTIME = new Property<TimeSpan>("Runtime", new SteppedValueRange<TimeSpan, TimeSpan>(TimeSpan.Zero, TimeSpan.MaxValue, TimeSpan.FromMinutes(1)));
        public static readonly Property<string> ORIGINAL_TITLE = new Property<string>("Original Title");
        public static readonly Property<string> ORIGINAL_LANGUAGE = new Property<string>("Original Language");
        public static readonly Property<IEnumerable<string>> LANGUAGES = new Property<IEnumerable<string>>("Languages");
        public static readonly Property<string> GENRES = new Property<string>("Genres", new DiscreteValueRange<string>(new List<string> { "Action", "Adventure", "Romance", "Comedy", "Thriller", "Mystery", "Sci-Fi", "Horror", "Documentary" }));

        public static readonly Property<string> POSTER_PATH = new Property<string>("Poster Path");
        public static readonly Property<string> BACKDROP_PATH = new Property<string>("Backdrop Path");
        public static readonly Property<string> TRAILER_PATH = new Property<string>("Trailer Path");

        public static readonly Property<Rating> RATING = new Property<Rating>("Rating");
        public static readonly Property<IEnumerable<Credit>> CAST = new Property<IEnumerable<Credit>>("Cast");
        public static readonly Property<IEnumerable<Credit>> CREW = new Property<IEnumerable<Credit>>("Crew");
        public static readonly Property<IEnumerable<Company>> PRODUCTION_COMPANIES = new Property<IEnumerable<Company>>("Production Companies");
        public static readonly Property<IEnumerable<string>> PRODUCTION_COUNTRIES = new Property<IEnumerable<string>>("Production Countries");
        public static readonly Property<WatchProvider> WATCH_PROVIDERS = new Property<WatchProvider>("Watch Providers", new List<WatchProvider> { MockData.NetflixStreaming });
        public static readonly Property<string> KEYWORDS = new Property<string>("Keywords", new KeywordsSearch());
        public static readonly Property<IAsyncEnumerable<Item>> RECOMMENDED = new Property<IAsyncEnumerable<Item>>("Recommended");

        public class KeywordsSearch : Filterable<string>
        {
            protected override async IAsyncEnumerable<string> GetItems(List<Constraint> filters)
            {
                foreach (var item in await System.Threading.Tasks.Task.FromResult(App.AdKeywords))
                {
                    yield return item;
                }
            }
        }
    }
}