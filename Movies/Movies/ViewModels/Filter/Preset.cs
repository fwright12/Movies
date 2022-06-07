using System;
using System.Collections.Generic;
using System.Text;

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
        public IList<Constraint> Value { get; set; } = new List<Constraint>();

        private bool _IsActive;
    }
}
