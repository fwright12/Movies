using System;
using System.Collections.Generic;
using System.Text;

namespace Movies.Models
{
    public enum MonetizationType { Subscription, Free, Ads, Rent, Buy }

    public class WatchProvider
    {
        public int Id { get; set; }
        public Company Company { get; set; }
        public MonetizationType Type { get; set; }
        public double Price { get; set; }

        public override bool Equals(object obj) => obj is WatchProvider provider && provider.Id == Id;
        public override int GetHashCode() => Id;

        public override string ToString() => Company?.Name ?? base.ToString();
    }
}
