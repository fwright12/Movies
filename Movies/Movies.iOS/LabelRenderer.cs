using System.ComponentModel;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

//[assembly: ExportRenderer(typeof(Label), typeof(Movies.iOS.LabelRenderer))]
namespace Movies.iOS
{
    class LabelRenderer : Xamarin.Forms.Platform.iOS.LabelRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                UpdateAutoSizeFont();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (Control == null || Element == null)
            {
                return;
            }

            if (e.PropertyName == Views.Extensions.AutoSizeFontProperty.PropertyName)
            {
                UpdateAutoSizeFont();
            }
        }

        private void UpdateAutoSizeFont()
        {
            var autoSize = Views.Extensions.GetAutoSizeFont(Element);
            Control.AdjustsFontSizeToFitWidth = autoSize;
        }
    }
}