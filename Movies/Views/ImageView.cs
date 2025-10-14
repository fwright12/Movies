using Microsoft.Maui.Layouts;
using System.ComponentModel;
using System.Globalization;

namespace Movies.Views
{
    public class TemplateConverter : IValueConverter<object, View>
    {
        public ElementTemplate? Template { get; set; }
        public object? EmptyView { get; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return ObjectToView(EmptyView);
            }
            else
            {
                var template = parameter as ElementTemplate ?? Template;
                if (template is DataTemplateSelector selector)
                {
                    template = selector.SelectTemplate(value, null);
                }

                return template?.CreateContent();
            }
        }

        public object? ConvertBack(View? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private View? ObjectToView(object? obj)
        {
            if (obj is View view)
            {
                return view;
            }
            else if (obj is ElementTemplate template)
            {
                return template.CreateContent() as View;
            }
            else
            {
                return null;
            }
        }
    }

    public class AspectRatioConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || sourceType.IsAssignableFrom(typeof(double));

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is double d)
            {
                return d;
            }
            else if (value is string str)
            {
                var colon = str.IndexOf(":");

                if (colon == -1)
                {
                    if (double.TryParse(str, out var result))
                    {
                        return result;
                    }
                }
                else
                {
                    if (double.TryParse(str.Substring(0, colon), out var width) && double.TryParse(str.Substring(colon + 1), out var height))
                    {
                        return width / height;
                    }
                }
            }

            throw new InvalidOperationException($"Cannot convert {value} into {typeof(double)}");
        }
    }

    public class AspectContentView : ContentView
    {
        public static readonly BindableProperty AspectRequestProperty = BindableProperty.Create(nameof(AspectRequest), typeof(double), typeof(AspectContentView), -1d);

        [TypeConverter(typeof(AspectRatioConverter))]
        public double AspectRequest
        {
            get => (double)GetValue(AspectRequestProperty);
            set => SetValue(AspectRequestProperty, value);
        }

        public Aspect Aspect { get; set; } = Aspect.AspectFit;

        public AspectContentView()
        {
            //HorizontalOptions = LayoutOptions.Center;
            //VerticalOptions = LayoutOptions.Center;
        }

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            var size = base.MeasureOverride(widthConstraint, heightConstraint);
            return size;
            widthConstraint = Math.Max(widthConstraint, size.Width);
            heightConstraint = Math.Max(heightConstraint, size.Height);
            
            size = Measure(size.Width, size.Height, AspectRequest, Aspect);
            if (size.Width > widthConstraint)
            {
                size.Width = widthConstraint;
            }
            else if (size.Height > heightConstraint)
            {
                size.Height = heightConstraint;
            }
            else
            {
                return size;
            }

            // AspectFill will always shrink
            return Measure(size.Width, size.Height, AspectRequest, Aspect.AspectFill);
            //return Measure(size.Width, size.Height, widthConstraint, heightConstraint, AspectRequest, Aspect);
        }

        private static Size Measure(double width, double height, double widthConstraint, double heightConstraint, double ratio, Aspect aspect)
        {
            if (aspect != Aspect.Fill && ratio > 0)
            {
                if (width / height < ratio)
                {
                    if (aspect == Aspect.AspectFit)
                    {
                        width = Math.Min(widthConstraint, height * ratio);
                    }
                    height = width / ratio;
                }
                else
                {
                    if (aspect == Aspect.AspectFit)
                    {
                        height = Math.Min(heightConstraint, width / ratio);
                    }
                    width = height * ratio;
                }
            }

            return new Size(width, height);
        }

        private static Size Measure(double width, double height, double ratio, Aspect aspect)
        {
            if (aspect != Aspect.Fill && ratio > 0)
            {
                if (width / height < ratio == (aspect == Aspect.AspectFill))
                {
                    height = width / ratio;
                }
                else
                {
                    width = height * ratio;
                }
            }

            return new Size(width, height);
        }

        private Size MakeAspect(double width, double height)
        {
            //width -= Margin.HorizontalThickness;
            //height -= Margin.VerticalThickness;

            //if (!double.IsInfinity(widthConstraint) && double.IsInfinity(heightConstraint))
            if (width / height < AspectRequest)
            {
                height = width / AspectRequest;
            }
            else //if (!double.IsInfinity(heightConstraint) && double.IsInfinity(widthConstraint))
            {
                width = height * AspectRequest;
            }

            // iOS seems to have trouble handling double precision in CollectionView
            // TODO Xamarin.Forms.Device.RuntimePlatform is no longer supported. Use Microsoft.Maui.Devices.DeviceInfo.Platform instead. For more details see https://learn.microsoft.com/en-us/dotnet/maui/migration/forms-projects#device-changes
            //if (Device.RuntimePlatform == Device.iOS)
            //{
            //    size.Width = Math.Round(size.Width);
            //    size.Height = Math.Round(size.Height);
            //}

            return new Size(width, height);
            //return new Size(width + Margin.HorizontalThickness, height + Margin.VerticalThickness);
        }

        protected override Size ArrangeOverride(Rect bounds)
        {
            return base.ArrangeOverride(bounds);
            Aspect aspect;
            if (Aspect == Aspect.AspectFit)
            {
                aspect = Aspect.AspectFill;
            }
            else if (Aspect == Aspect.AspectFill)
            {
                aspect = Aspect.AspectFit;
            }
            else
            {
                aspect = Aspect;
            }

            var backup = DesiredSize;
            DesiredSize = Measure(bounds.Width, bounds.Height, AspectRequest, aspect);

            try
            {
                return base.ArrangeOverride(bounds);
            }
            finally
            {
                DesiredSize = backup;
            }
        }
    }

    //[ContentProperty(nameof(Image))]
    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public class ImageView : AspectContentView, IImageElement
    {
        public static readonly BindableProperty SourceProperty = Image.SourceProperty;

        public static readonly BindableProperty AspectProperty = Image.AspectProperty;

        public static readonly BindableProperty IsOpaqueProperty = Image.IsOpaqueProperty;

        public static readonly BindableProperty IsLoadingProperty = Image.IsLoadingProperty;

        public static readonly BindableProperty IsAnimationPlayingProperty = Image.IsAnimationPlayingProperty;

        public static readonly BindableProperty MaximumWidthProperty = BindableProperty.Create(nameof(MaximumWidth), typeof(double), typeof(ImageView));

        public static readonly BindableProperty MaximumHeightProperty = BindableProperty.Create(nameof(MaximumHeight), typeof(double), typeof(ImageView));

        public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(Image), typeof(ImageView));

        public static readonly BindableProperty AltTextProperty = BindableProperty.Create(nameof(AltText), typeof(string), typeof(ImageView));

        public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(CornerRadius), typeof(CornerRadius), typeof(ImageView), new CornerRadius());

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

        [TypeConverter(typeof(ImageSourceConverter))]
        public ImageSource Source
        {
            get => Image.Source;
            set => Image.Source = value;
        }

        public ImageView()
        {
            Content = Image = new Image();
        }

        public void RaiseImageSourcePropertyChanged()
        {
            ((IImageElement)Image).RaiseImageSourcePropertyChanged();
        }

        public void OnImageSourceSourceChanged(object sender, EventArgs e)
        {
            ((IImageElement)Image).OnImageSourceSourceChanged(sender, e);
        }

        protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
        {
            if (IsSet(WidthRequestProperty) || IsSet(HeightRequestProperty) || Content == null)
            {
                return base.MeasureOverride(widthConstraint, heightConstraint);
            }
            else
            {
                return Content.Measure(widthConstraint, heightConstraint);
            }
        }

        /*protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            //widthConstraint = Math.Min(widthConstraint, MaximumWidth);
            //heightConstraint = Math.Min(heightConstraint, MaximumHeight);

            return Content?.Measure(widthConstraint, heightConstraint) ?? base.OnMeasure(widthConstraint, heightConstraint);
        }*/
    }
}