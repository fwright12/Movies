﻿using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(SearchBar), typeof(Movies.iOS.SearchBarRenderer))]
namespace Movies.iOS
{
    public class SearchBarRenderer : Xamarin.Forms.Platform.iOS.SearchBarRenderer
    {
        private bool IsEditing;

        protected override void OnElementChanged(ElementChangedEventArgs<SearchBar> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                UISearchBar.Appearance.TintColor = e.NewElement?.CancelButtonColor.ToUIColor();

                Control.OnEditingStarted -= ControlEditingStarted;
                Control.OnEditingStopped -= ControlEditingStopped;

                Control.OnEditingStarted += ControlEditingStarted;
                Control.OnEditingStopped += ControlEditingStopped;
            }
        }

        public override void UpdateCancelButton()
        {
            base.UpdateCancelButton();

            if (!IsEditing)
            {
                Control.ShowsCancelButton = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Control != null)
            {
                Control.OnEditingStarted -= ControlEditingStarted;
                Control.OnEditingStopped -= ControlEditingStopped;
            }

            base.Dispose(disposing);
        }

        private void ControlEditingStarted(object sender, System.EventArgs e)
        {
            IsEditing = true;
            Control.SetShowsCancelButton(true, true);
        }

        private void ControlEditingStopped(object sender, System.EventArgs e)
        {
            IsEditing = false;
            Control.SetShowsCancelButton(false, true);
        }
    }
}