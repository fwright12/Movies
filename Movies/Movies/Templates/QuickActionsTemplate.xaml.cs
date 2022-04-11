using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Movies.Templates
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class QuickActionsTemplate : ControlTemplate
    {
        public QuickActionsTemplate()
        {
            InitializeComponent();
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.CurrentPage.Navigation.PopAsync();
        }
    }
}