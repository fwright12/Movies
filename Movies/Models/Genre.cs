namespace Movies.Models
{
    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj) => obj is Genre genre && genre.Id == Id && genre.Name == Name;
        public override int GetHashCode() => Id;

        public override string ToString() => Name ?? base.ToString();
    }
}