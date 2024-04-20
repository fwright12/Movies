using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace Movies.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App(new JavaScriptEvaluator()));

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init();
            FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageSourceHandler();
            Google.MobileAds.MobileAds.SharedInstance.Start(CompletionHandler);

            return base.FinishedLaunching(app, options);
        }

        public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        {
            if (userActivity.ActivityType == NSUserActivityType.BrowsingWeb && userActivity.WebPageUrl.ToString().StartsWith(App.BASE_OAUTH_REDIRECT_URI.ToString()) && Xamarin.Essentials.Platform.GetCurrentUIViewController() is SafariServices.SFSafariViewController safari)
            {
                safari.DismissViewController(true, null);
            }

            return base.ContinueUserActivity(application, userActivity, completionHandler);
        }

        private void CompletionHandler(Google.MobileAds.InitializationStatus status) { }
    }
}
