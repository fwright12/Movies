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
    public enum LabelPosition { Left = 0, Top = 1, Right = 2, Bottom = 3 }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LabeledContentView : ContentView
    {
        public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(LabeledContentView));

        public static readonly BindableProperty LabelPositionProperty = BindableProperty.Create(nameof(LabelPosition), typeof(LabelPosition), typeof(LabeledContentView), LabelPosition.Bottom);

        public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(LabeledContentView), StackLayout.SpacingProperty.DefaultValue);

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public LabelPosition LabelPosition
        {
            get => (LabelPosition)GetValue(LabelPositionProperty);
            set => SetValue(LabelPositionProperty, value);
        }

        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public LabeledContentView()
        {
            InitializeComponent();
        }
    }
}