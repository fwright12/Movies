namespace Movies.Models
{
    public class Person : Item
    {
        public int BirthYear { get; }

        public Person(string name, int? birthYear = null)
        {
            Name = name;
            BirthYear = birthYear ?? 0;
        }
    }
}
