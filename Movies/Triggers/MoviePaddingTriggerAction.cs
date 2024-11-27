using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies
{
    public class MoviePaddingTriggerAction : TriggerAction<Layout>
    {
        protected override void Invoke(Layout sender)
        {
            //if (Device.RuntimePlatform == Device.iOS)
            //return;
            Microsoft.Maui.Controls.Compatibility.Layout root = Microsoft.Maui.Controls.Compatibility.ElementExtensions.Parent<ScrollView>(sender);
            View child = sender;//.Children[sender.Children.Count - 1];

            if (root == null)
            {
                return;
            }

            //Print.Log(root.Height, child.PositionOn(root).Y, root.Padding.Top, child.Height);
            var padding = root.Height - (Microsoft.Maui.Controls.Compatibility.VisualElementExtensions.PositionOn(child, root).Y - root.Padding.Top) - child.Height;
            // Overdraw bottom padding to fix ios bug
            // TODO Xamarin.Forms.Device.RuntimePlatform is no longer supported. Use Microsoft.Maui.Devices.DeviceInfo.Platform instead. For more details see https://learn.microsoft.com/en-us/dotnet/maui/migration/forms-projects#device-changes
                        root.Padding = new Thickness(0, padding, 0, Device.RuntimePlatform == Device.iOS ? (-padding * 2) : 0);
        }
    }
}
