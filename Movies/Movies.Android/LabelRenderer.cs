using Android.Content;
using Android.Util;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

//[assembly: ExportRenderer(typeof(Label), typeof(Movies.Droid.LabelRenderer))]
namespace Movies.Droid
{
    class LabelRenderer : Xamarin.Forms.Platform.Android.LabelRenderer
    {
        public LabelRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);
            UpdateAutoSizeFont();
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
            Control.SetAutoSizeTextTypeWithDefaults(autoSize ? Android.Widget.AutoSizeTextType.Uniform : Android.Widget.AutoSizeTextType.None);
        }
    }
}