using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Movies.Views
{
    //[ContentProperty(nameof(Image))]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageView : Frame
    {
        public const double ThreeToTwo = 3d / 2d;

        public static readonly BindableProperty AspectRequestProperty = BindableProperty.Create(nameof(AspectRequest), typeof(double), typeof(ImageView), -1d);

        public static readonly BindableProperty MaximumWidthProperty = BindableProperty.Create(nameof(MaximumWidth), typeof(double), typeof(ImageView));

        public static readonly BindableProperty MaximumHeightProperty = BindableProperty.Create(nameof(MaximumHeight), typeof(double), typeof(ImageView));

        public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(Image), typeof(ImageView));

        public static readonly BindableProperty AltTextProperty = BindableProperty.Create(nameof(AltText), typeof(string), typeof(ImageView));

        public double AspectRequest
        {
            get => (double)GetValue(AspectRequestProperty);
            set => SetValue(AspectRequestProperty, value);
        }

        public double MaximumWidth
        {
            get => (double)GetValue(MaximumWidthProperty);
            set => SetValue(MaximumWidthProperty, value);
        }

        public double MaximumHeight
        {
            get => (double)GetValue(MaximumHeightProperty);
            set => SetValue(MaximumHeightProperty, value);
        }

        public string AltText
        {
            get => (string)GetValue(AltTextProperty);
            set => SetValue(AltTextProperty, value);
        }

        public Image Image
        {
            get => (Image)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public ImageView()
        {
            InitializeComponent();
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            SizeRequest request = Content?.Measure(widthConstraint, heightConstraint) ?? base.OnMeasure(widthConstraint, heightConstraint);

            if (AspectRequest > 0 && !(double.IsPositiveInfinity(widthConstraint) && double.IsPositiveInfinity(heightConstraint)))
            {
                //if (!double.IsInfinity(widthConstraint) && double.IsInfinity(heightConstraint))
                if (widthConstraint / heightConstraint < AspectRequest)
                {
                    request.Request = new Size(widthConstraint, widthConstraint / AspectRequest);
                }
                else //if (!double.IsInfinity(heightConstraint) && double.IsInfinity(widthConstraint))
                {
                    request.Request = new Size(heightConstraint * AspectRequest, heightConstraint);
                }
            }

            return request;
        }

        /*protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            //widthConstraint = Math.Min(widthConstraint, MaximumWidth);
            //heightConstraint = Math.Min(heightConstraint, MaximumHeight);

            return Content?.Measure(widthConstraint, heightConstraint) ?? base.OnMeasure(widthConstraint, heightConstraint);
        }*/
    }
}