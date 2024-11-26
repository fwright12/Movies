using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CollectionItemView : Frame
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(CollectionItemView));

        public static readonly BindableProperty ThumbnailTemplateProperty = BindableProperty.Create(nameof(ThumbnailTemplate), typeof(DataTemplate), typeof(CollectionItemView), propertyChanged: (bindable, oldValue, newValue) =>
        {
            CollectionItemView view = (CollectionItemView)bindable;
            DataTemplate template = (DataTemplate)newValue;

            View content = (View)template.CreateContent();
            content.SetBinding(BindingContextProperty, new Binding(nameof(BindingContext), source: view));

            view.ThumbnailView = content;
            view.OnPropertyChanged(nameof(ThumbnailView));
        });//, propertyChanged: (bindable, oldValue, newValue) => ((CollectionItemView)bindable).UpdateThumbnail());

        public static readonly BindableProperty DetailPageTemplateProperty = BindableProperty.Create(nameof(DetailPageTemplate), typeof(DataTemplate), typeof(CollectionItemView));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public DataTemplate ThumbnailTemplate
        {
            get => (DataTemplate)GetValue(ThumbnailTemplateProperty);
            set => SetValue(ThumbnailTemplateProperty, value);
        }

        public View ThumbnailView { get; private set; }

        public DataTemplate DetailPageTemplate
        {
            get => (DataTemplate)GetValue(DetailPageTemplateProperty);
            set => SetValue(DetailPageTemplateProperty, value);
        }

        //private ContentView ThumbnailView;

        public CollectionItemView()
        {
            InitializeComponent();
        }

        /*protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("thumbnail") is ContentView contentView && contentView != ThumbnailView)
            {
                ThumbnailView = contentView;
                UpdateThumbnail();
            }
        }

        private void UpdateThumbnail()
        {
            if (ThumbnailView != null && ThumbnailTemplate?.CreateContent() is View content)
            {
                ThumbnailView.Content = content;
            }
        }*/
    }
}