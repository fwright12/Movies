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
    public partial class InfoPageResources : ResourceDictionary
    {
        public static readonly Type RatingEnumerableType = typeof(IEnumerable<Models.Rating>);
        public static readonly Type WatchProviderEnumerableType = typeof(IEnumerable<ViewModels.Group<Models.WatchProvider>>);
        public static readonly Type CreditEnumerableType = typeof(IEnumerable<ViewModels.Group<Models.Credit>>);
        public static readonly Type CompanyEnumerableType = typeof(IEnumerable<Models.Company>);

        public InfoPageResources()
        {
            InitializeComponent();
        }
    }
}