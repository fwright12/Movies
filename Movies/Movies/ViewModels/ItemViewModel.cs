using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Movies.Models;
using Xamarin.Forms;

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

        private ChainLink<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> Controller => DataService.Instance.Controller;

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

        private Dictionary<string, RestRequestArgs> Batched = new Dictionary<string, RestRequestArgs>();

        private async void BatchEnded(object sender, EventArgs e)
        {
            DataService.Instance.BatchCommitted -= BatchEnded;

            var batch = Batched;
            Batched = new Dictionary<string, RestRequestArgs>();

            await Controller.Get(batch.Values);

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
                if (this is PersonViewModel && Batched.Count == 0)
                {
                    Batched["credits"] = new RestRequestArgs(new UniformItemIdentifier(Item, Person.CREDITS), Person.CREDITS.FullType);
                }
                if (this is TVSeasonViewModel && Batched.Count == 0)
                {
                    Batched["episodes"] = new RestRequestArgs(new UniformItemIdentifier(Item, TVSeason.EPISODES), TVSeason.EPISODES.FullType);
                    Batched[nameof(TVSeasonViewModel.Cast)] = new RestRequestArgs(new UniformItemIdentifier(Item, TVSeason.CAST), TVSeason.CAST.FullType);
                    Batched[nameof(TVSeasonViewModel.Crew)] = new RestRequestArgs(new UniformItemIdentifier(Item, TVSeason.CREW), TVSeason.CREW.FullType);
                }

                Batched[propertyName] = new RestRequestArgs(new UniformItemIdentifier(Item, property), property.FullType);
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
            return TryGetValue(GetValue<T>(property), out value, propertyName);
        }

        private async Task<T> GetValue<T>(Property property)
        {
            //return DataService.Instance.ResourceCache.ReadAsync(new UniformItemIdentifier(Item, property)).Result.TryGetRepresentation<T>(out var temp) ? temp : default;
            //var request = new RestRequestArgs<T>();
            var request = await Controller.TryGet<T>(new UniformItemIdentifier(Item, property));
            return request.IsHandled ? request.Value : default;
        }

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
