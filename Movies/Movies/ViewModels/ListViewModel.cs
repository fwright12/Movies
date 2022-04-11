using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Movies.Models;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class ItemTypeFilterViewModel : StringFilterViewModel
    {
        public ItemType Selected
        {
            get
            {
                var selected = (ItemType)0;

                foreach (var value in Enum.GetValues(typeof(ItemType)))
                {
                    var type = (ItemType)value;

                    if (SelectedOptions.Contains(ItemTypeDisplayName(type)))
                    {
                        selected |= type;
                    }
                }

                return selected;
            }
        }

        public ItemTypeFilterViewModel(ItemType allowedTypes)
        {
            Options = new List<string>(allowedTypes.ToString().Split(',').Select(type => ItemTypeDisplayName(Enum.Parse<ItemType>(type.Trim()))));

            SelectedOptions = new ObservableCollection<object>();
            SelectedOptions.CollectionChanged += CoerceSelection;
        }

        private string ItemTypeDisplayName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Movie:
                case ItemType.Collection: return type.ToString() + "s";
                case ItemType.TVShow: return "TV Shows";
                case ItemType.Person: return "People";
                case ItemType.Company: return "Companies";
                default: return type.ToString();
            }
        }

        private bool IsExclusive(object item) => item is string str && (str == Options[3]);// || str == Options[4]);

        private void CoerceSelection(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SelectedOptions.Count > 1)
            {
                ObservableCollection<object> oldSelectedOptions = SelectedOptions;

                if (e.NewItems != null)
                {
                    foreach (object item in e.NewItems)
                    {
                        if (IsExclusive(item))
                        {
                            SelectedOptions = new ObservableCollection<object> { (string)item };
                            break;
                        }
                    }
                }

                if (SelectedOptions.Count > 1 && Enumerable.Any(SelectedOptions, IsExclusive))
                {
                    SelectedOptions = new ObservableCollection<object>(Enumerable.Where(SelectedOptions, value => !IsExclusive(value)));
                }

                if (SelectedOptions != oldSelectedOptions)
                {
                    SelectedOptions.CollectionChanged += CoerceSelection;
                    OnPropertyChanged(nameof(SelectedOptions));
                }
            }

            OnValueChanged();
        }
    }

    public class StringFilterViewModel : FilterViewModel
    {
        public IList<string> Options { get; set; }
        public ObservableCollection<object> SelectedOptions { get; set; }
    }

    public class SearchFilterViewModel : FilterViewModel
    {
        public string Query
        {
            get => _Query;
            set
            {
                if (value != _Query)
                {
                    _Query = value;
                    OnPropertyChanged(nameof(Query));
                }
            }
        }

        public string Placeholder { get; set; }
        public ICommand SearchCommand { get; }

        private string _Query;

        public SearchFilterViewModel()
        {
            SearchCommand = new Command(() => OnValueChanged());
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Query) && Query == null)
                {
                    OnValueChanged();
                }
            };
        }
    }

    public abstract class FilterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ValueChanged;

        public ICommand ItemTappedCommand { get; set; }

        protected void OnValueChanged()
        {
            ValueChanged?.Invoke(this, new EventArgs());
        }

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class AsyncList<T> : ObservableCollection<T>
    {
        public ICommand LoadCommand { get; }
        public bool Loading { get; private set; }

        private IAsyncEnumerator<T> Itr;

        public AsyncList(IAsyncEnumerable<T> items)
        {
            Itr = items.GetAsyncEnumerator();

            LoadCommand = new Command<int?>(count => _ = LoadLists(count));
        }

        private async Task LoadLists(int? count = null)
        {
            if (Loading)
            {
                return;
            }

            Loading = true;

            for (int i = 0; i < (count ?? 1) && await Itr.MoveNextAsync(); i++)
            {
                Add(Itr.Current);
            }

            Loading = false;
        }
    }

    public class ListSourceViewModel : ObservableCollection<CollectionViewModel>
    {
        public IListProvider Source { get; }
        public string Name { get; set; }

        public ListSourceViewModel(IListProvider source)
        {
            Source = source;
        }
    }

    public class NamedListViewModel : ListViewModel
    {
        public NamedListViewModel(DataManager dataManager, string name, ItemType allowedTypes, IEnumerable<SyncOptions> sources) : base(dataManager, sources)
        {
            if (Item is List list)
            {
                list.Reverse = true;
                list.Name = name;
                list.AllowedTypes = allowedTypes;
            }
        }
    }

    public class ListViewModel : CollectionViewModel
    {
        public event EventHandler SyncChanged;
        public event EventHandler<SavedEventArgs> Saved;

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand RemoveMultipleCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand RemoveSyncCommand { get; }

        public override bool Editable => true;
        public bool Editing
        {
            get => _Editing;
            set
            {
                if (_Editing != value)
                {
                    _Editing = value;
                    OnPropertyChanged();
                }
            }
        }
        public ICommand ToggleEditCommand { get; }

        public bool Syncing
        {
            get => _Syncing;
            set
            {
                if (value != _Syncing)
                {
                    _Syncing = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _Editing;
        private bool _Syncing;

        public ObservableCollection<SyncOptions> SyncWith { get; private set; }

        private Dictionary<object, bool?> ContainsCache = new Dictionary<object, bool?>();
        private IList<SyncOptions> SyncBackup;

        public class SyncOptions
        {
            public IListProvider Provider { get; set; }
            public List List { get; set; }
            public int Direction { get; set; }
        }

        public class SavedEventArgs
        {
            public IList<SyncOptions> OldSources { get; set; }
            public IList<SyncOptions> NewSources { get; set; }
        }

        public ListViewModel(DataManager manager, params SyncOptions[] sources) : this(manager, (IEnumerable<SyncOptions>)sources) { }
        public ListViewModel(DataManager manager, IEnumerable<SyncOptions> sources) : base(manager, new SyncList(sources.Select(sync => sync.List)))
        {
            SyncWith = new ObservableCollection<SyncOptions>(sources);
            SyncBackup = new List<SyncOptions>(SyncWith);

            SyncCommand = new Command<SyncOptions>(AddSync);
            RemoveSyncCommand = new Command<SyncOptions>(options => RemoveSync(options), options => SyncWith.Count > 1);
            SyncWith.CollectionChanged += (sender, e) => (RemoveSyncCommand as Command)?.ChangeCanExecute();

            AddCommand = new Command<Item>(item => _ = Add(item), item => item != null && (Item as List)?.AllowedTypes.HasFlag(item.ItemType) == true && Contains(item) == false);
            RemoveCommand = new Command<Item>(item => _ = Remove(item), item => item != null && (Item as List)?.AllowedTypes.HasFlag(item.ItemType) == true && Contains(item) == true);
            RemoveMultipleCommand = new Command<IEnumerable>(items => _ = Remove(items.OfType<ItemViewModel>().Select(item => item.Item)));

            DeleteListCommand = new Command(async () =>
            {
                //await Task.WhenAll(SyncWith.Select(source => source.List.Delete()));
                await (Item as List)?.Delete();
            });

            ToggleEditCommand = new Command(async () =>
            {
                Editing = !Editing;

                if (!Editing)
                {
                    await Save();
                }
            });

            if (Items is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += ListChanged;
            }
        }

        public void AddSync(SyncOptions options)
        {
            SyncWith.Add(options);
            SyncChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool RemoveSync(SyncOptions options)
        {
            if (SyncWith.Count > 1 && SyncWith.Remove(options))
            {
                SyncChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public async Task Save()
        {
            //await Task.WhenAll(SyncWith.Select(sync => sync.List.Update()));

            for (int i = 0; i < SyncBackup.Count; i++)
            {
                var backup = SyncBackup[i];
                var same = SyncWith.FirstOrDefault(sync => sync.Provider == backup.Provider);

                if (same != null && same.List != backup.List)
                {
                    SyncBackup[i] = same;
                }
            }

            var oldSources = SyncBackup.Except(SyncWith).ToList();
            var newSources = SyncWith.Except(SyncBackup).ToList();

            try
            {
                foreach (var source in oldSources)
                {
                    (Item as SyncList)?.RemoveSource(source.List);
                }

                if (Item is List list)
                {
                    if (string.IsNullOrEmpty(list.Description))
                    {
                        list.Description = null;
                    }

                    await list.Update();
                }

                if (Item is SyncList syncList)
                {
                    var before = syncList.Count;

                    foreach (var source in newSources)
                    {
                        await syncList.AddSourceAsync(source.List);
                    }

                    if (syncList.Count != before)
                    {
                        syncList.Reset();
                        UpdateItems(syncList);
                    }
                }

                OnPropertyChanged(nameof(Item));
                OnSaved(oldSources, newSources);
            }
            catch (Exception e)
            {
#if DEBUG
                Print.Log(e);
                ;
#endif
            }
        }

        protected virtual void OnSaved(IList<SyncOptions> oldSources, IList<SyncOptions> newSources)
        {
            Saved?.Invoke(this, new SavedEventArgs { OldSources = oldSources, NewSources = newSources });
            SyncBackup = new List<SyncOptions>(SyncWith);
        }

        public void UpdateCanExecute()
        {
            (AddCommand as Command)?.ChangeCanExecute();
            (RemoveCommand as Command)?.ChangeCanExecute();
        }

        public async Task Add(params Item[] items)
        {
            await ListChangedAsync(items, null);

            var observable = Items as INotifyCollectionChanged;

            if (observable != null)
            {
                observable.CollectionChanged -= ListChanged;
            }

            var order = this is NamedListViewModel ? items.Reverse() : items;
            //for (int i = items.Length - 1; i >= 0; i--)
            foreach (var item in order)
            {
                //ContainsCache[item] = true;
                if (Items is IList list)
                {
                    int index = -1;
                    if (this is NamedListViewModel)
                    {
                        index = 0;
                    }
                    else if (!(Item is Collection collection) || collection.IsFullyLoaded)
                    {
                        index = list.Count;
                    }

                    if (index != -1)
                    {
                        list.Insert(index, Map(item));
                    }
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += ListChanged;
            }

            //await Task.WhenAll(SyncWith.Select(source => source.List.AddAsync(items)));
            //await (Item as List)?.AddAsync(items);
        }

        public Task Remove(params Item[] items) => Remove((IEnumerable<Item>)items);
        public async Task Remove(IEnumerable<Item> items)
        {
            await ListChangedAsync(null, items);

            var observable = Items as INotifyCollectionChanged;

            if (observable != null)
            {
                observable.CollectionChanged -= ListChanged;
            }

            foreach (var item in items)
            {
                //ContainsCache[item] = false;

                if (Items is IList list)
                {
                    int index = 0;
                    foreach (var model in list)
                    {
                        if (model is ItemViewModel ivm && ivm.Item.Equals(item))
                        {
                            break;
                        }

                        index++;
                    }

                    if (index < list.Count)
                    {
                        list.RemoveAt(index);
                    }
                }
            }

            if (observable != null)
            {
                observable.CollectionChanged += ListChanged;
            }

            //await Task.WhenAll(SyncWith.Select(list => list.List.RemoveAsync(items)));
            //await (Item as List)?.RemoveAsync(items);
        }

        private async void ListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!Loading)
            {
                await ListChangedAsync(e.NewItems?.OfType<ItemViewModel>().Select(ivm => ivm.Item), e.OldItems?.OfType<ItemViewModel>().Select(ivm => ivm.Item));
            }
        }

        private async Task ListChangedAsync(IEnumerable<Item> newItems, IEnumerable<Item> oldItems)
        {
            if (newItems != null)
            {
                foreach (var item in newItems)
                {
                    ContainsCache[item] = true;
                }

                await (Item as List)?.AddAsync(newItems);
            }

            if (oldItems != null)
            {
                foreach (var item in oldItems)
                {
                    ContainsCache[item] = false;
                }

                await (Item as List)?.RemoveAsync(oldItems);
            }

            UpdateCanExecute();
        }

        private bool? Contains(Item item)
        {
            /*var check = (Item as List)?.Contains(item);
            if (check.Status == TaskStatus.RanToCompletion)
            {
                return check.Result;
            }
            else
            {
                _ = CanExecuteAsync(check);
                return null;
            }*/

            if (ContainsCache.TryGetValue(item, out var contains))
            {
                if (contains.HasValue)
                {
                    //ContainsCache.Remove(item);
                }

                return contains;
            }
            else
            {
                ContainsCache[item] = null;
                var check = CanExecuteAsync(item);
                return check.Status == TaskStatus.RanToCompletion ? (bool?)check.Result : null;
            }
        }

        private async Task<bool> CanExecuteAsync(Task<bool> task)
        {
            var result = await task;
            UpdateCanExecute();
            return result;
        }

        private async Task<bool?> CanExecuteAsync(Item item)
        {
            var result = await (Item as List)?.ContainsAsync(item);

            /*foreach (var source in SyncWith)
            {
                if (await source.List.ContainsAsync(item))
                {
                    result = true;
                    break;
                }
            }*/

            /*var checkAll = SyncWith.Select(sources => sources.List.ContainsAsync(item)).ToList();

            while (checkAll.Count > 0)
            {
                var task = await Task.WhenAny(checkAll);

                if (await task)
                {
                    result = true;
                    break;
                }

                checkAll.Remove(task);
            }*/

            if (ContainsCache.TryGetValue(item, out var contains) && !contains.HasValue)
            {
                ContainsCache[item] = result;
                UpdateCanExecute();
            }

            return result;
        }
    }
}
