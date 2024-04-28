using Movies.Models;
using Movies.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    [ContentProperty(nameof(PageTemplate))]
    public class OpenReviewsCommand : PushPageCommand
    {
        public override bool CanExecute(object parameter) => false == parameter is Rating rating || rating.Reviews != null;
    }

    public class NullToBooleanIntConverter : IValueConverter
    {
        public static readonly NullToBooleanIntConverter Instance = new NullToBooleanIntConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null ? 0 : 1;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NewObjectConverter<T> : IValueConverter where T : new()
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new T();

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CollectionManagerViewModel<T> : BindableViewModel
    {
        public ICollection<T> Items { get; }
        public T SelectedItem
        {
            get => _SelectedItem;
            set => UpdateValue(ref _SelectedItem, value);
        }
        public int SelectedIndex
        {
            get => _SelectedIndex;
            set => UpdateValue(ref _SelectedIndex, value);
        }

        public bool SwitchToNewlyAddedItem { get; set; } = true;

        public Command<T> AddCommand { get; }
        public Command<T> RemoveCommand { get; }

        private T _SelectedItem;
        private int _SelectedIndex;

        public CollectionManagerViewModel()
        {
            Items = new ObservableCollection<T>();

            AddCommand = new Command<T>(Add);
            RemoveCommand = new Command<T>(item => Items.Remove(item));
        }

        private void Add(T item)
        {
            Items.Add(item);

            if (SwitchToNewlyAddedItem)
            {
                SelectedItem = item;
                SelectedIndex = Items.Count - 1;
            }
        }
    }
}
