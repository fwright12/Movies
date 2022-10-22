using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Movies.Views
{
    public class ShowFiltersBehavior : Behavior<CollectionView>
    {
        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);

            ListScrolled(bindable, new ItemsViewScrolledEventArgs
            {
                VerticalOffset = 0
            });
            bindable.Scrolled += ListScrolled;
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            base.OnDetachingFrom(bindable);

            bindable.Scrolled -= ListScrolled;
        }

        private void ListScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            CollectionView collectionView = (CollectionView)sender;

            if (collectionView.Parent?.Parent is DrawerView drawerView && drawerView.DrawerContentView != null)
            {
                double height = 0;

                if (collectionView.Header is View header)
                {
                    height = header.Height - header.Margin.VerticalThickness;

                    if (header is Layout<View> layout)
                    {
                        height -= layout.Padding.VerticalThickness;

                        if (layout.Children.Count > 0)
                        {
                            //height -= layout.Children[layout.Children.Count - 1].Height;

                            //Print.Log(e.VerticalOffset, header.Height, layout.Children[layout.Children.Count - 1].Height);
                        }
                    }
                }

                //Print.Log(e.VerticalDelta, e.VerticalOffset, height);
                //drawerView.DrawerContentView.IsVisible = e.VerticalOffset > height - collectionView.Height && e.VerticalDelta < 0;

                if (e.VerticalOffset > height)
                {
                    drawerView.DrawerContentView.IsVisible = e.VerticalDelta < 0;
                }
                else
                {
                    double drawerHeight = 0;

                    if (collectionView.Parent<DrawerView>() is DrawerView drawer)
                    {
                        drawerHeight = drawer.SnapPoints.Count > 0 ? drawer.SnapPoints.Min(snapPoint => snapPoint.Value) : drawer.Height;
                    }

                    drawerView.DrawerContentView.IsVisible = e.VerticalOffset > height + drawerHeight - collectionView.Height;
                }

                if (e.VerticalOffset > height)
                {

                    var view = collectionView.Parent as Layout;
                    //view.Padding = new Thickness(view.Padding.Left, view.Padding.Top, view.Padding.Right, drawerView.DrawerContentView.IsVisible ? drawerView.DrawerContentView.Height : 0);
                    //Print.Log(drawerView.DrawerContentView.IsVisible, drawerView.DrawerContentView.Height);
                    //drawerView.DrawerContentView.IsVisible = (e.VerticalOffset <= 0 && height == 0) || (e.VerticalDelta < 0 && e.VerticalOffset >= height);
                }
                //Print.Log(drawerView.DrawerContentView.IsVisible, e.VerticalOffset, height);
            }

            //string state = e.FirstVisibleItemIndex > 0 && e.LastVisibleItemIndex < ((collectionView.ItemsSource as System.Collections.IList)?.Count ?? -1) ? "Centered" : "Normal";
            //Print.Log("list scrolled", e.VerticalOffset, (collectionView.Header as View)?.Height, collectionView.Parent<AbsoluteLayout>()?.Children[1].Height);
            //string state = e.VerticalOffset < ((collectionView.Header as View)?.Height ?? 0) ? "Hidden" : "Showing";// + (collectionView.Parent<AbsoluteLayout>()?.Children[1].Height ?? 0);

            //VisualStateManager.GoToState(collectionView.Parent as VisualElement, state);
        }
    }
}
