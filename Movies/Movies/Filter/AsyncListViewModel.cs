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
    public class ShellTabFix : Tab
    {
        private Dictionary<ShellContent, DataTemplate> Tabs = new Dictionary<ShellContent, DataTemplate>();

        public ShellTabFix()
        {
            if (Items is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += Observable_CollectionChanged;
            }

            PropertyChanged += ShellTabFix_PropertyChanged;
        }

        private void ShellTabFix_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentItem))
            {
                if (Tabs.Remove(CurrentItem, out var template) && CurrentItem.Content is ContentPage page && template.CreateContent() is ContentPage contentPage)
                {
                    page.Content = contentPage.Content;
                    foreach (var item in contentPage.ToolbarItems)
                    {
                        page.ToolbarItems.Add(item);
                    }
                }
            }
        }

        private void Observable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var content in e.NewItems.OfType<ShellContent>())
                {
                    if (content == CurrentItem)
                    {
                        continue;
                    }

                    Tabs[content] = content.ContentTemplate;
                    content.ClearValue(ShellContent.ContentTemplateProperty);
                    content.Content = new ContentPage();
                }
            }
        }
    }

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

    public static class RefreshHelper
    {
        public static readonly BindableProperty RefreshViewProperty = BindableProperty.CreateAttached("RefreshView", typeof(RefreshView), typeof(ItemsView), null, propertyChanged: (bindable, oldValue, newValue) => RefreshViewChanged((ItemsView)bindable, (RefreshView)oldValue, (RefreshView)newValue));

        public static RefreshView GetRefreshView(this ItemsView bindable) => (RefreshView)bindable.GetValue(RefreshViewProperty);
        public static void SetRefreshView(this ItemsView bindable, RefreshView value) => bindable.SetValue(RefreshViewProperty, value);

        private static void RefreshViewChanged(ItemsView items, RefreshView oldValue, RefreshView newValue)
        {
            oldValue?.RemoveBinding(BindableObject.BindingContextProperty);
            newValue?.SetBinding(BindableObject.BindingContextProperty, new Binding(ItemsView.ItemsSourceProperty.PropertyName, source: items));
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

        public bool IsRefreshRequired
        {
            get => _IsRefreshRequired;
            set => UpdateValue(ref _IsRefreshRequired, value);
        }

        public int InitialCount { get; set; } = 5;

        public bool CanRefresh => IsRefreshEnabled || IsRefreshRequired;

        public bool IsRefreshEnabled { get; set; }

        public ICommand RefreshCommand { get; }
        public ICommand LoadMoreCommand { get; }

        private IAsyncEnumerable<T> Source;
        private IAsyncEnumerator<T> Itr;
        private int LoadCount;
        private bool _IsRefreshRequired;
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
                if (e.PropertyName == nameof(IsRefreshEnabled) || e.PropertyName == nameof(IsRefreshRequired))
                {
                    OnPropertyChanged(nameof(CanRefresh));
                }
            };

            Reset();
        }

        public async Task LoadMore(int count = 1)
        {
            LoadCount = Math.Max(LoadCount, count);

            if (Loading)
            {
                return;
            }

            Loading = true;

            try
            {
                for (int i = 0; i < LoadCount && await Itr.MoveNextAsync(); i++)
                {
                    if (Itr.Current != null)
                    {
                        Items.Add(Itr.Current);
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
                ;
#endif
            }

            LoadCount = 0;
            Loading = false;
            IsRefreshRequired = false;
        }

        public async Task Refresh()
        {
            IsRefreshRequired = false;
            await LoadMore(InitialCount);
        }

        public void Reset()
        {
            CancelLoad?.Cancel();
            CancelLoad = new CancellationTokenSource();
            Itr = Source.GetAsyncEnumerator(CancelLoad.Token);

            Items.Clear();
            IsRefreshRequired = true;
        }

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //private void Filter(object sender, EventArgs<IEnumerable<Constraint>> e) => UpdateValues(e.Value);
    }
}