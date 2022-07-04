using System;

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

        public static readonly Property<DateTime> BIRTHDAY = new Property<DateTime>("Birthday");
        public static readonly Property<string> BIRTHPLACE = new Property<string>("Birthplace");
        public static readonly Property<DateTime?> DEATHDAY = new Property<DateTime?>("Deathday");
        public static readonly MultiProperty<string> ALSO_KNOWN_AS = new MultiProperty<string>("Also Known As");
        public static readonly Property<string> GENDER = new Property<string>("Gender");
        public static readonly Property<string> BIO = new Property<string>("Bio");
        public static readonly Property<string> PROFILE_PATH = new Property<string>("Profile Path");
        public static readonly MultiProperty<Item> CREDITS = new MultiProperty<Item>("Credits");
    }
}
