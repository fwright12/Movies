namespace Movies.Views
{
    //[ContentProperty(nameof(Image))]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageView : ContentView, IImageElement
    {
        public static readonly BindableProperty SourceProperty = Image.SourceProperty;

        public static readonly BindableProperty AspectProperty = Image.AspectProperty;

        public static readonly BindableProperty IsOpaqueProperty = Image.IsOpaqueProperty;

        public static readonly BindableProperty IsLoadingProperty = Image.IsLoadingProperty;

        public static readonly BindableProperty IsAnimationPlayingProperty = Image.IsAnimationPlayingProperty;

        public static readonly BindableProperty AspectRequestProperty = BindableProperty.Create(nameof(AspectRequest), typeof(double), typeof(ImageView), -1d);

        public static readonly BindableProperty MaximumWidthProperty = BindableProperty.Create(nameof(MaximumWidth), typeof(double), typeof(ImageView));

        public static readonly BindableProperty MaximumHeightProperty = BindableProperty.Create(nameof(MaximumHeight), typeof(double), typeof(ImageView));

        public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(Image), typeof(ImageView));

        public static readonly BindableProperty AltTextProperty = BindableProperty.Create(nameof(AltText), typeof(string), typeof(ImageView));

        public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(CornerRadius), typeof(CornerRadius), typeof(ImageView), new CornerRadius());

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

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Image Image
        {
            get => (Image)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

		public Aspect Aspect
        {
            get => Image.Aspect;
            set => Image.Aspect = value;
        }

        public bool IsLoading
        {
            get => Image.IsLoading;
        }

        public bool IsOpaque
        {
            get => Image.IsOpaque;
            set => Image.IsOpaque = value;
        }

        public bool IsAnimationPlaying
        {
            get => Image.IsAnimationPlaying;
            set => Image.IsAnimationPlaying = value;
        }

        [System.ComponentModel.TypeConverter(typeof(ImageSourceConverter))]
        public ImageSource Source
        {
            get => Image.Source;
            set => Image.Source = value;
        }

        public ImageView()
        {
            Content = Image = new Image();
            InitializeComponent();
        }

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        //protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            if (AspectRequest > 0 && !(double.IsPositiveInfinity(widthConstraint) && double.IsPositiveInfinity(heightConstraint)))
            {
                Size size;

                //if (!double.IsInfinity(widthConstraint) && double.IsInfinity(heightConstraint))
                if (widthConstraint / heightConstraint < AspectRequest)
                {
                    size = new Size(widthConstraint, widthConstraint / AspectRequest);
                }
                else //if (!double.IsInfinity(heightConstraint) && double.IsInfinity(widthConstraint))
                {
                    size = new Size(heightConstraint * AspectRequest, heightConstraint);
                }

                // iOS seems to have trouble handling double precision in CollectionView
                // TODO Xamarin.Forms.Device.RuntimePlatform is no longer supported. Use Microsoft.Maui.Devices.DeviceInfo.Platform instead. For more details see https://learn.microsoft.com/en-us/dotnet/maui/migration/forms-projects#device-changes
                                if (Device.RuntimePlatform == Device.iOS)
                {
                    size.Width = Math.Round(size.Width);
                    size.Height = Math.Round(size.Height);
                }

                return new SizeRequest(size);
            }
            else
            {
                return Content?.Measure(widthConstraint, heightConstraint) ?? base.OnMeasure(widthConstraint, heightConstraint);
            }
        }

        public void RaiseImageSourcePropertyChanged()
        {
            ((IImageElement)Image).RaiseImageSourcePropertyChanged();
        }

        public void OnImageSourceSourceChanged(object sender, EventArgs e)
        {
            ((IImageElement)Image).OnImageSourceSourceChanged(sender, e);
        }

        /*protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            //widthConstraint = Math.Min(widthConstraint, MaximumWidth);
            //heightConstraint = Math.Min(heightConstraint, MaximumHeight);

            return Content?.Measure(widthConstraint, heightConstraint) ?? base.OnMeasure(widthConstraint, heightConstraint);
        }*/
    }
}