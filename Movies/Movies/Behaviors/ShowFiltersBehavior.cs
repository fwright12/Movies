using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Movies.Views
{
    public class TotalHeightConverter : IMultiValueConverter
    {
        public static readonly TotalHeightConverter Instance = new TotalHeightConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double height) {

                if (values[1] is double margin)
                {
                    height -= margin;
                }

                if (values[2] is double padding)
                {
                    height -= padding;
                }

                return height;
            }

            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShowFiltersBehavior : Behavior<DrawerView>
    {
        public CollectionView CollectionView
        {
            get => _CollectionView;
            set
            {
                if (value != _CollectionView)
                {
                    if (_CollectionView != null)
                    {
                        _CollectionView.Scrolled -= ListScrolled;
                    }

                    ListScrolled(_CollectionView = value);

                    if (_CollectionView != null)
                    {
                        _CollectionView.Scrolled += ListScrolled;
                        _CollectionView.PropertyChanged += HeaderChanged;
                    }
                }
            }
        }

        private CollectionView _CollectionView;

        protected override void OnAttachedTo(DrawerView bindable)
        {
            base.OnAttachedTo(bindable);

            //bindable.DescendantAdded += CollectionViewAdded;
            bindable.SizeChanged += ListScrolled;
            bindable.PropertyChanged += DrawerContentChanged;
        }

        private void DrawerContentChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DrawerView.DrawerContentView))
            {
                return;
            }

            var drawer = (DrawerView)sender;

            if (drawer.DrawerContentView != null)
            {
                drawer.DrawerContentView.SizeChanged += ListScrolled;

                ListScrolled(CollectionView);
            }
        }

        private void HeaderChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != StructuredItemsView.HeaderProperty.PropertyName)
            {
                return;
            }

            var collectionView = (CollectionView)sender;

            if (collectionView.Header is Layout layout)
            {
                layout.LayoutChanged += ListScrolled;
            }
            else if (collectionView.Header is View view)
            {
                view.SizeChanged += ListScrolled;
            }
        }

        private void CollectionViewAdded(object sender, ElementEventArgs e)
        {
            if (!(e.Element is CollectionView bindable))
            {
                return;
            }

            ((Element)sender).DescendantAdded -= CollectionViewAdded;
            bindable.Scrolled += ListScrolled;
        }

        protected override void OnDetachingFrom(DrawerView bindable)
        {
            base.OnDetachingFrom(bindable);

            //bindable.Scrolled -= ListScrolled;
        }

        private void ListScrolled(object sender, EventArgs e) => ListScrolled(CollectionView);
        private void ListScrolled(object sender, ItemsViewScrolledEventArgs e) => ListScrolled((CollectionView)sender, e.VerticalOffset, e.VerticalDelta);

        private void ListScrolled(CollectionView collectionView) => ListScrolled(collectionView, 0, 0);
        private void ListScrolled(CollectionView collectionView, double verticalOffset, double verticalDelta)
        {
            if (collectionView?.Parent<DrawerView>() is DrawerView drawerView && drawerView.DrawerContentView != null)
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

                if (verticalOffset > height)
                {
                    drawerView.DrawerContentView.IsVisible = verticalDelta < 0;
                }
                else
                {
                    double drawerHeight = 0;
                    drawerHeight = drawerView.SnapPoints.Count > 0 ? drawerView.SnapPoints.Min(snapPoint => snapPoint.Value) : drawerView.Height;

                    drawerView.DrawerContentView.IsVisible = verticalOffset > height + drawerHeight - drawerView.Height;
                }

                if (verticalOffset > height)
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
