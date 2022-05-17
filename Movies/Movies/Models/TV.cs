﻿using System.Collections.Generic;
using System;

namespace Movies.Models
{
    public class TVShow : Collection
    {
        public string Title => Name;

        public TVShow(string title)
        {
            Name = title;
        }

        public static readonly Property<DateTime> FIRST_AIR_DATE = new Property<DateTime>("First Air Date");
        public static readonly Property<DateTime> LAST_AIR_DATE = new Property<DateTime>("Last Air Date");
        public static readonly Property<IEnumerable<Company>> NETWORKS = new Property<IEnumerable<Company>>("Networks");
    }

    public class TVSeason : Collection
    {
        public TVShow TVShow { get; }
        public int SeasonNumber { get; }

        public TVSeason(TVShow tvShow, int number)
        {
            TVShow = tvShow;
            Name = (tvShow?.Name ?? string.Empty) + " - Season " + number;
            SeasonNumber = number;
        }

        public static readonly Property<DateTime> YEAR = new Property<DateTime>("Year");
        public static readonly Property<TimeSpan> AVERAGE_RUNTIME = new Property<TimeSpan>("Average Runtime");
        public static readonly Property<IEnumerable<Credit>> CAST = new Property<IEnumerable<Credit>>("Cast");
        public static readonly Property<IEnumerable<Credit>> CREW = new Property<IEnumerable<Credit>>("Crew");
    }

    public class TVEpisode : Media
    {
        public TVSeason Season { get; }
        public int EpisodeNumber { get; }

        public TVEpisode(TVSeason season, string title, int episodeNumber) : base(title)
        {
            Season = season;
            EpisodeNumber = episodeNumber;
            //Name = (season?.TVShow?.Name ?? string.Empty) + " S" + season.SeasonNumber + "E" + episodeNumber;
        }

        public static readonly Property<DateTime> AIR_DATE = new Property<DateTime>("Air Date");
    }
}
