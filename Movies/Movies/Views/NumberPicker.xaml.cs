using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Movies.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NumberPicker : ContentView
    {
        public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(NumberPicker), defaultBindingMode: BindingMode.TwoWay, coerceValue: (bindable, value) =>
        {
            var picker = (NumberPicker)bindable;
            var coerced = Math.Round((double)value / picker.Step) * picker.Step;
            
            if (picker.AbsoluteMin.HasValue)
            {
                coerced = Math.Max(picker.AbsoluteMin.Value, coerced);
            }
            if (picker.AbsoluteMax.HasValue)
            {
                coerced = Math.Min(picker.AbsoluteMax.Value, coerced);
            }

            if (coerced > picker.Upper)
            {
                picker.Upper = coerced;
            }
            else if (coerced < picker.Lower)
            {
                picker.Lower = coerced;
            }

            return coerced;
        });

        public static readonly BindableProperty LowerProperty = BindableProperty.Create(nameof(Lower), typeof(double), typeof(NumberPicker), Slider.MinimumProperty.DefaultValue, coerceValue: (bindable, value) =>
        {
            var result = (double)value;
            var constraint = ((NumberPicker)bindable).Upper;
            
            return result >= constraint ? constraint - 1 : result;
        });

        public static readonly BindableProperty UpperProperty = BindableProperty.Create(nameof(Upper), typeof(double), typeof(NumberPicker), Slider.MaximumProperty.DefaultValue, coerceValue: (bindable, value) =>
        {
            var result = (double)value;
            var constraint = ((NumberPicker)bindable).Lower;

            return result <= constraint ? constraint + 1 : result;
        });

        public static readonly BindableProperty AbsoluteMinProperty = BindableProperty.Create(nameof(AbsoluteMin), typeof(double?), typeof(NumberPicker), null, coerceValue: (bindable, value) =>
        {
            var result = (double?)value;
            var constraint = ((NumberPicker)bindable).AbsoluteMax;

            return result >= constraint ? constraint - 1 : result;
        });

        public static readonly BindableProperty AbsoluteMaxProperty = BindableProperty.Create(nameof(AbsoluteMax), typeof(double?), typeof(NumberPicker), null, coerceValue: (bindable, value) =>
        {
            var result = (double?)value;
            var constraint = ((NumberPicker)bindable).AbsoluteMin;

            return result <= constraint ? constraint + 1 : result;
        });

        public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(double), typeof(NumberPicker), 0.1);

        public static readonly BindableProperty DragStartedCommandProperty = BindableProperty.Create(nameof(DragStartedCommand), typeof(ICommand), typeof(NumberPicker));

        public static readonly BindableProperty DragCompletedCommandProperty = BindableProperty.Create(nameof(DragCompletedCommand), typeof(ICommand), typeof(NumberPicker));

        public ICommand StepUpCommand { get; }
        public ICommand StepDownCommand { get; }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Lower
        {
            get => (double)GetValue(LowerProperty);
            set => SetValue(LowerProperty, value);
        }

        public double Upper
        {
            get => (double)GetValue(UpperProperty);
            set => SetValue(UpperProperty, value);
        }

        public double? AbsoluteMin
        {
            get => (double?)GetValue(AbsoluteMinProperty);
            set => SetValue(AbsoluteMinProperty, value);
        }

        public double? AbsoluteMax
        {
            get => (double?)GetValue(AbsoluteMaxProperty);
            set => SetValue(AbsoluteMaxProperty, value);
        }

        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public ICommand DragStartedCommand
        {
            get => (ICommand)GetValue(DragStartedCommandProperty);
            set => SetValue(DragStartedCommandProperty, value);
        }

        public ICommand DragCompletedCommand
        {
            get => (ICommand)GetValue(DragCompletedCommandProperty);
            set => SetValue(DragCompletedCommandProperty, value);
        }

        public NumberPicker()
        {
            StepUpCommand = new Command(StepUp);
            StepDownCommand = new Command(StepDown);

            InitializeComponent();
        }

        public void StepUp() => Value += Step;
        public void StepDown() => Value -= Step;
    }
}