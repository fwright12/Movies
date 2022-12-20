using Android.Content;
using Android.Gms.Ads;
using Android.Gms.Ads.NativeAd;
using Google.Ads.Mediation.Admob;
using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.Android;
using XFAdView = Xamarin.Forms.AdView;

[assembly: Xamarin.Forms.ExportRenderer(typeof(XFAdView), typeof(Movies.Droid.AdRenderer))]
[assembly: Xamarin.Forms.ExportRenderer(typeof(Xamarin.Forms.NativeAdView), typeof(Movies.Droid.NativeAdRenderer))]
//[assembly: ExportRenderer(typeof(XFAdView), typeof(Movies.Droid.TestAdViewRenderer))]
namespace Movies.Droid
{
    public class NativeAdRenderer : ViewRenderer<Xamarin.Forms.NativeAdView, NativeAdView>
    {
        public NativeAdRenderer(Context context) : base(context) { }

        private void GetAd()
        {
            var adLoader = new AdLoader.Builder(Context, "ca-app-pub-3940256099942544/2247696110");
            var listener = new NativeListener(nativeAd =>
            {
                Element.Headline = nativeAd.Headline;

                SetNativeControl(new NativeAdView(Context));
            });
            adLoader.ForNativeAd(listener);
            adLoader.WithAdListener(listener);
            adLoader.WithNativeAdOptions(new NativeAdOptions.Builder().Build());

            adLoader.Build().LoadAd(new AdRequest.Builder().Build());
        }

        private class NativeListener : AdListener, NativeAd.IOnNativeAdLoadedListener
        {
            private Action<NativeAd> Action;

            public NativeListener(Action<NativeAd> action)
            {
                Action = action;
            }

            public override void OnAdOpened()
            {
                base.OnAdOpened();
            }

            public override void OnAdFailedToLoad(LoadAdError p0)
            {
                base.OnAdFailedToLoad(p0);
                System.Diagnostics.Debug.WriteLine(p0);
            }

            public void OnNativeAdLoaded(NativeAd p0) => Action(p0);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.NativeAdView> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                GetAd();
            }
        }
    }

    public class AdRenderer : ViewRenderer<XFAdView, AdView>
    {
        public AdRenderer(Context context) : base(context) { }

        //Note you may want to adjust this, see further down.
        private AdView AdView
        {
            get
            {
                if (adView != null)
                {
                    return adView;
                }
                else if (Element?.AdUnitID == null || Element?.AdSize == null)
                {
                    return null;
                }

                adView = new AdView(Context)
                {
                    AdUnitId = Element.AdUnitID,
                };
                UpdateAdSize();
                adView.LoadAd(new AdRequest.Builder().Build());

                return adView;
            }
        }
        private AdView adView;

        private class Listener : AdListener
        {
            private Action OnLoaded;

            public Listener(Action onLoaded)
            {
                OnLoaded = onLoaded;
            }

            public override void OnAdLoaded()
            {
                base.OnAdLoaded();
                OnLoaded();
            }
        }

        private void UpdateAdSize()
        {
            var adSize = GetAdSize();
            AdView.AdSize = adSize;

            if (Element.HeightRequest == XFAdView.InlineBannerHeight)
            {
                AdView.AdListener = new Listener(() =>
                {
                    try
                    {
                        Element.HeightRequest = AdView.AdSize.Height;
                    }
                    catch { }
                });
            }
            else
            {
                Element.HeightRequest = AdView.AdSize.Height;
            }

            Element.WidthRequest = AdView.AdSize.Width;
        }

        private AdSize GetAdSize()
        {
            var adSize = Element.AdSize;

            if (adSize == Xamarin.Forms.AdSizes.Banner)
            {
                return AdSize.Banner;
            }
            else if (adSize == Xamarin.Forms.AdSizes.MediumRectangle)
            {
                return AdSize.MediumRectangle;
            }
            else if (Element.HeightRequest == XFAdView.InlineBannerHeight)
            {
                return AdSize.GetCurrentOrientationInlineAdaptiveBannerAdSize(Context, (int)Element.WidthRequest);
            }
            else
            {
                return new AdSize((int)Element.WidthRequest, (int)Element.HeightRequest);
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == XFAdView.AdSizeProperty.PropertyName)
            {
                UpdateAdSize();
            }
        }

        protected override void OnElementChanged(ElementChangedEventArgs<XFAdView> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                //MainPage.LoadAd = CreateNativeAdControl;
                //CreateNativeAdControl();
                SetNativeControl(AdView);
            }
        }
    }

    public class TestAdViewRenderer : ViewRenderer<XFAdView, AdView>
    {
        public TestAdViewRenderer(Context context) : base(context) { }

        string adUnitId = "ca-app-pub-3940256099942544/6300978111";
        //Note you may want to adjust this, see further down.
        AdSize adSize = AdSize.Banner;
        AdView adView;

        AdView CreateNativeAdControl()
        {
            if (adView != null)
                return adView;

            // This is a string in the Resources/values/strings.xml that I added or you can modify it here. This comes from admob and contains a / in it
            //adUnitId = Forms.Context.Resources.GetString(Resource.String.banner_ad_unit_id);
            adView = new AdView(Context)
            {
                AdSize = adSize,
                AdUnitId = adUnitId,
                LayoutParameters = new Android.Widget.LinearLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent)
            };

            //var test = new AdLoader.Builder(Context, adUnitId);
            //adView.LayoutParameters = adParams;

            var extras = new Android.OS.Bundle();
            extras.PutString("npa", "1");
            var request = new AdRequest.Builder().AddNetworkExtrasBundle(new AdMobAdapter().Class, extras).Build();
            foreach (var keyword in App.AdKeywords)
            {
                request.Keywords.Add(keyword);
            }

            adView.LoadAd(request);
            return adView;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<XFAdView> e)
        {
            base.OnElementChanged(e);
            if (Control == null)
            {
                CreateNativeAdControl();
                SetNativeControl(adView);
            }
        }
    }
}