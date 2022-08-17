using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;
using Xamarin.Forms.Platform.Android.AppLinks;
using Firebase;

namespace Movies.Droid
{
    [Activity(Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
              DataScheme = App.OAUTH_SCHEME,
              DataHost = App.OAUTH_HOST,
              AutoVerify = true)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            var app = Xamarin.Forms.Application.Current;

            base.OnCreate(savedInstanceState);

            Android.Gms.Ads.MobileAds.Initialize(ApplicationContext);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(true);
            FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageViewHandler();

            FirebaseApp.InitializeApp(this);
            AndroidAppLinks.Init(this);
            
            LoadApplication(app as App ?? new App());
        }
    }
}