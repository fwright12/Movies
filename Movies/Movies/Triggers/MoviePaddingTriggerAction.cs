using Xamarin.Forms;

namespace Movies
{
    public class MoviePaddingTriggerAction : TriggerAction<Layout>
    {
        protected override void Invoke(Layout sender)
        {
            //if (Device.RuntimePlatform == Device.iOS)
            //return;
            Layout root = sender.Parent<ScrollView>();
            View child = sender;//.Children[sender.Children.Count - 1];

            if (root == null)
            {
                return;
            }

            //Print.Log(root.Height, child.PositionOn(root).Y, root.Padding.Top, child.Height);
            var padding = root.Height - (child.PositionOn(root).Y - root.Padding.Top) - child.Height;
            // Overdraw bottom padding to fix ios bug
            root.Padding = new Thickness(0, padding, 0, Device.RuntimePlatform == Device.iOS ? (-padding * 2) : 0);
        }
    }
}
