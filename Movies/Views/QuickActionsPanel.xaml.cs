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
    public partial class QuickActionsPanel : HorizontalStackLayout
    {
        public QuickActionsPanel()
        {
            InitializeComponent();
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.CurrentPage.Navigation.PopAsync();
        }
    }
}