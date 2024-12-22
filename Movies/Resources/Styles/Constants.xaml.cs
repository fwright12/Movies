namespace Movies
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Constants : ResourceDictionary
    {
        public const double TwoThirds = 2d / 3d;

        public Constants()
        {
            InitializeComponent();
        }
    }
}