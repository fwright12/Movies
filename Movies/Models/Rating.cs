using System;
using System.Collections.Generic;
using System.Text;

namespace Movies.Models
{
    public class Rating
    {
        public Company Company { get; set; }
        public string Score { get; set; }
        public double TotalVotes { get; set; }
        public IAsyncEnumerable<Review> Reviews { get; set; }
    }
}
