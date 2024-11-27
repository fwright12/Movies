using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Movies
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    //[Activity(Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
              DataScheme = App.OAUTH_SCHEME,
              DataHost = App.OAUTH_HOST,
              AutoVerify = true)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            //TabLayoutResource = Resource.Layout.Tabbar;
            //ToolbarResource = Resource.Layout.Toolbar;
            //var app = Xamarin.Forms.Application.Current;

            base.OnCreate(savedInstanceState);

            //Android.Gms.Ads.MobileAds.Initialize(ApplicationContext);

            //global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            //FFImageLoading.Forms.Platform.CachedImageRenderer.Init(true);
            //FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageViewHandler();

            //Firebase.InitializeApp(this);
            //AndroidAppLinks.Init(this);

            JavaScriptEvaluationService.Register(new JavaScriptEvaluatorFactory(ApplicationContext));
            //LoadApplication(app as App ?? new App());
        }
    }
}
