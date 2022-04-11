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
        public DateTime? Birthday => RequestSingle(PersonService.BirthdayRequested);
        public string Birthplace => RequestSingle(PersonService.BirthplaceRequested);
        public DateTime? Deathday => RequestSingle(PersonService.DeathdayRequested);
        public int? Age => ((Deathday ?? DateTime.Now) - Birthday)?.Days / 365;
        public IEnumerable<string> AlsoKnownAs => RequestSingle(PersonService.AlsoKnownAsRequested);
        public string Gender => RequestSingle(PersonService.GenderRequested);
        public string Bio => RequestSingle(PersonService.BioRequested);
        public string ProfilePath => RequestSingle(PersonService.ProfilePathRequested);
        public IEnumerable<Item> Credits => RequestSingle(PersonService.CreditsRequested);

        public override string PrimaryImagePath => ProfilePath;
        public override string Description => Bio;

        private PersonService PersonService => DataManager.PersonService;

        public PersonViewModel(DataManager dataManager, Person person) : base(dataManager, person)
        {
            //List.Description = Bio;
            //OnPropertyChanged(nameof(Item));
            ListLayout = Layout.Grid;

            DescriptionLabel = "Bio";
            ListLabel = "Credits";

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Bio))
                {
                    OnPropertyChanged(nameof(Item));
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

        public static async IAsyncEnumerable<Item> GetCredits(PersonService service, Person person)
        {
            var credits = await service.CreditsRequested.GetSingle(person);

            if (credits == null)
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
