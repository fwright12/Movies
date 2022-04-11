using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Movies
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Constants : ResourceDictionary
    {
        public Constants()
        {
            InitializeComponent();
        }
    }
}