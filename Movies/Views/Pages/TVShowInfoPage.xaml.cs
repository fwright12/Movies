namespace Movies.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TVShowInfoPage : ContentPage
    {
        public TVShowInfoPage()
        {
            DataService.Instance.BatchBegin();
            InitializeComponent();
        }
    }
}