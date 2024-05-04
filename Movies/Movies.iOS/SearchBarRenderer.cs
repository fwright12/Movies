using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(SearchBar), typeof(Movies.iOS.SearchBarRenderer))]
namespace Movies.iOS
{
    public class SearchBarRenderer : Xamarin.Forms.Platform.iOS.SearchBarRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<SearchBar> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                UISearchBar.Appearance.TintColor = e.NewElement?.CancelButtonColor.ToUIColor();
                Control.ShowsCancelButton = false;
                return;

                Control.TextChanged += (sender, e1) =>
                {
                    Control.ShowsCancelButton = true;
                };
                Control.OnEditingStarted += (sender, e1) =>
                {
                    Control.SetShowsCancelButton(true, true);
                };
                Control.OnEditingStopped += (sender, e1) =>
                {
                    Control.SetShowsCancelButton(false, true);
                };
            }
        }

        protected override UISearchBar CreateNativeControl()
        {
            var control = base.CreateNativeControl();
            control.ShowsCancelButton = false;
            return control;
        }
    }
}