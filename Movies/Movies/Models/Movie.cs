namespace Movies.Models
{
    public class Movie : Media
    {
        public Movie(string title, int? year = null) : base(title, year) { }
    }
}
