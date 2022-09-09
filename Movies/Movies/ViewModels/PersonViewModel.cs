using System;
using System.Collections;
using System.Collections.Generic;
using Movies.Models;

namespace Movies.ViewModels
{
    public class CreditList : IEnumerable
    {
        public IEnumerable Stuff { get; set; }
        public string Role { get; set; }

        public IEnumerator GetEnumerator() => Stuff.GetEnumerator();
    }

    public class PersonViewModel : CollectionViewModel
    {
        public DateTime? Birthday => TryRequestValue(Person.BIRTHDAY, out var birthday) ? birthday : (DateTime?)null;
        public string Birthplace => RequestValue(Person.BIRTHPLACE);
        public DateTime? Deathday => RequestValue(Person.DEATHDAY);
        public int? Age => ((Deathday ?? DateTime.Now) - Birthday)?.Days / 365;
        public IEnumerable<string> AlsoKnownAs => RequestValue(Person.ALSO_KNOWN_AS);
        public string Gender => RequestValue(Person.GENDER);
        public string Bio => RequestValue(Person.BIO);
        public string ProfilePath => RequestValue(Person.PROFILE_PATH);
        public IEnumerable<Item> Credits => RequestValue(Person.CREDITS);

        public override string PrimaryImagePath => ProfilePath;
        public override string Description => Bio;

        public PersonViewModel(Person person) : base(person)
        {
            //List.Description = Bio;
            //OnPropertyChanged(nameof(Item));
            ListLayout = ListLayouts.Grid;

            DescriptionLabel = "Bio";
            ListLabel = "Credits";

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Bio))
                {
                    OnPropertyChanged(nameof(Description));
                }
                else if (e.PropertyName == nameof(ProfilePath))
                {
                    OnPropertyChanged(nameof(PrimaryImagePath));
                }
                else if (e.PropertyName == nameof(Birthday))
                {
                    OnPropertyChanged(nameof(Age));
                }
                else if (e.PropertyName == nameof(Deathday))
                {
                    OnPropertyChanged(nameof(Age));
                }
            };
        }

        public static async IAsyncEnumerable<Item> GetCredits(Person person)
        {
            //var credits = await service.CreditsRequested.GetSingle(person);

            if (!DataService.Instance.GetDetails(person).TryGetValues(Person.CREDITS, out var task) || !(await task is IEnumerable<Item> credits))
            {
                yield break;
            }

            foreach (var media in credits)
            {
                yield return media;
            }
        }
    }
}
