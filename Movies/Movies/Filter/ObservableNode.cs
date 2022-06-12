﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Movies.ViewModels
{
    public class ObservableList<T> : ObservableCollection<T>, IList<object>
    {
        object IList<object>.this[int index]
        {
            get => ((IList)this)[index];
            set => ((IList)this)[index] = value;
        }

        bool ICollection<object>.IsReadOnly => ((IList)this).IsReadOnly;

        void ICollection<object>.Add(object item) => ((IList)this).Add(item);

        bool ICollection<object>.Contains(object item) => ((IList)this).Contains(item);

        void ICollection<object>.CopyTo(object[] array, int arrayIndex) => ((IList)this).CopyTo(array, arrayIndex);

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => this.OfType<object>().GetEnumerator();

        int IList<object>.IndexOf(object item) => ((IList)this).IndexOf(item);

        void IList<object>.Insert(int index, object item) => ((IList)this).Insert(index, item);

        bool ICollection<object>.Remove(object item) => item is T t ? Remove(t) : false;
    }

    public sealed class ObservableNode<T>
    {
        public event NotifyCollectionChangedEventHandler SubtreeChanged;

        public T Value { get; }

        public ObservableNode<T> Parent { get; private set; }
        public ObservableList<ObservableNode<T>> Children { get; }

        public ObservableNode(T value)
        {
            Children = new ObservableList<ObservableNode<T>>();
            Children.CollectionChanged += AssignParent;

            Value = value;
        }

        public void Add(ObservableNode<T> subtree)
        {
            Children.Add(subtree);
        }

        public void Add(T value)
        {
            Children.Add(new ObservableNode<T>(value));
        }

        public bool Remove(T value)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Equals(Children[i].Value, value))
                {
                    Children.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        protected void OnSubtreeChanged(ObservableNode<T> changed)
        {
            //SubtreeChanged?.Invoke(this, new EventArgs<ObservableTree<T>>(changed));
        }

        private void AssignParent(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var node in e.OldItems.OfType<ObservableNode<T>>())
                {
                    //node.SubtreeChanged -= SubtreeChanged;
                    node.Parent = null;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var node in e.NewItems.OfType<ObservableNode<T>>())
                {
                    node.Parent = this;
                    //node.SubtreeChanged += SubtreeChanged;
                }
            }

            var parent = this;
            do
            {
                parent.SubtreeChanged?.Invoke(this, e);
                parent = parent.Parent;
            }
            while (parent != null);
        }

        //private void ChildSubtreeChanged(object sender, EventArgs<ObservableTree<T>> e) => OnSubtreeChanged(e.Value);
    }
}