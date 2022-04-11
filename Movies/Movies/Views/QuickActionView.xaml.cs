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
    public partial class QuickActionView : LabeledContentView
    {
        public static readonly BindableProperty ToggleListMemberProperty = BindableProperty.CreateAttached("ToggleListMember", typeof(ICommand), typeof(Button), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var button = (Button)bindable;

            if (oldValue is ICommand oldCommand)
            {
                //oldEdit.AddCommand.CanExecuteChanged -= UpdateToggled;
            }

            if (newValue is ICommand newCommand)
            {
                newCommand.CanExecuteChanged += (sender, e) => UpdateState(newCommand, button);
                button.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == Button.CommandParameterProperty.PropertyName)
                    {
                        UpdateState(newCommand, (Button)sender);
                    }
                };

                UpdateState(newCommand, button);
            }
        });

        private static void UpdateState(ICommand command, Button button)
        {
            VisualStateManager.GoToState(button, command.CanExecute(button.CommandParameter) ? "Toggled" : "Untoggled");
        }

        public static ICommand GetToggleListMember(Button button) => (ICommand)button.GetValue(ToggleListMemberProperty);
        public static void SetToggleListMember(Button button, ICommand value) => button.SetValue(ToggleListMemberProperty, value);

        public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(QuickActionView));

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}