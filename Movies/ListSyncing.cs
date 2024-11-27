using Movies.Models;
using Movies.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies
{
    public partial class App
    {
        public ICommand AddSyncSourceCommand { get; }
        public ICommand CreateListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand LoadListsCommand { get; }
        public ICommand OpenFiltersCommand { get; }

        private async Task AddSyncSource(ListViewModel model)
        {
            if (model == null)
            {
                return;
            }

            var options = LoggedInListProviders().Except(model.SyncWith.Select(sync => sync.Provider)).ToList();
            var cancel = "Cancel";
            var choice = await Shell.Current.CurrentPage.DisplayActionSheet("Add Sync Source", cancel, null, options.Select(provider => provider.Name).ToArray());

            if (choice != cancel)
            {
                var provider = options.FirstOrDefault(provider => provider.Name == choice);

                var id = GetNamedId(model);
                var list = await GetList(provider, id);

                if (id != null && list == null)
                {
                    await foreach (var temp in provider.GetAllListsAsync())
                    {
                        if (temp.Name == model.Name)
                        {
                            list = temp;

                            /*for (int i = 0; i < CustomLists.Count; i++)
                            {
                                if (CustomLists[i].SyncWith.FirstOrDefault(sync => Equals(sync.List.ID, temp.ID)) is ListViewModel.SyncOptions sync)
                                {
                                    if (!CustomLists[i].RemoveSync(sync))
                                    {
                                        CustomLists.RemoveAt(i);
                                    }
                                    break;
                                }
                            }*/

                            break;
                        }
                    }
                }

                model.AddSync(new ListViewModel.SyncOptions
                {
                    Provider = provider,
                    List = list ?? provider.CreateList()
                });
            }
        }

        private async Task CreateList(ElementTemplate template)
        {
            var source = Services.Values.OfType<IListProvider>().FirstOrDefault();

            if (source == null)
            {
                return;
            }

            var model = new ListViewModel
            {
                Editing = true
            };

            model.AddSync(new ListViewModel.SyncOptions
            {
                Provider = source,
                List = source.CreateList()
            });

            if (model.Item is List list)
            {
                list.Name = "New List";
            }

            CustomListsChanged(CustomLists, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<ListViewModel> { model }));

            var content = template.CreateContent();
            var page = content as Page ?? new ContentPage { Content = content as View };
            page.BindingContext = model;

            await Shell.Current.CurrentPage.Navigation.PushAsync(page);
        }

        private async Task DeleteList(ListViewModel list)
        {
            if (list?.GetType() == typeof(ListViewModel) && await Shell.Current.CurrentPage.DisplayAlert("Are you sure?", "This will permanently delete this list and all synced lists. This action cannot be undone", "Delete", "Cancel"))
            {
                await Current.MainPage.Navigation.PopAsync();
                CustomLists.Remove(list);
            }
        }

        private Queue<(IListProvider Provider, IAsyncEnumerator<List> Itr)> QueuedProviders;
        //private Dictionary<ServiceName, HashSet<object>> lists = new Dictionary<ServiceName, HashSet<object>>();
        private async Task<IEnumerable<ListViewModel>> AllLists()
        {
            await Task.WhenAll(LazyFavorites.Value, LazyWatchlist.Value, LazyHistory.Value);
            return CustomLists.Prepend(Favorites).Prepend(Watchlist).Prepend(History);
        }

        private async Task<bool> IsListInUse(IListProvider provider, List list) => (await AllLists()).Any(lvm => lvm.SyncWith.Any(source => source.Provider == provider && source.List.ID.Equals(list.ID)));

        private bool LoadingLists;

        private async Task LoadLists(int? count = null)
        {
            if (LoadingLists)
            {
                return;
            }
            LoadingLists = true;

            //while (QueuedProviders.Count > 0)
            for (int i = 0; i < (count ?? 1) && QueuedProviders.Count > 0;)
            {
                if (!await QueuedProviders.Peek().Itr.MoveNextAsync())
                {
                    QueuedProviders.Dequeue();
                }
                else
                {
                    var source = QueuedProviders.Peek().Provider;
                    var list = QueuedProviders.Peek().Itr.Current;

                    if (!TryGetProviderName(source, out var name) || await IsListInUse(source, list))// || (lists.TryGetValue(name, out var cache) && cache.Contains(list.ID)))
                    {
                        continue;
                    }

                    /*if (list.Name.ToLower() == "tmdb list" && TryGetProvider<TMDB>(ServiceName.TMDb.ToString(), out var tmdb))
                    {
                        var itr = tmdb.GetTrendingMoviesAsync().GetAsyncEnumerator();
                        var items = new List<Item>();
                        for (int i = 0; i < 50 && await itr.MoveNextAsync(); i++)
                        {
                            items.Add(itr.Current);
                        }

                        await list.AddAsync(items);
                    }*/

                    //var syncs = await LocalDatabase.GetSyncsWithAsync(name.ToString(), list.ID);
                    var sources = (await GetSyncOptionsAsync(name, list.ID)).Prepend(new ListViewModel.SyncOptions
                    {
                        Provider = source,
                        List = list
                    });

                    /*foreach (var synced in sync)
                    {
                        if (!TryGetProviderName(synced.Provider, out var otherName))
                        {
                            continue;
                        }

                        if (!lists.TryGetValue(otherName, out var l))
                        {
                            lists[otherName] = l = new HashSet<object>();
                        }

                        l.Add(synced.List.ID);
                    }*/

                    var lvm = new ListViewModel(sources);
                    CustomLists.Add(lvm);
                    i++;
                }
            }

            LoadingLists = false;
        }

        private async void CustomListsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var list in e.OldItems.OfType<ListViewModel>())
                {
                    var args = new ListViewModel.SavedEventArgs
                    {
                        OldSources = new List<ListViewModel.SyncOptions>(list.SyncWith),
                        NewSources = new List<ListViewModel.SyncOptions>()
                    };

                    list.SyncWith.Clear();
                    UpdateSyncTable(list, args);
                    //await Task.WhenAll(list.SyncWith.Select(sync => sync.List.Delete()));
                    await (list.Item as List)?.Delete();
                }
            }

            if (e.NewItems != null)
            {
                foreach (var list in e.NewItems.OfType<ListViewModel>())
                {
                    list.Saved += UpdateSyncTable;
                    if (!(list is NamedListViewModel))
                    {
                        list.Saved += RenameOldLists;
                    }
                    //lvm.SyncWith.CollectionChanged += UpdateSyncTable;
                    list.SyncChanged += (sender, e) => CoerceSyncList(((ListViewModel)sender).SyncWith);
                    list.SyncWith.CollectionChanged += (sender, e) => (AddSyncSourceCommand as Command)?.ChangeCanExecute();
                    CoerceSyncList(list.SyncWith);

                    await RestoreFilters(list);
                }
            }
        }

        private async void CoerceSyncList(IList<ListViewModel.SyncOptions> list)
        {
            if (list.Count > 1 && list[0].Provider != LocalDatabase)
            {
                int i;
                for (i = 0; i < list.Count; i++)
                {
                    if (list[i].Provider == LocalDatabase)
                    {
                        if (list is ObservableCollection<ListViewModel.SyncOptions> observable)
                        {
                            observable.Move(i, 0);
                        }
                        else
                        {
                            list.Insert(0, list[i]);
                            list.RemoveAt(i + 1);
                        }

                        break;
                    }
                }

                if (i == list.Count)
                {
                    list.Insert(0, new ListViewModel.SyncOptions
                    {
                        Provider = LocalDatabase,
                        List = LocalDatabase.CreateList()
                    });
                    await Shell.Current.CurrentPage.DisplayAlert("", "If this list syncs with two or more remote sources, a local list must be the first source", "OK");
                }
            }
        }

        /*private Task<IEnumerable<ListViewModel.SyncOptions>> ParseSync(params (string Name, object ID)[] syncs) => ParseSync((IEnumerable<(string, object)>)syncs);
        private async Task<IEnumerable<ListViewModel.SyncOptions>> ParseSync(IEnumerable<(string Name, object ID)> syncs)
        {
            var parsed = new List<ListViewModel.SyncOptions>();

            foreach (var sync in syncs)
            {
                if ((await TryParseSync(sync.Name, sync.ID)) is ListViewModel.SyncOptions options)
                {
                    parsed.Add(options);
                }
            }

            return parsed;
        }*/

        private async void RenameOldLists(object sender, ListViewModel.SavedEventArgs e)
        {
            foreach (var sync in e.OldSources)
            {
                sync.List.Name += string.Format(" [{0}]", sync.Provider.Name);
                await sync.List.Update();
                CustomLists.Add(new ListViewModel(sync));
            }
        }

        private async void UpdateSyncTable(object sender, ListViewModel.SavedEventArgs e)
        {
            var list = (ListViewModel)sender;
            var oldSources = ParseSync(e.OldSources);
            var newSources = ParseSync(e.NewSources);
            var current = ParseSync(list.SyncWith);

            if (GetNamedId(list) is string namedList)
            {
                current = current.Prepend((ServiceName.Local.ToString(), namedList));
            }

            var past = current.Except(newSources).Concat(oldSources).ToList();

            foreach (var sync in oldSources)
            {
                foreach (var other in past.Intersect(await LocalDatabase.GetSyncsWithAsync(sync.Name, sync.ID)))
                {
                    if (other != sync)
                    {
                        await Task.WhenAll(
                            LocalDatabase.RemoveSyncsWithAsync(sync.Name, sync.ID, other.Name, other.ID),
                            LocalDatabase.RemoveSyncsWithAsync(other.Name, other.ID, sync.Name, sync.ID));
                    }
                }
            }

            foreach (var sync in current)
            {
                var locus = current.FirstOrDefault();

                if (sync != locus)
                {
                    await Task.WhenAll(
                        LocalDatabase.SetSyncsWithAsync(locus.Name, locus.ID, sync.Name, sync.ID),
                        LocalDatabase.SetSyncsWithAsync(sync.Name, sync.ID, locus.Name, locus.ID));
                }
            }
        }
    }
}