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
    public partial class DetailView : ContentView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(DetailView));

        public static readonly BindableProperty ThumbnailTemplateProperty = BindableProperty.Create(nameof(ThumbnailTemplate), typeof(DataTemplate), typeof(DetailView), propertyChanged: (bindable, oldValue, newValue) =>
        {
            DetailView view = (DetailView)bindable;
            DataTemplate template = (DataTemplate)newValue;

            view.ThumbnailView = (View)template.CreateContent();
            view.OnPropertyChanged(nameof(ThumbnailView));
        });//, propertyChanged: (bindable, oldValue, newValue) => ((CollectionItemView)bindable).UpdateThumbnail());

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

        //private ContentView ThumbnailView;

        public DetailView()
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