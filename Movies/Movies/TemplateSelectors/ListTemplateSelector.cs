using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies
{
    public class RatingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ExternalRatingTemplate { get; set; }
        public DataTemplate PersonalRatingTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Models.Rating rating = (Models.Rating)item;
            return rating.Company.Name == "Me" ? PersonalRatingTemplate : ExternalRatingTemplate;
        }
    }

    public class AspectContentView : ContentView
    {
        public DataTemplateSelector ContentTemplate { get; set; }

        public double Ratio { get; set; }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            Content = ContentTemplate.SelectTemplate(BindingContext, this).CreateContent() as View;
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            SizeRequest request = base.OnMeasure(widthConstraint, heightConstraint);

            if (!double.IsInfinity(widthConstraint) && double.IsInfinity(heightConstraint))
            {
                request.Request = new Size(widthConstraint, widthConstraint * Ratio);
            }
            else if (!double.IsInfinity(heightConstraint) && double.IsInfinity(widthConstraint))
            {
                request.Request = new Size(heightConstraint / Ratio, heightConstraint);
            }

            return request;
        }
    }

    public static class CollectionViewExtensions
    {
        public static readonly BindableProperty ItemTappedCommandProperty = BindableProperty.CreateAttached("ItemTappedCommand", typeof(ICommand), typeof(CollectionView), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            CollectionView collectionView = (CollectionView)bindable;

            if (newValue == null)
            {
                collectionView.PropertyChanged -= ReplaceItemTemplate;
            }
            else if (oldValue == null)
            {
                collectionView.PropertyChanged += ReplaceItemTemplate;
            }
        });

        private static void ReplaceItemTemplate(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != CollectionView.ItemTemplateProperty.PropertyName)
            {
                return;
            }

            CollectionView collectionView = (CollectionView)sender;


        }

        public static ICommand GetItemTappedCommand(CollectionView collectionView) => (ICommand)collectionView.GetValue(ItemTappedCommandProperty);

        public static void SetItemTappedCommand(CollectionView collectionView, ICommand command) => collectionView.SetValue(ItemTappedCommandProperty, command);
    }

    public class ItemTappedTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ContentTemplate { get; set; }
        public DataTemplateSelector PushPageTemplateSelector { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return new DataTemplate(() =>
            {
                object content = (ContentTemplate as DataTemplateSelector)?.SelectTemplate(item, container).CreateContent() ?? ContentTemplate.CreateContent();

                if (content is View view && PushPageTemplateSelector != null)
                {
                    view.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Views.PushPageCommand
                        {
                            PageTemplate = PushPageTemplateSelector.SelectTemplate(item, container)
                        },
                        CommandParameter = item
                    });
                }

                return content;
            });
        }
    }

    public class ListTemplateSelector : TypeTemplateSelector
    {
        public DataTemplateSelector OpenPageTemplateSelector { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            DataTemplate template = base.OnSelectTemplate(item, container) ?? new DataTemplate(typeof(Label));

            return OpenPageTemplateSelector == null ? template : new DataTemplate(() =>
            {
                object content = template.CreateContent();

                if (content is View view)
                {
                    view.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Views.PushPageCommand
                        {
                            PageTemplate = OpenPageTemplateSelector.SelectTemplate(item, container)
                        },
                        CommandParameter = item// item is ViewModels.TVShowViewModel show ? new ViewModels.TVSeriesViewModel(App.DataManager, (Models.TVShow)show.Item) : item
                    });
                }

                return content;
            });
        }
    }
}
