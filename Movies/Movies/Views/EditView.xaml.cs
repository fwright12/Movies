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
    public static class EditView1
    {
        public static BindableProperty IsEditingProperty = BindableProperty.CreateAttached("IsEditing", typeof(bool), typeof(ContentView), false, propertyChanged: (bindable, oldValue, newValue) =>
        {
            ContentView contentView = (ContentView)bindable;
            UpdateContent(contentView, (bool)newValue);
            

            //bindable.RemoveBinding((bool)oldValue ? EditingProperty : DisplayProperty);
            //UpdateContent(bindable, (bool)newValue);
        }, defaultValueCreator: bindable =>
        {
            //UpdateContent(bindable, false);
            return false;
        });

        private static void UpdateContent(ContentView contentView, bool? editing = null, object edit = null, object display = null)
        {
            var content = (editing ?? contentView.GetIsEditing()) ? (edit ?? contentView.GetEdit()) : (display ?? contentView.GetDisplay());

            contentView.ClearValue(Microsoft.Maui.Controls.Extensions.ContentView.ContentTemplateProperty);
            contentView.ClearValue(ContentView.ContentProperty);
            if (content is ElementTemplate template)
            {
                Microsoft.Maui.Controls.Extensions.ContentView.SetContentTemplate(contentView, template);
            }
            else if (content is View view)
            {
                contentView.Content = view;
            }
            else
            {
                contentView.Content = new Label { Text = content.ToString() };
            }
        }

        //private static void UpdateContent(BindableObject bindable, bool editing) => bindable.SetBinding(editing ? EditingProperty : DisplayProperty, new Binding(ContentView.ContentProperty.PropertyName, BindingMode.OneWayToSource, source: bindable));

        public static BindableProperty DisplayProperty = BindableProperty.CreateAttached("Display", typeof(object), typeof(ContentView), null, propertyChanged: (bindable, oldValue, newValue) => UpdateContent((ContentView)bindable, display: newValue));

        public static BindableProperty EditProperty = BindableProperty.CreateAttached("Edit", typeof(object), typeof(ContentView), null, propertyChanged: (bindable, oldValue, newValue) => UpdateContent((ContentView)bindable, edit: newValue));

        public static bool GetIsEditing(this ContentView content) => (bool)content.GetValue(IsEditingProperty);
        public static object GetDisplay(this ContentView content) => content.GetValue(DisplayProperty);
        public static object GetEdit(this ContentView content) => content.GetValue(EditProperty);

        public static void SetIsEditing(this ContentView content, bool value) => content.SetValue(IsEditingProperty, value);
        public static void SetDisplay(this ContentView content, object value) => content.SetValue(DisplayProperty, value);
        public static void SetEdit(this ContentView content, object value) => content.SetValue(EditProperty, value);

    }
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditView : ContentView
    {
        /*public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public View DisplayView
        {
            get => (View)GetValue(DisplayViewProperty);
            set => SetValue(DisplayViewProperty, value);
        }

        public View EditingView
        {
            get => (View)GetValue(EditingViewProperty);
            set => SetValue(EditingViewProperty, value);
        }*/

        public EditView()
        {
            InitializeComponent();
        }
    }
}