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
    }
}