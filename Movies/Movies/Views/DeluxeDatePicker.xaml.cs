using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Movies.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeluxeDatePicker : ContentView
    {
        public static readonly BindableProperty DatePickerProperty = BindableProperty.Create(nameof(DatePicker), typeof(DatePicker), typeof(DeluxeDatePicker), propertyChanged: (bindable, oldValue, newValue) =>
        {
            var picker = (DeluxeDatePicker)bindable;

            if (oldValue is DatePicker oldPicker)
            {
                oldPicker.DateSelected -= picker.DateChanged;
            }

            if (newValue is DatePicker newPicker)
            {
                picker.DateChanged(newPicker.Date);
                newPicker.DateSelected += picker.DateChanged;
            }
        });

        public static readonly BindableProperty YearProperty = BindableProperty.Create(nameof(Year), typeof(int), typeof(DeluxeDatePicker), propertyChanged: ComponentChanged);

        public static readonly BindableProperty MonthProperty = BindableProperty.Create(nameof(Month), typeof(int), typeof(DeluxeDatePicker), propertyChanged: ComponentChanged);

        public static readonly BindableProperty DayProperty = BindableProperty.Create(nameof(Day), typeof(int), typeof(DeluxeDatePicker), propertyChanged: ComponentChanged);

        public DatePicker DatePicker
        {
            get => (DatePicker)GetValue(DatePickerProperty);
            set => SetValue(DatePickerProperty, value);
        }

        public int Year
        {
            get => (int)GetValue(YearProperty);
            set => SetValue(YearProperty, value);
        }

        public int Month
        {
            get => (int)GetValue(MonthProperty);
            set => SetValue(MonthProperty, value);
        }

        public int Day
        {
            get => (int)GetValue(DayProperty);
            set => SetValue(DayProperty, value);
        }

        public DeluxeDatePicker()
        {
            InitializeComponent();
            
            SetBinding(DatePickerProperty, new Binding(nameof(Content), BindingMode.TwoWay, source: this));
        }

        private static void ComponentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var picker = (DeluxeDatePicker)bindable;
            picker.ComponentChanged();
        }

        private void ComponentChanged()
        {
            if (!Batch && DatePicker != null)
            {
                DatePicker.Date = new DateTime(Year, Month, Day);
            }
        }

        private bool Batch;

        private void DateChanged(object sender, DateChangedEventArgs e) => DateChanged(e.NewDate);

        private void DateChanged(DateTime date)
        {
            Batch = true;

            Year = date.Year;
            Month = date.Month;
            Day = date.Day;

            Batch = false;
            ComponentChanged();
        }
    }
}