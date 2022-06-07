﻿using System;
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

        public static readonly DataService Data = new DataService();
        protected PropertyDictionary Properties;
        protected DataManager DataManager;

        public ItemViewModel(DataManager dataManager, Item item)
        {
            DataManager = dataManager;
            Item = item;
            if (Item != null)
            {
                Properties = Data.GetDetails(Item);
            }

            AddToListCommand = new Command<IEnumerable<object>>(async lists =>
            {
                await Task.WhenAll(lists.OfType<ListViewModel>().Select(list => Item != null && ((list.Item as List)?.AllowedTypes ?? ItemType.All).HasFlag(Item.ItemType) ? list.Add(Item) : Task.CompletedTask));
            });
        }

        protected T RequestValue<T>(Property<T> property, [CallerMemberName] string propertyName = null) => GetValue(Properties.GetSingle(property), propertyName);
        protected IEnumerable<T> RequestValue<T>(MultiProperty<T> property, [CallerMemberName] string propertyName = null) => GetValue(Properties.GetMultiple(property), propertyName);
        protected TValue RequestSingle<TItem, TValue>(InfoRequestHandler<TItem, TValue> handler, [CallerMemberName] string property = null) where TItem : Item => Item is TItem item ? GetValue(handler.GetSingle(item), property) : default;

        public override string ToString() => Item?.ToString() ?? base.ToString();
    }
}
