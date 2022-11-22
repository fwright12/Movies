using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class KeepItemsViewPopulatedBehavior : Behavior<ItemsView>
    {
        private static HashSet<string> Dependents = new HashSet<string>(new BindableProperty[]
        {
            ItemsView.ItemsSourceProperty,
            ItemsView.RemainingItemsThresholdReachedCommandProperty,
            ItemsView.RemainingItemsThresholdReachedCommandParameterProperty
        }.Select(property => property.PropertyName));

        protected override void OnAttachedTo(ItemsView bindable)
        {
            base.OnAttachedTo(bindable);

            bindable.ChildRemoved += (sender, e) => ChildrenChanged(bindable, e);
            bindable.ChildAdded += ChildrenChanged;
            bindable.PropertyChanged += ItemsChanged;
        }

        protected override void OnDetachingFrom(ItemsView bindable)
        {
            base.OnDetachingFrom(bindable);

            //bindable.ChildRemoved -= ChildrenChanged;
            bindable.ChildAdded -= ChildrenChanged;
            bindable.PropertyChanged -= ItemsChanged;
        }

        private void ItemsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Dependents.Contains(e.PropertyName))
            {
                var itemsView = (ItemsView)sender;
                Load(itemsView);

                if (e.PropertyName == ItemsView.ItemsSourceProperty.PropertyName && itemsView.ItemsSource is INotifyCollectionChanged observable)
                {
                    //observable.CollectionChanged += (sender, e) => Load(itemsView);
                }
            }
        }

        private void ChildrenChanged(object sender, ElementEventArgs e) => Load((ItemsView)sender);

        private void Load(ItemsView itemsView)
        {
            var items = itemsView.ItemsSource;

            // No items
            if (items?.GetEnumerator().MoveNext() == false)
            {
                var command = itemsView.RemainingItemsThresholdReachedCommand;
                var parameter = itemsView.RemainingItemsThresholdReachedCommandParameter;

                if (command?.CanExecute(parameter) == true)
                {
                    command.Execute(parameter);
                }
            }
        }
    }

    public class AsyncListViewModel<T> : BindableViewModel, IEnumerable<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IList<T> Items { get; set; }
        public bool Loading
        {
            get => _Loading;
            private set
            {
                if (value != _Loading)
                {
                    _Loading = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Refreshing
        {
            get => _Refreshing;
            set => UpdateValue(ref _Refreshing, value);
        }

        public bool CanRefresh => IsRefreshEnabled || Refreshing;

        public bool IsRefreshEnabled { get; set; }

        public ICommand RefreshCommand { get; }
        public ICommand LoadMoreCommand { get; }

        private IAsyncEnumerable<T> Source;
        private IAsyncEnumerator<T> Itr;
        private bool _Refreshing;
        private bool _Loading;

        private CancellationTokenSource CancelLoad;

        public AsyncListViewModel(IAsyncEnumerable<T> source)
        {
            var items = new ObservableCollection<T>();
            items.CollectionChanged += (sender, e) => CollectionChanged?.Invoke(this, e);
            Items = items;

            Source = source;
            LoadMoreCommand = new Command<int?>(count => _ = LoadMore(count ?? 1));
            RefreshCommand = new Command(() => _ = Refresh());

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(IsRefreshEnabled) || e.PropertyName == nameof(Refreshing))
                {
                    OnPropertyChanged(nameof(CanRefresh));
                }
            };

            Reset();
        }

        public async Task LoadMore(int count = 1)
        {
            if (Loading)
            {
                return;
            }

            Loading = true;

            try
            {
                for (int i = 0; i < count && await Itr.MoveNextAsync(); i++)
                {
                    Items.Add(Itr.Current);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
#endif
            }

            Loading = false;
            Refreshing = false;
        }

        public async Task Refresh()
        {
            Refreshing = false;
            await LoadMore(10);
        }

        public void Reset()
        {
            CancelLoad?.Cancel();
            CancelLoad = new CancellationTokenSource();
            Itr = Source.GetAsyncEnumerator(CancelLoad.Token);

            Items.Clear();
            Refreshing = true;
        }

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //private void Filter(object sender, EventArgs<IEnumerable<Constraint>> e) => UpdateValues(e.Value);
    }
}