using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Movies
{
    public class ReadOnlyLazyList<T> : IReadOnlyLazyCollection<T>
    {
        public Task<int> AsyncCount => Count;

        private Task<int> Count;
        private Func<T, Task<bool>> Contains;
        private IAsyncEnumerable<T> Items;

        public ReadOnlyLazyList(Task<int> count, Func<T, Task<bool>> contains, IAsyncEnumerable<T> items)
        {
            Items = items;
            Contains = contains;
            Count = count;
        }

        public Task<bool> ContainsAsync(T item) => Contains(item);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => Items.GetAsyncEnumerator();
    }

    public class ListSourceItems1<T> : IReadOnlyLazyCollection<T>
    {
        public Task<int> AsyncCount => Task.FromResult(0);// ListSource.CountAsync(ID);

        private IListProvider ListSource;
        private IAsyncEnumerable<T> Items;
        private int ID;

        public ListSourceItems1(int id, IListProvider listSource, IAsyncEnumerable<T> items)
        {
            ListSource = listSource;
            Items = items;
            ID = id;
        }

        public Task<bool> ContainsAsync(T item) => Task.FromResult(false);// ListSource.IsInListAsync(ID, item);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => Items.GetAsyncEnumerator(cancellationToken);
    }

    public class ObservableSet<T> : ObservableCollection<T>
    {
        private HashSet<T> Index = new HashSet<T>();

        public ObservableSet() : base() { }
        public ObservableSet(IEnumerable<T> collection) : base(collection) { }
        public ObservableSet(List<T> list) : base(list) { }

        new public bool Contains(T item) => Index.Contains(item);

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    Index.Remove((T)item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    Index.Add((T)item);
                }
            }
        }
    }

    public class LazyObservableCollection<T> : LazyCollection<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public LazyObservableCollection() : this(new ObservableCollection<T>()) { }

        private LazyObservableCollection(ObservableCollection<T> observable) : base(observable)
        {
            observable.CollectionChanged += OnCollectionChanged;
        }

        protected void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);
    }

    public class ItemList<T> : LazyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event NotifyCollectionChangedEventHandler ItemsChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Count
        {
            get
            {
                _ = UpdateCountAsync();
                return _Count;
            }
            private set
            {
                if (value != _Count)
                {
                    _Count = value;
                    OnPropertyChanged(nameof(Count));
                }
            }
        }

        public ICommand LoadMoreCommand => LoadMoreCommandCreator.Value;

        private Lazy<ICommand> LoadMoreCommandCreator;
        private int _Count;
        private Task Loading = Task.CompletedTask;
        private bool Busy = false;

        public ItemList(bool indexed = false) : this(indexed ? new ObservableSet<T>() : new ObservableCollection<T>()) { }

        private ItemList(ObservableCollection<T> observable) : base(observable)
        {
            LoadMoreCommandCreator = new Lazy<ICommand>(() =>
            {
                if (observable.Count == 0 && !HasLoadedCompletely)
                {
                    var task = ExecuteLoadMore(10);
                    //Task.Run(async () => await ExecuteLoadMore(10));
                }
                
                return new Xamarin.Forms.Command(async () => await ExecuteLoadMore());
            });

            observable.CollectionChanged += OnCollectionChanged;
            //CollectionChanged += OnItemsChanged;
        }

        private async Task ExecuteLoadMore(int count = 1)
        {
            if (Busy)
            {
                //return;
            }
            
            Busy = true;
            CollectionChanged -= OnItemsChanged;
            await LoadMore(count);
            //Loading = Loading.ContinueWith(async task => await LoadMore());
            //await Loading;
            CollectionChanged += OnItemsChanged;
        }

        private async Task UpdateCountAsync() => Count = await AsyncCount;

        protected void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        protected void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e) => ItemsChanged?.Invoke(this, e);

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

#if False
    public class ItemList1<T> : ObservableCollection<T>, IReadOnlyLazyCollection<T>
    {
        public ICommand LoadMoreCommand { get; private set; }

        Task<int> IReadOnlyLazyCollection<T>.AsyncCount => throw new NotImplementedException();

        private LazyCollection<T> LazyCollection;
        private HashSet<T> Index = new HashSet<T>();

        public ItemList1(bool indexed = false) : base() { Init(); }
        public ItemList1(IEnumerable<T> collection) : base(collection) { Init(); }
        public ItemList1(List<T> list) : base(list) { Init(); }

        private void Init()
        {
            LazyCollection = new LazyCollection<T>(this);
            CollectionChanged += ItemsChanged;

            LoadMoreCommand = new Xamarin.Forms.Command(async () =>
            {
                await LazyCollection.LoadMore();
            });
        }

        public void AddLazy(IReadOnlyLazyCollection<T> lazyCollection) => LazyCollection.AddLazy(lazyCollection);

        public bool ContainsAsync(T item) => Index.Contains(item);

        private void ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    Index.Remove((T)item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    Index.Add((T)item);
                }
            }
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        Task<bool> IReadOnlyLazyCollection<T>.ContainsAsync(T item)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class ItemListOld<T> : ObservableCollection<T>
    {
        public abstract int TotalCount { get; }

        public ICommand LoadMore { get; private set; }

        private LinkedList<IAsyncEnumerator<T>> Streams;
        private HashSet<T> Index;
        private bool IsFullyLoaded;

        public ItemListOld() : base() { }

        public ItemListOld(IEnumerable<T> collections) : base(collections) { }

        public ItemListOld(IAsyncEnumerable<T> asyncCollection) : base()
        {
            Streams.Add(asyncCollection.GetAsyncEnumerator());
        }

        public ItemListOld(IEnumerable<T> collection, IAsyncEnumerable<T> asyncCollection) : this(collection)
        {
            
        }

        private void Init(IAsyncEnumerator<T> itr)
        {
            Streams = new LinkedList<IAsyncEnumerator<T>>();
            if (itr != null)
            {
                Streams.AddFirst(itr);
            }

            Index = new HashSet<T>();

            LoadMore = new Xamarin.Forms.Command(async () =>
            {

            });
        }

        public abstract void AddLazy(IAsyncEnumerator<T> itr);
        public async Task<bool> ContainsAsync(T item) => Index.Contains(item) || await UnloadedContainsAsync(item);
        public abstract Task<bool> UnloadedContainsAsync(T item);
        public abstract Task<bool> RemoveAsync(T item);

        private async Task LoadMore1(int count = 1)
        {
            for (int i = 0; i < count && !IsFullyLoaded; i++)
            {
                if (Streams.First != null && await Streams.First.Value.MoveNextAsync())
                {
                    Add(Streams.First.Value.Current);
                }
                else
                {
                    IsFullyLoaded = true;
                }
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    Index.Remove((T)item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    Index.Add((T)item);
                }
            }
        }

        public abstract IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
    }
#endif
}