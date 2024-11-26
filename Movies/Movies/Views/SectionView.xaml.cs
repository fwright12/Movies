using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SectionView : Frame
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(SectionView));

        public static readonly BindableProperty HeaderProperty = BindableProperty.CreateAttached("Header", typeof(object), typeof(VisualElement), null, propertyChanged: (bindable, oldValue, newValue) => UpdateViewProperty(bindable, HeaderViewPropertyKey, newValue));

        private static readonly BindablePropertyKey HeaderViewPropertyKey = BindableProperty.CreateAttachedReadOnly("HeaderView", typeof(object), typeof(VisualElement), null);

        public static readonly BindableProperty HeaderViewProperty = HeaderViewPropertyKey.BindableProperty;

        public static readonly BindableProperty FooterProperty = BindableProperty.CreateAttached("Footer", typeof(object), typeof(VisualElement), null, propertyChanged: (bindable, oldValue, newValue) => UpdateViewProperty(bindable, FooterViewPropertyKey, newValue));

        private static readonly BindablePropertyKey FooterViewPropertyKey = BindableProperty.CreateAttachedReadOnly("FooterView", typeof(object), typeof(VisualElement), null);

        public static readonly BindableProperty FooterViewProperty = FooterViewPropertyKey.BindableProperty;

        public static object GetHeader(VisualElement visualElement) => visualElement.GetValue(HeaderProperty);
        public static object GetFooter(VisualElement visualElement) => visualElement.GetValue(FooterProperty);
        public static View GetHeaderView(VisualElement visualElement) => GetView(visualElement, HeaderViewProperty);
        public static View GetFooterView(VisualElement visualElement) => GetView(visualElement, FooterViewProperty);

        public static void SetHeader(VisualElement visualElement, object value) => visualElement.SetValue(HeaderProperty, value);
        public static void SetFooter(VisualElement visualElement, object value) => visualElement.SetValue(FooterProperty, value);

        private static View GetView(VisualElement visualElement, BindableProperty property)
        {
            object view = visualElement.GetValue(property);
            return view as View ?? (view as Lazy<View>)?.Value;
        }

        private static void UpdateViewProperty(BindableObject bindable, BindablePropertyKey propertyKey, object newValue)
        {
            bindable.SetValue(propertyKey, new Lazy<View>(() =>
            {
                View view = (View)new ToViewConverter().Convert(newValue, typeof(View), null, null);
                view.SetBinding(BindingContextProperty, new Binding(BindingContextProperty.PropertyName, source: bindable));

                return view;
            }));
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public View HeaderView => GetHeaderView(this);

        public View FooterView => GetFooterView(this);

        public SectionView()
        {
            InitializeComponent();
        }
    }
}