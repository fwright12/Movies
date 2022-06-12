using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    //
    // Summary:
    //     Represents the method that will handle the System.ComponentModel.INotifyPropertyChanged.PropertyChanged
    //     event raised when a property is changed on a component.
    //
    // Parameters:
    //   sender:
    //     The source of the event.
    //
    //   e:
    //     A System.ComponentModel.PropertyChangedEventArgs that contains the event data.
    public delegate void PropertyChangeEventHandler(object sender, PropertyChangeEventArgs e);

    //
    // Summary:
    //     Provides data for the System.ComponentModel.INotifyPropertyChanged.PropertyChanged
    //     event.
    public class PropertyChangeEventArgs : EventArgs
    {
        //
        // Summary:
        //     Gets the name of the property that changed.
        //
        // Returns:
        //     The name of the property that changed.
        public string PropertyName { get; }

        public object OldValue { get; }
        public object NewValue { get; }

        //
        // Summary:
        //     Initializes a new instance of the System.ComponentModel.PropertyChangedEventArgs
        //     class.
        //
        // Parameters:
        //   propertyName:
        //     The name of the property that changed.
        public PropertyChangeEventArgs(string propertyName, object oldValue, object newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    //
    // Summary:
    //     Notifies clients that a property value has changed.
    public interface INotifyPropertyChange
    {
        //
        // Summary:
        //     Occurs when a property value changes.
        event PropertyChangeEventHandler PropertyChange;
    }
}

namespace Movies.ViewModels
{
    public abstract class BindableViewModel : INotifyPropertyChanged, INotifyPropertyChanging, INotifyPropertyChange
    {
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangeEventHandler PropertyChange;

        private Dictionary<string, object> Values = new Dictionary<string, object>();

        protected T GetValue<T>([CallerMemberName] string propertyName = null) => Values.TryGetValue(propertyName, out var value) && value is T t ? t : default;

        protected void UpdateValue<T>(T value, [CallerMemberName] string propertyName = null) => Values[propertyName] = value;

        protected bool UpdateValue<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(property, value))
            {
                OnPropertyChanging(propertyName);
                var e = new PropertyChangeEventArgs(propertyName, property, property = value);
                OnPropertyChanged(propertyName);
                PropertyChange?.Invoke(this, e);

                return true;
            }

            return false;
        }

        protected virtual void OnPropertyChanging([CallerMemberName] string property = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(property));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        protected virtual void OnPropertyChange(object oldValue, object newValue, [CallerMemberName] string property = null)
        {
            PropertyChange?.Invoke(this, new PropertyChangeEventArgs(property, oldValue, newValue));
        }
    }
}
