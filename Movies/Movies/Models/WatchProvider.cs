using System;
using System.Collections.Generic;
using System.Text;

namespace Movies.Models
{
    public enum MonetizationType { Subscription, Free, Ads, Rent, Buy }

    public class WatchProvider
    {
        public Company Company { get; set; }
        public MonetizationType Type { get; set; }
        public double Price { get; set; }
    }
}
