using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Movies.Views
{
    public class CarouselViewPatch : Behavior<CarouselView>
    {
        public static readonly CarouselViewPatch Instance = new CarouselViewPatch();
        public int Test { get; } = 0;

        private class Items : ObservableCollection<object>
        {
            private readonly CarouselView Carousel;
            private INotifyCollectionChanged Original;

            private bool SkipNext;

            public Items(CarouselView carousel, INotifyCollectionChanged observable)
            {
                Carousel = carousel;
                Original = observable;

                observable.CollectionChanged += (sender, e) =>
                {
                    if (e.NewItems != null)
                    {
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            Insert(e.NewStartingIndex + i, e.NewStartingIndex == 0 ? e.NewItems[0] : e.NewStartingIndex);
                        }
                    }

                    SkipNext = true;
                };

                Carousel.PositionChanged += PositionChanged;
            }

            private void PositionChanged(object sender, PositionChangedEventArgs e)
            {
                Print.Log("position changed to " + e.CurrentPosition, SkipNext);
                if (SkipNext)
                {
                    SkipNext = false;
                    //return;
                }
                if (!(Original is IList list))
                {
                    return;
                }

                Carousel.PositionChanged -= PositionChanged;

                if (this[e.CurrentPosition] != list[e.CurrentPosition])
                {
                    this[e.CurrentPosition] = list[e.CurrentPosition];
                }

                Carousel.PositionChanged += PositionChanged;
            }
        }

        protected override void OnAttachedTo(CarouselView bindable)
        {
            base.OnAttachedTo(bindable);

            CarouselPropertyChanged(bindable, new PropertyChangedEventArgs(ItemsView.ItemsSourceProperty.PropertyName));
            bindable.PropertyChanged += CarouselPropertyChanged;
        }

        private void CarouselPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CarouselView carousel = (CarouselView)sender;

            if (e.PropertyName == ItemsView.ItemsSourceProperty.PropertyName)
            {
                if (carousel.ItemsSource is INotifyCollectionChanged observable && !(observable is Items))
                {
                    carousel.ItemsSource = new Items(carousel, observable);
                    return;

                    observable.CollectionChanged += (sender, e) =>
                    {
                        if (carousel.ItemsSource is IList list && list.Count > 1)
                            carousel.ItemsSource = null;
                    };
                    return;

                    carousel.PropertyChanged -= CarouselPropertyChanged;

                    var test = new ObservableCollection<object>(carousel.ItemsSource.OfType<object>());
                    observable.CollectionChanged += ItemsChanged;
                    carousel.ItemsSource = test;

                    carousel.PropertyChanged -= CarouselPropertyChanged;
                }
            }
        }

        private void ItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!(sender is IList list) || !(sender is INotifyCollectionChanged observable))
            {
                return;
            }

            observable.CollectionChanged -= ItemsChanged;

            Print.Log("collection changed", e.NewItems?.Count);
            if (e.NewItems != null)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    if (list.Count == 1)
                    {
                        continue;
                    }

                    list[e.NewStartingIndex + i] = "test";
                }
            }

            observable.CollectionChanged += ItemsChanged;
        }
    }

    public class Carousel : CarouselView
    {
        protected override void OnRemainingItemsThresholdReached()
        {
            base.OnRemainingItemsThresholdReached();
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TVShowInfoPage : ContentPage
    {
        public TVShowInfoPage()
        {
            DataService.Instance.BatchBegin();
            InitializeComponent();
        }

        private void Carousel_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            Print.Log("item changed", e.CurrentItem, e.PreviousItem);
            ;
        }

        private void Carousel_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            CarouselView carousel = (CarouselView)sender;
            Print.Log("position changed", e.CurrentPosition, e.PreviousPosition);
            if (carousel.ItemsSource is System.Collections.IList list && e.CurrentPosition + 1 + carousel.RemainingItemsThreshold >= list.Count)
            {
                carousel.RemainingItemsThresholdReachedCommand?.Execute(carousel.RemainingItemsThresholdReachedCommandParameter);
            }
        }

        private void Carousel_RemainingItemsThresholdReached(object sender, EventArgs e)
        {
            Print.Log("threshold reached");
        }
    }
}