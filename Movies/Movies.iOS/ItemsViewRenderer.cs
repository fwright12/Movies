﻿using Movies.Views;
using System;
using System.ComponentModel;
using System.Linq;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

//[assembly: ExportRenderer(typeof(Page), typeof(Movies.iOS.NavigationPageRenderer))]
//[assembly: ExportRenderer(typeof(ScrollView), typeof(Movies.iOS.ScrollViewRenderer))]
//[assembly: ExportRenderer(typeof(PageRenderer), typeof(Movies.iOS.PageRenderer))]
//[assembly: ExportRenderer(typeof(Shell), typeof(Movies.iOS.Test))]
[assembly: ExportRenderer(typeof(CollectionView), typeof(Movies.iOS.CollectionViewRenderer))]
[assembly: ExportRenderer(typeof(CarouselView), typeof(Movies.iOS.CarouselViewRenderer))]
[assembly: ResolutionGroupName("Movies")]
[assembly: ExportEffect(typeof(Movies.iOS.FixiOSCollectionViewScrollsToTopPlatformEffect), nameof(Movies.iOS.FixiOSCollectionViewScrollsToTopPlatformEffect))]
namespace Movies.iOS
{
    public class FixiOSCollectionViewScrollsToTopPlatformEffect : PlatformEffect
    {
        private UICollectionView _uiCollectionView;
        public FixiOSCollectionViewScrollsToTopPlatformEffect()
        { }

        protected override void OnAttached()
        {
            _uiCollectionView = Control.Subviews.FirstOrDefault() as UICollectionView;

            if (_uiCollectionView == null)
            {
                return;
            }

            Element.PropertyChanged += ElementPropertyChanged;
        }

        protected override void OnDetached()
        {
            Element.PropertyChanged -= ElementPropertyChanged;
        }

        private void ElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //When the Height is defined, the complete View Hierarchy is created
            if (e.PropertyName == VisualElement.HeightProperty.PropertyName)
            {
                //Get the top parent View
                var superView = _uiCollectionView.Superview;

                while (superView.Superview != null)
                {
                    superView = superView.Superview;
                }

                EnsureScrollsToTop(superView, _uiCollectionView);
            }
        }

        private void EnsureScrollsToTop(UIView topMostView, UICollectionView uiCollectionViewWithScrollToTop)
        {
            foreach (var subview in topMostView.Subviews)
            {
                if (subview is UIScrollView uiScrollView)
                {
                    uiScrollView.ScrollsToTop = uiScrollView == uiCollectionViewWithScrollToTop;
                }

                if (subview.Subviews.Any())
                {
                    EnsureScrollsToTop(subview, uiCollectionViewWithScrollToTop);
                }
            }
        }
    }

    public class EntryRenderer : Xamarin.Forms.Platform.iOS.EntryRenderer
    {
        public EntryRenderer()
        {
            
        }
    }

    public static class Extensions
    {
        public static void UpdatePadding(ItemsView items, UICollectionView view)
        {
            Thickness padding = items.GetPadding();

            view.ContentInset = new UIEdgeInsets(view.ContentInset.Top + (nfloat)padding.Top, view.ContentInset.Left + (nfloat)padding.Left, view.ContentInset.Bottom + (nfloat)padding.Bottom, view.ContentInset.Right + (nfloat)padding.Right);
        }
    }

    public class ScrollViewRenderer : Xamarin.Forms.Platform.iOS.ScrollViewRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                //ElementPropertyChanged(e.NewElement, new PropertyChangedEventArgs(Layout.PaddingProperty.PropertyName));
                //e.NewElement.PropertyChanged += ElementPropertyChanged;
            }

            //ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
            //ContentInset = new UIEdgeInsets(250, 0, 0, 0);
        }

        private void ElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var scrollView = (ScrollView)sender;
            if (e.PropertyName == Layout.PaddingProperty.PropertyName && scrollView.IsSet(Layout.PaddingProperty))
            {
                var padding = scrollView.Padding;
                System.Diagnostics.Debug.WriteLine("setting padding, " + (padding.Bottom - padding.Top));
                //ContentInset = new UIEdgeInsets(0, 0, (nfloat)(padding.Bottom - padding.Top), 0);
                ContentInset = new UIEdgeInsets((nfloat)padding.Top, (nfloat)padding.Left, (nfloat)padding.Bottom, (nfloat)padding.Right);
                scrollView.ClearValue(Layout.PaddingProperty);
            }
        }
    }

    public class NavigationPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (NavigationController != null)
            NavigationController.HidesBarsOnSwipe = true;
            //new UINavigationBar().ScrollEdgeAppearance = new UINavigationBarAppearance();
        }
    }

    public class CollectionViewRenderer : Xamarin.Forms.Platform.iOS.CollectionViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<GroupableItemsView> e)
        {
            base.OnElementChanged(e);
            
            if (e.NewElement != null)
            {
                Extensions.UpdatePadding(e.NewElement, Controller.CollectionView);
            }

            if (Controller?.CollectionView != null)
            {
                Controller.CollectionView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;
                Controller.CollectionView.ScrollsToTop = true;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs changedProperty)
        {
            base.OnElementPropertyChanged(sender, changedProperty);

            if (changedProperty.PropertyName == Views.Extensions.PaddingProperty.PropertyName)
            {
                Extensions.UpdatePadding((ItemsView)sender, Controller.CollectionView);
            }
        }
    }

    public class CarouselViewRenderer : Xamarin.Forms.Platform.iOS.CarouselViewRenderer
    {
        protected override CarouselViewController CreateController(CarouselView newElement, ItemsViewLayout layout)
        {
            var controller = base.CreateController(newElement, layout);

            if (controller?.CollectionView != null)
            {
                controller.CollectionView.DecelerationRate = UIScrollView.DecelerationRateFast;
            }

            return controller;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CarouselView> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                Extensions.UpdatePadding(e.NewElement, Controller.CollectionView);
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs changedProperty)
        {
            base.OnElementPropertyChanged(sender, changedProperty);

            if (changedProperty.PropertyName == Views.Extensions.PaddingProperty.PropertyName)
            {
                Extensions.UpdatePadding((ItemsView)sender, Controller.CollectionView);
            }
        }
    }
}