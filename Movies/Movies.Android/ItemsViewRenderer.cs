using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Movies.Views;

[assembly: ExportRenderer (typeof(CollectionView), typeof(Movies.Droid.CollectionViewRenderer))]
[assembly: ExportRenderer (typeof(CarouselView), typeof(Movies.Droid.CarouselViewRenderer))]
namespace Movies.Droid
{
    public static class Extensions
    {
        public static void UpdatePadding(ItemsView items, ViewGroup view)
        {
            Thickness padding = items.GetPadding();
            var context = view.Context;
            view.SetPadding((int)context.ToPixels(padding.Left), (int)context.ToPixels(padding.Top), (int)context.ToPixels(padding.Right), (int)context.ToPixels(padding.Bottom));
            view.SetClipToPadding(false);
        }
    }

    public class CollectionViewRenderer : Xamarin.Forms.Platform.Android.CollectionViewRenderer
    {
        public CollectionViewRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<ItemsView> elementChangedEvent)
        {
            base.OnElementChanged(elementChangedEvent);
            Extensions.UpdatePadding(elementChangedEvent.NewElement, this);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs changedProperty)
        {
            base.OnElementPropertyChanged(sender, changedProperty);
            if (changedProperty.PropertyName == Views.Extensions.PaddingProperty.PropertyName)
            {
                Extensions.UpdatePadding((ItemsView)sender, this);
            }
        }
    }

    public class CarouselViewRenderer : Xamarin.Forms.Platform.Android.CarouselViewRenderer
    {
        public CarouselViewRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<ItemsView> elementChangedEvent)
        {
            base.OnElementChanged(elementChangedEvent);
            Extensions.UpdatePadding(elementChangedEvent.NewElement, this);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs changedProperty)
        {
            base.OnElementPropertyChanged(sender, changedProperty);
            if (changedProperty.PropertyName == Views.Extensions.PaddingProperty.PropertyName)
            {
                Extensions.UpdatePadding((ItemsView)sender, this);
            }
        }
    }
}