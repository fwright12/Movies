﻿using Movies.ViewModels;
using System.Collections.Generic;
using System;
using System.Threading;

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
        public static readonly Property<TimeSpan> RUNTIME = new Property<TimeSpan>("Runtime", new SteppedValueRange
        {
            First = TimeSpan.Zero,
            Step = TimeSpan.FromMinutes(1)
        });
        public static readonly Property<string> ORIGINAL_TITLE = new Property<string>("Original Title");
        public static readonly Property<Language> ORIGINAL_LANGUAGE = new Property<Language>("Original Language");
        public static readonly MultiProperty<Language> LANGUAGES = new MultiProperty<Language>("Languages");

        public static readonly Property<string> POSTER_PATH = new Property<string>("Poster Path");
        public static readonly Property<string> BACKDROP_PATH = new Property<string>("Backdrop Path");
        public static readonly Property<string> TRAILER_PATH = new Property<string>("Trailer Path");

        public static readonly Property<Rating> RATING = new Property<Rating>("Rating");
        public static readonly MultiProperty<Credit> CAST = new MultiProperty<Credit>("Cast");
        public static readonly MultiProperty<Credit> CREW = new MultiProperty<Credit>("Crew");
        public static readonly MultiProperty<Company> PRODUCTION_COMPANIES = new MultiProperty<Company>("Production Companies");
        public static readonly MultiProperty<string> PRODUCTION_COUNTRIES = new MultiProperty<string>("Production Countries");
        public static readonly Property<IAsyncEnumerable<Item>> RECOMMENDED = new Property<IAsyncEnumerable<Item>>("Recommended");
        public static readonly MultiProperty<Keyword> KEYWORDS = new MultiProperty<Keyword>("Keywords", new FilterListViewModel<Keyword>(new App.KeywordsSearch())
        { 
            Predicate = new SearchPredicateBuilder()
        });
    }
}