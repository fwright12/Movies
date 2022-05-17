using Movies.ViewModels;
using System;

namespace Movies.Models
{
    public class Movie : Media
    {
        public Movie(string title, int? year = null) : base(title, year) { }

        public static readonly Property<DateTime> RELEASE_DATE = new Property<DateTime>("Release Date", new SteppedValueRange<DateTime, TimeSpan>(DateTime.MinValue, DateTime.MaxValue, TimeSpan.FromDays(1)));
        public static readonly Property<long> BUDGET = new Property<long>("Budget", new SteppedValueRange<long>(0, long.MaxValue, 1));
        public static readonly Property<long> REVENUE = new Property<long>("Revenue", new SteppedValueRange<long>(0, long.MaxValue, 1));
        public static readonly Property<Collection> PARENT_COLLECTION = new Property<Collection>("Parent Collection");
    }
}
