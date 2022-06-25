using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.ViewModels
{
    public class AsyncListViewModel<T> : BindableViewModel, IEnumerable<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IList<T> Items { get; set; }

        public ICommand LoadMoreCommand { get; }

        private IAsyncEnumerable<T> Source;
        private IAsyncEnumerator<T> Itr;

        public AsyncListViewModel(IAsyncEnumerable<T> source)
        {
            var items = new ObservableCollection<T>();
            items.CollectionChanged += (sender, e) => CollectionChanged?.Invoke(this, e);
            Items = items;

            Source = source;
            LoadMoreCommand = new Command<int?>(count => _ = LoadMore(count ?? 1));

            Refresh();
        }

        public async Task LoadMore(int count = 1)
        {
            if (Itr == null)
            {
                return;
            }

            for (int i = 0; i < count && await Itr.MoveNextAsync(); i++)
            {
                Items.Add(Itr.Current);
            }
        }

        public void Refresh()
        {
            Items.Clear();
            Itr = Source.GetAsyncEnumerator();
            _ = LoadMore(10);
        }

        public static async IAsyncEnumerable<T> Where(IAsyncEnumerable<T> items, Func<T, bool> predicate)
        {
            await foreach (var item in items)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //private void Filter(object sender, EventArgs<IEnumerable<Constraint>> e) => UpdateValues(e.Value);
    }
}