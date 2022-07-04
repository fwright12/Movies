namespace Movies.Models
{
    public class Keyword
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj) => obj is Keyword keyword && keyword.Id == Id && keyword.Name == Name;
        public override int GetHashCode() => Id;

        public override string ToString() => Name ?? base.ToString();
    }
}