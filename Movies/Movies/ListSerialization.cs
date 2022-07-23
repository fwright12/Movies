using Movies.Models;
using Movies.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Movies
{
    public partial class App
    {
        private enum ServiceName
        {
            Local,
            TMDb,
            Trakt
        }

        private static readonly object HISTORY_ID = new string("History");
        private static readonly object WATCHLIST_ID = new string("Watchlist");
        private static readonly object FAVORITES_ID = new string("Favorites");

        private async Task<List<ListViewModel.SyncOptions>> GetSyncOptionsAsync(ServiceName name, object id)
        {
            await LocalDatabase.Init;

            var result = new List<ListViewModel.SyncOptions>();

            foreach (var sync in await LocalDatabase.GetSyncsWithAsync(name.ToString(), id.ToString()))
            {
                try
                {
                    ListViewModel.SyncOptions options = null;

                    if (id == WATCHLIST_ID || id == HISTORY_ID || id == FAVORITES_ID)
                    {
                        options = await TryParseSync(sync.Name, id);
                    }

                    if (options == null)
                    {
                        options = await TryParseSync(sync.Name, sync.ID);
                    }

                    if (options == null)
                    {
                        await Task.WhenAll(
                            LocalDatabase.RemoveSyncsWithAsync(sync.Name, sync.ID, name.ToString(), id.ToString()),
                            LocalDatabase.RemoveSyncsWithAsync(name.ToString(), id.ToString(), sync.Name, sync.ID));
                    }
                    else
                    {
                        result.Add(options);
                    }
                }
                catch { }
            }

            return result;
        }

        /*private async Task<List<ListViewModel.SyncOptions>> GetNamedSyncList(object name)
        {
            await LocalDatabase.Init;

            var syncs = await LocalDatabase.GetSyncsWithAsync(ServiceName.Local.ToString(), name.ToString());
            var result = new List<ListViewModel.SyncOptions>();

            foreach (var sync in syncs)
            {
                try
                {
                    if ((await TryParseSync(sync.Name, name) ?? await TryParseSync(sync.Name, sync.ID)) is ListViewModel.SyncOptions options)
                    {
                        result.Add(options);
                    }
                }
                catch { }
            }

            return result;
        }*/

        private async Task<List<ListViewModel.SyncOptions>> GetNamedSyncList(object name)
        {
            var result = await GetSyncOptionsAsync(ServiceName.Local, name);

            if (result.Count == 0)
            {
                result.Add(new ListViewModel.SyncOptions
                {
                    Provider = LocalDatabase,
                    List = await GetList(LocalDatabase, name)
                });
                //result.Add((ServiceName.Local.ToString(), null));
            }

            return result;
        }

        private async Task<ListViewModel> GetWatchlist() => WithHandlers(new NamedListViewModel("Watchlist", await GetNamedSyncList(WATCHLIST_ID), ItemType.Movie | ItemType.TVShow));
        private async Task<ListViewModel> GetFavorites() => WithHandlers(new NamedListViewModel("Favorites", await GetNamedSyncList(FAVORITES_ID)));
        private async Task<ListViewModel> GetHistory() => WithHandlers(new NamedListViewModel("Watched", await GetNamedSyncList(HISTORY_ID), ItemType.Movie | ItemType.TVShow));

        private ListViewModel WithHandlers(ListViewModel list)
        {
            CustomListsChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<ListViewModel> { list }));
            return list;
        }

        private async Task<ListViewModel.SyncOptions> TryParseSync(string name, object id)
        {
            if (TryGetProvider<IListProvider>(name, out var provider))
            {
                List list = await GetList(provider, id);

                if (list != null)
                {
                    return new ListViewModel.SyncOptions
                    {
                        Provider = provider,
                        List = list
                    };
                }
            }

            return null;
        }

        private IEnumerable<(string Name, string ID)> ParseSync(IEnumerable<ListViewModel.SyncOptions> sources)
        {
            foreach (var source in sources)
            {
                if (TryGetProviderName(source.Provider, out var name))
                {
                    yield return (name.ToString(), source.List.ID);
                }
            }
        }

        private object GetNamedId(ListViewModel list)
        {
            if (list == Watchlist)
            {
                return WATCHLIST_ID;
            }
            else if (list == History)
            {
                return HISTORY_ID;
            }
            else if (list == Favorites)
            {
                return FAVORITES_ID;
            }
            else
            {
                return null;
            }
        }

        private Task<List> GetList(IListProvider provider, object id)
        {
            if (id == WATCHLIST_ID)
            {
                return provider.GetWatchlist();
            }
            else if (id == FAVORITES_ID)
            {
                return provider.GetFavorites();
            }
            else if (id == HISTORY_ID)
            {
                return provider.GetHistory();
            }
            else if (id != null)
            {
                return provider.GetListAsync(id.ToString());
            }
            else
            {
                return Task.FromResult<List>(null);
            }
        }

        private bool TryGetProvider<T>(string name, out T provider)
        {
            if (Enum.TryParse(typeof(ServiceName), name, out var value) && value is ServiceName providerName && Services.TryGetValue(providerName, out var value1) && value1 is T tempProvider)
            {
                provider = tempProvider;
                return true;
            }

            provider = default;
            return false;
        }

        private bool TryGetProviderName(IService provider, out ServiceName name)
        {
            foreach (var kvp in Services)
            {
                if (kvp.Value == provider)
                {
                    name = kvp.Key;
                    return true;
                }
            }

            name = default;
            return false;
        }
    }
}