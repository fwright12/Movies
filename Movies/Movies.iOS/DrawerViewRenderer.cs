using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(DrawerView), typeof(Movies.iOS.DrawerViewRenderer))]

namespace Movies.iOS
{
    public class DrawerViewRenderer : ViewRenderer
    {
        public DrawerViewRenderer()
        {
            UIKeyboard.Notifications.ObserveWillChangeFrame((sender, e) =>
            {
                CoreGraphics.CGRect rect = UIKeyboard.FrameEndFromNotification(e.Notification);
                //System.Diagnostics.Debug.WriteLine("keyboard size changed, " + e.FrameBegin + ", " + e.FrameEnd);
                //System.Diagnostics.Debug.WriteLine(UIApplication.SharedApplication.KeyWindow.SafeAreaInsets + ", " + SafeAreaLayoutGuide.LayoutFrame);
                /*if (rect.Width != Xamarin.Forms.Application.Current.MainPage.Width)
                {
                    rect = new CoreGraphics.CGRect(0, 0, Xamarin.Forms.Application.Current.MainPage.Width, 0);
                }*/

                if (Element != null)
                {
                    double delta = e.FrameBegin.Y - e.FrameEnd.Y;
                    var insets = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets;
                    delta -= (insets.Top + insets.Bottom) * System.Math.Sign(delta);
                    
                    Element.Margin = new Thickness(Element.Margin.Left, Element.Margin.Top, Element.Margin.Right, Element.Margin.Bottom + delta);
                }
            });
        }
    }
}