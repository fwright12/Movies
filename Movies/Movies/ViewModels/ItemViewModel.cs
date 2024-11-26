using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Movies.Data.Local;
using Movies.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies.ViewModels
{
    public abstract class ItemViewModel : AsyncDataViewModel
    {
        public Item Item { get; protected set; }
        public virtual string Name
        {
            get => Item?.Name;
            set
            {
                if (Item != null)
                {
                    Item.Name = value;
                }
            }
        }
        public virtual string PrimaryImagePath => null;
        public ICommand AddToListCommand { get; }

        private ChainLink<EventArgsAsyncWrapper<IEnumerable<KeyValueRequestArgs<Uri>>>> Controller => DataService.Instance.Controller;

        protected Task BatchRequest => BatchRequestSource?.Task ?? Task.CompletedTask;
        private TaskCompletionSource<bool> BatchRequestSource;

        protected delegate bool TryGet<T>(out T value);

#if true
        public ItemViewModel(Item item)
        {
            Item = item;

            AddToListCommand = new Command<IEnumerable<object>>(async lists =>
            {
                await Task.WhenAll(lists.OfType<ListViewModel>().Select(list => Item != null && ((list.Item as List)?.AllowedTypes ?? ItemType.All).HasFlag(Item.ItemType) ? list.Add(Item) : Task.CompletedTask));
            });
        }

        private Dictionary<string, KeyValueRequestArgs<Uri>> Batched = new Dictionary<string, KeyValueRequestArgs<Uri>>();

        private async void BatchEnded(object sender, EventArgs e)
        {
            DataService.Instance.BatchCommitted -= BatchEnded;

            var batch = Batched;
            Batched = new Dictionary<string, KeyValueRequestArgs<Uri>>();

            await Controller.Get(batch.Values);
            BatchRequestSource?.SetResult(true);
            BatchRequestSource = null;

            foreach (var kvp in batch)
            {
                if (kvp.Value.IsHandled)
                {
                    OnPropertyChanged(kvp.Key);
                }
            }
        }

        protected bool TryGetValue<T>(Property property, out T value, [CallerMemberName] string propertyName = null)
        {
            if (DataService.Instance.Batched)
            {
                if (Batched.Count == 0)
                {
                    if (this is PersonViewModel)
                    {
                        Batched["credits"] = new KeyValueRequestArgs<Uri>(CreateUii(Item, Person.CREDITS), Person.CREDITS.FullType);
                    }
                    if (this is TVSeasonViewModel)
                    {
                        Batched["episodes"] = new KeyValueRequestArgs<Uri>(CreateUii(Item, TVSeason.EPISODES), TVSeason.EPISODES.FullType);
                        Batched[nameof(TVSeasonViewModel.Cast)] = new KeyValueRequestArgs<Uri>(CreateUii(Item, TVSeason.CAST), TVSeason.CAST.FullType);
                        Batched[nameof(TVSeasonViewModel.Crew)] = new KeyValueRequestArgs<Uri>(CreateUii(Item, TVSeason.CREW), TVSeason.CREW.FullType);
                    }

                    TMDbRequest idsRequest;
                    if (this is MovieViewModel) idsRequest = API.MOVIES.GET_EXTERNAL_IDS;
                    else if (this is TVShowViewModel) idsRequest = API.TV.GET_EXTERNAL_IDS;
                    else idsRequest = null;

                    if (idsRequest != null && Item?.TryGetID(TMDB.ID, out var id) == true)
                    {
                        var uri = new Uri(string.Format(idsRequest.GetURL(), id), UriKind.Relative);
                        Batched["externalIDs"] = new KeyValueRequestArgs<Uri>(uri);
                    }
                }

                Batched[propertyName] = new KeyValueRequestArgs<Uri>(CreateUii(Item, property), property.FullType);
                BatchRequestSource ??= new TaskCompletionSource<bool>();
                DataService.Instance.BatchCommitted -= BatchEnded;
                DataService.Instance.BatchCommitted += BatchEnded;

                value = default;
                return false;
            }

            //value = default;
            //var state = DataService.Instance.ResourceCache.ReadAsync(new UniformItemIdentifier(Item, property));

            //if (state.IsCompleted)
            //{
            //    state.Result.TryGetRepresentation<T>(out value);
            //    return true;
            //}

            //return false;
            try
            {
                return TryGetValue(GetValue<T>(property), out value, propertyName);
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(e);
#endif
                value = default;
                return false;
            }
        }

        private async Task<T> GetValue<T>(Property property)
        {
            //return DataService.Instance.ResourceCache.ReadAsync(new UniformItemIdentifier(Item, property)).Result.TryGetRepresentation<T>(out var temp) ? temp : default;
            //var request = new RestRequestArgs<T>();
            var request = await Controller.TryGet<T>(CreateUii(Item, property));
            return request.IsHandled ? request.Value : default;
        }

        private static UniformItemIdentifier CreateUii(Item item, Property property) => new UniformItemIdentifier(item, property);//, language: TMDB.LANGUAGE);

        protected bool TryRequestValue<T>(Property<T> property, out T value, [CallerMemberName] string propertyName = null) => TryGetValue(property, out value, propertyName);
        protected bool TryRequestValue<T>(MultiProperty<T> property, out IEnumerable<T> value, [CallerMemberName] string propertyName = null) => TryGetValue(property, out value, propertyName);
#else
        protected PropertyDictionary Properties;

        public ItemViewModel(Item item)
        {
            Item = item;

            if (Item != null)
            {
                Properties = DataService.Instance.GetDetails(Item);
            }

            AddToListCommand = new Command<IEnumerable<object>>(async lists =>
            {
                await Task.WhenAll(lists.OfType<ListViewModel>().Select(list => Item != null && ((list.Item as List)?.AllowedTypes ?? ItemType.All).HasFlag(Item.ItemType) ? list.Add(Item) : Task.CompletedTask));
            });
        }

        private Dictionary<string, Property> Batched = new Dictionary<string, Property>();

        private async void BatchEnded(object sender, EventArgs e)
        {
            Properties.RequestValues(Batched.Values);

            var tasks = new Dictionary<Task, string>();

            foreach (var kvp in Batched)
            {
                var property = kvp.Value;
                var count = int.MaxValue;
                Task<object> task = null;

                while (Properties.TryGetValue(property, out task) && Invalidate(property, task) && count > 0)
                {
                    count = Properties.ValueCount(property);
                }

                if (task != null)
                {
                    tasks[task] = kvp.Key;
                }
            }

            Batched.Clear();

            while (tasks.Count > 0)
            {
                var ready = await Task.WhenAny(tasks.Keys);

                if (tasks.TryGetValue(ready, out var propertyName))
                {
                    tasks.Remove(ready);
                    OnPropertyChanged(propertyName);
                }
            }
        }

        protected bool TryRequestValue<T>(TryGet<Task<T>> getTask, Property property, out T value, [CallerMemberName] string propertyName = null)
        {
            if (DataService.Instance.Batched)
            {
                Batched[propertyName] = property;
                DataService.Instance.BatchCommitted -= BatchEnded;
                DataService.Instance.BatchCommitted += BatchEnded;

                value = default;
                return false;
            }

            var count = int.MaxValue;
            Task<T> task = null;

            while (getTask(out task) && Invalidate(property, task) && count > 0)
            {
                count = Properties.ValueCount(property);
            }

            return TryGetValue(task, out value, propertyName);
        }

        protected bool TryRequestValue<T>(Property<T> property, out T value, [CallerMemberName] string propertyName = null) => TryRequestValue((out Task<T> task) => Properties.TryGetValue(property, out task), property, out value, propertyName);
        protected bool TryRequestValue<T>(MultiProperty<T> property, out IEnumerable<T> value, [CallerMemberName] string propertyName = null) => TryRequestValue((out Task<IEnumerable<T>> task) => Properties.TryGetValues(property, out task), property, out value, propertyName);

        private bool Invalidate(Property property, Task value) => value.Exception?.InnerExceptions.All(exception => exception is System.Net.Http.HttpRequestException) == true && Properties.Invalidate(property, value);
#endif

        protected T RequestValue<T>(Property<T> property, [CallerMemberName] string propertyName = null) => TryRequestValue(property, out var value, propertyName) ? value : default;
        protected IEnumerable<T> RequestValue<T>(MultiProperty<T> property, [CallerMemberName] string propertyName = null) => TryRequestValue(property, out var value, propertyName) ? value : default;

        public override string ToString() => Item?.ToString() ?? base.ToString();
    }
}
