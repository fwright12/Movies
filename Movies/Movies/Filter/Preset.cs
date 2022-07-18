using System.ComponentModel;

namespace Movies.ViewModels
{
    public class Preset : BindableViewModel
    {
        public string Text { get; set; }
        public bool IsActive
        {
            get => _IsActive;
            set
            {
                if (value != _IsActive)
                {
                    _IsActive = value;
                    OnPropertyChanged();
                }
            }
        }
        public FilterPredicate Value { get; set; }

        private bool _IsActive;
    }
}
