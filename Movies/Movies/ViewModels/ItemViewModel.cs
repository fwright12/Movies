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

        protected delegate bool TryGet<T>(out T value);

        protected bool TryRequestValue<T>(Property property, out T value, [CallerMemberName] string propertyName = null)
        {
            var uii = new UniformItemIdentifier(Item, property);
            //var resource = DataService.Locator.Locate(uii);

            return TryGetValue(null, out value);
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

        protected T RequestValue<T>(Property<T> property, [CallerMemberName] string propertyName = null) => TryRequestValue(property, out var value, propertyName) ? value : default;
        protected IEnumerable<T> RequestValue<T>(MultiProperty<T> property, [CallerMemberName] string propertyName = null) => TryRequestValue(property, out var value, propertyName) ? value : default;

        private bool Invalidate(Property property, Task value) => value.Exception?.InnerExceptions.All(exception => exception is System.Net.Http.HttpRequestException) == true && Properties.Invalidate(property, value);

        public override string ToString() => Item?.ToString() ?? base.ToString();
    }
}
