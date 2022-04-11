using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CoreGraphics;
using Foundation;
using Google.MobileAds;
using Movies.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(AdView), typeof(Movies.iOS.AdRenderer))]

namespace Movies.iOS
{
    public class AdRenderer : ViewRenderer<AdView, BannerView>
    {
        private BannerView NativeAdView
        {
            get
            {
                if (nativeAdView != null)
                {
                    return nativeAdView;
                }
                else if (Element?.AdUnitID == null || Element?.AdSize == null)
                {
                    return null;
                }

                // Setup your BannerView, review AdSizeCons class for more Ad sizes. 
                //adView = new BannerView(size: AdSizeCons.SmartBannerPortrait, origin: new CGPoint(0, UIScreen.MainScreen.Bounds.Size.Height - AdSizeCons.Banner.Size.Height))
                nativeAdView = new BannerView(GetAdSize())
                {
                    AdUnitId = Element.AdUnitID,
                    RootViewController = GetVisibleViewController()
                };
                AdSizeUpdated();

                // Wire AdReceived event to know when the Ad is ready to be displayed
                /*adView.AdReceived += (object sender, EventArgs e) =>
                {
                    //ad has come in
                };*/

                nativeAdView.LoadRequest(Request.GetDefaultRequest());
                return nativeAdView;
            }
        }
        private BannerView nativeAdView;

        protected override void OnElementChanged(ElementChangedEventArgs<AdView> e)
        {
            base.OnElementChanged(e);
            
            if (Control == null && nativeAdView == null)
            {
                SetNativeControl(NativeAdView);
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == AdView.AdSizeProperty.PropertyName)
            {
                return;
                var adSize = GetAdSize();
                NativeAdView.AdSize = adSize;

                AdSizeUpdated();
            }
        }

        private void AdSizeUpdated()
        {
            var size = NativeAdView.AdSize.Size;
            /*if (Element.HeightRequest == AdView.InlineBannerHeight)
            {
                AdView.AdListener = new Listener(() =>
                {
                    Print.Log("loaded", AdView.AdSize.Height);
                    Element.HeightRequest = AdView.AdSize.Height;
                });
            }
            else
            {
                Element.HeightRequest = AdView.AdSize.Height;
            }*/

            Element.HeightRequest = size.Height;
            Element.WidthRequest = size.Width;
        }

        private AdSize GetAdSize()
        {
            var adSize = Element.AdSize;

            if (adSize == AdSizes.Banner)
            {
                return AdSizeCons.Banner;
            }
            else if (adSize == AdSizes.MediumRectangle)
            {
                return AdSizeCons.MediumRectangle;
            }
            else if (Element.HeightRequest == AdView.InlineBannerHeight)
            {
                return AdSizeCons.MediumRectangle;
            }
            else
            {
                return new AdSize { Size = new CGSize((float)Element.WidthRequest, (float)Element.HeightRequest) };
            }
        }

        /// 
        /// Gets the visible view controller.
        /// 
        /// The visible view controller.
        UIViewController GetVisibleViewController()
        {
            var rootController = UIApplication.SharedApplication.KeyWindow.RootViewController;

            if (rootController.PresentedViewController == null)
                return rootController;

            if (rootController.PresentedViewController is UINavigationController)
            {
                return ((UINavigationController)rootController.PresentedViewController).VisibleViewController;
            }

            if (rootController.PresentedViewController is UITabBarController)
            {
                return ((UITabBarController)rootController.PresentedViewController).SelectedViewController;
            }

            return rootController.PresentedViewController;
        }
    }
}