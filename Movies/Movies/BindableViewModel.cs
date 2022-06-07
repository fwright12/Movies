using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Movies.ViewModels
{
    public abstract class BindableViewModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<string, object> Values = new Dictionary<string, object>();

        protected T GetValue<T>([CallerMemberName] string propertyName = null) => Values.TryGetValue(propertyName, out var value) && value is T t ? t : default;

        protected void UpdateValue<T>(T value, [CallerMemberName] string propertyName = null) => Values[propertyName] = value;

        protected void UpdateValue<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(property, value))
            {
                OnPropertyChanging(propertyName);
                property = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected virtual void OnPropertyChanging([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(property));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
